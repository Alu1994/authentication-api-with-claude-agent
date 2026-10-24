namespace Authentication.Application.Roles.Create;

/// <summary>Criacao de uma role. Os campos sao normalizados e validados pelo handler.</summary>
public sealed record CreateRoleCommand(string? Name, string? Description);
