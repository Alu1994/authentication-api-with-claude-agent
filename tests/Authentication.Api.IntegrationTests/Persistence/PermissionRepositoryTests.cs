using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Permissions;
using Microsoft.Extensions.DependencyInjection;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public class PermissionRepositoryTests(ApiFactory factory)
{
    [Fact]
    public async Task SaveChangesAsync_TranslatesUniqueViolation_WhenPreCheckIsBypassed()
    {
        var resource = UniqueResource();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
            repository.Add(Permission.Create(resource, "read", null, DateTimeOffset.UtcNow));
            await repository.SaveChangesAsync(Ct);
        }

        await using var raceScope = factory.Services.CreateAsyncScope();
        var raceRepository = raceScope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        Assert.True(await raceRepository.ExistsAsync(resource, "read", Ct));
        raceRepository.Add(Permission.Create(resource, "read", null, DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => raceRepository.SaveChangesAsync(Ct));
        Assert.Equal("ix_permissions_resource_action", exception.ConstraintName);
    }
}
