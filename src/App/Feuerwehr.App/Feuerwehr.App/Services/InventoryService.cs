using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Implements equipment operations for the Avalonia client.
    /// </summary>
    /// <param name="http">The HTTP client used to call the backend API.</param>
    public sealed class InventoryService(AuthenticatedHttpClient http) : IInventoryService
    {
        /// <summary>
        /// Looks up equipment by its unique barcode and loads its inspection history.
        /// </summary>
        /// <param name="barcode">The unique equipment barcode used for lookup and import matching.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is the equipment's current details and inspection history.</returns>
        public async Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct)
        {
            using HttpResponseMessage response = await http.GetAsync($"/api/inventory/barcode/{Uri.EscapeDataString(barcode)}", ct);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Die Geräteantwort ist leer.");
        }

        /// <summary>
        /// Records an equipment inspection with concurrency checking and a snapshot of its inspection plan.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The record equipment inspection input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is the equipment's current details and inspection history.</returns>
        public async Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct)
        {
            using JsonContent content = JsonContent.Create(request);
            using HttpResponseMessage response = await http.PostAsync($"/api/inventory/{id}/inspections", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Status bleibt für die Benutzerführung auswertbar; Antwortinhalte werden nicht protokolliert.
                throw new System.Net.Http.HttpRequestException("Die Prüfung konnte nicht gespeichert werden.", null, response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Die Prüfantwort ist leer.");
        }
    }
}
