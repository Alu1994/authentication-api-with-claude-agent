namespace Authentication.Application.Abstractions.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

/// <summary>
/// Erro de negocio esperado. Erros de validacao trazem o detalhamento por campo em <see cref="ValidationErrors"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; private init; } = NoValidationErrors;

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new("validation", "Um ou mais campos sao invalidos.", ErrorType.Validation) { ValidationErrors = errors };

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
}
