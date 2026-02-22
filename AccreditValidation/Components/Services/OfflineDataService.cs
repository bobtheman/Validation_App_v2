namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Requests.V2;
    using global::AccreditValidation.Responses;
    using global::AccreditValidation.Shared.Constants;
    using SQLite;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class OfflineDataService : IOfflineDataService
    {
        private const string DbName = "OfflineDatabase.db3";

        private readonly string _dbPath;
        private readonly IRestDataService _restDataService;

        private SQLiteAsyncConnection _db;

        public OfflineDataService(IRestDataService restDataService)
        {
            _restDataService = restDataService;
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
            _db = new SQLiteAsyncConnection(_dbPath);
        }

        // ── Initialisation ────────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            if (_db == null)
            {
                Debug.WriteLine("[InitializeAsync] SQLite connection is null. Aborting initialization.");
                return;
            }

            try
            {
                Debug.WriteLine("[InitializeAsync] Starting initialization...");

                if (File.Exists(_dbPath))
                {
                    Debug.WriteLine("[InitializeAsync] Database exists. Dropping existing tables...");
                    await _db.ExecuteAsync("DROP TABLE IF EXISTS Badge");
                    await _db.ExecuteAsync("DROP TABLE IF EXISTS Area");
                    await _db.ExecuteAsync("DROP TABLE IF EXISTS AreaValidationResult");
                    await _db.ExecuteAsync("DROP TABLE IF EXISTS OfflineScanData");
                    Debug.WriteLine("[InitializeAsync] All tables dropped.");
                }

                // Re-create all tables fresh
                await SafeCreateTableAsync<Badge>();
                await SafeCreateTableAsync<Area>();
                await SafeCreateTableAsync<AreaValidationResult>();
                await SafeCreateTableAsync<OfflineScanData>();

                Debug.WriteLine("[InitializeAsync] Tables recreated successfully.");
            }
            catch (SQLiteException sqliteEx)
            {
                Debug.WriteLine("[InitializeAsync] SQLite error during initialization: " + sqliteEx.Message);
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine("[InitializeAsync] I/O error during initialization: " + ioEx.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[InitializeAsync] Unexpected error during initialization: " + ex);
            }
        }

        public SQLiteAsyncConnection GetConnection() => throw new NotImplementedException();

        public Task EnsureDatabaseCreatedAsync() => throw new NotImplementedException();

        // ── Sync from REST ────────────────────────────────────────────────────

        public async Task SetOfflineAreaAsync()
        {
            try
            {
                var result = await _restDataService.GetAreaAsync();

                if (result == null || !result.Any())
                {
                    Debug.WriteLine("No areas found in the response.");
                    return;
                }

                // Bulk insert is significantly faster than one-by-one InsertAsync
                await _db.InsertAllAsync(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Code: " + ex.Message);
            }
        }

        public async Task SetLocalValidationResultAsync(ValidationResultsResponse results)
        {
            try
            {
                if (results?.Data == null || results.Data.Length == 0)
                    return;

                var badges = new List<Badge>(results.Data.Length);
                var areaValidations = new List<AreaValidationResult>();

                // Bug fix: use Data.Length — TotalRecords comes from the API metadata
                // and may differ from the actual number of items in the Data array.
                foreach (var validationData in results.Data)
                {
                    if (validationData?.Badge != null)
                        badges.Add(validationData.Badge);

                    if (validationData?.AreaValidationResults != null)
                        areaValidations.AddRange(validationData.AreaValidationResults);
                }

                // Bulk insert both collections in a single pass each
                if (badges.Count > 0)
                    await _db.InsertAllAsync(badges);

                if (areaValidations.Count > 0)
                    await _db.InsertAllAsync(areaValidations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SetLocalValidationResultAsync] Error: " + ex.Message);
            }
        }

        // ── Offline validation ────────────────────────────────────────────────

        public async Task<BadgeValidationResponse> ValidateRequestOffline(BadgeValidationRequest badgeValidationRequest)
        {
            var badgeValidationResponse = new BadgeValidationResponse();

            try
            {
                if (badgeValidationRequest == null || _db == null)
                    return NotFound(badgeValidationResponse);

                if (string.IsNullOrWhiteSpace(badgeValidationRequest.Barcode))
                    return NotFound(badgeValidationResponse);

                if (string.IsNullOrWhiteSpace(badgeValidationRequest.AreaIdentifier))
                    return NotFound(badgeValidationResponse);

                var badges = await GetAllBadgeAsync();
                var badge = badges?.FirstOrDefault(b => b.Barcode == badgeValidationRequest.Barcode);

                if (badge == null)
                    return NotFound(badgeValidationResponse);

                var areaValidationResults = await GetAllAreaValidationResultAsync();
                var matchedResult = areaValidationResults?.FirstOrDefault(
                    avr => avr.AreaIdentifier == badgeValidationRequest.AreaIdentifier
                        && avr.Barcode == badgeValidationRequest.Barcode);

                badgeValidationResponse.ValidationResult = matchedResult != null
                    ? (long)matchedResult.ValidationResult
                    : Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);

                badgeValidationResponse.ValidationResultName = matchedResult?.ValidationResultName
                    ?? ConstantsName.BadgeNotFound;

                await UpdateOfflineScanDataAsync(
                    badgeValidationRequest,
                    (int)(matchedResult?.ValidationResult ?? Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound)));

                badgeValidationResponse.Badge = badge;

                return badgeValidationResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ValidateRequestOffline] Error: " + ex.Message);
                return badgeValidationResponse;
            }
        }

        // ── Database management ───────────────────────────────────────────────

        public async Task DeleteOfflineDatabaseAsync()
        {
            try
            {
                if (_db != null)
                {
                    await _db.CloseAsync();
                    _db = null;
                }

                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                    Debug.WriteLine("[DeleteOfflineDatabase] Database file deleted successfully.");
                }
                else
                {
                    Debug.WriteLine("[DeleteOfflineDatabase] Database file does not exist.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteOfflineDatabase] Error: {ex.Message}");
            }
        }

        // ── Table queries ─────────────────────────────────────────────────────

        public Task<List<Area>> GetAllAreasAsync()
            => _db.Table<Area>().ToListAsync();

        public Task<List<Badge>> GetAllBadgeAsync()
            => _db.Table<Badge>().ToListAsync();

        public Task<List<AreaValidationResult>> GetAllAreaValidationResultAsync()
            => _db.Table<AreaValidationResult>().ToListAsync();

        public Task<List<OfflineScanData>> GetOfflineScans()
            => _db.Table<OfflineScanData>().ToListAsync();

        public async Task UpdateOfflineScanDataAsync(BadgeValidationRequest badgeValidationRequest, int validationResult)
        {
            try
            {
                if (badgeValidationRequest == null)
                    throw new ArgumentNullException(nameof(badgeValidationRequest));

                var area = await GetAreaByIdentifierAsync(badgeValidationRequest.AreaIdentifier);

                var offlineScanData = new OfflineScanData
                {
                    Barcode = badgeValidationRequest.Barcode,
                    AreaId = badgeValidationRequest.AreaIdentifier,
                    AreaName = area?.Name,
                    DateTime = badgeValidationRequest.DateTime,
                    ScannedDateTime = badgeValidationRequest.DateTime,
                    Mode = badgeValidationRequest.Mode,
                    Direction = badgeValidationRequest.Direction,
                    ValidationResult = validationResult
                };

                await _db.InsertAsync(offlineScanData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[UpdateOfflineScanDataAsync] Error: " + ex.Message);
            }
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            var result = await _db.ExecuteScalarAsync<int>(
                $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'");
            return result > 0;
        }

        public async Task DeleteOfflineRecordAsync(int recordId)
        {
            try
            {
                var record = await _db.Table<OfflineScanData>()
                    .Where(x => x.Id == recordId)
                    .FirstOrDefaultAsync();

                if (record != null)
                    await _db.DeleteAsync(record);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteOfflineRecordAsync] Error deleting record {recordId}: {ex.Message}");
            }
        }

        public async Task DeleteAllOfflineRecordsAsync()
        {
            try
            {
                await _db.DeleteAllAsync<OfflineScanData>();
                Debug.WriteLine("[DeleteAllOfflineRecordsAsync] All offline records deleted.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteAllOfflineRecordsAsync] Error: {ex.Message}");
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static BadgeValidationResponse NotFound(BadgeValidationResponse response)
        {
            response.ValidationResult = Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);
            response.ValidationResultName = ConstantsName.BadgeNotFound;
            return response;
        }

        private async Task SafeCreateTableAsync<T>() where T : new()
        {
            try
            {
                if (_db == null)
                {
                    Debug.WriteLine($"[SafeCreateTableAsync] DB connection null for {typeof(T).Name}");
                    return;
                }

                await _db.ExecuteScalarAsync<int>("SELECT 1");
                await _db.CreateTableAsync<T>();
                Debug.WriteLine($"[SafeCreateTableAsync] Table created: {typeof(T).Name}");
            }
            catch (SQLiteException sqliteEx)
            {
                Debug.WriteLine($"[SafeCreateTableAsync] SQLite error for {typeof(T).Name}: {sqliteEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SafeCreateTableAsync] Error for {typeof(T).Name}: {ex.Message}");
            }
        }

        private Task<Area?> GetAreaByIdentifierAsync(string? areaIdentifier)
        {
            if (string.IsNullOrWhiteSpace(areaIdentifier))
                return Task.FromResult<Area?>(null);

            return _db.Table<Area>()
                .Where(a => a.Identifier == areaIdentifier)
                .FirstOrDefaultAsync()!;
        }
    }
}
