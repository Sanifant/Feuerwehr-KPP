using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;
using Microsoft.VisualBasic.FileIO;

namespace De.Openelp.Feuerwehr.Server.Services
{
    internal static class HydrantCsvImport
    {
        private static readonly string[] RequiredColumns =
            ["street", "houseNumber", "postalCode", "city", "latitude", "longitude", "nominalDiameter", "type", "waterSource", "status"];

        internal static (List<Hydrant> Hydrants, List<HydrantImportError> Errors) Read(Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return ReadContent(stream, cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                return ([], [new(null, "Die Datei enthält ungültige Zeichen. Bitte als UTF-8-CSV speichern.")]);
            }
        }

        private static (List<Hydrant> Hydrants, List<HydrantImportError> Errors) ReadContent(Stream stream, CancellationToken cancellationToken)
        {
            List<Hydrant> hydrants = [];
            List<HydrantImportError> errors = [];
            using TextFieldParser parser = new TextFieldParser(stream, new UTF8Encoding(false, true), detectEncoding: true, leaveOpen: true)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true,
            };
            parser.SetDelimiters(";");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[]? headers = parser.ReadFields();
                if (headers is null)
                    return (hydrants, [new(null, "Die CSV-Datei ist leer.")]);

                Dictionary<string, int> columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < headers.Length; index++)
                {
                    if (!columns.TryAdd(headers[index], index))
                        errors.Add(new(1, $"Die Spalte '{headers[index]}' ist mehrfach vorhanden."));
                    else if (!RequiredColumns.Contains(headers[index], StringComparer.OrdinalIgnoreCase)
                        && !headers[index].Equals("notes", StringComparison.OrdinalIgnoreCase))
                        errors.Add(new(1, $"Unbekannte Spalte '{headers[index]}'."));
                }
                foreach (string required in RequiredColumns)
                    if (!columns.ContainsKey(required)) errors.Add(new(1, $"Die Pflichtspalte '{required}' fehlt."));

                if (errors.Count > 0) return (hydrants, errors);

                int rowCount = 0;
                while (!parser.EndOfData && errors.Count < 100)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    long line = parser.LineNumber;
                    string[] fields = parser.ReadFields()!;
                    if (++rowCount > 10_000)
                    {
                        errors.Add(new(line, "Pro Import sind höchstens 10.000 Hydranten erlaubt."));
                        break;
                    }
                    if (fields.Length > headers.Length)
                    {
                        errors.Add(new(line, "Die Zeile enthält mehr Werte als Spaltenüberschriften."));
                        continue;
                    }

                    string Field(string name) => columns.TryGetValue(name, out int index) && index < fields.Length ? fields[index] : "";
                    string[] missing = RequiredColumns.Where(name => string.IsNullOrWhiteSpace(Field(name))).ToArray();
                    if (missing.Length > 0)
                    {
                        errors.Add(new(line, $"Pflichtfelder fehlen: {string.Join(", ", missing)}."));
                        continue;
                    }

                    // Parse the stored types, but deliberately do not validate coordinate ranges or positions.
                    if (!double.TryParse(Field("latitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)
                        || !double.IsFinite(latitude)
                        || !double.TryParse(Field("longitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)
                        || !double.IsFinite(longitude)
                        || !int.TryParse(Field("nominalDiameter"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int diameter)
                        || !int.TryParse(Field("type"), out int type) || !Enum.IsDefined((HydrantType)type)
                        || !int.TryParse(Field("waterSource"), out int source) || !Enum.IsDefined((WaterSource)source)
                        || !int.TryParse(Field("status"), out int status) || !Enum.IsDefined((HydrantStatus)status))
                    {
                        errors.Add(new(line, "Koordinaten müssen Zahlen mit Dezimalpunkt, Nennweite eine ganze Zahl und Typ, Wasserquelle und Status Werte von 1 bis 4 sein."));
                        continue;
                    }

                    hydrants.Add(new Hydrant
                    {
                        Address = new Address
                        {
                            Street = Field("street"), HouseNumber = Field("houseNumber"),
                            PostalCode = Field("postalCode"), City = Field("city"),
                        },
                        Latitude = latitude, Longitude = longitude, NominalDiameter = diameter,
                        Type = (HydrantType)type, WaterSource = (WaterSource)source, Status = (HydrantStatus)status,
                        Notes = string.IsNullOrWhiteSpace(Field("notes")) ? null : Field("notes"),
                    });
                }
                if (rowCount == 0) errors.Add(new(null, "Die CSV-Datei enthält keine Hydranten."));
            }
            catch (MalformedLineException)
            {
                errors.Add(new(parser.ErrorLineNumber, "Ungültige CSV-Zeile. Bitte Trennzeichen und Anführungszeichen prüfen."));
            }
            return (hydrants, errors);
        }
    }

}
