using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.Delete;

public sealed partial class DeleteRoleCommandHandler(
    IRoleRepository repository,
    TimeProvider timeProvider,
    ILogger<DeleteRoleCommandHandler> logger)
    : ICommandHandler<DeleteRoleCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (role is null)
        {
            return RoleErrors.NotFound;
        }

        if (await repository.IsAssignedToActiveUserAsync(role.Id, cancellationToken))
        {
            LogDeleteRejectedInUse(logger, role.Id);
            return RoleErrors.InUse;
        }

        role.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        LogRoleDeleted(logger, role.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4201, Level = LogLevel.Information, Message = "Role {RoleId} excluida (soft delete).")]
    private static partial void LogRoleDeleted(ILogger logger, Guid roleId);

    [LoggerMessage(EventId = 4202, Level = LogLevel.Warning,
        Message = "Exclusao da role {RoleId} rejeitada: atribuida a usuarios ativos.")]
    private static partial void LogDeleteRejectedInUse(ILogger logger, Guid roleId);
}
