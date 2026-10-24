using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Roles;
using Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Roles;

[Collection(ApiCollection.Name)]
public class RoleEndpointsTests(ApiFactory factory)
{
    private const string AnyId = "0198a3c4-0000-7000-8000-000000000001";
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("POST", "/roles")]
    [InlineData("GET", "/roles")]
    [InlineData("GET", $"/roles/{AnyId}")]
    [InlineData("PUT", $"/roles/{AnyId}")]
    [InlineData("DELETE", $"/roles/{AnyId}")]
    [InlineData("GET", $"/roles/{AnyId}/permissions")]
    [InlineData("PUT", $"/roles/{AnyId}/permissions")]
    [InlineData("POST", $"/roles/{AnyId}/permissions/{AnyId}")]
    [InlineData("DELETE", $"/roles/{AnyId}/permissions/{AnyId}")]
    public async Task Endpoints_ReturnUnauthorized_WithoutToken(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null,
        };

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithNormalizedRoleAndLocation()
    {
        await _client.AuthenticateAsNewUserAsync();
        var name = UniqueRoleName();

        var response = await _client.PostAsJsonAsync("/roles", new { name = $"  {name} ", description = "  " }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var role = await response.Content.ReadFromJsonAsync<RoleResponse>(Ct);
        Assert.NotNull(role);
        Assert.Equal(name, role.Name);
        Assert.Null(role.Description);
        Assert.Null(role.UpdatedAt);
        Assert.Equal($"/roles/{role.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(role, await _client.GetFromJsonAsync<RoleResponse>($"/roles/{role.Id}", Ct));
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_ForInvalidPayload()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.PostAsJsonAsync("/roles", new { name = "a", description = new string('d', 501) }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Equal(["description", "name"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Create_ReturnsConflict_ForDuplicateName_IgnoringCase()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();

        var response = await _client.PostAsJsonAsync("/roles", new { name = role.Name.ToUpperInvariant() }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(RoleErrors.NameInUse.Message, problem!.Detail);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownRole()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync($"/roles/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(RoleErrors.NotFound.Message, problem!.Detail);
    }

    [Fact]
    public async Task List_FiltersByNameFragment_IgnoringCase_WithPaging()
    {
        await _client.AuthenticateAsNewUserAsync();
        var marker = Guid.NewGuid().ToString("N")[..10];
        await _client.CreateRoleAsync($"Busca {marker} A");
        await _client.CreateRoleAsync($"Busca {marker} B");
        await _client.CreateRoleAsync($"Busca {marker} C");

        var page = await _client.GetFromJsonAsync<PagedResponse<RoleResponse>>(
            $"/roles?name={marker.ToUpperInvariant()}&page=2&pageSize=2", Ct);

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal($"Busca {marker} C", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task List_ReturnsValidationProblem_ForInvalidPaging()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync("/roles?pageSize=101", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription_AndSetsUpdatedAt()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync(description: "Antiga");
        var newName = UniqueRoleName();

        var response = await _client.PutAsJsonAsync($"/roles/{role.Id}", new { name = newName, description = "Nova" }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await _client.GetFromJsonAsync<RoleResponse>($"/roles/{role.Id}", Ct);
        Assert.Equal(newName, updated!.Name);
        Assert.Equal("Nova", updated.Description);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(role.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task Update_AllowsChangingOnlyTheCaseOfItsOwnName()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();

        var response = await _client.PutAsJsonAsync($"/roles/{role.Id}", new { name = role.Name.ToUpperInvariant() }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsConflictOrNotFound()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();
        var other = await _client.CreateRoleAsync();

        var conflict = await _client.PutAsJsonAsync($"/roles/{role.Id}", new { name = other.Name }, Ct);
        var notFound = await _client.PutAsJsonAsync($"/roles/{Guid.NewGuid()}", new { name = UniqueRoleName() }, Ct);
        var invalid = await _client.PutAsJsonAsync($"/roles/{role.Id}", new { name = "" }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletesRole_AndAllowsReusingTheName()
    {
        await _client.AuthenticateAsNewUserAsync();
        var role = await _client.CreateRoleAsync();

        var response = await _client.DeleteAsync($"/roles/{role.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/roles/{role.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/roles/{role.Id}", Ct)).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            var stored = await db.Roles.IgnoreQueryFilters().AsNoTracking().SingleAsync(r => r.Id == role.Id, Ct);
            Assert.True(stored.IsDeleted);
            Assert.NotNull(stored.DeletedAt);
            Assert.Equal(stored.DeletedAt, stored.UpdatedAt);
        }

        // Unicidade vale so entre roles ativas.
        var recreated = await _client.CreateRoleAsync(role.Name);
        Assert.NotEqual(role.Id, recreated.Id);
    }
}
