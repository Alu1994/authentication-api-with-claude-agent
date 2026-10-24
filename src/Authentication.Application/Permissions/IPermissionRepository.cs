using Authentication.Application.Abstractions.Paging;

namespace Authentication.Application.Permissions;

public interface IPermissionRepository
{
    /// <summary>Busca uma permissao ativa (nao excluida) pelo Id.</summary>
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe uma permissao ativa com o mesmo recurso e acao.</summary>
    Task<bool> ExistsAsync(string resource, string action, CancellationToken cancellationToken);

    /// <summary>
    /// Lista permissoes ativas, ordenadas por recurso e acao, com filtros opcionais por igualdade exata.
    /// </summary>
    Task<PagedResponse<Permission>> ListAsync(
        int page,
        int pageSize,
        string? resource,
        string? action,
        CancellationToken cancellationToken);

    void Add(Permission permission);

    /// <summary>Persiste as alteracoes.</summary>
    /// <exception cref="Abstractions.Persistence.UniqueConstraintViolationException">
    /// Quando o par recurso/acao ja existe entre as permissoes ativas.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
