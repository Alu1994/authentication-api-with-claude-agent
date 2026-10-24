using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Application.Roles;

/// <summary>Representacao publica de uma role.</summary>
[ExcludeFromCodeCoverage]
public sealed record RoleResponse(
    [property: Description("Identificador da role.")] Guid Id,
    [property: Description("Nome da role (unico entre as ativas, sem diferenciar maiusculas/minusculas).")] string Name,
    [property: Description("Descricao opcional.")] string? Description,
    [property: Description("Data/hora de criacao (UTC).")] DateTimeOffset CreatedAt,
    [property: Description("Data/hora da ultima alteracao (UTC); nulo se nunca alterada.")] DateTimeOffset? UpdatedAt)
{
    public static RoleResponse FromRole(Role role) =>
        new(role.Id, role.Name, role.Description, role.CreatedAt, role.UpdatedAt);
}
