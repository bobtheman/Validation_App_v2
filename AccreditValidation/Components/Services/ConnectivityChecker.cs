namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using Microsoft.AspNetCore.Components;

    public class ConnectivityChecker : IConnectivityChecker
    {
        private readonly IAppState _appState;

        public ConnectivityChecker(IAppState appState)
        {
            _appState = appState;
        }

        public bool ConnectivityCheck(bool useOfflineMode)
        {
            if (useOfflineMode)
            {
                _appState.UseOfflineMode = true;
                return false;
            }

            if (_appState.UseOfflineMode)
            { 
                return false;
            }

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                 return true;
            }

            return false;
        }
    }
}
