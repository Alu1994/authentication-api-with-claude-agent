using System.Net;
using System.Text.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Platform;

[Collection(ApiCollection.Name)]
public class PlatformEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Heartbeat_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/heartbeat", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task HealthCheck_ReportsDatabaseStatus()
    {
        var response = await _client.GetAsync("/healthcheck", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("Healthy", json.RootElement.GetProperty("entries").GetProperty("postgresql").GetProperty("status").GetString());
    }

    [Fact]
    public async Task OpenApiDocument_DescribesEndpointsAndBearerScheme()
    {
        var response = await _client.GetAsync("/openapi/v1.json", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/auth/login", out _));
        Assert.True(paths.TryGetProperty("/users", out _));
        Assert.True(paths.TryGetProperty("/users/{id}", out _));
        Assert.True(paths.TryGetProperty("/users/{id}/password", out _));
        Assert.False(paths.TryGetProperty("/heartbeat", out _));

        var schemes = json.RootElement.GetProperty("components").GetProperty("securitySchemes");
        Assert.Equal("bearer", schemes.GetProperty("Bearer").GetProperty("scheme").GetString());

        var getUser = paths.GetProperty("/users/{id}").GetProperty("get");
        Assert.True(getUser.TryGetProperty("security", out _));
        Assert.True(getUser.GetProperty("responses").TryGetProperty("404", out _));
        var login = paths.GetProperty("/auth/login").GetProperty("post");
        Assert.False(login.TryGetProperty("security", out _));
        Assert.True(login.GetProperty("responses").TryGetProperty("429", out _));

        var userProperties = json.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("UserResponse").GetProperty("properties");
        Assert.True(userProperties.TryGetProperty("createdAt", out _));
        Assert.True(userProperties.TryGetProperty("updatedAt", out var updatedAt));
        Assert.False(string.IsNullOrWhiteSpace(updatedAt.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task ScalarUi_IsAvailable()
    {
        var response = await _client.GetAsync("/scalar/v1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
