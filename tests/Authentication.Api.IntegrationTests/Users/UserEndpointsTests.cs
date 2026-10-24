using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authentication.Api.IntegrationTests.Infrastructure;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Users;
using Microsoft.AspNetCore.Mvc;
using static Authentication.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Authentication.Api.IntegrationTests.Users;

[Collection(ApiCollection.Name)]
public class UserEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ReturnsCreated_WithNormalizedUserAndLocation()
    {
        var email = UniqueEmail();
        var cpf = UniqueCpf();
        var maskedCpf = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";

        var response = await _client.PostAsJsonAsync("/users", NewUserPayload(email.ToUpperInvariant(), maskedCpf), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(Ct);
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(cpf, user.Cpf);
        Assert.Equal("11987654321", user.Phone);
        Assert.Equal(new DateOnly(1990, 5, 10), user.BirthDate);
        Assert.Equal($"/users/{user.Id}", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_ReturnsValidationProblem_ForInvalidPayload()
    {
        var response = await _client.PostAsJsonAsync("/users", new
        {
            name = "Al",
            email = "invalido",
            cpf = "11111111111",
            phone = "123",
            password = "curta",
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Equal(["birthDate", "cpf", "email", "name", "password", "phone"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Register_ReturnsConflict_ForDuplicateEmailOrCpf()
    {
        var existing = await _client.RegisterAsync();

        var duplicateEmail = await _client.PostAsJsonAsync("/users", NewUserPayload(email: existing.Email), Ct);
        var duplicateCpf = await _client.PostAsJsonAsync("/users", NewUserPayload(cpf: existing.Cpf), Ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicateEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateCpf.StatusCode);
        var problem = await duplicateEmail.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(UserErrors.EmailAlreadyInUse.Message, problem!.Detail);
    }

    [Theory]
    [InlineData("GET", "/users")]
    [InlineData("GET", "/users/0198a3c4-0000-7000-8000-000000000001")]
    [InlineData("PUT", "/users/0198a3c4-0000-7000-8000-000000000001")]
    [InlineData("PUT", "/users/0198a3c4-0000-7000-8000-000000000001/password")]
    [InlineData("DELETE", "/users/0198a3c4-0000-7000-8000-000000000001")]
    public async Task ProtectedEndpoints_ReturnUnauthorized_WithoutToken(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "PUT" ? JsonContent.Create(new { }) : null,
        };

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_ReturnUnauthorized_WithInvalidToken()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await _client.GetAsync("/users", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsUser_WhenAuthenticated()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}", Ct);

        Assert.Equal(user, response);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownUser()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync($"/users/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(UserErrors.NotFound.Message, problem!.Detail);
    }

    [Fact]
    public async Task List_ReturnsPagedResults_FilteredBySearch()
    {
        await _client.AuthenticateAsNewUserAsync();
        var marker = Guid.NewGuid().ToString("N")[..10];
        await _client.RegisterAsync(NewUserPayload(name: $"Busca {marker} A"));
        await _client.RegisterAsync(NewUserPayload(name: $"Busca {marker} B"));
        await _client.RegisterAsync(NewUserPayload(name: $"Busca {marker} C"));

        var page = await _client.GetFromJsonAsync<PagedResponse<UserResponse>>(
            $"/users?search={marker}&page=2&pageSize=2", Ct);

        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        var item = Assert.Single(page.Items);
        Assert.Equal($"Busca {marker} C", item.Name);
    }

    [Fact]
    public async Task List_ReturnsValidationProblem_ForInvalidPaging()
    {
        await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync("/users?pageSize=1000", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesProfile_AndKeepsCpf()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var newEmail = UniqueEmail();

        var response = await _client.PutAsJsonAsync($"/users/{user.Id}", new
        {
            name = "Nome Atualizado",
            email = newEmail,
            phone = "1133334444",
            birthDate = "1985-01-01",
            cpf = "52998224725",
        }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await _client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}", Ct);
        Assert.Equal("Nome Atualizado", updated!.Name);
        Assert.Equal(newEmail, updated.Email);
        Assert.Equal("1133334444", updated.Phone);
        Assert.Equal(new DateOnly(1985, 1, 1), updated.BirthDate);
        Assert.Equal(user.Cpf, updated.Cpf);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenEmailBelongsToAnotherUser()
    {
        var user = await _client.AuthenticateAsNewUserAsync();
        var other = await _client.RegisterAsync();

        var response = await _client.PutAsJsonAsync($"/users/{user.Id}", new
        {
            name = user.Name,
            email = other.Email,
            phone = user.Phone,
            birthDate = user.BirthDate,
        }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownUser()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.PutAsJsonAsync($"/users/{Guid.NewGuid()}", new
        {
            name = user.Name,
            email = UniqueEmail(),
            phone = user.Phone,
            birthDate = user.BirthDate,
        }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReplacesPassword()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.PutAsJsonAsync($"/users/{user.Id}/password", new
        {
            currentPassword = DefaultPassword,
            newPassword = "NovaSenha456",
        }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var oldLogin = await _client.PostAsJsonAsync("/auth/login", new { email = user.Email, password = DefaultPassword }, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.False(string.IsNullOrEmpty(await _client.LoginAsync(user.Email, "NovaSenha456")));
    }

    [Fact]
    public async Task ChangePassword_ReturnsValidationProblem_WhenCurrentPasswordIsWrong()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.PutAsJsonAsync($"/users/{user.Id}/password", new
        {
            currentPassword = "Errada123",
            newPassword = "NovaSenha456",
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains("currentPassword", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Delete_SoftDeletesUser_BlockingLoginAndReuseOfEmailAndCpf()
    {
        var user = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.DeleteAsync($"/users/{user.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/users/{user.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/users/{user.Id}", Ct)).StatusCode);

        var login = await _client.PostAsJsonAsync("/auth/login", new { email = user.Email, password = DefaultPassword }, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var reuseEmail = await _client.PostAsJsonAsync("/users", NewUserPayload(email: user.Email), Ct);
        var reuseCpf = await _client.PostAsJsonAsync("/users", NewUserPayload(cpf: user.Cpf), Ct);
        Assert.Equal(HttpStatusCode.Conflict, reuseEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reuseCpf.StatusCode);
    }
}
