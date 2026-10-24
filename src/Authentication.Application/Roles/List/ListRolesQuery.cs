using Authentication.Application.Abstractions.Paging;

namespace Authentication.Application.Roles.List;

/// <summary>Listagem paginada de roles ativas, com filtro opcional por trecho do nome (case-insensitive).</summary>
public sealed record ListRolesQuery(
    int Page = Paging.DefaultPage,
    int PageSize = Paging.DefaultPageSize,
    string? Name = null);
