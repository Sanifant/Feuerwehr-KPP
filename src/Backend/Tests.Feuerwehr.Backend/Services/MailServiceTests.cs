using De.Openelp.Feuerwehr.Server.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public class MailServiceTests
{
    private static SmtpSettings Settings(bool authenticate = true) => new()
    {
        Host = authenticate ? "smtp.strato.de" : "mailpit",
        Port = authenticate ? 465 : 1025,
        Security = authenticate ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None,
        Username = authenticate ? "mail@example.org" : null,
        Password = authenticate ? "test-only-password" : null,
        From = "mail@example.org", FromName = "Feuerwehr Ãœbung", TimeoutMilliseconds = 45000,
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    /// <summary>
    /// Sends the connects Authenticates And Sends Configured Message.
    /// </summary>
    /// <param name="authenticate">The authenticate.</param>
    public async Task Send_ConnectsAuthenticatesAndSendsConfiguredMessage(bool authenticate)
    {
        var settings = Settings(authenticate);
        var client = new Mock<ISmtpClient>(MockBehavior.Strict);
        client.SetupSet(c => c.Timeout = settings.TimeoutMilliseconds);
        client.SetupGet(c => c.IsConnected).Returns(true);
        var sequence = new MockSequence();
        client.InSequence(sequence).Setup(c => c.ConnectAsync(settings.Host, settings.Port, settings.Security, default))
            .Returns(Task.CompletedTask);
        if (authenticate)
            client.InSequence(sequence).Setup(c => c.AuthenticateAsync(settings.Username!, settings.Password!, default))
                .Returns(Task.CompletedTask);
        MimeMessage? sent = null;
        client.InSequence(sequence).Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null))
            .Callback<MimeMessage, CancellationToken, MailKit.ITransferProgress?>((message, _, _) => sent = message)
            .ReturnsAsync("queued");
        client.InSequence(sequence).Setup(c => c.DisconnectAsync(true, default)).Returns(Task.CompletedTask);

        await new MailService(client.Object, settings, NullLogger<MailService>.Instance)
            .SendAsync("recipient@example.org", "Einladung für Müller", "Passwort vergeben: https://example.org/password/test");

        Assert.NotNull(sent);
        var sender = Assert.Single(sent.From.Mailboxes);
        Assert.Equal(settings.From, sender.Address);
        Assert.Equal(settings.FromName, sender.Name);
        Assert.Equal("recipient@example.org", Assert.Single(sent.To.Mailboxes).Address);
        Assert.Equal("Einladung für Müller", sent.Subject);
        Assert.Equal("Passwort vergeben: https://example.org/password/test", sent.TextBody);
        Assert.Null(sent.HtmlBody);
        client.VerifyAll();
        if (!authenticate)
            client.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Theory]
    [InlineData("connect")]
    [InlineData("authenticate")]
    [InlineData("send")]
    /// <summary>
    /// Sends the propagates Failures And Closes Open Connections.
    /// </summary>
    /// <param name="failingStep">The failing Step.</param>
    public async Task Send_PropagatesFailuresAndClosesOpenConnections(string failingStep)
    {
        var settings = Settings();
        var client = new Mock<ISmtpClient>();
        var failure = new IOException("SMTP test failure");
        client.SetupGet(c => c.IsConnected).Returns(failingStep != "connect");
        client.Setup(c => c.ConnectAsync(settings.Host, settings.Port, settings.Security, default))
            .Returns(failingStep == "connect" ? Task.FromException(failure) : Task.CompletedTask);
        client.Setup(c => c.AuthenticateAsync(settings.Username!, settings.Password!, default))
            .Returns(failingStep == "authenticate" ? Task.FromException(failure) : Task.CompletedTask);
        client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null))
            .Returns(failingStep == "send" ? Task.FromException<string>(failure) : Task.FromResult("queued"));
        client.Setup(c => c.DisconnectAsync(true, default)).ThrowsAsync(new IOException("Disconnect failed"));

        var exception = await Assert.ThrowsAsync<IOException>(() =>
            new MailService(client.Object, settings, NullLogger<MailService>.Instance).SendAsync("recipient@example.org", "Test", "Body"));

        Assert.Same(failure, exception);
        client.Verify(c => c.DisconnectAsync(true, default), failingStep == "connect" ? Times.Never() : Times.Once());
        if (failingStep != "send") client.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null), Times.Never);
    }

    [Fact]
    /// <summary>
    /// Executes disconnect Failure After Accepted Message Does Not Fail Delivery.
    /// </summary>
    public async Task DisconnectFailure_AfterAcceptedMessageDoesNotFailDelivery()
    {
        var client = new Mock<ISmtpClient>();
        client.SetupGet(c => c.IsConnected).Returns(true);
        client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null)).ReturnsAsync("queued");
        client.Setup(c => c.DisconnectAsync(true, default)).ThrowsAsync(new IOException("Disconnect failed"));

        await new MailService(client.Object, Settings(), NullLogger<MailService>.Instance)
            .SendAsync("recipient@example.org", "Test", "Body");

        client.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null), Times.Once);
    }
}

