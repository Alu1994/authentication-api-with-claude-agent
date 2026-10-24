using static Authentication.Application.Abstractions.Time.Timestamps;

namespace Authentication.Application.Roles;

/// <summary>
/// Vinculo entre uma role e uma permissao. Removido fisicamente ao desvincular (sem soft delete)
/// e nao altera o <c>updatedAt</c> da role.
/// </summary>
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RolePermission Create(Guid roleId, Guid permissionId, DateTimeOffset createdAt) =>
        new()
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = TruncateToMicroseconds(createdAt),
        };
}
