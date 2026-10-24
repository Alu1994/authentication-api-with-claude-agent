using Authentication.Application.Abstractions.Paging;

namespace Authentication.Application.Users.List;

/// <summary>Listagem paginada de usuarios ativos, com busca opcional por nome ou e-mail.</summary>
public sealed record ListUsersQuery(
    int Page = ListUsersQuery.DefaultPage,
    int PageSize = ListUsersQuery.DefaultPageSize,
    string? Search = null)
{
    public const int DefaultPage = Paging.DefaultPage;
    public const int DefaultPageSize = Paging.DefaultPageSize;
    public const int MaxPageSize = Paging.MaxPageSize;
}
