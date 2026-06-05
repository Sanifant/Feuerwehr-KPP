var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("compose");

var mailpit = builder.AddMailPit("mailpit");

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "postgres";
    })
    .WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("postgresdb");

var cache = builder.AddRedis("cache")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "cache";
    });

var server = builder.AddProject<Projects.Feuerwehr_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(mailpit)
    .WithHttpHealthCheck("/health")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "api";
        service.Ports.Add("5000");
    })
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "webfrontend";
    })
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
