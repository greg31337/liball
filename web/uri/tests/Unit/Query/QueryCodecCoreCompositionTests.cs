namespace uri.Tests;

public class QueryCodecCoreCompositionTests
{
    [TestCase("a=1&a=2&b&=empty&empty=&&last", '&')]
    [TestCase("a=1;a=2;b;=empty;empty=;;last", ';')]
    public void ParsedQueriesComposeWithOrderDuplicatesAndEmptyStatesIntact(
        string query,
        char delimiter)
    {
        UriQueryOptions options = CreateOptions(delimiter);
        UriQueryParseCoreResult parsed = QueryCodecCore.ParseQuery(
            query,
            options);

        UriQueryComposeCoreResult composed = QueryCodecCore.ComposeQuery(
            parsed.Parameters,
            options);

        AssertSuccess(composed, query, delimiter);
    }

    [Test]
    public void CompositionPercentEncodesUnicodeAndStructuralQueryCharacters()
    {
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a&b", true, "c&d"),
            new UriQueryParameterValue("equals=name", true, "equals=value"),
            new UriQueryParameterValue("snow", true, "☃"),
            new UriQueryParameterValue("space value", true, "100% #done"),
            new UriQueryParameterValue("path", true, "/?@:+"),
            new UriQueryParameterValue("plus", true, "a+b")
        };

        UriQueryComposeCoreResult result = QueryCodecCore.ComposeQuery(
            parameters,
            CreateOptions('&'));

        AssertSuccess(
            result,
            "a%26b=c%26d&equals%3Dname=equals=value&snow=%E2%98%83&space%20value=100%25%20%23done&path=/?@:+&plus=a+b",
            '&');
    }

    [Test]
    public void OnlyTheSelectedPairDelimiterIsStructuralData()
    {
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a&b;c", true, "d&e;f")
        };

        UriQueryComposeCoreResult ampersand = QueryCodecCore.ComposeQuery(
            parameters,
            CreateOptions('&'));
        UriQueryComposeCoreResult semicolon = QueryCodecCore.ComposeQuery(
            parameters,
            CreateOptions(';'));

        Assert.Multiple(() =>
        {
            AssertSuccess(ampersand, "a%26b;c=d%26e;f", '&');
            AssertSuccess(semicolon, "a&b%3Bc=d&e%3Bf", ';');
        });
    }

    [Test]
    public void MissingEqualsAndPresentEmptyValuesComposeDifferently()
    {
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("missing", false, "ignored"),
            new UriQueryParameterValue("empty", true, ""),
            new UriQueryParameterValue("", true, "value"),
            new UriQueryParameterValue("", false, "")
        };

        UriQueryComposeCoreResult result = QueryCodecCore.ComposeQuery(
            parameters,
            CreateOptions('&'));

        AssertSuccess(result, "missing&empty=&=value&", '&');
    }

    [Test]
    public void EmptyCollectionsAndMultipleEmptyItemsRemainBoundedAndDeterministic()
    {
        UriQueryComposeCoreResult none = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[0],
            CreateOptions('&'));
        UriQueryComposeCoreResult two = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[]
            {
                new UriQueryParameterValue("", false, ""),
                new UriQueryParameterValue("", false, "")
            },
            CreateOptions('&'));

        Assert.Multiple(() =>
        {
            AssertSuccess(none, "", '&');
            AssertSuccess(two, "&", '&');
        });
    }

    [Test]
    public void ComposeThenParseRestoresEveryParameterState()
    {
        UriQueryParameterValue[] expected = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("duplicate", true, "1"),
            new UriQueryParameterValue("duplicate", true, "2"),
            new UriQueryParameterValue("missing", false, "unused"),
            new UriQueryParameterValue("", true, ""),
            new UriQueryParameterValue("&=", true, "☃;+ ")
        };
        UriQueryOptions options = CreateOptions('&');

        UriQueryComposeCoreResult composed = QueryCodecCore.ComposeQuery(
            expected,
            options);
        UriQueryParseCoreResult reparsed = QueryCodecCore.ParseQuery(
            composed.Text,
            options);

        Assert.That(composed.Success, Is.True, composed.Diagnostic?.Message);
        Assert.That(reparsed.Success, Is.True, reparsed.Diagnostic?.Message);
        Assert.That(reparsed.Parameters, Has.Length.EqualTo(expected.Length));
        int offset = 0;
        while (offset < expected.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(reparsed.Parameters[offset].Name, Is.EqualTo(expected[offset].Name));
                Assert.That(reparsed.Parameters[offset].HasValue, Is.EqualTo(expected[offset].HasValue));
                if (expected[offset].HasValue)
                {
                    Assert.That(reparsed.Parameters[offset].Value, Is.EqualTo(expected[offset].Value));
                }
            });
            offset = offset + 1;
        }
    }

    [Test]
    public void CompositionRejectsInvalidUnicodeAndMissingParameterData()
    {
        UriQueryParameterValue invalidUnicode = new UriQueryParameterValue(
            "bad\ud800",
            false,
            "");
        UriQueryParameterValue missingName = new UriQueryParameterValue(
            "name",
            false,
            "");
        missingName.Name = null!;
        UriQueryParameterValue missingValue = new UriQueryParameterValue(
            "name",
            true,
            "value");
        missingValue.Value = null!;

        UriQueryComposeCoreResult unicode = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[] { invalidUnicode },
            CreateOptions('&'));
        UriQueryComposeCoreResult name = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[] { missingName },
            CreateOptions('&'));
        UriQueryComposeCoreResult value = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[] { missingValue },
            CreateOptions('&'));
        UriQueryParameterValue[] missingItem = new UriQueryParameterValue[1];
        missingItem[0] = null!;
        UriQueryComposeCoreResult item = QueryCodecCore.ComposeQuery(
            missingItem,
            CreateOptions('&'));

        Assert.Multiple(() =>
        {
            AssertFailure(unicode, UriDiagnosticCode.InvalidUnicode, UriComponent.Query);
            AssertFailure(name, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(value, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(item, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        });
    }

    [Test]
    public void CompositionAppliesItemInputComponentAndOutputLimits()
    {
        UriQueryOptions itemOptions = CreateOptions('&');
        itemOptions.Limits.MaxQueryItems = 1;
        UriQueryOptions inputOptions = CreateOptions('&');
        inputOptions.Limits.MaxInputCharacters = 2;
        UriQueryOptions componentOptions = CreateOptions('&');
        componentOptions.Limits.MaxComponentCharacters = 2;
        UriQueryOptions outputOptions = CreateOptions('&');
        outputOptions.Limits.MaxOutputCharacters = 4;

        UriQueryComposeCoreResult items = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[]
            {
                new UriQueryParameterValue("a", false, ""),
                new UriQueryParameterValue("b", false, "")
            },
            itemOptions);
        UriQueryComposeCoreResult input = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[]
            {
                new UriQueryParameterValue("abc", false, "")
            },
            inputOptions);
        UriQueryComposeCoreResult component = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[]
            {
                new UriQueryParameterValue("abc", false, "")
            },
            componentOptions);
        UriQueryComposeCoreResult output = QueryCodecCore.ComposeQuery(
            new UriQueryParameterValue[]
            {
                new UriQueryParameterValue("☃", false, "")
            },
            outputOptions);

        Assert.Multiple(() =>
        {
            AssertFailure(items, UriDiagnosticCode.LimitExceeded, UriComponent.Query);
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertFailure(component, UriDiagnosticCode.LimitExceeded, UriComponent.Query);
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    [Test]
    public void MissingOptionsLimitsCollectionsAndUnsupportedDelimitersFail()
    {
        UriQueryOptions missingLimits = CreateOptions('&');
        missingLimits.Limits = null!;
        UriQueryOptions invalidDelimiter = CreateOptions(',');
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a", true, "1")
        };

        UriQueryComposeCoreResult[] results = new UriQueryComposeCoreResult[]
        {
            QueryCodecCore.ComposeQuery(null, CreateOptions('&')),
            QueryCodecCore.ComposeQuery(parameters, null),
            QueryCodecCore.ComposeQuery(parameters, missingLimits),
            QueryCodecCore.ComposeQuery(parameters, invalidDelimiter)
        };

        foreach (UriQueryComposeCoreResult result in results)
        {
            AssertFailure(result, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        }
    }

    [Test]
    public void QueryOptionsDefaultToIndependentAmpersandConfiguration()
    {
        UriQueryOptions first = new UriQueryOptions();
        UriQueryOptions second = new UriQueryOptions();
        first.PairDelimiter = ';';

        Assert.Multiple(() =>
        {
            Assert.That(first.PairDelimiter, Is.EqualTo(';'));
            Assert.That(second.PairDelimiter, Is.EqualTo('&'));
            Assert.That(first.FormUrlEncoded, Is.False);
            Assert.That(second.FormUrlEncoded, Is.False);
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
        });
    }

    private static UriQueryOptions CreateOptions(char delimiter)
    {
        UriQueryOptions options = new UriQueryOptions();
        options.PairDelimiter = delimiter;
        return options;
    }

    private static void AssertSuccess(
        UriQueryComposeCoreResult result,
        string expected,
        char delimiter)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.PairDelimiter, Is.EqualTo(delimiter));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriQueryComposeCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
