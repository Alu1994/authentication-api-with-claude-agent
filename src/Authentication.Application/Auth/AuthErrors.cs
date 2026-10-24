using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Auth;

public static class AuthErrors
{
    /// <summary>
    /// Mensagem unica para usuario inexistente, excluido ou senha incorreta,
    /// evitando enumeracao de e-mails cadastrados.
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "E-mail ou senha invalidos.");
}
