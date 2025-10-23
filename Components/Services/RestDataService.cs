namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Requests;
    using global::AccreditValidation.Responses;
    using RestSharp;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;

    public class RestDataService : IRestDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IFileService _fileService;
        private HttpResponseMessage responseMessage = new HttpResponseMessage();

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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await SecureStorage.GetAsync("token"));

                responseMessage = await _httpClient.GetAsync($"{await SecureStorage.GetAsync("serverUrl")}/api/v2/badge/Areas");

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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await SecureStorage.GetAsync("token"));

                responseMessage = await _httpClient.GetAsync($"{await SecureStorage.GetAsync("serverUrl")}/api/v2/badge/validationResults");

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

                                    if (downloadAllPhotos)
                                    {
                                        badgeData.Photo = await _fileService.GetImageBaseString(badgeData.PhotoUrl);
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

            BadgeValidationResponse badgeValidationResponse = new BadgeValidationResponse();

            if (string.IsNullOrEmpty(await SecureStorage.GetAsync("serverUrl")))
            {
                return badgeValidationResponse;
            }

            try
            {
                var clientOptions = new RestClientOptions(await SecureStorage.GetAsync("serverUrl"))
                {
                    MaxTimeout = -1,
                };
                var client = new RestClient(clientOptions);
                var request = new RestRequest("/api/v2/badge/Validation", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {await SecureStorage.GetAsync("token")}");

                request.AddStringBody(JsonSerializer.Serialize(validationRequest), DataFormat.Json);

                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    string jsonString = response.Content;
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

        public async Task<BadgeValidationResponse> SyncValidationResults(IEnumerable<BadgeValidationRequest> request)
        {
            var badgeValidationResponse = new BadgeValidationResponse();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await SecureStorage.GetAsync("token"));

                var jsonContent = JsonSerializer.Serialize(request);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                responseMessage = await _httpClient.PostAsync($"{await SecureStorage.GetAsync("serverUrl")}/api/v2/badge/validationResults", content);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonString = await responseMessage.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    badgeValidationResponse = JsonSerializer.Deserialize<BadgeValidationResponse>(jsonString, options);
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

            return badgeValidationResponse;
        }
    }
}