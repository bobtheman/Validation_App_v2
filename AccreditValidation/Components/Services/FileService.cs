namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using System.Net.Http.Headers;
    using static global::AccreditValidation.Shared.Constants.ConstantsName;

    public class FileService : IFileService
    {
        private readonly IConnectivityChecker _connectivityChecker;

        public FileService(IConnectivityChecker connectivityChecker)
        {
            _connectivityChecker = connectivityChecker;
        }

        public async Task<string> GetImageBaseString(string photoId)
        {
            if(!_connectivityChecker.ConnectivityCheck())
            {
                return string.Empty;
            }

            var token = await SecureStorage.GetAsync("token");
            var serverUrl = await SecureStorage.GetAsync("serverUrl");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(photoId))
            {
                Console.WriteLine("Missing token, serverUrl, or photoId.");
                return string.Empty;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var imageUrl = $"{serverUrl}{Endpoints.File}{photoId}{Endpoints.FileThumbnail}";
                var imageBytes = await client.GetByteArrayAsync(imageUrl);
                var base64String = Convert.ToBase64String(imageBytes);
                return $"data:image/jpeg;base64,{base64String}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching or converting image: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
