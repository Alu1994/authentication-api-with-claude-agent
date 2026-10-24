using Authentication.Application.Abstractions.Paging;
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
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Name, pattern, LikeEscapeCharacter)
                || EF.Functions.ILike(u.Email, pattern, LikeEscapeCharacter));
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

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesTranslatingUniqueViolationAsync(cancellationToken);

    private const string LikeEscapeCharacter = @"\";

    // Trata %, _ e \ digitados na busca como literais.
    private static string EscapeLikePattern(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
