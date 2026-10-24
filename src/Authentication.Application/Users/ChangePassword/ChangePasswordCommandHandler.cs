using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.ChangePassword;

public sealed partial class ChangePasswordCommandHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<ChangePasswordCommandHandler> logger)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrorCollector()
            .AddIf(string.IsNullOrEmpty(command.CurrentPassword), "currentPassword", "A senha atual e obrigatoria.")
            .AddRange("newPassword", PasswordPolicy.Validate(command.NewPassword))
            .AddIf(
                !string.IsNullOrEmpty(command.NewPassword) && command.NewPassword == command.CurrentPassword,
                "newPassword",
                "A nova senha deve ser diferente da senha atual.");

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var user = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (passwordHasher.Verify(user.PasswordHash, command.CurrentPassword!) == PasswordVerification.Failed)
        {
            LogInvalidCurrentPassword(logger, user.Id);
            return new ValidationErrorCollector()
                .AddIf(true, "currentPassword", "A senha atual esta incorreta.")
                .ToError();
        }

        user.ChangePasswordHash(passwordHasher.Hash(command.NewPassword!), timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        LogPasswordChanged(logger, user.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Senha do usuario {UserId} alterada.")]
    private static partial void LogPasswordChanged(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Warning,
        Message = "Troca de senha do usuario {UserId} rejeitada: senha atual incorreta.")]
    private static partial void LogInvalidCurrentPassword(ILogger logger, Guid userId);
}
