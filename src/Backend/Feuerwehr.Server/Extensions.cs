using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace De.Openelp.Feuerwehr.Server
{
    // Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    // This project should be referenced by each service project in your solution.
    // To learn more about using this project, see https://aka.ms/aspire/service-defaults
    /// <summary>
    /// Configures service discovery, resilience, health checks, and OpenTelemetry for the API host.
    /// </summary>
    public static class Extensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        /// <summary>
        /// Registers service discovery, HTTP resilience, health checks, and OpenTelemetry defaults.
        /// </summary>
        /// <typeparam name="TBuilder">The application builder type, preserved by the configuration extension.</typeparam>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
        /// <returns>The same application builder with the requested defaults configured.</returns>
        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            _ = builder.ConfigureOpenTelemetry();

            _ = builder.AddDefaultHealthChecks();

            _ = builder.Services.AddServiceDiscovery();

            _ = builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                _ = http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                _ = http.AddServiceDiscovery();
            });

            // Uncomment the following to restrict the allowed schemes for service discovery.
            // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
            // {
            //     options.AllowedSchemes = ["https"];
            // });

            return builder;
        }

        /// <summary>
        /// Configures application logging, metrics, tracing, and telemetry export.
        /// </summary>
        /// <typeparam name="TBuilder">The application builder type, preserved by the configuration extension.</typeparam>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
        /// <returns>The same application builder with the requested defaults configured.</returns>
        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            _ = builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            _ = builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    _ = metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();
                })
                .WithTracing(tracing =>
                {
                    _ = tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(tracing =>
                            // Exclude health check requests from tracing
                            tracing.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                        )
                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation();
                });

            _ = builder.AddOpenTelemetryExporters();

            return builder;
        }

        /// <summary>
        /// Registers the self health check used by the liveness endpoint.
        /// </summary>
        /// <typeparam name="TBuilder">The application builder type, preserved by the configuration extension.</typeparam>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
        /// <returns>The same application builder with the requested defaults configured.</returns>
        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            _ = builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        /// <summary>
        /// Maps development health endpoints and the Prometheus metrics endpoint.
        /// </summary>
        /// <param name="app">The web application receiving the default endpoints.</param>
        /// <returns>The application with the default endpoints mapped.</returns>
        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Adding health checks endpoints to applications in non-development environments has security implications.
            // See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
            if (app.Environment.IsDevelopment())
            {
                // All health checks must pass for app to be considered ready to accept traffic after starting
                _ = app.MapHealthChecks(HealthEndpointPath);

                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                _ = app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
            }

            return app;
        }

        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                _ = builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
            //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            //{
            //    builder.Services.AddOpenTelemetry()
            //       .UseAzureMonitor();
            //}

            return builder;
        }
    }
}
