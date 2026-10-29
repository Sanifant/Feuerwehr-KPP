using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.App.Services;
using de.openelp.feuerwehr.Common.Models.Inventory;

namespace de.openelp.feuerwehr.App.ViewModels;

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
    public bool CanInspect => CanView && Detail?.Equipment.IsActive == true && auth.UserRoles.Any(x => x is "Admin" or "EquipmentOfficer");
    public bool CanScan => CanView && scanner.IsSupported;
    public string DeviceTitle => Detail is null ? "Gerät scannen" : $"{Detail.Equipment.Barcode} · {Detail.Equipment.EquipmentTypeName}";
    public string DeviceDescription => Detail is null ? "" : $"{Detail.Equipment.CategoryName}\n{Detail.Equipment.Manufacturer} · Seriennummer {Detail.Equipment.SerialNumber}\nID: {Detail.Equipment.Id}";
    public string DeviceStatus => Detail is null ? "" : $"{StatusOptions[(int)Detail.Equipment.Status]} · {(Detail.Equipment.IsActive ? "Aktiv" : "Deaktiviert")}";
    public string NextInspection => Detail is null ? "" : $"Nächste Prüfung: {Detail.Equipment.NextInspectionOn:dd.MM.yyyy} · Intervall: {Detail.Equipment.InspectionIntervalMonths} Monate";
    public string LastInspection => Detail is null ? "" : Detail.Equipment.LastInspectedOn is { } date ? $"Letzte Prüfung: {date:dd.MM.yyyy}" : "Noch nicht geprüft";
    public string Instructions => Detail?.Equipment.InspectionInstructions ?? "";

    public void Activate() => auth.AuthStateChanged += AuthChanged;
    public void Deactivate()
    {
        lookupVersion++;
        auth.AuthStateChanged -= AuthChanged;
        LookupCommand.Cancel(); ScanCommand.Cancel(); SaveInspectionCommand.Cancel();
    }
    private void AuthChanged(object? sender, EventArgs e)
    {
        lookupVersion++;
        LookupCommand.Cancel(); ScanCommand.Cancel(); SaveInspectionCommand.Cancel();
        Detail = null; History.Clear(); Comment = "";
        OnPropertyChanged(nameof(CanView)); OnPropertyChanged(nameof(CanScan));
        LookupCommand.NotifyCanExecuteChanged(); ScanCommand.NotifyCanExecuteChanged();
        Message = "Die Anmeldung wurde geändert. Bitte das Gerät erneut laden.";
    }
    partial void OnDetailChanged(EquipmentDetailDto? value)
    {
        foreach (var name in new[] { nameof(CanInspect), nameof(DeviceTitle), nameof(DeviceDescription), nameof(DeviceStatus), nameof(NextInspection), nameof(LastInspection), nameof(Instructions) }) OnPropertyChanged(name);
        SaveInspectionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanView), IncludeCancelCommand = true)]
    public async Task LookupAsync(CancellationToken ct)
    {
        if (!CanView) return;
        var version = ++lookupVersion;
        Detail = null; History.Clear(); Message = "";
        try
        {
            var result = await service.FindBarcodeAsync(Barcode.Trim().ToUpperInvariant(), ct);
            ct.ThrowIfCancellationRequested();
            if (!CanView || version != lookupVersion) return;
            SetDetail(result);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (version == lookupVersion) Message = ErrorMessage(ex); }
    }

    [RelayCommand(CanExecute = nameof(CanScan), IncludeCancelCommand = true)]
    public async Task ScanAsync(CancellationToken ct)
    {
        if (!CanScan) return;
        try
        {
            var value = await scanner.ScanAsync(ct);
            ct.ThrowIfCancellationRequested();
            if (!CanView || value is null) return;
            Barcode = value;
            await LookupAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception) { Message = "Kamera nicht verfügbar oder Zugriff verweigert. Bitte Barcode eingeben oder Scanner verwenden."; }
    }

    [RelayCommand(CanExecute = nameof(CanInspect), IncludeCancelCommand = true)]
    public async Task SaveInspectionAsync(CancellationToken ct)
    {
        var equipment = Detail?.Equipment;
        if (!CanInspect || equipment is null) return;
        if (InspectedOn is null || SelectedStatus is < 0 or > 2 || Comment.Length > 4000)
        { Message = "Bitte Prüfdatum, Status und einen Kommentar mit höchstens 4000 Zeichen angeben."; return; }
        var date = DateOnly.FromDateTime(InspectedOn.Value.Date);
        if (date > DateOnly.FromDateTime(DateTime.UtcNow) || date < (equipment.LastInspectedOn ?? equipment.CreatedOn))
        { Message = "Das Prüfdatum darf nicht in der Zukunft oder vor der Anlage bzw. letzten Prüfung liegen."; return; }
        try
        {
            var result = await service.InspectAsync(equipment.Id, new RecordEquipmentInspectionRequest
            {
                CommandId = commandId, InspectedOn = date, Comment = Comment, Status = (EquipmentStatus)SelectedStatus, ExpectedVersion = equipment.Version,
            }, ct);
            ct.ThrowIfCancellationRequested();
            if (!CanView || Detail?.Equipment.Id != equipment.Id) return;
            SetDetail(result); Message = "Prüfung dokumentiert.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { Message = ErrorMessage(ex); }
    }

    private void SetDetail(EquipmentDetailDto result)
    {
        Detail = result; History.Clear(); foreach (var entry in result.Inspections) History.Add(entry);
        Comment = ""; SelectedStatus = (int)result.Equipment.Status; InspectedOn = DateTimeOffset.UtcNow; commandId = Guid.NewGuid();
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
