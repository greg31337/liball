using System.Globalization;
using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class WeekRulesTests
{
    [Test]
    public void Iso_UsesMondayAndFourDayMinimum()
    {
        WeekRules rules = WeekRules.Iso();

        Assert.That(rules.GetFirstDayOfWeek(), Is.EqualTo(TimeConstants.IsoMonday));
        Assert.That(rules.GetMinimumDaysInFirstWeek(), Is.EqualTo(4));
    }

    [Test]
    public void Create_StoresConfiguredRules()
    {
        WeekRules rules = WeekRules.Create(TimeConstants.IsoSunday, 1);

        Assert.That(rules.GetFirstDayOfWeek(), Is.EqualTo(TimeConstants.IsoSunday));
        Assert.That(rules.GetMinimumDaysInFirstWeek(), Is.EqualTo(1));
    }

    [TestCase(0, 4, "firstDayOfWeek")]
    [TestCase(8, 4, "firstDayOfWeek")]
    [TestCase(1, 0, "minimumDaysInFirstWeek")]
    [TestCase(1, 8, "minimumDaysInFirstWeek")]
    public void Create_RejectsInvalidRules(int firstDayOfWeek, int minimumDaysInFirstWeek, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => WeekRules.Create(firstDayOfWeek, minimumDaysInFirstWeek))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase(2024, 1, 1, 2024, 1)]
    [TestCase(2024, 12, 31, 2025, 1)]
    [TestCase(2021, 1, 1, 2020, 53)]
    [TestCase(2020, 12, 31, 2020, 53)]
    public void IsoWeekRules_MatchDotNetIsoWeek(int year, int month, int day, int expectedWeekYear, int expectedWeek)
    {
        LocalDate date = LocalDate.Create(year, month, day);
        DateTime dateTime = new DateTime(year, month, day);
        WeekRules rules = WeekRules.Iso();

        Assert.That(rules.GetWeekYear(date), Is.EqualTo(expectedWeekYear));
        Assert.That(rules.GetWeekOfWeekYear(date), Is.EqualTo(expectedWeek));
        Assert.That(rules.GetWeekYear(date), Is.EqualTo(ISOWeek.GetYear(dateTime)));
        Assert.That(rules.GetWeekOfWeekYear(date), Is.EqualTo(ISOWeek.GetWeekOfYear(dateTime)));
    }

    [Test]
    public void GetStartOfWeek_ReturnsConfiguredWeekStart()
    {
        WeekRules sundayRules = WeekRules.Create(TimeConstants.IsoSunday, 1);
        WeekRules mondayRules = WeekRules.Iso();
        LocalDate wednesday = LocalDate.Create(2024, 5, 15);

        AssertDate(sundayRules.GetStartOfWeek(wednesday), 2024, 5, 12);
        AssertDate(mondayRules.GetStartOfWeek(wednesday), 2024, 5, 13);
    }

    [Test]
    public void ConfigurableRules_UseMinimumDaysToChooseFirstWeek()
    {
        WeekRules oneDayMinimum = WeekRules.Create(TimeConstants.IsoSunday, 1);
        WeekRules sevenDayMinimum = WeekRules.Create(TimeConstants.IsoSunday, 7);
        LocalDate date = LocalDate.Create(2022, 1, 1);

        Assert.That(oneDayMinimum.GetWeekYear(date), Is.EqualTo(2022));
        Assert.That(oneDayMinimum.GetWeekOfWeekYear(date), Is.EqualTo(1));
        Assert.That(sevenDayMinimum.GetWeekYear(date), Is.EqualTo(2021));
    }

    [Test]
    public void GetStartOfWeekYear_ReturnsFirstConfiguredWeekStart()
    {
        WeekRules iso = WeekRules.Iso();
        WeekRules sundayOneDay = WeekRules.Create(TimeConstants.IsoSunday, 1);

        AssertDate(iso.GetStartOfWeekYear(2024), 2024, 1, 1);
        AssertDate(iso.GetStartOfWeekYear(2025), 2024, 12, 30);
        AssertDate(sundayOneDay.GetStartOfWeekYear(2022), 2021, 12, 26);
    }

    [Test]
    public void Methods_RequireDate()
    {
        WeekRules rules = WeekRules.Iso();

        Assert.Throws<ArgumentNullException>(() => rules.GetStartOfWeek(null!));
        Assert.Throws<ArgumentNullException>(() => rules.GetWeekYear(null!));
        Assert.Throws<ArgumentNullException>(() => rules.GetWeekOfWeekYear(null!));
    }

    [Test]
    public void IsoWeekRules_MatchDotNetIsoWeekAcrossYearBoundaryWindows()
    {
        WeekRules rules = WeekRules.Iso();

        for (int year = 1995; year <= 2030; year++)
        {
            DateTime december = new DateTime(year, 12, 29);
            for (int offset = 0; offset <= 6; offset++)
            {
                DateTime dateTime = december.AddDays(offset);
                LocalDate date = LocalDate.Create(dateTime.Year, dateTime.Month, dateTime.Day);

                Assert.That(rules.GetWeekYear(date), Is.EqualTo(ISOWeek.GetYear(dateTime)));
                Assert.That(rules.GetWeekOfWeekYear(date), Is.EqualTo(ISOWeek.GetWeekOfYear(dateTime)));
            }
        }
    }

    [Test]
    public void SundayStartWithOneDayMinimum_StartsWeekYearInPreviousCalendarYear()
    {
        WeekRules rules = WeekRules.Create(TimeConstants.IsoSunday, 1);

        AssertDate(rules.GetStartOfWeekYear(2022), 2021, 12, 26);
        Assert.That(rules.GetWeekYear(LocalDate.Create(2022, 1, 1)), Is.EqualTo(2022));
        Assert.That(rules.GetWeekOfWeekYear(LocalDate.Create(2022, 1, 1)), Is.EqualTo(1));
        Assert.That(rules.GetWeekYear(LocalDate.Create(2021, 12, 31)), Is.EqualTo(2022));
        Assert.That(rules.GetWeekOfWeekYear(LocalDate.Create(2021, 12, 31)), Is.EqualTo(1));
    }

    [Test]
    public void SundayStartWithSevenDayMinimum_DelaysFirstWeekUntilFullWeek()
    {
        WeekRules rules = WeekRules.Create(TimeConstants.IsoSunday, 7);

        AssertDate(rules.GetStartOfWeekYear(2022), 2022, 1, 2);
        Assert.That(rules.GetWeekYear(LocalDate.Create(2022, 1, 1)), Is.EqualTo(2021));
        Assert.That(rules.GetWeekOfWeekYear(LocalDate.Create(2022, 1, 1)), Is.EqualTo(52));
        Assert.That(rules.GetWeekYear(LocalDate.Create(2022, 1, 2)), Is.EqualTo(2022));
        Assert.That(rules.GetWeekOfWeekYear(LocalDate.Create(2022, 1, 2)), Is.EqualTo(1));
    }

    [Test]
    public void FirstWeekRules_CanMoveLateDecemberIntoNextWeekYear()
    {
        WeekRules sundayOneDay = WeekRules.Create(TimeConstants.IsoSunday, 1);
        WeekRules sundaySevenDays = WeekRules.Create(TimeConstants.IsoSunday, 7);
        LocalDate date = LocalDate.Create(2021, 12, 26);

        Assert.That(sundayOneDay.GetWeekYear(date), Is.EqualTo(2022));
        Assert.That(sundayOneDay.GetWeekOfWeekYear(date), Is.EqualTo(1));
        Assert.That(sundaySevenDays.GetWeekYear(date), Is.EqualTo(2021));
        Assert.That(sundaySevenDays.GetWeekOfWeekYear(date), Is.EqualTo(52));
    }

    [Test]
    public void GetStartOfWeekYear_ClampsAtMinimumSupportedDateWhenRuleWouldStartBeforeRange()
    {
        WeekRules sundayOneDay = WeekRules.Create(TimeConstants.IsoSunday, 1);

        AssertDate(sundayOneDay.GetStartOfWeekYear(1), 1, 1, 1);
    }

    private static void AssertDate(LocalDate date, int year, int month, int day)
    {
        Assert.That(date.GetYear(), Is.EqualTo(year));
        Assert.That(date.GetMonth(), Is.EqualTo(month));
        Assert.That(date.GetDay(), Is.EqualTo(day));
    }
}
