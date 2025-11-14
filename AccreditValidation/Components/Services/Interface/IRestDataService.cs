namespace AccreditValidation.Components.Services.Interface
{
    using global::AccreditValidation.Requests.V3;
    using global::AccreditValidation.Responses;
    using global::AccreditValidation.Models;

    public interface IRestDataService
    {
        Task<List<Area>> GetAreaAsync();

        Task<ValidationResultsResponse> DownloadOfflineData(bool downloadAllPhotos);
        Task<BadgeValidationResponse> ValidateRequest(BadgeValidationRequest validationRequest);
        Task<SyncValidationResponse> SyncValidationResults(IEnumerable<BadgeValidationRequest> validationRequest);
    }
}
