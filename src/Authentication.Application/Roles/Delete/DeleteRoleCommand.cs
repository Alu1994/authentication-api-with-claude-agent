namespace Authentication.Application.Roles.Delete;

/// <summary>
/// Exclusao logica (soft delete) de uma role. Bloqueada enquanto houver usuarios ativos com a role;
/// os vinculos com permissoes sao mantidos, sem efeito.
/// </summary>
public sealed record DeleteRoleCommand(Guid Id);
