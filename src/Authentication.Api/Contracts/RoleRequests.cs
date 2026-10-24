using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Contracts;

[ExcludeFromCodeCoverage]
[Description("Dados para criacao de uma role.")]
public sealed record CreateRoleRequest(
    [property: Description(
        "Nome (2 a 100 caracteres, texto livre). Unico entre as roles ativas, sem diferenciar maiusculas/minusculas.")]
    string? Name,
    [property: Description("Descricao opcional (ate 500 caracteres).")] string? Description);

[ExcludeFromCodeCoverage]
[Description("Dados editaveis de uma role. Descricao ausente ou em branco remove a descricao atual.")]
public sealed record UpdateRoleRequest(
    [property: Description("Nome (2 a 100 caracteres). Unico entre as roles ativas.")] string? Name,
    [property: Description("Descricao opcional (ate 500 caracteres).")] string? Description);

[ExcludeFromCodeCoverage]
public sealed record ListRolesRequest(
    [property: FromQuery, Description("Pagina (inicia em 1). Padrao: 1.")] int? Page,
    [property: FromQuery, Description("Itens por pagina (1 a 100). Padrao: 20.")] int? PageSize,
    [property: FromQuery, Description("Filtro opcional por trecho do nome (case-insensitive).")] string? Name);

[ExcludeFromCodeCoverage]
[Description("Lista completa de permissoes da role (substitui a atual).")]
public sealed record ReplaceRolePermissionsRequest(
    [property: Description(
        "Ids das permissoes. Lista vazia remove todos os vinculos; Ids repetidos sao ignorados. " +
        "Se algum Id nao existir ou estiver excluido, nada e alterado (400).")]
    IReadOnlyList<Guid>? PermissionIds);
