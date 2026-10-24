namespace Authentication.Application.Users.Validation;

/// <summary>
/// Regras de normalizacao e validacao de CPF (formato + digitos verificadores).
/// </summary>
public static class Cpf
{
    public const int Length = 11;

    /// <summary>Remove qualquer caractere que nao seja digito (mascara, espacos etc.).</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return string.Create(value.Count(char.IsAsciiDigit), value, static (span, source) =>
        {
            var index = 0;
            foreach (var c in source)
            {
                if (char.IsAsciiDigit(c))
                {
                    span[index++] = c;
                }
            }
        });
    }

    /// <summary>Valida tamanho, sequencias repetidas e os dois digitos verificadores.</summary>
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);

        if (digits.Length != Length || digits.All(c => c == digits[0]))
        {
            return false;
        }

        return digits[9] - '0' == CalculateCheckDigit(digits.AsSpan(0, 9))
            && digits[10] - '0' == CalculateCheckDigit(digits.AsSpan(0, 10));
    }

    private static int CalculateCheckDigit(ReadOnlySpan<char> digits)
    {
        var weight = digits.Length + 1;
        var sum = 0;

        foreach (var c in digits)
        {
            sum += (c - '0') * weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
