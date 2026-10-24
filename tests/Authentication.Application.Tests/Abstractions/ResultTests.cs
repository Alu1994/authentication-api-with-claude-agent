using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Tests.Abstractions;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ImplicitConversionFromError_CreatesFailure()
    {
        Result result = Error.NotFound("x.not_found", "Nao encontrado.");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public void GenericResult_ExposesValueOnSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericResult_ThrowsWhenAccessingValueOfFailure()
    {
        Result<int> result = Error.Conflict("x.conflict", "Conflito.");

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ErrorFactories_SetExpectedTypes()
    {
        var fields = new Dictionary<string, string[]> { ["email"] = ["invalido"] };

        var validation = Error.Validation(fields);

        Assert.Equal(ErrorType.Validation, validation.Type);
        Assert.Same(fields, validation.ValidationErrors);
        Assert.Equal(ErrorType.Unauthorized, Error.Unauthorized("a", "b").Type);
        Assert.Equal(ErrorType.Conflict, Error.Conflict("a", "b").Type);
        Assert.Empty(Error.NotFound("a", "b").ValidationErrors);
    }
}
