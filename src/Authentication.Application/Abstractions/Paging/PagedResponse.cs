namespace Authentication.Application.Abstractions.Paging;

/// <summary>Pagina de resultados.</summary>
/// <param name="Items">Itens da pagina atual.</param>
/// <param name="Page">Numero da pagina (inicia em 1).</param>
/// <param name="PageSize">Quantidade maxima de itens por pagina.</param>
/// <param name="TotalCount">Total de itens que atendem ao filtro.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
