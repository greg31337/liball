using System.Text;
using email;

namespace email.Tests;

public class EmailDateFormatterTests
{
    [Test]
    public void CanonicalizesObsoleteSyntaxAndNamedZone()
    {
        EmailDateTime value = Parse("Mon 1 jan 24 09 : 05 PST");

        EmailDateFormatResult result = new EmailDateFormatter().FormatWithValue(value);

        Assert.That(Text(result), Is.EqualTo("Mon, 01 Jan 2024 09:05:00 -0800"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void CanonicalizesEveryUnknownZoneAsMinusZero()
    {
        EmailDateTime named = Parse("1 Jan 2024 00:00 LOCAL");
        EmailDateTime military = Parse("1 Jan 2024 00:00 Z");
        EmailDateTime numeric = Parse("1 Jan 2024 00:00 -0000");

        Assert.That(Text(named.Format()), Is.EqualTo("Mon, 01 Jan 2024 00:00:00 -0000"));
        Assert.That(Text(military.Format()), Is.EqualTo("Mon, 01 Jan 2024 00:00:00 -0000"));
        Assert.That(Text(numeric.Format()), Is.EqualTo("Mon, 01 Jan 2024 00:00:00 -0000"));
    }

    [Test]
    public void CanonicalizesKnownUtcAsPlusZero()
    {
        EmailDateTime value = Parse("1 Jan 2024 00:00 GMT");

        Assert.That(Text(value.Format()), Is.EqualTo("Mon, 01 Jan 2024 00:00:00 +0000"));
    }

    [Test]
    public void PreservesLeapSecondInCanonicalOutput()
    {
        EmailDateTime value = Parse("Sat, 31 Dec 2016 23:59:60 +0000");

        EmailDateFormatResult result = value.FormatWithOptions(new EmailDateFormatOptions());

        Assert.That(Text(result), Is.EqualTo("Sat, 31 Dec 2016 23:59:60 +0000"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RecomputesMismatchedDayOfWeekAndReportsCorrection()
    {
        EmailDateTime value = Parse("Mon, 21 Nov 1997 09:55:06 -0600");

        EmailDateFormatResult result = new EmailDateFormatter().FormatWithValue(value);

        Assert.That(Text(result), Is.EqualTo("Fri, 21 Nov 1997 09:55:06 -0600"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateDayOfWeekMismatch), Is.True);
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void EmitsAtLeastFourYearDigitsWithoutTruncatingLargerYears()
    {
        EmailDateTime value = Parse("1 Jan 12024 00:00 +0000");

        Assert.That(Text(new EmailDateFormatter().FormatWithValue(value)), Is.EqualTo("Mon, 01 Jan 12024 00:00:00 +0000"));
    }

    [Test]
    public void RejectsSemanticallyInvalidValuesWithoutPartialOutput()
    {
        EmailDateTime value = Create(2023, 2, 29, 12, 0, 0, true, false, false, EmailDateDayOfWeek.Sunday, true, EmailDateZoneKind.Numeric, "+0000", 0, true);

        EmailDateFormatResult result = new EmailDateFormatter().FormatWithValue(value);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateFormattingInvalid), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void AppliesOutputLimitWithoutReturningPartialBytes()
    {
        EmailDateFormatOptions options = new EmailDateFormatOptions();
        options.SetMaximumOutputBytes(20);

        EmailDateFormatResult result = new EmailDateFormatter().FormatWithValueAndOptions(Parse("1 Jan 2024 00:00 +0000"), options);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateOutputLimitExceeded), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void FormatResultsDefensivelySnapshotBytesAndIssues()
    {
        EmailDateFormatResult result = new EmailDateFormatter().FormatWithValue(Parse("1 Jan 2024 00:00 +0000"));
        byte[] bytes = result.GetBytes();
        bytes[0] = (byte)'X';
        EmailIssueList issues = result.GetIssues();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.DateMalformed, "external"));

        Assert.That(Text(result), Is.EqualTo("Mon, 01 Jan 2024 00:00:00 +0000"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void FormatOptionsValidateAndCloneIndependently()
    {
        EmailDateFormatOptions options = new EmailDateFormatOptions();
        options.SetMaximumOutputBytes(80);
        EmailDateFormatOptions clone = options.Clone();
        clone.SetMaximumOutputBytes(40);

        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(80));
        Assert.That(clone.GetMaximumOutputBytes(), Is.EqualTo(40));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumOutputBytes(0));
    }

    static EmailDateTime Parse(string value)
    {
        return new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Tolerant).GetValue()!;
    }

    static EmailDateTime Create(int year, int month, int day, int hour, int minute, int second, bool secondPresent, bool leapSecond, bool dayOfWeekPresent, EmailDateDayOfWeek dayOfWeek, bool dayOfWeekMatches, EmailDateZoneKind zoneKind, string originalZone, int zoneOffsetMinutes, bool zoneOffsetKnown)
    {
        return new EmailDateTime(year, month, day, hour, minute, second, secondPresent, leapSecond, dayOfWeekPresent, dayOfWeek, dayOfWeekMatches, zoneKind, originalZone, zoneOffsetMinutes, zoneOffsetKnown, false, 4, new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    static string Text(EmailDateFormatResult result)
    {
        return Encoding.ASCII.GetString(result.GetBytes());
    }
}
