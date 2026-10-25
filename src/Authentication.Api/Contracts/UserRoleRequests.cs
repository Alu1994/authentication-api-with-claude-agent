using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Authentication.Api.Contracts;

[ExcludeFromCodeCoverage]
[Description("Lista completa de roles do usuario (substitui a atual).")]
public sealed record ReplaceUserRolesRequest(
    [property: Description(
        "Ids das roles. Lista vazia remove todas as atribuicoes; Ids repetidos sao ignorados. " +
        "Se algum Id nao existir ou estiver excluido, nada e alterado (400).")]
    IReadOnlyList<Guid>? RoleIds);
