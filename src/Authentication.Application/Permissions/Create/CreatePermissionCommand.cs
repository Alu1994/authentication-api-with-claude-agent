namespace Authentication.Application.Permissions.Create;

/// <summary>Criacao de uma permissao. Os campos sao normalizados e validados pelo handler.</summary>
public sealed record CreatePermissionCommand(string? Resource, string? Action, string? Description);
