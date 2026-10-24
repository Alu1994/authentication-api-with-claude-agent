using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Application.Auth.Login;

[ExcludeFromCodeCoverage]
public sealed record LoginResponse(
    [property: Description("JWT a ser enviado no header Authorization: Bearer {token}.")] string AccessToken,
    [property: Description("Tipo do token (sempre \"Bearer\").")] string TokenType,
    [property: Description("Data/hora (UTC) de expiracao do token.")] DateTimeOffset ExpiresAt);
