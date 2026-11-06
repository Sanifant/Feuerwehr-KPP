using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for upsert map element operations.
    /// </summary>
    public class UpsertMapElementRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client session that owns or requests the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the opaque token proving ownership of the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string LeaseToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the map element.
        /// </summary>
        public Guid? ElementId { get; set; }

        /// <summary>
        /// Gets or sets the geometry kind of the map element.
        /// </summary>
        [Required]
        public MapElementType ElementType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the map symbol assigned to the element.
        /// </summary>
        [MaxLength(120)]
        public string? SymbolId { get; set; }

        /// <summary>
        /// Gets or sets the category label assigned to the upsert map element.
        /// </summary>
        [MaxLength(120)]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the label displayed for the upsert map element.
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
        /// Gets or sets the note recorded for the upsert map element.
        /// </summary>
        [MaxLength(2000)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the display color encoded as a hexadecimal color string.
        /// </summary>
        [MaxLength(20)]
        public string? ColorHex { get; set; }

        /// <summary>
        /// Gets or sets the GeoJSON geometry of the map element.
        /// </summary>
        [Required]
        public string GeometryJson { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        public int? ExpectedVersion { get; set; }
    }
}
