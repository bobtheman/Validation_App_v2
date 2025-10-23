namespace AccreditValidation.Components.Services.Interface
{
    public interface IConnectivityChecker
    {
        bool ConnectivityCheck(bool useOfflineMode = false);
    }
}
