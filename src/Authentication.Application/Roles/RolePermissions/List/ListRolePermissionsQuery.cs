namespace Authentication.Application.Roles.RolePermissions.List;

/// <summary>Permissoes ativas vinculadas a uma role (sem paginacao).</summary>
public sealed record ListRolePermissionsQuery(Guid RoleId);
