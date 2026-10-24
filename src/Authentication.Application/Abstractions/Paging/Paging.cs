using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Abstractions.Paging;

/// <summary>Padroes e validacao de paginacao compartilhados pelas listagens.</summary>
public static class Paging
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    internal static ValidationErrorCollector ValidatePaging(this ValidationErrorCollector errors, int page, int pageSize) =>
        errors
            .AddIf(page < 1, "page", "A pagina deve ser maior ou igual a 1.")
            .AddIf(pageSize is < 1 or > MaxPageSize, "pageSize",
                $"O tamanho da pagina deve estar entre 1 e {MaxPageSize}.");
}
