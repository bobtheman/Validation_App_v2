namespace AccreditValidation.Components.Services.Interface
{
    public interface IFileService
    {
        Task<string> GetImageBaseString(string fileName);
    }
}
