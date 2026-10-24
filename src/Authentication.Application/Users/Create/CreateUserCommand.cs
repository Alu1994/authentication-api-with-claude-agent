namespace Authentication.Application.Users.Create;

/// <summary>Cadastro de um novo usuario. Os campos sao normalizados e validados pelo handler.</summary>
public sealed record CreateUserCommand(
    string? Name,
    string? Email,
    string? Cpf,
    string? Phone,
    DateOnly? BirthDate,
    string? Password);
