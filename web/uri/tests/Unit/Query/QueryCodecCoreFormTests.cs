namespace uri.Tests;

public class QueryCodecCoreFormTests
{
    [Test]
    public void FormParsingConvertsLiteralPlusButPreservesEncodedPlus()
    {
        UriQueryOptions plainOptions = CreateOptions('&', false);
        UriQueryOptions formOptions = CreateOptions('&', true);
        string query = "a+b=c+d&literal=%2B&space=%20&mixed=+%2B+";

        UriQueryParseCoreResult plain = QueryCodecCore.ParseQuery(
            query,
            plainOptions);
        UriQueryParseCoreResult form = QueryCodecCore.ParseQuery(
            query,
            formOptions);

        Assert.Multiple(() =>
        {
            AssertSuccess(plain, '&', 4);
            AssertParameter(plain.Parameters[0], "a+b", true, "c+d");
            AssertParameter(plain.Parameters[1], "literal", true, "+");
            AssertParameter(plain.Parameters[2], "space", true, " ");
            AssertParameter(plain.Parameters[3], "mixed", true, "+++");
            AssertSuccess(form, '&', 4);
            AssertParameter(form.Parameters[0], "a b", true, "c d");
            AssertParameter(form.Parameters[1], "literal", true, "+");
            AssertParameter(form.Parameters[2], "space", true, " ");
            AssertParameter(form.Parameters[3], "mixed", true, " + ");
        });
    }

    [Test]
    public void FormCompositionUsesPlusForSpaceAndPercentEncodingForPlus()
    {
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a b", true, "c+d e"),
            new UriQueryParameterValue("+", true, " + "),
            new UriQueryParameterValue("snow", true, "☃")
        };

        UriQueryComposeCoreResult result = QueryCodecCore.ComposeQuery(
            parameters,
            CreateOptions('&', true));

        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(
            result.Text,
            Is.EqualTo("a+b=c%2Bd+e&%2B=+%2B+&snow=%E2%98%83"));
        Assert.That(result.Length, Is.EqualTo(result.Text.Length));
        Assert.That(result.PairDelimiter, Is.EqualTo('&'));
        Assert.That(result.Diagnostic, Is.Null);
    }

    [Test]
    public void FormCompositionAndParsingRoundTripEveryParameterState()
    {
        UriQueryParameterValue[] expected = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a b", true, " + "),
            new UriQueryParameterValue("a b", false, "ignored"),
            new UriQueryParameterValue("&=;", true, "☃+ space"),
            new UriQueryParameterValue("", true, "")
        };
        UriQueryOptions options = CreateOptions('&', true);

        UriQueryComposeCoreResult composed = QueryCodecCore.ComposeQuery(
            expected,
            options);
        UriQueryParseCoreResult parsed = QueryCodecCore.ParseQuery(
            composed.Text,
            options);

        Assert.That(composed.Success, Is.True, composed.Diagnostic?.Message);
        Assert.That(parsed.Success, Is.True, parsed.Diagnostic?.Message);
        Assert.That(parsed.Parameters, Has.Length.EqualTo(expected.Length));
        int offset = 0;
        while (offset < expected.Length)
        {
            AssertParameter(
                parsed.Parameters[offset],
                expected[offset].Name,
                expected[offset].HasValue,
                expected[offset].HasValue ? expected[offset].Value : "");
            offset = offset + 1;
        }
    }

    [Test]
    public void FormModeHonorsTheSelectedPairDelimiter()
    {
        UriQueryParameterValue[] parameters = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("a;b c", true, "d;e+ f")
        };
        UriQueryOptions options = CreateOptions(';', true);

        UriQueryComposeCoreResult composed = QueryCodecCore.ComposeQuery(
            parameters,
            options);
        UriQueryParseCoreResult parsed = QueryCodecCore.ParseQuery(
            composed.Text,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(composed.Success, Is.True, composed.Diagnostic?.Message);
            Assert.That(composed.Text, Is.EqualTo("a%3Bb+c=d%3Be%2B+f"));
            AssertSuccess(parsed, ';', 1);
            AssertParameter(parsed.Parameters[0], "a;b c", true, "d;e+ f");
        });
    }

    [TestCase("a=+%", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=+%GG", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=+%FF", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=+%E2%28%A1", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=raw space", UriDiagnosticCode.InvalidQuery)]
    [TestCase("a=☃", UriDiagnosticCode.InvalidQuery)]
    public void FormParsingRetainsStrictUriPercentAndUtf8Validation(
        string query,
        UriDiagnosticCode code)
    {
        UriQueryParseCoreResult result = QueryCodecCore.ParseQuery(
            query,
            CreateOptions('&', true));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Parameters, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Query));
    }

    [Test]
    public void PlusTranslationDoesNotChangeFailureOffsets()
    {
        UriQueryParseCoreResult percent = QueryCodecCore.ParseQuery(
            "a=+%GG",
            CreateOptions('&', true));
        UriQueryParseCoreResult utf8 = QueryCodecCore.ParseQuery(
            "a=+%FF",
            CreateOptions('&', true));

        Assert.Multiple(() =>
        {
            Assert.That(percent.Diagnostic!.Range.Offset, Is.EqualTo(4));
            Assert.That(utf8.Diagnostic!.Range.Offset, Is.EqualTo(3));
        });
    }

    [Test]
    public void OutputLimitsApplyAfterModeSpecificConversion()
    {
        UriQueryParameterValue[] space = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue(" ", false, "")
        };
        UriQueryParameterValue[] plus = new UriQueryParameterValue[]
        {
            new UriQueryParameterValue("+", false, "")
        };
        UriQueryOptions formOne = CreateOptions('&', true);
        formOne.Limits.MaxOutputCharacters = 1;
        UriQueryOptions plainOne = CreateOptions('&', false);
        plainOne.Limits.MaxOutputCharacters = 1;
        UriQueryOptions formTwo = CreateOptions('&', true);
        formTwo.Limits.MaxOutputCharacters = 2;

        UriQueryComposeCoreResult compactSpace = QueryCodecCore.ComposeQuery(
            space,
            formOne);
        UriQueryComposeCoreResult plainSpace = QueryCodecCore.ComposeQuery(
            space,
            plainOne);
        UriQueryComposeCoreResult expandedPlus = QueryCodecCore.ComposeQuery(
            plus,
            formTwo);

        Assert.Multiple(() =>
        {
            Assert.That(compactSpace.Success, Is.True, compactSpace.Diagnostic?.Message);
            Assert.That(compactSpace.Text, Is.EqualTo("+"));
            AssertOutputLimitFailure(plainSpace);
            AssertOutputLimitFailure(expandedPlus);
        });
    }

    private static UriQueryOptions CreateOptions(
        char delimiter,
        bool formUrlEncoded)
    {
        UriQueryOptions options = new UriQueryOptions();
        options.PairDelimiter = delimiter;
        options.FormUrlEncoded = formUrlEncoded;
        return options;
    }

    private static void AssertSuccess(
        UriQueryParseCoreResult result,
        char delimiter,
        int count)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Parameters, Has.Length.EqualTo(count));
        Assert.That(result.PairDelimiter, Is.EqualTo(delimiter));
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

    private static void AssertOutputLimitFailure(UriQueryComposeCoreResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
    }
}
