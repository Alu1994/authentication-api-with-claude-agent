namespace Authentication.Application.Users.Delete;

/// <summary>Exclusao logica (soft delete) de um usuario.</summary>
public sealed record DeleteUserCommand(Guid Id);
