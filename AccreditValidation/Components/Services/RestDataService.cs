namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Requests.V2;
    using global::AccreditValidation.Responses;
    using RestSharp;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using static global::AccreditValidation.Shared.Constants.ConstantsName;

    public class RestDataService : IRestDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IFileService _fileService;

        // Shared response message — only accessed in the context of each method's own await chain.
        private HttpResponseMessage _responseMessage = new HttpResponseMessage();

        public RestDataService(IFileService fileService)
        {
            _fileService = fileService;

            // Single HttpClient instance shared across all calls (correct pattern for HttpClient lifetime).
            _httpClient = new HttpClient();
        }

        // ── Areas ─────────────────────────────────────────────────────────────

        public async Task<List<Area>> GetAreaAsync()
        {
            var areaList = new List<Area>();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                _responseMessage = await _httpClient.GetAsync(
                    $"{await SecureStorage.GetAsync(SecureStorageServerUrl)}{Endpoints.Areas}");

                if (!_responseMessage.IsSuccessStatusCode)
                    return areaList;

                var jsonString = await _responseMessage.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AreasResponse>(jsonString, options);

                return result?.Areas?.ToList() ?? areaList;
            }
            catch (Exception)
            {
                Debug.WriteLine($"[GetAreaAsync] {(int)_responseMessage.StatusCode} - {_responseMessage.ReasonPhrase}");
                return areaList;
            }
        }

        // ── Offline download ──────────────────────────────────────────────────

        public async Task<ValidationResultsResponse> DownloadOfflineData(bool downloadAllPhotos)
        {
            var validationResultsResponse = new ValidationResultsResponse { Data = [] };

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                _responseMessage = await _httpClient.GetAsync(
                    $"{await SecureStorage.GetAsync(SecureStorageServerUrl)}{Endpoints.ValidationResults}");

                if (!_responseMessage.IsSuccessStatusCode)
                    return validationResultsResponse;

                var jsonString = await _responseMessage.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonString);

                validationResultsResponse.TotalPages = jsonObject.TryGetProperty("TotalPages", out var totalPages) ? totalPages.GetInt64() : 0;
                validationResultsResponse.TotalRecords = jsonObject.TryGetProperty("TotalRecords", out var totalRecords) ? totalRecords.GetInt64() : 0;
                validationResultsResponse.PageSize = jsonObject.TryGetProperty("PageSize", out var pageSize) ? pageSize.GetInt64() : 0;
                validationResultsResponse.PageNumber = jsonObject.TryGetProperty("PageNumber", out var pageNumber) ? pageNumber.GetInt64() : 0;

                if (!jsonObject.TryGetProperty("Data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
                    return validationResultsResponse;

                var validationDataList = new List<ValidationData>();

                // Photo downloads run in parallel when requested to avoid serial HTTP bottleneck
                var photoTasks = new List<Task>();

                foreach (var item in dataArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("Badge", out var badgeJson))
                        continue;

                    Badge badgeData;
                    try
                    {
                        badgeData = JsonSerializer.Deserialize<Badge>(badgeJson.GetRawText(), options);
                        if (badgeData == null)
                        {
                            Debug.WriteLine("[DownloadOfflineData] Badge deserialized as null — skipping.");
                            continue;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.WriteLine($"[DownloadOfflineData] Badge JSON error: {jsonEx.Message}");
                        continue;
                    }

                    var areaValidationResults = new List<AreaValidationResult>();

                    if (item.TryGetProperty("AreaValidationResults", out var areaValidationJson))
                    {
                        areaValidationResults = JsonSerializer.Deserialize<List<AreaValidationResult>>(
                            areaValidationJson.GetRawText(), options) ?? new List<AreaValidationResult>();
                    }

                    // Stamp the barcode onto each area validation row
                    foreach (var avr in areaValidationResults)
                        avr.Barcode = badgeData.Barcode;

                    var entry = new ValidationData
                    {
                        Badge = badgeData,
                        AreaValidationResults = areaValidationResults
                    };

                    validationDataList.Add(entry);

                    // Queue photo downloads concurrently rather than awaiting each in turn
                    if (downloadAllPhotos)
                    {
                        photoTasks.Add(Task.Run(async () =>
                        {
                            badgeData.Photo = await _fileService.GetImageBaseString(badgeData.PhotoUrl);
                            badgeData.PhotoDownloaded = true;
                        }));
                    }
                }

                if (photoTasks.Count > 0)
                    await Task.WhenAll(photoTasks);

                validationResultsResponse.Data = validationDataList.ToArray();

                return validationResultsResponse;
            }
            catch (Exception)
            {
                Debug.WriteLine($"[DownloadOfflineData] {(int)_responseMessage.StatusCode} - {_responseMessage.ReasonPhrase}");
                return validationResultsResponse;
            }
        }

        // ── Validate ──────────────────────────────────────────────────────────

        public async Task<BadgeValidationResponse> ValidateRequest(BadgeValidationRequest validationRequest)
        {
            var badgeValidationResponse = new BadgeValidationResponse();

            var serverUrl = await SecureStorage.GetAsync(SecureStorageServerUrl);

            if (string.IsNullOrEmpty(serverUrl))
                return badgeValidationResponse;

            try
            {
                var clientOptions = new RestClientOptions(serverUrl);
                var client = new RestClient(clientOptions);
                var request = new RestRequest(Endpoints.Validation, Method.Post);

                request.AddHeader(Headers.ContentType, MimeTypes.ApplicationJson);
                request.AddHeader(Headers.Authorization, $"{Headers.Bearer} {await SecureStorage.GetAsync(SecureStorageToken)}");
                request.AddStringBody(JsonSerializer.Serialize(validationRequest), DataFormat.Json);

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    return badgeValidationResponse;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<BadgeValidationResponse>(response.Content, options)
                    ?? badgeValidationResponse;
            }
            catch (Exception)
            {
                Debug.WriteLine($"[ValidateRequest] {(int)_responseMessage.StatusCode} - {_responseMessage.ReasonPhrase}");
                return badgeValidationResponse;
            }
        }

        // ── Sync ──────────────────────────────────────────────────────────────

        public async Task<BadgeValidationResponse> SyncValidationResults(IEnumerable<BadgeValidationRequest> request)
        {
            var badgeValidationResponse = new BadgeValidationResponse();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, MimeTypes.ApplicationJson);

                _responseMessage = await _httpClient.PostAsync(
                    $"{await SecureStorage.GetAsync(SecureStorageServerUrl)}{Endpoints.ValidationResults}",
                    content);

                if (!_responseMessage.IsSuccessStatusCode)
                    return badgeValidationResponse;

                var jsonString = await _responseMessage.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                badgeValidationResponse = JsonSerializer.Deserialize<BadgeValidationResponse>(jsonString, options)
                    ?? badgeValidationResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncValidationResults] Exception: {ex.Message}");
                Debug.WriteLine($"[SyncValidationResults] HTTP: {(int)_responseMessage.StatusCode} - {_responseMessage.ReasonPhrase}");
            }

            return badgeValidationResponse;
        }
    }
}
