using System.Text;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Authentication.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "authentication-api",
        Audience = "authentication-clients",
        SigningKey = "chave-de-teste-com-pelo-menos-32-caracteres!",
        ExpirationMinutes = 30,
    };

    private static readonly User User = User.Create(
        "Fulano de Tal", "fulano@example.com", "52998224725", "11987654321",
        new DateOnly(1990, 5, 10), "hash", Now);

    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(Options), new FakeTimeProvider(Now));

    private static Task<TokenValidationResult> ValidateAsync(string token) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.SigningKey)),
            ValidateLifetime = false,
        });

    [Fact]
    public async Task CreateToken_ReturnsSignedTokenWithIdentityClaims()
    {
        var token = CreateService().CreateToken(User, UserAuthorization.None);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.SigningKey)),
            ValidateLifetime = false,
        });

        Assert.True(validation.IsValid, validation.Exception?.Message);
        var claims = validation.Claims;
        Assert.Equal(User.Id.ToString(), claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal(User.Email, claims[JwtRegisteredClaimNames.Email]);
        Assert.Equal(User.Name, claims[JwtRegisteredClaimNames.Name]);
        Assert.False(string.IsNullOrEmpty(claims[JwtRegisteredClaimNames.Jti] as string));
        Assert.False(claims.ContainsKey(AuthorizationClaimTypes.Role));
        Assert.False(claims.ContainsKey(AuthorizationClaimTypes.Permission));
    }

    [Fact]
    public async Task CreateToken_EmitsOneClaimPerRoleAndPermission()
    {
        var authorization = new UserAuthorization(["Admin", "Suporte N2"], ["roles:read", "users:read", "users:update"]);

        var token = CreateService().CreateToken(User, authorization);
        var validation = await ValidateAsync(token.Value);

        Assert.True(validation.IsValid, validation.Exception?.Message);
        var identity = validation.ClaimsIdentity;
        Assert.Equal(["Admin", "Suporte N2"], identity.FindAll(AuthorizationClaimTypes.Role).Select(c => c.Value));
        Assert.Equal(
            ["roles:read", "users:read", "users:update"],
            identity.FindAll(AuthorizationClaimTypes.Permission).Select(c => c.Value));
    }

    [Fact]
    public async Task CreateToken_EmitsSingleRoleAsItsOwnClaim()
    {
        var token = CreateService().CreateToken(User, new UserAuthorization(["Admin"], []));
        var validation = await ValidateAsync(token.Value);

        Assert.Equal("Admin", Assert.Single(validation.ClaimsIdentity.FindAll(AuthorizationClaimTypes.Role)).Value);
        Assert.Empty(validation.ClaimsIdentity.FindAll(AuthorizationClaimTypes.Permission));
    }

    [Fact]
    public void CreateToken_UsesConfiguredExpiration()
    {
        var token = CreateService().CreateToken(User, UserAuthorization.None);

        var jwt = new JsonWebToken(token.Value);
        Assert.Equal(Now.AddMinutes(30), token.ExpiresAt);
        Assert.Equal(Now.AddMinutes(30).UtcDateTime, jwt.ValidTo);
        Assert.Equal(Now.UtcDateTime, jwt.IssuedAt);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
    }

    [Fact]
    public void CreateToken_GeneratesUniqueJtiPerToken()
    {
        var service = CreateService();

        var first = new JsonWebToken(service.CreateToken(User, UserAuthorization.None).Value).Id;
        var second = new JsonWebToken(service.CreateToken(User, UserAuthorization.None).Value).Id;

        Assert.NotEqual(first, second);
    }
}
