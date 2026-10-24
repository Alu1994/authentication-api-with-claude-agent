using Authentication.Application.Roles;
using Authentication.Application.Roles.RolePermissions.Remove;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.RolePermissions;

public class RemoveRolePermissionCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly RemoveRolePermissionCommandHandler _handler;
    private readonly Role _role = TestData.CreateRole();
    private readonly Guid _permissionId = Guid.NewGuid();

    public RemoveRolePermissionCommandHandlerTests()
    {
        _handler = new RemoveRolePermissionCommandHandler(
            _roles.Object,
            NullLogger<RemoveRolePermissionCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_RemovesLink()
    {
        var link = RolePermission.Create(_role.Id, _permissionId, TestData.Now);
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _roles.Setup(r => r.GetPermissionLinkAsync(_role.Id, _permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        _roles.Setup(r => r.RemovePermissionLink(link));
        _roles.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new RemoveRolePermissionCommand(_role.Id, _permissionId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _roles.Verify(r => r.RemovePermissionLink(link), Times.Once);
        _roles.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(
            new RemoveRolePermissionCommand(_role.Id, _permissionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _roles.Setup(r => r.GetPermissionLinkAsync(_role.Id, _permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);

        var result = await _handler.HandleAsync(
            new RemoveRolePermissionCommand(_role.Id, _permissionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.PermissionNotLinked, result.Error);
    }
}
