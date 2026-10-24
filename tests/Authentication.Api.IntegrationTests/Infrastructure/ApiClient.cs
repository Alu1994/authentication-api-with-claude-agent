using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authentication.Application.Auth.Login;
using Authentication.Application.Permissions;
using Authentication.Application.Users;
using Authentication.Application.Users.Validation;

namespace Authentication.Api.IntegrationTests.Infrastructure;

/// <summary>Helpers para montar cenarios de teste com dados unicos por execucao.</summary>
internal static class ApiClient
{
    public const string DefaultPassword = "Senha123";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static string UniqueCpf()
    {
        while (true)
        {
            var baseDigits = string.Concat(Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(0, 10)));
            for (var check = 0; check < 100; check++)
            {
                var candidate = $"{baseDigits}{check:D2}";
                if (Cpf.IsValid(candidate))
                {
                    return candidate;
                }
            }
        }
    }

    public static object NewUserPayload(string? email = null, string? cpf = null, string name = "Fulano de Tal") => new
    {
        name,
        email = email ?? UniqueEmail(),
        cpf = cpf ?? UniqueCpf(),
        phone = "(11) 98765-4321",
        birthDate = "1990-05-10",
        password = DefaultPassword,
    };

    public static async Task<UserResponse> RegisterAsync(this HttpClient client, object? payload = null)
    {
        var response = await client.PostAsJsonAsync("/users", payload ?? NewUserPayload(), Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserResponse>(Ct))!;
    }

    public static async Task<string> LoginAsync(this HttpClient client, string email, string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(Ct))!.AccessToken;
    }

    /// <summary>Recurso de permissao unico por execucao (comeca com letra; apenas [a-z0-9]).</summary>
    public static string UniqueResource() => $"r{Guid.NewGuid():N}"[..21];

    public static async Task<PermissionResponse> CreatePermissionAsync(
        this HttpClient client,
        string resource,
        string action = "read",
        string? description = null)
    {
        var response = await client.PostAsJsonAsync("/permissions", new { resource, action, description }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PermissionResponse>(Ct))!;
    }

    /// <summary>Cadastra um usuario novo e autentica o client com o token dele.</summary>
    public static async Task<UserResponse> AuthenticateAsNewUserAsync(this HttpClient client)
    {
        var user = await client.RegisterAsync();
        var token = await client.LoginAsync(user.Email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return user;
    }
}
