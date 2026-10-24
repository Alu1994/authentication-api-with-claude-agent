using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Permissions.Delete;

public sealed partial class DeletePermissionCommandHandler(
    IPermissionRepository repository,
    TimeProvider timeProvider,
    ILogger<DeletePermissionCommandHandler> logger)
    : ICommandHandler<DeletePermissionCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePermissionCommand command, CancellationToken cancellationToken)
    {
        var permission = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (permission is null)
        {
            return PermissionErrors.NotFound;
        }

        permission.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        LogPermissionDeleted(logger, permission.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information,
        Message = "Permissao {PermissionId} excluida (soft delete).")]
    private static partial void LogPermissionDeleted(ILogger logger, Guid permissionId);
}
