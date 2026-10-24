using Authentication.Infrastructure.Security;

namespace Authentication.Infrastructure.Tests.Security;

public class JwtOptionsValidatorTests
{
    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = new string('k', JwtOptions.MinSigningKeyLength),
        ExpirationMinutes = 60,
    };

    [Fact]
    public void Validate_Succeeds_ForValidOptions()
    {
        var result = new JwtOptionsValidator().Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenRequiredValuesAreMissingOrInvalid()
    {
        var options = new JwtOptions
        {
            Issuer = "",
            Audience = " ",
            SigningKey = "curta",
            ExpirationMinutes = 0,
        };

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        var failures = result.Failures!.ToList();
        Assert.Equal(4, failures.Count);
        Assert.Contains(failures, f => f.Contains(nameof(JwtOptions.Issuer), StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(nameof(JwtOptions.Audience), StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(nameof(JwtOptions.SigningKey), StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(nameof(JwtOptions.ExpirationMinutes), StringComparison.Ordinal));
    }
}
