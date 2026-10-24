namespace Authentication.Application.Abstractions.Validation;

/// <summary>Regras da descricao opcional usada por roles e permissoes.</summary>
public static class DescriptionRules
{
    public const int MaxLength = 500;

    public const string TooLongMessage = "A descricao deve ter no maximo 500 caracteres.";

    /// <summary>Remove espacos das pontas; descricao em branco vira nula.</summary>
    public static string? Normalize(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public static bool IsValid(string? description) => description is null || description.Length <= MaxLength;
}
