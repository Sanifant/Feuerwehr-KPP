using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Services;

/// <summary>
/// Executes inventory Service.
/// </summary>
/// <param name="http">The http.</param>
public sealed class InventoryService(AuthenticatedHttpClient http) : IInventoryService
{
    /// <summary>
    /// Executes find Barcode.
    /// </summary>
    /// <param name="barcode">The barcode.</param>
    /// <param name="ct">The ct.</param>
    public async Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct)
    {
        using var response = await http.GetAsync($"/api/inventory/barcode/{Uri.EscapeDataString(barcode)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Die GerÃ¤teantwort ist leer.");
    }

    /// <summary>
    /// Executes inspect.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct)
    {
        using var content = JsonContent.Create(request);
        using var response = await http.PostAsync($"/api/inventory/{id}/inspections", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Status bleibt fÃ¼r die BenutzerfÃ¼hrung auswertbar; Antwortinhalte werden nicht protokolliert.
            throw new System.Net.Http.HttpRequestException("Die PrÃ¼fung konnte nicht gespeichert werden.", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Die PrÃ¼fantwort ist leer.");
    }
}
