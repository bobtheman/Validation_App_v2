using AccreditValidation.Shared.Services.Notification;
using AccreditValidation.Shared.Services.SignalRNotificationHub;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AccreditValidation.Components.Services
{
    public class SignalRNotificationHubService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;
        private bool _isConnected;
        private CancellationTokenSource? _connectionCts;
        private bool _isDisposed;

        public event EventHandler<(string Title, string Message, NotificationType Type)>? OnNotificationReceived;
        public event EventHandler<bool>? OnConnectionStateChanged;

        public bool IsConnected => _isConnected;

        public SignalRNotificationHubService(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        /// <summary>
        /// Initialize and start the SignalR connection
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SignalRNotificationHubService));

            try
            {
                // Create a new cancellation token source for this connection attempt
                _connectionCts?.Cancel();
                _connectionCts?.Dispose();
                _connectionCts = new CancellationTokenSource();

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl, options =>
                    {
                        // CRITICAL FIX: Handle self-signed SSL certificates in development
#if DEBUG
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                // Bypass SSL validation for localhost development
                                clientHandler.ServerCertificateCustomValidationCallback =
                                    (sender, certificate, chain, sslPolicyErrors) => true;
                            }
                            return handler;
                        };
#endif
                        // Don't skip negotiation - let SignalR choose best transport
                        options.SkipNegotiation = false;
                    })
                    .WithAutomaticReconnect(new[] {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    })
                    .ConfigureLogging(logging =>
                    {
#if DEBUG
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
#endif
                    })
                    .Build();

                // Register handlers before starting connection
                RegisterHandlers();

                // Handle reconnection events
                _hubConnection.Reconnecting += OnReconnecting;
                _hubConnection.Reconnected += OnReconnected;
                _hubConnection.Closed += OnConnectionClosed;

                await StartConnectionAsync(_connectionCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("SignalR connection initialization was cancelled");
                _isConnected = false;
                OnConnectionStateChanged?.Invoke(this, false);
                // Don't re-throw cancellation exceptions
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing SignalR connection: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _isConnected = false;
                OnConnectionStateChanged?.Invoke(this, false);
                throw; // Re-throw so caller knows it failed
            }
        }

        /// <summary>
        /// Start the SignalR connection with retry logic
        /// </summary>
        private async Task StartConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_hubConnection == null)
                return;

            const int maxRetries = 5;
            int retryCount = 0;

            while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if connection is null again (could have been disposed during retry)
                    if (_hubConnection == null)
                    {
                        Debug.WriteLine("Connection was disposed during retry attempt");
                        return;
                    }

                    Debug.WriteLine($"Attempting to connect to SignalR hub: {_hubUrl}");
                    await _hubConnection.StartAsync(cancellationToken);
                    _isConnected = true;
                    OnConnectionStateChanged?.Invoke(this, true);
                    Debug.WriteLine($"✅ SignalR connection established successfully. Connection ID: {_hubConnection.ConnectionId}");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("SignalR connection attempt was cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Debug.WriteLine($"❌ SignalR connection attempt {retryCount}/{maxRetries} failed: {ex.Message}");

                    if (retryCount >= maxRetries)
                    {
                        _isConnected = false;
                        OnConnectionStateChanged?.Invoke(this, false);
                        Debug.WriteLine($"Failed to connect after {maxRetries} attempts");
                        throw;
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    Debug.WriteLine($"Retrying in {delay.TotalSeconds} seconds...");

                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Retry delay was cancelled");
                        throw;
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("Connection retry loop was cancelled");
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Register SignalR event handlers
        /// </summary>
        private void RegisterHandlers()
        {
            if (_hubConnection == null)
                return;

            Debug.WriteLine("📡 Registering SignalR handlers...");

            // CORRECT: Handle the object sent by the server
            _hubConnection.On<NotificationDto>("ReceiveNotification", (notification) =>
            {
                Debug.WriteLine($"📬 [ReceiveNotification] {notification.Title} - {notification.Message} (Type: {notification.Type})");
                
                // Parse the type string to NotificationType enum
                NotificationType notifType = NotificationType.Info;
                if (Enum.TryParse<NotificationType>(notification.Type, true, out var parsedType))
                {
                    notifType = parsedType;
                }
                
                OnNotificationReceived?.Invoke(this, (notification.Title, notification.Message, notifType));
            });

            Debug.WriteLine("✅ Handler registered successfully!");
        }

        /// <summary>
        /// Stop the SignalR connection
        /// </summary>
        public async Task StopAsync()
        {
            // Cancel any ongoing connection attempts
            _connectionCts?.Cancel();

            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    _isConnected = false;
                    OnConnectionStateChanged?.Invoke(this, false);
                    Debug.WriteLine("SignalR connection stopped");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping SignalR connection: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Manually reconnect to the hub
        /// </summary>
        public async Task ReconnectAsync()
        {
            Debug.WriteLine("Attempting manual reconnection...");
            await StopAsync();
            await Task.Delay(1000);

            // Create new cancellation token for reconnection
            _connectionCts?.Dispose();
            _connectionCts = new CancellationTokenSource();

            await StartConnectionAsync(_connectionCts.Token);
        }

        // Event handlers for connection state changes
        private Task OnReconnecting(Exception? exception)
        {
            _isConnected = false;
            OnConnectionStateChanged?.Invoke(this, false);
            Debug.WriteLine($"🔄 SignalR reconnecting... {exception?.Message}");
            return Task.CompletedTask;
        }

        private Task OnReconnected(string? connectionId)
        {
            _isConnected = true;
            OnConnectionStateChanged?.Invoke(this, true);
            Debug.WriteLine($"✅ SignalR reconnected with ID: {connectionId}");
            return Task.CompletedTask;
        }

        private Task OnConnectionClosed(Exception? exception)
        {
            _isConnected = false;
            OnConnectionStateChanged?.Invoke(this, false);
            Debug.WriteLine($"❌ SignalR connection closed: {exception?.Message}");
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Cancel any ongoing connection attempts first
            _connectionCts?.Cancel();

            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing hub connection: {ex.Message}");
                }
                finally
                {
                    _hubConnection = null;
                }
            }

            _connectionCts?.Dispose();
            _connectionCts = null;
        }
    }
}