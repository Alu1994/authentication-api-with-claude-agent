using Authentication.Application.Roles;

namespace Authentication.Application.Tests.Roles;

public class RoleTests
{
    [Fact]
    public void Create_PopulatesAllFields_AndGeneratesId()
    {
        var role = Role.Create("Admin Global", "Acesso total", TestData.Now);

        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Admin Global", role.Name);
        Assert.Equal("admin global", role.NormalizedName);
        Assert.Equal("Acesso total", role.Description);
        Assert.Equal(TestData.Now, role.CreatedAt);
        Assert.Null(role.UpdatedAt);
        Assert.False(role.IsDeleted);
        Assert.Null(role.DeletedAt);
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
    {
        Assert.NotEqual(TestData.CreateRole().Id, TestData.CreateRole().Id);
    }

    [Fact]
    public void Update_ChangesNameAndDescription_AndSetsUpdatedAt()
    {
        var role = TestData.CreateRole();
        var updatedAt = TestData.Now.AddHours(1);

        role.Update("Suporte N2", null, updatedAt);

        Assert.Equal("Suporte N2", role.Name);
        Assert.Equal("suporte n2", role.NormalizedName);
        Assert.Null(role.Description);
        Assert.Equal(updatedAt, role.UpdatedAt);
        Assert.Equal(TestData.Now, role.CreatedAt);
    }

    [Fact]
    public void Delete_MarksAsDeleted_OnlyOnce_AndSetsUpdatedAtToDeletionTime()
    {
        var role = TestData.CreateRole();
        var firstDeletion = TestData.Now.AddHours(1);

        role.Delete(firstDeletion);
        role.Delete(firstDeletion.AddHours(1));

        Assert.True(role.IsDeleted);
        Assert.Equal(firstDeletion, role.DeletedAt);
        Assert.Equal(firstDeletion, role.UpdatedAt);
    }

    [Fact]
    public void Timestamps_AreTruncatedToMicroseconds()
    {
        var withTicks = TestData.Now.AddTicks(1234567);
        var expected = TestData.Now.AddTicks(1234560);

        var role = Role.Create("Admin", null, withTicks);
        var updated = TestData.CreateRole();
        updated.Update("Outro", null, withTicks);
        role.Delete(withTicks);

        Assert.Equal(expected, role.CreatedAt);
        Assert.Equal(expected, role.DeletedAt);
        Assert.Equal(expected, role.UpdatedAt);
        Assert.Equal(expected, updated.UpdatedAt);
    }

    [Fact]
    public void RolePermission_Create_LinksRoleAndPermission()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var link = RolePermission.Create(roleId, permissionId, TestData.Now.AddTicks(7));

        Assert.Equal(roleId, link.RoleId);
        Assert.Equal(permissionId, link.PermissionId);
        Assert.Equal(TestData.Now, link.CreatedAt);
    }
}
