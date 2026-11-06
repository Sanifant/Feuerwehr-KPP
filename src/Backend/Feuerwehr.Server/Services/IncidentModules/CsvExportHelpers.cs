using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Formats audit records as CSV and protects exported cells from spreadsheet formula interpretation.
    /// </summary>
    public static class CsvExportHelpers
    {
        /// <summary>
        /// Prefixes potentially executable spreadsheet formulas so exported cells remain text.
        /// </summary>
        /// <param name="value">The string? value to process.</param>
        /// <returns>The escaped cell text safe for spreadsheet-compatible CSV export.</returns>
        public static string SanitizeCsvCell(string? value)
        {
            string normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            if (normalized.StartsWith("=") || normalized.StartsWith("+") || normalized.StartsWith("-") || normalized.StartsWith("@"))
            {
                normalized = $"'{normalized}";
            }

            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Builds the map Audit Csv.
        /// </summary>
        /// <param name="events">The ordered audit events to include in the CSV export.</param>
        /// <returns>The generated export file bytes.</returns>
        public static byte[] BuildMapAuditCsv(IEnumerable<MapAuditEvent> events)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine("Sequence;OccurredAtUtc;Action;User;ElementId;CommandId;Before;After");
            foreach (MapAuditEvent e in events)
            {
                _ = sb.AppendLine(string.Join(
                    ';',
                    e.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                    SanitizeCsvCell(e.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                    SanitizeCsvCell(e.Action),
                    SanitizeCsvCell(e.UserDisplayName),
                    SanitizeCsvCell(e.ElementId?.ToString()),
                    SanitizeCsvCell(e.CommandId),
                    SanitizeCsvCell(e.BeforeStateJson),
                    SanitizeCsvCell(e.AfterStateJson)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
