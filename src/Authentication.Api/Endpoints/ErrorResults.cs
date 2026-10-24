using Authentication.Application.Abstractions.Results;

namespace Authentication.Api.Endpoints;

/// <summary>Converte erros de negocio em respostas ProblemDetails (RFC 9457).</summary>
internal static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        return error.Type switch
        {
            ErrorType.Validation => TypedResults.ValidationProblem(
                error.ValidationErrors.ToDictionary(),
                detail: error.Message,
                extensions: extensions),
            ErrorType.NotFound => Problem(StatusCodes.Status404NotFound, "Recurso nao encontrado", error, extensions),
            ErrorType.Conflict => Problem(StatusCodes.Status409Conflict, "Conflito", error, extensions),
            ErrorType.Unauthorized => Problem(StatusCodes.Status401Unauthorized, "Nao autorizado", error, extensions),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Tipo de erro nao mapeado."),
        };
    }

    private static IResult Problem(int statusCode, string title, Error error, Dictionary<string, object?> extensions) =>
        TypedResults.Problem(statusCode: statusCode, title: title, detail: error.Message, extensions: extensions);
}
