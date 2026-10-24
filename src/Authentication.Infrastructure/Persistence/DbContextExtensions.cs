using Authentication.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Authentication.Infrastructure.Persistence;

internal static class DbContextExtensions
{
    /// <summary>
    /// Persiste as alteracoes, convertendo violacoes de indice unico do PostgreSQL em
    /// <see cref="UniqueConstraintViolationException"/> (ex.: corrida entre duas requisicoes).
    /// </summary>
    public static async Task SaveChangesTranslatingUniqueViolationAsync(
        this DbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                           } postgresException)
        {
            throw new UniqueConstraintViolationException(postgresException.ConstraintName, ex);
        }
    }
}
