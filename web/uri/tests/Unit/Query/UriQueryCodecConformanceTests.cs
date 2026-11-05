namespace uri.Tests;

public class UriQueryCodecConformanceTests
{
    [Test]
    public void RoundTripPreservesDuplicateOrderAndEveryEmptyState()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        string query = "a=1&&a=1&flag&=value&empty=&";

        UriQueryResult parsed = codec.ParseQuery(query, options);
        UriQueryResult composed = codec.ComposeQuery(parsed.Parameters, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(parsed, query, '&', 7);
            AssertParameter(parsed.Parameters[0], "a", true, "1");
            AssertParameter(parsed.Parameters[1], "", false, "");
            AssertParameter(parsed.Parameters[2], "a", true, "1");
            AssertParameter(parsed.Parameters[3], "flag", false, "");
            AssertParameter(parsed.Parameters[4], "", true, "value");
            AssertParameter(parsed.Parameters[5], "empty", true, "");
            AssertParameter(parsed.Parameters[6], "", false, "");
            AssertSuccess(composed, query, '&', 7);
        });
    }

    [Test]
    public void EmptyQueryAndDelimiterOnlyQueryRemainDistinct()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();

        UriQueryResult empty = codec.ParseQuery("", options);
        UriQueryResult delimiter = codec.ParseQuery("&", options);
        UriQueryResult composedEmpty = codec.ComposeQuery(
            new UriQueryParameter[0],
            options);

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, "", '&', 0);
            AssertSuccess(delimiter, "&", '&', 2);
            AssertParameter(delimiter.Parameters[0], "", false, "");
            AssertParameter(delimiter.Parameters[1], "", false, "");
            AssertSuccess(composedEmpty, "", '&', 0);
        });
    }

    [Test]
    public void SelectedSeparatorAloneSplitsAndIsEncodedInsideData()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        options.PairDelimiter = ';';

        UriQueryResult parsed = codec.ParseQuery(
            "a%3Bb=x%3By;a&b=c%26d",
            options);
        UriQueryResult composed = codec.ComposeQuery(parsed.Parameters, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(parsed, "a%3Bb=x%3By;a&b=c%26d", ';', 2);
            AssertParameter(parsed.Parameters[0], "a;b", true, "x;y");
            AssertParameter(parsed.Parameters[1], "a&b", true, "c&d");
            AssertSuccess(composed, "a%3Bb=x%3By;a&b=c&d", ';', 2);
        });
    }

    [Test]
    public void PlainAndFormModesDistinguishLiteralEncodedPlusAndSpace()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions plainOptions = new UriQueryOptions();
        UriQueryOptions formOptions = new UriQueryOptions();
        formOptions.FormUrlEncoded = true;
        string query = "literal=+&encoded=%2B&space=%20&mixed=+%2B%20";

        UriQueryResult plain = codec.ParseQuery(query, plainOptions);
        UriQueryResult form = codec.ParseQuery(query, formOptions);
        UriQueryParameter[] parameters = new UriQueryParameter[]
        {
            Parameter("a b", true, "+ +")
        };
        UriQueryResult plainComposition = codec.ComposeQuery(parameters, plainOptions);
        UriQueryResult formComposition = codec.ComposeQuery(parameters, formOptions);

        Assert.Multiple(() =>
        {
            AssertParameter(plain.Parameters[0], "literal", true, "+");
            AssertParameter(plain.Parameters[1], "encoded", true, "+");
            AssertParameter(plain.Parameters[2], "space", true, " ");
            AssertParameter(plain.Parameters[3], "mixed", true, "++ ");
            AssertParameter(form.Parameters[0], "literal", true, " ");
            AssertParameter(form.Parameters[1], "encoded", true, "+");
            AssertParameter(form.Parameters[2], "space", true, " ");
            AssertParameter(form.Parameters[3], "mixed", true, " + ");
            Assert.That(plainComposition.Text, Is.EqualTo("a%20b=+%20+"));
            Assert.That(formComposition.Text, Is.EqualTo("a+b=%2B+%2B"));
        });
    }

    [Test]
    public void EncodedDelimitersAndUtf8UnicodeDecodeWithoutChangingItemBoundaries()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();

        UriQueryResult parsed = codec.ParseQuery(
            "emoji=%F0%9F%98%80&snow=%E2%98%83&reserved=%26%3D%3B",
            options);
        UriQueryResult composed = codec.ComposeQuery(parsed.Parameters, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(
                parsed,
                "emoji=%F0%9F%98%80&snow=%E2%98%83&reserved=%26%3D%3B",
                '&',
                3);
            AssertParameter(parsed.Parameters[0], "emoji", true, "😀");
            AssertParameter(parsed.Parameters[1], "snow", true, "☃");
            AssertParameter(parsed.Parameters[2], "reserved", true, "&=;");
            AssertSuccess(
                composed,
                "emoji=%F0%9F%98%80&snow=%E2%98%83&reserved=%26=;",
                '&',
                3);
        });
    }

    [Test]
    public void OrdinalSortCoversCaseUnicodeValueStateAndStableTies()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryParameter firstMissing = Parameter("a", false, "first");
        UriQueryParameter secondMissing = Parameter("a", false, "second");
        UriQueryParameter[] parameters = new UriQueryParameter[]
        {
            Parameter("☃", true, "snow"),
            Parameter("é", true, "accent"),
            Parameter("a", true, "2"),
            secondMissing,
            Parameter("Z", true, "upper"),
            Parameter("a", true, "1"),
            firstMissing
        };

        UriQueryResult result = codec.SortQueryParameters(
            parameters,
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
            AssertParameter(result.Parameters[0], "Z", true, "upper");
            AssertParameter(result.Parameters[1], "a", false, "second");
            AssertParameter(result.Parameters[2], "a", false, "first");
            AssertParameter(result.Parameters[3], "a", true, "1");
            AssertParameter(result.Parameters[4], "a", true, "2");
            AssertParameter(result.Parameters[5], "é", true, "accent");
            AssertParameter(result.Parameters[6], "☃", true, "snow");
            Assert.That(parameters[0].Name, Is.EqualTo("☃"));
        });
    }

    [Test]
    public void DeduplicationKeepsFirstExactPairAndDistinctValueStates()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryParameter[] parameters = new UriQueryParameter[]
        {
            Parameter("a", false, "first-missing"),
            Parameter("a", false, "second-missing"),
            Parameter("a", true, ""),
            Parameter("a", true, ""),
            Parameter("a", true, "1"),
            Parameter("A", true, "1"),
            Parameter("a", true, "1")
        };

        UriQueryResult result = codec.DeduplicateQueryParameters(
            parameters,
            new UriQueryOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, "a&a=&a=1&A=1", '&', 4);
            AssertParameter(result.Parameters[0], "a", false, "first-missing");
            AssertParameter(result.Parameters[1], "a", true, "");
            AssertParameter(result.Parameters[2], "a", true, "1");
            AssertParameter(result.Parameters[3], "A", true, "1");
        });
    }

    [Test]
    public void EveryPublicMethodParticipatesInOneOrderedWorkflow()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        UriQueryResult parsed = codec.ParseQuery(
            "b=2&a=2&a=1&a=1&drop=x",
            options);
        UriQueryResult added = codec.AddQueryParameter(
            parsed.Parameters,
            Parameter("c", true, "3"),
            options);
        UriQueryResult replaced = codec.ReplaceQueryParameters(
            added.Parameters,
            "b",
            new UriQueryParameter[]
            {
                Parameter("b", true, "0"),
                Parameter("b", false, "")
            },
            options);
        UriQueryResult removed = codec.RemoveQueryParameters(
            replaced.Parameters,
            "drop",
            options);
        UriQueryResult sorted = codec.SortQueryParameters(
            removed.Parameters,
            options);
        UriQueryResult deduplicated = codec.DeduplicateQueryParameters(
            sorted.Parameters,
            options);
        UriQueryResult composed = codec.ComposeQuery(
            deduplicated.Parameters,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, parsed.Diagnostic?.Message);
            Assert.That(added.Success, Is.True, added.Diagnostic?.Message);
            Assert.That(replaced.Success, Is.True, replaced.Diagnostic?.Message);
            Assert.That(removed.Success, Is.True, removed.Diagnostic?.Message);
            Assert.That(sorted.Text, Is.EqualTo("a=1&a=1&a=2&b&b=0&c=3"));
            Assert.That(deduplicated.Text, Is.EqualTo("a=1&a=2&b&b=0&c=3"));
            AssertSuccess(composed, "a=1&a=2&b&b=0&c=3", '&', 5);
        });
    }

    [TestCase("a=%", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=%0", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=%GG", UriDiagnosticCode.InvalidPercentEncoding)]
    [TestCase("a=%FF", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=%C0%AF", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=%E2%28%A1", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=%F4%90%80%80", UriDiagnosticCode.InvalidUtf8)]
    [TestCase("a=raw space", UriDiagnosticCode.InvalidQuery)]
    [TestCase("a=#fragment", UriDiagnosticCode.InvalidQuery)]
    [TestCase("a=☃", UriDiagnosticCode.InvalidQuery)]
    public void MalformedQueryInputReturnsSpecificDiagnostics(
        string query,
        UriDiagnosticCode code)
    {
        IUriQueryCodec codec = new UriQueryCodec();

        UriQueryResult result = codec.ParseQuery(query, new UriQueryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Parameters, Is.Empty);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Query));
        });
    }

    [Test]
    public void UnsupportedSeparatorIsRejectedByParsingAndComposition()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        options.PairDelimiter = ',';

        UriQueryResult parsed = codec.ParseQuery("a=1,b=2", options);
        UriQueryResult composed = codec.ComposeQuery(
            new UriQueryParameter[] { Parameter("a", true, "1") },
            options);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(parsed);
            AssertInvalidInput(composed);
            Assert.That(parsed.PairDelimiter, Is.EqualTo(','));
            Assert.That(composed.PairDelimiter, Is.EqualTo(','));
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

    private static void AssertInvalidInput(UriQueryResult result)
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
