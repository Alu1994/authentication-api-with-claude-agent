using static Authentication.Application.Abstractions.Time.Timestamps;

namespace Authentication.Application.Permissions;

/// <summary>
/// Permissao no formato recurso + acao (ex.: <c>users:read</c>). Imutavel apos criada:
/// so pode ser criada e excluida. Os dados recebidos aqui ja devem estar normalizados e validados.
/// </summary>
public sealed class Permission
{
    /// <summary>Separador entre recurso e acao na representacao externa (claims e contratos).</summary>
    public const char NameSeparator = ':';

    // Construtor usado pelo EF Core na materializacao.
    private Permission()
    {
        Resource = string.Empty;
        Action = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Resource { get; private set; }

    public string Action { get; private set; }

    /// <summary>Representacao externa: <c>resource:action</c>.</summary>
    public string Name => $"{Resource}{NameSeparator}{Action}";

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora da ultima alteracao (hoje, apenas a exclusao). Nulo enquanto nunca alterada.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static Permission Create(string resource, string action, string? description, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(createdAt),
            Resource = resource,
            Action = action,
            Description = description,
            CreatedAt = TruncateToMicroseconds(createdAt),
        };

    /// <summary>
    /// Exclusao logica. Tambem registra a exclusao como ultima alteracao (<see cref="UpdatedAt"/>).
    /// Chamadas repetidas preservam as datas da primeira exclusao.
    /// </summary>
    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = TruncateToMicroseconds(deletedAt);
        UpdatedAt = DeletedAt;
    }
}
