using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Users;
using Microsoft.Extensions.Time.Testing;

namespace Authentication.Application.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    public const string ValidName = "Fulano de Tal";
    public const string ValidEmail = "fulano@example.com";
    public const string ValidCpf = "52998224725";
    public const string ValidPhone = "11987654321";
    public const string ValidPassword = "Senha123";
    public const string PasswordHash = "hashed-password";
    public static readonly DateOnly ValidBirthDate = new(1990, 5, 10);

    public static FakeTimeProvider CreateTimeProvider() => new(Now);

    public static User CreateUser(string email = ValidEmail, string cpf = ValidCpf) =>
        User.Create(ValidName, email, cpf, ValidPhone, ValidBirthDate, PasswordHash, Now);

    public static Permission CreatePermission(string resource = "users", string action = "read") =>
        Permission.Create(resource, action, "Descricao", Now);

    public static Role CreateRole(string name = "Admin") => Role.Create(name, "Descricao", Now);
}
