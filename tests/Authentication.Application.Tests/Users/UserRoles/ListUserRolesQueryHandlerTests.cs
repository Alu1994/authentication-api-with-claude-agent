using Authentication.Application.Roles;
using Authentication.Application.Users;
using Authentication.Application.Users.UserRoles.List;
using Moq;

namespace Authentication.Application.Tests.Users.UserRoles;

public class ListUserRolesQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly ListUserRolesQueryHandler _handler;
    private readonly User _user = TestData.CreateUser();

    public ListUserRolesQueryHandlerTests()
    {
        _handler = new ListUserRolesQueryHandler(_users.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsActiveRolesOfUser()
    {
        var role = TestData.CreateRole();
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(u => u.ListActiveRolesAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync([role]);

        var result = await _handler.HandleAsync(new ListUserRolesQuery(_user.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoleResponse.FromRole(role), Assert.Single(result.Value));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(new ListUserRolesQuery(_user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
