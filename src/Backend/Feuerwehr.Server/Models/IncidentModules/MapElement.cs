using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores a situation-map geometry, display attributes, symbol snapshot, and concurrency version.
    /// </summary>
    public class MapElement
    {
        /// <summary>
        /// Gets or sets the unique identifier of the map element.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the incident.
        /// </summary>
        public Guid IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the incident associated with this map element.
        /// </summary>
        [ForeignKey(nameof(IncidentId))]
        public Incident Incident { get; set; } = null!;

        /// <summary>
        /// Gets or sets the geometry kind of the map element.
        /// </summary>
        public MapElementType ElementType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the map symbol assigned to the element.
        /// </summary>
        [MaxLength(120)]
        public string? SymbolId { get; set; }

        /// <summary>
        /// Gets or sets the SVG markup captured for rendering the map element.
        /// </summary>
        public string? SymbolSvg { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the map symbol represents a vehicle.
        /// </summary>
        public bool IsVehicle { get; set; }

        /// <summary>
        /// Gets or sets the category label assigned to the map element.
        /// </summary>
        [MaxLength(120)]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the label displayed for the map element.
        /// </summary>
        [MaxLength(200)]
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the radio call name displayed on the situation map.
        /// </summary>
        [MaxLength(120)]
        public string? RadioCallName { get; set; }

        /// <summary>
        /// Gets or sets the personnel strength associated with the map element.
        /// </summary>
        [MaxLength(120)]
        public string? Strength { get; set; }

        /// <summary>
        /// Gets or sets the note recorded for the map element.
        /// </summary>
        [MaxLength(2000)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the display color encoded as a hexadecimal color string.
        /// </summary>
        [MaxLength(20)]
        public string ColorHex { get; set; } = "#d62828";

        /// <summary>
        /// Gets or sets the GeoJSON geometry of the map element.
        /// </summary>
        [Required]
        public string GeometryJson { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the latest change.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last changed the record.
        /// </summary>
        [MaxLength(450)]
        public string UpdatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last editor's display name captured with the record.
        /// </summary>
        [MaxLength(200)]
        public string UpdatedByDisplayName { get; set; } = string.Empty;
    }
}
