using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Abstractions.Security;
using Authentication.Application.Users;
using Authentication.Application.Users.Create;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.Create;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _hasher = new(MockBehavior.Strict);
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(
            _repository.Object,
            _hasher.Object,
            TestData.CreateTimeProvider(),
            NullLogger<CreateUserCommandHandler>.Instance);
    }

    private static CreateUserCommand ValidCommand() => new(
        "  Fulano de Tal ",
        " Fulano@Example.com ",
        "529.982.247-25",
        "(11) 98765-4321",
        TestData.ValidBirthDate,
        TestData.ValidPassword);

    private void SetupNoDuplicates()
    {
        _repository.Setup(r => r.EmailExistsAsync(TestData.ValidEmail, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.CpfExistsAsync(TestData.ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task HandleAsync_CreatesUser_WithNormalizedDataAndHashedPassword()
    {
        SetupNoDuplicates();
        _hasher.Setup(h => h.Hash(TestData.ValidPassword)).Returns(TestData.PasswordHash);
        User? added = null;
        _repository.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => added = u);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("Fulano de Tal", added.Name);
        Assert.Equal(TestData.ValidEmail, added.Email);
        Assert.Equal(TestData.ValidCpf, added.Cpf);
        Assert.Equal(TestData.ValidPhone, added.Phone);
        Assert.Equal(TestData.PasswordHash, added.PasswordHash);
        Assert.Equal(TestData.Now, added.CreatedAt);

        var response = result.Value;
        Assert.Equal(added.Id, response.Id);
        Assert.Equal(TestData.ValidEmail, response.Email);
        Assert.Equal(TestData.Now, response.CreatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        _repository.Setup(r => r.EmailExistsAsync(TestData.ValidEmail, null, token)).ReturnsAsync(false);
        _repository.Setup(r => r.CpfExistsAsync(TestData.ValidCpf, token)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(TestData.PasswordHash);
        _repository.Setup(r => r.Add(It.IsAny<User>()));
        _repository.Setup(r => r.SaveChangesAsync(token)).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(ValidCommand(), token);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForEveryInvalidField()
    {
        var command = new CreateUserCommand("Al", "invalido", "12345678900", "123", null, "curta");

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        var errors = result.Error.ValidationErrors;
        Assert.Contains("name", errors.Keys);
        Assert.Contains("email", errors.Keys);
        Assert.Contains("cpf", errors.Keys);
        Assert.Contains("phone", errors.Keys);
        Assert.Contains("birthDate", errors.Keys);
        Assert.Contains("password", errors.Keys);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenAllFieldsAreMissing()
    {
        var command = new CreateUserCommand(null, null, null, null, null, null);

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(6, result.Error.ValidationErrors.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenEmailAlreadyExists()
    {
        _repository.Setup(r => r.EmailExistsAsync(TestData.ValidEmail, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.EmailAlreadyInUse, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenCpfAlreadyExists()
    {
        _repository.Setup(r => r.EmailExistsAsync(TestData.ValidEmail, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.CpfExistsAsync(TestData.ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.CpfAlreadyInUse, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenDatabaseDetectsDuplicate()
    {
        SetupNoDuplicates();
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(TestData.PasswordHash);
        _repository.Setup(r => r.Add(It.IsAny<User>()));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("ix_users_email", new InvalidOperationException()));

        var result = await _handler.HandleAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.Duplicate, result.Error);
    }
}
