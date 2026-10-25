using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Users;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("user.not_found", "Usuario nao encontrado.");

    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("user.email_in_use", "Ja existe um usuario cadastrado com este e-mail.");

    public static readonly Error CpfAlreadyInUse =
        Error.Conflict("user.cpf_in_use", "Ja existe um usuario cadastrado com este CPF.");

    public static readonly Error RoleNotLinked =
        Error.NotFound("user.role_not_linked", "A role nao esta atribuida a este usuario.");

    public static readonly Error Duplicate =
        Error.Conflict("user.duplicate", "Ja existe um usuario cadastrado com este e-mail ou CPF.");
}
