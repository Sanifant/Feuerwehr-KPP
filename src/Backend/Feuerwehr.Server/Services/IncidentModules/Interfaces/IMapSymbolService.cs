using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IMapSymbolService
    {
        Task<ServiceResult> GetSymbols(CancellationToken cancellationToken);

        Task<ServiceResult> GetCategories(CancellationToken cancellationToken);

        Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken);
    }
}
