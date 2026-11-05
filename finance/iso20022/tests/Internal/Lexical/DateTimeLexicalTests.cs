using iso20022;

namespace iso20022.Tests;

public class DateTimeLexicalTests
{
    [TestCase("2024-02-29", 2024, false, 0, false, false)]
    [TestCase("2026-08-31Z", 2026, true, 0, true, false)]
    [TestCase("-0001-01-01-00:00", -1, true, 0, false, true)]
    [TestCase("12026-12-31+14:00", 12026, true, 840, false, false)]
    public void DateParserPreservesCalendarAndTimezoneState(string text, int expectedYear, bool hasTimezone, int offsetMinutes, bool isUtc, bool negativeZero)
    {
        IsoDateValue? result = DateTimeLexical.ParseDate(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Year, Is.EqualTo(expectedYear));
            Assert.That(result?.HasTimezone, Is.EqualTo(hasTimezone));
            Assert.That(result?.TimezoneOffsetMinutes, Is.EqualTo(offsetMinutes));
            Assert.That(result?.IsUtc, Is.EqualTo(isUtc));
            Assert.That(result?.IsNegativeTimezoneZero, Is.EqualTo(negativeZero));
            Assert.That(result?.LexicalMode, Is.EqualTo(DateTimeLexicalMode.Preserve));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(DateTimeLexical.FormatDate(result!), Is.EqualTo(text));
        });
    }

    [TestCase("0000-01-01")]
    [TestCase("2023-02-29")]
    [TestCase("2024-04-31")]
    [TestCase("026-01-01")]
    [TestCase("02026-01-01")]
    [TestCase("+2026-01-01")]
    [TestCase("2026-01-01+14:01")]
    [TestCase("2026-01-01z")]
    [TestCase(" 2026-01-01")]
    public void DateParserRejectsInvalidCalendarAndTimezoneForms(string text)
    {
        Assert.That(DateTimeLexical.ParseDate(text.AsSpan()), Is.Null);
    }

    [TestCase("00:00:00", 0, "", false, 0, false, false)]
    [TestCase("23:59:59.123400Z", 23, "123400", true, 0, true, false)]
    [TestCase("24:00:00.000+00:00", 24, "000", true, 0, false, false)]
    [TestCase("12:34:56-00:00", 12, "", true, 0, false, true)]
    [TestCase("12:34:56-14:00", 12, "", true, -840, false, false)]
    [TestCase("23:59:60Z", 23, "", true, 0, true, false)]
    public void TimeParserPreservesFractionAndTimezoneState(string text, int hour, string fractionalDigits, bool hasTimezone, int offsetMinutes, bool isUtc, bool negativeZero)
    {
        IsoTimeValue? result = DateTimeLexical.ParseTime(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Hour, Is.EqualTo(hour));
            Assert.That(result?.FractionalSecondDigits, Is.EqualTo(fractionalDigits));
            Assert.That(result?.HasTimezone, Is.EqualTo(hasTimezone));
            Assert.That(result?.TimezoneOffsetMinutes, Is.EqualTo(offsetMinutes));
            Assert.That(result?.IsUtc, Is.EqualTo(isUtc));
            Assert.That(result?.IsNegativeTimezoneZero, Is.EqualTo(negativeZero));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(DateTimeLexical.FormatTime(result!), Is.EqualTo(text));
        });
    }

    [TestCase("24:00:01")]
    [TestCase("24:00:00.001")]
    [TestCase("25:00:00")]
    [TestCase("23:60:00")]
    [TestCase("12:34:56.")]
    [TestCase("12:34")]
    [TestCase("12:34:56+15:00")]
    [TestCase("12:34:56+14:30")]
    [TestCase("12:34:56+0700")]
    public void TimeParserRejectsInvalidClockAndTimezoneForms(string text)
    {
        Assert.That(DateTimeLexical.ParseTime(text.AsSpan()), Is.Null);
    }

    [TestCase("2026-08-31T09:05:07.1200-07:00")]
    [TestCase("2024-02-29T24:00:00Z")]
    [TestCase("-0001-01-01T00:00:00")]
    public void DateTimeParserRoundTripsExactFractionAndOffset(string text)
    {
        IsoDateTimeValue? result = DateTimeLexical.ParseDateTime(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Date.HasTimezone, Is.EqualTo(result?.Time.HasTimezone));
            Assert.That(result?.Date.TimezoneOffsetMinutes, Is.EqualTo(result?.Time.TimezoneOffsetMinutes));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(DateTimeLexical.FormatDateTime(result!), Is.EqualTo(text));
        });
    }

    [TestCase("2026-08-31t09:05:07Z")]
    [TestCase("2026-08-31")]
    [TestCase("09:05:07Z")]
    [TestCase("2026-08-31TT09:05:07Z")]
    [TestCase("2026-02-29T09:05:07Z")]
    [TestCase("2026-08-31T24:00:01Z")]
    public void DateTimeParserRejectsMalformedCombinedForms(string text)
    {
        Assert.That(DateTimeLexical.ParseDateTime(text.AsSpan()), Is.Null);
    }

    [Test]
    public void DateTimeFormatterRejectsConflictingComponentTimezones()
    {
        IsoDateTimeValue value = new IsoDateTimeValue
        {
            Date = new IsoDateValue
            {
                Year = 2026,
                Month = 8,
                Day = 31,
                HasTimezone = true,
                IsUtc = true
            },
            Time = new IsoTimeValue
            {
                Hour = 9,
                Minute = 5,
                Second = 7,
                HasTimezone = true,
                TimezoneOffsetMinutes = 60
            }
        };

        Assert.That(DateTimeLexical.FormatDateTime(value), Is.Null);
    }
}
