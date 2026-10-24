using Authentication.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Infrastructure.Security;

/// <summary>
/// Adapter sobre o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core Identity
/// (PBKDF2 com HMAC-SHA512, salt aleatorio e 100.000 iteracoes no formato V3).
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // O PasswordHasher padrao nao utiliza a instancia do usuario; um placeholder evita acoplamento a entidade.
    private static readonly object UserPlaceholder = new();
    private readonly PasswordHasher<object> _hasher = new();

    // Hash descartavel gerado com os mesmos parametros (formato/iteracoes) dos hashes reais,
    // para que a verificacao simulada tenha o mesmo custo. Gerado uma unica vez, sob demanda.
    private readonly Lazy<string> _dummyHash;

    public IdentityPasswordHasher()
    {
        _dummyHash = new Lazy<string>(() => Hash(Guid.NewGuid().ToString("N")));
    }

    public string Hash(string password) => _hasher.HashPassword(UserPlaceholder, password);

    public void SimulateVerification(string providedPassword) =>
        _ = _hasher.VerifyHashedPassword(UserPlaceholder, _dummyHash.Value, providedPassword);

    public PasswordVerification Verify(string passwordHash, string providedPassword) =>
        _hasher.VerifyHashedPassword(UserPlaceholder, passwordHash, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed,
        };
}
