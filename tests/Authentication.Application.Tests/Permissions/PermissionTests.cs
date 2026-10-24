using Authentication.Application.Permissions;

namespace Authentication.Application.Tests.Permissions;

public class PermissionTests
{
    [Fact]
    public void Create_PopulatesAllFields_AndGeneratesId()
    {
        var permission = Permission.Create("users", "read", "Consulta usuarios", TestData.Now);

        Assert.NotEqual(Guid.Empty, permission.Id);
        Assert.Equal("users", permission.Resource);
        Assert.Equal("read", permission.Action);
        Assert.Equal("users:read", permission.Name);
        Assert.Equal("Consulta usuarios", permission.Description);
        Assert.Equal(TestData.Now, permission.CreatedAt);
        Assert.Null(permission.UpdatedAt);
        Assert.False(permission.IsDeleted);
        Assert.Null(permission.DeletedAt);
    }

    [Fact]
    public void Create_AcceptsMissingDescription()
    {
        var permission = Permission.Create("users", "read", null, TestData.Now);

        Assert.Null(permission.Description);
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
    {
        Assert.NotEqual(TestData.CreatePermission().Id, TestData.CreatePermission().Id);
    }

    [Fact]
    public void Delete_MarksAsDeleted_OnlyOnce_AndSetsUpdatedAtToDeletionTime()
    {
        var permission = TestData.CreatePermission();
        var firstDeletion = TestData.Now.AddHours(1);

        permission.Delete(firstDeletion);
        permission.Delete(firstDeletion.AddHours(1));

        Assert.True(permission.IsDeleted);
        Assert.Equal(firstDeletion, permission.DeletedAt);
        Assert.Equal(firstDeletion, permission.UpdatedAt);
    }

    [Fact]
    public void Timestamps_AreTruncatedToMicroseconds()
    {
        // 1 tick = 100ns; PostgreSQL armazena timestamps com precisao de microssegundos.
        var withTicks = TestData.Now.AddTicks(1234567);
        var expected = TestData.Now.AddTicks(1234560);

        var permission = Permission.Create("users", "read", null, withTicks);
        permission.Delete(withTicks);

        Assert.Equal(expected, permission.CreatedAt);
        Assert.Equal(expected, permission.DeletedAt);
        Assert.Equal(expected, permission.UpdatedAt);
    }
}
