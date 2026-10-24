namespace Authentication.Application.Roles.Update;

/// <summary>Edicao de nome e descricao de uma role (descricao ausente/em branco remove a descricao).</summary>
public sealed record UpdateRoleCommand(Guid Id, string? Name, string? Description);
