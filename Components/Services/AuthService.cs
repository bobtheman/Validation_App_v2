namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models.Authentication;
    using RestSharp;
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
                var options = new RestClientOptions(userLoginModel.ServerUrl)
                {
                    Timeout = TimeSpan.FromMilliseconds(-1),
                };
                var client = new RestClient(options);
                var request = new RestRequest("/token", Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "password");
                request.AddParameter("username", userLoginModel.Username);
                request.AddParameter("password", userLoginModel.Password);
                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    string jsonString = response.Content;
                    await SetIsAuthenticatedAsync(true);
                    return JsonSerializer.Deserialize<TokenResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
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
