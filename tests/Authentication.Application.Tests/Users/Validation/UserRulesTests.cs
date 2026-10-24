using Authentication.Application.Users.Validation;

namespace Authentication.Application.Tests.Users.Validation;

public class UserRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 23);

    [Theory]
    [InlineData("  Fulano@Example.COM ", "fulano@example.com")]
    [InlineData(null, "")]
    public void NormalizeEmail_TrimsAndLowercases(string? input, string expected)
    {
        Assert.Equal(expected, UserRules.NormalizeEmail(input));
    }

    [Theory]
    [InlineData("fulano@example.com")]
    [InlineData("fulano.silva+tag@sub.example.com.br")]
    public void IsValidEmail_ReturnsTrue_ForValidEmail(string email)
    {
        Assert.True(UserRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fulano")]
    [InlineData("fulano@")]
    [InlineData("@example.com")]
    [InlineData("fulano@example")]
    [InlineData("ful ano@example.com")]
    [InlineData("fulano@@example.com")]
    public void IsValidEmail_ReturnsFalse_ForInvalidEmail(string email)
    {
        Assert.False(UserRules.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_ReturnsFalse_WhenLongerThanMaxLength()
    {
        var email = new string('a', UserRules.EmailMaxLength) + "@example.com";

        Assert.False(UserRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData(null, "")]
    public void NormalizePhone_KeepsOnlyDigits(string? input, string expected)
    {
        Assert.Equal(expected, UserRules.NormalizePhone(input));
    }

    [Theory]
    [InlineData("11987654321")]
    [InlineData("1133334444")]
    public void IsValidPhone_ReturnsTrue_ForBrazilianPhone(string phone)
    {
        Assert.True(UserRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("987654321")]
    [InlineData("119876543210")]
    [InlineData("01987654321")]
    public void IsValidPhone_ReturnsFalse_ForInvalidPhone(string phone)
    {
        Assert.False(UserRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("  Fulano de Tal  ", "Fulano de Tal")]
    [InlineData(null, "")]
    public void NormalizeName_Trims(string? input, string expected)
    {
        Assert.Equal(expected, UserRules.NormalizeName(input));
    }

    [Theory]
    [InlineData("Ana")]
    [InlineData("Fulano de Tal")]
    public void IsValidName_ReturnsTrue_ForValidName(string name)
    {
        Assert.True(UserRules.IsValidName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Al")]
    public void IsValidName_ReturnsFalse_ForTooShortName(string name)
    {
        Assert.False(UserRules.IsValidName(name));
    }

    [Fact]
    public void IsValidName_ReturnsFalse_ForTooLongName()
    {
        Assert.False(UserRules.IsValidName(new string('a', UserRules.NameMaxLength + 1)));
    }

    [Fact]
    public void IsValidBirthDate_ReturnsTrue_ForPastDateWithinRange()
    {
        Assert.True(UserRules.IsValidBirthDate(new DateOnly(1990, 5, 10), Today));
        Assert.True(UserRules.IsValidBirthDate(Today.AddYears(-UserRules.MaxAgeInYears), Today));
    }

    [Fact]
    public void IsValidBirthDate_ReturnsFalse_ForNullTodayFutureOrTooOld()
    {
        Assert.False(UserRules.IsValidBirthDate(null, Today));
        Assert.False(UserRules.IsValidBirthDate(Today, Today));
        Assert.False(UserRules.IsValidBirthDate(Today.AddDays(1), Today));
        Assert.False(UserRules.IsValidBirthDate(Today.AddYears(-UserRules.MaxAgeInYears).AddDays(-1), Today));
    }
}
