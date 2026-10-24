namespace Authentication.Application.Users.List;

/// <summary>Listagem paginada de usuarios ativos, com busca opcional por nome ou e-mail.</summary>
public sealed record ListUsersQuery(
    int Page = ListUsersQuery.DefaultPage,
    int PageSize = ListUsersQuery.DefaultPageSize,
    string? Search = null)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
