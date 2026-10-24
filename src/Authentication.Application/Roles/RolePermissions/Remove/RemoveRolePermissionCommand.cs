namespace Authentication.Application.Roles.RolePermissions.Remove;

/// <summary>Remove o vinculo entre uma permissao e uma role.</summary>
public sealed record RemoveRolePermissionCommand(Guid RoleId, Guid PermissionId);
