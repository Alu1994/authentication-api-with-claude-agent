using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

internal sealed class RoleRepository(AuthenticationDbContext context) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string normalizedName, Guid? ignoreRoleId, CancellationToken cancellationToken) =>
        context.Roles.AnyAsync(
            r => r.NormalizedName == normalizedName && (ignoreRoleId == null || r.Id != ignoreRoleId),
            cancellationToken);

    public async Task<PagedResponse<Role>> ListAsync(
        int page,
        int pageSize,
        string? name,
        CancellationToken cancellationToken)
    {
        var query = context.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var pattern = LikePatterns.Contains(name);
            query = query.Where(r => EF.Functions.ILike(r.Name, pattern, LikePatterns.EscapeCharacter));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Role>(items, page, pageSize, totalCount);
    }

    public void Add(Role role) => context.Roles.Add(role);

    public Task<RolePermission?> GetPermissionLinkAsync(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken) =>
        context.RolePermissions.FirstOrDefaultAsync(
            rp => rp.RoleId == roleId && rp.PermissionId == permissionId,
            cancellationToken);

    public async Task<IReadOnlyList<RolePermission>> GetPermissionLinksAsync(
        Guid roleId,
        CancellationToken cancellationToken) =>
        await context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);

    public void AddPermissionLink(RolePermission link) => context.RolePermissions.Add(link);

    public void RemovePermissionLink(RolePermission link) => context.RolePermissions.Remove(link);

    public async Task<IReadOnlyList<Permission>> ListActivePermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken) =>
        await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p)
            .AsNoTracking()
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesTranslatingUniqueViolationAsync(cancellationToken);
}
