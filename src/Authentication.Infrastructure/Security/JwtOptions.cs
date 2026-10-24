using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Authentication.Infrastructure.Security;

/// <summary>Configuracao de emissao/validacao de JWT (secao <c>Jwt</c>).</summary>
[ExcludeFromCodeCoverage]
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Tamanho minimo da chave HS256 (256 bits).</summary>
    public const int MinSigningKeyLength = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Chave simetrica de assinatura. Nunca versionar: usar user-secrets/variavel de ambiente/secret manager.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)} e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)} e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Length < JwtOptions.MinSigningKeyLength)
        {
            failures.Add(
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} deve ter ao menos {JwtOptions.MinSigningKeyLength} caracteres.");
        }

        if (options.ExpirationMinutes <= 0)
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.ExpirationMinutes)} deve ser maior que zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
