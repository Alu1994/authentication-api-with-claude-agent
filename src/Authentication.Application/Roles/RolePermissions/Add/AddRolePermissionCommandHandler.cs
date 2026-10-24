using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.RolePermissions.Add;

public sealed partial class AddRolePermissionCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    TimeProvider timeProvider,
    ILogger<AddRolePermissionCommandHandler> logger)
    : ICommandHandler<AddRolePermissionCommand, Result>
{
    public async Task<Result> HandleAsync(AddRolePermissionCommand command, CancellationToken cancellationToken)
    {
        if (await roles.GetByIdAsync(command.RoleId, cancellationToken) is null)
        {
            return RoleErrors.NotFound;
        }

        if (await permissions.GetByIdAsync(command.PermissionId, cancellationToken) is null)
        {
            return PermissionErrors.NotFound;
        }

        if (await roles.GetPermissionLinkAsync(command.RoleId, command.PermissionId, cancellationToken) is not null)
        {
            return Result.Success();
        }

        roles.AddPermissionLink(RolePermission.Create(command.RoleId, command.PermissionId, timeProvider.GetUtcNow()));

        try
        {
            await roles.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // Outra requisicao criou o mesmo vinculo em paralelo: o estado final e o desejado.
            return Result.Success();
        }

        LogPermissionLinked(logger, command.PermissionId, command.RoleId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4301, Level = LogLevel.Information,
        Message = "Permissao {PermissionId} vinculada a role {RoleId}.")]
    private static partial void LogPermissionLinked(ILogger logger, Guid permissionId, Guid roleId);
}
