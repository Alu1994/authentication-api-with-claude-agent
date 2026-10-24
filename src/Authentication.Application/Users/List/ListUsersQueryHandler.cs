using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Users.List;

public sealed class ListUsersQueryHandler(IUserRepository repository)
    : IQueryHandler<ListUsersQuery, Result<PagedResponse<UserResponse>>>
{
    public async Task<Result<PagedResponse<UserResponse>>> HandleAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var errors = new ValidationErrorCollector()
            .AddIf(query.Page < 1, "page", "A pagina deve ser maior ou igual a 1.")
            .AddIf(query.PageSize is < 1 or > ListUsersQuery.MaxPageSize, "pageSize",
                $"O tamanho da pagina deve estar entre 1 e {ListUsersQuery.MaxPageSize}.");

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var page = await repository.ListAsync(query.Page, query.PageSize, search, cancellationToken);

        return new PagedResponse<UserResponse>(
            page.Items.Select(UserResponse.FromUser).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
