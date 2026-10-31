using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

/// <summary>
/// Executes inventory Category Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
public sealed record InventoryCategoryDto(Guid Id, string Name);
