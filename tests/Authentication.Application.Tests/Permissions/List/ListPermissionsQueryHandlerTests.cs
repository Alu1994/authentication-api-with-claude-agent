using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Permissions;
using Authentication.Application.Permissions.List;
using Moq;

namespace Authentication.Application.Tests.Permissions.List;

public class ListPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _repository = new(MockBehavior.Strict);
    private readonly ListPermissionsQueryHandler _handler;

    public ListPermissionsQueryHandlerTests()
    {
        _handler = new ListPermissionsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMappedPage_WithTrimmedFilters()
    {
        var permission = TestData.CreatePermission();
        _repository.Setup(r => r.ListAsync(2, 10, "users", "read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<Permission>([permission], 2, 10, 11));

        var result = await _handler.HandleAsync(
            new ListPermissionsQuery(2, 10, " users ", " read "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(10, result.Value.PageSize);
        Assert.Equal(11, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(permission.Id, item.Id);
        Assert.Equal("users:read", item.Name);
    }

    [Fact]
    public async Task HandleAsync_IgnoresBlankFilters_AndUsesDefaults()
    {
        _repository.Setup(r => r.ListAsync(Paging.DefaultPage, Paging.DefaultPageSize, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<Permission>([], 1, 20, 0));

        var result = await _handler.HandleAsync(
            new ListPermissionsQuery(Resource: "  ", Action: ""),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidPaging(int page, int pageSize, string field)
    {
        var result = await _handler.HandleAsync(
            new ListPermissionsQuery(page, pageSize),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal([field], result.Error.ValidationErrors.Keys);
    }
}
