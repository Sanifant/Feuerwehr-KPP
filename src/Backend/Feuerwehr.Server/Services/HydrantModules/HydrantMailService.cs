namespace de.openelp.feuerwehr.Server.Services.HydrantModules
{
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Hydrant;
    using de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces;
    using de.openelp.feuerwehr.Server.Services.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    [SettingsModule("hydrant", "Hydrant Management", ApiPaths = new[] { "/api/hydrants" }, PagePaths = new[] { "/hydrants" })]
    [SettingDefinition(ServiceAddress, "Enable Hydrant Mail Service", typeof(EmailSettingValidator), Mandatory = true)]
    public sealed class HydrantMailService(IMailQueue queue,
        ILogger<HydrantMailService> logger,
        ISettingsRuntime settings) : IHydrantMailService
    {
        private const string ServiceAddress = "HYDRANT_MAIL_SERVICE_ENABLED";

        public async Task QueueMailAsync(HydrantInspection entity, CancellationToken stoppingToken)
        {
            string subject = $"Hydrant Status Update: {entity.HydrantId}";
            string htmlBody = ComposeHtmlBody(entity);

            string? recipientMail = await settings.GetValueAsync(ServiceAddress, stoppingToken);

            if (string.IsNullOrWhiteSpace(recipientMail))
            {
                logger.LogInformation("Hydrant Mail Service is disabled. Skipping email queueing for Hydrant ID {HydrantId}.", entity.HydrantId);
                return;
            }

            queue.Enqueue(new MailMessage
            {
                Source = "hydrant",
                DeduplicationKey = entity.Id.ToString(),
                Recipient = recipientMail,
                Subject = subject,
                TextBody = $"Der Hydrant mit der ID {entity.HydrantId} hat den Status {entity.NewStatus}.",
                HtmlBody = htmlBody,
            }, entity.InspectorId);
        }

        private static string ComposeHtmlBody(HydrantInspection entity)
        {
            return $@"
                <html>
                    <body>
                        <h1>Hydrant Status Update</h1>
                        <p>Der Hydrant mit der ID {entity.HydrantId} hat den Status {entity.NewStatus}.</p>
                    </body>
                </html>";
        }
    }
}
