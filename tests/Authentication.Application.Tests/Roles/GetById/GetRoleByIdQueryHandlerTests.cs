using Authentication.Application.Roles;
using Authentication.Application.Roles.GetById;
using Moq;

namespace Authentication.Application.Tests.Roles.GetById;

public class GetRoleByIdQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new(MockBehavior.Strict);
    private readonly GetRoleByIdQueryHandler _handler;

    public GetRoleByIdQueryHandlerTests()
    {
        _handler = new GetRoleByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRole()
    {
        var role = TestData.CreateRole();
        _repository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        var result = await _handler.HandleAsync(new GetRoleByIdQuery(role.Id), TestContext.Current.CancellationToken);

        Assert.Equal(new RoleResponse(role.Id, "Admin", "Descricao", TestData.Now, null), result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await _handler.HandleAsync(new GetRoleByIdQuery(id), TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NotFound, result.Error);
    }
}
