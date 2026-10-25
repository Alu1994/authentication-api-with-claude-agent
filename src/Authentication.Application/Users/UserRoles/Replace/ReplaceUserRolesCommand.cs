namespace Authentication.Application.Users.UserRoles.Replace;

/// <summary>
/// Substitui todas as roles do usuario pela lista informada (tudo ou nada). Lista vazia remove todas
/// as atribuicoes; Ids repetidos sao ignorados.
/// </summary>
public sealed record ReplaceUserRolesCommand(Guid UserId, IReadOnlyList<Guid>? RoleIds);
