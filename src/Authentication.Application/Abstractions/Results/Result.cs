using System.Diagnostics.CodeAnalysis;

namespace Authentication.Application.Abstractions.Results;

/// <summary>
/// Resultado de uma operacao de negocio, evitando o uso de excecoes para fluxo esperado.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value) : base(null) => _value = value;

    private Result(Error error) : base(error)
    {
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Nao e possivel acessar o valor de um resultado com falha.");

    /// <summary>Sucesso explicito, necessario quando <typeparamref name="TValue"/> e uma interface
    /// (o C# nao permite conversao implicita definida pelo usuario a partir de interfaces).</summary>
    public static Result<TValue> Success(TValue value) => new(value);

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new(error);
}
