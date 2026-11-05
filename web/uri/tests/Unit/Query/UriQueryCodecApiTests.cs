namespace uri.Tests;

public class UriQueryCodecApiTests
{
    [Test]
    public void ParseQueryReturnsOrderedPublicDataAndOriginalEncodedText()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        options.FormUrlEncoded = true;

        UriQueryResult result = codec.ParseQuery(
            "a+b=one+two&a+b=%2B&flag&empty=",
            options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, "a+b=one+two&a+b=%2B&flag&empty=", '&', 4);
            AssertParameter(result.Parameters[0], "a b", true, "one two");
            AssertParameter(result.Parameters[1], "a b", true, "+");
            AssertParameter(result.Parameters[2], "flag", false, "");
            AssertParameter(result.Parameters[3], "empty", true, "");
        });
    }

    [Test]
    public void ComposeQueryAppliesOptionsAndReturnsIndependentPublicData()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        options.PairDelimiter = ';';
        options.FormUrlEncoded = true;
        UriQueryParameter first = Parameter("a b", true, "c+d e");
        UriQueryParameter[] parameters = new UriQueryParameter[]
        {
            first,
            Parameter("flag", false, "ignored")
        };

        UriQueryResult result = codec.ComposeQuery(parameters, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, "a+b=c%2Bd+e;flag", ';', 2);
            AssertParameter(result.Parameters[0], "a b", true, "c+d e");
            AssertParameter(result.Parameters[1], "flag", false, "ignored");
            Assert.That(result.Parameters, Is.Not.SameAs(parameters));
            Assert.That(result.Parameters[0], Is.Not.SameAs(first));
        });
    }

    [Test]
    public void PublicMutationMethodsAreExplicitAndReturnComposedResults()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        UriQueryParameter[] source = new UriQueryParameter[]
        {
            Parameter("b", true, "2"),
            Parameter("a", true, "2"),
            Parameter("a", true, "1"),
            Parameter("a", true, "1"),
            Parameter("flag", false, ""),
            Parameter("flag", true, "")
        };

        UriQueryResult added = codec.AddQueryParameter(
            source,
            Parameter("c", true, "3"),
            options);
        UriQueryResult replaced = codec.ReplaceQueryParameters(
            source,
            "a",
            new UriQueryParameter[]
            {
                Parameter("a", true, "new"),
                Parameter("a", false, "")
            },
            options);
        UriQueryResult removed = codec.RemoveQueryParameters(source, "a", options);
        UriQueryResult sorted = codec.SortQueryParameters(source, options);
        UriQueryResult deduplicated = codec.DeduplicateQueryParameters(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(added.Text, Is.EqualTo("b=2&a=2&a=1&a=1&flag&flag=&c=3"));
            Assert.That(replaced.Text, Is.EqualTo("b=2&a=new&a&flag&flag="));
            Assert.That(removed.Text, Is.EqualTo("b=2&flag&flag="));
            Assert.That(sorted.Text, Is.EqualTo("a=1&a=1&a=2&b=2&flag&flag="));
            Assert.That(deduplicated.Text, Is.EqualTo("b=2&a=2&a=1&flag&flag="));
            Assert.That(source, Has.Length.EqualTo(6));
            Assert.That(source[0].Name, Is.EqualTo("b"));
        });
    }

    [Test]
    public void ParseQueryEnforcesInputComponentItemAndOutputLimits()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions inputOptions = new UriQueryOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        UriQueryOptions componentOptions = new UriQueryOptions();
        componentOptions.Limits.MaxComponentCharacters = 2;
        UriQueryOptions itemOptions = new UriQueryOptions();
        itemOptions.Limits.MaxQueryItems = 1;
        UriQueryOptions outputOptions = new UriQueryOptions();
        outputOptions.Limits.MaxOutputCharacters = 2;

        UriQueryResult input = codec.ParseQuery("abc", inputOptions);
        UriQueryResult component = codec.ParseQuery("abc", componentOptions);
        UriQueryResult items = codec.ParseQuery("a&b", itemOptions);
        UriQueryResult output = codec.ParseQuery("abc", outputOptions);

        Assert.Multiple(() =>
        {
            AssertLimitFailure(input, UriComponent.Input);
            AssertLimitFailure(component, UriComponent.Query);
            AssertLimitFailure(items, UriComponent.Query);
            AssertLimitFailure(output, UriComponent.Output);
        });
    }

    [Test]
    public void ComposeQueryEnforcesInputComponentItemAndOutputLimits()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryParameter[] two = new UriQueryParameter[]
        {
            Parameter("a", true, "1"),
            Parameter("b", true, "2")
        };
        UriQueryOptions itemOptions = new UriQueryOptions();
        itemOptions.Limits.MaxQueryItems = 1;
        UriQueryOptions inputOptions = new UriQueryOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        UriQueryOptions componentOptions = new UriQueryOptions();
        componentOptions.Limits.MaxComponentCharacters = 2;
        UriQueryOptions outputOptions = new UriQueryOptions();
        outputOptions.Limits.MaxOutputCharacters = 4;

        UriQueryResult items = codec.ComposeQuery(two, itemOptions);
        UriQueryResult input = codec.ComposeQuery(
            new UriQueryParameter[] { Parameter("abc", false, "") },
            inputOptions);
        UriQueryResult component = codec.ComposeQuery(
            new UriQueryParameter[] { Parameter("abc", false, "") },
            componentOptions);
        UriQueryResult output = codec.ComposeQuery(
            new UriQueryParameter[] { Parameter("☃", false, "") },
            outputOptions);

        Assert.Multiple(() =>
        {
            AssertLimitFailure(items, UriComponent.Query);
            AssertLimitFailure(input, UriComponent.Input);
            AssertLimitFailure(component, UriComponent.Query);
            AssertLimitFailure(output, UriComponent.Output);
        });
    }

    [Test]
    public void MutationMethodsEnforceResultItemAndOutputLimits()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryParameter[] source = new UriQueryParameter[]
        {
            Parameter("a", true, "1")
        };
        UriQueryOptions itemOptions = new UriQueryOptions();
        itemOptions.Limits.MaxQueryItems = 1;
        UriQueryOptions outputOptions = new UriQueryOptions();
        outputOptions.Limits.MaxOutputCharacters = 3;

        UriQueryResult added = codec.AddQueryParameter(
            source,
            Parameter("b", true, "2"),
            itemOptions);
        UriQueryResult replaced = codec.ReplaceQueryParameters(
            source,
            "a",
            new UriQueryParameter[]
            {
                Parameter("a", true, "1"),
                Parameter("a", true, "2")
            },
            itemOptions);
        UriQueryResult output = codec.AddQueryParameter(
            source,
            Parameter("b", true, "2"),
            outputOptions);

        Assert.Multiple(() =>
        {
            AssertLimitFailure(added, UriComponent.Query);
            AssertLimitFailure(replaced, UriComponent.Query);
            AssertLimitFailure(output, UriComponent.Output);
        });
    }

    [Test]
    public void InvalidModelsAndOptionsReturnEmptyDiagnosticResults()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryParameter invalid = Parameter("name", true, "value");
        invalid.Value = null!;
        UriQueryOptions noLimits = new UriQueryOptions();
        noLimits.Limits = null!;

        UriQueryResult nullQuery = codec.ParseQuery(null!, new UriQueryOptions());
        UriQueryResult nullParameters = codec.ComposeQuery(
            null!,
            new UriQueryOptions());
        UriQueryResult invalidParameter = codec.ComposeQuery(
            new UriQueryParameter[] { invalid },
            new UriQueryOptions());
        UriQueryResult invalidOptions = codec.SortQueryParameters(
            new UriQueryParameter[0],
            noLimits);

        Assert.Multiple(() =>
        {
            AssertInvalidInputFailure(nullQuery);
            AssertInvalidInputFailure(nullParameters);
            AssertInvalidInputFailure(invalidParameter);
            AssertInvalidInputFailure(invalidOptions);
        });
    }

    private static UriQueryParameter Parameter(
        string name,
        bool hasValue,
        string value)
    {
        return new UriQueryParameter(name, hasValue, value);
    }

    private static void AssertSuccess(
        UriQueryResult result,
        string text,
        char delimiter,
        int parameterCount)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Length, Is.EqualTo(text.Length));
        Assert.That(result.PairDelimiter, Is.EqualTo(delimiter));
        Assert.That(result.Parameters, Has.Length.EqualTo(parameterCount));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertParameter(
        UriQueryParameter parameter,
        string name,
        bool hasValue,
        string value)
    {
        Assert.That(parameter.Name, Is.EqualTo(name));
        Assert.That(parameter.HasValue, Is.EqualTo(hasValue));
        Assert.That(parameter.Value, Is.EqualTo(value));
    }

    private static void AssertLimitFailure(
        UriQueryResult result,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Parameters, Is.Empty);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static void AssertInvalidInputFailure(UriQueryResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Parameters, Is.Empty);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
    }
}
