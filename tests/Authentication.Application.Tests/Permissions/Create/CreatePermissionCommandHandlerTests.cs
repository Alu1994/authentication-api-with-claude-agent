using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Authentication.Application.Permissions.Create;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Permissions.Create;

public class CreatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repository = new(MockBehavior.Strict);
    private readonly CreatePermissionCommandHandler _handler;

    public CreatePermissionCommandHandlerTests()
    {
        _handler = new CreatePermissionCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<CreatePermissionCommandHandler>.Instance);
    }

    private void SetupNotExisting() =>
        _repository.Setup(r => r.ExistsAsync("users", "read", It.IsAny<CancellationToken>())).ReturnsAsync(false);

    [Fact]
    public async Task HandleAsync_CreatesPermission_WithNormalizedData()
    {
        SetupNotExisting();
        Permission? added = null;
        _repository.Setup(r => r.Add(It.IsAny<Permission>())).Callback<Permission>(p => added = p);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new CreatePermissionCommand(" users ", " read ", "  Consulta usuarios  "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("users", added.Resource);
        Assert.Equal("read", added.Action);
        Assert.Equal("Consulta usuarios", added.Description);
        Assert.Equal(TestData.Now, added.CreatedAt);

        var response = result.Value;
        Assert.Equal(added.Id, response.Id);
        Assert.Equal("users:read", response.Name);
        Assert.Equal("users", response.Resource);
        Assert.Equal("read", response.Action);
        Assert.Equal("Consulta usuarios", response.Description);
        Assert.Equal(TestData.Now, response.CreatedAt);
        Assert.Null(response.UpdatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StoresBlankDescriptionAsNull()
    {
        SetupNotExisting();
        Permission? added = null;
        _repository.Setup(r => r.Add(It.IsAny<Permission>())).Callback<Permission>(p => added = p);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new CreatePermissionCommand("users", "read", "   "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Null(added!.Description);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidFields()
    {
        var command = new CreatePermissionCommand("Users", "", new string('a', 501));

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(["action", "description", "resource"], result.Error.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenFieldsAreMissing()
    {
        var result = await _handler.HandleAsync(
            new CreatePermissionCommand(null, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(["action", "resource"], result.Error!.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenPermissionAlreadyExists()
    {
        _repository.Setup(r => r.ExistsAsync("users", "read", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new CreatePermissionCommand("users", "read", null),
            TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.AlreadyExists, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenUniqueIndexIsViolatedConcurrently()
    {
        SetupNotExisting();
        _repository.Setup(r => r.Add(It.IsAny<Permission>()));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("ix_permissions_resource_action", new Exception()));

        var result = await _handler.HandleAsync(
            new CreatePermissionCommand("users", "read", null),
            TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.AlreadyExists, result.Error);
    }
}
