using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Authentication.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Platform;

/// <summary>
/// Valida quais Meters a API exporta. Sem OTEL_EXPORTER_OTLP_ENDPOINT o MeterProvider nao tem
/// reader (e nao assina nenhum Meter), entao o teste pluga um exporter em memoria num host derivado.
/// </summary>
[Collection(ApiCollection.Name)]
public class TelemetryTests(ApiFactory factory)
{
    [Fact]
    public async Task DatabaseAccess_ExportsNpgsqlMetrics()
    {
        var exporter = new CollectingMetricExporter();
        await using var app = CreateAppWith(exporter);
        using var client = app.CreateClient();

        // /healthcheck consulta o PostgreSQL (AddDbContextCheck) e gera metricas do Npgsql.
        var response = await client.GetAsync("/healthcheck", Ct);
        response.EnsureSuccessStatusCode();
        app.Services.GetRequiredService<MeterProvider>().ForceFlush();

        Assert.Contains("Npgsql", exporter.MeterNames);
    }

    [Fact]
    public async Task MeterProvider_IgnoresUnregisteredMeters()
    {
        await using var app = CreateAppWith(new CollectingMetricExporter());
        _ = app.Services.GetRequiredService<MeterProvider>();

        using var meter = new Meter($"Unregistered.{Guid.NewGuid():N}");
        var probe = meter.CreateCounter<long>("telemetry.test.probe");

        Assert.False(probe.Enabled);
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateAppWith(CollectingMetricExporter exporter) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddReader(
                new PeriodicExportingMetricReader(exporter, exportIntervalMilliseconds: int.MaxValue)))));

    private sealed class CollectingMetricExporter : BaseExporter<Metric>
    {
        private readonly ConcurrentDictionary<string, byte> _meterNames = new();

        public ICollection<string> MeterNames => _meterNames.Keys;

        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                _meterNames.TryAdd(metric.MeterName, 0);
            }

            return ExportResult.Success;
        }
    }
}
