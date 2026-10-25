using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Roles;
using Authentication.Application.Users;
using Authentication.Application.Users.UserRoles.Add;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.UserRoles;

public class AddUserRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly AddUserRoleCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();
    private readonly Role _role = TestData.CreateRole();

    public AddUserRoleCommandHandlerTests()
    {
        _handler = new AddUserRoleCommandHandler(
            _users.Object,
            _roles.Object,
            TestData.CreateTimeProvider(),
            NullLogger<AddUserRoleCommandHandler>.Instance);
    }

    private AddUserRoleCommand Command => new(_user.Id, _role.Id);

    private void SetupExisting()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
    }

    [Fact]
    public async Task HandleAsync_AssignsRole_WithoutChangingUserUpdatedAt()
    {
        SetupExisting();
        _users.Setup(u => u.GetRoleLinkAsync(_user.Id, _role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole?)null);
        UserRole? added = null;
        _users.Setup(u => u.AddRoleLink(It.IsAny<UserRole>())).Callback<UserRole>(l => added = l);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(_user.Id, added.UserId);
        Assert.Equal(_role.Id, added.RoleId);
        Assert.Equal(TestData.Now, added.CreatedAt);
        Assert.Null(_user.UpdatedAt);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_WhenAlreadyAssigned()
    {
        SetupExisting();
        _users.Setup(u => u.GetRoleLinkAsync(_user.Id, _role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserRole.Create(_user.Id, _role.Id, TestData.Now));

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_WhenLinkIsCreatedConcurrently()
    {
        SetupExisting();
        _users.Setup(u => u.GetRoleLinkAsync(_user.Id, _role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole?)null);
        _users.Setup(u => u.AddRoleLink(It.IsAny<UserRole>()));
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("pk_user_roles", new Exception()));

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(Command, TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }
}
