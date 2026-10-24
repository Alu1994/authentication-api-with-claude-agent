using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.Create;

public sealed partial class CreateUserCommandHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<CreateUserCommandHandler> logger)
    : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var name = UserRules.NormalizeName(command.Name);
        var email = UserRules.NormalizeEmail(command.Email);
        var cpf = Cpf.Normalize(command.Cpf);
        var phone = UserRules.NormalizePhone(command.Phone);

        var errors = new ValidationErrorCollector()
            .ValidateProfile(name, email, phone, command.BirthDate, DateOnly.FromDateTime(now.UtcDateTime))
            .AddIf(!Cpf.IsValid(cpf), "cpf", "Informe um CPF valido.")
            .AddRange("password", PasswordPolicy.Validate(command.Password));

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        if (await repository.EmailExistsAsync(email, null, cancellationToken))
        {
            LogDuplicateRejected(logger, "email");
            return UserErrors.EmailAlreadyInUse;
        }

        if (await repository.CpfExistsAsync(cpf, cancellationToken))
        {
            LogDuplicateRejected(logger, "cpf");
            return UserErrors.CpfAlreadyInUse;
        }

        var user = User.Create(
            name,
            email,
            cpf,
            phone,
            command.BirthDate!.Value,
            passwordHasher.Hash(command.Password!),
            now);

        repository.Add(user);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ex)
        {
            LogDuplicateRejected(logger, ex.ConstraintName ?? "unknown");
            return UserErrors.Duplicate;
        }

        LogUserCreated(logger, user.Id);
        return UserResponse.FromUser(user);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Usuario {UserId} cadastrado.")]
    private static partial void LogUserCreated(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Cadastro de usuario rejeitado por duplicidade ({Field}).")]
    private static partial void LogDuplicateRejected(ILogger logger, string field);
}
