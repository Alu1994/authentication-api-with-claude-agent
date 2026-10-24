namespace Authentication.Application.Users.Update;

/// <summary>Atualizacao cadastral. O CPF e imutavel e nao faz parte deste comando.</summary>
public sealed record UpdateUserCommand(
    Guid Id,
    string? Name,
    string? Email,
    string? Phone,
    DateOnly? BirthDate);
