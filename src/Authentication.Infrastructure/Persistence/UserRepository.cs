using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

internal sealed class UserRepository(AuthenticationDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? ignoreUserId, CancellationToken cancellationToken) =>
        context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && (ignoreUserId == null || u.Id != ignoreUserId), cancellationToken);

    public Task<bool> CpfExistsAsync(string cpf, CancellationToken cancellationToken) =>
        context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Cpf == cpf, cancellationToken);

    public async Task<PagedResponse<User>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePatterns.Contains(search);
            query = query.Where(u =>
                EF.Functions.ILike(u.Name, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.ILike(u.Email, pattern, LikePatterns.EscapeCharacter));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<User>(items, page, pageSize, totalCount);
    }

    public void Add(User user) => context.Users.Add(user);

    public Task<UserRole?> GetRoleLinkAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
        context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<UserRole>> GetRoleLinksAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);

    public void AddRoleLink(UserRole link) => context.UserRoles.Add(link);

    public void RemoveRoleLink(UserRole link) => context.UserRoles.Remove(link);

    // context.Roles/Permissions aplicam o filtro de soft delete: so itens ativos entram.
    public async Task<IReadOnlyList<Role>> ListActiveRolesAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public async Task<UserAuthorization> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => new { r.Id, r.Name })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            return UserAuthorization.None;
        }

        var roleIds = roles.Select(r => r.Id).ToList();
        var permissions = await context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => new { p.Resource, p.Action })
            .Distinct()
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);

        return new UserAuthorization(
            roles.Select(r => r.Name).ToList(),
            permissions.Select(p => $"{p.Resource}{Permission.NameSeparator}{p.Action}").ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesTranslatingUniqueViolationAsync(cancellationToken);
}
