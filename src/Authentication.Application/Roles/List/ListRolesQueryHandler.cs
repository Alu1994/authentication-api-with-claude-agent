using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Roles.List;

public sealed class ListRolesQueryHandler(IRoleRepository repository)
    : IQueryHandler<ListRolesQuery, Result<PagedResponse<RoleResponse>>>
{
    public async Task<Result<PagedResponse<RoleResponse>>> HandleAsync(
        ListRolesQuery query,
        CancellationToken cancellationToken)
    {
        var errors = new ValidationErrorCollector().ValidatePaging(query.Page, query.PageSize);

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var name = string.IsNullOrWhiteSpace(query.Name) ? null : query.Name.Trim();
        var page = await repository.ListAsync(query.Page, query.PageSize, name, cancellationToken);

        return new PagedResponse<RoleResponse>(
            page.Items.Select(RoleResponse.FromRole).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
