namespace Authentication.Application.Abstractions.Results;

/// <summary>
/// Acumula mensagens de validacao por campo (nomes em camelCase, iguais ao contrato JSON).
/// </summary>
internal sealed class ValidationErrorCollector
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrorCollector AddIf(bool condition, string field, string message)
    {
        if (condition)
        {
            Add(field, message);
        }

        return this;
    }

    public ValidationErrorCollector AddRange(string field, IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            Add(field, message);
        }

        return this;
    }

    public Error ToError() =>
        Error.Validation(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal));

    private void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }
}
