namespace Authentication.Application.Permissions.Delete;

/// <summary>Exclusao logica (soft delete) de uma permissao.</summary>
public sealed record DeletePermissionCommand(Guid Id);
