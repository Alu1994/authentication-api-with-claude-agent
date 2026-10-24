using System.Text;
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

    [Fact]
    public async Task CreateToken_ReturnsSignedTokenWithIdentityClaims()
    {
        var token = CreateService().CreateToken(User);

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
    }

    [Fact]
    public void CreateToken_UsesConfiguredExpiration()
    {
        var token = CreateService().CreateToken(User);

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

        var first = new JsonWebToken(service.CreateToken(User).Value).Id;
        var second = new JsonWebToken(service.CreateToken(User).Value).Id;

        Assert.NotEqual(first, second);
    }
}
