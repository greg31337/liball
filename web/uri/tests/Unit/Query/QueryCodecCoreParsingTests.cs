namespace uri.Tests;

public class QueryCodecCoreParsingTests
{
    [Test]
    public void PlainParsingPreservesDuplicatesMissingEqualsAndOriginalOrder()
    {
        UriQueryParseCoreResult result = Parse(
            "a=1&a=2&b&=empty&empty=&",
            '&',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, '&', 6);
            AssertParameter(result.Parameters[0], "a", true, "1");
            AssertParameter(result.Parameters[1], "a", true, "2");
            AssertParameter(result.Parameters[2], "b", false, "");
            AssertParameter(result.Parameters[3], "", true, "empty");
            AssertParameter(result.Parameters[4], "empty", true, "");
            AssertParameter(result.Parameters[5], "", false, "");
        });
    }

    [Test]
    public void EmptyQueryContainsNoItemsWhileDelimitersCreateEmptyItems()
    {
        UriQueryParseCoreResult empty = Parse(
            "",
            '&',
            new UriQueryOptions());
        UriQueryParseCoreResult oneDelimiter = Parse(
            "&",
            '&',
            new UriQueryOptions());
        UriQueryParseCoreResult twoDelimiters = Parse(
            "&&",
            '&',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, '&', 0);
            AssertSuccess(oneDelimiter, '&', 2);
            AssertParameter(oneDelimiter.Parameters[0], "", false, "");
            AssertParameter(oneDelimiter.Parameters[1], "", false, "");
            AssertSuccess(twoDelimiters, '&', 3);
            AssertParameter(twoDelimiters.Parameters[0], "", false, "");
            AssertParameter(twoDelimiters.Parameters[1], "", false, "");
            AssertParameter(twoDelimiters.Parameters[2], "", false, "");
        });
    }

    [Test]
    public void SelectedPairDelimiterIsPreservedAndOnlyItSplitsItems()
    {
        UriQueryParseCoreResult ampersand = Parse(
            "a=1;b=2&c=3",
            '&',
            new UriQueryOptions());
        UriQueryParseCoreResult semicolon = Parse(
            "a=1;b=2&c=3",
            ';',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(ampersand, '&', 2);
            AssertParameter(ampersand.Parameters[0], "a", true, "1;b=2");
            AssertParameter(ampersand.Parameters[1], "c", true, "3");
            AssertSuccess(semicolon, ';', 2);
            AssertParameter(semicolon.Parameters[0], "a", true, "1");
            AssertParameter(semicolon.Parameters[1], "b", true, "2&c=3");
        });
    }

    [Test]
    public void FirstEqualsSeparatesTheNameAndValue()
    {
        UriQueryParseCoreResult result = Parse(
            "a=b=c&==&name",
            '&',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, '&', 3);
            AssertParameter(result.Parameters[0], "a", true, "b=c");
            AssertParameter(result.Parameters[1], "", true, "=");
            AssertParameter(result.Parameters[2], "name", false, "");
        });
    }

    [Test]
    public void PlainParsingDecodesUtf8AndEncodedDelimitersButKeepsPlusLiteral()
    {
        UriQueryParseCoreResult result = Parse(
            "%26=%3D&snow=%E2%98%83&slash=%2F%3F&plus=a+b",
            '&',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, '&', 4);
            AssertParameter(result.Parameters[0], "&", true, "=");
            AssertParameter(result.Parameters[1], "snow", true, "☃");
            AssertParameter(result.Parameters[2], "slash", true, "/?");
            AssertParameter(result.Parameters[3], "plus", true, "a+b");
        });
    }

    [TestCase("a=%", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=%GG", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=%FF", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=#fragment", UriDiagnosticCode.InvalidQuery)]
    [TestCase("a=☃", UriDiagnosticCode.InvalidQuery)]
    public void InvalidQueryTextReturnsTheSpecificDiagnostic(
        string query,
        UriDiagnosticCode code)
    {
        UriQueryParseCoreResult result = Parse(
            query,
            '&',
            new UriQueryOptions());

        AssertFailure(result, code, UriComponent.Query);
    }

    [Test]
    public void InvalidUtf8AndPercentDiagnosticsRetainAbsoluteQueryOffsets()
    {
        UriQueryParseCoreResult percent = Parse(
            "a=1&b=%GG",
            '&',
            new UriQueryOptions());
        UriQueryParseCoreResult utf8 = Parse(
            "a=1&b=%E2%28%A1",
            '&',
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(percent, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Query);
            Assert.That(percent.Diagnostic!.Range.Offset, Is.EqualTo(7));
            AssertFailure(utf8, UriDiagnosticCode.InvalidUtf8, UriComponent.Query);
            Assert.That(utf8.Diagnostic!.Range.Offset, Is.EqualTo(9));
        });
    }

    [Test]
    public void ParsingAppliesInputComponentAndItemLimitsBeforeAllocation()
    {
        UriQueryOptions inputOptions = new UriQueryOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        UriQueryOptions componentOptions = new UriQueryOptions();
        componentOptions.Limits.MaxComponentCharacters = 2;
        UriQueryOptions itemOptions = new UriQueryOptions();
        itemOptions.Limits.MaxQueryItems = 2;

        UriQueryParseCoreResult input = Parse("abc", '&', inputOptions);
        UriQueryParseCoreResult component = Parse("abc", '&', componentOptions);
        UriQueryParseCoreResult items = Parse("a&b&c", '&', itemOptions);

        Assert.Multiple(() =>
        {
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertFailure(component, UriDiagnosticCode.LimitExceeded, UriComponent.Query);
            AssertFailure(items, UriDiagnosticCode.LimitExceeded, UriComponent.Query);
        });
    }

    [Test]
    public void MissingInputsLimitsAndUnsupportedDelimitersReturnInvalidInput()
    {
        UriQueryOptions missingLimits = new UriQueryOptions();
        missingLimits.Limits = null!;
        UriQueryParseCoreResult[] results = new UriQueryParseCoreResult[]
        {
            Parse(null, '&', new UriQueryOptions()),
            Parse("a=1", '&', null),
            Parse("a=1", '&', missingLimits),
            Parse("a=1", ',', new UriQueryOptions())
        };

        foreach (UriQueryParseCoreResult result in results)
        {
            AssertFailure(result, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        }
    }

    private static UriQueryParseCoreResult Parse(
        string? query,
        char pairDelimiter,
        UriQueryOptions? options)
    {
        if (options != null)
        {
            options.PairDelimiter = pairDelimiter;
        }

        return QueryCodecCore.ParseQuery(query, options);
    }

    private static void AssertSuccess(
        UriQueryParseCoreResult result,
        char pairDelimiter,
        int count)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Parameters, Has.Length.EqualTo(count));
        Assert.That(result.PairDelimiter, Is.EqualTo(pairDelimiter));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertParameter(
        UriQueryParameterValue parameter,
        string name,
        bool hasValue,
        string value)
    {
        Assert.That(parameter.Name, Is.EqualTo(name));
        Assert.That(parameter.HasValue, Is.EqualTo(hasValue));
        Assert.That(parameter.Value, Is.EqualTo(value));
    }

    private static void AssertFailure(
        UriQueryParseCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Parameters, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
