using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Auth;
using Authentication.Application.Auth.Login;
using Authentication.Application.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _hasher = new(MockBehavior.Strict);
    private readonly Mock<ITokenService> _tokenService = new(MockBehavior.Strict);
    private readonly LoginCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();
    private readonly AccessToken _token = new("jwt-token", TestData.Now.AddHours(1));
    private readonly UserAuthorization _authorization = new(["Admin"], ["users:read", "users:update"]);

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _repository.Object,
            _hasher.Object,
            _tokenService.Object,
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ReturnsToken_ForValidCredentials()
    {
        _repository.Setup(r => r.GetByEmailAsync(TestData.ValidEmail, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, TestData.ValidPassword)).Returns(PasswordVerification.Success);
        _repository.Setup(r => r.GetAuthorizationAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_authorization);
        _tokenService.Setup(t => t.CreateToken(_user, _authorization)).Returns(_token);

        var result = await _handler.HandleAsync(
            new LoginCommand(" Fulano@Example.com ", TestData.ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(new LoginResponse("jwt-token", "Bearer", _token.ExpiresAt), result.Value);
        _tokenService.Verify(t => t.CreateToken(_user, _authorization), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RehashesPassword_WhenHasherRequestsIt()
    {
        _repository.Setup(r => r.GetByEmailAsync(TestData.ValidEmail, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, TestData.ValidPassword))
            .Returns(PasswordVerification.SuccessRehashNeeded);
        _hasher.Setup(h => h.Hash(TestData.ValidPassword)).Returns("hash-atualizado");
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetAuthorizationAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_authorization);
        _tokenService.Setup(t => t.CreateToken(_user, _authorization)).Returns(_token);

        var result = await _handler.HandleAsync(
            new LoginCommand(TestData.ValidEmail, TestData.ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("hash-atualizado", _user.PasswordHash);
        Assert.Null(_user.UpdatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        _repository.Setup(r => r.GetByEmailAsync(TestData.ValidEmail, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _hasher.Setup(h => h.SimulateVerification(TestData.ValidPassword));

        var result = await _handler.HandleAsync(
            new LoginCommand(TestData.ValidEmail, TestData.ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task HandleAsync_SimulatesPasswordVerification_WhenUserDoesNotExist()
    {
        // Mitiga enumeracao de contas por timing: o caminho "e-mail inexistente" tambem paga o custo do hash.
        _repository.Setup(r => r.GetByEmailAsync(TestData.ValidEmail, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _hasher.Setup(h => h.SimulateVerification(TestData.ValidPassword));

        await _handler.HandleAsync(
            new LoginCommand(TestData.ValidEmail, TestData.ValidPassword),
            TestContext.Current.CancellationToken);

        _hasher.Verify(h => h.SimulateVerification(TestData.ValidPassword), Times.Once);
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenService.Verify(t => t.CreateToken(It.IsAny<User>(), It.IsAny<UserAuthorization>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        _repository.Setup(r => r.GetByEmailAsync(TestData.ValidEmail, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, "Errada123")).Returns(PasswordVerification.Failed);

        var result = await _handler.HandleAsync(
            new LoginCommand(TestData.ValidEmail, "Errada123"),
            TestContext.Current.CancellationToken);

        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "Senha123")]
    public async Task HandleAsync_ReturnsValidationError_WhenCredentialsAreMissing(string? email, string? password)
    {
        var result = await _handler.HandleAsync(new LoginCommand(email, password), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("email", result.Error.ValidationErrors.Keys);
    }
}
