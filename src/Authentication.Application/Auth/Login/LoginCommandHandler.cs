using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Authentication.Application.Users.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Auth.Login;

public sealed partial class LoginCommandHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<LoginCommandHandler> logger)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public const string TokenType = "Bearer";

    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = UserRules.NormalizeEmail(command.Email);

        var errors = new ValidationErrorCollector()
            .AddIf(email.Length == 0, "email", "O e-mail e obrigatorio.")
            .AddIf(string.IsNullOrEmpty(command.Password), "password", "A senha e obrigatoria.");

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        // Usuarios excluidos (soft delete) nao sao retornados pelo repositorio e, portanto, nao autenticam.
        var user = await repository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Mesmo custo de CPU do caminho "senha incorreta", evitando enumeracao de contas por timing.
            passwordHasher.SimulateVerification(command.Password!);
            LogLoginFailed(logger);
            return AuthErrors.InvalidCredentials;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, command.Password!);
        if (verification == PasswordVerification.Failed)
        {
            LogLoginFailed(logger);
            return AuthErrors.InvalidCredentials;
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.UpgradePasswordHash(passwordHasher.Hash(command.Password!));
            await repository.SaveChangesAsync(cancellationToken);
        }

        // Roles/permissoes refletem o estado no momento do login; mudancas valem a partir do proximo login.
        var authorization = await repository.GetAuthorizationAsync(user.Id, cancellationToken);
        var token = tokenService.CreateToken(user, authorization);
        LogLoginSucceeded(logger, user.Id);

        return new LoginResponse(token.Value, TokenType, token.ExpiresAt);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Usuario {UserId} autenticado.")]
    private static partial void LogLoginSucceeded(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Tentativa de login com credenciais invalidas.")]
    private static partial void LogLoginFailed(ILogger logger);
}
