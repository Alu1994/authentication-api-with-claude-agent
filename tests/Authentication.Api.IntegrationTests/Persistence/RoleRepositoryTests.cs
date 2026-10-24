using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Roles;
using Microsoft.Extensions.DependencyInjection;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public class RoleRepositoryTests(ApiFactory factory)
{
    [Fact]
    public async Task SaveChangesAsync_TranslatesUniqueViolation_ForNameDifferingOnlyInCase()
    {
        var name = UniqueRoleName();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            repository.Add(Role.Create(name, null, DateTimeOffset.UtcNow));
            await repository.SaveChangesAsync(Ct);
        }

        await using var raceScope = factory.Services.CreateAsyncScope();
        var raceRepository = raceScope.ServiceProvider.GetRequiredService<IRoleRepository>();
        raceRepository.Add(Role.Create(name.ToUpperInvariant(), null, DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => raceRepository.SaveChangesAsync(Ct));
        Assert.Equal("ix_roles_normalized_name", exception.ConstraintName);
    }
}
