using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Api.Contracts;

[ExcludeFromCodeCoverage]
[Description("Credenciais para autenticacao.")]
public sealed record LoginRequest(
    [property: Description("E-mail cadastrado.")] string? Email,
    [property: Description("Senha do usuario.")] string? Password);
