namespace Authentication.Application.Roles.RolePermissions.Add;

/// <summary>Vincula uma permissao a uma role (idempotente).</summary>
public sealed record AddRolePermissionCommand(Guid RoleId, Guid PermissionId);
