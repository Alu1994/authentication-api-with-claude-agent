using System.Text.RegularExpressions;

namespace Authentication.Application.Users.Validation;

/// <summary>
/// Regras de normalizacao e validacao dos campos cadastrais do usuario.
/// </summary>
public static partial class UserRules
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 150;
    public const int EmailMaxLength = 254;
    public const int PhoneMinLength = 10;
    public const int PhoneMaxLength = 11;
    public const int MaxAgeInYears = 120;

    public static string NormalizeName(string? name) => name?.Trim() ?? string.Empty;

    public static bool IsValidName(string name) =>
        name.Length is >= NameMinLength and <= NameMaxLength;

    public static string NormalizeEmail(string? email) =>
        email?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool IsValidEmail(string email) =>
        email.Length <= EmailMaxLength && EmailRegex().IsMatch(email);

    /// <summary>Mantem apenas os digitos do telefone (DDD + numero).</summary>
    public static string NormalizePhone(string? phone) => Cpf.Normalize(phone);

    /// <summary>Telefone brasileiro: DDD (sem zero a esquerda) + 8 ou 9 digitos.</summary>
    public static bool IsValidPhone(string phone) =>
        phone.Length is >= PhoneMinLength and <= PhoneMaxLength
        && phone.All(char.IsAsciiDigit)
        && phone[0] != '0';

    public static bool IsValidBirthDate(DateOnly? birthDate, DateOnly today) =>
        birthDate is { } date
        && date < today
        && date >= today.AddYears(-MaxAgeInYears);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
