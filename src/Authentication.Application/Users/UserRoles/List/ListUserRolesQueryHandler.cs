using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;

namespace Authentication.Application.Users.UserRoles.List;

public sealed class ListUserRolesQueryHandler(IUserRepository users)
    : IQueryHandler<ListUserRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> HandleAsync(
        ListUserRolesQuery query,
        CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(query.UserId, cancellationToken) is null)
        {
            return UserErrors.NotFound;
        }

        var items = await users.ListActiveRolesAsync(query.UserId, cancellationToken);

        return Result<IReadOnlyList<RoleResponse>>.Success(items.Select(RoleResponse.FromRole).ToList());
    }
}
