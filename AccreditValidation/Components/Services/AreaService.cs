namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;

    public class AreaService : IAreaService
    {
        private readonly IConnectivityChecker _connectivityChecker;
        private readonly IOfflineDataService _offlineDataService;
        private readonly IRestDataService _restDataService;

        public AreaService(
            IConnectivityChecker connectivityChecker,
            IOfflineDataService offlineDataService,
            IRestDataService restDataService)
        {
            _connectivityChecker = connectivityChecker;
            _offlineDataService = offlineDataService;
            _restDataService = restDataService;
        }

        public async Task<List<Area>> GetAreaList(string selectedAreaCode)
        {
            var areaList = new List<Area>();

            try
            {
                areaList = _connectivityChecker.ConnectivityCheck()
                    ? await _restDataService.GetAreaAsync()
                    : await _offlineDataService.GetAllAreasAsync();
            }
            catch (Exception)
            {
                return areaList;
            }

            if (areaList.Count == 0)
                return areaList;

            if (areaList.Count == 1)
            {
                areaList[0].IsSelected = true;
            }
            else
            {
                SetSelectedArea(selectedAreaCode, areaList);
            }

            return areaList;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void SetSelectedArea(string selectedAreaCode, List<Area> areaList)
        {
            if (string.IsNullOrEmpty(selectedAreaCode))
            {
                areaList[0].IsSelected = true;
                return;
            }

            foreach (var area in areaList)
                area.IsSelected = area.Identifier == selectedAreaCode;
        }
    }
}
