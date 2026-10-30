using De.Openelp.Feuerwehr.Server.Services;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public class SmtpSettingsTests
{
    /// <summary>
    /// Executes read.
    /// </summary>
    public static SmtpSettings Read(Dictionary<string, string?> values) => SmtpSettings.FromConfiguration(
        new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    [Theory]
    [InlineData("smtp://mailpit:32781")]
    [InlineData("endpoint=smtp://mailpit:32781")]
    /// <summary>
    /// Executes settings Use Aspire Mailpit Endpoint.
    /// </summary>
    /// <param name="connectionString">The connection String.</param>
    public void Settings_UseAspireMailpitEndpoint(string connectionString)
    {
        var settings = Read(new() { ["ConnectionStrings:mail"] = connectionString });
        Assert.Equal("mailpit", settings.Host);
        Assert.Equal(32781, settings.Port);
        Assert.Equal(SecureSocketOptions.None, settings.Security);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
    }

    [Fact]
    /// <summary>
    /// Executes settings Read All Provider Environment Keys And Override Old Aspire Configuration.
    /// </summary>
    public void Settings_ReadAllProviderEnvironmentKeysAndOverrideOldAspireConfiguration()
    {
        var settings = Read(new()
        {
            ["ConnectionStrings:mail"] = "invalid obsolete connection",
            ["MAIL_HOST"] = "smtp.strato.de", ["MAIL_PORT"] = "465", ["MAIL_SECURITY"] = "SslOnConnect",
            ["MAIL_USERNAME"] = "mail@example.org", ["MAIL_PASSWORD"] = " password with spaces ",
            ["MAIL_FROM"] = "sender@example.org", ["MAIL_FROM_NAME"] = "Feuerwehr Ãœbung", ["MAIL_TIMEOUT_SECONDS"] = "45",
            ["Mail:From"] = "old@example.org",
        });
        Assert.Equal("smtp.strato.de", settings.Host);
        Assert.Equal(465, settings.Port);
        Assert.Equal(SecureSocketOptions.SslOnConnect, settings.Security);
        Assert.Equal("mail@example.org", settings.Username);
        Assert.Equal(" password with spaces ", settings.Password);
        Assert.Equal("sender@example.org", settings.From);
        Assert.Equal("Feuerwehr Ãœbung", settings.FromName);
        Assert.Equal(45000, settings.TimeoutMilliseconds);
    }

    [Theory]
    [InlineData("465", null, SecureSocketOptions.SslOnConnect, 465)]
    [InlineData("587", null, SecureSocketOptions.StartTls, 587)]
    [InlineData(null, null, SecureSocketOptions.StartTls, 587)]
    [InlineData(null, "sslonconnect", SecureSocketOptions.SslOnConnect, 465)]
    [InlineData(null, "STARTTLS", SecureSocketOptions.StartTls, 587)]
    /// <summary>
    /// Executes provider Defaults Require Encryption.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="security">The security.</param>
    /// <param name="expected">The expected.</param>
    /// <param name="expectedPort">The expected Port.</param>
    public void ProviderDefaults_RequireEncryption(string? port, string? security, SecureSocketOptions expected, int expectedPort)
    {
        var settings = Read(new() { ["MAIL_HOST"] = "smtp.example.org", ["MAIL_PORT"] = port, ["MAIL_SECURITY"] = security });
        Assert.Equal(expected, settings.Security);
        Assert.Equal(expectedPort, settings.Port);
    }

    [Fact]
    /// <summary>
    /// Executes local Defaults And Legacy Sender Remain Supported.
    /// </summary>
    public void LocalDefaultsAndLegacySender_RemainSupported()
    {
        var settings = Read(new() { ["Mail:From"] = "legacy@example.org" });
        Assert.Equal("localhost", settings.Host);
        Assert.Equal(1025, settings.Port);
        Assert.Equal(SecureSocketOptions.None, settings.Security);
        Assert.Equal("legacy@example.org", settings.From);
        Assert.Equal(30000, settings.TimeoutMilliseconds);
    }

    [Fact]
    /// <summary>
    /// Executes empty Provider Values Allow App Host To Force Mailpit When Provider Credentials Are Inherited.
    /// </summary>
    public void EmptyProviderValues_AllowAppHostToForceMailpitWhenProviderCredentialsAreInherited()
    {
        var settings = Read(new()
        {
            ["ConnectionStrings:mail"] = "smtp://mailpit:32781", ["MAIL_HOST"] = string.Empty, ["MAIL_PORT"] = string.Empty,
            ["MAIL_USERNAME"] = string.Empty, ["MAIL_PASSWORD"] = string.Empty, ["MAIL_SECURITY"] = "None", ["MAIL_FROM"] = "noreply@localhost.de",
        });
        Assert.Equal("mailpit", settings.Host);
        Assert.Equal(32781, settings.Port);
        Assert.Equal(SecureSocketOptions.None, settings.Security);
        Assert.Null(settings.Username);
    }

    [Theory]
    [InlineData("MAIL_PORT", "abc")]
    [InlineData("MAIL_PORT", "0")]
    [InlineData("MAIL_PORT", "65536")]
    [InlineData("MAIL_TIMEOUT_SECONDS", "0")]
    [InlineData("MAIL_TIMEOUT_SECONDS", "2147483647")]
    [InlineData("MAIL_SECURITY", "Auto")]
    [InlineData("MAIL_SECURITY", "StartTlsWhenAvailable")]
    [InlineData("MAIL_SECURITY", "3")]
    [InlineData("MAIL_FROM", "invalid address")]
    /// <summary>
    /// Executes invalid Settings Are Rejected.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void InvalidSettings_AreRejected(string key, string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Read(new() { [key] = value }));
        Assert.Contains(key, exception.Message);
    }

    [Theory]
    [InlineData("user@example.org", null, "StartTls")]
    [InlineData(null, "secret-value", "StartTls")]
    [InlineData("user@example.org", "secret-value", "None")]
    /// <summary>
    /// Executes incomplete Or Unencrypted Credentials Are Rejected Without Leaking Password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="security">The security.</param>
    public void IncompleteOrUnencryptedCredentials_AreRejectedWithoutLeakingPassword(string? username, string? password, string security)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Read(new()
        {
            ["MAIL_USERNAME"] = username, ["MAIL_PASSWORD"] = password, ["MAIL_SECURITY"] = security,
        }));
        Assert.DoesNotContain("secret-value", exception.ToString());
    }

    [Fact]
    /// <summary>
    /// Executes invalid Connection String Is Rejected Without Leaking Credentials.
    /// </summary>
    public void InvalidConnectionString_IsRejectedWithoutLeakingCredentials()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Read(new() { ["ConnectionStrings:mail"] = "endpoint=invalid;password=secret-value" }));
        Assert.DoesNotContain("secret-value", exception.ToString());
    }
}

