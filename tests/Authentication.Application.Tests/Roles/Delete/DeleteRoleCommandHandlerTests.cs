using Authentication.Application.Roles;
using Authentication.Application.Roles.Delete;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.Delete;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new(MockBehavior.Strict);
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _handler = new DeleteRoleCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<DeleteRoleCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesRole()
    {
        var role = TestData.CreateRole();
        _repository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _repository.Setup(r => r.IsAssignedToActiveUserAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(role.IsDeleted);
        Assert.Equal(TestData.Now, role.DeletedAt);
        Assert.Equal(TestData.Now, role.UpdatedAt);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenRoleIsAssignedToActiveUser()
    {
        var role = TestData.CreateRole();
        _repository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _repository.Setup(r => r.IsAssignedToActiveUserAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.InUse, result.Error);
        Assert.False(role.IsDeleted);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(new DeleteRoleCommand(id), TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }
}
