using System.Text.RegularExpressions;

namespace Authentication.Application.Permissions.Validation;

/// <summary>Regras de normalizacao e validacao de permissoes.</summary>
public static partial class PermissionRules
{
    public const int SegmentMaxLength = 50;
    public const int DescriptionMaxLength = 500;

    public const string SegmentFormatMessage =
        "Deve comecar com letra minuscula e conter de 2 a 50 caracteres entre letras minusculas, digitos, '_' e '-'.";

    /// <summary>Remove espacos das pontas de <c>resource</c>/<c>action</c>. Nao converte para minusculas.</summary>
    public static string NormalizeSegment(string? value) => value?.Trim() ?? string.Empty;

    /// <summary>Valida <c>resource</c>/<c>action</c> contra <c>^[a-z][a-z0-9_-]{1,49}$</c>.</summary>
    public static bool IsValidSegment(string value) => SegmentRegex().IsMatch(value);

    /// <summary>Remove espacos das pontas; descricao em branco vira nula.</summary>
    public static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public static bool IsValidDescription(string? description) =>
        description is null || description.Length <= DescriptionMaxLength;

    // \z (e nao $) para nao aceitar uma quebra de linha final.
    [GeneratedRegex(@"^[a-z][a-z0-9_-]{1,49}\z", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentRegex();
}
