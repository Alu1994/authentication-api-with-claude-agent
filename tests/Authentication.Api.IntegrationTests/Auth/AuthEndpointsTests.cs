using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
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
    }

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
