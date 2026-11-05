namespace odf.Tests;

public class OdfTemporalTests
{
    [TestCase("2000-02-29", "2000", 2, 29, false, 0, "2000-02-29")]
    [TestCase("-0001-02-29", "-1", 2, 29, false, 0, "-0001-02-29")]
    [TestCase("12345-01-02", "12345", 1, 2, false, 0, "12345-01-02")]
    [TestCase("2001-01-01Z", "2001", 1, 1, true, 0, "2001-01-01Z")]
    [TestCase("2001-01-01+05:30", "2001", 1, 1, true, 330, "2001-01-01+05:30")]
    [TestCase("2001-01-01-00:00", "2001", 1, 1, true, 0, "2001-01-01Z")]
    public void DatesParseCalendarAndTimezoneFields(
        string lexical,
        string year,
        int month,
        int day,
        bool hasTimeZone,
        int offset,
        string canonical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseDate(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasDate, Is.True);
            Assert.That(result.HasTime, Is.False);
            Assert.That(result.Year.ToString(), Is.EqualTo(year));
            Assert.That(result.Month, Is.EqualTo(month));
            Assert.That(result.Day, Is.EqualTo(day));
            Assert.That(result.HasTimeZone, Is.EqualTo(hasTimeZone));
            Assert.That(result.TimeZoneMinutes, Is.EqualTo(offset));
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(OdfLexicalFormatter.FormatDate(result), Is.EqualTo(canonical));
        });
    }

    [TestCase("2002-10-10+13:00", "2002-10-09-11:00")]
    [TestCase("2002-10-10+14:00", "2002-10-09-10:00")]
    [TestCase("2002-10-10-12:00", "2002-10-11+12:00")]
    [TestCase("2002-10-10-14:00", "2002-10-11+10:00")]
    public void DateFormattingUsesRecoverableCanonicalTimezones(string lexical, string canonical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseDate(lexical);

        Assert.That(OdfLexicalFormatter.FormatDate(result), Is.EqualTo(canonical));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("0000-01-01")]
    [TestCase("+2000-01-01")]
    [TestCase("02000-01-01")]
    [TestCase("2001-02-29")]
    [TestCase("2000-13-01")]
    [TestCase("2000-04-31")]
    [TestCase("2000-1-01")]
    [TestCase("2000-01-1")]
    [TestCase("2000-01-01+14:01")]
    [TestCase("2000-01-01+15:00")]
    [TestCase(" 2000-01-01")]
    public void InvalidDatesArePreserved(string? lexical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseDate(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatDate(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("00:00:00", 0, 0, 0, "", false, 0, "00:00:00")]
    [TestCase("23:59:59.1200", 23, 59, 59, "1200", false, 0, "23:59:59.12")]
    [TestCase("24:00:00", 24, 0, 0, "", false, 0, "00:00:00")]
    [TestCase("12:30:00Z", 12, 30, 0, "", true, 0, "12:30:00Z")]
    [TestCase("13:20:00-05:00", 13, 20, 0, "", true, -300, "18:20:00Z")]
    [TestCase("00:30:00+01:00", 0, 30, 0, "", true, 60, "23:30:00Z")]
    public void TimesParseAndFormatCanonically(
        string lexical,
        int hour,
        int minute,
        int second,
        string fraction,
        bool hasTimeZone,
        int offset,
        string canonical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseTime(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasDate, Is.False);
            Assert.That(result.HasTime, Is.True);
            Assert.That(result.Hour, Is.EqualTo(hour));
            Assert.That(result.Minute, Is.EqualTo(minute));
            Assert.That(result.Second, Is.EqualTo(second));
            Assert.That(result.FractionalSecond, Is.EqualTo(fraction));
            Assert.That(result.HasTimeZone, Is.EqualTo(hasTimeZone));
            Assert.That(result.TimeZoneMinutes, Is.EqualTo(offset));
            Assert.That(OdfLexicalFormatter.FormatTime(result), Is.EqualTo(canonical));
        });
    }

    [TestCase("23:59:60", "00:00:00")]
    [TestCase("23:59:60.500Z", "00:00:00.5Z")]
    public void LeapSecondsCanonicalizeByCarryingIntoTheNextMinute(string lexical, string canonical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseTime(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Second, Is.EqualTo(60));
            Assert.That(OdfLexicalFormatter.FormatTime(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1:00:00")]
    [TestCase("01:00")]
    [TestCase("25:00:00")]
    [TestCase("24:00:01")]
    [TestCase("24:00:00.1")]
    [TestCase("12:60:00")]
    [TestCase("12:00:61")]
    [TestCase("12:00:00.")]
    [TestCase("12:00:00+01")]
    [TestCase("12:00:00+14:30")]
    public void InvalidTimesArePreserved(string? lexical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseTime(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatTime(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("2002-10-10T12:00:00-05:00", "2002", 10, 10, 12, 0, -300, "2002-10-10T17:00:00Z")]
    [TestCase("2000-02-29T23:30:00-01:00", "2000", 2, 29, 23, 30, -60, "2000-03-01T00:30:00Z")]
    [TestCase("1999-12-31T24:00:00", "1999", 12, 31, 24, 0, 0, "2000-01-01T00:00:00")]
    [TestCase("-0001-12-31T24:00:00", "-1", 12, 31, 24, 0, 0, "0001-01-01T00:00:00")]
    [TestCase("12345-01-02T03:04:05.600", "12345", 1, 2, 3, 4, 0, "12345-01-02T03:04:05.6")]
    public void DateTimesNormalizeCalendarAndTimezoneBoundaries(
        string lexical,
        string year,
        int month,
        int day,
        int hour,
        int minute,
        int offset,
        string canonical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseDateTime(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasDate, Is.True);
            Assert.That(result.HasTime, Is.True);
            Assert.That(result.Year.ToString(), Is.EqualTo(year));
            Assert.That(result.Month, Is.EqualTo(month));
            Assert.That(result.Day, Is.EqualTo(day));
            Assert.That(result.Hour, Is.EqualTo(hour));
            Assert.That(result.Minute, Is.EqualTo(minute));
            Assert.That(result.TimeZoneMinutes, Is.EqualTo(offset));
            Assert.That(OdfLexicalFormatter.FormatDateTime(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("2000-01-01 12:00:00")]
    [TestCase("2000-01-01T")]
    [TestCase("T12:00:00")]
    [TestCase("2001-02-29T12:00:00")]
    [TestCase("2000-01-01T25:00:00")]
    [TestCase("2000-01-01T12:00:00T13:00:00")]
    [TestCase("2000-01-01T12:00:00+14:01")]
    public void InvalidDateTimesArePreserved(string? lexical)
    {
        OdfTemporalValue result = OdfLexicalParser.ParseDateTime(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatDateTime(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("P1Y2M3DT10H30M", "1", "2", "3", "10", "30", "0", "", false, "P1Y2M3DT10H30M")]
    [TestCase("-P120D", "0", "0", "120", "0", "0", "0", "", true, "-P120D")]
    [TestCase("PT0.500S", "0", "0", "0", "0", "0", "0", "500", false, "PT0.5S")]
    [TestCase("P0Y1347M0D", "0", "1347", "0", "0", "0", "0", "", false, "P1347M")]
    [TestCase("PT90S", "0", "0", "0", "0", "0", "90", "", false, "PT90S")]
    [TestCase("-P0D", "0", "0", "0", "0", "0", "0", "", true, "PT0S")]
    public void DurationsParseComponentsAndFormatReducedLexicalValues(
        string lexical,
        string years,
        string months,
        string days,
        string hours,
        string minutes,
        string seconds,
        string fraction,
        bool negative,
        string canonical)
    {
        OdfDurationValue result = OdfLexicalParser.ParseDuration(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Years.ToString(), Is.EqualTo(years));
            Assert.That(result.Months.ToString(), Is.EqualTo(months));
            Assert.That(result.Days.ToString(), Is.EqualTo(days));
            Assert.That(result.Hours.ToString(), Is.EqualTo(hours));
            Assert.That(result.Minutes.ToString(), Is.EqualTo(minutes));
            Assert.That(result.Seconds.ToString(), Is.EqualTo(seconds));
            Assert.That(result.FractionalSecond, Is.EqualTo(fraction));
            Assert.That(result.IsNegative, Is.EqualTo(negative));
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(OdfLexicalFormatter.FormatDuration(result), Is.EqualTo(canonical));
        });
    }

    [Test]
    public void DurationsSupportArbitrarilyLargeComponentsAndFractions()
    {
        string lexical = "P123456789012345678901234567890Y" +
            "987654321098765432109876543210M" +
            "123456789012345678901234567890DT" +
            "999999999999999999999999999999H" +
            "888888888888888888888888888888M" +
            "777777777777777777777777777777.12345678901234567890S";
        string canonical = lexical.Substring(0, lexical.Length - 2) + "S";

        OdfDurationValue result = OdfLexicalParser.ParseDuration(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Years.ToString(), Is.EqualTo("123456789012345678901234567890"));
            Assert.That(result.Seconds.ToString(), Is.EqualTo("777777777777777777777777777777"));
            Assert.That(result.FractionalSecond, Is.EqualTo("12345678901234567890"));
            Assert.That(OdfLexicalFormatter.FormatDuration(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("P")]
    [TestCase("PT")]
    [TestCase("1D")]
    [TestCase("+P1D")]
    [TestCase("P-1D")]
    [TestCase("P1D2Y")]
    [TestCase("P1Y2Y")]
    [TestCase("PT1M1H")]
    [TestCase("P1YT")]
    [TestCase("P1.5D")]
    [TestCase("PT1.S")]
    [TestCase("PT.5S")]
    [TestCase("PT1Q")]
    [TestCase("P1DT2M3H")]
    public void InvalidDurationsArePreserved(string? lexical)
    {
        OdfDurationValue result = OdfLexicalParser.ParseDuration(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatDuration(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void NullTemporalFormatInputsProduceEmptyText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLexicalFormatter.FormatDate(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatTime(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatDateTime(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatDuration(null), Is.Empty);
        });
    }
}
