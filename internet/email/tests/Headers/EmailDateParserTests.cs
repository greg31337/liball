using System.Text;
using email;

namespace email.Tests;

public class EmailDateParserTests
{
    [Test]
    public void ParsesCanonicalDateAndPreservesItsRepresentation()
    {
        const string input = "Fri, 21 Nov 1997 09:55:06 -0600";

        EmailDateParseResult result = Parse(input, EmailDateParseMode.Strict);

        Assert.That(result.HasValue(), Is.True);
        EmailDateTime value = result.GetValue()!;
        Assert.That(value!.GetYear(), Is.EqualTo(1997));
        Assert.That(value.GetMonth(), Is.EqualTo(11));
        Assert.That(value.GetDay(), Is.EqualTo(21));
        Assert.That(value.GetHour(), Is.EqualTo(9));
        Assert.That(value.GetMinute(), Is.EqualTo(55));
        Assert.That(value.HasSecond(), Is.True);
        Assert.That(value.GetSecond(), Is.EqualTo(6));
        Assert.That(value.HasDayOfWeek(), Is.True);
        Assert.That(value.GetDayOfWeek(), Is.EqualTo(EmailDateDayOfWeek.Friday));
        Assert.That(value.DoesDayOfWeekMatch(), Is.True);
        Assert.That(value.GetZoneKind(), Is.EqualTo(EmailDateZoneKind.Numeric));
        Assert.That(value.HasKnownZoneOffset(), Is.True);
        Assert.That(value.GetZoneOffsetMinutes(), Is.EqualTo(-360));
        Assert.That(value.GetOriginalZone(), Is.EqualTo("-0600"));
        Assert.That(value.UsesObsoleteSyntax(), Is.False);
        Assert.That(value.GetOriginalYearDigitCount(), Is.EqualTo(4));
        Assert.That(Encoding.ASCII.GetString(value.GetRawBytes()), Is.EqualTo(input));
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(input.Length));
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesDateWithoutOptionalSeconds()
    {
        EmailDateParseResult result = Parse("21 Nov 1997 09:55 +0000", EmailDateParseMode.Strict);

        Assert.That(result.HasValue(), Is.True);
        Assert.That(result.GetValue()!.HasSecond(), Is.False);
        Assert.That(result.GetValue()!.GetSecond(), Is.Zero);
        Assert.That(result.GetValue()!.IsLeapSecond(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void AcceptsAndIdentifiesLeapSecond()
    {
        EmailDateParseResult result = Parse("31 Dec 2016 23:59:60 +0000", EmailDateParseMode.Strict);

        Assert.That(result.HasValue(), Is.True);
        Assert.That(result.GetValue()!.GetSecond(), Is.EqualTo(60));
        Assert.That(result.GetValue()!.IsLeapSecond(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DistinguishesKnownUtcFromUnknownLocalZone()
    {
        EmailDateParseResult known = Parse("1 Jan 2024 00:00 +0000", EmailDateParseMode.Strict);
        EmailDateParseResult unknown = Parse("1 Jan 2024 00:00 -0000", EmailDateParseMode.Strict);

        Assert.That(known.GetValue()!.HasKnownZoneOffset(), Is.True);
        Assert.That(known.GetValue()!.IsUnknownZone(), Is.False);
        Assert.That(unknown.GetValue()!.HasKnownZoneOffset(), Is.False);
        Assert.That(unknown.GetValue()!.IsUnknownZone(), Is.True);
        Assert.That(unknown.GetValue()!.GetZoneOffsetMinutes(), Is.Zero);
        Assert.That(unknown.GetValue()!.GetOriginalZone(), Is.EqualTo("-0000"));
        Assert.That(unknown.GetIssues().GetCount(), Is.Zero);
    }

    [TestCase("1 Jan 49 00:00 +0000", 2049, 2)]
    [TestCase("1 Jan 50 00:00 +0000", 1950, 2)]
    [TestCase("1 Jan 999 00:00 +0000", 2899, 3)]
    public void MapsObsoleteYearsUsingRfcRules(string input, int expectedYear, int expectedDigits)
    {
        EmailDateParseResult tolerant = Parse(input, EmailDateParseMode.Tolerant);
        EmailDateParseResult strict = Parse(input, EmailDateParseMode.Strict);

        Assert.That(tolerant.GetValue()!.GetYear(), Is.EqualTo(expectedYear));
        Assert.That(tolerant.GetValue()!.GetOriginalYearDigitCount(), Is.EqualTo(expectedDigits));
        Assert.That(tolerant.GetValue()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.DateObsoleteSyntax), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasValue(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
    }

    [TestCase("UT", 0)]
    [TestCase("GMT", 0)]
    [TestCase("EDT", -240)]
    [TestCase("EST", -300)]
    [TestCase("CDT", -300)]
    [TestCase("CST", -360)]
    [TestCase("MDT", -360)]
    [TestCase("MST", -420)]
    [TestCase("PDT", -420)]
    [TestCase("PST", -480)]
    public void MapsObsoleteNamedZones(string zone, int expectedMinutes)
    {
        EmailDateParseResult result = Parse("1 Jan 2024 00:00 " + zone, EmailDateParseMode.Tolerant);

        EmailDateTime value = result.GetValue()!;
        Assert.That(value!.GetZoneKind(), Is.EqualTo(EmailDateZoneKind.ObsoleteNamed));
        Assert.That(value.HasKnownZoneOffset(), Is.True);
        Assert.That(value.GetZoneOffsetMinutes(), Is.EqualTo(expectedMinutes));
        Assert.That(value.GetOriginalZone(), Is.EqualTo(zone));
        Assert.That(value.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateObsoleteSyntax), Is.True);
    }

    [TestCase("Z", EmailDateZoneKind.ObsoleteMilitary)]
    [TestCase("A", EmailDateZoneKind.ObsoleteMilitary)]
    [TestCase("J", EmailDateZoneKind.UnknownNamed)]
    [TestCase("LOCAL", EmailDateZoneKind.UnknownNamed)]
    public void PreservesUnknownAlphabeticZones(string zone, EmailDateZoneKind expectedKind)
    {
        EmailDateParseResult result = Parse("1 Jan 2024 00:00 " + zone, EmailDateParseMode.Tolerant);

        EmailDateTime value = result.GetValue()!;
        Assert.That(value!.GetZoneKind(), Is.EqualTo(expectedKind));
        Assert.That(value.GetOriginalZone(), Is.EqualTo(zone));
        Assert.That(value.HasKnownZoneOffset(), Is.False);
        Assert.That(value.IsUnknownZone(), Is.True);
        Assert.That(value.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateUnknownZone), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateObsoleteSyntax), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RetainsDateAndReportsMismatchedDayOfWeek()
    {
        EmailDateParseResult tolerant = Parse("Mon, 21 Nov 1997 09:55:06 -0600", EmailDateParseMode.Tolerant);
        EmailDateParseResult strict = Parse("Mon, 21 Nov 1997 09:55:06 -0600", EmailDateParseMode.Strict);

        Assert.That(tolerant.HasValue(), Is.True);
        Assert.That(tolerant.GetValue()!.DoesDayOfWeekMatch(), Is.False);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.DateDayOfWeekMismatch), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasValue(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ParsesObsoleteCommentsAndWhitespaceAroundTimeColons()
    {
        EmailDateParseResult result = Parse("Fri, 21 (day) Nov 1997 09 : 55 : 06 -0600", EmailDateParseMode.Tolerant);

        Assert.That(result.HasValue(), Is.True);
        Assert.That(result.GetValue()!.GetSecond(), Is.EqualTo(6));
        Assert.That(result.GetValue()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateObsoleteSyntax), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [TestCase("29 Feb 2023 12:00 +0000")]
    [TestCase("1 Jan 2024 24:00 +0000")]
    [TestCase("1 Jan 2024 12:60 +0000")]
    [TestCase("1 Jan 2024 12:00:61 +0000")]
    [TestCase("1 Jan 2024 12:00 +1260")]
    [TestCase("1 Jan 1899 12:00 +0000")]
    public void RejectsSemanticallyInvalidDateComponents(string input)
    {
        EmailDateParseResult result = Parse(input, EmailDateParseMode.Tolerant);

        Assert.That(result.HasValue(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateSemanticInvalid), Is.True);
    }

    [Test]
    public void RejectsMalformedDateStructure()
    {
        EmailDateParseResult result = Parse("Fri, 21 Nov 1997 0955 -0600", EmailDateParseMode.Tolerant);

        Assert.That(result.HasValue(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateMalformed), Is.True);
    }

    [Test]
    public void AppliesInputLimitBeforeParsing()
    {
        EmailDateParserOptions options = new EmailDateParserOptions();
        options.SetMaximumInputBytes(10);

        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("1 Jan 2024 00:00 +0000"), options);

        Assert.That(result.HasValue(), Is.False);
        Assert.That(result.GetConsumedByteCount(), Is.Zero);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateInputLimitExceeded), Is.True);
    }

    [Test]
    public void TracksInitialPositionForValuesAndZoneIssues()
    {
        const string input = "1 Jan 2024 00:00 LOCAL";
        EmailDateParserOptions options = new EmailDateParserOptions();
        EmailSourcePosition initial = new EmailSourcePosition(100, 7, 4);

        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes(input), options, initial);

        EmailSourcePosition valuePosition = result.GetValue()!.GetSourcePosition();
        Assert.That(valuePosition.GetByteOffset(), Is.EqualTo(100));
        Assert.That(valuePosition.GetLineNumber(), Is.EqualTo(7));
        Assert.That(valuePosition.GetColumnNumber(), Is.EqualTo(4));
        EmailIssue unknownZone = FindIssue(result.GetIssues(), EmailIssueCode.DateUnknownZone);
        Assert.That(unknownZone.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(117));
        Assert.That(unknownZone.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(7));
        Assert.That(unknownZone.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(21));
    }

    [Test]
    public void DateValueDefensivelyCopiesRawBytes()
    {
        byte[] input = Encoding.ASCII.GetBytes("1 Jan 2024 00:00 +0000");
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(input, EmailDateParseMode.Strict);

        input[0] = (byte)'9';
        byte[] firstCopy = result.GetValue()!.GetRawBytes();
        firstCopy[0] = (byte)'8';

        Assert.That(Encoding.ASCII.GetString(result.GetValue()!.GetRawBytes()), Is.EqualTo("1 Jan 2024 00:00 +0000"));
    }

    static EmailDateParseResult Parse(string input, EmailDateParseMode mode)
    {
        return new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(input), mode);
    }

    static EmailIssue FindIssue(EmailIssueList issues, EmailIssueCode code)
    {
        int index = 0;
        while (index < issues.GetCount())
        {
            if (issues.GetAt(index).GetCode() == code)
            {
                return issues.GetAt(index);
            }

            index++;
        }

        Assert.Fail("Expected issue code " + code + ".");
        return null!;
    }
}
