using Authentication.Api.Contracts;
using Authentication.Api.OpenApi;
using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Authentication.Application.Users.UserRoles.Add;
using Authentication.Application.Users.UserRoles.List;
using Authentication.Application.Users.UserRoles.Remove;
using Authentication.Application.Users.UserRoles.Replace;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Endpoints;

/// <summary>
/// Atribuicao de roles a usuarios (<c>/users/{id}/roles</c>). Fica separado de <see cref="UserEndpoints"/>
/// para nao alterar os endpoints de usuario existentes.
/// </summary>
internal static class UserRoleEndpoints
{
    private const string UserNotFoundDescription = "Retorna 404 se o usuario nao existir ou tiver sido excluido.";

    private const string LinkNote =
        "Atribuir ou remover roles nao altera o `updatedAt` do usuario. Tokens ja emitidos nao sao invalidados: " +
        "a mudanca aparece nas claims do proximo login.";

    public static IEndpointRouteBuilder MapUserRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/{id:guid}/roles").WithTags("Users").RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithName("ListUserRoles")
            .WithSummary("Lista as roles de um usuario")
            .WithDescription("Roles ativas atribuidas ao usuario, ordenadas por nome (sem paginacao). " +
                             UserNotFoundDescription)
            .Produces<IReadOnlyList<RoleResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/", ReplaceAsync)
            .WithName("ReplaceUserRoles")
            .WithSummary("Substitui as roles de um usuario")
            .WithDescription(
                "Define exatamente as roles do usuario (tudo ou nada): lista vazia remove todas as atribuicoes e Ids " +
                "repetidos sao ignorados. Se algum Id nao existir ou estiver excluido, retorna 400 listando os Ids " +
                "invalidos em `roleIds` e nada e alterado. " + LinkNote + " " + UserNotFoundDescription)
            .Accepts<ReplaceUserRolesRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithRequestExample(new { roleIds = new[] { Guid.Parse("0198a3c4-0000-7000-8000-000000000001") } });

        group.MapPost("/{roleId:guid}", AddAsync)
            .WithName("AddUserRole")
            .WithSummary("Atribui uma role a um usuario")
            .WithDescription(
                "Idempotente: atribuir uma role ja atribuida retorna 204. Retorna 404 se o usuario ou a role nao " +
                "existir (ou estiver excluido). " + LinkNote)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{roleId:guid}", RemoveAsync)
            .WithName("RemoveUserRole")
            .WithSummary("Remove uma role de um usuario")
            .WithDescription(
                "Retorna 404 se o usuario nao existir ou se a role nao estiver atribuida a ele. " + LinkNote)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid id,
        [FromServices] IQueryHandler<ListUserRolesQuery, Result<IReadOnlyList<RoleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListUserRolesQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ReplaceAsync(
        Guid id,
        ReplaceUserRolesRequest request,
        [FromServices] ICommandHandler<ReplaceUserRolesCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReplaceUserRolesCommand(id, request.RoleIds), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> AddAsync(
        Guid id,
        Guid roleId,
        [FromServices] ICommandHandler<AddUserRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddUserRoleCommand(id, roleId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> RemoveAsync(
        Guid id,
        Guid roleId,
        [FromServices] ICommandHandler<RemoveUserRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveUserRoleCommand(id, roleId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
