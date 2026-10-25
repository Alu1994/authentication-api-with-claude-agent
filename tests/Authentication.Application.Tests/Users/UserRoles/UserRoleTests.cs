using Authentication.Application.Users;

namespace Authentication.Application.Tests.Users.UserRoles;

public class UserRoleTests
{
    [Fact]
    public void Create_LinksUserAndRole_WithTruncatedTimestamp()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var link = UserRole.Create(userId, roleId, TestData.Now.AddTicks(7));

        Assert.Equal(userId, link.UserId);
        Assert.Equal(roleId, link.RoleId);
        Assert.Equal(TestData.Now, link.CreatedAt);
    }
}
