namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Shared.Constants;
    using global::AccreditValidation.Shared.Models.Authentication;
    using System.Diagnostics;
    using System.Text.Json;

    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // ── Cached options — avoids re-allocating JsonSerializerOptions on every login
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ── Authentication state ──────────────────────────────────────────────

        public async Task<bool> GetIsAuthenticatedAsync()
        {
            try
            {
                var value = await SecureStorage.GetAsync(SecureStorageKeys.IsAuthenticated);
                return bool.TryParse(value, out var result) && result;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetIsAuthenticatedAsync(bool value)
        {
            await SecureStorage.SetAsync(SecureStorageKeys.IsAuthenticated, value.ToString());
        }

        // ── Token authentication ──────────────────────────────────────────────

        public async Task<TokenResponse> AuthenticateUserAsync(UserLoginModel userLoginModel)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine($"[AuthService] No internet: {Connectivity.Current.NetworkAccess}");
                _ = SetIsAuthenticatedAsync(false);
                return new TokenResponse();
            }

            try
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username",   userLoginModel.Username ?? string.Empty),
                    new KeyValuePair<string, string>("password",   userLoginModel.Password ?? string.Empty)
                });

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(userLoginModel.ServerUrl!);
                client.Timeout = TimeSpan.FromSeconds(30);

                var response = await client.PostAsync("/token", formData);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[AuthService] Authentication failed: {response.StatusCode}");
                    _ = SetIsAuthenticatedAsync(false);
                    return new TokenResponse();
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);

                if (tokenResponse?.AccessToken is null)
                {
                    Debug.WriteLine("[AuthService] Token deserialization failed or missing AccessToken.");
                    _ = SetIsAuthenticatedAsync(false);
                    return new TokenResponse();
                }

                _ = SetIsAuthenticatedAsync(true);

                return tokenResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] Exception: {ex.Message}");
                _ = SetIsAuthenticatedAsync(false);
                return new TokenResponse();
            }
        }

        // ── Logout ────────────────────────────────────────────────────────────

        public async Task Logout()
        {
            await SetIsAuthenticatedAsync(false);
        }
    }
}