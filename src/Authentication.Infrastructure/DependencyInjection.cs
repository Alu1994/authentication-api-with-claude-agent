using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Authentication.Infrastructure.Persistence;
using Authentication.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Authentication.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Nome da connection string do PostgreSQL (injetada pelo Aspire como <c>ConnectionStrings__authdb</c>).</summary>
    public const string ConnectionStringName = "authdb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AuthenticationDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                    .GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' nao configurada.");

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateOnStart();

        return services;
    }

    /// <summary>Aplica as migrations pendentes. Uso restrito a Development/Aspire.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
