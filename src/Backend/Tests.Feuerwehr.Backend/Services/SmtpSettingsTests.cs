using De.Openelp.Feuerwehr.Server.Services;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests smtp settings behavior and its expected outcomes.
    /// </summary>
    public class SmtpSettingsTests
    {
        /// <summary>
        /// Parses smtp settings rows from the import stream and reports format or field validation errors.
        /// </summary>
        /// <param name="values">The configuration key-value pairs supplied for the SMTP resolution scenario.</param>
        /// <returns>The resolved SMTP host, port, sender, and transport settings.</returns>
        public static SmtpSettings Read(Dictionary<string, string?> values) => SmtpSettings.FromConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());

        /// <summary>
        /// Verifies that settings use aspire mailpit endpoint.
        /// </summary>
        /// <param name="connectionString">The SMTP connection string to parse in this scenario.</param>
        [Theory]
        [InlineData("smtp://mailpit:32781")]
        [InlineData("endpoint=smtp://mailpit:32781")]
        public void Settings_UseAspireMailpitEndpoint(string connectionString)
        {
            SmtpSettings settings = Read(new () { ["ConnectionStrings:mail"] = connectionString });
            Assert.Equal("mailpit", settings.Host);
            Assert.Equal(32781, settings.Port);
            Assert.Equal(SecureSocketOptions.None, settings.Security);
            Assert.Null(settings.Username);
            Assert.Null(settings.Password);
        }

        /// <summary>
        /// Verifies that settings read all provider environment keys and override old aspire configuration.
        /// </summary>
        [Fact]
        public void Settings_ReadAllProviderEnvironmentKeysAndOverrideOldAspireConfiguration()
        {
            SmtpSettings settings = Read(new ()
            {
                ["ConnectionStrings:mail"] = "invalid obsolete connection",
                ["MAIL_HOST"] = "smtp.strato.de",
                ["MAIL_PORT"] = "465",
                ["MAIL_SECURITY"] = "SslOnConnect",
                ["MAIL_USERNAME"] = "mail@example.org",
                ["MAIL_PASSWORD"] = " password with spaces ",
                ["MAIL_FROM"] = "sender@example.org",
                ["MAIL_FROM_NAME"] = "Feuerwehr Ãœbung",
                ["MAIL_TIMEOUT_SECONDS"] = "45",
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

        /// <summary>
        /// Verifies that provider defaults require encryption.
        /// </summary>
        /// <param name="port">The SMTP server port.</param>
        /// <param name="security">The transport security mode for the SMTP connection.</param>
        /// <param name="expected">The expected result or value for the scenario.</param>
        /// <param name="expectedPort">The SMTP port expected after connection-string parsing.</param>
        [Theory]
        [InlineData("465", null, SecureSocketOptions.SslOnConnect, 465)]
        [InlineData("587", null, SecureSocketOptions.StartTls, 587)]
        [InlineData(null, null, SecureSocketOptions.StartTls, 587)]
        [InlineData(null, "sslonconnect", SecureSocketOptions.SslOnConnect, 465)]
        [InlineData(null, "STARTTLS", SecureSocketOptions.StartTls, 587)]
        public void ProviderDefaults_RequireEncryption(string? port, string? security, SecureSocketOptions expected, int expectedPort)
        {
            SmtpSettings settings = Read(new () { ["MAIL_HOST"] = "smtp.example.org", ["MAIL_PORT"] = port, ["MAIL_SECURITY"] = security });
            Assert.Equal(expected, settings.Security);
            Assert.Equal(expectedPort, settings.Port);
        }

        /// <summary>
        /// Verifies that local defaults and legacy sender remain supported.
        /// </summary>
        [Fact]
        public void LocalDefaultsAndLegacySender_RemainSupported()
        {
            SmtpSettings settings = Read(new () { ["Mail:From"] = "legacy@example.org" });
            Assert.Equal("localhost", settings.Host);
            Assert.Equal(1025, settings.Port);
            Assert.Equal(SecureSocketOptions.None, settings.Security);
            Assert.Equal("legacy@example.org", settings.From);
            Assert.Equal(30000, settings.TimeoutMilliseconds);
        }

        /// <summary>
        /// Verifies that empty provider values allow app host to force mailpit when provider credentials are inherited.
        /// </summary>
        [Fact]
        public void EmptyProviderValues_AllowAppHostToForceMailpitWhenProviderCredentialsAreInherited()
        {
            SmtpSettings settings = Read(new ()
            {
                ["ConnectionStrings:mail"] = "smtp://mailpit:32781",
                ["MAIL_HOST"] = string.Empty,
                ["MAIL_PORT"] = string.Empty,
                ["MAIL_USERNAME"] = string.Empty,
                ["MAIL_PASSWORD"] = string.Empty,
                ["MAIL_SECURITY"] = "None",
                ["MAIL_FROM"] = "noreply@localhost.de",
            });
            Assert.Equal("mailpit", settings.Host);
            Assert.Equal(32781, settings.Port);
            Assert.Equal(SecureSocketOptions.None, settings.Security);
            Assert.Null(settings.Username);
        }

        /// <summary>
        /// Verifies that invalid settings are rejected.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <param name="value">The string value to process.</param>
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
        public void InvalidSettings_AreRejected(string key, string value)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Read(new () { [key] = value }));
            Assert.Contains(key, exception.Message);
        }

        /// <summary>
        /// Verifies that incomplete or unencrypted credentials are rejected without leaking password.
        /// </summary>
        /// <param name="username">The user's login name.</param>
        /// <param name="password">The password supplied for authentication.</param>
        /// <param name="security">The transport security mode for the SMTP connection.</param>
        [Theory]
        [InlineData("user@example.org", null, "StartTls")]
        [InlineData(null, "secret-value", "StartTls")]
        [InlineData("user@example.org", "secret-value", "None")]
        public void IncompleteOrUnencryptedCredentials_AreRejectedWithoutLeakingPassword(string? username, string? password, string security)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Read(new ()
            {
                ["MAIL_USERNAME"] = username,
                ["MAIL_PASSWORD"] = password,
                ["MAIL_SECURITY"] = security,
            }));
            Assert.DoesNotContain("secret-value", exception.ToString());
        }

        /// <summary>
        /// Verifies that invalid connection string is rejected without leaking credentials.
        /// </summary>
        [Fact]
        public void InvalidConnectionString_IsRejectedWithoutLeakingCredentials()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Read(new () { ["ConnectionStrings:mail"] = "endpoint=invalid;password=secret-value" }));
            Assert.DoesNotContain("secret-value", exception.ToString());
        }
    }
}
