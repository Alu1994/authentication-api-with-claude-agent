using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.RolePermissions.Replace;

public sealed partial class ReplaceRolePermissionsCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    TimeProvider timeProvider,
    ILogger<ReplaceRolePermissionsCommandHandler> logger)
    : ICommandHandler<ReplaceRolePermissionsCommand, Result>
{
    private const string Field = "permissionIds";

    public async Task<Result> HandleAsync(ReplaceRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        if (command.PermissionIds is null)
        {
            return new ValidationErrorCollector()
                .AddIf(true, Field, "A lista de permissoes e obrigatoria (use [] para remover todas).")
                .ToError();
        }

        if (await roles.GetByIdAsync(command.RoleId, cancellationToken) is null)
        {
            return RoleErrors.NotFound;
        }

        var requested = command.PermissionIds.ToHashSet();

        if (requested.Count > 0)
        {
            var active = await permissions.GetActiveIdsAsync(requested, cancellationToken);
            var invalid = requested.Where(id => !active.Contains(id)).ToList();
            if (invalid.Count > 0)
            {
                return new ValidationErrorCollector()
                    .AddIf(true, Field, $"Permissoes inexistentes ou excluidas: {string.Join(", ", invalid)}.")
                    .ToError();
            }
        }

        var links = await roles.GetPermissionLinksAsync(command.RoleId, cancellationToken);
        var current = links.Select(l => l.PermissionId).ToHashSet();
        var now = timeProvider.GetUtcNow();

        foreach (var link in links.Where(l => !requested.Contains(l.PermissionId)))
        {
            roles.RemovePermissionLink(link);
        }

        foreach (var permissionId in requested.Where(id => !current.Contains(id)))
        {
            roles.AddPermissionLink(RolePermission.Create(command.RoleId, permissionId, now));
        }

        await roles.SaveChangesAsync(cancellationToken);

        LogPermissionsReplaced(logger, command.RoleId, requested.Count);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4303, Level = LogLevel.Information,
        Message = "Permissoes da role {RoleId} substituidas ({Count} vinculos).")]
    private static partial void LogPermissionsReplaced(ILogger logger, Guid roleId, int count);
}
