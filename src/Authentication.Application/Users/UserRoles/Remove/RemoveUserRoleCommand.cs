namespace Authentication.Application.Users.UserRoles.Remove;

/// <summary>Remove a atribuicao de uma role a um usuario.</summary>
public sealed record RemoveUserRoleCommand(Guid UserId, Guid RoleId);
