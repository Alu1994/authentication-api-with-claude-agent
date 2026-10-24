namespace Authentication.Application.Abstractions.Messaging;

/// <summary>
/// Handler de um Command (operacao de escrita). Injetado diretamente no endpoint, sem dispatcher central.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
