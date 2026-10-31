using System.Data.Common;
using System.Globalization;
using MailKit.Security;
using MimeKit;

namespace De.Openelp.Feuerwehr.Server.Services;

public sealed class SmtpSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required SecureSocketOptions Security { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public required string From { get; init; }
    public required string FromName { get; init; }
    public required int TimeoutMilliseconds { get; init; }

    /// <summary>
    /// Executes from Configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public static SmtpSettings FromConfiguration(IConfiguration configuration)
    {
        static string? NonBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        var configuredHost = NonBlank(configuration["MAIL_HOST"]);
        // A fully configured provider must not depend on an old Mailpit connection string.
        var endpoint = configuredHost is null ? ReadEndpoint(configuration.GetConnectionString("mail")) : null;
        var host = configuredHost ?? endpoint?.Host ?? "localhost";
        var localDefaults = configuredHost is null && endpoint?.Scheme != "smtps";
        var configuredPort = NonBlank(configuration["MAIL_PORT"]);
        var configuredSecurity = NonBlank(configuration["MAIL_SECURITY"]);
        var security = configuredSecurity?.ToLowerInvariant() switch
        {
            null => endpoint?.Scheme == "smtps" || configuredPort == "465" ? SecureSocketOptions.SslOnConnect
                : localDefaults ? SecureSocketOptions.None : SecureSocketOptions.StartTls,
            "none" => SecureSocketOptions.None,
            "starttls" => SecureSocketOptions.StartTls,
            "sslonconnect" => SecureSocketOptions.SslOnConnect,
            _ => throw new InvalidOperationException("MAIL_SECURITY muss None, StartTls oder SslOnConnect sein."),
        };
        var defaultPort = endpoint is { Port: > 0 } ? endpoint.Port : security switch
        {
            SecureSocketOptions.SslOnConnect => 465,
            SecureSocketOptions.StartTls => 587,
            _ => 1025,
        };
        var port = ReadPositiveInteger(configuredPort, "MAIL_PORT", defaultPort, 65535);
        var username = NonBlank(configuration["MAIL_USERNAME"]);
        var password = configuration["MAIL_PASSWORD"];
        if ((username is null) != string.IsNullOrEmpty(password))
            throw new InvalidOperationException("MAIL_USERNAME und MAIL_PASSWORD müssen gemeinsam gesetzt werden.");
        if (username is not null && security == SecureSocketOptions.None)
            throw new InvalidOperationException("SMTP-Anmeldung erfordert MAIL_SECURITY=StartTls oder SslOnConnect.");

        var from = NonBlank(configuration["MAIL_FROM"]) ?? NonBlank(configuration["Mail:From"]) ?? username ?? "noreply@localhost.de";
        if (!MailboxAddress.TryParse(from, out var sender))
            throw new InvalidOperationException("MAIL_FROM muss eine gültige Absenderadresse enthalten.");

        return new SmtpSettings
        {
            Host = host, Port = port, Security = security, Username = username, Password = password,
            From = sender.Address, FromName = NonBlank(configuration["MAIL_FROM_NAME"]) ?? sender.Name ?? "",
            TimeoutMilliseconds = 1000 * ReadPositiveInteger(NonBlank(configuration["MAIL_TIMEOUT_SECONDS"]),
                "MAIL_TIMEOUT_SECONDS", 30, int.MaxValue / 1000),
        };
    }

    private static int ReadPositiveInteger(string? value, string name, int fallback, int maximum)
    {
        if (value is null) return fallback;
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0 && number <= maximum)
            return number;
        throw new InvalidOperationException($"{name} muss eine ganze Zahl zwischen 1 und {maximum} sein.");
    }

    private static Uri? ReadEndpoint(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection)) return null;
        try
        {
            if (!Uri.TryCreate(connection, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("smtp" or "smtps"))
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = connection };
                endpoint = new Uri((string)builder["endpoint"], UriKind.Absolute);
            }
            if (endpoint.Scheme is not ("smtp" or "smtps") || string.IsNullOrWhiteSpace(endpoint.Host))
                throw new FormatException();
            return endpoint;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or KeyNotFoundException)
        {
            // Do not include connection strings, which could contain credentials, in diagnostics.
            throw new InvalidOperationException("ConnectionStrings:mail enthält keinen gültigen SMTP-Endpunkt.");
        }
    }
}

