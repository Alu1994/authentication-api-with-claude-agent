namespace Authentication.Application.Users.ChangePassword;

/// <summary>Troca de senha, exigindo a senha atual.</summary>
public sealed record ChangePasswordCommand(Guid Id, string? CurrentPassword, string? NewPassword);
