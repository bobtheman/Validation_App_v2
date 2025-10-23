namespace AccreditValidation.Components.Services.Interface
{
    using global::AccreditValidation.Requests;
    using global::AccreditValidation.Responses;
    using global::AccreditValidation.Models;

    public interface IRestDataService
    {
        Task<List<Area>> GetAreaAsync();

        Task<ValidationResultsResponse> DownloadOfflineData(bool downloadAllPhotos);
        Task<BadgeValidationResponse> ValidateRequest(BadgeValidationRequest validationRequest);

        Task<BadgeValidationResponse> SyncValidationResults(IEnumerable<BadgeValidationRequest> request);
    }
}
