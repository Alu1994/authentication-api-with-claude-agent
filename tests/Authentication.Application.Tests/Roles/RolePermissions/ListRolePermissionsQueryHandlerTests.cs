using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Roles.RolePermissions.List;
using Moq;

namespace Authentication.Application.Tests.Roles.RolePermissions;

public class ListRolePermissionsQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly ListRolePermissionsQueryHandler _handler;
    private readonly Role _role = TestData.CreateRole();

    public ListRolePermissionsQueryHandlerTests()
    {
        _handler = new ListRolePermissionsQueryHandler(_roles.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsActivePermissionsOfRole()
    {
        var permission = TestData.CreatePermission();
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _roles.Setup(r => r.ListActivePermissionsAsync(_role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([permission]);

        var result = await _handler.HandleAsync(
            new ListRolePermissionsQuery(_role.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(PermissionResponse.FromPermission(permission), Assert.Single(result.Value));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(
            new ListRolePermissionsQuery(_role.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }
}
