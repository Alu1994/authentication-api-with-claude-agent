using Authentication.Application.Permissions;
using Authentication.Application.Permissions.GetById;
using Moq;

namespace Authentication.Application.Tests.Permissions.GetById;

public class GetPermissionByIdQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _repository = new(MockBehavior.Strict);
    private readonly GetPermissionByIdQueryHandler _handler;

    public GetPermissionByIdQueryHandlerTests()
    {
        _handler = new GetPermissionByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPermission()
    {
        var permission = TestData.CreatePermission();
        _repository.Setup(r => r.GetByIdAsync(permission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(permission);

        var result = await _handler.HandleAsync(
            new GetPermissionByIdQuery(permission.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new PermissionResponse(
                permission.Id,
                permission.Resource,
                permission.Action,
                "users:read",
                permission.Description,
                permission.CreatedAt,
                permission.UpdatedAt),
            result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Permission?)null);

        var result = await _handler.HandleAsync(new GetPermissionByIdQuery(id), TestContext.Current.CancellationToken);

        Assert.Equal(PermissionErrors.NotFound, result.Error);
    }
}
