using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Specifies the input for create vehicle reservation operations.
    /// </summary>
    public sealed class CreateVehicleReservationRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        /// Gets or sets the first date of the reservation range, inclusive.
        /// </summary>
        public DateOnly From { get; set; }

        /// <summary>
        /// Gets or sets the last date of the reservation range, inclusive.
        /// </summary>
        public DateOnly To { get; set; }

        /// <summary>
        /// Gets or sets the comment recorded with the create vehicle reservation.
        /// </summary>
        [StringLength(2000)] public string? Comment { get; set; }
    }
}
