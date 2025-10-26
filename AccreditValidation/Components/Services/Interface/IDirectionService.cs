namespace AccreditValidation.Components.Services.Interface
{
    using global::AccreditValidation.Models;

    public interface IDirectionService
    {
        Task<List<ValidationDirection>> GetValidationDirectionList(string selectedDirectionCode);
    }
}
