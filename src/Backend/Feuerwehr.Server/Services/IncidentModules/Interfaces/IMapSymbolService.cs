using System.Xml;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

public interface IMapSymbolService
{
    Task<ServiceResult> GetSymbols(CancellationToken cancellationToken);
    Task<ServiceResult> GetCategories(CancellationToken cancellationToken);
    Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken);
}
