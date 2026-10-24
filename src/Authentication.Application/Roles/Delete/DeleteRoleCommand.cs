namespace Authentication.Application.Roles.Delete;

/// <summary>Exclusao logica (soft delete) de uma role. Os vinculos com permissoes sao mantidos, sem efeito.</summary>
public sealed record DeleteRoleCommand(Guid Id);
