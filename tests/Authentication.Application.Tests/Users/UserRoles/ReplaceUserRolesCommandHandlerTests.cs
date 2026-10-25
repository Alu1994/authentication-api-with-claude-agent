using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Authentication.Application.Users;
using Authentication.Application.Users.UserRoles.Replace;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.UserRoles;

public class ReplaceUserRolesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly ReplaceUserRolesCommandHandler _handler;
    private readonly User _user = TestData.CreateUser();
    private readonly Guid _kept = Guid.NewGuid();
    private readonly Guid _removed = Guid.NewGuid();
    private readonly Guid _added = Guid.NewGuid();

    public ReplaceUserRolesCommandHandlerTests()
    {
        _handler = new ReplaceUserRolesCommandHandler(
            _users.Object,
            _roles.Object,
            TestData.CreateTimeProvider(),
            NullLogger<ReplaceUserRolesCommandHandler>.Instance);
    }

    private void SetupUserWithLinks(params Guid[] roleIds)
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(u => u.GetRoleLinksAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleIds.Select(id => UserRole.Create(_user.Id, id, TestData.Now)).ToList());
    }

    [Fact]
    public async Task HandleAsync_AddsMissingLinks_AndRemovesTheOthers()
    {
        SetupUserWithLinks(_kept, _removed);
        _roles.Setup(r => r.GetActiveIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(_kept) && ids.Contains(_added)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _kept, _added });
        var added = new List<UserRole>();
        var removed = new List<UserRole>();
        _users.Setup(u => u.AddRoleLink(It.IsAny<UserRole>())).Callback<UserRole>(added.Add);
        _users.Setup(u => u.RemoveRoleLink(It.IsAny<UserRole>())).Callback<UserRole>(removed.Add);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ReplaceUserRolesCommand(_user.Id, [_kept, _added, _added]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(_added, Assert.Single(added).RoleId);
        Assert.Equal(TestData.Now, added[0].CreatedAt);
        Assert.Equal(_removed, Assert.Single(removed).RoleId);
        Assert.Null(_user.UpdatedAt);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_RemovesAllLinks()
    {
        SetupUserWithLinks(_kept, _removed);
        var removed = new List<UserRole>();
        _users.Setup(u => u.RemoveRoleLink(It.IsAny<UserRole>())).Callback<UserRole>(removed.Add);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ReplaceUserRolesCommand(_user.Id, []),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, removed.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndChangesNothing_WhenSomeIdIsUnknown()
    {
        SetupUserWithLinks(_kept);
        _roles.Setup(r => r.GetActiveIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _kept });

        var result = await _handler.HandleAsync(
            new ReplaceUserRolesCommand(_user.Id, [_kept, _added]),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        var message = Assert.Single(result.Error.ValidationErrors["roleIds"]);
        Assert.Contains(_added.ToString(), message, StringComparison.Ordinal);
        Assert.DoesNotContain(_kept.ToString(), message, StringComparison.Ordinal);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenListIsMissing()
    {
        var result = await _handler.HandleAsync(
            new ReplaceUserRolesCommand(_user.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(["roleIds"], result.Error!.ValidationErrors.Keys);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(
            new ReplaceUserRolesCommand(_user.Id, [_added]),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
