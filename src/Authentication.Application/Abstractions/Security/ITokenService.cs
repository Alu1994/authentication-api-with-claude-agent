using Authentication.Application.Users;

namespace Authentication.Application.Abstractions.Security;

/// <summary>Token de acesso emitido para um usuario autenticado.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Emite tokens de acesso (JWT) para usuarios autenticados.</summary>
public interface ITokenService
{
    AccessToken CreateToken(User user);
}
