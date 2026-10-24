using System.Net;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class LoginRateLimitingTests(ApiFactory factory)
{
    private const int PermitLimit = 3;

    private HttpClient CreateLimitedClient() =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Login:PermitLimit", PermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:Login:WindowSeconds", "60");
        }).CreateClient();

    [Fact]
    public async Task Login_ReturnsTooManyRequests_AfterPermitLimitIsExceeded()
    {
        using var client = CreateLimitedClient();
        var credentials = new { email = UniqueEmail(), password = "Errada123" };

        for (var attempt = 0; attempt < PermitLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/auth/login", credentials, Ct);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var rejected = await client.PostAsJsonAsync("/auth/login", credentials, Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem!.Status);
    }

    [Fact]
    public async Task RateLimit_AppliesOnlyToLogin()
    {
        using var client = CreateLimitedClient();
        for (var attempt = 0; attempt <= PermitLimit; attempt++)
        {
            await client.PostAsJsonAsync("/auth/login", new { email = UniqueEmail(), password = "Errada123" }, Ct);
        }

        for (var attempt = 0; attempt <= PermitLimit; attempt++)
        {
            var register = await client.PostAsJsonAsync("/users", NewUserPayload(), Ct);
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/heartbeat", Ct)).StatusCode);
    }
}
