using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Validation;
using Authentication.Application.Roles.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Roles.Update;

public sealed partial class UpdateRoleCommandHandler(
    IRoleRepository repository,
    TimeProvider timeProvider,
    ILogger<UpdateRoleCommandHandler> logger)
    : ICommandHandler<UpdateRoleCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var name = RoleRules.NormalizeName(command.Name);
        var description = DescriptionRules.Normalize(command.Description);

        var errors = new ValidationErrorCollector().ValidateRole(name, description);
        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        var role = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (role is null)
        {
            return RoleErrors.NotFound;
        }

        if (await repository.NameExistsAsync(RoleRules.NormalizeForComparison(name), role.Id, cancellationToken))
        {
            return RoleErrors.NameInUse;
        }

        role.Update(name, description, timeProvider.GetUtcNow());

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return RoleErrors.NameInUse;
        }

        LogRoleUpdated(logger, role.Id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4101, Level = LogLevel.Information, Message = "Role {RoleId} atualizada.")]
    private static partial void LogRoleUpdated(ILogger logger, Guid roleId);
}
