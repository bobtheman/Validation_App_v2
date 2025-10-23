using AccreditValidation.Models;

namespace AccreditValidation.Components.Services.Interface
{
    public interface IAreaService
    {
        Task<List<Area>> GetAreaList(string selectedAreaCode);
    }
}
