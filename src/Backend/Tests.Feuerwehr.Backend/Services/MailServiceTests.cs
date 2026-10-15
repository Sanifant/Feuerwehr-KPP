using de.openelp.feuerwehr.Server.Services;
using Microsoft.Extensions.Configuration;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public class MailServiceTests
{
    [Theory]
    [InlineData("smtp://mailpit:32781")]
    [InlineData("endpoint=smtp://mailpit:32781")]
    public void CreateClient_UsesAspireMailpitEndpoint(string connectionString)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mail"] = connectionString,
        }).Build();
        using var client = MailService.CreateClient(config);
        Assert.Equal("mailpit", client.Host);
        Assert.Equal(32781, client.Port);
    }

    [Fact]
    public void CreateClient_ExplicitSmtpOverridesAspire()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mail"] = "smtp://mailpit:32781",
            ["MAIL_HOST"] = "smtp.example.com",
            ["MAIL_PORT"] = "2525",
        }).Build();
        using var client = MailService.CreateClient(config);
        Assert.Equal("smtp.example.com", client.Host);
        Assert.Equal(2525, client.Port);
    }
}
