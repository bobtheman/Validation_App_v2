using AccreditValidation.Shared.Models.Authentication;

namespace AccreditValidation.Components.Services.Interface
{
    public interface IAuthService
    {
        Task<bool> GetIsAuthenticatedAsync();
        Task SetIsAuthenticatedAsync(bool value);
        Task<TokenResponse> AuthenticateUserAsync(UserLoginModel userLoginModel);
        Task Logout();
    }
}
