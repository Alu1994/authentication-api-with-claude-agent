using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.UserRoles.Remove;

public sealed partial class RemoveUserRoleCommandHandler(
    IUserRepository users,
    ILogger<RemoveUserRoleCommandHandler> logger)
    : ICommandHandler<RemoveUserRoleCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveUserRoleCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(command.UserId, cancellationToken) is null)
        {
            return UserErrors.NotFound;
        }

        // Vinculos com roles ja excluidas tambem podem ser removidos (limpeza).
        var link = await users.GetRoleLinkAsync(command.UserId, command.RoleId, cancellationToken);
        if (link is null)
        {
            return UserErrors.RoleNotLinked;
        }

        users.RemoveRoleLink(link);
        await users.SaveChangesAsync(cancellationToken);

        LogRoleUnassigned(logger, command.RoleId, command.UserId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Role {RoleId} removida do usuario {UserId}.")]
    private static partial void LogRoleUnassigned(ILogger logger, Guid roleId, Guid userId);
}
