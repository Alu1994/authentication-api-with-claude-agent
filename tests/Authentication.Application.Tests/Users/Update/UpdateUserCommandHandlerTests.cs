using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Users;
using Authentication.Application.Users.Update;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.Update;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly UpdateUserCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();

    public UpdateUserCommandHandlerTests()
    {
        _handler = new UpdateUserCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<UpdateUserCommandHandler>.Instance);
    }

    private UpdateUserCommand ValidCommand() =>
        new(_user.Id, " Beltrano ", " Beltrano@Example.com ", "(11) 3333-4444", new DateOnly(1985, 1, 1));

    [Fact]
    public async Task HandleAsync_UpdatesProfile_WithNormalizedData()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _repository.Setup(r => r.EmailExistsAsync("beltrano@example.com", _user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Beltrano", _user.Name);
        Assert.Equal("beltrano@example.com", _user.Email);
        Assert.Equal("1133334444", _user.Phone);
        Assert.Equal(new DateOnly(1985, 1, 1), _user.BirthDate);
        Assert.Equal(TestData.ValidCpf, _user.Cpf);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidFields()
    {
        var command = new UpdateUserCommand(_user.Id, "", "email-invalido", "0", new DateOnly(2100, 1, 1));

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(["birthDate", "email", "name", "phone"], result.Error.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenEmailBelongsToAnotherUser()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _repository.Setup(r => r.EmailExistsAsync("beltrano@example.com", _user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.EmailAlreadyInUse, result.Error);
        Assert.Equal(TestData.ValidEmail, _user.Email);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenDatabaseDetectsDuplicate()
    {
        _repository.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _repository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), _user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("ix_users_email", new InvalidOperationException()));

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.Duplicate, result.Error);
    }
}
