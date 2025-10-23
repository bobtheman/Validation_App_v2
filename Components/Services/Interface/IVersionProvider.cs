namespace AccreditValidation.Components.Services.Interface
{
    public interface IVersionProvider
    {
        Task<string> GetVersionAsync();
        string Version { get; }
        string Build { get; }
    }
}
