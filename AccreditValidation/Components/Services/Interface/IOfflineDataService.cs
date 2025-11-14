using AccreditValidation.Models;
using AccreditValidation.Requests.V3;
using AccreditValidation.Responses;
using SQLite;

namespace AccreditValidation.Components.Services.Interface
{
    public interface IOfflineDataService
    {
        Task InitializeAsync();
        Task SetOfflineAreaAsync();
        Task SetLocalValidationResultAsync(ValidationResultsResponse validationResultsResponse);
        Task<BadgeValidationResponse> ValidateRequestOffline(BadgeValidationRequest badgeValidationRequest);
        Task<List<Area>> GetAllAreasAsync();
        Task<List<Badge>> GetAllBadgeAsync();
        Task<List<AreaValidationResult>> GetAllAreaValidationResultAsync();
        Task<List<OfflineScanData>> GetOfflineScans();
        Task DeleteOfflineRecordAsync(int recordId);
        Task DeleteAllOfflineRecordsAsync();
        Task DeleteOfflineDatabaseAsync();
    }
}
