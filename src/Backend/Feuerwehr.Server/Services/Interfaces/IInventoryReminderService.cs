namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IInventoryReminderService
    {
        Task SendDueReminders(CancellationToken ct);
    }
}
