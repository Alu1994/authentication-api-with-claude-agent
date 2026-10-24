using Authentication.Api.Contracts;
using Authentication.Api.OpenApi;
using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Roles.Create;
using Authentication.Application.Roles.Delete;
using Authentication.Application.Roles.GetById;
using Authentication.Application.Roles.List;
using Authentication.Application.Roles.RolePermissions.Add;
using Authentication.Application.Roles.RolePermissions.List;
using Authentication.Application.Roles.RolePermissions.Remove;
using Authentication.Application.Roles.RolePermissions.Replace;
using Authentication.Application.Roles.Update;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Endpoints;

internal static class RoleEndpoints
{
    private const string RoleNotFoundDescription = "Retorna 404 se a role nao existir ou tiver sido excluida.";

    private const string LinkNote =
        "Vincular ou desvincular nao altera o `updatedAt` da role. Mudancas so aparecem no token no proximo login.";

    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles").WithTags("Roles").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreateRole")
            .WithSummary("Cria uma role")
            .WithDescription(
                "Cria uma role com nome (texto livre, 2 a 100 caracteres) e descricao opcional. O nome e unico entre " +
                "as roles ativas, sem diferenciar maiusculas/minusculas (409 se ja existir).")
            .Accepts<CreateRoleRequest>("application/json")
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithRequestExample(new { name = "Suporte N2", description = "Atendimento de segundo nivel" });

        group.MapGet("/", ListAsync)
            .WithName("ListRoles")
            .WithSummary("Lista roles")
            .WithDescription(
                "Lista paginada de roles ativas, ordenada por nome. Aceita filtro por trecho do nome (case-insensitive).")
            .Produces<PagedResponse<RoleResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetRoleById")
            .WithSummary("Consulta uma role pelo Id")
            .WithDescription(RoleNotFoundDescription)
            .Produces<RoleResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateRole")
            .WithSummary("Atualiza uma role")
            .WithDescription(
                "Substitui nome e descricao (descricao ausente ou em branco remove a atual) e registra o `updatedAt`. " +
                "409 se o nome pertencer a outra role ativa. " + RoleNotFoundDescription)
            .Accepts<UpdateRoleRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithRequestExample(new { name = "Suporte N2", description = "Atendimento de segundo nivel e escalonamento" });

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteRole")
            .WithSummary("Exclui uma role (soft delete)")
            .WithDescription(
                "Exclusao logica: a role deixa de aparecer nas consultas e o nome pode ser reutilizado. Os vinculos " +
                "com permissoes sao mantidos, sem efeito. " + RoleNotFoundDescription)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/permissions", ListPermissionsAsync)
            .WithName("ListRolePermissions")
            .WithSummary("Lista as permissoes de uma role")
            .WithDescription(
                "Permissoes ativas vinculadas a role, ordenadas por recurso e acao (sem paginacao). " +
                RoleNotFoundDescription)
            .Produces<IReadOnlyList<PermissionResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/permissions", ReplacePermissionsAsync)
            .WithName("ReplaceRolePermissions")
            .WithSummary("Substitui as permissoes de uma role")
            .WithDescription(
                "Define exatamente as permissoes da role (tudo ou nada): lista vazia remove todos os vinculos e Ids " +
                "repetidos sao ignorados. Se algum Id nao existir ou estiver excluido, retorna 400 listando os Ids " +
                "invalidos em `permissionIds` e nada e alterado. " + LinkNote + " " + RoleNotFoundDescription)
            .Accepts<ReplaceRolePermissionsRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithRequestExample(new
            {
                permissionIds = new[]
                {
                    Guid.Parse("0198a3c4-0000-7000-8000-000000000001"),
                    Guid.Parse("0198a3c4-0000-7000-8000-000000000002"),
                },
            });

        group.MapPost("/{id:guid}/permissions/{permissionId:guid}", AddPermissionAsync)
            .WithName("AddRolePermission")
            .WithSummary("Vincula uma permissao a uma role")
            .WithDescription(
                "Idempotente: vincular uma permissao ja vinculada retorna 204. Retorna 404 se a role ou a permissao " +
                "nao existir (ou estiver excluida). " + LinkNote)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/permissions/{permissionId:guid}", RemovePermissionAsync)
            .WithName("RemoveRolePermission")
            .WithSummary("Desvincula uma permissao de uma role")
            .WithDescription(
                "Retorna 404 se a role nao existir ou se a permissao nao estiver vinculada a ela. " + LinkNote)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateRoleRequest request,
        [FromServices] ICommandHandler<CreateRoleCommand, Result<RoleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateRoleCommand(request.Name, request.Description), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/roles/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListRolesRequest request,
        [FromServices] IQueryHandler<ListRolesQuery, Result<PagedResponse<RoleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListRolesQuery(
            request.Page ?? Paging.DefaultPage,
            request.PageSize ?? Paging.DefaultPageSize,
            request.Name);

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        [FromServices] IQueryHandler<GetRoleByIdQuery, Result<RoleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetRoleByIdQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        [FromServices] ICommandHandler<UpdateRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateRoleCommand(id, request.Name, request.Description),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteRoleCommand(id), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> ListPermissionsAsync(
        Guid id,
        [FromServices] IQueryHandler<ListRolePermissionsQuery, Result<IReadOnlyList<PermissionResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListRolePermissionsQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ReplacePermissionsAsync(
        Guid id,
        ReplaceRolePermissionsRequest request,
        [FromServices] ICommandHandler<ReplaceRolePermissionsCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReplaceRolePermissionsCommand(id, request.PermissionIds),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> AddPermissionAsync(
        Guid id,
        Guid permissionId,
        [FromServices] ICommandHandler<AddRolePermissionCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddRolePermissionCommand(id, permissionId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> RemovePermissionAsync(
        Guid id,
        Guid permissionId,
        [FromServices] ICommandHandler<RemoveRolePermissionCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveRolePermissionCommand(id, permissionId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
