namespace AccreditValidation.Components.Layout
{
    using AccreditValidation.Components.Base.Notification;
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Shared.Services.AlertService;
    using AccreditValidation.Shared.Services.Notification;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Diagnostics;
    using System.Globalization;

    public partial class MainLayout : IDisposable
    {
        [Inject] IAppState AppState { get; set; }
        [Inject] IAlertService AlertService { get; set; }
        [Inject] INotificationService NotificationService { get; set; }
        [Inject] IConfiguration Configuration { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        private Action? _appStateChangedHandler;
        private TaskCompletionSource<bool>? _confirmTcs;
        private NotificationBellComponent? notificationBell;

        private bool IsConfirmVisible { get; set; }
        private bool IsShowModalVisible { get; set; }
        private string ConfirmTitle { get; set; } = string.Empty;
        private string ConfirmMessage { get; set; } = string.Empty;
        private string ModalTitle { get; set; } = string.Empty;
        private string ModalMessage { get; set; } = string.Empty;
        private string OkText { get; set; } = string.Empty;
        private string CancelText { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            AppState.ShowSpinner = true;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

            _appStateChangedHandler = () =>
            {
                InvokeAsync(StateHasChanged);
            };
            AppState.OnChange += _appStateChangedHandler;
            AppState.ShowSpinner = false;
            AlertService.RegisterRefreshCallback(StateHasChanged);
            AlertService.OnConfirmRequested += ShowConfirmDialog;
            AlertService.OnModalRequested += ShowModalDialog;

            // FIXED: Initialize SignalR with proper error handling and debugging
            await InitializeSignalRWithDebugAsync();
        }

        private async Task InitializeSignalRWithDebugAsync()
        {
            try
            {
                Debug.WriteLine("🔵 Starting SignalR initialization...");

                // Read hub URL from appsettings.json
                var hubUrl = Configuration["SignalR:HubUrl"];
                
                if (string.IsNullOrEmpty(hubUrl))
                {
                    Debug.WriteLine("❌ SignalR hub URL not configured in appsettings.json");
                    await AlertService.ShowErrorAlertAsync("Configuration Error", "SignalR hub URL is not configured.");
                    return;
                }

                Debug.WriteLine($"🔵 Hub URL: {hubUrl}");
                Debug.WriteLine($"🔵 Platform: {DeviceInfo.Platform}");

                await NotificationService.InitializeSignalRAsync(hubUrl);

                // Check connection status
                if (NotificationService.IsSignalRConnected)
                {
                    Debug.WriteLine("✅ SignalR connection established successfully!");
                    await AlertService.ShowSuccessAlertAsync("SignalR", "Connected to notification hub");

                    // Send a test notification to verify it's working
                    //await TestSignalRConnectionAsync();
                }
                else
                {
                    Debug.WriteLine("❌ SignalR connection failed!");
                    await AlertService.ShowErrorAlertAsync("SignalR Error", "Failed to connect to notification hub. Check if the server is running.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"❌ HTTP Error connecting to SignalR: {httpEx.Message}");
                Debug.WriteLine($"   Inner Exception: {httpEx.InnerException?.Message}");
                await AlertService.ShowErrorAlertAsync(
                    "Connection Error",
                    $"Cannot reach SignalR server. Make sure the server is running.\n\nError: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error initializing SignalR: {ex.Message}");
                Debug.WriteLine($"   Stack Trace: {ex.StackTrace}");
                await AlertService.ShowErrorAlertAsync(
                    "SignalR Error",
                    $"Failed to initialize notifications: {ex.Message}");
            }
        }

        private async Task TestSignalRConnectionAsync()
        {
            try
            {
                Debug.WriteLine("🧪 Testing SignalR connection with a test notification...");

                // This will show up if the connection is working
                await NotificationService.ShowInAppNotificationAsync(
                    "System",
                    "SignalR notifications are now active!",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Test notification failed: {ex.Message}");
            }
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            //SpinnerService.Show();
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;

            if (_appStateChangedHandler != null)
            {
                AppState.OnChange -= _appStateChangedHandler;
            }

            // FIXED: Properly disconnect SignalR on dispose
            try
            {
                NotificationService.DisconnectSignalRAsync().GetAwaiter().GetResult();
                Debug.WriteLine("✅ SignalR connection closed gracefully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error disconnecting SignalR: {ex.Message}");
            }
        }

        private Task<bool> ShowConfirmDialog(string title, string message, string okText, string cancelText)
        {
            ConfirmTitle = title;
            ConfirmMessage = message;
            OkText = okText;
            CancelText = cancelText;

            _confirmTcs = new TaskCompletionSource<bool>();
            IsConfirmVisible = true;

            StateHasChanged();

            return _confirmTcs.Task;
        }

        private void ConfirmOkClicked()
        {
            IsConfirmVisible = false;
            _confirmTcs?.SetResult(true);
            StateHasChanged();
        }

        private void ConfirmCancelClicked()
        {
            IsConfirmVisible = false;
            _confirmTcs?.SetResult(false);
            StateHasChanged();
        }

        private Task<bool> ShowModalDialog(string title, string message, string okText, string cancelText)
        {
            ModalTitle = title;
            ModalMessage = message;
            OkText = okText;
            CancelText = cancelText;

            IsShowModalVisible = true;
            StateHasChanged();

            return Task.FromResult(true);
        }

        private void ModalOkClicked()
        {
            IsShowModalVisible = false;
            StateHasChanged();
        }

        private void ModalCancelClicked()
        {
            IsShowModalVisible = false;
            StateHasChanged();
        }
    }
}