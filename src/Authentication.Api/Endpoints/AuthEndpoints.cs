using Authentication.Api.Contracts;
using Authentication.Api.OpenApi;
using Authentication.Api.Security;
using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Auth.Login;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Autentica um usuario")
            .WithDescription(
                "Valida e-mail e senha e retorna um JWT (HS256) com as claims de identidade `sub`, `email`, `name` e `jti`. " +
                "Usuarios excluidos nao conseguem autenticar. Para evitar enumeracao de contas, e-mail inexistente e " +
                "senha incorreta retornam a mesma resposta 401 (com tempo de resposta equivalente). " +
                "Protegido por rate limiting por IP (janela fixa, padrao de 5 tentativas por 60s): excedentes " +
                "recebem 429 com o header `Retry-After`.")
            .RequireRateLimiting(LoginRateLimiting.PolicyName)
            .Accepts<LoginRequest>("application/json")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithRequestExample(new { email = "fulano@example.com", password = "Senha123" });

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, Result<LoginResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
