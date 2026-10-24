using static Authentication.Application.Abstractions.Time.Timestamps;

namespace Authentication.Application.Users;

/// <summary>
/// Atribuicao de uma role a um usuario. Removida fisicamente ao desvincular (sem soft delete)
/// e nao altera o <c>updatedAt</c> do usuario.
/// </summary>
public sealed class UserRole
{
    private UserRole()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId, DateTimeOffset createdAt) =>
        new()
        {
            UserId = userId,
            RoleId = roleId,
            CreatedAt = TruncateToMicroseconds(createdAt),
        };
}
