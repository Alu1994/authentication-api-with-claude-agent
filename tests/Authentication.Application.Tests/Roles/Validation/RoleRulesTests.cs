using Authentication.Application.Roles.Validation;

namespace Authentication.Application.Tests.Roles.Validation;

public class RoleRulesTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("  Admin  ", "Admin")]
    public void NormalizeName_Trims(string? value, string expected)
    {
        Assert.Equal(expected, RoleRules.NormalizeName(value));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("Gestão de Acessos", true)] // texto livre: acentos e espacos sao aceitos
    [InlineData("a", false)]
    [InlineData("", false)]
    public void IsValidName_ChecksLength(string value, bool expected)
    {
        Assert.Equal(expected, RoleRules.IsValidName(value));
    }

    [Fact]
    public void IsValidName_AcceptsUpToMaxLength()
    {
        Assert.True(RoleRules.IsValidName(new string('a', RoleRules.NameMaxLength)));
        Assert.False(RoleRules.IsValidName(new string('a', RoleRules.NameMaxLength + 1)));
        Assert.Equal(100, RoleRules.NameMaxLength);
    }

    [Fact]
    public void NormalizeForComparison_IsCaseInsensitive()
    {
        Assert.Equal(RoleRules.NormalizeForComparison("ADMIN"), RoleRules.NormalizeForComparison("admin"));
        Assert.Equal("gestão", RoleRules.NormalizeForComparison("GESTÃO"));
    }
}
