using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class CalendarMathTests
{
    [TestCase(1, false)]
    [TestCase(4, true)]
    [TestCase(1900, false)]
    [TestCase(2000, true)]
    [TestCase(2024, true)]
    [TestCase(9999, false)]
    public void IsLeapYear_UsesGregorianLeapYearRules(int year, bool expected)
    {
        Assert.That(CalendarMath.IsLeapYear(year), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(10000)]
    public void IsLeapYear_RejectsYearsOutsideSupportedRange(int year)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.IsLeapYear(year))!;

        Assert.That(exception.ParamName, Is.EqualTo("year"));
    }

    [TestCase(2024, 1, 31)]
    [TestCase(2024, 2, 29)]
    [TestCase(2023, 2, 28)]
    [TestCase(2024, 4, 30)]
    [TestCase(2024, 12, 31)]
    public void GetDaysInMonth_ReturnsGregorianMonthLength(int year, int month, int expected)
    {
        Assert.That(CalendarMath.GetDaysInMonth(year, month), Is.EqualTo(expected));
    }

    [TestCase(2024, 0, "month")]
    [TestCase(2024, 13, "month")]
    [TestCase(10000, 1, "year")]
    public void GetDaysInMonth_RejectsInvalidInputs(int year, int month, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.GetDaysInMonth(year, month))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase(2023, 365)]
    [TestCase(2024, 366)]
    public void GetDaysInYear_ReturnsGregorianYearLength(int year, int expected)
    {
        Assert.That(CalendarMath.GetDaysInYear(year), Is.EqualTo(expected));
    }

    [TestCase(2024, 1, 1, 1)]
    [TestCase(2024, 2, 29, 60)]
    [TestCase(2023, 12, 31, 365)]
    [TestCase(2024, 12, 31, 366)]
    public void GetDayOfYear_ReturnsOneBasedOrdinalDay(int year, int month, int day, int expected)
    {
        Assert.That(CalendarMath.GetDayOfYear(year, month, day), Is.EqualTo(expected));
    }

    [TestCase(1, 1, 0)]
    [TestCase(2023, 2, 29)]
    public void GetDayOfYear_RejectsInvalidDate(int year, int month, int day)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.GetDayOfYear(year, month, day))!;

        Assert.That(exception.ParamName, Is.EqualTo("day"));
    }

    [TestCase(1, 0)]
    [TestCase(2, 365)]
    [TestCase(2000, 730119)]
    [TestCase(9999, 3651694)]
    public void GetStartDayNumberOfYear_ReturnsZeroBasedStartDay(int year, int expected)
    {
        Assert.That(CalendarMath.GetStartDayNumberOfYear(year), Is.EqualTo(expected));
    }

    [TestCase(1, 1, 1)]
    [TestCase(1, 1, 2)]
    [TestCase(1970, 1, 1)]
    [TestCase(2024, 2, 29)]
    [TestCase(9999, 12, 31)]
    public void GetDayNumber_MatchesDateOnly(int year, int month, int day)
    {
        DateOnly date = new DateOnly(year, month, day);

        Assert.That(CalendarMath.GetDayNumber(year, month, day), Is.EqualTo(date.DayNumber));
    }

    [TestCase(1, 1, 1, 1)]
    [TestCase(2024, 2, 29, 4)]
    [TestCase(2024, 3, 3, 7)]
    [TestCase(9999, 12, 31, 5)]
    public void GetDayOfWeek_ReturnsIsoWeekdayNumber(int year, int month, int day, int expected)
    {
        Assert.That(CalendarMath.GetDayOfWeek(year, month, day), Is.EqualTo(expected));
    }

    [TestCase(0, 1, 1)]
    [TestCase(2024, 2, 30)]
    public void GetDayNumber_RejectsInvalidDate(int year, int month, int day)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.GetDayNumber(year, month, day))!;

        Assert.That(exception.ParamName, Is.EqualTo("day").Or.EqualTo("year"));
    }

    [TestCase(0, 1, 1, 1)]
    [TestCase(719162, 1970, 1, 1)]
    [TestCase(738944, 2024, 2, 29)]
    [TestCase(3652058, 9999, 12, 31)]
    public void DayNumberReverseHelpers_ReturnGregorianDateParts(int dayNumber, int expectedYear, int expectedMonth, int expectedDay)
    {
        Assert.That(CalendarMath.GetYearFromDayNumber(dayNumber), Is.EqualTo(expectedYear));
        Assert.That(CalendarMath.GetMonthFromDayNumber(dayNumber), Is.EqualTo(expectedMonth));
        Assert.That(CalendarMath.GetDayFromDayNumber(dayNumber), Is.EqualTo(expectedDay));
    }

    [TestCase(-1)]
    [TestCase(3652059)]
    public void DayNumberReverseHelpers_RejectUnsupportedDayNumbers(int dayNumber)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.GetYearFromDayNumber(dayNumber))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayNumber"));
    }

    [Test]
    public void CalendarMath_MatchesDateOnlyAcrossRepresentativeYearsAndMonthEdges()
    {
        int[] years =
        [
            1,
            4,
            100,
            400,
            1582,
            1899,
            1900,
            1999,
            2000,
            2023,
            2024,
            2100,
            2400,
            9999
        ];

        foreach (int year in years)
        {
            DateOnly lastDayOfYear = new DateOnly(year, 12, 31);

            Assert.That(CalendarMath.IsLeapYear(year), Is.EqualTo(DateTime.IsLeapYear(year)));
            Assert.That(CalendarMath.GetDaysInYear(year), Is.EqualTo(lastDayOfYear.DayOfYear));
            Assert.That(CalendarMath.GetStartDayNumberOfYear(year), Is.EqualTo(new DateOnly(year, 1, 1).DayNumber));

            for (int month = 1; month <= 12; month++)
            {
                int daysInMonth = GetDateOnlyDaysInMonth(year, month);
                int[] days =
                [
                    1,
                    (daysInMonth + 1) / 2,
                    daysInMonth
                ];

                Assert.That(CalendarMath.GetDaysInMonth(year, month), Is.EqualTo(daysInMonth));

                foreach (int day in days)
                {
                    DateOnly date = new DateOnly(year, month, day);

                    Assert.That(CalendarMath.GetDayOfYear(year, month, day), Is.EqualTo(date.DayOfYear));
                    Assert.That(CalendarMath.GetDayNumber(year, month, day), Is.EqualTo(date.DayNumber));
                    Assert.That(CalendarMath.GetDayOfWeek(year, month, day), Is.EqualTo(GetIsoDayOfWeek(date)));
                    Assert.That(CalendarMath.GetYearFromDayNumber(date.DayNumber), Is.EqualTo(date.Year));
                    Assert.That(CalendarMath.GetMonthFromDayNumber(date.DayNumber), Is.EqualTo(date.Month));
                    Assert.That(CalendarMath.GetDayFromDayNumber(date.DayNumber), Is.EqualTo(date.Day));
                }
            }
        }
    }

    [Test]
    public void CalendarMath_MatchesDateOnlyAtSupportedDayNumberEdges()
    {
        for (int dayNumber = DateOnly.MinValue.DayNumber; dayNumber <= DateOnly.MinValue.DayNumber + 10; dayNumber++)
        {
            AssertDayNumberMatchesDateOnly(dayNumber);
        }

        for (int dayNumber = DateOnly.MaxValue.DayNumber - 10; dayNumber <= DateOnly.MaxValue.DayNumber; dayNumber++)
        {
            AssertDayNumberMatchesDateOnly(dayNumber);
        }
    }

    private static void AssertDayNumberMatchesDateOnly(int dayNumber)
    {
        DateOnly date = DateOnly.FromDayNumber(dayNumber);

        Assert.That(CalendarMath.GetDayNumber(date.Year, date.Month, date.Day), Is.EqualTo(dayNumber));
        Assert.That(CalendarMath.GetDayOfYear(date.Year, date.Month, date.Day), Is.EqualTo(date.DayOfYear));
        Assert.That(CalendarMath.GetDayOfWeek(date.Year, date.Month, date.Day), Is.EqualTo(GetIsoDayOfWeek(date)));
        Assert.That(CalendarMath.GetYearFromDayNumber(dayNumber), Is.EqualTo(date.Year));
        Assert.That(CalendarMath.GetMonthFromDayNumber(dayNumber), Is.EqualTo(date.Month));
        Assert.That(CalendarMath.GetDayFromDayNumber(dayNumber), Is.EqualTo(date.Day));
    }

    private static int GetDateOnlyDaysInMonth(int year, int month)
    {
        if (month == 12)
        {
            return new DateOnly(year, month, 31).Day;
        }

        DateOnly firstDayOfNextMonth = new DateOnly(year, month + 1, 1);
        return firstDayOfNextMonth.AddDays(-1).Day;
    }

    private static int GetIsoDayOfWeek(DateOnly date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0)
        {
            return 7;
        }

        return dayOfWeek;
    }
}
