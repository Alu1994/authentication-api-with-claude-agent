using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Authentication.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API completa (DI, middlewares, auth, migrations) contra um PostgreSQL real em container.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "chave-de-integracao-com-mais-de-32-caracteres";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync() =>
        await _postgres.StartAsync(TestContext.Current.CancellationToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development: aplica as migrations automaticamente no startup (mesmo fluxo do Aspire).
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:authdb", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);

        // No TestServer todas as requisicoes compartilham o mesmo "IP"; limite alto evita interferencia
        // entre testes. O comportamento do limite e coberto em LoginRateLimitingTests.
        builder.UseSetting("RateLimiting:Login:PermitLimit", "10000");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
