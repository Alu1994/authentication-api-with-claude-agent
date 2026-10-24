namespace Authentication.Application.Users.Validation;

/// <summary>
/// Politica minima de senha: obrigatoria, entre <see cref="MinLength"/> e <see cref="MaxLength"/>
/// caracteres, com ao menos uma letra e um digito.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public const string RequiredMessage = "A senha e obrigatoria.";
    public static readonly string MinLengthMessage = $"A senha deve ter no minimo {MinLength} caracteres.";
    public static readonly string MaxLengthMessage = $"A senha deve ter no maximo {MaxLength} caracteres.";
    public const string LetterMessage = "A senha deve conter ao menos uma letra.";
    public const string DigitMessage = "A senha deve conter ao menos um digito.";

    public static IReadOnlyList<string> Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return [RequiredMessage];
        }

        var errors = new List<string>();

        if (password.Length < MinLength)
        {
            errors.Add(MinLengthMessage);
        }

        if (password.Length > MaxLength)
        {
            errors.Add(MaxLengthMessage);
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add(LetterMessage);
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add(DigitMessage);
        }

        return errors;
    }
}
