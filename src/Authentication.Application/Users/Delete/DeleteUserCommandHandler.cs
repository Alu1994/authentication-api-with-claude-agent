using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.Delete;

public sealed partial class DeleteUserCommandHandler(
    IUserRepository repository,
    TimeProvider timeProvider,
    ILogger<DeleteUserCommandHandler> logger)
    : ICommandHandler<DeleteUserCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        user.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        LogUserDeleted(logger, user.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 1301, Level = LogLevel.Information, Message = "Usuario {UserId} excluido (soft delete).")]
    private static partial void LogUserDeleted(ILogger logger, Guid userId);
}
