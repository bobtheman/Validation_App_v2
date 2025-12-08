namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Shared.Models.Authentication;
    using Microsoft.Maui.Storage;
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
                Logout();
                return null;
            }

            try
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", userLoginModel.Username),
                    new KeyValuePair<string, string>("password", userLoginModel.Password)
                });

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(userLoginModel.ServerUrl),
                    Timeout = Timeout.InfiniteTimeSpan
                };

                var response = await client.PostAsync("/token", formData);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        Debug.WriteLine("Received empty response from authentication endpoint");
                        await SetIsAuthenticatedAsync(false);
                        await Logout();
                        return new TokenResponse();
                    }

                    try
                    {
                        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (tokenResponse is null)
                        {
                            Debug.WriteLine("Failed to deserialize token response - result was null");
                            await SetIsAuthenticatedAsync(false);
                            await Logout();
                            return new TokenResponse();
                        }

                        await SetIsAuthenticatedAsync(true);
                        return tokenResponse;
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
                        Debug.WriteLine($"Response content: {jsonString}");
                        await SetIsAuthenticatedAsync(false);
                        await Logout();
                        return new TokenResponse();
                    }
                }

                await SetIsAuthenticatedAsync(false);
                await Logout();
                return new TokenResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                await Logout();
            }
            return new TokenResponse();
        }

        public async Task Logout()
        {
            await SetIsAuthenticatedAsync(false);
        }
    }
}
