using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Users;
using Authentication.Application.Users.List;
using Moq;

namespace Authentication.Application.Tests.Users.List;

public class ListUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly ListUsersQueryHandler _handler;

    public ListUsersQueryHandlerTests()
    {
        _handler = new ListUsersQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMappedPage()
    {
        var users = new[] { TestData.CreateUser(), TestData.CreateUser("outro@example.com", "11144477735") };
        _repository.Setup(r => r.ListAsync(2, 2, "fulano", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<User>(users, 2, 2, 5));

        var result = await _handler.HandleAsync(new ListUsersQuery(2, 2, "  fulano "), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var page = result.Value;
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(users.Select(u => u.Id), page.Items.Select(i => i.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_IgnoresBlankSearch(string? search)
    {
        _repository.Setup(r => r.ListAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<User>([], 1, 20, 0));

        var result = await _handler.HandleAsync(new ListUsersQuery(1, 20, search), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, ListUsersQuery.MaxPageSize + 1, "pageSize")]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidPaging(int page, int pageSize, string field)
    {
        var result = await _handler.HandleAsync(new ListUsersQuery(page, pageSize, null), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(field, result.Error.ValidationErrors.Keys);
    }
}
