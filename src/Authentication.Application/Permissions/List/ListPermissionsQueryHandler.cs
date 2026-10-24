using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Permissions.List;

public sealed class ListPermissionsQueryHandler(IPermissionRepository repository)
    : IQueryHandler<ListPermissionsQuery, Result<PagedResponse<PermissionResponse>>>
{
    public async Task<Result<PagedResponse<PermissionResponse>>> HandleAsync(
        ListPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        var errors = new ValidationErrorCollector().ValidatePaging(query.Page, query.PageSize);

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var page = await repository.ListAsync(
            query.Page,
            query.PageSize,
            NormalizeFilter(query.Resource),
            NormalizeFilter(query.Action),
            cancellationToken);

        return new PagedResponse<PermissionResponse>(
            page.Items.Select(PermissionResponse.FromPermission).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    private static string? NormalizeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
