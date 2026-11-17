using de.openelp.feuerwehr.Common.Models;

namespace de.openelp.feuerwehr.Server.Models.Hydrant
{
    public class HydrantInspection
    {
        public Guid Id { get; set; }

        public Guid HydrantId { get; set; }

        public Hydrant Hydrant { get; set; } = null!;

        public DateOnly InspectedOn { get; set; }

        public DateTime InspectedAtUtc { get; set; }

        public string InspectorId { get; set; } = string.Empty;

        public string InspectorName { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public HydrantStatus Status { get; set; }
    }
}
