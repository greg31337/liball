using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class LocalDateTimeTests
{
    [Test]
    public void Create_FromDateAndTime_StoresComponents()
    {
        LocalDate date = LocalDate.Create(2024, 2, 29);
        LocalTime time = LocalTime.Create(12, 34, 56, 789, 123, 4);
        LocalDateTime dateTime = LocalDateTime.Create(date, time);

        Assert.That(dateTime.GetDate().EqualsValue(date), Is.True);
        Assert.That(dateTime.GetTime().EqualsValue(time), Is.True);
        AssertFields(dateTime, 2024, 2, 29, 12, 34, 56, 789, 123, 4);
    }

    [Test]
    public void Create_RequiresDateAndTime()
    {
        LocalDate date = LocalDate.Create(2024, 1, 1);
        LocalTime time = LocalTime.Midnight();

        ArgumentNullException dateException = Assert.Throws<ArgumentNullException>(() => LocalDateTime.Create(null!, time))!;
        ArgumentNullException timeException = Assert.Throws<ArgumentNullException>(() => LocalDateTime.Create(date, null!))!;

        Assert.That(dateException.ParamName, Is.EqualTo("date"));
        Assert.That(timeException.ParamName, Is.EqualTo("time"));
    }

    [Test]
    public void Create_FromFields_ValidatesAndStoresComponents()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);

        AssertFields(dateTime, 2024, 2, 29, 12, 34, 56, 789, 123, 4);
    }

    [Test]
    public void Create_FromBasicFields_DefaultsFractionalTimeToZero()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56);

        AssertFields(dateTime, 2024, 2, 29, 12, 34, 56, 0, 0, 0);
    }

    [Test]
    public void Create_FromFields_RejectsInvalidDateOrTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDateTime.Create(2023, 2, 29, 12, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDateTime.Create(2024, 2, 29, 24, 0, 0));
    }

    [Test]
    public void FromDateTime_CopiesDateAndTimeAndIgnoresKind()
    {
        DateTime source = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc).AddTicks(7891234L);
        LocalDateTime dateTime = LocalDateTime.FromDateTime(source);

        AssertFields(dateTime, 2024, 2, 29, 12, 34, 56, 789, 123, 4);
    }

    [Test]
    public void CompareTo_OrdersByDateThenTime()
    {
        Assert.That(LocalDateTime.Create(2024, 1, 1, 23, 0, 0).CompareTo(LocalDateTime.Create(2024, 1, 2, 0, 0, 0)), Is.EqualTo(-1));
        Assert.That(LocalDateTime.Create(2024, 1, 1, 1, 0, 0).CompareTo(LocalDateTime.Create(2024, 1, 1, 1, 0, 0)), Is.EqualTo(0));
        Assert.That(LocalDateTime.Create(2024, 1, 1, 2, 0, 0).CompareTo(LocalDateTime.Create(2024, 1, 1, 1, 0, 0)), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_RequiresOtherDateTime()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDateTime.Create(2024, 1, 1, 0, 0, 0).CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesDateAndTimeAndHandlesNull()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 1, 1, 1, 2, 3, 4, 5, 6);

        Assert.That(dateTime.EqualsValue(LocalDateTime.Create(2024, 1, 1, 1, 2, 3, 4, 5, 6)), Is.True);
        Assert.That(dateTime.EqualsValue(LocalDateTime.Create(2024, 1, 1, 1, 2, 3, 4, 5, 7)), Is.False);
        Assert.That(dateTime.EqualsValue(null), Is.False);
    }

    [Test]
    public void ToDateTimeUnspecified_ReturnsEquivalentDateTimeWithUnspecifiedKind()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);
        DateTime result = dateTime.ToDateTimeUnspecified();

        Assert.That(result.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        Assert.That(result, Is.EqualTo(new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Unspecified).AddTicks(7891234L)));
    }

    [Test]
    public void ToDateTimeUnspecified_HandlesRangeEdges()
    {
        Assert.That(LocalDateTime.Create(1, 1, 1, 0, 0, 0).ToDateTimeUnspecified(), Is.EqualTo(DateTime.MinValue));
        Assert.That(LocalDateTime.Create(9999, 12, 31, 23, 59, 59, 999, 999, 9).ToDateTimeUnspecified(), Is.EqualTo(DateTime.MaxValue));
    }

    [Test]
    public void AddDuration_MovesWithinSameDate()
    {
        LocalDateTime result = LocalDateTime.Create(2024, 2, 29, 12, 0, 0).AddDuration(Duration.FromMinutes(90L));

        AssertFields(result, 2024, 2, 29, 13, 30, 0, 0, 0, 0);
    }

    [Test]
    public void AddDuration_RollsAcrossDateBoundaries()
    {
        LocalDateTime result = LocalDateTime.Create(2024, 12, 31, 23, 30, 0).AddDuration(Duration.FromHours(2L));

        AssertFields(result, 2025, 1, 1, 1, 30, 0, 0, 0, 0);
    }

    [Test]
    public void SubtractDuration_RollsBackwardAcrossDateBoundaries()
    {
        LocalDateTime result = LocalDateTime.Create(2024, 1, 1, 0, 30, 0).SubtractDuration(Duration.FromHours(1L));

        AssertFields(result, 2023, 12, 31, 23, 30, 0, 0, 0, 0);
    }

    [Test]
    public void DurationArithmetic_RequiresDuration()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);

        ArgumentNullException addException = Assert.Throws<ArgumentNullException>(() => dateTime.AddDuration(null!))!;
        ArgumentNullException subtractException = Assert.Throws<ArgumentNullException>(() => dateTime.SubtractDuration(null!))!;

        Assert.That(addException.ParamName, Is.EqualTo("duration"));
        Assert.That(subtractException.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void DurationArithmetic_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDateTime.Create(1, 1, 1, 0, 0, 0).SubtractDuration(Duration.FromTicks(1L)));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDateTime.Create(9999, 12, 31, 23, 59, 59, 999, 999, 9).AddDuration(Duration.FromTicks(1L)));
    }

    [Test]
    public void AddPeriod_AppliesDateFieldsThenTimeFields()
    {
        Period period = Period.Create(0L, 1L, 0L, 0L, 2L, 0L, 0L, 0L, 0L, 0L);
        LocalDateTime result = LocalDateTime.Create(2024, 1, 31, 23, 30, 0).AddPeriod(period);

        AssertFields(result, 2024, 3, 1, 1, 30, 0, 0, 0, 0);
    }

    [Test]
    public void AddPeriod_HandlesSubsecondTimeFields()
    {
        Period period = Period.Create(0L, 0L, 0L, 0L, 0L, 0L, 1L, 2L, 3L, 4L);
        LocalDateTime result = LocalDateTime.Create(2024, 1, 1, 0, 0, 0).AddPeriod(period);

        AssertFields(result, 2024, 1, 1, 0, 0, 1, 2, 3, 4);
    }

    [Test]
    public void AddPeriod_AppliesMixedSignedParsedPeriodAcrossMonthAndMidnight()
    {
        Period period = Period.Parse("P-1Y-1M-1W2DT-2H50M10.0000001S");
        LocalDateTime result = LocalDateTime.Create(2024, 3, 31, 0, 15, 0, 250, 0, 0).AddPeriod(period);

        AssertFields(result, 2023, 2, 22, 23, 5, 10, 250, 0, 1);
    }

    [Test]
    public void SubtractPeriod_AppliesNegatedFields()
    {
        Period period = Period.Create(0L, 0L, 0L, 1L, 2L, 0L, 0L, 0L, 0L, 0L);
        LocalDateTime result = LocalDateTime.Create(2024, 1, 2, 1, 30, 0).SubtractPeriod(period);

        AssertFields(result, 2023, 12, 31, 23, 30, 0, 0, 0, 0);
    }

    [Test]
    public void PeriodArithmetic_RequiresPeriod()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);

        ArgumentNullException addException = Assert.Throws<ArgumentNullException>(() => dateTime.AddPeriod(null!))!;
        ArgumentNullException subtractException = Assert.Throws<ArgumentNullException>(() => dateTime.SubtractPeriod(null!))!;

        Assert.That(addException.ParamName, Is.EqualTo("period"));
        Assert.That(subtractException.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void AddPeriod_ThrowsWhenTimeFieldConversionOverflows()
    {
        Period period = Period.FromHours(long.MaxValue);

        Assert.Throws<OverflowException>(() => LocalDateTime.Create(2024, 1, 1, 0, 0, 0).AddPeriod(period));
    }

    [TestCase(1, 1, 1, 0, 0, 0, 0, 0, 0, "0001-01-01T00:00:00")]
    [TestCase(2024, 2, 29, 12, 34, 56, 789, 123, 4, "2024-02-29T12:34:56.7891234")]
    [TestCase(9999, 12, 31, 23, 59, 59, 999, 999, 9, "9999-12-31T23:59:59.9999999")]
    public void ToIsoString_FormatsIsoLocalDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder,
        string expected)
    {
        LocalDateTime dateTime = LocalDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);

        Assert.That(dateTime.ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase(1, 1, 1, 0, 0, 0, 0, 0, 0, "00010101T000000")]
    [TestCase(2024, 2, 29, 12, 34, 56, 700, 0, 0, "20240229T123456.7")]
    [TestCase(2024, 2, 29, 12, 34, 56, 789, 123, 4, "20240229T123456.7891234")]
    [TestCase(9999, 12, 31, 23, 59, 59, 999, 999, 9, "99991231T235959.9999999")]
    public void ToIsoBasicString_FormatsCompactIsoLocalDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder,
        string expected)
    {
        ILocalDateTime dateTime = LocalDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);

        Assert.That(dateTime.ToIsoBasicString(), Is.EqualTo(expected));
    }

    [TestCase("0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0, 0, 0, 0)]
    [TestCase("2024-02-29T12:34:56.7891234", 2024, 2, 29, 12, 34, 56, 789, 123, 4)]
    [TestCase("9999-12-31T23:59:59.9999999", 9999, 12, 31, 23, 59, 59, 999, 999, 9)]
    public void Parse_ReadsIsoLocalDateTime(
        string text,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder)
    {
        AssertFields(LocalDateTime.Parse(text), year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);
    }

    [TestCase("00010101T000000", 1, 1, 1, 0, 0, 0, 0, 0, 0)]
    [TestCase("20240229T123456.1", 2024, 2, 29, 12, 34, 56, 100, 0, 0)]
    [TestCase("20240229T123456.12", 2024, 2, 29, 12, 34, 56, 120, 0, 0)]
    [TestCase("20240229T123456.123", 2024, 2, 29, 12, 34, 56, 123, 0, 0)]
    [TestCase("20240229T123456.1234", 2024, 2, 29, 12, 34, 56, 123, 400, 0)]
    [TestCase("20240229T123456.12345", 2024, 2, 29, 12, 34, 56, 123, 450, 0)]
    [TestCase("20240229T123456.123456", 2024, 2, 29, 12, 34, 56, 123, 456, 0)]
    [TestCase("99991231T235959.9999999", 9999, 12, 31, 23, 59, 59, 999, 999, 9)]
    public void ParseIsoBasic_ReadsCompactDateTimeAndFractionalPrecision(
        string text,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder)
    {
        AssertFields(LocalDateTime.ParseIsoBasic(text), year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);
        AssertFields(TimeApi.Factory.ParseIsoBasicLocalDateTime(text), year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDateTime.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [Test]
    public void ParseIsoBasic_RequiresNonNullText()
    {
        ArgumentNullException internalException = Assert.Throws<ArgumentNullException>(() => LocalDateTime.ParseIsoBasic(null!))!;
        ArgumentNullException factoryException = Assert.Throws<ArgumentNullException>(() => TimeApi.Factory.ParseIsoBasicLocalDateTime(null!))!;

        Assert.That(internalException.ParamName, Is.EqualTo("text"));
        Assert.That(factoryException.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("2024-02-29")]
    [TestCase("2024-02-29 12:34:56")]
    [TestCase("2024-02-29t12:34:56")]
    [TestCase("2023-02-29T12:34:56")]
    [TestCase("2024-02-29T24:00:00")]
    [TestCase("2024-02-29T12:34:56.12345678")]
    public void Parse_RejectsMalformedOrInvalidIsoLocalDateTime(string text)
    {
        Assert.Throws<FormatException>(() => LocalDateTime.Parse(text));
    }

    [TestCase("")]
    [TestCase("20240229")]
    [TestCase("20240229T12345")]
    [TestCase("20240229T123456.")]
    [TestCase("20240229T123456.12345678")]
    [TestCase("20240229t123456")]
    [TestCase("20240229 123456")]
    [TestCase("2024-02-29T12:34:56")]
    [TestCase("20240229T12:34:56")]
    [TestCase("20240229T123456,1")]
    [TestCase("20240229T123456Z")]
    [TestCase("20230229T123456")]
    [TestCase("20240229T240000")]
    [TestCase("20240229T126000")]
    [TestCase("20240229T123460")]
    [TestCase("2024aa29T123456")]
    [TestCase("20240229T12aa56")]
    public void ParseIsoBasic_RejectsMalformedOrInvalidLocalDateTime(string text)
    {
        Assert.Throws<FormatException>(() => LocalDateTime.ParseIsoBasic(text));
    }

    [Test]
    public void ExtendedAndBasicDateTimeEntryPoints_RemainDistinctAndRoundTrip()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);

        Assert.That(dateTime.ToIsoString(), Is.EqualTo("2024-02-29T12:34:56.7891234"));
        Assert.That(dateTime.ToIsoBasicString(), Is.EqualTo("20240229T123456.7891234"));
        Assert.That(LocalDateTime.Parse(dateTime.ToIsoString()).EqualsValue(dateTime), Is.True);
        Assert.That(LocalDateTime.ParseIsoBasic(dateTime.ToIsoBasicString()).EqualsValue(dateTime), Is.True);
        Assert.Throws<FormatException>(() => LocalDateTime.Parse(dateTime.ToIsoBasicString()));
        Assert.Throws<FormatException>(() => LocalDateTime.ParseIsoBasic(dateTime.ToIsoString()));
    }

    [TestCase(1, 1, 1, 0, 0, 0, 0, 0, 0)]
    [TestCase(1970, 1, 1, 0, 0, 0, 0, 0, 1)]
    [TestCase(2024, 2, 29, 12, 34, 56, 789, 123, 4)]
    [TestCase(9999, 12, 31, 23, 59, 59, 999, 999, 9)]
    public void Parse_RoundTripsFormattedLocalDateTimes(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder)
    {
        LocalDateTime original = LocalDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);
        LocalDateTime parsed = LocalDateTime.Parse(original.ToIsoString());

        Assert.That(parsed.EqualsValue(original), Is.True);
    }

    [Test]
    public void LocalDateTime_MatchesDateTimeAcrossRepresentativeValues()
    {
        DateTime[] values =
        [
            DateTime.MinValue,
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Local).AddTicks(7891234L),
            DateTime.MaxValue
        ];

        foreach (DateTime value in values)
        {
            LocalDateTime dateTime = LocalDateTime.FromDateTime(value);

            AssertMatchesDateTime(dateTime, value);
            Assert.That(dateTime.ToDateTimeUnspecified().Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(LocalDateTime.Parse(dateTime.ToIsoString()).EqualsValue(dateTime), Is.True);
        }
    }

    [Test]
    public void DurationArithmetic_MatchesDateTimeAddForRepresentativeDurations()
    {
        DateTime source = new DateTime(2024, 2, 28, 23, 30, 0, DateTimeKind.Unspecified).AddTicks(1234L);
        LocalDateTime dateTime = LocalDateTime.FromDateTime(source);
        Duration[] durations =
        [
            Duration.FromTicks(1L),
            Duration.FromMinutes(90L),
            Duration.FromDays(-2L),
            Duration.FromDays(3L).Add(Duration.FromSeconds(45L))
        ];

        foreach (Duration duration in durations)
        {
            DateTime expected = source.Add(duration.ToTimeSpan());
            LocalDateTime actual = dateTime.AddDuration(duration);

            AssertMatchesDateTime(actual, expected);
            AssertMatchesDateTime(actual.SubtractDuration(duration), source);
        }
    }

    [Test]
    public void PeriodArithmetic_MatchesDateTimeOrderingForRepresentativePeriod()
    {
        DateTime source = new DateTime(2024, 1, 31, 23, 30, 0, DateTimeKind.Unspecified);
        Period period = Period.Create(1L, 1L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L);
        DateTime expected = source
            .AddYears(1)
            .AddMonths(1)
            .AddDays(7)
            .AddDays(2)
            .AddHours(3)
            .AddMinutes(4)
            .AddSeconds(5)
            .AddTicks(60070L)
            .AddTicks(8L);

        LocalDateTime actual = LocalDateTime.FromDateTime(source).AddPeriod(period);

        AssertMatchesDateTime(actual, expected);
    }

    [Test]
    public void DateAndTimeExtraction_ReturnsEquivalentComponentValues()
    {
        LocalDateTime dateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);

        Assert.That(dateTime.GetDate().EqualsValue(LocalDate.Create(2024, 2, 29)), Is.True);
        Assert.That(dateTime.GetTime().EqualsValue(LocalTime.Create(12, 34, 56, 789, 123, 4)), Is.True);
        Assert.That(dateTime.GetDate().ToIsoString(), Is.EqualTo("2024-02-29"));
        Assert.That(dateTime.GetTime().ToIsoString(), Is.EqualTo("12:34:56.7891234"));
    }

    [Test]
    public void ParseFormatAndDateTimeInterop_RoundTripRangeEdges()
    {
        LocalDateTime min = LocalDateTime.FromDateTime(DateTime.MinValue);
        LocalDateTime max = LocalDateTime.FromDateTime(DateTime.MaxValue);

        Assert.That(LocalDateTime.Parse(min.ToIsoString()).ToDateTimeUnspecified(), Is.EqualTo(DateTime.MinValue));
        Assert.That(LocalDateTime.Parse(max.ToIsoString()).ToDateTimeUnspecified(), Is.EqualTo(DateTime.MaxValue));
    }

    private static void AssertFields(
        LocalDateTime dateTime,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder)
    {
        Assert.That(dateTime.GetYear(), Is.EqualTo(year));
        Assert.That(dateTime.GetMonth(), Is.EqualTo(month));
        Assert.That(dateTime.GetDay(), Is.EqualTo(day));
        Assert.That(dateTime.GetHour(), Is.EqualTo(hour));
        Assert.That(dateTime.GetMinute(), Is.EqualTo(minute));
        Assert.That(dateTime.GetSecond(), Is.EqualTo(second));
        Assert.That(dateTime.GetMillisecond(), Is.EqualTo(millisecond));
        Assert.That(dateTime.GetMicrosecond(), Is.EqualTo(microsecond));
        Assert.That(dateTime.GetTickRemainder(), Is.EqualTo(tickRemainder));
    }

    private static void AssertMatchesDateTime(LocalDateTime dateTime, DateTime expected)
    {
        Assert.That(dateTime.GetYear(), Is.EqualTo(expected.Year));
        Assert.That(dateTime.GetMonth(), Is.EqualTo(expected.Month));
        Assert.That(dateTime.GetDay(), Is.EqualTo(expected.Day));
        Assert.That(dateTime.GetHour(), Is.EqualTo(expected.Hour));
        Assert.That(dateTime.GetMinute(), Is.EqualTo(expected.Minute));
        Assert.That(dateTime.GetSecond(), Is.EqualTo(expected.Second));
        Assert.That(dateTime.ToDateTimeUnspecified(), Is.EqualTo(DateTime.SpecifyKind(expected, DateTimeKind.Unspecified)));
    }
}
