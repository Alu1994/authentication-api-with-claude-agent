using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Roles;

namespace Authentication.Application.Users;

public interface IUserRepository
{
    /// <summary>Busca um usuario ativo (nao excluido) pelo Id.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Busca um usuario ativo (nao excluido) pelo e-mail normalizado.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se o e-mail ja esta em uso por qualquer usuario, inclusive excluidos.
    /// </summary>
    /// <param name="email">E-mail normalizado.</param>
    /// <param name="ignoreUserId">Usuario a desconsiderar (o proprio usuario em uma atualizacao).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<bool> EmailExistsAsync(string email, Guid? ignoreUserId, CancellationToken cancellationToken);

    /// <summary>Indica se o CPF ja esta em uso por qualquer usuario, inclusive excluidos.</summary>
    Task<bool> CpfExistsAsync(string cpf, CancellationToken cancellationToken);

    /// <summary>Lista usuarios ativos, ordenados por nome, com filtro opcional por nome ou e-mail.</summary>
    Task<PagedResponse<User>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);

    void Add(User user);

    /// <summary>Busca o vinculo entre o usuario e a role, independentemente de a role estar ativa.</summary>
    Task<UserRole?> GetRoleLinkAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);

    /// <summary>Todos os vinculos do usuario (inclusive com roles excluidas).</summary>
    Task<IReadOnlyList<UserRole>> GetRoleLinksAsync(Guid userId, CancellationToken cancellationToken);

    void AddRoleLink(UserRole link);

    void RemoveRoleLink(UserRole link);

    /// <summary>Roles ativas atribuidas ao usuario, ordenadas por nome.</summary>
    Task<IReadOnlyList<Role>> ListActiveRolesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Nomes das roles ativas do usuario e permissoes ativas distintas (<c>resource:action</c>) dessas roles,
    /// ambos ordenados. Usado para emitir as claims de autorizacao no login.
    /// </summary>
    Task<UserAuthorization> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Persiste as alteracoes.</summary>
    /// <exception cref="Abstractions.Persistence.UniqueConstraintViolationException">
    /// Quando uma restricao de unicidade (e-mail/CPF) e violada.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
