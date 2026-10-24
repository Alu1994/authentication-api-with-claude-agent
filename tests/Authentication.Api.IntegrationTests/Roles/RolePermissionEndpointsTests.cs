using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Microsoft.AspNetCore.Mvc;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Roles;

[Collection(ApiCollection.Name)]
public class RolePermissionEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<IReadOnlyList<PermissionResponse>> ListAsync(Guid roleId) =>
        (await _client.GetFromJsonAsync<List<PermissionResponse>>($"/roles/{roleId}/permissions", Ct))!;

    [Fact]
    public async Task Add_LinksPermission_Idempotently_WithoutChangingRoleUpdatedAt()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var resource = UniqueResource();
        var read = await _client.CreatePermissionAsync(resource, "read");
        var create = await _client.CreatePermissionAsync(resource, "create");

        var first = await _client.PostAsync($"/roles/{role.Id}/permissions/{read.Id}", null, Ct);
        var again = await _client.PostAsync($"/roles/{role.Id}/permissions/{read.Id}", null, Ct);
        await _client.PostAsync($"/roles/{role.Id}/permissions/{create.Id}", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        var permissions = await ListAsync(role.Id);
        Assert.Equal([$"{resource}:create", $"{resource}:read"], permissions.Select(p => p.Name));
        Assert.Null((await _client.GetFromJsonAsync<RoleResponse>($"/roles/{role.Id}", Ct))!.UpdatedAt);
    }

    [Fact]
    public async Task Add_ReturnsNotFound_ForUnknownRoleOrPermission()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var permission = await _client.CreatePermissionAsync(UniqueResource());

        var unknownRole = await _client.PostAsync($"/roles/{Guid.NewGuid()}/permissions/{permission.Id}", null, Ct);
        var unknownPermission = await _client.PostAsync($"/roles/{role.Id}/permissions/{Guid.NewGuid()}", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
        Assert.Equal(RoleErrors.NotFound.Message,
            (await unknownRole.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
        Assert.Equal(HttpStatusCode.NotFound, unknownPermission.StatusCode);
        Assert.Equal(PermissionErrors.NotFound.Message,
            (await unknownPermission.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }

    [Fact]
    public async Task Remove_UnlinksPermission_AndReturnsNotFoundWhenNotLinked()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var permission = await _client.CreatePermissionAsync(UniqueResource());
        await _client.PostAsync($"/roles/{role.Id}/permissions/{permission.Id}", null, Ct);

        var removed = await _client.DeleteAsync($"/roles/{role.Id}/permissions/{permission.Id}", Ct);
        var notLinked = await _client.DeleteAsync($"/roles/{role.Id}/permissions/{permission.Id}", Ct);
        var unknownRole = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/permissions/{permission.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(await ListAsync(role.Id));
        Assert.Equal(HttpStatusCode.NotFound, notLinked.StatusCode);
        Assert.Equal(RoleErrors.PermissionNotLinked.Message,
            (await notLinked.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
        Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
    }

    [Fact]
    public async Task Replace_SetsExactlyTheGivenPermissions()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var resource = UniqueResource();
        var a = await _client.CreatePermissionAsync(resource, "aa");
        var b = await _client.CreatePermissionAsync(resource, "bb");
        var c = await _client.CreatePermissionAsync(resource, "cc");
        await _client.PostAsync($"/roles/{role.Id}/permissions/{a.Id}", null, Ct);
        await _client.PostAsync($"/roles/{role.Id}/permissions/{b.Id}", null, Ct);

        var replaced = await _client.PutAsJsonAsync(
            $"/roles/{role.Id}/permissions", new { permissionIds = new[] { b.Id, c.Id, c.Id } }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);
        Assert.Equal([b.Id, c.Id], (await ListAsync(role.Id)).Select(p => p.Id));

        var cleared = await _client.PutAsJsonAsync(
            $"/roles/{role.Id}/permissions", new { permissionIds = Array.Empty<Guid>() }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
        Assert.Empty(await ListAsync(role.Id));
    }

    [Fact]
    public async Task Replace_IsAllOrNothing_WhenSomeIdIsInvalid()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var resource = UniqueResource();
        var kept = await _client.CreatePermissionAsync(resource, "read");
        var deleted = await _client.CreatePermissionAsync(resource, "delete");
        await _client.DeleteAsync($"/permissions/{deleted.Id}", Ct);
        await _client.PostAsync($"/roles/{role.Id}/permissions/{kept.Id}", null, Ct);
        var unknown = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync(
            $"/roles/{role.Id}/permissions", new { permissionIds = new[] { deleted.Id, unknown } }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        var message = Assert.Single(problem!.Errors["permissionIds"]);
        Assert.Contains(deleted.Id.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(unknown.ToString(), message, StringComparison.Ordinal);
        Assert.Equal([kept.Id], (await ListAsync(role.Id)).Select(p => p.Id));
    }

    [Fact]
    public async Task Replace_ReturnsValidationProblem_WhenListIsMissing_AndNotFoundForUnknownRole()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();

        var missing = await _client.PutAsJsonAsync($"/roles/{role.Id}/permissions", new { }, Ct);
        var unknownRole = await _client.PutAsJsonAsync(
            $"/roles/{Guid.NewGuid()}/permissions", new { permissionIds = Array.Empty<Guid>() }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsNotFound_ForUnknownRole()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync($"/roles/{Guid.NewGuid()}/permissions", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePermission_ReturnsConflict_WhileLinkedToActiveRole()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var permission = await _client.CreatePermissionAsync(UniqueResource());
        await _client.PostAsync($"/roles/{role.Id}/permissions/{permission.Id}", null, Ct);

        var blocked = await _client.DeleteAsync($"/permissions/{permission.Id}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal(PermissionErrors.InUse.Message,
            (await blocked.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);

        // Role com permissoes (e sem usuarios) pode ser excluida; o vinculo fica inerte
        // e deixa de bloquear a exclusao da permissao.
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/roles/{role.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/permissions/{permission.Id}", Ct)).StatusCode);
    }
}
