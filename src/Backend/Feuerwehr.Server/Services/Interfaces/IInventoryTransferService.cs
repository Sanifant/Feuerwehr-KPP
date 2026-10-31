using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services;

public interface IInventoryTransferService
{
    Task<ServiceResult> Export(InventoryTransferKind kind, CancellationToken ct);
    Task<ServiceResult> Import(InventoryTransferKind kind, Stream stream, CancellationToken ct);
}
