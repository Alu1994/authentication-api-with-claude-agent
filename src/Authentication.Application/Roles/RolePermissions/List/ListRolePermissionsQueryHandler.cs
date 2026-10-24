using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;

namespace Authentication.Application.Roles.RolePermissions.List;

public sealed class ListRolePermissionsQueryHandler(IRoleRepository roles)
    : IQueryHandler<ListRolePermissionsQuery, Result<IReadOnlyList<PermissionResponse>>>
{
    public async Task<Result<IReadOnlyList<PermissionResponse>>> HandleAsync(
        ListRolePermissionsQuery query,
        CancellationToken cancellationToken)
    {
        if (await roles.GetByIdAsync(query.RoleId, cancellationToken) is null)
        {
            return RoleErrors.NotFound;
        }

        var items = await roles.ListActivePermissionsAsync(query.RoleId, cancellationToken);

        return Result<IReadOnlyList<PermissionResponse>>.Success(
            items.Select(PermissionResponse.FromPermission).ToList());
    }
}
