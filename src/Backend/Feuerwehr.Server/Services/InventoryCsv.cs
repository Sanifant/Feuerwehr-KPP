using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using Microsoft.VisualBasic.FileIO;

namespace De.Openelp.Feuerwehr.Server.Services;

internal sealed record InventoryCsvRow(long Line, int BarcodeNumber, Guid? TypeId, string TypeName, string Category,
    string SerialNumber, string Manufacturer, EquipmentStatus Status, bool IsActive, DateOnly? CreatedOn, DateOnly? LastInspectedOn);

internal static class InventoryCsv
{
    internal static readonly string[] Columns = ["id", "barcode", "equipmentTypeId", "equipmentType", "category", "serialNumber", "manufacturer", "status", "isActive", "createdOn", "lastInspectedOn", "nextInspectionOn"];
    private static readonly string[] RequiredColumns = ["barcode", "serialNumber", "manufacturer"];

    internal static string Quote(string? value)
    {
        value ??= "";
        // Escape formula prefixes and existing leading apostrophes reversibly for Excel round trips.
        if (NeedsPrefix(value)) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static bool NeedsPrefix(string value) => value.StartsWith('\'') ||
        value.Length > 0 && ("\t\r\n".Contains(value[0]) || value.TrimStart() is { Length: > 0 } trimmed && "=+-@".Contains(trimmed[0]));

    private static string Decode(string value) => value.StartsWith('\'') && NeedsPrefix(value[1..]) ? value[1..] : value;

    internal static (List<InventoryCsvRow> Rows, List<InventoryImportError> Errors) Read(Stream stream, CancellationToken ct)
    {
        List<InventoryCsvRow> rows = [];
        List<InventoryImportError> errors = [];
        using var parser = new TextFieldParser(stream, new UTF8Encoding(false, true), detectEncoding: true, leaveOpen: true)
        { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(";");
        try
        {
            var headers = parser.ReadFields();
            if (headers is null) return (rows, [new(null, "Die CSV-Datei ist leer.")]);
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim().TrimStart('\uFEFF');
                if (!columns.TryAdd(header, i)) errors.Add(new(1, $"Die Spalte '{header}' ist mehrfach vorhanden."));
                else if (!Columns.Contains(header, StringComparer.OrdinalIgnoreCase)) errors.Add(new(1, $"Unbekannte Spalte '{header}'."));
            }
            foreach (var column in RequiredColumns)
                if (!columns.ContainsKey(column)) errors.Add(new(1, $"Die Pflichtspalte '{column}' fehlt."));
            if (!columns.ContainsKey("equipmentTypeId") && !columns.ContainsKey("equipmentType"))
                errors.Add(new(1, "Die Spalte equipmentTypeId oder equipmentType muss vorhanden sein."));
            if (errors.Count > 0) return (rows, errors);
            var numbers = new HashSet<int>();
            var count = 0;
            while (!parser.EndOfData && errors.Count < 100)
            {
                ct.ThrowIfCancellationRequested();
                var line = parser.LineNumber;
                var fields = parser.ReadFields()!;
                if (++count > 999) { errors.Add(new(line, "Pro Datei sind höchstens 999 Geräte erlaubt.")); break; }
                if (fields.Length > headers.Length) { errors.Add(new(line, "Die Zeile enthält mehr Werte als Überschriften.")); continue; }
                string Field(string name) => columns.TryGetValue(name, out var index) && index < fields.Length ? Decode(fields[index]).Trim() : "";
                var barcode = Field("barcode").ToUpperInvariant();
                if (barcode.Length != 7 || !barcode.StartsWith("KPP-", StringComparison.Ordinal) ||
                    !int.TryParse(barcode.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number is < 1 or > 999)
                { errors.Add(new(line, "Der Barcode muss KPP-001 bis KPP-999 entsprechen.")); continue; }
                if (!numbers.Add(number)) { errors.Add(new(line, $"Der Barcode {barcode} ist in der Datei mehrfach vorhanden.")); continue; }
                var serial = Field("serialNumber"); var manufacturer = Field("manufacturer");
                if (serial.Length is 0 or > 150 || manufacturer.Length is 0 or > 150)
                { errors.Add(new(line, "Seriennummer und Hersteller sind erforderlich und dürfen jeweils höchstens 150 Zeichen enthalten.")); continue; }
                Guid? typeId = null;
                if (Field("equipmentTypeId") is { Length: > 0 } typeIdText)
                {
                    if (!Guid.TryParse(typeIdText, out var parsed) || parsed == Guid.Empty) { errors.Add(new(line, "equipmentTypeId muss eine gültige UUID sein.")); continue; }
                    typeId = parsed;
                }
                if (typeId is null && Field("equipmentType").Length == 0) { errors.Add(new(line, "Ein Gerätetyp muss angegeben werden.")); continue; }
                if (Field("equipmentType").Length > 150 || Field("category").Length > 100) { errors.Add(new(line, "Gerätetyp oder Kategorie ist zu lang.")); continue; }
                EquipmentStatus? status = Field("status").ToLowerInvariant() switch
                { "" or "ready" or "einsatzklar" => EquipmentStatus.Ready, "defective" or "defekt" => EquipmentStatus.Defective, "inrepair" or "in reparatur" => EquipmentStatus.InRepair, _ => null };
                if (status is null) { errors.Add(new(line, "status muss Einsatzklar, Defekt oder In Reparatur sein.")); continue; }
                bool? active = Field("isActive").ToLowerInvariant() switch { "" or "true" or "1" or "ja" => true, "false" or "0" or "nein" => false, _ => null };
                if (active is null) { errors.Add(new(line, "isActive muss true oder false sein.")); continue; }
                bool TryDate(string column, out DateOnly? value)
                {
                    var text = Field(column); value = null;
                    if (text.Length == 0) return true;
                    if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
                    value = parsed; return true;
                }
                if (!TryDate("createdOn", out var created) || !TryDate("lastInspectedOn", out var last))
                { errors.Add(new(line, "Datumswerte müssen das Format JJJJ-MM-TT haben.")); continue; }
                rows.Add(new(line, number, typeId, Field("equipmentType"), Field("category"), serial, manufacturer, status.Value, active.Value, created, last));
            }
            if (count == 0) errors.Add(new(null, "Die CSV-Datei enthält keine Geräte."));
        }
        catch (MalformedLineException) { errors.Add(new(parser.ErrorLineNumber, "Ungültige CSV-Zeile. Bitte Semikolon und Anführungszeichen prüfen.")); }
        catch (DecoderFallbackException) { errors.Add(new(null, "Die CSV-Datei muss als UTF-8 gespeichert sein.")); }
        return (rows, errors);
    }
}

