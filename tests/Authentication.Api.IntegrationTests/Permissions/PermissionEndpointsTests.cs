using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Permissions;
using Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Permissions;

[Collection(ApiCollection.Name)]
public class PermissionEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("POST", "/permissions")]
    [InlineData("GET", "/permissions")]
    [InlineData("GET", "/permissions/0198a3c4-0000-7000-8000-000000000001")]
    [InlineData("DELETE", "/permissions/0198a3c4-0000-7000-8000-000000000001")]
    public async Task Endpoints_ReturnUnauthorized_WithoutToken(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(new { resource = "users", action = "read" }) : null,
        };

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithNormalizedPermissionAndLocation()
    {
        await _client.AuthenticateAsNewUserAsync();
        var resource = UniqueResource();

        var response = await _client.PostAsJsonAsync("/permissions", new
        {
            resource = $"  {resource} ",
            action = "read",
            description = "  Consulta  ",
        }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var permission = await response.Content.ReadFromJsonAsync<PermissionResponse>(Ct);
        Assert.NotNull(permission);
        Assert.Equal(resource, permission.Resource);
        Assert.Equal("read", permission.Action);
        Assert.Equal($"{resource}:read", permission.Name);
        Assert.Equal("Consulta", permission.Description);
        Assert.Null(permission.UpdatedAt);
        Assert.Equal($"/permissions/{permission.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_ForInvalidPayload()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.PostAsJsonAsync("/permissions", new
        {
            resource = "Users",
            action = "users:read",
            description = new string('a', 501),
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Equal(["action", "description", "resource"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Create_ReturnsConflict_ForDuplicateResourceAndAction()
    {
        await _client.AuthenticateAsNewUserAsync();
        var resource = UniqueResource();
        await _client.CreatePermissionAsync(resource, "read");

        var duplicate = await _client.PostAsJsonAsync("/permissions", new { resource, action = "read" }, Ct);
        var otherAction = await _client.PostAsJsonAsync("/permissions", new { resource, action = "update" }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(PermissionErrors.AlreadyExists.Message, problem!.Detail);
        Assert.Equal(HttpStatusCode.Created, otherAction.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsPermission_OrNotFound()
    {
        await _client.AuthenticateAsNewUserAsync();
        var created = await _client.CreatePermissionAsync(UniqueResource(), "read", "Consulta");

        var found = await _client.GetFromJsonAsync<PermissionResponse>($"/permissions/{created.Id}", Ct);
        var missing = await _client.GetAsync($"/permissions/{Guid.NewGuid()}", Ct);

        Assert.Equal(created, found);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var problem = await missing.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(PermissionErrors.NotFound.Message, problem!.Detail);
    }

    [Fact]
    public async Task List_FiltersByExactResourceAndAction_WithPaging()
    {
        await _client.AuthenticateAsNewUserAsync();
        var resource = UniqueResource();
        await _client.CreatePermissionAsync(resource, "create");
        await _client.CreatePermissionAsync(resource, "delete");
        await _client.CreatePermissionAsync(resource, "read");
        await _client.CreatePermissionAsync($"{resource}x", "read");

        var byResource = await _client.GetFromJsonAsync<PagedResponse<PermissionResponse>>(
            $"/permissions?resource={resource}&page=2&pageSize=2", Ct);
        var byResourceAndAction = await _client.GetFromJsonAsync<PagedResponse<PermissionResponse>>(
            $"/permissions?resource={resource}&action=read", Ct);
        var partialResource = await _client.GetFromJsonAsync<PagedResponse<PermissionResponse>>(
            $"/permissions?resource={resource[..10]}", Ct);

        Assert.Equal(3, byResource!.TotalCount);
        Assert.Equal(2, byResource.TotalPages);
        Assert.Equal("read", Assert.Single(byResource.Items).Action); // ordenado por recurso e acao
        Assert.Equal($"{resource}:read", Assert.Single(byResourceAndAction!.Items).Name);
        Assert.Equal(0, partialResource!.TotalCount); // igualdade exata, nao "contem"
    }

    [Fact]
    public async Task List_ReturnsValidationProblem_ForInvalidPaging()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync("/permissions?page=0", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletesPermission_AndAllowsRecreatingTheSamePair()
    {
        await _client.AuthenticateAsNewUserAsync();
        var resource = UniqueResource();
        var permission = await _client.CreatePermissionAsync(resource, "read");

        var response = await _client.DeleteAsync($"/permissions/{permission.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/permissions/{permission.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/permissions/{permission.Id}", Ct)).StatusCode);
        var listed = await _client.GetFromJsonAsync<PagedResponse<PermissionResponse>>(
            $"/permissions?resource={resource}", Ct);
        Assert.Equal(0, listed!.TotalCount);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            var stored = await db.Permissions.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(p => p.Id == permission.Id, Ct);
            Assert.True(stored.IsDeleted);
            Assert.NotNull(stored.DeletedAt);
            Assert.Equal(stored.DeletedAt, stored.UpdatedAt);
        }

        // Unicidade vale so entre ativas: o mesmo par pode ser recriado apos a exclusao.
        var recreated = await _client.CreatePermissionAsync(resource, "read");
        Assert.NotEqual(permission.Id, recreated.Id);
    }
}
