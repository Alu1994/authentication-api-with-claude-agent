using Authentication.Application.Users;
using Authentication.Application.Users.UserRoles.Remove;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.UserRoles;

public class RemoveUserRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly RemoveUserRoleCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();
    private readonly Guid _roleId = Guid.NewGuid();

    public RemoveUserRoleCommandHandlerTests()
    {
        _handler = new RemoveUserRoleCommandHandler(_users.Object, NullLogger<RemoveUserRoleCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_RemovesLink()
    {
        var link = UserRole.Create(_user.Id, _roleId, TestData.Now);
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(u => u.GetRoleLinkAsync(_user.Id, _roleId, It.IsAny<CancellationToken>())).ReturnsAsync(link);
        _users.Setup(u => u.RemoveRoleLink(link));
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new RemoveUserRoleCommand(_user.Id, _roleId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _users.Verify(u => u.RemoveRoleLink(link), Times.Once);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(
            new RemoveUserRoleCommand(_user.Id, _roleId),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(u => u.GetRoleLinkAsync(_user.Id, _roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole?)null);

        var result = await _handler.HandleAsync(
            new RemoveUserRoleCommand(_user.Id, _roleId),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.RoleNotLinked, result.Error);
    }
}
