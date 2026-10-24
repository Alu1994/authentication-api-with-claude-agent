using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

internal sealed class PermissionRepository(AuthenticationDbContext context) : IPermissionRepository
{
    public Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Permissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(string resource, string action, CancellationToken cancellationToken) =>
        context.Permissions.AnyAsync(p => p.Resource == resource && p.Action == action, cancellationToken);

    public async Task<PagedResponse<Permission>> ListAsync(
        int page,
        int pageSize,
        string? resource,
        string? action,
        CancellationToken cancellationToken)
    {
        var query = context.Permissions.AsNoTracking();

        if (resource is not null)
        {
            query = query.Where(p => p.Resource == resource);
        }

        if (action is not null)
        {
            query = query.Where(p => p.Action == action);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Permission>(items, page, pageSize, totalCount);
    }

    public void Add(Permission permission) => context.Permissions.Add(permission);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesTranslatingUniqueViolationAsync(cancellationToken);
}
