using Authentication.Api.Contracts;
using Authentication.Api.OpenApi;
using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Users;
using Authentication.Application.Users.ChangePassword;
using Authentication.Application.Users.Create;
using Authentication.Application.Users.Delete;
using Authentication.Application.Users.GetById;
using Authentication.Application.Users.List;
using Authentication.Application.Users.Update;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Endpoints;

internal static class UserEndpoints
{
    private const string UserNotFoundDescription = "Retorna 404 se o usuario nao existir ou tiver sido excluido.";

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .AllowAnonymous()
            .WithName("CreateUser")
            .WithSummary("Cadastra um usuario (auto-registro)")
            .WithDescription(
                "Endpoint publico. Normaliza e valida os dados (e-mail em minusculas; CPF e telefone apenas com digitos) " +
                "e armazena a senha com hash PBKDF2. E-mail e CPF sao unicos globalmente, inclusive entre usuarios " +
                "excluidos, e nao podem ser reaproveitados.")
            .Accepts<CreateUserRequest>("application/json")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithRequestExample(new
            {
                name = "Fulano de Tal",
                email = "fulano@example.com",
                cpf = "529.982.247-25",
                phone = "(11) 98765-4321",
                birthDate = "1990-05-10",
                password = "Senha123",
            });

        group.MapGet("/", ListAsync)
            .WithName("ListUsers")
            .WithSummary("Lista usuarios")
            .WithDescription(
                "Lista paginada de usuarios ativos, ordenada por nome. Aceita filtro por trecho do nome ou do e-mail.")
            .Produces<PagedResponse<UserResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Consulta um usuario pelo Id")
            .WithDescription(UserNotFoundDescription)
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateUser")
            .WithSummary("Atualiza os dados cadastrais de um usuario")
            .WithDescription(
                "Atualiza nome, e-mail, telefone e data de nascimento. O CPF e imutavel e e ignorado se enviado. " +
                UserNotFoundDescription)
            .Accepts<UpdateUserRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithRequestExample(new
            {
                name = "Fulano de Tal Silva",
                email = "fulano.silva@example.com",
                phone = "(11) 3333-4444",
                birthDate = "1990-05-10",
            });

        group.MapPut("/{id:guid}/password", ChangePasswordAsync)
            .WithName("ChangeUserPassword")
            .WithSummary("Troca a senha de um usuario")
            .WithDescription(
                "Exige a senha atual. A nova senha segue a mesma politica do cadastro e deve ser diferente da atual. " +
                "Senha atual incorreta retorna 400 com erro no campo `currentPassword`. " + UserNotFoundDescription)
            .Accepts<ChangePasswordRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithRequestExample(new { currentPassword = "Senha123", newPassword = "NovaSenha456" });

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteUser")
            .WithSummary("Exclui um usuario (soft delete)")
            .WithDescription(
                "Exclusao logica: o usuario deixa de aparecer nas consultas e nao consegue mais autenticar. " +
                UserNotFoundDescription)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        [FromServices] ICommandHandler<CreateUserCommand, Result<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.Name, request.Email, request.Cpf, request.Phone, request.BirthDate, request.Password);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/users/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListUsersRequest request,
        [FromServices] IQueryHandler<ListUsersQuery, Result<PagedResponse<UserResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListUsersQuery(
            request.Page ?? ListUsersQuery.DefaultPage,
            request.PageSize ?? ListUsersQuery.DefaultPageSize,
            request.Search);

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        [FromServices] IQueryHandler<GetUserByIdQuery, Result<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserByIdQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        [FromServices] ICommandHandler<UpdateUserCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.Name, request.Email, request.Phone, request.BirthDate);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> ChangePasswordAsync(
        Guid id,
        ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteUserCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteUserCommand(id), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
