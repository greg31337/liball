using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceExpansionTests
{
    [Test]
    public void AddFrequency_ExpandsSecondlyCandidates()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 1, 1, 0, 0, 58),
            RecurrenceFrequency.Secondly,
            5);

        AssertFields(result, 2024, 1, 1, 0, 1, 3);
    }

    [Test]
    public void AddFrequency_ExpandsMinutelyCandidates()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 1, 1, 23, 58, 30),
            RecurrenceFrequency.Minutely,
            3);

        AssertFields(result, 2024, 1, 2, 0, 1, 30);
    }

    [Test]
    public void AddFrequency_ExpandsHourlyCandidates()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 12, 31, 22, 15, 0),
            RecurrenceFrequency.Hourly,
            4);

        AssertFields(result, 2025, 1, 1, 2, 15, 0);
    }

    [Test]
    public void AddFrequency_ExpandsDailyCandidates()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 2, 27, 8, 30, 0),
            RecurrenceFrequency.Daily,
            2);

        AssertFields(result, 2024, 2, 29, 8, 30, 0);
    }

    [Test]
    public void AddFrequency_ExpandsWeeklyCandidates()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 12, 20, 9, 0, 0),
            RecurrenceFrequency.Weekly,
            2);

        AssertFields(result, 2025, 1, 3, 9, 0, 0);
    }

    [Test]
    public void AddFrequency_ExpandsMonthlyCandidatesWithCalendarNormalization()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 1, 31, 10, 0, 0),
            RecurrenceFrequency.Monthly,
            1);

        AssertFields(result, 2024, 2, 29, 10, 0, 0);
    }

    [Test]
    public void AddFrequency_ExpandsYearlyCandidatesWithCalendarNormalization()
    {
        LocalDateTime result = RecurrenceExpansion.AddFrequency(
            LocalDateTime.Create(2024, 2, 29, 10, 0, 0),
            RecurrenceFrequency.Yearly,
            1);

        AssertFields(result, 2025, 2, 28, 10, 0, 0);
    }

    [Test]
    public void GetNextCandidate_UsesRuleFrequencyAndInterval()
    {
        RecurrenceRule rule = new RecurrenceRule(
            RecurrenceFrequency.Hourly,
            6,
            0,
            null,
            null,
            TimeConstants.IsoMonday,
            new int[0],
            new int[0],
            new int[0],
            new RecurrenceDay[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);

        LocalDateTime result = RecurrenceExpansion.GetNextCandidate(
            LocalDateTime.Create(2024, 1, 1, 20, 0, 0),
            rule);

        AssertFields(result, 2024, 1, 2, 2, 0, 0);
    }

    [Test]
    public void Expansion_RequiresCandidateAndRule()
    {
        LocalDateTime candidate = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);
        RecurrenceRule rule = RecurrenceRule.Create(RecurrenceFrequency.Daily);

        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.AddFrequency(null!, RecurrenceFrequency.Daily, 1))!.ParamName, Is.EqualTo("candidate"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.GetNextCandidate(null!, rule))!.ParamName, Is.EqualTo("candidate"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.GetNextCandidate(candidate, null!))!.ParamName, Is.EqualTo("rule"));
    }

    [Test]
    public void AddFrequency_RejectsInvalidFrequencyOrInterval()
    {
        LocalDateTime candidate = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceExpansion.AddFrequency(candidate, RecurrenceFrequency.Daily, 0))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceExpansion.AddFrequency(candidate, (RecurrenceFrequency)99, 1))!.ParamName, Is.EqualTo("frequency"));
    }

    [Test]
    public void MatchesFilters_AppliesBySecondByMinuteByHourAndByMonth()
    {
        RecurrenceRule rule = CreateRule(
            bySecond: new int[] { 30 },
            byMinute: new int[] { 15 },
            byHour: new int[] { 9 },
            byMonth: new int[] { 5 });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 1, 9, 15, 30), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 1, 9, 15, 31), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 1, 9, 16, 30), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 1, 10, 15, 30), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 6, 1, 9, 15, 30), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesByDayWithoutOrdinal()
    {
        RecurrenceRule rule = CreateRule(
            byDay: new RecurrenceDay[]
            {
                RecurrenceDay.Create(TimeConstants.IsoMonday),
                RecurrenceDay.Create(TimeConstants.IsoWednesday)
            });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 1, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 3, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 2, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesMonthlyByDayOrdinals()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Monthly,
            byDay: new RecurrenceDay[]
            {
                RecurrenceDay.Create(TimeConstants.IsoMonday, 1),
                RecurrenceDay.Create(TimeConstants.IsoFriday, -1)
            });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 6, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 31, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 13, 0, 0, 0), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 24, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesYearlyByDayOrdinalsWhenByMonthIsAbsent()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            byDay: new RecurrenceDay[]
            {
                RecurrenceDay.Create(TimeConstants.IsoMonday, 1),
                RecurrenceDay.Create(TimeConstants.IsoTuesday, -1)
            });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 1, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 31, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 8, 0, 0, 0), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 24, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesMonthScopedYearlyByDayOrdinalsWhenByMonthIsPresent()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            byMonth: new int[] { 5 },
            byDay: new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoFriday, -1) });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 5, 31, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 27, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesByMonthDay()
    {
        RecurrenceRule rule = CreateRule(byMonthDay: new int[] { 15, -1 });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 2, 15, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 2, 29, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 2, 28, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesByYearDay()
    {
        RecurrenceRule rule = CreateRule(frequency: RecurrenceFrequency.Yearly, byYearDay: new int[] { 60, -1 });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 2, 29, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 31, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 2, 28, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void MatchesFilters_AppliesByWeekNo()
    {
        RecurrenceRule rule = CreateRule(frequency: RecurrenceFrequency.Yearly, byWeekNo: new int[] { 1, -1 });

        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 1, 1, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 23, 0, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesFilters(LocalDateTime.Create(2024, 12, 16, 0, 0, 0), rule), Is.False);
    }

    [Test]
    public void FilterCandidates_AppliesBaseFiltersThenSetPosition()
    {
        LocalDateTime[] candidates = new LocalDateTime[]
        {
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 2, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 3, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 4, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 5, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 6, 9, 0, 0)
        };
        RecurrenceRule rule = CreateRule(
            byMonthDay: new int[] { 1, 2, 3, 4, 5 },
            bySetPos: new int[] { 2, -1 });

        LocalDateTime[] result = RecurrenceExpansion.FilterCandidates(candidates, rule);

        Assert.That(result.Length, Is.EqualTo(2));
        AssertFields(result[0], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 5, 9, 0, 0);
    }

    [Test]
    public void FilterCandidates_IgnoresSetPositionsOutsideCandidateRangeAndDeduplicates()
    {
        LocalDateTime[] candidates = new LocalDateTime[]
        {
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            LocalDateTime.Create(2024, 1, 2, 9, 0, 0)
        };
        RecurrenceRule rule = CreateRule(byMonth: new int[] { 1 }, bySetPos: new int[] { 1, -2, 3 });

        LocalDateTime[] result = RecurrenceExpansion.FilterCandidates(candidates, rule);

        Assert.That(result.Length, Is.EqualTo(1));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
    }

    [Test]
    public void Filtering_RequiresCandidateRuleAndCandidateArrayValues()
    {
        LocalDateTime candidate = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);
        RecurrenceRule rule = RecurrenceRule.Create(RecurrenceFrequency.Daily);

        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.MatchesFilters(null!, rule))!.ParamName, Is.EqualTo("candidate"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.MatchesFilters(candidate, null!))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.FilterCandidates(null!, rule))!.ParamName, Is.EqualTo("candidates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.FilterCandidates(new LocalDateTime[0], null!))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentException>(() => RecurrenceExpansion.FilterCandidates(new LocalDateTime[] { null! }, rule))!.ParamName, Is.EqualTo("candidates"));
    }

    [Test]
    public void ExpandBounded_IncludesDtStartAndStopsAtCount()
    {
        RecurrenceRule rule = CreateRule(count: 3);

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 3, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_StopsAtInclusiveUntil()
    {
        RecurrenceRule rule = CreateRule(localUntil: LocalDateTime.Create(2024, 1, 3, 9, 0, 0));

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 3, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_ReturnsEmptyWhenUntilPrecedesDtStart()
    {
        RecurrenceRule rule = CreateRule(localUntil: LocalDateTime.Create(2023, 12, 31, 9, 0, 0));

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void ExpandBounded_HonorsRuleInterval()
    {
        RecurrenceRule rule = CreateRule(interval: 2, count: 3);

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 3, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 5, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_ExcludesDtStartWhenFiltersRejectIt()
    {
        RecurrenceRule rule = CreateRule(byDay: new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoTuesday) });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            5);

        Assert.That(result.Length, Is.EqualTo(1));
        AssertFields(result[0], 2024, 1, 2, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_SecondlyRulesLimitTheSteppedSecond()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Secondly,
            count: 3,
            bySecond: new int[] { 1, 3, 5 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 0, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 0, 0, 1);
        AssertFields(result[1], 2024, 1, 1, 0, 0, 3);
        AssertFields(result[2], 2024, 1, 1, 0, 0, 5);
    }

    [Test]
    public void ExpandBounded_MinutelyRulesExpandSecondsWithinEachMinute()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Minutely,
            count: 4,
            bySecond: new int[] { 40, 10 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 20),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(4));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 40);
        AssertFields(result[1], 2024, 1, 1, 9, 1, 10);
        AssertFields(result[2], 2024, 1, 1, 9, 1, 40);
        AssertFields(result[3], 2024, 1, 1, 9, 2, 10);
    }

    [Test]
    public void ExpandBounded_HourlyRulesExpandMinutesAndSecondsInChronologicalOrder()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Hourly,
            count: 5,
            byMinute: new int[] { 45, 15 },
            bySecond: new int[] { 30, 0 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 20, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(5));
        AssertFields(result[0], 2024, 1, 1, 9, 45, 0);
        AssertFields(result[1], 2024, 1, 1, 9, 45, 30);
        AssertFields(result[2], 2024, 1, 1, 10, 15, 0);
        AssertFields(result[3], 2024, 1, 1, 10, 15, 30);
        AssertFields(result[4], 2024, 1, 1, 10, 45, 0);
    }

    [Test]
    public void ExpandBounded_DailyRulesExpandTimesWithinEachSelectedDay()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Daily,
            interval: 2,
            count: 5,
            byHour: new int[] { 16, 8 },
            byMinute: new int[] { 30 },
            bySecond: new int[] { 0 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 10, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(5));
        AssertFields(result[0], 2024, 1, 1, 16, 30, 0);
        AssertFields(result[1], 2024, 1, 3, 8, 30, 0);
        AssertFields(result[2], 2024, 1, 3, 16, 30, 0);
        AssertFields(result[3], 2024, 1, 5, 8, 30, 0);
        AssertFields(result[4], 2024, 1, 5, 16, 30, 0);
    }

    [Test]
    public void ExpandBounded_WeeklyRulesExpandDaysAcrossCalendarBoundaries()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Weekly,
            count: 4,
            byDay: new RecurrenceDay[]
            {
                RecurrenceDay.Create(TimeConstants.IsoWednesday),
                RecurrenceDay.Create(TimeConstants.IsoMonday)
            });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 12, 28, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(4));
        AssertFields(result[0], 2024, 12, 30, 9, 0, 0);
        AssertFields(result[1], 2025, 1, 1, 9, 0, 0);
        AssertFields(result[2], 2025, 1, 6, 9, 0, 0);
        AssertFields(result[3], 2025, 1, 8, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_MonthlySetPositionSelectsTheLastWeekdayPerMonth()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Monthly,
            count: 3,
            byDay: new RecurrenceDay[]
            {
                RecurrenceDay.Create(TimeConstants.IsoMonday),
                RecurrenceDay.Create(TimeConstants.IsoTuesday),
                RecurrenceDay.Create(TimeConstants.IsoWednesday),
                RecurrenceDay.Create(TimeConstants.IsoThursday),
                RecurrenceDay.Create(TimeConstants.IsoFriday)
            },
            bySetPos: new int[] { -1 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 31, 9, 0, 0);
        AssertFields(result[1], 2024, 2, 29, 9, 0, 0);
        AssertFields(result[2], 2024, 3, 29, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_MonthlyDefaultsOmitInvalidMonthEndDates()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Monthly,
            count: 3);

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 31, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 31, 9, 0, 0);
        AssertFields(result[1], 2024, 3, 31, 9, 0, 0);
        AssertFields(result[2], 2024, 5, 31, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_YearlyRulesGenerateLeapDaysWithoutConstrainingThem()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 3,
            byMonthDay: new int[] { 29 },
            byMonth: new int[] { 2 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2023, 1, 1, 9, 0, 0),
            rule,
            12);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 2, 29, 9, 0, 0);
        AssertFields(result[1], 2028, 2, 29, 9, 0, 0);
        AssertFields(result[2], 2032, 2, 29, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_YearlyDefaultOmitsInvalidLeapDayYears()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 3);

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 2, 29, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 2, 29, 9, 0, 0);
        AssertFields(result[1], 2028, 2, 29, 9, 0, 0);
        AssertFields(result[2], 2032, 2, 29, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_YearlyMonthDefaultsSortDeduplicateAndOmitInvalidDays()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 4,
            byMonth: new int[] { 4, 3, 2, 4 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 30, 9, 0, 0),
            rule,
            10);

        Assert.That(result.Length, Is.EqualTo(4));
        AssertFields(result[0], 2024, 3, 30, 9, 0, 0);
        AssertFields(result[1], 2024, 4, 30, 9, 0, 0);
        AssertFields(result[2], 2025, 3, 30, 9, 0, 0);
        AssertFields(result[3], 2025, 4, 30, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_YearlyWeekNumbersUseTheConfiguredWeekYearBoundary()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 3,
            byDay: new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoMonday) },
            byWeekNo: new int[] { 1 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            5);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 12, 30, 9, 0, 0);
        AssertFields(result[2], 2025, 12, 29, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_YearlyWeekNumberHandlesMaximumSupportedWeekYear()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 1,
            byDay: new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoFriday) },
            byWeekNo: new int[] { -1 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(9999, 1, 1, 9, 0, 0),
            rule,
            2);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].GetYear(), Is.EqualTo(9999));
        Assert.That(result[0].GetDate().GetDayOfWeek(), Is.EqualTo(TimeConstants.IsoFriday));
    }

    [Test]
    public void ExpandBounded_SetPositionsAreAppliedPerYearAndReturnedChronologically()
    {
        RecurrenceRule rule = CreateRule(
            frequency: RecurrenceFrequency.Yearly,
            count: 4,
            byMonthDay: new int[] { 1, -1 },
            byMonth: new int[] { 3, 9 },
            bySetPos: new int[] { -1, 1 });

        LocalDateTime[] result = RecurrenceExpansion.ExpandBounded(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            rule,
            5);

        Assert.That(result.Length, Is.EqualTo(4));
        AssertFields(result[0], 2024, 3, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 9, 30, 9, 0, 0);
        AssertFields(result[2], 2025, 3, 1, 9, 0, 0);
        AssertFields(result[3], 2025, 9, 30, 9, 0, 0);
    }

    [Test]
    public void IsOnInterval_UsesWeekStartForWeeklyRules()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 3, 9, 0, 0);
        LocalDateTime candidate = LocalDateTime.Create(2024, 1, 14, 9, 0, 0);
        RecurrenceRule mondayRule = CreateRule(frequency: RecurrenceFrequency.Weekly, interval: 2, weekStart: TimeConstants.IsoMonday);
        RecurrenceRule sundayRule = CreateRule(frequency: RecurrenceFrequency.Weekly, interval: 2, weekStart: TimeConstants.IsoSunday);

        Assert.That(RecurrenceExpansion.IsOnInterval(start, candidate, mondayRule), Is.False);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, candidate, sundayRule), Is.True);
    }

    [Test]
    public void IsOnInterval_CoversFixedAndCalendarFrequencies()
    {
        LocalDateTime start = LocalDateTime.Parse("2024-01-01T00:00:00.5");

        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-01-01T00:00:02.5"), CreateRule(frequency: RecurrenceFrequency.Secondly, interval: 2)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-01-01T00:00:01"), CreateRule(frequency: RecurrenceFrequency.Secondly, interval: 1)), Is.False);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-01-01T00:04:00.5"), CreateRule(frequency: RecurrenceFrequency.Minutely, interval: 2)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-01-01T06:00:00.5"), CreateRule(frequency: RecurrenceFrequency.Hourly, interval: 3)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-01-05T00:00:00.5"), CreateRule(frequency: RecurrenceFrequency.Daily, interval: 2)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2024-05-01T00:00:00.5"), CreateRule(frequency: RecurrenceFrequency.Monthly, interval: 2)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2028-01-01T00:00:00.5"), CreateRule(frequency: RecurrenceFrequency.Yearly, interval: 2)), Is.True);
        Assert.That(RecurrenceExpansion.IsOnInterval(start, LocalDateTime.Parse("2023-12-31T23:59:59.5"), CreateRule(frequency: RecurrenceFrequency.Secondly)), Is.False);
    }

    [Test]
    public void MatchesOccurrence_RejectsBeforeStartOutsideIntervalUntilOrFilters()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceRule rule = CreateRule(
            interval: 2,
            localUntil: LocalDateTime.Create(2024, 1, 5, 9, 0, 0),
            byDay: new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoFriday) });

        Assert.That(RecurrenceExpansion.MatchesOccurrence(start, LocalDateTime.Create(2023, 12, 29, 9, 0, 0), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesOccurrence(start, LocalDateTime.Create(2024, 1, 2, 9, 0, 0), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesOccurrence(start, LocalDateTime.Create(2024, 1, 3, 9, 0, 0), rule), Is.False);
        Assert.That(RecurrenceExpansion.MatchesOccurrence(start, LocalDateTime.Create(2024, 1, 5, 9, 0, 0), rule), Is.True);
        Assert.That(RecurrenceExpansion.MatchesOccurrence(start, LocalDateTime.Create(2024, 1, 19, 9, 0, 0), rule), Is.False);
    }

    [Test]
    public void BoundedExpansion_RequiresStartRuleAndPositiveMaximumCandidates()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceRule rule = RecurrenceRule.Create(RecurrenceFrequency.Daily);

        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.ExpandBounded(null!, rule, 1))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.ExpandBounded(start, null!, 1))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceExpansion.ExpandBounded(start, rule, 0))!.ParamName, Is.EqualTo("maximumCandidates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.MatchesOccurrence(null!, start, rule))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.MatchesOccurrence(start, null!, rule))!.ParamName, Is.EqualTo("candidate"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.MatchesOccurrence(start, start, null!))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.IsOnInterval(null!, start, rule))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.IsOnInterval(start, null!, rule))!.ParamName, Is.EqualTo("candidate"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceExpansion.IsOnInterval(start, start, null!))!.ParamName, Is.EqualTo("rule"));
    }

    static void AssertFields(LocalDateTime value, int year, int month, int day, int hour, int minute, int second)
    {
        Assert.That(value.GetYear(), Is.EqualTo(year));
        Assert.That(value.GetMonth(), Is.EqualTo(month));
        Assert.That(value.GetDay(), Is.EqualTo(day));
        Assert.That(value.GetHour(), Is.EqualTo(hour));
        Assert.That(value.GetMinute(), Is.EqualTo(minute));
        Assert.That(value.GetSecond(), Is.EqualTo(second));
    }

    static RecurrenceRule CreateRule(
        RecurrenceFrequency frequency = RecurrenceFrequency.Daily,
        int interval = 1,
        int count = 0,
        Instant? until = null,
        LocalDateTime? localUntil = null,
        int weekStart = TimeConstants.IsoMonday,
        int[]? bySecond = null,
        int[]? byMinute = null,
        int[]? byHour = null,
        RecurrenceDay[]? byDay = null,
        int[]? byMonthDay = null,
        int[]? byYearDay = null,
        int[]? byWeekNo = null,
        int[]? byMonth = null,
        int[]? bySetPos = null)
    {
        return new RecurrenceRule(
            frequency,
            interval,
            count,
            until,
            localUntil,
            weekStart,
            bySecond ?? new int[0],
            byMinute ?? new int[0],
            byHour ?? new int[0],
            byDay ?? new RecurrenceDay[0],
            byMonthDay ?? new int[0],
            byYearDay ?? new int[0],
            byWeekNo ?? new int[0],
            byMonth ?? new int[0],
            bySetPos ?? new int[0]);
    }
}
