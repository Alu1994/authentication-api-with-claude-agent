namespace Authentication.Application.Abstractions.Security;

public enum PasswordVerification
{
    Failed,
    Success,
    SuccessRehashNeeded,
}

/// <summary>Gera e verifica hashes de senha.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string passwordHash, string providedPassword);

    /// <summary>
    /// Executa uma verificacao com custo equivalente a <see cref="Verify"/> contra um hash descartavel.
    /// Usado quando o usuario nao existe, para que o tempo de resposta nao revele quais e-mails estao cadastrados.
    /// </summary>
    void SimulateVerification(string providedPassword);
}
