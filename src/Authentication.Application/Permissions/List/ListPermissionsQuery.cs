using Authentication.Application.Abstractions.Paging;

namespace Authentication.Application.Permissions.List;

/// <summary>Listagem paginada de permissoes ativas, com filtros opcionais (igualdade exata) por recurso e acao.</summary>
public sealed record ListPermissionsQuery(
    int Page = Paging.DefaultPage,
    int PageSize = Paging.DefaultPageSize,
    string? Resource = null,
    string? Action = null);
