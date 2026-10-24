using Authentication.Application.Users;

namespace Authentication.Application.Tests.Users;

public class UserTests
{
    [Fact]
    public void Create_PopulatesAllFields_AndGeneratesId()
    {
        var user = User.Create("Fulano", "fulano@example.com", "52998224725", "11987654321",
            new DateOnly(1990, 5, 10), "hash", TestData.Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Fulano", user.Name);
        Assert.Equal("fulano@example.com", user.Email);
        Assert.Equal("52998224725", user.Cpf);
        Assert.Equal("11987654321", user.Phone);
        Assert.Equal(new DateOnly(1990, 5, 10), user.BirthDate);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(TestData.Now, user.CreatedAt);
        Assert.False(user.IsDeleted);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public void CreateAndDelete_TruncateTimestampsToMicroseconds()
    {
        // 1 tick = 100ns; PostgreSQL armazena timestamps com precisao de microssegundos.
        var withTicks = TestData.Now.AddTicks(1234567);
        var expected = TestData.Now.AddTicks(1234560);

        var user = User.Create("Fulano", "fulano@example.com", "52998224725", "11987654321",
            new DateOnly(1990, 5, 10), "hash", withTicks);
        user.Delete(withTicks);

        Assert.Equal(expected, user.CreatedAt);
        Assert.Equal(expected, user.DeletedAt);
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
    {
        Assert.NotEqual(TestData.CreateUser().Id, TestData.CreateUser().Id);
    }

    [Fact]
    public void UpdateProfile_ChangesMutableFields_AndKeepsCpf()
    {
        var user = TestData.CreateUser();

        user.UpdateProfile("Beltrano", "beltrano@example.com", "1133334444", new DateOnly(1985, 1, 1));

        Assert.Equal("Beltrano", user.Name);
        Assert.Equal("beltrano@example.com", user.Email);
        Assert.Equal("1133334444", user.Phone);
        Assert.Equal(new DateOnly(1985, 1, 1), user.BirthDate);
        Assert.Equal(TestData.ValidCpf, user.Cpf);
    }

    [Fact]
    public void ChangePasswordHash_ReplacesHash()
    {
        var user = TestData.CreateUser();

        user.ChangePasswordHash("novo-hash");

        Assert.Equal("novo-hash", user.PasswordHash);
    }

    [Fact]
    public void Delete_MarksAsDeleted_OnlyOnce()
    {
        var user = TestData.CreateUser();
        var firstDeletion = TestData.Now.AddHours(1);

        user.Delete(firstDeletion);
        user.Delete(firstDeletion.AddHours(1));

        Assert.True(user.IsDeleted);
        Assert.Equal(firstDeletion, user.DeletedAt);
    }
}
