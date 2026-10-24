using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Contracts;

[ExcludeFromCodeCoverage]
[Description("Dados para criacao de uma permissao (recurso + acao).")]
public sealed record CreatePermissionRequest(
    [property: Description(
        "Recurso (ex.: users). Regex ^[a-z][a-z0-9_-]{1,49}$: inicia com letra minuscula, 2 a 50 caracteres.")]
    string? Resource,
    [property: Description(
        "Acao (ex.: read). Regex ^[a-z][a-z0-9_-]{1,49}$: inicia com letra minuscula, 2 a 50 caracteres.")]
    string? Action,
    [property: Description("Descricao opcional (ate 500 caracteres).")] string? Description);

[ExcludeFromCodeCoverage]
public sealed record ListPermissionsRequest(
    [property: FromQuery, Description("Pagina (inicia em 1). Padrao: 1.")] int? Page,
    [property: FromQuery, Description("Itens por pagina (1 a 100). Padrao: 20.")] int? PageSize,
    [property: FromQuery, Description("Filtro opcional por recurso (igualdade exata).")] string? Resource,
    [property: FromQuery, Description("Filtro opcional por acao (igualdade exata).")] string? Action);
