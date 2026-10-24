using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Contracts;

[ExcludeFromCodeCoverage]
[Description("Dados para cadastro de um novo usuario.")]
public sealed record CreateUserRequest(
    [property: Description("Nome completo (3 a 150 caracteres).")] string? Name,
    [property: Description("E-mail valido e unico. Normalizado para minusculas.")] string? Email,
    [property: Description("CPF valido e unico, com ou sem mascara. Imutavel apos o cadastro.")] string? Cpf,
    [property: Description("Telefone com DDD (10 ou 11 digitos), com ou sem mascara.")] string? Phone,
    [property: Description("Data de nascimento (yyyy-MM-dd), no passado e com no maximo 120 anos.")] DateOnly? BirthDate,
    [property: Description("Senha: 8 a 128 caracteres, com ao menos uma letra e um digito.")] string? Password);

[ExcludeFromCodeCoverage]
[Description("Dados cadastrais atualizaveis. O CPF nao pode ser alterado.")]
public sealed record UpdateUserRequest(
    [property: Description("Nome completo (3 a 150 caracteres).")] string? Name,
    [property: Description("E-mail valido e unico.")] string? Email,
    [property: Description("Telefone com DDD (10 ou 11 digitos).")] string? Phone,
    [property: Description("Data de nascimento (yyyy-MM-dd).")] DateOnly? BirthDate);

[ExcludeFromCodeCoverage]
[Description("Troca de senha do usuario.")]
public sealed record ChangePasswordRequest(
    [property: Description("Senha atual do usuario.")] string? CurrentPassword,
    [property: Description("Nova senha, seguindo a mesma politica do cadastro e diferente da atual.")] string? NewPassword);

[ExcludeFromCodeCoverage]
public sealed record ListUsersRequest(
    [property: FromQuery, Description("Pagina (inicia em 1). Padrao: 1.")] int? Page,
    [property: FromQuery, Description("Itens por pagina (1 a 100). Padrao: 20.")] int? PageSize,
    [property: FromQuery, Description("Filtro opcional por trecho do nome ou do e-mail (case-insensitive).")] string? Search);
