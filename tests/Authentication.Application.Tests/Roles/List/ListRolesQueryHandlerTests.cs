using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Authentication.Application.Roles.List;
using Moq;

namespace Authentication.Application.Tests.Roles.List;

public class ListRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new(MockBehavior.Strict);
    private readonly ListRolesQueryHandler _handler;

    public ListRolesQueryHandlerTests()
    {
        _handler = new ListRolesQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMappedPage_WithTrimmedFilter()
    {
        var role = TestData.CreateRole();
        _repository.Setup(r => r.ListAsync(2, 10, "adm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<Role>([role], 2, 10, 11));

        var result = await _handler.HandleAsync(new ListRolesQuery(2, 10, " adm "), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Value.TotalCount);
        Assert.Equal(role.Id, Assert.Single(result.Value.Items).Id);
    }

    [Fact]
    public async Task HandleAsync_IgnoresBlankFilter_AndUsesDefaults()
    {
        _repository.Setup(r => r.ListAsync(Paging.DefaultPage, Paging.DefaultPageSize, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<Role>([], 1, 20, 0));

        var result = await _handler.HandleAsync(new ListRolesQuery(Name: "  "), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidPaging()
    {
        var result = await _handler.HandleAsync(new ListRolesQuery(0, 500), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(["page", "pageSize"], result.Error.ValidationErrors.Keys.Order());
    }
}
