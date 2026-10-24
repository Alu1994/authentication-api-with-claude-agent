using Authentication.Application.Users;
using Authentication.Application.Users.GetById;
using Moq;

namespace Authentication.Application.Tests.Users.GetById;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUser_WithoutPasswordHash()
    {
        var user = TestData.CreateUser();
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.HandleAsync(new GetUserByIdQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new UserResponse(user.Id, user.Name, user.Email, user.Cpf, user.Phone, user.BirthDate, user.CreatedAt),
            result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(new GetUserByIdQuery(id), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
