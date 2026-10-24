using Authentication.Application.Abstractions.Security;
using Authentication.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Authentication.Infrastructure.Tests.Security;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotReturnPlainTextAndIsSalted()
    {
        var first = _hasher.Hash("Senha123");
        var second = _hasher.Hash("Senha123");

        Assert.DoesNotContain("Senha123", first, StringComparison.Ordinal);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ReturnsSuccess_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Senha123");

        Assert.Equal(PasswordVerification.Success, _hasher.Verify(hash, "Senha123"));
    }

    [Fact]
    public void Verify_ReturnsFailed_ForWrongPassword()
    {
        var hash = _hasher.Hash("Senha123");

        Assert.Equal(PasswordVerification.Failed, _hasher.Verify(hash, "Outra123"));
    }

    [Fact]
    public void SimulateVerification_HasCostComparableToFailedVerification()
    {
        // Mede a mediana de varias execucoes para reduzir ruido. Limites largos de proposito:
        // o objetivo e garantir que o PBKDF2 completo e executado (nao um retorno imediato).
        var hash = _hasher.Hash("Senha123");
        _hasher.SimulateVerification("aquecimento1");
        _hasher.Verify(hash, "aquecimento1");

        var simulated = MedianTicks(() => _hasher.SimulateVerification("Errada123"));
        var failed = MedianTicks(() => _hasher.Verify(hash, "Errada123"));

        var ratio = simulated / (double)failed;
        Assert.InRange(ratio, 0.33, 3.0);
    }

    [Fact]
    public void SimulateVerification_AcceptsAnyInput()
    {
        _hasher.SimulateVerification(string.Empty);
        _hasher.SimulateVerification(new string('x', 200));
    }

    private static long MedianTicks(Action action)
    {
        var samples = new long[7];
        for (var i = 0; i < samples.Length; i++)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            action();
            samples[i] = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }

        Array.Sort(samples);
        return samples[samples.Length / 2];
    }

    [Fact]
    public void Verify_ReturnsSuccessRehashNeeded_ForLegacyHash()
    {
        var legacyHasher = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2,
        }));
        var legacyHash = legacyHasher.HashPassword(new object(), "Senha123");

        Assert.Equal(PasswordVerification.SuccessRehashNeeded, _hasher.Verify(legacyHash, "Senha123"));
    }
}
