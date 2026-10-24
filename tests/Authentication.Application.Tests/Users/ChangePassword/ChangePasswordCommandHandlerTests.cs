using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Authentication.Application.Users.ChangePassword;
using Authentication.Application.Users.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    private const string NewPassword = "NovaSenha456";
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _hasher = new(MockBehavior.Strict);
    private readonly ChangePasswordCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(
            _repository.Object,
            _hasher.Object,
            TestData.CreateTimeProvider(),
            NullLogger<ChangePasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ReplacesHash_WhenCurrentPasswordMatches()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, TestData.ValidPassword)).Returns(PasswordVerification.Success);
        _hasher.Setup(h => h.Hash(NewPassword)).Returns("novo-hash");
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, TestData.ValidPassword, NewPassword),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("novo-hash", _user.PasswordHash);
        Assert.Equal(TestData.Now, _user.UpdatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AcceptsCurrentPasswordThatNeedsRehash()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, TestData.ValidPassword))
            .Returns(PasswordVerification.SuccessRehashNeeded);
        _hasher.Setup(h => h.Hash(NewPassword)).Returns("novo-hash");
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, TestData.ValidPassword, NewPassword),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenCurrentPasswordIsWrong()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify(TestData.PasswordHash, "Errada123")).Returns(PasswordVerification.Failed);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, "Errada123", NewPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("currentPassword", result.Error.ValidationErrors.Keys);
        Assert.Equal(TestData.PasswordHash, _user.PasswordHash);
        Assert.Null(_user.UpdatedAt);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenNewPasswordViolatesPolicy()
    {
        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, TestData.ValidPassword, "fraca"),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(PasswordPolicy.MinLengthMessage, result.Error.ValidationErrors["newPassword"]);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenPasswordsAreMissing()
    {
        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(["currentPassword", "newPassword"], result.Error!.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenNewPasswordEqualsCurrent()
    {
        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, TestData.ValidPassword, TestData.ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Contains("newPassword", result.Error!.ValidationErrors.Keys);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(_user.Id, TestData.ValidPassword, NewPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
