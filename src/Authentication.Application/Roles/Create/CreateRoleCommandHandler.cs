using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Validation;
using Authentication.Application.Roles.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.Create;

public sealed partial class CreateRoleCommandHandler(
    IRoleRepository repository,
    TimeProvider timeProvider,
    ILogger<CreateRoleCommandHandler> logger)
    : ICommandHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var name = RoleRules.NormalizeName(command.Name);
        var description = DescriptionRules.Normalize(command.Description);

        var errors = new ValidationErrorCollector().ValidateRole(name, description);
        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        if (await repository.NameExistsAsync(RoleRules.NormalizeForComparison(name), null, cancellationToken))
        {
            LogDuplicateRejected(logger);
            return RoleErrors.NameInUse;
        }

        var role = Role.Create(name, description, timeProvider.GetUtcNow());
        repository.Add(role);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            LogDuplicateRejected(logger);
            return RoleErrors.NameInUse;
        }

        LogRoleCreated(logger, role.Id);
        return RoleResponse.FromRole(role);
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Role {RoleId} criada.")]
    private static partial void LogRoleCreated(ILogger logger, Guid roleId);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Criacao de role rejeitada: nome em uso.")]
    private static partial void LogDuplicateRejected(ILogger logger);
}
