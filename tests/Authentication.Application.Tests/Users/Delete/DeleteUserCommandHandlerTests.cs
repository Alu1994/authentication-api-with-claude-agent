using Authentication.Application.Users;
using Authentication.Application.Users.Delete;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Users.Delete;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _handler = new DeleteUserCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<DeleteUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesUser()
    {
        var user = TestData.CreateUser();
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(new DeleteUserCommand(user.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsDeleted);
        Assert.Equal(TestData.Now, user.DeletedAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(new DeleteUserCommand(id), TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
