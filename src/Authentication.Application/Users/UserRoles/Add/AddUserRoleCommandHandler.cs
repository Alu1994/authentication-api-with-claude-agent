using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.UserRoles.Add;

public sealed partial class AddUserRoleCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    TimeProvider timeProvider,
    ILogger<AddUserRoleCommandHandler> logger)
    : ICommandHandler<AddUserRoleCommand, Result>
{
    public async Task<Result> HandleAsync(AddUserRoleCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(command.UserId, cancellationToken) is null)
        {
            return UserErrors.NotFound;
        }

        if (await roles.GetByIdAsync(command.RoleId, cancellationToken) is null)
        {
            return RoleErrors.NotFound;
        }

        if (await users.GetRoleLinkAsync(command.UserId, command.RoleId, cancellationToken) is not null)
        {
            return Result.Success();
        }

        users.AddRoleLink(UserRole.Create(command.UserId, command.RoleId, timeProvider.GetUtcNow()));

        try
        {
            await users.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // Outra requisicao criou o mesmo vinculo em paralelo: o estado final e o desejado.
            return Result.Success();
        }

        LogRoleAssigned(logger, command.RoleId, command.UserId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Role {RoleId} atribuida ao usuario {UserId}.")]
    private static partial void LogRoleAssigned(ILogger logger, Guid roleId, Guid userId);
}
