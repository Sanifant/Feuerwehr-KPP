using Aspire.Hosting.Docker;
using Aspire.Hosting.JavaScript;
using De.Openelp.Feuerwehr.Diagnostics;

using ProcessDiagnostics diagnostics = ProcessDiagnostics.Start("AppHost");
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> seedAdminEmail = builder.AddParameterFromConfiguration(
    "seed-admin-email", "SeedAdmin:Email");
IResourceBuilder<ParameterResource> seedAdminPassword = builder.AddParameterFromConfiguration(
    "seed-admin-password", "SeedAdmin:Password", secret: true);

IResourceBuilder<DockerComposeEnvironmentResource> compose = builder.AddDockerComposeEnvironment("compose");

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithPgWeb()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "postgres";
    })
    .WithDataVolume(isReadOnly: false);

IResourceBuilder<PostgresDatabaseResource> postgresdb = postgres.AddDatabase("postgresdb");

IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "cache";
    });

IResourceBuilder<ProjectResource> server = builder.AddProject<Projects.Feuerwehr_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithEnvironment("SeedAdmin__Email", seedAdminEmail)
    .WithEnvironment("SeedAdmin__Password", seedAdminPassword)
    .WithEnvironment("JwtSettings__SecretKey", "YourSuperSecretKeyForJWT_MinimumLength32Characters_ChangeInProduction!")
    .WithHttpHealthCheck("/health")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "api";
        service.Ports.Add("5000");
    })
    .WithUrl("/scalar/v1", "Scalar API Endpoint")
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    IResourceBuilder<MailPitContainerResource> mailpit = builder.AddMailPit("mail");
    server.WithReference(mailpit)
        .WaitFor(mailpit)
        // Override inherited provider settings: local debugging always delivers to Mailpit.
        .WithEnvironment("MAIL_HOST", "")
        .WithEnvironment("MAIL_PORT", "")
        .WithEnvironment("MAIL_USERNAME", "")
        .WithEnvironment("MAIL_PASSWORD", "")
        .WithEnvironment("MAIL_SECURITY", "None")
        .WithEnvironment("MAIL_FROM", "noreply@localhost.de")
        .WithEnvironment("MAIL_FROM_NAME", "Feuerwehr (lokal)")
        .WithEnvironment("MAIL_TIMEOUT_SECONDS", "30");
}
else
{
    server.WithEnvironment("MAIL_HOST", builder.AddParameterFromConfiguration("mail-host", "MAIL_HOST"))
        .WithEnvironment("MAIL_PORT", builder.AddParameterFromConfiguration("mail-port", "MAIL_PORT"))
        .WithEnvironment("MAIL_USERNAME", builder.AddParameterFromConfiguration("mail-username", "MAIL_USERNAME"))
        .WithEnvironment("MAIL_PASSWORD", builder.AddParameterFromConfiguration("mail-password", "MAIL_PASSWORD", secret: true))
        .WithEnvironment("MAIL_FROM", builder.AddParameterFromConfiguration("mail-from", "MAIL_FROM"))
        .WithEnvironment("MAIL_SECURITY", builder.AddParameter("mail-security", builder.Configuration["MAIL_SECURITY"] ?? "StartTls", publishValueAsDefault: true))
        .WithEnvironment("MAIL_FROM_NAME", builder.AddParameter("mail-from-name", builder.Configuration["MAIL_FROM_NAME"] ?? "Feuerwehr", publishValueAsDefault: true))
        .WithEnvironment("MAIL_TIMEOUT_SECONDS", builder.AddParameter("mail-timeout-seconds", builder.Configuration["MAIL_TIMEOUT_SECONDS"] ?? "30", publishValueAsDefault: true));
}

IResourceBuilder<JavaScriptAppResource> webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithHttpEndpoint(port: 54321, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(server)
    .WithEnvironment("VITE_API_URL", "https://localhost:7538")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "webfrontend";
    })
    .WaitFor(server)
    .WithNpm();

builder.AddProject<Projects.Feuerwehr_App_Desktop>("app")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

diagnostics.Record("AppHost.Build.Start");
try
{
    DistributedApplication app = builder.Build();
    diagnostics.Record("AppHost.Run.Start");
    app.Run();
}
catch (Exception error)
{
    diagnostics.Record("AppHost.FatalException", exception: error);
    throw;
}

