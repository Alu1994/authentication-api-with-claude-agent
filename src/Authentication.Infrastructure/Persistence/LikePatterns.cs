namespace Authentication.Infrastructure.Persistence;

/// <summary>Montagem de padroes LIKE/ILIKE tratando %, _ e \ digitados pelo usuario como literais.</summary>
internal static class LikePatterns
{
    public const string EscapeCharacter = @"\";

    /// <summary>Padrao "contem" (<c>%valor%</c>) com os curingas do valor escapados.</summary>
    public static string Contains(string value) => $"%{Escape(value)}%";

    private static string Escape(string value) =>
        value.Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);
}
