namespace Authentication.Application.Abstractions.Messaging;

/// <summary>
/// Handler de uma Query (operacao de leitura). Injetado diretamente no endpoint, sem dispatcher central.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
