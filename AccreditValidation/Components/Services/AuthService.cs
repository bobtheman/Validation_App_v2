namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Shared.Models.Authentication;
    using Microsoft.Maui.Storage;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text.Json;

    public class AuthService : IAuthService
    {
        private const string AuthenticatedKey = "IsAuthenticated";
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

            try
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", userLoginModel.Username ?? string.Empty),
                    new KeyValuePair<string, string>("password", userLoginModel.Password ?? string.Empty)
                });

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(userLoginModel.ServerUrl!);
                client.Timeout = TimeSpan.FromSeconds(30); // Use reasonable timeout instead of infinite

                var response = await client.PostAsync("/token", formData);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

                    if (tokenResponse?.AccessToken is null)
                    {
                        Debug.WriteLine("Failed to deserialize token response or missing access token");
                        await SetIsAuthenticatedAsync(false);
                        return new TokenResponse();
                    }

                    await SetIsAuthenticatedAsync(true);
                    return tokenResponse;
                }

                Debug.WriteLine($"Authentication failed with status: {response.StatusCode}");
                await SetIsAuthenticatedAsync(false);
                return new TokenResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                await SetIsAuthenticatedAsync(false);
            }
            return new TokenResponse();
        }

        public async Task Logout()
        {
            await SetIsAuthenticatedAsync(false);
        }
    }
}
