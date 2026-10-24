using Authentication.Api.Contracts;
using Authentication.Api.OpenApi;
using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Authentication.Application.Permissions.Create;
using Authentication.Application.Permissions.Delete;
using Authentication.Application.Permissions.GetById;
using Authentication.Application.Permissions.List;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Endpoints;

internal static class PermissionEndpoints
{
    private const string PermissionNotFoundDescription =
        "Retorna 404 se a permissao nao existir ou tiver sido excluida.";

    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreatePermission")
            .WithSummary("Cria uma permissao")
            .WithDescription(
                "Cria uma permissao no formato recurso + acao, exposta externamente como `resource:action` " +
                "(ex.: `users:read`). Recurso e acao sao texto livre validado por `^[a-z][a-z0-9_-]{1,49}$`. " +
                "O par recurso/acao e unico entre as permissoes ativas (409 se ja existir). " +
                "Permissoes sao imutaveis: nao ha edicao, apenas criacao e exclusao.")
            .Accepts<CreatePermissionRequest>("application/json")
            .Produces<PermissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithRequestExample(new
            {
                resource = "users",
                action = "read",
                description = "Consultar usuarios",
            });

        group.MapGet("/", ListAsync)
            .WithName("ListPermissions")
            .WithSummary("Lista permissoes")
            .WithDescription(
                "Lista paginada de permissoes ativas, ordenada por recurso e acao. " +
                "Aceita filtros por recurso e por acao (igualdade exata).")
            .Produces<PagedResponse<PermissionResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetPermissionById")
            .WithSummary("Consulta uma permissao pelo Id")
            .WithDescription(PermissionNotFoundDescription)
            .Produces<PermissionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeletePermission")
            .WithSummary("Exclui uma permissao (soft delete)")
            .WithDescription(
                "Exclusao logica: a permissao deixa de aparecer nas consultas e o mesmo par recurso/acao " +
                "pode ser criado novamente. Retorna 409 enquanto a permissao estiver vinculada a alguma role ativa " +
                "(desvincule-a antes). " + PermissionNotFoundDescription)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreatePermissionRequest request,
        [FromServices] ICommandHandler<CreatePermissionCommand, Result<PermissionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreatePermissionCommand(request.Resource, request.Action, request.Description);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/permissions/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListPermissionsRequest request,
        [FromServices] IQueryHandler<ListPermissionsQuery, Result<PagedResponse<PermissionResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListPermissionsQuery(
            request.Page ?? Paging.DefaultPage,
            request.PageSize ?? Paging.DefaultPageSize,
            request.Resource,
            request.Action);

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        [FromServices] IQueryHandler<GetPermissionByIdQuery, Result<PermissionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPermissionByIdQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        [FromServices] ICommandHandler<DeletePermissionCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePermissionCommand(id), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
