namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the inventory reminder operations required by application consumers.
    /// </summary>
    public interface IInventoryReminderService
    {
        /// <summary>
        /// Sends officers a digest of active equipment due within fourteen days, excluding reminders already delivered for the same due date.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task SendDueReminders(CancellationToken ct);
    }
}
