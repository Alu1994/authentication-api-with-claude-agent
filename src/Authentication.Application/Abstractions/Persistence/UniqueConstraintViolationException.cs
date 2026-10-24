namespace Authentication.Application.Abstractions.Persistence;

/// <summary>
/// Lancada pela camada de persistencia quando uma restricao de unicidade e violada
/// (ex.: corrida entre duas requisicoes cadastrando o mesmo e-mail).
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base($"Violacao de unicidade na restricao '{constraintName}'.", innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}
