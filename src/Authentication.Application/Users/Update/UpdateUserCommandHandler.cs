using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Users.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.Update;

public sealed partial class UpdateUserCommandHandler(
    IUserRepository repository,
    TimeProvider timeProvider,
    ILogger<UpdateUserCommandHandler> logger)
    : ICommandHandler<UpdateUserCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var name = UserRules.NormalizeName(command.Name);
        var email = UserRules.NormalizeEmail(command.Email);
        var phone = UserRules.NormalizePhone(command.Phone);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var errors = new ValidationErrorCollector()
            .ValidateProfile(name, email, phone, command.BirthDate, today);

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var user = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (await repository.EmailExistsAsync(email, user.Id, cancellationToken))
        {
            return UserErrors.EmailAlreadyInUse;
        }

        user.UpdateProfile(name, email, phone, command.BirthDate!.Value);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return UserErrors.Duplicate;
        }

        LogUserUpdated(logger, user.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Usuario {UserId} atualizado.")]
    private static partial void LogUserUpdated(ILogger logger, Guid userId);
}
