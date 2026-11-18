namespace de.openelp.feuerwehr.Server.Models.Hydrant
{
    using System.ComponentModel.DataAnnotations;
    using de.openelp.feuerwehr.Common.Models;

    public class HydrantInspection
    {
        public const int MaximumCommentLength = 500;

        public Guid Id { get; set; }

        public int HydrantId { get; set; }

        public Hydrant Hydrant { get; set; } = null!;

        public DateOnly InspectedOn { get; set; }

        public DateTime InspectedAtUtc { get; set; }

        public string InspectorId { get; set; } = string.Empty;

        public string InspectorName { get; set; } = string.Empty;

        [MaxLength(MaximumCommentLength)]
        public string? Comment { get; set; }

        public HydrantStatus PreviousStatus { get; set; }

        public HydrantStatus NewStatus { get; set; }

        public string? PhotoStorageKey { get; set; }

        public string? PhotoContentType { get; set; }
    }
}
