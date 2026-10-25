namespace Authentication.Application.Users.UserRoles.List;

/// <summary>Roles ativas atribuidas a um usuario (sem paginacao).</summary>
public sealed record ListUserRolesQuery(Guid UserId);
