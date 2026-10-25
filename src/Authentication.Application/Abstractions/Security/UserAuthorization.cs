namespace Authentication.Application.Abstractions.Security;

/// <summary>
/// Dados de autorizacao do usuario no momento do login, emitidos como claims no token:
/// nomes das roles ativas e permissoes ativas distintas (<c>resource:action</c>) dessas roles.
/// </summary>
public sealed record UserAuthorization(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)
{
    public static readonly UserAuthorization None = new([], []);
}

/// <summary>Tipos de claim de autorizacao emitidos no JWT (uma claim por item).</summary>
public static class AuthorizationClaimTypes
{
    public const string Role = "role";
    public const string Permission = "permission";
}
