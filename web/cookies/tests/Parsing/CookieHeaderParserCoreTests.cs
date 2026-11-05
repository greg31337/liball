namespace cookies.Tests;

public class CookieHeaderParserCoreTests
{
    [Test]
    public void ParserPreservesPairDuplicateAndFieldOrder()
    {
        CookieHeaderParseResult result = Parse(
            new string[] { "a=1; a=2; empty=", "b=3; a=4" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(
            new string[] { "a", "a", "empty", "b", "a" }));
        Assert.That(result.Pairs.Select(pair => pair.Value), Is.EqualTo(
            new string[] { "1", "2", "", "3", "4" }));
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ParserTrimsPairWhitespaceAndRetainsEqualsAfterTheFirst()
    {
        CookieHeaderParseResult result = Parse(
            new string[] { "  token = a=b=c  ; empty=   " });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs, Has.Length.EqualTo(2));
        Assert.That(result.Pairs[0].Name, Is.EqualTo("token"));
        Assert.That(result.Pairs[0].Value, Is.EqualTo("a=b=c"));
        Assert.That(result.Pairs[1].Name, Is.EqualTo("empty"));
        Assert.That(result.Pairs[1].Value, Is.Empty);
    }

    [Test]
    public void MalformedSegmentsAreSkippedIndependentlyWithOrderedIssues()
    {
        CookieHeaderParseResult result = Parse(new string[]
        {
            "good=1;; missing; =bad; bad name=2; badvalue=a b; final=3;"
        });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "good", "final" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(new CookieErrorCode[]
        {
            CookieErrorCode.IgnoredSegment,
            CookieErrorCode.MalformedSyntax,
            CookieErrorCode.InvalidName,
            CookieErrorCode.InvalidName,
            CookieErrorCode.InvalidValue,
            CookieErrorCode.IgnoredSegment
        }));
        Assert.That(result.Issues.All(issue => issue.HasOffset), Is.True);
    }

    [Test]
    public void EmptyAndDelimiterOnlyFieldsReportEveryEmptySegment()
    {
        CookieHeaderParseResult result = Parse(new string[] { "", ";", "   " });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(4));
        Assert.That(result.Issues.All(issue => issue.ErrorCode == CookieErrorCode.IgnoredSegment), Is.True);
    }

    [TestCase("safe=1", "bad=2\0x")]
    [TestCase("safe=1", "bad=2\rInjected: x")]
    [TestCase("safe=1", "bad=2\nInjected: x")]
    public void HeaderInjectionInAnyFieldIsFatalAndReturnsNoPartialPairs(string first, string second)
    {
        CookieHeaderParseResult result = Parse(new string[] { first, second });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].SafeFragment, Is.EqualTo("?"));
    }

    [Test]
    public void ModernProfileRejectsNonTabControlsBeforeReturningPairs()
    {
        CookieParseOptions options = ModernOptions();

        CookieHeaderParseResult result = CookieHeaderParserCore.Parse(
            new string[] { "safe=1", "bad=a\u007fb" },
            options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(result.Pairs, Is.Empty);
    }

    [Test]
    public void BaselineProfileTreatsAControlAsAnInvalidSegment()
    {
        CookieHeaderParseResult result = Parse(new string[] { "bad=a\u007fb; good=1" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "good" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new CookieErrorCode[] { CookieErrorCode.InvalidValue }));
    }

    [Test]
    public void ModernFieldAndPairLimitsAreEnforced()
    {
        CookieParseOptions pairOptions = ModernOptions();
        pairOptions.Limits.MaximumNameValueOctets = 3;
        CookieHeaderParseResult pairResult = CookieHeaderParserCore.Parse(
            new string[] { "wide=1; ok=1" },
            pairOptions);

        CookieParseOptions fieldOptions = ModernOptions();
        fieldOptions.Limits.MaximumInputLineOctets = 3;
        CookieHeaderParseResult fieldResult = CookieHeaderParserCore.Parse(
            new string[] { "é=x" },
            fieldOptions);

        Assert.That(pairResult.Success, Is.True);
        Assert.That(pairResult.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "ok" }));
        Assert.That(pairResult.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.NameValueLimitExceeded));
        Assert.That(fieldResult.Success, Is.False);
        Assert.That(fieldResult.ErrorCode, Is.EqualTo(CookieErrorCode.InputLimitExceeded));
    }

    [Test]
    public void ModernPairLimitSkipsOnlyTheOversizedPair()
    {
        CookieParseOptions options = ModernOptions();
        options.Limits.MaximumNameValueOctets = 4;

        CookieHeaderParseResult result = CookieHeaderParserCore.Parse(
            new string[] { "large=value; ok=1" },
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "ok" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new CookieErrorCode[] { CookieErrorCode.NameValueLimitExceeded }));
    }

    [Test]
    public void CompatibleProfileRecoversSafeQuotedPairs()
    {
        CookieParseOptions options = CompatibleOptions();

        CookieHeaderParseResult result = CookieHeaderParserCore.Parse(
            new string[] { "first=\"a value,with commas\"; empty=\"\"", "last=3" },
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs, Has.Length.EqualTo(3));
        Assert.That(result.Pairs[0].Value, Is.EqualTo("a value,with commas"));
        Assert.That(result.Pairs[0].IsQuoted, Is.True);
        Assert.That(result.Pairs[1].Value, Is.Empty);
        Assert.That(result.Pairs[1].IsQuoted, Is.True);
        Assert.That(result.Pairs[2].IsQuoted, Is.False);
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.CompatibilityRecovery),
            Is.EqualTo(2));
    }

    [Test]
    public void UnsafeQuotedSegmentDoesNotPreventLaterPairs()
    {
        CookieHeaderParseResult result = CookieHeaderParserCore.Parse(
            new string[] { "bad=\"back\\slash\"; good=1" },
            CompatibleOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "good" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new CookieErrorCode[] { CookieErrorCode.InvalidValue }));
    }

    [Test]
    public void StandardsProfileDoesNotApplyCompatibleQuotedRecovery()
    {
        CookieHeaderParseResult result = Parse(new string[] { "quoted=\"value\"; good=1" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "good" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new CookieErrorCode[] { CookieErrorCode.InvalidValue }));
    }

    [Test]
    public void CompatibleRecoveryReportingCanBeSuppressedWithoutChangingPairs()
    {
        CookieParseOptions options = CompatibleOptions();
        options.ReportCompatibilityRecoveries = false;

        CookieHeaderParseResult result = CookieHeaderParserCore.Parse(
            new string[] { "quoted=\"value\"" },
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs[0].Value, Is.EqualTo("value"));
        Assert.That(result.Pairs[0].IsQuoted, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void EmptyFieldArrayProducesAnEmptySuccessfulResult()
    {
        CookieHeaderParseResult result = Parse([]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void NullFieldCollectionOrElementIsFatalMalformedSyntax()
    {
        CookieHeaderParseResult nullCollection = CookieHeaderParserCore.Parse(null!, new CookieParseOptions());
        CookieHeaderParseResult nullElement = CookieHeaderParserCore.Parse(
            new string[] { null! },
            new CookieParseOptions());

        Assert.That(nullCollection.Success, Is.False);
        Assert.That(nullCollection.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(nullElement.Success, Is.False);
        Assert.That(nullElement.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
    }

    static CookieHeaderParseResult Parse(string[] fields)
    {
        return CookieHeaderParserCore.Parse(fields, new CookieParseOptions());
    }

    static CookieParseOptions ModernOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };
    }

    static CookieParseOptions CompatibleOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible
        };
    }
}
