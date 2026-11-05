using System.Text;
using email;

namespace email.Tests;

public class EmailDateValidatorTests
{
    [Test]
    public void AcceptsSemanticallyConsistentParsedValue()
    {
        EmailDateTime value = Parse("Mon, 29 Feb 2016 12:34:56 +0530");

        EmailDateValidationResult result = new EmailDateValidator().Validate(value);

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(value.Validate().IsValid(), Is.True);
    }

    [Test]
    public void RejectsImpossibleCalendarDay()
    {
        EmailDateTime value = Create(2023, 2, 29, 0, true, false, false, EmailDateDayOfWeek.Sunday, true, EmailDateZoneKind.Numeric, 0, true);

        EmailDateValidationResult result = value.Validate();

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateSemanticInvalid), Is.True);
    }

    [Test]
    public void RejectsYearBeforeRfcMinimum()
    {
        EmailDateTime value = Create(1899, 1, 1, 0, true, false, false, EmailDateDayOfWeek.Sunday, true, EmailDateZoneKind.Numeric, 0, true);

        Assert.That(value.Validate().IsValid(), Is.False);
        Assert.That(value.Validate().GetIssues().ContainsCode(EmailIssueCode.DateSemanticInvalid), Is.True);
    }

    [Test]
    public void RejectsMismatchedDayOfWeek()
    {
        EmailDateTime value = Parse("Tue, 1 Jan 2024 00:00 +0000");

        EmailDateValidationResult result = new EmailDateValidator().Validate(value);

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateDayOfWeekMismatch), Is.True);
    }

    [TestCase(60, true, false)]
    [TestCase(59, true, true)]
    [TestCase(1, false, false)]
    public void RejectsInconsistentSecondMetadata(int second, bool secondPresent, bool leapSecond)
    {
        EmailDateTime value = Create(2024, 1, 1, second, secondPresent, leapSecond, false, EmailDateDayOfWeek.Sunday, true, EmailDateZoneKind.Numeric, 0, true);

        EmailDateValidationResult result = value.Validate();

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateSemanticInvalid), Is.True);
    }

    [TestCase(EmailDateZoneKind.Numeric, 1, false)]
    [TestCase(EmailDateZoneKind.ObsoleteNamed, 0, false)]
    [TestCase(EmailDateZoneKind.ObsoleteMilitary, 0, true)]
    [TestCase(EmailDateZoneKind.UnknownNamed, 0, true)]
    [TestCase(EmailDateZoneKind.Numeric, 6000, true)]
    public void RejectsInconsistentOrUnrepresentableZoneMetadata(EmailDateZoneKind kind, int offset, bool known)
    {
        EmailDateTime value = Create(2024, 1, 1, 0, false, false, false, EmailDateDayOfWeek.Sunday, true, kind, offset, known);

        EmailDateValidationResult result = value.Validate();

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateSemanticInvalid), Is.True);
    }

    [Test]
    public void ValidationResultsDefensivelySnapshotIssues()
    {
        EmailDateValidationResult result = Parse("1 Jan 2024 00:00 +0000").Validate();
        EmailIssueList issues = result.GetIssues();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.DateSemanticInvalid, "external"));

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    static EmailDateTime Parse(string value)
    {
        return new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Tolerant).GetValue()!;
    }

    static EmailDateTime Create(int year, int month, int day, int second, bool secondPresent, bool leapSecond, bool dayOfWeekPresent, EmailDateDayOfWeek dayOfWeek, bool dayOfWeekMatches, EmailDateZoneKind zoneKind, int zoneOffsetMinutes, bool zoneOffsetKnown)
    {
        string originalZone = zoneOffsetKnown ? "+0000" : "LOCAL";
        return new EmailDateTime(year, month, day, 0, 0, second, secondPresent, leapSecond, dayOfWeekPresent, dayOfWeek, dayOfWeekMatches, zoneKind, originalZone, zoneOffsetMinutes, zoneOffsetKnown, false, 4, new byte[0], new EmailSourcePosition(0, 1, 1));
    }
}
