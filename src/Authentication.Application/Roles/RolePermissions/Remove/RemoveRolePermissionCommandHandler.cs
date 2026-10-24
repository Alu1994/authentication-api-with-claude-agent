using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.RolePermissions.Remove;

public sealed partial class RemoveRolePermissionCommandHandler(
    IRoleRepository roles,
    ILogger<RemoveRolePermissionCommandHandler> logger)
    : ICommandHandler<RemoveRolePermissionCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveRolePermissionCommand command, CancellationToken cancellationToken)
    {
        if (await roles.GetByIdAsync(command.RoleId, cancellationToken) is null)
        {
            return RoleErrors.NotFound;
        }

        // Vinculos com permissoes ja excluidas tambem podem ser removidos (limpeza).
        var link = await roles.GetPermissionLinkAsync(command.RoleId, command.PermissionId, cancellationToken);
        if (link is null)
        {
            return RoleErrors.PermissionNotLinked;
        }

        roles.RemovePermissionLink(link);
        await roles.SaveChangesAsync(cancellationToken);

        LogPermissionUnlinked(logger, command.PermissionId, command.RoleId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4302, Level = LogLevel.Information,
        Message = "Permissao {PermissionId} desvinculada da role {RoleId}.")]
    private static partial void LogPermissionUnlinked(ILogger logger, Guid permissionId, Guid roleId);
}
