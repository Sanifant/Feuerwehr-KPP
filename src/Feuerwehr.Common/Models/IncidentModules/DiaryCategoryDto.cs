using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public record DiaryCategoryDto(Guid Id, string Code, string Name, bool IsActive);
