using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.ViewModels
{
    /// <summary>
    /// Executes inventory View Model.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="auth">The auth.</param>
    /// <param name="scanner">The scanner.</param>
    public partial class InventoryViewModel(IInventoryService service, IAuthService auth, IBarcodeScanner scanner) : ViewModelBase
    {
        private Guid commandId = Guid.NewGuid();
        private int lookupVersion;
        [ObservableProperty] private string barcode = "";
        [ObservableProperty] private EquipmentDetailDto? detail;
        [ObservableProperty] private string message = "";
        [ObservableProperty] private string comment = "";
        [ObservableProperty] private DateTimeOffset? inspectedOn = DateTimeOffset.UtcNow;
        [ObservableProperty] private int selectedStatus;

        public ObservableCollection<EquipmentInspectionDto> History { get; } = [];

        public string[] StatusOptions { get; } = ["Einsatzklar", "Defekt", "In Reparatur"];

        public bool CanView => auth.IsAuthenticated && auth.UserRoles.Any(x => x is "Admin" or "EquipmentOfficer" or "InventoryViewer");

        public bool CanInspect => this.CanView && this.Detail?.Equipment.IsActive == true && auth.UserRoles.Any(x => x is "Admin" or "EquipmentOfficer");

        public bool CanScan => this.CanView && scanner.IsSupported;

        public string DeviceTitle => this.Detail is null ? "Gerät scannen" : $"{this.Detail.Equipment.Barcode} Â· {this.Detail.Equipment.EquipmentTypeName}";

        public string DeviceDescription => this.Detail is null ? "" : $"{this.Detail.Equipment.CategoryName}\n{this.Detail.Equipment.Manufacturer} Â· Seriennummer {this.Detail.Equipment.SerialNumber}\nID: {this.Detail.Equipment.Id}";

        public string DeviceStatus => this.Detail is null ? "" : $"{this.StatusOptions[(int)this.Detail.Equipment.Status]} Â· {(this.Detail.Equipment.IsActive ? "Aktiv" : "Deaktiviert")}";

        public string NextInspection => this.Detail is null ? "" : $"Nächste Prüfung: {this.Detail.Equipment.NextInspectionOn:dd.MM.yyyy} Â· Intervall: {this.Detail.Equipment.InspectionIntervalMonths} Monate";

        public string LastInspection => this.Detail is null ? "" : this.Detail.Equipment.LastInspectedOn is { } date ? $"Letzte Prüfung: {date:dd.MM.yyyy}" : "Noch nicht geprüft";

        public string Instructions => this.Detail?.Equipment.InspectionInstructions ?? "";

        /// <summary>
        /// Executes activate.
        /// </summary>
        public void Activate() => auth.AuthStateChanged += this.AuthChanged;

        /// <summary>
        /// Executes deactivate.
        /// </summary>
        public void Deactivate()
        {
            this.lookupVersion++;
            auth.AuthStateChanged -= this.AuthChanged;
            this.LookupCommand.Cancel();
            this.ScanCommand.Cancel();
            this.SaveInspectionCommand.Cancel();
        }

        [RelayCommand(CanExecute = nameof(CanView), IncludeCancelCommand = true)]
        /// <summary>
        /// Executes lookup.
        /// </summary>
        /// <param name="ct">The ct.</param>
        public async Task LookupAsync(CancellationToken ct)
        {
            if (!this.CanView)
            {
                return;
            }

            int version = ++this.lookupVersion;
            this.Detail = null;
            this.History.Clear();
            this.Message = "";
            try
            {
                EquipmentDetailDto result = await service.FindBarcodeAsync(this.Barcode.Trim().ToUpperInvariant(), ct);
                ct.ThrowIfCancellationRequested();
                if (!this.CanView || version != this.lookupVersion)
                {
                    return;
                }

                this.SetDetail(result);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) { if (version == this.lookupVersion)
                {
                    this.Message = ErrorMessage(ex);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanScan), IncludeCancelCommand = true)]
        /// <summary>
        /// Executes scan.
        /// </summary>
        /// <param name="ct">The ct.</param>
        public async Task ScanAsync(CancellationToken ct)
        {
            if (!this.CanScan)
            {
                return;
            }

            try
            {
                string? value = await scanner.ScanAsync(ct);
                ct.ThrowIfCancellationRequested();
                if (!this.CanView || value is null)
                {
                    return;
                }

                this.Barcode = value;
                await this.LookupAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception) { this.Message = "Kamera nicht verfügbar oder Zugriff verweigert. Bitte Barcode eingeben oder Scanner verwenden."; }
        }

        [RelayCommand(CanExecute = nameof(CanInspect), IncludeCancelCommand = true)]
        /// <summary>
        /// Saves the inspection.
        /// </summary>
        /// <param name="ct">The ct.</param>
        public async Task SaveInspectionAsync(CancellationToken ct)
        {
            EquipmentDto? equipment = this.Detail?.Equipment;
            if (!this.CanInspect || equipment is null)
            {
                return;
            }

            if (this.InspectedOn is null || this.SelectedStatus is < 0 or > 2 || this.Comment.Length > 4000)
            { this.Message = "Bitte Prüfdatum, Status und einen Kommentar mit höchstens 4000 Zeichen angeben.";
                return; }
            DateOnly date = DateOnly.FromDateTime(this.InspectedOn.Value.Date);
            if (date > DateOnly.FromDateTime(DateTime.UtcNow) || date < (equipment.LastInspectedOn ?? equipment.CreatedOn))
            { this.Message = "Das Prüfdatum darf nicht in der Zukunft oder vor der Anlage bzw. letzten Prüfung liegen.";
                return; }
            try
            {
                EquipmentDetailDto result = await service.InspectAsync(equipment.Id, new RecordEquipmentInspectionRequest
                {
                    CommandId = this.commandId, InspectedOn = date, Comment = this.Comment, Status = (EquipmentStatus)this.SelectedStatus, ExpectedVersion = equipment.Version,
                }, ct);
                ct.ThrowIfCancellationRequested();
                if (!this.CanView || this.Detail?.Equipment.Id != equipment.Id)
                {
                    return;
                }

                this.SetDetail(result);
                this.Message = "Prüfung dokumentiert.";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) { this.Message = ErrorMessage(ex); }
        }

        private void AuthChanged(object? sender, EventArgs e)
        {
            this.lookupVersion++;
            this.LookupCommand.Cancel();
            this.ScanCommand.Cancel();
            this.SaveInspectionCommand.Cancel();
            this.Detail = null;
            this.History.Clear();
            this.Comment = "";
            this.OnPropertyChanged(nameof(this.CanView));
            this.OnPropertyChanged(nameof(this.CanScan));
            this.LookupCommand.NotifyCanExecuteChanged();
            this.ScanCommand.NotifyCanExecuteChanged();
            this.Message = "Die Anmeldung wurde geändert. Bitte das Gerät erneut laden.";
        }

        partial void OnDetailChanged(EquipmentDetailDto? value)
        {
            foreach (string name in new[] { nameof(CanInspect), nameof(DeviceTitle), nameof(DeviceDescription), nameof(DeviceStatus), nameof(NextInspection), nameof(LastInspection), nameof(Instructions) }) OnPropertyChanged(name);
            SaveInspectionCommand.NotifyCanExecuteChanged();
        }

        private void SetDetail(EquipmentDetailDto result)
        {
            this.Detail = result;
            this.History.Clear();
            foreach (EquipmentInspectionDto entry in result.Inspections)
            {
                this.History.Add(entry);
            }

            this.Comment = "";
            this.SelectedStatus = (int)result.Equipment.Status;
            this.InspectedOn = DateTimeOffset.UtcNow;
            this.commandId = Guid.NewGuid();
        }

        private static string ErrorMessage(Exception exception) => exception switch
        {
            HttpRequestException { StatusCode: HttpStatusCode.NotFound } => "Kein Gerät zu diesem Barcode gefunden.",
            HttpRequestException { StatusCode: HttpStatusCode.Conflict } => "Das Gerät wurde inzwischen geändert. Bitte neu laden und Angaben prüfen.",
            HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized } => "Keine Berechtigung. Bitte Anmeldung prüfen.",
            HttpRequestException { StatusCode: HttpStatusCode.BadRequest } => "Barcode oder Prüfungsangaben sind ungültig.",
            _ => "Die Anfrage konnte nicht abgeschlossen werden. Bitte Verbindung prüfen und erneut versuchen.",
        };
    }
}
