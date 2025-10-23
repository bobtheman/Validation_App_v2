namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models.Authentication;
    using System.Diagnostics;
    using System.Text.Json;

    public class AuthService : IAuthService
    {
        private const string AuthenticatedKey = "IsAuthenticated";
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> GetIsAuthenticatedAsync()
        {
            try
            {
                return bool.TryParse(await SecureStorage.GetAsync(AuthenticatedKey), out var result) && result;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetIsAuthenticatedAsync(bool value)
        {
            await SecureStorage.SetAsync(AuthenticatedKey, value.ToString());
        }

        public async Task<TokenResponse> AuthenticateUserAsync(UserLoginModel userLoginModel)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine(Connectivity.Current.NetworkAccess.ToString());
                await Logout();
                return new TokenResponse();
            }

            HttpClient client = null;

            try
            {
                // Get the server URL and fix it for Android emulator
                var serverUrl = userLoginModel.ServerUrl;
                var originalHost = serverUrl; // Default host header value

#if ANDROID
                // Android emulator: localhost = emulator itself, use 10.0.2.2 for host machine
                if (serverUrl.Contains("localhost") || serverUrl.Contains("127.0.0.1"))
                {
                    // Extract the original host for the Host header
                    var uri = new Uri(serverUrl);
                    originalHost = uri.Host + (uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "");
                    
                    serverUrl = serverUrl.Replace("localhost", "10.0.2.2")
                                         .Replace("127.0.0.1", "10.0.2.2");
                }
#endif

                Debug.WriteLine($"Connecting to: {serverUrl}/token");

                // For HTTP (non-HTTPS) requests, use default HttpClient
                // For HTTPS requests with self-signed certs, use custom handler
                bool useCustomHandler = serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                
                client = useCustomHandler 
                    ? new HttpClient(GetPlatformMessageHandler()) { Timeout = TimeSpan.FromSeconds(30) }
                    : new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{serverUrl}/token");
                request.Headers.Add("Accept", "application/json");
                
#if ANDROID
                // Override the Host header to use localhost instead of 10.0.2.2
                request.Headers.Host = originalHost;
#endif

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "password"),
                    new("username", userLoginModel.Username),
                    new("password", userLoginModel.Password)
                };

                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await SetIsAuthenticatedAsync(false);
                    return new TokenResponse();
                }

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse != null)
                {
                    await SetIsAuthenticatedAsync(true);
                    return tokenResponse;
                }

                await SetIsAuthenticatedAsync(false);
                return new TokenResponse();
            }
            catch (Exception ex)
            {
                await Logout();
                return new TokenResponse();
            }
            finally
            {
                client?.Dispose();
            }
        }

        private static HttpMessageHandler GetPlatformMessageHandler()
        {
#if ANDROID
            var handler = new Xamarin.Android.Net.AndroidMessageHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
#elif IOS || MACCATALYST
            var handler = new NSUrlSessionHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
#else
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
#endif

#if DEBUG
            // Bypass SSL certificate validation in debug mode only
#if ANDROID
            if (handler is Xamarin.Android.Net.AndroidMessageHandler androidHandler)
            {
                androidHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
#elif IOS || MACCATALYST
            if (handler is NSUrlSessionHandler nsHandler)
            {
                nsHandler.TrustOverrideForUrl = (sender, url, trust) => true;
            }
#else
            if (handler is HttpClientHandler httpHandler)
            {
                httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
#endif
#endif
            return handler;
        }

        public async Task Logout()
        {
            await SetIsAuthenticatedAsync(false);
        }
    }
}
