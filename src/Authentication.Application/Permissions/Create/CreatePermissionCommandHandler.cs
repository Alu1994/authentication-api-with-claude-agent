using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions.Validation;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Permissions.Create;

public sealed partial class CreatePermissionCommandHandler(
    IPermissionRepository repository,
    TimeProvider timeProvider,
    ILogger<CreatePermissionCommandHandler> logger)
    : ICommandHandler<CreatePermissionCommand, Result<PermissionResponse>>
{
    public async Task<Result<PermissionResponse>> HandleAsync(
        CreatePermissionCommand command,
        CancellationToken cancellationToken)
    {
        var resource = PermissionRules.NormalizeSegment(command.Resource);
        var action = PermissionRules.NormalizeSegment(command.Action);
        var description = PermissionRules.NormalizeDescription(command.Description);

        var errors = new ValidationErrorCollector()
            .AddIf(!PermissionRules.IsValidSegment(resource), "resource", PermissionRules.SegmentFormatMessage)
            .AddIf(!PermissionRules.IsValidSegment(action), "action", PermissionRules.SegmentFormatMessage)
            .AddIf(!PermissionRules.IsValidDescription(description), "description",
                $"A descricao deve ter no maximo {PermissionRules.DescriptionMaxLength} caracteres.");

        if (errors.HasErrors)
        {
            return errors.ToError();
        }

        if (await repository.ExistsAsync(resource, action, cancellationToken))
        {
            LogDuplicateRejected(logger, resource, action);
            return PermissionErrors.AlreadyExists;
        }

        var permission = Permission.Create(resource, action, description, timeProvider.GetUtcNow());
        repository.Add(permission);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            LogDuplicateRejected(logger, resource, action);
            return PermissionErrors.AlreadyExists;
        }

        LogPermissionCreated(logger, permission.Id, permission.Name);
        return PermissionResponse.FromPermission(permission);
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Permissao {PermissionId} ({Name}) criada.")]
    private static partial void LogPermissionCreated(ILogger logger, Guid permissionId, string name);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Criacao de permissao rejeitada por duplicidade ({Resource}:{Action}).")]
    private static partial void LogDuplicateRejected(ILogger logger, string resource, string action);
}
