using Authentication.Application.Abstractions.Persistence;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Roles;
using Authentication.Application.Roles.Create;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Authentication.Application.Tests.Roles.Create;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new(MockBehavior.Strict);
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _handler = new CreateRoleCommandHandler(
            _repository.Object,
            TestData.CreateTimeProvider(),
            NullLogger<CreateRoleCommandHandler>.Instance);
    }

    private void SetupNameAvailable() =>
        _repository.Setup(r => r.NameExistsAsync("admin", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

    [Fact]
    public async Task HandleAsync_CreatesRole_WithNormalizedData()
    {
        SetupNameAvailable();
        Role? added = null;
        _repository.Setup(r => r.Add(It.IsAny<Role>())).Callback<Role>(r => added = r);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(
            new CreateRoleCommand("  Admin ", "  Acesso total "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("Admin", added.Name);
        Assert.Equal("Acesso total", added.Description);
        Assert.Equal(TestData.Now, added.CreatedAt);
        Assert.Equal(new RoleResponse(added.Id, "Admin", "Acesso total", TestData.Now, null), result.Value);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_ForInvalidFields()
    {
        var result = await _handler.HandleAsync(
            new CreateRoleCommand(" a ", new string('d', 501)),
            TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(["description", "name"], result.Error.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenNameIsInUse_IgnoringCase()
    {
        _repository.Setup(r => r.NameExistsAsync("admin", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.HandleAsync(new CreateRoleCommand("ADMIN", null), TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NameInUse, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenUniqueIndexIsViolatedConcurrently()
    {
        SetupNameAvailable();
        _repository.Setup(r => r.Add(It.IsAny<Role>()));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("ix_roles_normalized_name", new Exception()));

        var result = await _handler.HandleAsync(new CreateRoleCommand("Admin", null), TestContext.Current.CancellationToken);

        Assert.Equal(RoleErrors.NameInUse, result.Error);
    }
}
