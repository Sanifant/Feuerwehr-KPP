namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Defines the supported training status values.
    /// </summary>
    public enum TrainingStatus
    {
        /// <summary>
        /// The planned option for training status.
        /// </summary>
        Planned = 1,
        /// <summary>
        /// The open option for training status.
        /// </summary>
        Open = 2,
        /// <summary>
        /// The fully booked option for training status.
        /// </summary>
        FullyBooked = 3,
        /// <summary>
        /// The completed option for training status.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// The reservation was canceled and no longer claims calendar days.
        /// </summary>
        Cancelled = 5
    }
}
