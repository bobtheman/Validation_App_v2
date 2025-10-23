namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Resources.Constants;
    using System.Reflection;
    public class VersionProvider : IVersionProvider
    {
        public Task<string> GetVersionAsync()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? ConstantsName.Version;
            return Task.FromResult(version);
        }

        public string Version => AppInfo.VersionString;
        public string Build => AppInfo.BuildString;
    }

}
