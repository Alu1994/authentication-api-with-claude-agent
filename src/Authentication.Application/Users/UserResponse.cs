using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Application.Users;

/// <summary>Representacao publica do usuario. Nunca expoe a senha nem o hash.</summary>
[ExcludeFromCodeCoverage]
public sealed record UserResponse(
    [property: Description("Identificador do usuario.")] Guid Id,
    [property: Description("Nome completo.")] string Name,
    [property: Description("E-mail (normalizado em minusculas).")] string Email,
    [property: Description("CPF, apenas digitos.")] string Cpf,
    [property: Description("Telefone com DDD, apenas digitos.")] string Phone,
    [property: Description("Data de nascimento (yyyy-MM-dd).")] DateOnly BirthDate,
    [property: Description("Data/hora de cadastro (UTC).")] DateTimeOffset CreatedAt,
    [property: Description("Data/hora da ultima alteracao (UTC); nulo se o usuario nunca foi alterado.")]
    DateTimeOffset? UpdatedAt)
{
    public static UserResponse FromUser(User user) =>
        new(user.Id, user.Name, user.Email, user.Cpf, user.Phone, user.BirthDate, user.CreatedAt, user.UpdatedAt);
}
