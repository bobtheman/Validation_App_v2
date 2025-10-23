namespace AccreditValidation.Components.Services.Interface
{
    using global::AccreditValidation.Models.Authentication;

    public interface IAuthService
    {
        Task<bool> GetIsAuthenticatedAsync();
        Task SetIsAuthenticatedAsync(bool value);
        Task<TokenResponse> AuthenticateUserAsync(UserLoginModel userLoginModel);
        Task Logout();
    }
}
