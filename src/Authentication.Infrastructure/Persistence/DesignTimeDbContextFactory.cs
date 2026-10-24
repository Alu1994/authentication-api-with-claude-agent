using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Authentication.Infrastructure.Persistence;

/// <summary>
/// Usado apenas pelo <c>dotnet ef</c> (migrations/bundle). A connection string real e
/// informada em tempo de execucao (ex.: <c>efbundle --connection</c>).
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
{
    public AuthenticationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseNpgsql("Host=localhost;Database=authdb")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuthenticationDbContext(options);
    }
}
