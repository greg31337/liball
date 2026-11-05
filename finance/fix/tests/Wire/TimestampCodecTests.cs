using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class TimestampCodecTests
{
    [Test]
    public void MonthYearParsesMonthCalendarDayAndWeekForms()
    {
        TimestampParseResult month = TimestampParser.ParseTimestamp("202608"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult day = TimestampParser.ParseTimestamp("20260228"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult week = TimestampParser.ParseTimestamp("202608W5"u8, FixFieldType.MonthYear, 32);

        Assert.Multiple(() =>
        {
            Assert.That(month.Success, Is.True);
            Assert.That(month.Year, Is.EqualTo(2026));
            Assert.That(month.Month, Is.EqualTo(8));
            Assert.That(month.HasDay, Is.False);
            Assert.That(day.Success, Is.True);
            Assert.That(day.Day, Is.EqualTo(28));
            Assert.That(day.HasDay, Is.True);
            Assert.That(week.Success, Is.True);
            Assert.That(week.Week, Is.EqualTo(5));
            Assert.That(week.HasWeek, Is.True);
        });

        Assert.That(FormatValue(month), Is.EqualTo("202608"));
        Assert.That(FormatValue(day), Is.EqualTo("20260228"));
        Assert.That(FormatValue(week), Is.EqualTo("202608w5"));
    }

    [Test]
    public void MonthYearRejectsInvalidCalendarAndWeekValues()
    {
        TimestampParseResult monthZero = TimestampParser.ParseTimestamp("202600"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult monthThirteen = TimestampParser.ParseTimestamp("202613"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult impossibleDay = TimestampParser.ParseTimestamp("20260229"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult weekZero = TimestampParser.ParseTimestamp("202608w0"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult weekSix = TimestampParser.ParseTimestamp("202608w6"u8, FixFieldType.MonthYear, 32);
        TimestampParseResult malformed = TimestampParser.ParseTimestamp("20268"u8, FixFieldType.MonthYear, 32);

        Assert.Multiple(() =>
        {
            Assert.That(monthZero.ErrorCode, Is.EqualTo("invalid-month"));
            Assert.That(monthThirteen.Success, Is.False);
            Assert.That(impossibleDay.ErrorCode, Is.EqualTo("invalid-calendar-date"));
            Assert.That(weekZero.ErrorCode, Is.EqualTo("invalid-week"));
            Assert.That(weekSix.ErrorCode, Is.EqualTo("invalid-week"));
            Assert.That(malformed.ErrorCode, Is.EqualTo("incomplete"));
        });
    }

    [Test]
    public void DayOfMonthParsesPermittedLexicalFormsAndFormatsCanonically()
    {
        TimestampParseResult one = TimestampParser.ParseTimestamp("1"u8, FixFieldType.DayOfMonth, 32);
        TimestampParseResult padded = TimestampParser.ParseTimestamp("01"u8, FixFieldType.DayOfMonth, 32);
        TimestampParseResult last = TimestampParser.ParseTimestamp("31"u8, FixFieldType.DayOfMonth, 32);
        TimestampParseResult zero = TimestampParser.ParseTimestamp("0"u8, FixFieldType.DayOfMonth, 32);
        TimestampParseResult overflow = TimestampParser.ParseTimestamp("32"u8, FixFieldType.DayOfMonth, 32);
        TimestampParseResult sign = TimestampParser.ParseTimestamp("+1"u8, FixFieldType.DayOfMonth, 32);

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(padded.Success, Is.True);
            Assert.That(last.Success, Is.True);
            Assert.That(FormatValue(one), Is.EqualTo("1"));
            Assert.That(FormatValue(padded), Is.EqualTo("1"));
            Assert.That(FormatValue(last), Is.EqualTo("31"));
            Assert.That(zero.ErrorCode, Is.EqualTo("invalid-day-of-month"));
            Assert.That(overflow.Success, Is.False);
            Assert.That(sign.Success, Is.False);
        });
    }

    [Test]
    public void DateTypesApplyProlepticGregorianLeapRules()
    {
        FixFieldType[] types = [FixFieldType.LocalMktDate, FixFieldType.UtcDateOnly];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < types.Length; index++)
            {
                TimestampParseResult yearZero = TimestampParser.ParseTimestamp("00000229"u8, types[index], 32);
                TimestampParseResult leapCentury = TimestampParser.ParseTimestamp("20000229"u8, types[index], 32);
                TimestampParseResult ordinaryLeap = TimestampParser.ParseTimestamp("20240229"u8, types[index], 32);
                TimestampParseResult nonLeapCentury = TimestampParser.ParseTimestamp("19000229"u8, types[index], 32);
                TimestampParseResult nonLeap = TimestampParser.ParseTimestamp("20230229"u8, types[index], 32);
                TimestampParseResult shortMonth = TimestampParser.ParseTimestamp("20260431"u8, types[index], 32);

                Assert.That(yearZero.Success, Is.True, types[index].ToString());
                Assert.That(leapCentury.Success, Is.True, types[index].ToString());
                Assert.That(ordinaryLeap.Success, Is.True, types[index].ToString());
                Assert.That(nonLeapCentury.ErrorCode, Is.EqualTo("invalid-calendar-date"), types[index].ToString());
                Assert.That(nonLeap.ErrorCode, Is.EqualTo("invalid-calendar-date"), types[index].ToString());
                Assert.That(shortMonth.ErrorCode, Is.EqualTo("invalid-calendar-date"), types[index].ToString());
                Assert.That(FormatValue(leapCentury), Is.EqualTo("20000229"), types[index].ToString());
            }
        });
    }

    [Test]
    public void UtcTimeSupportsSecondsThroughPicosecondsWithoutLosingDigits()
    {
        string[] values =
        [
            "12:34:56",
            "12:34:56.123",
            "12:34:56.123456",
            "12:34:56.123456789",
            "12:34:56.123456789012"
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < values.Length; index++)
            {
                TimestampParseResult parsed = ParseText(values[index], FixFieldType.UtcTimeOnly);
                Assert.That(parsed.Success, Is.True, values[index]);
                Assert.That(parsed.FractionalDigits, Is.EqualTo(index == 0 ? 0 : index * 3), values[index]);
                Assert.That(FormatValue(parsed), Is.EqualTo(values[index]), values[index]);
            }
        });
    }

    [Test]
    public void UtcTimeRejectsRangesSeparatorsAndUnsupportedPrecisions()
    {
        string[] invalid =
        [
            "24:00:00",
            "23:60:00",
            "12-34:56",
            "12:34-56",
            "12:34:60",
            "12:34:56.",
            "12:34:56.1",
            "12:34:56.1234",
            "12:34:56.123X56"
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < invalid.Length; index++)
            {
                TimestampParseResult parsed = ParseText(invalid[index], FixFieldType.UtcTimeOnly);
                Assert.That(parsed.Success, Is.False, invalid[index]);
                Assert.That(parsed.ErrorIndex, Is.GreaterThanOrEqualTo(0), invalid[index]);
            }
        });

        TimestampParseResult leap = TimestampParser.ParseTimestamp("23:59:60"u8, FixFieldType.UtcTimeOnly, 32);
        Assert.That(leap.Success, Is.True);
    }

    [Test]
    public void UtcTimestampChecksLeapSecondDateAndPreservesHighPrecision()
    {
        TimestampParseResult december = TimestampParser.ParseTimestamp("20161231-23:59:60.123456789012"u8, FixFieldType.UtcTimestamp, 64);
        TimestampParseResult june = TimestampParser.ParseTimestamp("20150630-23:59:60"u8, FixFieldType.UtcTimestamp, 64);
        TimestampParseResult wrongDay = TimestampParser.ParseTimestamp("20161230-23:59:60"u8, FixFieldType.UtcTimestamp, 64);
        TimestampParseResult wrongTime = TimestampParser.ParseTimestamp("20161231-22:59:60"u8, FixFieldType.UtcTimestamp, 64);
        TimestampParseResult wrongDate = TimestampParser.ParseTimestamp("20230229-12:00:00"u8, FixFieldType.UtcTimestamp, 64);

        Assert.Multiple(() =>
        {
            Assert.That(december.Success, Is.True);
            Assert.That(december.Fraction, Is.EqualTo("123456789012"));
            Assert.That(FormatValue(december), Is.EqualTo("20161231-23:59:60.123456789012"));
            Assert.That(june.Success, Is.True);
            Assert.That(wrongDay.ErrorCode, Is.EqualTo("invalid-leap-second-date"));
            Assert.That(wrongTime.ErrorCode, Is.EqualTo("invalid-leap-second-time"));
            Assert.That(wrongDate.ErrorCode, Is.EqualTo("invalid-calendar-date"));
        });
    }

    [Test]
    public void TzTimeSupportsOptionalSecondsAndStandardTimezoneForms()
    {
        string[] valid =
        [
            "12:34",
            "12:34:56",
            "12:34Z",
            "12:34:56Z",
            "12:34+05",
            "12:34:56-05:30",
            "00:00+12:59"
        ];
        string[] invalid =
        [
            "12:34+00",
            "12:34+13",
            "12:34+05:60",
            "12:34:60Z",
            "12:34z",
            "12:34+5",
            "12:34+0500"
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < valid.Length; index++)
            {
                TimestampParseResult parsed = ParseText(valid[index], FixFieldType.TzTimeOnly);
                Assert.That(parsed.Success, Is.True, valid[index]);
                Assert.That(FormatValue(parsed), Is.EqualTo(valid[index]), valid[index]);
            }
            for (int index = 0; index < invalid.Length; index++)
            {
                Assert.That(ParseText(invalid[index], FixFieldType.TzTimeOnly).Success, Is.False, invalid[index]);
            }
        });
    }

    [Test]
    public void TzTimestampCombinesStrictCalendarPrecisionAndTimezoneRules()
    {
        string[] valid =
        [
            "20260830-12:34:56",
            "20260830-12:34:56Z",
            "20260830-12:34:56.123+05",
            "20260830-12:34:56.123456-05:30",
            "20260830-12:34:56.123456789012+12:59"
        ];
        string[] invalid =
        [
            "20260229-12:34:56Z",
            "20260830/12:34:56Z",
            "20260830-12:34:60Z",
            "20260830-12:34:56.12Z",
            "20260830-12:34:56+00",
            "20260830-12:34:56+05:60"
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < valid.Length; index++)
            {
                TimestampParseResult parsed = ParseText(valid[index], FixFieldType.TzTimestamp);
                Assert.That(parsed.Success, Is.True, valid[index]);
                Assert.That(FormatValue(parsed), Is.EqualTo(valid[index]), valid[index]);
            }
            for (int index = 0; index < invalid.Length; index++)
            {
                Assert.That(ParseText(invalid[index], FixFieldType.TzTimestamp).Success, Is.False, invalid[index]);
            }
        });
    }

    [Test]
    public void TimestampCodecReportsLimitsUnsupportedTypesAndDestinationRequirements()
    {
        TimestampParseResult limited = TimestampParser.ParseTimestamp("20260830"u8, FixFieldType.UtcDateOnly, 7);
        TimestampParseResult unsupported = TimestampParser.ParseTimestamp("1"u8, FixFieldType.Int, 32);
        TimestampParseResult parsed = TimestampParser.ParseTimestamp("20260830-12:34:56.123456789012Z"u8, FixFieldType.TzTimestamp, 64);
        byte[] destination = [88, 88, 88];
        ScalarFormatResult shortResult = TimestampFormatter.FormatTimestamp(parsed, destination);
        ScalarFormatResult invalidResult = TimestampFormatter.FormatTimestamp(limited, new byte[32]);

        Assert.Multiple(() =>
        {
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(unsupported.ErrorCode, Is.EqualTo("unsupported-field-type"));
            Assert.That(shortResult.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(shortResult.Required, Is.EqualTo(31));
            Assert.That(destination, Is.EqualTo(new byte[] { 88, 88, 88 }));
            Assert.That(invalidResult.ErrorCode, Is.EqualTo("invalid-temporal-value"));
        });
    }

    private static TimestampParseResult ParseText(string value, FixFieldType fieldType)
    {
        return TimestampParser.ParseTimestamp(Encoding.ASCII.GetBytes(value), fieldType, 64);
    }

    private static string FormatValue(TimestampParseResult value)
    {
        byte[] destination = new byte[64];
        ScalarFormatResult formatted = TimestampFormatter.FormatTimestamp(value, destination);
        Assert.That(formatted.Success, Is.True, formatted.ErrorCode);
        return Encoding.ASCII.GetString(destination, 0, formatted.Written);
    }
}
