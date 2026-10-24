using Authentication.Application.Users.Validation;

namespace Authentication.Application.Tests.Users.Validation;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Senha123")]
    [InlineData("abcdefg1")]
    [InlineData("1234567a")]
    [InlineData("uma senha longa com espacos 2")]
    public void Validate_ReturnsNoErrors_ForCompliantPassword(string password)
    {
        Assert.Empty(PasswordPolicy.Validate(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("        ")]
    public void Validate_ReturnsRequiredError_WhenEmpty(string? password)
    {
        var errors = PasswordPolicy.Validate(password);

        var error = Assert.Single(errors);
        Assert.Equal(PasswordPolicy.RequiredMessage, error);
    }

    [Fact]
    public void Validate_ReturnsMinLengthError_WhenTooShort()
    {
        var errors = PasswordPolicy.Validate("abc12");

        Assert.Contains(PasswordPolicy.MinLengthMessage, errors);
    }

    [Fact]
    public void Validate_ReturnsMaxLengthError_WhenTooLong()
    {
        var password = new string('a', PasswordPolicy.MaxLength) + "1";

        var errors = PasswordPolicy.Validate(password);

        Assert.Contains(PasswordPolicy.MaxLengthMessage, errors);
    }

    [Fact]
    public void Validate_RequiresAtLeastOneLetter()
    {
        var errors = PasswordPolicy.Validate("12345678");

        Assert.Contains(PasswordPolicy.LetterMessage, errors);
    }

    [Fact]
    public void Validate_RequiresAtLeastOneDigit()
    {
        var errors = PasswordPolicy.Validate("abcdefgh");

        Assert.Contains(PasswordPolicy.DigitMessage, errors);
    }

    [Fact]
    public void Validate_AcceptsExactlyMinAndMaxLength()
    {
        Assert.Empty(PasswordPolicy.Validate("abcdef12"));
        Assert.Empty(PasswordPolicy.Validate(new string('a', PasswordPolicy.MaxLength - 1) + "1"));
    }
}
