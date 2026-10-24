using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Authentication.Application.Roles;
using Authentication.Application.Roles.RolePermissions.Replace;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.RolePermissions;

public class ReplaceRolePermissionsCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roles = new(MockBehavior.Strict);
    private readonly Mock<IPermissionRepository> _permissions = new(MockBehavior.Strict);
    private readonly ReplaceRolePermissionsCommandHandler _handler;
    private readonly Role _role = TestData.CreateRole();
    private readonly Guid _kept = Guid.NewGuid();
    private readonly Guid _removed = Guid.NewGuid();
    private readonly Guid _added = Guid.NewGuid();

    public ReplaceRolePermissionsCommandHandlerTests()
    {
        _handler = new ReplaceRolePermissionsCommandHandler(
            _roles.Object,
            _permissions.Object,
            TestData.CreateTimeProvider(),
            NullLogger<ReplaceRolePermissionsCommandHandler>.Instance);
    }

    private void SetupRoleWithLinks(params Guid[] linkedPermissionIds)
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_role);
        _roles.Setup(r => r.GetPermissionLinksAsync(_role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedPermissionIds.Select(id => RolePermission.Create(_role.Id, id, TestData.Now)).ToList());
    }

    [Fact]
    public async Task HandleAsync_AddsMissingLinks_AndRemovesTheOthers()
    {
        SetupRoleWithLinks(_kept, _removed);
        _permissions.Setup(p => p.GetActiveIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(_kept) && ids.Contains(_added)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _kept, _added });
        var added = new List<RolePermission>();
        var removed = new List<RolePermission>();
        _roles.Setup(r => r.AddPermissionLink(It.IsAny<RolePermission>())).Callback<RolePermission>(added.Add);
        _roles.Setup(r => r.RemovePermissionLink(It.IsAny<RolePermission>())).Callback<RolePermission>(removed.Add);
        _roles.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ReplaceRolePermissionsCommand(_role.Id, [_kept, _added, _added]), // duplicados sao ignorados
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(_added, Assert.Single(added).PermissionId);
        Assert.Equal(TestData.Now, added[0].CreatedAt);
        Assert.Equal(_removed, Assert.Single(removed).PermissionId);
        _roles.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_RemovesAllLinks()
    {
        SetupRoleWithLinks(_kept, _removed);
        var removed = new List<RolePermission>();
        _roles.Setup(r => r.RemovePermissionLink(It.IsAny<RolePermission>())).Callback<RolePermission>(removed.Add);
        _roles.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new ReplaceRolePermissionsCommand(_role.Id, []),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, removed.Count);
        _permissions.Verify(p => p.GetActiveIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndChangesNothing_WhenSomeIdIsUnknown()
    {
        SetupRoleWithLinks(_kept);
        _permissions.Setup(p => p.GetActiveIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _kept });

        var result = await _handler.HandleAsync(
            new ReplaceRolePermissionsCommand(_role.Id, [_kept, _added]),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        var message = Assert.Single(result.Error.ValidationErrors["permissionIds"]);
        Assert.Contains(_added.ToString(), message, StringComparison.Ordinal);
        Assert.DoesNotContain(_kept.ToString(), message, StringComparison.Ordinal);
        _roles.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenListIsMissing()
    {
        var result = await _handler.HandleAsync(
            new ReplaceRolePermissionsCommand(_role.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(["permissionIds"], result.Error!.ValidationErrors.Keys);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        _roles.Setup(r => r.GetByIdAsync(_role.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(
            new ReplaceRolePermissionsCommand(_role.Id, [_added]),
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }
}
