namespace Authentication.Application.Users.UserRoles.Add;

/// <summary>Atribui uma role a um usuario (idempotente).</summary>
public sealed record AddUserRoleCommand(Guid UserId, Guid RoleId);
