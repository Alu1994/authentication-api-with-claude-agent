namespace Authentication.Application.Users;

/// <summary>
/// Usuario cadastrado no servico de identidade. Os dados recebidos aqui ja devem estar
/// normalizados e validados pelos handlers.
/// </summary>
/// <remarks>
/// Relacionamentos futuros (ex.: Roles/Permissoes) devem ser adicionados em tabelas proprias
/// referenciando <see cref="Id"/>, sem alterar a estrutura atual.
/// </remarks>
public sealed class User
{
    // Construtor usado pelo EF Core na materializacao.
    private User()
    {
        Name = string.Empty;
        Email = string.Empty;
        Cpf = string.Empty;
        Phone = string.Empty;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    /// <summary>CPF (apenas digitos). Imutavel apos o cadastro.</summary>
    public string Cpf { get; private set; }

    public string Phone { get; private set; }

    public DateOnly BirthDate { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static User Create(
        string name,
        string email,
        string cpf,
        string phone,
        DateOnly birthDate,
        string passwordHash,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(createdAt),
            Name = name,
            Email = email,
            Cpf = cpf,
            Phone = phone,
            BirthDate = birthDate,
            PasswordHash = passwordHash,
            CreatedAt = TruncateToMicroseconds(createdAt),
        };

    public void UpdateProfile(string name, string email, string phone, DateOnly birthDate)
    {
        Name = name;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
    }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>Exclusao logica. Chamadas repetidas preservam a data da primeira exclusao.</summary>
    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = TruncateToMicroseconds(deletedAt);
    }

    // Alinha a precisao a do PostgreSQL (microssegundos), garantindo que o valor retornado
    // logo apos a escrita seja identico ao lido do banco posteriormente.
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
