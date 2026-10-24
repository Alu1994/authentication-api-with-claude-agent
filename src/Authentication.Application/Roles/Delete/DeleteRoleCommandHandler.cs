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

        role.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        LogRoleDeleted(logger, role.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4201, Level = LogLevel.Information, Message = "Role {RoleId} excluida (soft delete).")]
    private static partial void LogRoleDeleted(ILogger logger, Guid roleId);
}
