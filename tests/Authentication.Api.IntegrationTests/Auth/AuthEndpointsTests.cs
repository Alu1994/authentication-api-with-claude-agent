using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Auth;
using Authentication.Application.Auth.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_ReturnsBearerToken_WithIdentityClaims()
    {
        var user = await _client.RegisterAsync();

        var response = await _client.PostAsJsonAsync("/auth/login", new { email = user.Email, password = DefaultPassword }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Ct);
        Assert.Equal("Bearer", login!.TokenType);
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);

        var jwt = new JsonWebToken(login.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Email, jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("authentication-api", jwt.Issuer);
        Assert.False(jwt.TryGetClaim(AuthorizationClaimTypes.Role, out _)); // sem roles: claims omitidas
        Assert.False(jwt.TryGetClaim(AuthorizationClaimTypes.Permission, out _));
    }

    [Fact]
    public async Task Login_EmitsRoleAndDistinctPermissionClaims_ReflectingChangesOnlyOnNextLogin()
    {
        await _client.AuthenticateAsNewUserAsync();
        var user = await _client.RegisterAsync();
        var resource = UniqueResource();
        var read = await _client.CreatePermissionAsync(resource, "read");
        var update = await _client.CreatePermissionAsync(resource, "update");
        var delete = await _client.CreatePermissionAsync(resource, "delete");
        var marker = Guid.NewGuid().ToString("N")[..8];
        var editor = await _client.CreateRoleAsync($"Editor {marker}");
        var viewer = await _client.CreateRoleAsync($"Viewer {marker}");
        var unrelated = await _client.CreateRoleAsync($"Unrelated {marker}");
        await _client.PutAsJsonAsync($"/roles/{editor.Id}/permissions", new { permissionIds = new[] { read.Id, update.Id } }, Ct);
        await _client.PutAsJsonAsync($"/roles/{viewer.Id}/permissions", new { permissionIds = new[] { read.Id } }, Ct);
        await _client.PutAsJsonAsync($"/roles/{unrelated.Id}/permissions", new { permissionIds = new[] { delete.Id } }, Ct);
        await _client.PutAsJsonAsync($"/users/{user.Id}/roles", new { roleIds = new[] { viewer.Id, editor.Id } }, Ct);

        var token = new JsonWebToken(await _client.LoginAsync(user.Email));

        Assert.Equal([editor.Name, viewer.Name], RoleClaims(token));
        Assert.Equal([$"{resource}:read", $"{resource}:update"], PermissionClaims(token)); // read aparece uma vez

        // Mudanca de vinculo nao altera o token ja emitido; vale a partir do proximo login.
        await _client.DeleteAsync($"/users/{user.Id}/roles/{editor.Id}", Ct);
        Assert.Equal([editor.Name, viewer.Name], RoleClaims(token));

        var next = new JsonWebToken(await _client.LoginAsync(user.Email));
        Assert.Equal([viewer.Name], RoleClaims(next));
        Assert.Equal([$"{resource}:read"], PermissionClaims(next));
    }

    private static string[] RoleClaims(JsonWebToken token) =>
        token.Claims.Where(c => c.Type == AuthorizationClaimTypes.Role).Select(c => c.Value).ToArray();

    private static string[] PermissionClaims(JsonWebToken token) =>
        token.Claims.Where(c => c.Type == AuthorizationClaimTypes.Permission).Select(c => c.Value).ToArray();

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPasswordOrUnknownEmail()
    {
        var user = await _client.RegisterAsync();

        var wrongPassword = await _client.PostAsJsonAsync("/auth/login", new { email = user.Email, password = "Errada123" }, Ct);
        var unknownEmail = await _client.PostAsJsonAsync("/auth/login", new { email = UniqueEmail(), password = DefaultPassword }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var problem = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(AuthErrors.InvalidCredentials.Message, problem!.Detail);
    }

    [Fact]
    public async Task Login_ReturnsValidationProblem_WhenCredentialsAreMissing()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
