namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;

    public class AreaService : IAreaService
    {
        public IConnectivityChecker _connectivityChecker;
        public IOfflineDataService _offlineDataService;
        public IRestDataService _restDataService;

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
                if (!_connectivityChecker.ConnectivityCheck())
                {
                    areaList = await _offlineDataService.GetAllAreasAsync();
                }
                else
                {
                    areaList = await _restDataService.GetAreaAsync();
                }
            }
            catch (Exception ex)
            {
                return areaList;
            }

            if (areaList.Count == 0)
            {
                return areaList;
            }

            if (areaList.Count == 1)
            {
                areaList[0].IsSelected = true;
                selectedAreaCode = areaList[0].Identifier;
            }
            else
            {
                SetSelectedAreaCode(selectedAreaCode, areaList);
            }

            return areaList;
        }

        private void SetSelectedAreaCode(string selectedAreaCode, List<Area> areaList)
        {
            if (string.IsNullOrEmpty(selectedAreaCode))
            {
                areaList[0].IsSelected = true;
                selectedAreaCode = areaList[0].Identifier;
            }
            else 
            {
                foreach (var area in areaList)
                {
                    area.IsSelected = area.Identifier == selectedAreaCode;
                }
            }
        }
    }
}
