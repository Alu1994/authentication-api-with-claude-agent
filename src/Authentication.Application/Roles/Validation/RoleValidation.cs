using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Validation;

namespace Authentication.Application.Roles.Validation;

/// <summary>Validacao compartilhada entre criacao e edicao de role.</summary>
internal static class RoleValidation
{
    public static ValidationErrorCollector ValidateRole(
        this ValidationErrorCollector errors,
        string name,
        string? description) =>
        errors
            .AddIf(!RoleRules.IsValidName(name), "name", RoleRules.NameLengthMessage)
            .AddIf(!DescriptionRules.IsValid(description), "description", DescriptionRules.TooLongMessage);
}
