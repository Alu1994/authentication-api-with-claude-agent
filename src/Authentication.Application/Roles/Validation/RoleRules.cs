namespace Authentication.Application.Roles.Validation;

/// <summary>Regras de normalizacao e validacao de roles.</summary>
public static class RoleRules
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;

    public const string NameLengthMessage = "O nome deve ter entre 2 e 100 caracteres.";

    /// <summary>Remove espacos das pontas. O nome e texto livre (aceita espacos e acentos).</summary>
    public static string NormalizeName(string? name) => name?.Trim() ?? string.Empty;

    public static bool IsValidName(string name) => name.Length is >= NameMinLength and <= NameMaxLength;

    /// <summary>Forma usada para unicidade e comparacao sem diferenciar maiusculas/minusculas.</summary>
    public static string NormalizeForComparison(string name) => name.ToLowerInvariant();
}
