using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Users.Validation;

/// <summary>Validacoes compartilhadas entre cadastro e atualizacao de usuario.</summary>
internal static class UserProfileValidation
{
    public static ValidationErrorCollector ValidateProfile(
        this ValidationErrorCollector errors,
        string name,
        string email,
        string phone,
        DateOnly? birthDate,
        DateOnly today) =>
        errors
            .AddIf(!UserRules.IsValidName(name), "name",
                $"O nome e obrigatorio e deve ter entre {UserRules.NameMinLength} e {UserRules.NameMaxLength} caracteres.")
            .AddIf(!UserRules.IsValidEmail(email), "email", "Informe um e-mail valido.")
            .AddIf(!UserRules.IsValidPhone(phone), "phone",
                "Informe um telefone valido com DDD (10 ou 11 digitos).")
            .AddIf(!UserRules.IsValidBirthDate(birthDate, today), "birthDate",
                $"Informe uma data de nascimento anterior a hoje e de no maximo {UserRules.MaxAgeInYears} anos atras.");
}
