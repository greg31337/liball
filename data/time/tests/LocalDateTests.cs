using System.Globalization;
using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class LocalDateTests
{
    [Test]
    public void Create_StoresValidatedDateFields()
    {
        LocalDate date = LocalDate.Create(2024, 2, 29);

        Assert.That(date.GetYear(), Is.EqualTo(2024));
        Assert.That(date.GetMonth(), Is.EqualTo(2));
        Assert.That(date.GetDay(), Is.EqualTo(29));
    }

    [TestCase(0, 1, 1, "year")]
    [TestCase(10000, 1, 1, "year")]
    [TestCase(2024, 0, 1, "month")]
    [TestCase(2024, 13, 1, "month")]
    [TestCase(2023, 2, 29, "day")]
    [TestCase(2024, 4, 31, "day")]
    public void Create_RejectsInvalidDateFields(int year, int month, int day, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(year, month, day))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [Test]
    public void FromDateOnly_CopiesDateOnlyFields()
    {
        DateOnly dateOnly = new DateOnly(1999, 12, 31);
        LocalDate date = LocalDate.FromDateOnly(dateOnly);

        Assert.That(date.GetYear(), Is.EqualTo(dateOnly.Year));
        Assert.That(date.GetMonth(), Is.EqualTo(dateOnly.Month));
        Assert.That(date.GetDay(), Is.EqualTo(dateOnly.Day));
    }

    [Test]
    public void FromDateTime_CopiesDateFieldsAndIgnoresTimeAndKind()
    {
        DateTime dateTime = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        LocalDate date = LocalDate.FromDateTime(dateTime);

        Assert.That(date.GetYear(), Is.EqualTo(2024));
        Assert.That(date.GetMonth(), Is.EqualTo(6));
        Assert.That(date.GetDay(), Is.EqualTo(7));
    }

    [TestCase(0, 1, 1, 1)]
    [TestCase(719162, 1970, 1, 1)]
    [TestCase(738944, 2024, 2, 29)]
    [TestCase(3652058, 9999, 12, 31)]
    public void FromDayNumber_CreatesMatchingDate(int dayNumber, int year, int month, int day)
    {
        LocalDate date = LocalDate.FromDayNumber(dayNumber);

        Assert.That(date.GetYear(), Is.EqualTo(year));
        Assert.That(date.GetMonth(), Is.EqualTo(month));
        Assert.That(date.GetDay(), Is.EqualTo(day));
        Assert.That(date.GetDayNumber(), Is.EqualTo(dayNumber));
    }

    [TestCase(-1)]
    [TestCase(3652059)]
    public void FromDayNumber_RejectsUnsupportedDayNumbers(int dayNumber)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.FromDayNumber(dayNumber))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayNumber"));
    }

    [Test]
    public void Accessors_MatchCalendarMathAndDateOnly()
    {
        LocalDate date = LocalDate.Create(2024, 2, 29);
        DateOnly dateOnly = new DateOnly(2024, 2, 29);

        Assert.That(date.GetDayOfWeek(), Is.EqualTo(CalendarMath.GetDayOfWeek(2024, 2, 29)));
        Assert.That(date.GetDayOfYear(), Is.EqualTo(dateOnly.DayOfYear));
        Assert.That(date.GetDayNumber(), Is.EqualTo(dateOnly.DayNumber));
        Assert.That(date.GetDaysInMonth(), Is.EqualTo(29));
        Assert.That(date.GetDaysInYear(), Is.EqualTo(366));
        Assert.That(date.IsLeapYear(), Is.True);
    }

    [Test]
    public void CompareTo_OrdersByYearMonthThenDay()
    {
        Assert.That(LocalDate.Create(2023, 12, 31).CompareTo(LocalDate.Create(2024, 1, 1)), Is.EqualTo(-1));
        Assert.That(LocalDate.Create(2024, 1, 1).CompareTo(LocalDate.Create(2024, 1, 1)), Is.EqualTo(0));
        Assert.That(LocalDate.Create(2024, 1, 2).CompareTo(LocalDate.Create(2024, 1, 1)), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_RequiresOtherDate()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDate.Create(2024, 1, 1).CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesDateFieldsAndHandlesNull()
    {
        LocalDate date = LocalDate.Create(2024, 1, 2);

        Assert.That(date.EqualsValue(LocalDate.Create(2024, 1, 2)), Is.True);
        Assert.That(date.EqualsValue(LocalDate.Create(2024, 1, 3)), Is.False);
        Assert.That(date.EqualsValue(null), Is.False);
    }

    [Test]
    public void ToDateOnly_ReturnsEquivalentDateOnly()
    {
        LocalDate date = LocalDate.Create(2024, 2, 29);

        Assert.That(date.ToDateOnly(), Is.EqualTo(new DateOnly(2024, 2, 29)));
    }

    [TestCase(1, 1, 1, "0001-01-01")]
    [TestCase(2024, 2, 29, "2024-02-29")]
    [TestCase(9999, 12, 31, "9999-12-31")]
    public void ToIsoString_FormatsAsIsoDate(int year, int month, int day, string expected)
    {
        Assert.That(LocalDate.Create(year, month, day).ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase(1, 1, 1, "00010101")]
    [TestCase(2024, 2, 29, "20240229")]
    [TestCase(9999, 12, 31, "99991231")]
    public void ToIsoBasicString_FormatsCompactIsoDate(int year, int month, int day, string expected)
    {
        ILocalDate date = LocalDate.Create(year, month, day);

        Assert.That(date.ToIsoBasicString(), Is.EqualTo(expected));
    }

    [TestCase("0001-01-01", 1, 1, 1)]
    [TestCase("2024-02-29", 2024, 2, 29)]
    [TestCase("9999-12-31", 9999, 12, 31)]
    public void Parse_ReadsIsoDateText(string text, int year, int month, int day)
    {
        AssertDate(LocalDate.Parse(text), year, month, day);
    }

    [TestCase("00010101", 1, 1, 1)]
    [TestCase("20240229", 2024, 2, 29)]
    [TestCase("99991231", 9999, 12, 31)]
    public void ParseIsoBasic_ReadsCompactIsoDateText(string text, int year, int month, int day)
    {
        AssertDate(LocalDate.ParseIsoBasic(text), year, month, day);
        AssertDate(TimeApi.Factory.ParseIsoBasicLocalDate(text), year, month, day);
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDate.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [Test]
    public void ParseIsoBasic_RequiresNonNullText()
    {
        ArgumentNullException internalException = Assert.Throws<ArgumentNullException>(() => LocalDate.ParseIsoBasic(null!))!;
        ArgumentNullException factoryException = Assert.Throws<ArgumentNullException>(() => TimeApi.Factory.ParseIsoBasicLocalDate(null!))!;

        Assert.That(internalException.ParamName, Is.EqualTo("text"));
        Assert.That(factoryException.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("2024-2-29")]
    [TestCase("20240229")]
    [TestCase("2024/02/29")]
    [TestCase("abcd-02-29")]
    [TestCase("2024-aa-29")]
    [TestCase("2024-02-bb")]
    [TestCase("0000-01-01")]
    [TestCase("10000-01-01")]
    [TestCase("2024-00-01")]
    [TestCase("2024-13-01")]
    [TestCase("2023-02-29")]
    [TestCase("2024-04-31")]
    public void Parse_RejectsMalformedOrInvalidIsoDateText(string text)
    {
        Assert.Throws<FormatException>(() => LocalDate.Parse(text));
    }

    [TestCase("")]
    [TestCase("2024022")]
    [TestCase("202402290")]
    [TestCase("2024-02-29")]
    [TestCase("2024/229")]
    [TestCase("abcd0229")]
    [TestCase("2024aa29")]
    [TestCase("202402bb")]
    [TestCase("00000101")]
    [TestCase("20240001")]
    [TestCase("20241301")]
    [TestCase("20230229")]
    [TestCase("20240431")]
    [TestCase(" 20240229")]
    [TestCase("20240229 ")]
    public void ParseIsoBasic_RejectsMalformedOrInvalidDateText(string text)
    {
        Assert.Throws<FormatException>(() => LocalDate.ParseIsoBasic(text));
    }

    [Test]
    public void ExtendedAndBasicDateEntryPoints_RemainDistinctAndRoundTrip()
    {
        LocalDate date = LocalDate.Create(2024, 2, 29);

        Assert.That(date.ToIsoString(), Is.EqualTo("2024-02-29"));
        Assert.That(date.ToIsoBasicString(), Is.EqualTo("20240229"));
        Assert.That(LocalDate.Parse(date.ToIsoString()).EqualsValue(date), Is.True);
        Assert.That(LocalDate.ParseIsoBasic(date.ToIsoBasicString()).EqualsValue(date), Is.True);
        Assert.Throws<FormatException>(() => LocalDate.Parse(date.ToIsoBasicString()));
        Assert.Throws<FormatException>(() => LocalDate.ParseIsoBasic(date.ToIsoString()));
    }

    [TestCase(1, 1, 1)]
    [TestCase(1970, 1, 1)]
    [TestCase(2024, 2, 29)]
    [TestCase(9999, 12, 31)]
    public void Parse_RoundTripsFormattedDates(int year, int month, int day)
    {
        LocalDate original = LocalDate.Create(year, month, day);
        LocalDate parsed = LocalDate.Parse(original.ToIsoString());

        Assert.That(parsed.EqualsValue(original), Is.True);
    }

    [Test]
    public void AddDays_MovesAcrossMonthAndYearBoundaries()
    {
        AssertDate(LocalDate.Create(2024, 2, 28).AddDays(1L), 2024, 2, 29);
        AssertDate(LocalDate.Create(2024, 12, 31).AddDays(1L), 2025, 1, 1);
        AssertDate(LocalDate.Create(2024, 1, 1).AddDays(-1L), 2023, 12, 31);
    }

    [Test]
    public void AddDays_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(1, 1, 1).AddDays(-1L));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(9999, 12, 31).AddDays(1L));
    }

    [Test]
    public void SubtractDays_DelegatesToNegativeDayArithmetic()
    {
        AssertDate(LocalDate.Create(2024, 3, 1).SubtractDays(1L), 2024, 2, 29);
    }

    [Test]
    public void SubtractDays_ThrowsWhenNegatingMinimumValue()
    {
        Assert.Throws<OverflowException>(() => LocalDate.Create(2024, 1, 1).SubtractDays(long.MinValue));
    }

    [Test]
    public void AddWeeks_UsesSevenDayWeeks()
    {
        AssertDate(LocalDate.Create(2024, 1, 1).AddWeeks(2L), 2024, 1, 15);
        AssertDate(LocalDate.Create(2024, 1, 15).SubtractWeeks(2L), 2024, 1, 1);
    }

    [Test]
    public void AddWeeks_ThrowsWhenWeekMultiplicationOverflows()
    {
        Assert.Throws<OverflowException>(() => LocalDate.Create(2024, 1, 1).AddWeeks(long.MaxValue));
    }

    [Test]
    public void AddMonths_ConstrainsToLastValidDayOfDestinationMonth()
    {
        AssertDate(LocalDate.Create(2024, 1, 31).AddMonths(1L), 2024, 2, 29);
        AssertDate(LocalDate.Create(2023, 1, 31).AddMonths(1L), 2023, 2, 28);
        AssertDate(LocalDate.Create(2024, 3, 31).AddMonths(-1L), 2024, 2, 29);
    }

    [Test]
    public void AddMonths_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(1, 1, 1).AddMonths(-1L));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(9999, 12, 31).AddMonths(1L));
    }

    [Test]
    public void SubtractMonths_DelegatesToNegativeMonthArithmetic()
    {
        AssertDate(LocalDate.Create(2024, 3, 31).SubtractMonths(1L), 2024, 2, 29);
    }

    [Test]
    public void SubtractMonths_ThrowsWhenNegatingMinimumValue()
    {
        Assert.Throws<OverflowException>(() => LocalDate.Create(2024, 1, 1).SubtractMonths(long.MinValue));
    }

    [Test]
    public void AddYears_ConstrainsLeapDayToLastValidDay()
    {
        AssertDate(LocalDate.Create(2024, 2, 29).AddYears(1L), 2025, 2, 28);
        AssertDate(LocalDate.Create(2024, 2, 29).AddYears(4L), 2028, 2, 29);
    }

    [Test]
    public void AddYears_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(1, 1, 1).AddYears(-1L));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDate.Create(9999, 12, 31).AddYears(1L));
    }

    [Test]
    public void SubtractYears_DelegatesToNegativeYearArithmetic()
    {
        AssertDate(LocalDate.Create(2024, 2, 29).SubtractYears(1L), 2023, 2, 28);
    }

    [Test]
    public void SubtractYears_ThrowsWhenNegatingMinimumValue()
    {
        Assert.Throws<OverflowException>(() => LocalDate.Create(2024, 1, 1).SubtractYears(long.MinValue));
    }

    [Test]
    public void AddPeriod_AppliesYearsMonthsWeeksThenDays()
    {
        Period period = Period.Create(1L, 1L, 1L, 2L, 0L, 0L, 0L, 0L, 0L, 0L);

        AssertDate(LocalDate.Create(2024, 1, 31).AddPeriod(period), 2025, 3, 9);
    }

    [Test]
    public void AddPeriod_RequiresPeriod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDate.Create(2024, 1, 1).AddPeriod(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void AddPeriod_RejectsTimeFields()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => LocalDate.Create(2024, 1, 1).AddPeriod(Period.FromHours(1L)))!;

        Assert.That(exception.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void SubtractPeriod_AppliesNegatedDateFields()
    {
        Period period = Period.Create(1L, 1L, 1L, 2L, 0L, 0L, 0L, 0L, 0L, 0L);

        AssertDate(LocalDate.Create(2025, 3, 9).SubtractPeriod(period), 2024, 1, 31);
    }

    [Test]
    public void SubtractPeriod_RequiresPeriod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalDate.Create(2024, 1, 1).SubtractPeriod(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void StartAndEndHelpers_ReturnExpectedMonthAndYearBoundaries()
    {
        LocalDate date = LocalDate.Create(2024, 2, 15);

        AssertDate(date.StartOfMonth(), 2024, 2, 1);
        AssertDate(date.EndOfMonth(), 2024, 2, 29);
        AssertDate(date.StartOfYear(), 2024, 1, 1);
        AssertDate(date.EndOfYear(), 2024, 12, 31);
    }

    [Test]
    public void StartAndEndHelpers_HandleNonLeapFebruary()
    {
        LocalDate date = LocalDate.Create(2023, 2, 15);

        AssertDate(date.EndOfMonth(), 2023, 2, 28);
        AssertDate(date.EndOfYear(), 2023, 12, 31);
    }

    [Test]
    public void WeekdayNavigation_FindsNextAndPreviousWeekdays()
    {
        LocalDate monday = LocalDate.Create(2024, 1, 1);

        AssertDate(monday.Next(TimeConstants.IsoMonday), 2024, 1, 8);
        AssertDate(monday.Next(TimeConstants.IsoSunday), 2024, 1, 7);
        AssertDate(monday.Previous(TimeConstants.IsoMonday), 2023, 12, 25);
        AssertDate(monday.Previous(TimeConstants.IsoSunday), 2023, 12, 31);
    }

    [Test]
    public void WeekdayNavigation_SameOrMethodsMayReturnCurrentDate()
    {
        LocalDate monday = LocalDate.Create(2024, 1, 1);

        AssertDate(monday.SameOrNext(TimeConstants.IsoMonday), 2024, 1, 1);
        AssertDate(monday.SameOrPrevious(TimeConstants.IsoMonday), 2024, 1, 1);
        AssertDate(monday.SameOrNext(TimeConstants.IsoSunday), 2024, 1, 7);
        AssertDate(monday.SameOrPrevious(TimeConstants.IsoSunday), 2023, 12, 31);
    }

    [TestCase(0)]
    [TestCase(8)]
    public void WeekdayNavigation_RejectsInvalidIsoWeekdays(int dayOfWeek)
    {
        LocalDate date = LocalDate.Create(2024, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => date.Next(dayOfWeek));
        Assert.Throws<ArgumentOutOfRangeException>(() => date.Previous(dayOfWeek));
        Assert.Throws<ArgumentOutOfRangeException>(() => date.SameOrNext(dayOfWeek));
        Assert.Throws<ArgumentOutOfRangeException>(() => date.SameOrPrevious(dayOfWeek));
    }

    [Test]
    public void LocalDate_MatchesDateOnlyAcrossRepresentativeDates()
    {
        DateOnly[] dates =
        [
            DateOnly.MinValue,
            new DateOnly(1, 12, 31),
            new DateOnly(1900, 2, 28),
            new DateOnly(2000, 2, 29),
            new DateOnly(2023, 12, 31),
            new DateOnly(2024, 2, 29),
            new DateOnly(2024, 12, 31),
            DateOnly.MaxValue
        ];

        foreach (DateOnly dateOnly in dates)
        {
            LocalDate date = LocalDate.FromDateOnly(dateOnly);

            AssertMatchesDateOnly(date, dateOnly);
            Assert.That(date.ToIsoString(), Is.EqualTo(dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            Assert.That(LocalDate.Parse(date.ToIsoString()).EqualsValue(date), Is.True);
        }
    }

    [Test]
    public void MonthEndArithmetic_MatchesDateOnlyAddMonths()
    {
        DateOnly[] dates =
        [
            new DateOnly(2023, 1, 31),
            new DateOnly(2024, 1, 31),
            new DateOnly(2024, 3, 31),
            new DateOnly(2024, 12, 31)
        ];

        int[] monthDeltas =
        [
            -1,
            1,
            2,
            13
        ];

        foreach (DateOnly dateOnly in dates)
        {
            foreach (int months in monthDeltas)
            {
                DateOnly expected = dateOnly.AddMonths(months);
                LocalDate actual = LocalDate.FromDateOnly(dateOnly).AddMonths(months);

                AssertMatchesDateOnly(actual, expected);
            }
        }
    }

    [Test]
    public void WeekdayNavigation_MatchesDateOnlyForEveryIsoWeekday()
    {
        DateOnly dateOnly = new DateOnly(2024, 5, 15);
        LocalDate date = LocalDate.FromDateOnly(dateOnly);

        for (int weekday = TimeConstants.IsoMonday; weekday <= TimeConstants.IsoSunday; weekday++)
        {
            AssertMatchesDateOnly(date.Next(weekday), MoveToWeekday(dateOnly, weekday, false, true));
            AssertMatchesDateOnly(date.Previous(weekday), MoveToWeekday(dateOnly, weekday, false, false));
            AssertMatchesDateOnly(date.SameOrNext(weekday), MoveToWeekday(dateOnly, weekday, true, true));
            AssertMatchesDateOnly(date.SameOrPrevious(weekday), MoveToWeekday(dateOnly, weekday, true, false));
        }
    }

    [Test]
    public void DateOnlyInterop_RoundTripsRangeEdges()
    {
        LocalDate min = LocalDate.FromDateOnly(DateOnly.MinValue);
        LocalDate max = LocalDate.FromDateOnly(DateOnly.MaxValue);

        Assert.That(min.ToDateOnly(), Is.EqualTo(DateOnly.MinValue));
        Assert.That(max.ToDateOnly(), Is.EqualTo(DateOnly.MaxValue));
        Assert.That(LocalDate.Parse(min.ToIsoString()).ToDateOnly(), Is.EqualTo(DateOnly.MinValue));
        Assert.That(LocalDate.Parse(max.ToIsoString()).ToDateOnly(), Is.EqualTo(DateOnly.MaxValue));
    }

    private static void AssertDate(LocalDate date, int year, int month, int day)
    {
        Assert.That(date.GetYear(), Is.EqualTo(year));
        Assert.That(date.GetMonth(), Is.EqualTo(month));
        Assert.That(date.GetDay(), Is.EqualTo(day));
    }

    private static void AssertMatchesDateOnly(LocalDate date, DateOnly dateOnly)
    {
        Assert.That(date.GetYear(), Is.EqualTo(dateOnly.Year));
        Assert.That(date.GetMonth(), Is.EqualTo(dateOnly.Month));
        Assert.That(date.GetDay(), Is.EqualTo(dateOnly.Day));
        Assert.That(date.GetDayNumber(), Is.EqualTo(dateOnly.DayNumber));
        Assert.That(date.GetDayOfYear(), Is.EqualTo(dateOnly.DayOfYear));
        Assert.That(date.GetDayOfWeek(), Is.EqualTo(GetIsoDayOfWeek(dateOnly)));
        Assert.That(date.ToDateOnly(), Is.EqualTo(dateOnly));
    }

    private static DateOnly MoveToWeekday(DateOnly date, int weekday, bool allowSame, bool forward)
    {
        DateOnly current = date;
        if (!allowSame)
        {
            if (forward)
            {
                current = current.AddDays(1);
            }
            else
            {
                current = current.AddDays(-1);
            }
        }

        while (GetIsoDayOfWeek(current) != weekday)
        {
            if (forward)
            {
                current = current.AddDays(1);
            }
            else
            {
                current = current.AddDays(-1);
            }
        }

        return current;
    }

    private static int GetIsoDayOfWeek(DateOnly date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0)
        {
            return TimeConstants.IsoSunday;
        }

        return dayOfWeek;
    }
}
