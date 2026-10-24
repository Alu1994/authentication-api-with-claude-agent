using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Permissions.GetById;

public sealed class GetPermissionByIdQueryHandler(IPermissionRepository repository)
    : IQueryHandler<GetPermissionByIdQuery, Result<PermissionResponse>>
{
    public async Task<Result<PermissionResponse>> HandleAsync(
        GetPermissionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var permission = await repository.GetByIdAsync(query.Id, cancellationToken);

        return permission is null ? PermissionErrors.NotFound : PermissionResponse.FromPermission(permission);
    }
}
