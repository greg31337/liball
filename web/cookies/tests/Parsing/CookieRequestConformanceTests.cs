namespace cookies.Tests;

public class CookieRequestConformanceTests
{
    [Test]
    public void MultipleFieldsPreserveDuplicatesEmptyValuesAndGlobalOrder()
    {
        ICookieParser parser = new CookieParser();

        CookieHeaderParseResult result = parser.TryParseCookieFields(
            ["a=1; a=2; empty=", "b=3; a=4"],
            new CookieParseOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(
            new[] { "a", "a", "empty", "b", "a" }));
        Assert.That(result.Pairs.Select(pair => pair.Value), Is.EqualTo(
            new[] { "1", "2", "", "3", "4" }));
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void InvalidSegmentsAreReportedWithoutDiscardingValidPairs()
    {
        ICookieParser parser = new CookieParser();

        CookieHeaderParseResult result = parser.TryParseCookieFields(
            ["first=1;; missing; =bad; bad name=2; badvalue=a b; last=3;"],
            new CookieParseOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(
            new[] { "first", "last" }));
        Assert.That(result.Pairs.Select(pair => pair.Value), Is.EqualTo(
            new[] { "1", "3" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new[]
            {
                CookieErrorCode.IgnoredSegment,
                CookieErrorCode.MalformedSyntax,
                CookieErrorCode.InvalidName,
                CookieErrorCode.InvalidName,
                CookieErrorCode.InvalidValue,
                CookieErrorCode.IgnoredSegment
            }));
    }

    [Test]
    public void CompatibleQuotedPairsRoundTripWhileStandardsModeRejectsThem()
    {
        ICookieParser parser = new CookieParser();
        ICookieSerializer serializer = new CookieSerializer();
        CookieParseOptions compatibleParsing = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible
        };
        CookieSerializationOptions compatibleSerialization =
            new CookieSerializationOptions
            {
                ComplianceMode = CookieComplianceMode.Compatible,
                PreserveQuotedValues = true
            };
        string field = "quoted=\"two words,ok\"; empty=\"\"; plain=3";

        CookieHeaderParseResult compatible = parser.TryParseCookieFields(
            [field], compatibleParsing);
        CookieHeaderParseResult standards = parser.TryParseCookieFields(
            [field], new CookieParseOptions());
        string serialized = serializer.SerializeCookiePairs(
            compatible.Pairs, compatibleSerialization);

        Assert.That(compatible.Success, Is.True);
        Assert.That(compatible.Pairs.Select(pair => pair.Value), Is.EqualTo(
            new[] { "two words,ok", "", "3" }));
        Assert.That(compatible.Pairs.Select(pair => pair.IsQuoted), Is.EqualTo(
            new[] { true, true, false }));
        Assert.That(compatible.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.CompatibilityRecovery), Is.EqualTo(2));
        Assert.That(serialized,
            Is.EqualTo("quoted=\"two words,ok\"; empty=\"\"; plain=3"));
        Assert.That(standards.Success, Is.True);
        Assert.That(standards.Pairs.Select(pair => pair.Name), Is.EqualTo(
            new[] { "plain" }));
        Assert.That(standards.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new[] { CookieErrorCode.InvalidValue, CookieErrorCode.InvalidValue }));
    }

    [Test]
    public void FieldSplittingIsGreedyOnlyAtPairBoundariesAndReparsesInOrder()
    {
        ICookieSerializer serializer = new CookieSerializer();
        ICookieParser parser = new CookieParser();
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "a", Value = "2" },
            new CookiePair { Name = "empty", Value = "" },
            new CookiePair { Name = "z", Value = "9" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult fields = serializer.SerializeCookieFields(pairs, options);
        CookiePair[] reparsed = parser.ParseCookieFields(
            fields.FieldValues, new CookieParseOptions());

        Assert.That(fields.Success, Is.True);
        Assert.That(fields.FieldValues, Is.EqualTo(
            new[] { "a=1; a=2", "empty=", "z=9" }));
        Assert.That(reparsed.Select(pair => pair.Name), Is.EqualTo(
            new[] { "a", "a", "empty", "z" }));
        Assert.That(reparsed.Select(pair => pair.Value), Is.EqualTo(
            new[] { "1", "2", "", "9" }));
    }

    [Test]
    public void IndivisibleOverLimitPairReturnsAnExplicitFailureWithoutPartialOutput()
    {
        ICookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;
        CookiePair[] pairs =
        [
            new CookiePair { Name = "large", Value = "value" },
            new CookiePair { Name = "ok", Value = "1" }
        ];

        CookieSelectionResult result = serializer.SerializeCookieFields(pairs, options);
        CookieSerializationException exception =
            Assert.Throws<CookieSerializationException>(() =>
                serializer.SerializeCookiePairs(pairs, options));

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Issues.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
        Assert.That(exception.Message,
            Is.EqualTo("Cookie serialization failed with error code 32."));
    }

    [TestCase("safe=1", "bad=2\0injected")]
    [TestCase("safe=1", "bad=2\rInjected: value")]
    [TestCase("safe=1", "bad=2\nInjected: value")]
    public void InjectionInAnyInputFieldIsFatalAndReturnsNoPartialPairs(
        string first,
        string second)
    {
        ICookieParser parser = new CookieParser();

        CookieHeaderParseResult result = parser.TryParseCookieFields(
            [first, second], new CookieParseOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.Issues.Single().Severity,
            Is.EqualTo(CookieParseSeverity.Error));
        Assert.That(result.Issues[0].SafeFragment, Is.EqualTo("?"));
    }

    [TestCase("bad\0value")]
    [TestCase("bad\rInjected: value")]
    [TestCase("bad\nInjected: value")]
    public void InjectionInAnyOutputPairIsRejectedWithoutReflectingInput(
        string value)
    {
        ICookieSerializer serializer = new CookieSerializer();

        CookieSerializationException exception =
            Assert.Throws<CookieSerializationException>(() =>
                serializer.SerializeCookiePairs(
                    [new CookiePair { Name = "id", Value = value }],
                    new CookieSerializationOptions()));

        Assert.That(exception.Message,
            Is.EqualTo("Cookie serialization failed with error code 2."));
        Assert.That(exception.Message, Does.Not.Contain("Injected"));
        Assert.That(exception.Message, Does.Not.Contain("bad"));
    }
}
