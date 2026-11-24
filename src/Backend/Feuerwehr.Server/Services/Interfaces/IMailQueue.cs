namespace de.openelp.feuerwehr.Server.Services
{
    /// <summary>Stages outgoing emails in the caller's database unit of work.</summary>
    public interface IMailQueue
    {
        /// <summary>Adds a message; the caller commits it together with the business changes.</summary>
        /// <param name="message">The module's prepared email.</param>
        /// <param name="createdBy">The creating user, or null for system-generated messages.</param>
        void Enqueue(Models.MailMessage message, string? createdBy);
    }
}
