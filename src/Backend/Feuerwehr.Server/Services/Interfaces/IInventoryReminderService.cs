namespace de.openelp.feuerwehr.Server.Services
{
    public interface IInventoryReminderService
    {
        Task SendDueReminders(CancellationToken ct);
    }
}
