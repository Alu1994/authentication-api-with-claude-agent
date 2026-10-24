using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Roles.GetById;

public sealed class GetRoleByIdQueryHandler(IRoleRepository repository)
    : IQueryHandler<GetRoleByIdQuery, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> HandleAsync(GetRoleByIdQuery query, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(query.Id, cancellationToken);

        return role is null ? RoleErrors.NotFound : RoleResponse.FromRole(role);
    }
}
