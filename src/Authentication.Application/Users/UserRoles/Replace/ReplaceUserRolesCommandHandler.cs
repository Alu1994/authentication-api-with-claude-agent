using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.UserRoles.Replace;

public sealed partial class ReplaceUserRolesCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    TimeProvider timeProvider,
    ILogger<ReplaceUserRolesCommandHandler> logger)
    : ICommandHandler<ReplaceUserRolesCommand, Result>
{
    private const string Field = "roleIds";

    public async Task<Result> HandleAsync(ReplaceUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command.RoleIds is null)
        {
            return new ValidationErrorCollector()
                .AddIf(true, Field, "A lista de roles e obrigatoria (use [] para remover todas).")
                .ToError();
        }

        if (await users.GetByIdAsync(command.UserId, cancellationToken) is null)
        {
            return UserErrors.NotFound;
        }

        var requested = command.RoleIds.ToHashSet();

        if (requested.Count > 0)
        {
            var active = await roles.GetActiveIdsAsync(requested, cancellationToken);
            var invalid = requested.Where(id => !active.Contains(id)).ToList();
            if (invalid.Count > 0)
            {
                return new ValidationErrorCollector()
                    .AddIf(true, Field, $"Roles inexistentes ou excluidas: {string.Join(", ", invalid)}.")
                    .ToError();
            }
        }

        var links = await users.GetRoleLinksAsync(command.UserId, cancellationToken);
        var current = links.Select(l => l.RoleId).ToHashSet();
        var now = timeProvider.GetUtcNow();

        foreach (var link in links.Where(l => !requested.Contains(l.RoleId)))
        {
            users.RemoveRoleLink(link);
        }

        foreach (var roleId in requested.Where(id => !current.Contains(id)))
        {
            users.AddRoleLink(UserRole.Create(command.UserId, roleId, now));
        }

        await users.SaveChangesAsync(cancellationToken);

        LogRolesReplaced(logger, command.UserId, requested.Count);
        return Result.Success();
    }

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information,
        Message = "Roles do usuario {UserId} substituidas ({Count} atribuicoes).")]
    private static partial void LogRolesReplaced(ILogger logger, Guid userId, int count);
}
