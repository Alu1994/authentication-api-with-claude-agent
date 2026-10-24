using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Authentication.Application.Roles.Update;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.Update;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new(MockBehavior.Strict);
    private readonly UpdateRoleCommandHandler _handler;
    private readonly Role _role = TestData.CreateRole();

    public UpdateRoleCommandHandlerTests()
    {
        _handler = new UpdateRoleCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<UpdateRoleCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_UpdatesRole_AndSetsUpdatedAt()
    {
        _repository.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _repository.Setup(r => r.NameExistsAsync("suporte", _role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new UpdateRoleCommand(_role.Id, " Suporte ", "   "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Suporte", _role.Name);
        Assert.Null(_role.Description);
        Assert.Equal(TestData.Now, _role.UpdatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidName()
    {
        var result = await _handler.HandleAsync(
            new UpdateRoleCommand(_role.Id, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(["name"], result.Error.ValidationErrors.Keys);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _repository.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(
            new UpdateRoleCommand(_role.Id, "Suporte", null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenNameBelongsToAnotherRole()
    {
        _repository.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _repository.Setup(r => r.NameExistsAsync("suporte", _role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new UpdateRoleCommand(_role.Id, "Suporte", null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NameInUse, result.Error);
        Assert.Equal("Admin", _role.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenUniqueIndexIsViolatedConcurrently()
    {
        _repository.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _repository.Setup(r => r.NameExistsAsync("suporte", _role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("ix_roles_normalized_name", new Exception()));

        var result = await _handler.HandleAsync(
            new UpdateRoleCommand(_role.Id, "Suporte", null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NameInUse, result.Error);
    }
}
