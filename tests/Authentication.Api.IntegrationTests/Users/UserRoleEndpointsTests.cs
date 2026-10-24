using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Roles;
using Authentication.Application.Users;
using Microsoft.AspNetCore.Mvc;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Users;

[Collection(ApiCollection.Name)]
public class UserRoleEndpointsTests(ApiFactory factory)
{
    private const string AnyId = "0198a3c4-0000-7000-8000-000000000001";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<IReadOnlyList<RoleResponse>> ListAsync(Guid userId) =>
        (await _client.GetFromJsonAsync<List<RoleResponse>>($"/users/{userId}/roles", Ct))!;

    [Theory]
    [InlineData("GET", $"/users/{AnyId}/roles")]
    [InlineData("PUT", $"/users/{AnyId}/roles")]
    [InlineData("POST", $"/users/{AnyId}/roles/{AnyId}")]
    [InlineData("DELETE", $"/users/{AnyId}/roles/{AnyId}")]
    public async Task Endpoints_ReturnUnauthorized_WithoutToken(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "PUT" ? JsonContent.Create(new { }) : null,
        };

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_AssignsRole_Idempotently_WithoutChangingUserUpdatedAt()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var b = await _client.CreateRoleAsync($"B {marker}");
        var a = await _client.CreateRoleAsync($"A {marker}");

        var first = await _client.PostAsync($"/users/{user.Id}/roles/{b.Id}", null, Ct);
        var again = await _client.PostAsync($"/users/{user.Id}/roles/{b.Id}", null, Ct);
        await _client.PostAsync($"/users/{user.Id}/roles/{a.Id}", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal([a.Id, b.Id], (await ListAsync(user.Id)).Select(r => r.Id)); // ordenado por nome
        Assert.Null((await _client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}", Ct))!.UpdatedAt);
    }

    [Fact]
    public async Task Add_ReturnsNotFound_ForUnknownUserOrRole()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();

        var unknownUser = await _client.PostAsync($"/users/{Guid.NewGuid()}/roles/{role.Id}", null, Ct);
        var unknownRole = await _client.PostAsync($"/users/{user.Id}/roles/{Guid.NewGuid()}", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
        Assert.Equal(UserErrors.NotFound.Message,
            (await unknownUser.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
        Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
        Assert.Equal(RoleErrors.NotFound.Message,
            (await unknownRole.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }

    [Fact]
    public async Task Remove_UnassignsRole_AndReturnsNotFoundWhenNotAssigned()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        await _client.PostAsync($"/users/{user.Id}/roles/{role.Id}", null, Ct);

        var removed = await _client.DeleteAsync($"/users/{user.Id}/roles/{role.Id}", Ct);
        var notAssigned = await _client.DeleteAsync($"/users/{user.Id}/roles/{role.Id}", Ct);
        var unknownUser = await _client.DeleteAsync($"/users/{Guid.NewGuid()}/roles/{role.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(await ListAsync(user.Id));
        Assert.Equal(HttpStatusCode.NotFound, notAssigned.StatusCode);
        Assert.Equal(UserErrors.RoleNotLinked.Message,
            (await notAssigned.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
    }

    [Fact]
    public async Task Replace_SetsExactlyTheGivenRoles_AndClearsWithEmptyList()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var a = await _client.CreateRoleAsync($"A {marker}");
        var b = await _client.CreateRoleAsync($"B {marker}");
        var c = await _client.CreateRoleAsync($"C {marker}");
        await _client.PostAsync($"/users/{user.Id}/roles/{a.Id}", null, Ct);
        await _client.PostAsync($"/users/{user.Id}/roles/{b.Id}", null, Ct);

        var replaced = await _client.PutAsJsonAsync(
            $"/users/{user.Id}/roles", new { roleIds = new[] { b.Id, c.Id, c.Id } }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);
        Assert.Equal([b.Id, c.Id], (await ListAsync(user.Id)).Select(r => r.Id));

        var cleared = await _client.PutAsJsonAsync($"/users/{user.Id}/roles", new { roleIds = Array.Empty<Guid>() }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
        Assert.Empty(await ListAsync(user.Id));
    }

    [Fact]
    public async Task Replace_IsAllOrNothing_WhenSomeIdIsInvalid()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var kept = await _client.CreateRoleAsync();
        var deleted = await _client.CreateRoleAsync();
        await _client.DeleteAsync($"/roles/{deleted.Id}", Ct);
        await _client.PostAsync($"/users/{user.Id}/roles/{kept.Id}", null, Ct);
        var unknown = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync(
            $"/users/{user.Id}/roles", new { roleIds = new[] { deleted.Id, unknown } }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        var message = Assert.Single(problem!.Errors["roleIds"]);
        Assert.Contains(deleted.Id.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(unknown.ToString(), message, StringComparison.Ordinal);
        Assert.Equal([kept.Id], (await ListAsync(user.Id)).Select(r => r.Id));
    }

    [Fact]
    public async Task Replace_ReturnsValidationProblem_WhenListIsMissing_AndNotFoundForUnknownUser()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var missing = await _client.PutAsJsonAsync($"/users/{user.Id}/roles", new { }, Ct);
        var unknownUser = await _client.PutAsJsonAsync(
            $"/users/{Guid.NewGuid()}/roles", new { roleIds = Array.Empty<Guid>() }, Ct);
        var listUnknownUser = await _client.GetAsync($"/users/{Guid.NewGuid()}/roles", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, listUnknownUser.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_ReturnsConflict_WhileAssignedToActiveUser()
    {
        await _client.AuthenticateAsNewUserAsync();
        var holder = await _client.RegisterAsync();
        var role = await _client.CreateRoleAsync();
        await _client.PostAsync($"/users/{holder.Id}/roles/{role.Id}", null, Ct);

        var blocked = await _client.DeleteAsync($"/roles/{role.Id}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal(RoleErrors.InUse.Message, (await blocked.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);

        // Atribuicao a usuario excluido fica inerte e deixa de bloquear a exclusao da role.
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/users/{holder.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/roles/{role.Id}", Ct)).StatusCode);
    }
}
