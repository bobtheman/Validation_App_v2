namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Requests.V3;
    using global::AccreditValidation.Responses;
    using global::AccreditValidation.Shared.Constants;
    using SQLite;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class OfflineDataService : IOfflineDataService
    {
        private const string DbName = "OfflineDatabase.db3";
        private readonly string dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
        private SQLiteAsyncConnection db;
        private readonly IRestDataService _restDataService;
        private readonly ILocalizationService _localizationService;

        public OfflineDataService(IRestDataService restDataService, ILocalizationService localizationService)
        {
            _restDataService = restDataService;
            _localizationService = localizationService;
            dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
            db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            if (db == null)
            {
                Debug.WriteLine("[InitializeAsync] SQLite connection is null. Aborting initialization.");
                return;
            }

            try
            {
                Debug.WriteLine("[InitializeAsync] Starting initialization...");

                // Check if DB file exists
                bool dbExists = File.Exists(dbPath);

                if (dbExists)
                {
                    Debug.WriteLine("[InitializeAsync] Database exists. Dropping existing tables...");

                    // Drop all tables
                    await db.ExecuteAsync("DROP TABLE IF EXISTS Badge");
                    await db.ExecuteAsync("DROP TABLE IF EXISTS Area");
                    await db.ExecuteAsync("DROP TABLE IF EXISTS AreaValidationResult");
                    await db.ExecuteAsync("DROP TABLE IF EXISTS OfflineScanData");

                    Debug.WriteLine("[InitializeAsync] All tables dropped.");
                }

                // Create tables (fresh)
                await SafeCreateTableAsync<Badge>();
                await SafeCreateTableAsync<Area>();
                await SafeCreateTableAsync<AreaValidationResult>();
                await SafeCreateTableAsync<OfflineScanData>();

                Debug.WriteLine("[InitializeAsync] Tables recreated successfully.");
            }
            catch (SQLite.SQLiteException sqliteEx)
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

        public SQLiteAsyncConnection GetConnection()
        {
            throw new NotImplementedException();
        }

        public Task EnsureDatabaseCreatedAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SetOfflineAreaAsync()
        {
            try
            {
                var result = await _restDataService.GetAreaAsync();

                if (!result.Any())
                {
                    Debug.WriteLine("No areas found in the response.");
                    return;
                }

                foreach (var area in result)
                {
                    await db.InsertAsync(area);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Code:" + ex.Message);
            }
        }

        public async Task SetLocalValidationResultAsync(ValidationResultsResponse results)
        {
            try
            {
                if (results?.Data == null)
                {
                    return;
                }

                for (int i = 0; i < results.TotalRecords; i++)
                {
                    var validationData = results.Data[i];

                    if (validationData?.Badge != null)
                    {
                        if (validationData.Badge.Photo != null)
                        {
                            validationData.Badge.PhotoId = validationData.Badge.Photo.Id;
                            validationData.Badge.PhotoFileName = validationData.Badge.Photo.PhotoFileName;
                            validationData.Badge.PhotoUrl = validationData.Badge.Photo.PhotoUrl;
                        }

                        await db.InsertAsync(validationData.Badge);
                    }

                    if (validationData?.AreaValidationResults != null)
                    {
                        for (int j = 0; j < validationData.AreaValidationResults.Count(); j++)
                        {
                            var areaValidation = validationData.AreaValidationResults[j];
                            await db.InsertAsync(areaValidation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public async Task<BadgeValidationResponse> ValidateRequestOffline(BadgeValidationRequest badgeValidationRequest)
        {
            var badgeValidationResponse = new BadgeValidationResponse();

            try
            {
                if (badgeValidationRequest == null || db == null)
                {
                    badgeValidationResponse.ValidationResult = Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);
                    badgeValidationResponse.Result = await _localizationService.SetLocalizedValidationResultName(ConstantsName.BadgeNotFound);
                    return badgeValidationResponse;
                }

                if (string.IsNullOrWhiteSpace(badgeValidationRequest.Barcode))
                {
                    badgeValidationResponse.ValidationResult = Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);
                    badgeValidationResponse.Result = await _localizationService.SetLocalizedValidationResultName(ConstantsName.BadgeNotFound);
                    return badgeValidationResponse;
                }

                if (string.IsNullOrWhiteSpace(badgeValidationRequest.AreaId))
                {
                    badgeValidationResponse.ValidationResult = Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);
                    badgeValidationResponse.Result = await _localizationService.SetLocalizedValidationResultName(ConstantsName.BadgeNotFound);
                    return badgeValidationResponse;
                }

                var badges = await GetAllBadgeAsync();
                var badge = badges?.FirstOrDefault(b => b.Barcode == badgeValidationRequest.Barcode);

                if (badge == null)
                {
                    badgeValidationResponse.ValidationResult = Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound);
                    badgeValidationResponse.Result = await _localizationService.SetLocalizedValidationResultName(ConstantsName.BadgeNotFound);
                    return badgeValidationResponse;
                }

                var areaValidationResults = await GetAllAreaValidationResultAsync();
                var successfulAreaValidationResult = areaValidationResults?
                    .FirstOrDefault(avr => avr.AreaId == badgeValidationRequest.AreaId && avr.Barcode == badgeValidationRequest.Barcode);

                badgeValidationResponse.ValidationResult = (int)(successfulAreaValidationResult != null
                ? (long)successfulAreaValidationResult.ValidationResult
                    : Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound));
                badgeValidationResponse.Result = successfulAreaValidationResult?.ValidationResultName ?? await _localizationService.SetLocalizedValidationResultName(ConstantsName.BadgeNotFound);

                await UpdateOfflineScanDataAsync(badgeValidationRequest, (int)(successfulAreaValidationResult?.ValidationResult ?? Convert.ToInt32(Enums.BadgeValidationResult.BadgeNotFound)));

                badgeValidationResponse.Badge = badge;

                return badgeValidationResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return badgeValidationResponse;
            }
        }

        public async Task DeleteOfflineDatabaseAsync()
        {
            try
            {
                if (db != null)
                {
                    await db.CloseAsync();
                    db = null;
                }
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    Debug.WriteLine("[DeleteOfflineDatabase] Database file deleted successfully.");
                }
                else
                {
                    Debug.WriteLine("[DeleteOfflineDatabase] Database file does not exist.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteOfflineDatabase] Error deleting database: {ex.Message}");
            }
        }

        #region table actions
        public async Task<List<Area>> GetAllAreasAsync()
        {
            var test = db.Table<Area>().ToListAsync();
            return await db.Table<Area>().ToListAsync();
        }

        public async Task<List<Badge>> GetAllBadgeAsync()
        {
            return await db.Table<Badge>().ToListAsync();
        }

        public async Task<List<AreaValidationResult>> GetAllAreaValidationResultAsync()
        {
            return await db.Table<AreaValidationResult>().ToListAsync();
        }

        public async Task<List<OfflineScanData>> GetOfflineScans()
        {
            return await db.Table<OfflineScanData>().ToListAsync();
        }

        public async Task UpdateOfflineScanDataAsync(BadgeValidationRequest badgeValidationRequest, int validationResult)
        {
            try
            {
                if (badgeValidationRequest == null)
                {
                    throw new ArgumentNullException(nameof(badgeValidationRequest));
                }

                var area = await GetAreaByIdentifierAsync(badgeValidationRequest.AreaId);

                var offlineScanData = new OfflineScanData
                {
                    Barcode = badgeValidationRequest.Barcode,
                    AreaId = badgeValidationRequest.AreaId,
                    AreaName = area.Name,
                    DateTime = badgeValidationRequest.Timestamp,
                    ScannedDateTime = badgeValidationRequest.Timestamp,
                    Mode = badgeValidationRequest.Mode,
                    Direction = badgeValidationRequest.Direction,
                    ValidationResult = validationResult
                };

                await db.InsertAsync(offlineScanData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            var result = await db.ExecuteScalarAsync<int>(
                $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'");
            return result > 0;
        }

        public async Task DeleteOfflineRecordAsync(int recordId)
        {
            try
            {
                var record = await db.Table<OfflineScanData>().Where(x => x.Id == recordId).FirstOrDefaultAsync();
                if (record != null)
                {
                    await db.DeleteAsync(record);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteOfflineRecordAsync] Error deleting record with Id {recordId}: {ex.Message}");
            }
        }

        public async Task DeleteAllOfflineRecordsAsync()
        {
            try
            {
                await db.DeleteAllAsync<OfflineScanData>();
                Debug.WriteLine("[DeleteAllOfflineRecordsAsync] All offline records deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteAllOfflineRecordsAsync] Error deleting all offline records: {ex.Message}");
            }
        }
        #endregion


        #region private methods
        private async Task SafeCreateTableAsync<T>() where T : new()
        {
            try
            {
                if (db == null)
                {
                    Debug.WriteLine($"[SafeCreateTableAsync] Database connection is null for type: {typeof(T).Name}");
                    return;
                }

                // Simple check to ensure DB is open and working
                await db.ExecuteScalarAsync<int>("SELECT 1");

                await db.CreateTableAsync<T>();
                Debug.WriteLine($"[SafeCreateTableAsync] Table created for type: {typeof(T).Name}");
            }
            catch (SQLite.SQLiteException sqliteEx)
            {
                Debug.WriteLine($"[SafeCreateTableAsync] SQLite error creating table {typeof(T).Name}: {sqliteEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SafeCreateTableAsync] Unexpected error creating table {typeof(T).Name}: {ex.Message}");
            }
        }

        private async Task<Area?> GetAreaByIdentifierAsync(string? areaIdentifier)
        {
            if (string.IsNullOrWhiteSpace(areaIdentifier))
            {
                return null;
            }
              
            return await db.Table<Area>()
                .Where(a => a.Identifier == areaIdentifier)
                .FirstOrDefaultAsync();
        }
        #endregion

    }
}
