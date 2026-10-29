using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Inventory;

namespace de.openelp.feuerwehr.App.Services;

public interface IInventoryService
{
    Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct);
    Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
}

public sealed class InventoryService(AuthenticatedHttpClient http) : IInventoryService
{
    public async Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct)
    {
        using var response = await http.GetAsync($"/api/inventory/barcode/{Uri.EscapeDataString(barcode)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Die Geräteantwort ist leer.");
    }

    public async Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct)
    {
        using var content = JsonContent.Create(request);
        using var response = await http.PostAsync($"/api/inventory/{id}/inspections", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Status bleibt für die Benutzerführung auswertbar; Antwortinhalte werden nicht protokolliert.
            throw new System.Net.Http.HttpRequestException("Die Prüfung konnte nicht gespeichert werden.", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Die Prüfantwort ist leer.");
    }
}
