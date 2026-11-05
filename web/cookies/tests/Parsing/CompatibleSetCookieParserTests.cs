namespace cookies.Tests;

public class CompatibleSetCookieParserTests
{
    [TestCase("id=\"value\"", "value")]
    [TestCase("id=\"value with spaces,commas\"", "value with spaces,commas")]
    [TestCase("id=\"a=b=c\"", "a=b=c")]
    [TestCase("id=\"\"", "")]
    public void CompatibleProfileRecoversSafeQuotedValues(string text, string expectedValue)
    {
        SetCookieParseResult result = ParseCompatible(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo(expectedValue));
        Assert.That(result.Cookie.IsValueQuoted, Is.True);
        Assert.That(RecoveryCount(result), Is.EqualTo(1));
    }

    [Test]
    public void StandardsProfilesDoNotApplyQuotedValueRecovery()
    {
        CookieParseOptions baselineOptions = new CookieParseOptions();
        CookieParseOptions modernOptions = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };

        SetCookieParseResult baseline = SetCookieParserCore.Parse("id=\"value\"".AsSpan(), baselineOptions);
        SetCookieParseResult modern = SetCookieParserCore.Parse("id=\"value\"".AsSpan(), modernOptions);

        Assert.That(baseline.Cookie!.Value, Is.EqualTo("\"value\""));
        Assert.That(baseline.Cookie.IsValueQuoted, Is.False);
        Assert.That(RecoveryCount(baseline), Is.Zero);
        Assert.That(modern.Cookie!.Value, Is.EqualTo("\"value\""));
        Assert.That(modern.Cookie.IsValueQuoted, Is.False);
        Assert.That(RecoveryCount(modern), Is.Zero);
    }

    [TestCase("id=\"unterminated")]
    [TestCase("id=value\"")]
    [TestCase("id=\"back\\slash\"")]
    [TestCase("id=\"inner\"quote\"")]
    [TestCase("id=\"semi;colon\"")]
    [TestCase("id=\"café\"")]
    public void CompatibleProfileRejectsUnsafeQuotedValueRecovery(string text)
    {
        SetCookieParseResult result = ParseCompatible(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Cookie, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidValue));
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [TestCase("id=\"a\0b\"")]
    [TestCase("id=\"a\rb\"")]
    [TestCase("id=\"a\nb\"")]
    public void CompatibleProfileNeverRecoversHeaderInjection(string text)
    {
        SetCookieParseResult result = ParseCompatible(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [Test]
    public void CompatibleRecoveryReportingCanBeSuppressed()
    {
        CookieParseOptions options = CompatibleOptions();
        options.ReportCompatibilityRecoveries = false;

        SetCookieParseResult result = SetCookieParserCore.Parse(
            "id=\"value\"; Expires=09-Jun-2021 10:18:14 GMT".AsSpan(),
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo("value"));
        Assert.That(result.Cookie.IsValueQuoted, Is.True);
        Assert.That(result.Cookie.HasExpires, Is.True);
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [TestCase("09-Jun-2021 10:18:14 GMT")]
    [TestCase("\"Wed, 09 Jun 2021 10:18:14 GMT\"")]
    [TestCase("Wed,   09 Jun 2021 10:18:14 GMT")]
    [TestCase("Wednesday, 09-Jun-21 10:18:14 GMT")]
    public void CompatibleProfileReportsNoncanonicalParseableDates(string date)
    {
        SetCookieParseResult result = ParseCompatible("id=1; Expires=" + date);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.True);
        Assert.That(result.Cookie.Expires, Is.EqualTo(
            new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
        Assert.That(RecoveryCount(result), Is.EqualTo(1));
    }

    [Test]
    public void CanonicalExpiresDoesNotProduceACompatibilityRecovery()
    {
        SetCookieParseResult result = ParseCompatible(
            "id=1; Expires=Wed, 09 Jun 2021 10:18:14 GMT");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.True);
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [Test]
    public void CompatibleProfileRecoversAnOtherwiseValidDateWithMissingSeconds()
    {
        SetCookieParseResult result = ParseCompatible(
            "id=1; Expires=Wed, 09 Jun 2021 10:18 GMT");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Expires, Is.EqualTo(
            new DateTimeOffset(2021, 6, 9, 10, 18, 0, TimeSpan.Zero)));
        Assert.That(RecoveryCount(result), Is.EqualTo(1));
    }

    [Test]
    public void StandardsProfilesDoNotRecoverMissingDateSeconds()
    {
        SetCookieParseResult result = SetCookieParserCore.Parse(
            "id=1; Expires=Wed, 09 Jun 2021 10:18 GMT".AsSpan(),
            new CookieParseOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.False);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.InvalidDate), Is.True);
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [TestCase("Wed, 09 Jun 2021 99:99 GMT")]
    [TestCase("Wed, 09 Jun 2021 10:18 11:19 GMT")]
    [TestCase("not-a-date")]
    public void InvalidOrAmbiguousDateIsNotRecovered(string date)
    {
        SetCookieParseResult result = ParseCompatible("id=1; Expires=" + date);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.False);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.InvalidDate), Is.True);
        Assert.That(RecoveryCount(result), Is.Zero);
    }

    [Test]
    public void EachIndependentCompatibleRecoveryProducesOneIssue()
    {
        SetCookieParseResult result = ParseCompatible(
            "id=\"value with space\"; Expires=09-Jun-2021 10:18 GMT");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo("value with space"));
        Assert.That(result.Cookie.Expires.Second, Is.Zero);
        Assert.That(RecoveryCount(result), Is.EqualTo(2));
    }

    static SetCookieParseResult ParseCompatible(string text)
    {
        return SetCookieParserCore.Parse(text.AsSpan(), CompatibleOptions());
    }

    static CookieParseOptions CompatibleOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible
        };
    }

    static int RecoveryCount(SetCookieParseResult result)
    {
        return result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.CompatibilityRecovery);
    }
}
