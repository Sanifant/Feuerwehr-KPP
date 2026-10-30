using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models.Vehicles;
using Microsoft.VisualBasic.FileIO;

namespace de.openelp.feuerwehr.Server.Services;

internal sealed record VehicleCsvRow(long Line, VehicleImportDto Data);

internal static class VehicleCsv
{
    internal static readonly string[] Columns = ["id", "radioCallSign", "name", "licensePlate", "fireDepartment", "fireDepartmentId", "vehicleType", "status", "equipmentOfficerEmail", "isActive"];
    internal static string Quote(string? value)
    {
        value ??= "";
        if (NeedsPrefix(value)) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static bool NeedsPrefix(string value) => value.StartsWith('\'') || value.Length > 0 &&
        ("\t\r\n".Contains(value[0]) || value.TrimStart() is { Length: > 0 } trimmed && "=+-@".Contains(trimmed[0]));
    private static string Decode(string value) => value.StartsWith('\'') && NeedsPrefix(value[1..]) ? value[1..] : value;

    internal static (List<VehicleCsvRow> Rows, List<VehicleImportError> Errors) Read(Stream stream, CancellationToken ct)
    {
        List<VehicleCsvRow> rows = [];
        List<VehicleImportError> errors = [];
        using var parser = new TextFieldParser(stream, new UTF8Encoding(false, true), true, true)
            { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(";");
        try
        {
            var headers = parser.ReadFields();
            if (headers is null) return (rows, [new(null, "Die CSV-Datei ist leer.")]);
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                var header = headers[index].Trim().TrimStart('\uFEFF');
                if (!columns.TryAdd(header, index)) errors.Add(new(1, $"Die Spalte '{header}' ist mehrfach vorhanden."));
                else if (!Columns.Contains(header, StringComparer.OrdinalIgnoreCase)) errors.Add(new(1, $"Unbekannte Spalte '{header}'."));
            }
            foreach (var required in new[] { "radioCallSign", "fireDepartment", "equipmentOfficerEmail" })
                if (!columns.ContainsKey(required)) errors.Add(new(1, $"Die Pflichtspalte '{required}' fehlt."));
            if (errors.Count > 0) return (rows, errors);
            var count = 0;
            while (!parser.EndOfData && errors.Count < 100)
            {
                ct.ThrowIfCancellationRequested();
                var line = parser.LineNumber;
                var fields = parser.ReadFields()!;
                if (++count > 10_000) { errors.Add(new(line, "Pro Import sind höchstens 10.000 Fahrzeuge erlaubt.")); break; }
                if (fields.Length > headers.Length) { errors.Add(new(line, "Die Zeile enthält mehr Werte als Überschriften.")); continue; }
                string? Field(string name) => columns.TryGetValue(name, out var index) ? index < fields.Length ? Decode(fields[index]).Trim() : "" : null;
                Guid? id = null;
                if (Field("id") is { Length: > 0 } idText)
                {
                    if (!Guid.TryParse(idText, out var parsed) || parsed == Guid.Empty) { errors.Add(new(line, "id muss eine gültige UUID sein.")); continue; }
                    id = parsed;
                }
                int? departmentId = null;
                if (Field("fireDepartmentId") is { Length: > 0 } departmentText)
                {
                    if (!int.TryParse(departmentText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1)
                    { errors.Add(new(line, "fireDepartmentId muss eine positive Zahl sein.")); continue; }
                    departmentId = parsed;
                }
                bool? active = null;
                if (Field("isActive") is { Length: > 0 } activeText)
                {
                    active = activeText.ToLowerInvariant() switch { "true" or "1" or "ja" => true, "false" or "0" or "nein" => false, _ => null };
                    if (active is null) { errors.Add(new(line, "isActive muss true oder false sein.")); continue; }
                }
                rows.Add(new(line, new VehicleImportDto
                {
                    Id = id, RadioCallSign = Field("radioCallSign") ?? "", FireDepartment = Field("fireDepartment") ?? "",
                    FireDepartmentId = departmentId, EquipmentOfficerEmail = Field("equipmentOfficerEmail") ?? "", Name = Field("name"),
                    LicensePlate = Field("licensePlate"), VehicleType = Field("vehicleType"), Status = Field("status"), IsActive = active
                }));
            }
            if (count == 0) errors.Add(new(null, "Die CSV-Datei enthält keine Fahrzeuge."));
        }
        catch (MalformedLineException) { errors.Add(new(parser.ErrorLineNumber, "Ungültige CSV-Zeile. Bitte Semikolon und Anführungszeichen prüfen.")); }
        catch (DecoderFallbackException) { errors.Add(new(null, "Die CSV-Datei muss als UTF-8 gespeichert sein.")); }
        return (rows, errors);
    }
}
