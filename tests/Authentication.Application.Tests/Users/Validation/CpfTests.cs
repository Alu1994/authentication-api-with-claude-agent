using Authentication.Application.Users.Validation;

namespace Authentication.Application.Tests.Users.Validation;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 529 982 247 25 ", "52998224725")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Normalize_RemovesEverythingButDigits(string? input, string expected)
    {
        Assert.Equal(expected, Cpf.Normalize(input));
    }

    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData("11144477735")]
    [InlineData("39053344705")]
    public void IsValid_ReturnsTrue_ForValidCpf(string cpf)
    {
        Assert.True(Cpf.IsValid(cpf));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("5299822472")]      // 10 digitos
    [InlineData("529982247250")]    // 12 digitos
    [InlineData("52998224724")]     // segundo digito verificador errado
    [InlineData("52998224715")]     // primeiro digito verificador errado
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    [InlineData("abcdefghijk")]
    public void IsValid_ReturnsFalse_ForInvalidCpf(string? cpf)
    {
        Assert.False(Cpf.IsValid(cpf));
    }
}
