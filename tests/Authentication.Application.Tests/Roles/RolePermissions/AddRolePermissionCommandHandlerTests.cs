using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Roles.RolePermissions.Add;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.RolePermissions;

public class AddRolePermissionCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly Mock<IPermissionRepository> _permissions = new(MockBehavior.Strict);
    private readonly AddRolePermissionCommandHandler _handler;
    private readonly Role _role = TestData.CreateRole();
    private readonly Permission _permission = TestData.CreatePermission();

    public AddRolePermissionCommandHandlerTests()
    {
        _handler = new AddRolePermissionCommandHandler(
            _roles.Object,
            _permissions.Object,
            TestData.CreateTimeProvider(),
            NullLogger<AddRolePermissionCommandHandler>.Instance);
    }

    private AddRolePermissionCommand Command => new(_role.Id, _permission.Id);

    private void SetupExisting()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _permissions.Setup(p => p.GetByIdAsync(_permission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_permission);
    }

    [Fact]
    public async Task HandleAsync_LinksPermission()
    {
        SetupExisting();
        _roles.Setup(r => r.GetPermissionLinkAsync(_role.Id, _permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);
        RolePermission? added = null;
        _roles.Setup(r => r.AddPermissionLink(It.IsAny<RolePermission>())).Callback<RolePermission>(l => added = l);
        _roles.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(_role.Id, added.RoleId);
        Assert.Equal(_permission.Id, added.PermissionId);
        Assert.Equal(TestData.Now, added.CreatedAt);
        Assert.Null(_role.UpdatedAt); // vincular nao altera a role
        _roles.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_WhenAlreadyLinked()
    {
        SetupExisting();
        _roles.Setup(r => r.GetPermissionLinkAsync(_role.Id, _permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RolePermission.Create(_role.Id, _permission.Id, TestData.Now));

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _roles.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_WhenLinkIsCreatedConcurrently()
    {
        SetupExisting();
        _roles.Setup(r => r.GetPermissionLinkAsync(_role.Id, _permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);
        _roles.Setup(r => r.AddPermissionLink(It.IsAny<RolePermission>()));
        _roles.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("pk_role_permissions", new Exception()));

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _permissions.Setup(p => p.GetByIdAsync(_permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.NotFound, result.Error);
    }
}
