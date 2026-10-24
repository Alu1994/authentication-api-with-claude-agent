using System.Text;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Infrastructure.Security;

/// <summary>
/// Emite JWT assinados com HS256 com claims de identidade (sub, email, name, jti) e de autorizacao:
/// <c>role</c> e <c>permission</c>, emitidas como arrays JSON (uma claim por item ao validar o token)
/// e omitidas quando vazias.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken CreateToken(User user, UserAuthorization authorization)
    {
        var jwt = options.Value;
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var expiresAt = issuedAt.AddMinutes(jwt.ExpirationMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Name] = user.Name,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };

        // Sempre como array (mesmo com um unico item) para manter o formato da claim estavel para os consumidores.
        AddMultiValued(claims, AuthorizationClaimTypes.Role, authorization.Roles);
        AddMultiValued(claims, AuthorizationClaimTypes.Permission, authorization.Permissions);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }

    private static void AddMultiValued(Dictionary<string, object> claims, string type, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            claims[type] = values.ToArray();
        }
    }
}
