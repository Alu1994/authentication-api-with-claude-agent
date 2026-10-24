namespace Authentication.Application.Roles.RolePermissions.Replace;

/// <summary>
/// Substitui todas as permissoes da role pela lista informada (tudo ou nada). Lista vazia remove todos
/// os vinculos; Ids repetidos sao ignorados.
/// </summary>
public sealed record ReplaceRolePermissionsCommand(Guid RoleId, IReadOnlyList<Guid>? PermissionIds);
