using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Permissions;

public static class PermissionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("permission.not_found", "Permissao nao encontrada.");

    public static readonly Error AlreadyExists =
        Error.Conflict("permission.already_exists", "Ja existe uma permissao ativa com este recurso e acao.");
}
