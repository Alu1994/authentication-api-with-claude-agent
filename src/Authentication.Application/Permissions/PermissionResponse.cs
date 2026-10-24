using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Application.Permissions;

/// <summary>Representacao publica de uma permissao.</summary>
[ExcludeFromCodeCoverage]
public sealed record PermissionResponse(
    [property: Description("Identificador da permissao.")] Guid Id,
    [property: Description("Recurso (ex.: users).")] string Resource,
    [property: Description("Acao (ex.: read).")] string Action,
    [property: Description("Representacao externa no formato resource:action (ex.: users:read), usada nas claims.")]
    string Name,
    [property: Description("Descricao opcional.")] string? Description,
    [property: Description("Data/hora de criacao (UTC).")] DateTimeOffset CreatedAt,
    [property: Description("Data/hora da ultima alteracao (UTC); nulo se nunca alterada.")] DateTimeOffset? UpdatedAt)
{
    public static PermissionResponse FromPermission(Permission permission) =>
        new(
            permission.Id,
            permission.Resource,
            permission.Action,
            permission.Name,
            permission.Description,
            permission.CreatedAt,
            permission.UpdatedAt);
}
