namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Requests.V3;
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
        private HttpResponseMessage responseMessage = new HttpResponseMessage();
        BadgeValidationResponse badgeValidationResponse = new BadgeValidationResponse();
        SyncValidationResponse syncValidationResponse = new SyncValidationResponse();

        public RestDataService(IFileService fileService)
        {
            _httpClient = new HttpClient();
            _fileService = fileService;
        }

        public async Task<List<Area>> GetAreaAsync()
        {
            List<Area> areaList = new List<Area>();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                responseMessage = await _httpClient.GetAsync($"{await SecureStorage.GetAsync(ServerUrl)}{Endpoints.Areas}");

                if (responseMessage.IsSuccessStatusCode)
                {
                    string jsonString = await responseMessage.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<AreasResponse>(jsonString, options);

                    return result.Areas.ToList();
                }

                return areaList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Code" + responseMessage.StatusCode + " : Message - " + responseMessage.ReasonPhrase);
                return areaList;
            }
        }

        public async Task<ValidationResultsResponse> DownloadOfflineData(bool downloadAllPhotos)
        {
            ValidationResultsResponse validationResultsResponse = new ValidationResultsResponse();
            validationResultsResponse.Data = [];
            List<Badge> badges = new List<Badge>();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                responseMessage = await _httpClient.GetAsync($"{await SecureStorage.GetAsync(ServerUrl)}{Endpoints.Validation}?page=1&pageSize=500");

                if (responseMessage.IsSuccessStatusCode)
                {
                    string jsonString = await responseMessage.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    validationResultsResponse.TotalPages = jsonObject.TryGetProperty("TotalPages", out var totalPages) ? totalPages.GetInt64() : 0;
                    validationResultsResponse.TotalRecords = jsonObject.TryGetProperty("TotalRecords", out var totalRecords) ? totalRecords.GetInt64() : 0;
                    validationResultsResponse.PageSize = jsonObject.TryGetProperty("PageSize", out var pageSize) ? pageSize.GetInt64() : 0;
                    validationResultsResponse.PageNumber = jsonObject.TryGetProperty("PageNumber", out var pageNumber) ? pageNumber.GetInt64() : 0;

                    // Parse Data array
                    if (jsonObject.TryGetProperty("Data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        var validationDataList = new List<ValidationData>();

                        foreach (var item in dataArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("Badge", out var badgeJson))
                            {
                                try
                                {
                                    var badgeData = JsonSerializer.Deserialize<Badge>(badgeJson.GetRawText(), options);

                                    if (badgeData == null)
                                    {
                                        Debug.WriteLine("Deserialization returned null for Badge.");
                                        continue;
                                    }

                                    if (downloadAllPhotos && !string.IsNullOrEmpty(badgeData.Photo?.Id))
                                    {
                                        badgeData.Photo.PhotoUrl = await _fileService.GetImageBaseString(badgeData.Photo.Id);
                                        badgeData.PhotoDownloaded = true;
                                    }

                                    var areaValidationResults = new List<AreaValidationResult>();

                                    if (item.TryGetProperty("AreaValidationResults", out var areaValidationJson))
                                    {
                                        areaValidationResults = JsonSerializer.Deserialize<List<AreaValidationResult>>(areaValidationJson.GetRawText(), options) ?? new List<AreaValidationResult>();
                                    }

                                    List<AreaValidationResult> successfulAreaValidationResults = new List<AreaValidationResult>();

                                    if (areaValidationResults != null)
                                    {
                                        foreach (var areaValidation in areaValidationResults)
                                        {
                                            areaValidation.Barcode = badgeData.Barcode;
                                            successfulAreaValidationResults.Add(areaValidation);
                                        }

                                        validationDataList.Add(new ValidationData
                                        {
                                            Badge = badgeData,
                                            AreaValidationResults = successfulAreaValidationResults
                                        });
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    Debug.WriteLine($"JSON deserialization error for Badge: {jsonEx.Message}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Unexpected error during Badge deserialization: {ex.Message}");
                                }
                            }
                        }

                        validationResultsResponse.Data = validationDataList.ToArray();
                    }

                    return validationResultsResponse;
                }

                return validationResultsResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Code" + responseMessage.StatusCode + " : Message - " + responseMessage.ReasonPhrase);
                return validationResultsResponse;
            }
        }

        public async Task<BadgeValidationResponse> ValidateRequest(BadgeValidationRequest validationRequest)
        {
            if (string.IsNullOrEmpty(await SecureStorage.GetAsync(ServerUrl)))
            {
                return badgeValidationResponse;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                var jsonContent = JsonSerializer.Serialize(validationRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, MimeTypes.ApplicationJson);

                responseMessage = await _httpClient.PostAsync($"{await SecureStorage.GetAsync(ServerUrl)}{Endpoints.Validation}", content);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string jsonString = await responseMessage.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<BadgeValidationResponse>(jsonString, options);
                }

                return badgeValidationResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Code" + responseMessage.StatusCode + " : Message - " + responseMessage.ReasonPhrase);
                return badgeValidationResponse;
            }
        }

        public async Task<SyncValidationResponse> SyncValidationResults(IEnumerable<BadgeValidationRequest> validationRequest)
        {
           
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.Bearer, await SecureStorage.GetAsync(SecureStorageToken));

                var jsonContent = JsonSerializer.Serialize(validationRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, MimeTypes.ApplicationJson);

                responseMessage = await _httpClient.PostAsync($"{await SecureStorage.GetAsync(ServerUrl)}{Endpoints.Validation}", content);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonString = await responseMessage.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    syncValidationResponse = JsonSerializer.Deserialize<SyncValidationResponse>(jsonString, options) ?? new SyncValidationResponse();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during sync: " + ex.Message);
                if (responseMessage != null)
                {
                    Debug.WriteLine($"HTTP response: {(int)responseMessage.StatusCode} - {responseMessage.ReasonPhrase}");
                }
            }

            return syncValidationResponse;
        }
    }
}