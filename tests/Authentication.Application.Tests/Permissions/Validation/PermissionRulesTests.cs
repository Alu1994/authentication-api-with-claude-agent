using Authentication.Application.Permissions.Validation;

namespace Authentication.Application.Tests.Permissions.Validation;

public class PermissionRulesTests
{
    [Theory]
    [InlineData("ab")]
    [InlineData("users")]
    [InlineData("a1")]
    [InlineData("user_roles")]
    [InlineData("user-roles")]
    [InlineData("a0123456789012345678901234567890123456789012345678")] // 50 caracteres
    public void IsValidSegment_AcceptsValidValues(string value)
    {
        Assert.True(PermissionRules.IsValidSegment(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Users")]
    [InlineData("1users")]
    [InlineData("_users")]
    [InlineData("-users")]
    [InlineData("users:read")]
    [InlineData("us ers")]
    [InlineData("usérs")]
    [InlineData("users\n")]
    [InlineData("a01234567890123456789012345678901234567890123456789")] // 51 caracteres
    public void IsValidSegment_RejectsInvalidValues(string value)
    {
        Assert.False(PermissionRules.IsValidSegment(value));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  users  ", "users")]
    [InlineData("Users", "Users")] // nao converte para minusculas: o formato e validado, nao corrigido
    public void NormalizeSegment_TrimsOnly(string? value, string expected)
    {
        Assert.Equal(expected, PermissionRules.NormalizeSegment(value));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Consulta  ", "Consulta")]
    public void NormalizeDescription_TrimsAndTreatsBlankAsNull(string? value, string? expected)
    {
        Assert.Equal(expected, PermissionRules.NormalizeDescription(value));
    }

    [Fact]
    public void IsValidDescription_LimitsLength()
    {
        Assert.True(PermissionRules.IsValidDescription(null));
        Assert.True(PermissionRules.IsValidDescription(new string('a', PermissionRules.DescriptionMaxLength)));
        Assert.False(PermissionRules.IsValidDescription(new string('a', PermissionRules.DescriptionMaxLength + 1)));
        Assert.Equal(500, PermissionRules.DescriptionMaxLength);
    }
}
