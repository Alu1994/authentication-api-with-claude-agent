using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Roles;

public static class RoleErrors
{
    public static readonly Error NotFound =
        Error.NotFound("role.not_found", "Role nao encontrada.");

    public static readonly Error NameInUse =
        Error.Conflict("role.name_in_use", "Ja existe uma role ativa com este nome.");

    public static readonly Error PermissionNotLinked =
        Error.NotFound("role.permission_not_linked", "A permissao nao esta vinculada a esta role.");
}
