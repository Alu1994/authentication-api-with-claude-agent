using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Defaults compartilhados: OpenTelemetry, health checks, service discovery e resiliencia de HttpClient.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Extensions
{
    /// <summary>Tag dos health checks de liveness (usados por <c>/heartbeat</c>).</summary>
    public const string LiveTag = "live";

    public const string HeartbeatPath = "/heartbeat";
    public const string HealthCheckPath = "/healthcheck";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !IsHealthEndpoint(context.Request.Path))
                .AddHttpClientInstrumentation());

        // Exporter OTLP habilitado somente quando OTEL_EXPORTER_OTLP_ENDPOINT estiver definida
        // (sidecar/collector do ambiente ou dashboard do Aspire). Sem valor fixo no codigo.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);

        return builder;
    }

    /// <summary>
    /// <c>/heartbeat</c>: liveness (apenas checks com a tag <see cref="LiveTag"/>).
    /// <c>/healthcheck</c>: todas as dependencias externas, com detalhamento em JSON.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HeartbeatPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
        }).AllowAnonymous().ExcludeFromDescription();

        app.MapHealthChecks(HealthCheckPath, new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponseAsync,
        }).AllowAnonymous().ExcludeFromDescription();

        return app;
    }

    private static bool IsHealthEndpoint(PathString path) =>
        path.StartsWithSegments(HeartbeatPath) || path.StartsWithSegments(HealthCheckPath);

    private static Task WriteDetailedResponseAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(
            new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        durationMs = entry.Value.Duration.TotalMilliseconds,
                        description = entry.Value.Description,
                    }),
            },
            context.RequestAborted);
}
