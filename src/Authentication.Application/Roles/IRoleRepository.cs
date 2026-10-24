using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Permissions;

namespace Authentication.Application.Roles;

public interface IRoleRepository
{
    /// <summary>Busca uma role ativa (nao excluida) pelo Id.</summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Indica se o nome (ja normalizado para comparacao) esta em uso por outra role ativa.</summary>
    /// <param name="normalizedName">Nome em minusculas.</param>
    /// <param name="ignoreRoleId">Role a desconsiderar (a propria role em uma atualizacao).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<bool> NameExistsAsync(string normalizedName, Guid? ignoreRoleId, CancellationToken cancellationToken);

    /// <summary>Lista roles ativas, ordenadas por nome, com filtro opcional por trecho do nome (case-insensitive).</summary>
    Task<PagedResponse<Role>> ListAsync(int page, int pageSize, string? name, CancellationToken cancellationToken);

    void Add(Role role);

    /// <summary>Busca o vinculo entre a role e a permissao, independentemente de a permissao estar ativa.</summary>
    Task<RolePermission?> GetPermissionLinkAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken);

    /// <summary>Todos os vinculos da role (inclusive com permissoes excluidas).</summary>
    Task<IReadOnlyList<RolePermission>> GetPermissionLinksAsync(Guid roleId, CancellationToken cancellationToken);

    void AddPermissionLink(RolePermission link);

    void RemovePermissionLink(RolePermission link);

    /// <summary>Permissoes ativas vinculadas a role, ordenadas por recurso e acao.</summary>
    Task<IReadOnlyList<Permission>> ListActivePermissionsAsync(Guid roleId, CancellationToken cancellationToken);

    /// <summary>Persiste as alteracoes.</summary>
    /// <exception cref="Abstractions.Persistence.UniqueConstraintViolationException">
    /// Quando o nome ja existe entre as roles ativas ou o vinculo ja existe.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
