using Authentication.Application.Permissions;
using Authentication.Application.Permissions.Delete;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Permissions.Delete;

public class DeletePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repository = new(MockBehavior.Strict);
    private readonly DeletePermissionCommandHandler _handler;

    public DeletePermissionCommandHandlerTests()
    {
        _handler = new DeletePermissionCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<DeletePermissionCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesPermission()
    {
        var permission = TestData.CreatePermission();
        _repository.Setup(r => r.GetByIdAsync(permission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        _repository.Setup(r => r.IsLinkedToActiveRoleAsync(permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new DeletePermissionCommand(permission.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(permission.IsDeleted);
        Assert.Equal(TestData.Now, permission.DeletedAt);
        Assert.Equal(TestData.Now, permission.UpdatedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenPermissionIsLinkedToActiveRole()
    {
        var permission = TestData.CreatePermission();
        _repository.Setup(r => r.GetByIdAsync(permission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        _repository.Setup(r => r.IsLinkedToActiveRoleAsync(permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new DeletePermissionCommand(permission.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.InUse, result.Error);
        Assert.False(permission.IsDeleted);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Permission?)null);

        var result = await _handler.HandleAsync(new DeletePermissionCommand(id), TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.NotFound, result.Error);
    }
}
