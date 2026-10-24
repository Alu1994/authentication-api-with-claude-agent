using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public class UserRepositoryTests(ApiFactory factory)
{
    private static User NewUser(string email, string cpf, string name = "Fulano de Tal") =>
        User.Create(name, email, cpf, "11987654321", new DateOnly(1990, 5, 10), "hash", DateTimeOffset.UtcNow);

    [Fact]
    public async Task SaveChangesAsync_TranslatesUniqueViolation_WhenPreCheckIsBypassed()
    {
        var email = UniqueEmail();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            repository.Add(NewUser(email, UniqueCpf()));
            await repository.SaveChangesAsync(Ct);
        }

        await using var raceScope = factory.Services.CreateAsyncScope();
        var raceRepository = raceScope.ServiceProvider.GetRequiredService<IUserRepository>();
        raceRepository.Add(NewUser(email, UniqueCpf()));

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => raceRepository.SaveChangesAsync(Ct));
        Assert.Equal("ix_users_email", exception.ConstraintName);
    }

    [Fact]
    public async Task ListAsync_TreatsLikeWildcardsInSearchAsLiterals()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        repository.Add(NewUser(UniqueEmail(), UniqueCpf(), $"Literal {marker} 100%"));
        repository.Add(NewUser(UniqueEmail(), UniqueCpf(), $"Literal {marker} 1000"));
        await repository.SaveChangesAsync(Ct);

        var page = await repository.ListAsync(1, 10, $"{marker} 100%", Ct);

        var user = Assert.Single(page.Items);
        Assert.Equal($"Literal {marker} 100%", user.Name);
    }
}
