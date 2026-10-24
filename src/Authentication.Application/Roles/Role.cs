using Authentication.Application.Roles.Validation;
using static Authentication.Application.Abstractions.Time.Timestamps;

namespace Authentication.Application.Roles;

/// <summary>
/// Agrupamento nomeado de permissoes atribuivel a usuarios. Os dados recebidos aqui ja devem
/// estar normalizados e validados pelos handlers.
/// </summary>
public sealed class Role
{
    // Construtor usado pelo EF Core na materializacao.
    private Role()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>Nome em minusculas, usado para garantir unicidade sem diferenciar maiusculas/minusculas.</summary>
    public string NormalizedName { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora da ultima alteracao (edicao ou exclusao). Nulo enquanto nunca alterada.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static Role Create(string name, string? description, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(createdAt),
            Name = name,
            NormalizedName = RoleRules.NormalizeForComparison(name),
            Description = description,
            CreatedAt = TruncateToMicroseconds(createdAt),
        };

    public void Update(string name, string? description, DateTimeOffset updatedAt)
    {
        Name = name;
        NormalizedName = RoleRules.NormalizeForComparison(name);
        Description = description;
        UpdatedAt = TruncateToMicroseconds(updatedAt);
    }

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
