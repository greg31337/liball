using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceSetTests
{
    [Test]
    public void Create_StoresStartAndDefaultsToEmptyCollections()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceSet set = RecurrenceSet.Create(start);

        Assert.That(set.GetStart().EqualsValue(start), Is.True);
        Assert.That(set.GetRules().Length, Is.EqualTo(0));
        Assert.That(set.GetRDates().Length, Is.EqualTo(0));
        Assert.That(set.GetExDates().Length, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_StoresValuesAndCopiesArrays()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceRule daily = CreateRule(count: 2);
        RecurrenceRule weekly = CreateRule(frequency: RecurrenceFrequency.Weekly, count: 1);
        RecurrenceRule[] rules = new RecurrenceRule[] { daily };
        LocalDateTime[] rDates = new LocalDateTime[] { LocalDateTime.Create(2024, 1, 10, 9, 0, 0) };
        LocalDateTime[] exDates = new LocalDateTime[] { LocalDateTime.Create(2024, 1, 2, 9, 0, 0) };
        RecurrenceSet set = new RecurrenceSet(start, rules, rDates, exDates);

        rules[0] = weekly;
        rDates[0] = LocalDateTime.Create(2024, 1, 11, 9, 0, 0);
        exDates[0] = LocalDateTime.Create(2024, 1, 12, 9, 0, 0);
        RecurrenceRule[] returnedRules = set.GetRules();
        LocalDateTime[] returnedRDates = set.GetRDates();
        LocalDateTime[] returnedExDates = set.GetExDates();
        returnedRules[0] = weekly;
        returnedRDates[0] = LocalDateTime.Create(2024, 1, 13, 9, 0, 0);
        returnedExDates[0] = LocalDateTime.Create(2024, 1, 14, 9, 0, 0);

        Assert.That(set.GetRules()[0].GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        AssertFields(set.GetRDates()[0], 2024, 1, 10, 9, 0, 0);
        AssertFields(set.GetExDates()[0], 2024, 1, 2, 9, 0, 0);
    }

    [Test]
    public void AddMethods_AppendValuesAndReturnSet()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceRule rule = CreateRule(count: 1);
        LocalDateTime rDate = LocalDateTime.Create(2024, 1, 5, 9, 0, 0);
        LocalDateTime exDate = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceSet set = RecurrenceSet.Create(start);

        RecurrenceSet result = set.AddRule(rule).AddRDate(rDate).AddExDate(exDate);

        Assert.That(result, Is.SameAs(set));
        Assert.That(set.GetRules().Length, Is.EqualTo(1));
        Assert.That(set.GetRules()[0].GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        AssertFields(set.GetRDates()[0], 2024, 1, 5, 9, 0, 0);
        AssertFields(set.GetExDates()[0], 2024, 1, 1, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_MergesRulesRDatesExDatesDeduplicatesAndSorts()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(CreateRule(count: 3));
        set.AddRule(CreateRule(interval: 2, count: 3));
        set.AddRDate(LocalDateTime.Create(2024, 1, 4, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 2, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 4, 9, 0, 0));
        set.AddExDate(LocalDateTime.Create(2024, 1, 3, 9, 0, 0));
        set.AddExDate(LocalDateTime.Create(2024, 1, 10, 9, 0, 0));

        LocalDateTime[] result = set.ExpandBounded(10);

        Assert.That(result.Length, Is.EqualTo(4));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 4, 9, 0, 0);
        AssertFields(result[3], 2024, 1, 5, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_AppliesFinalBoundAfterSorting()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 3, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 2, 9, 0, 0));

        LocalDateTime[] result = set.ExpandBounded(2);

        Assert.That(result.Length, Is.EqualTo(2));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_UsesExplicitDatesWithoutRules()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 5, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 6, 9, 0, 0));
        set.AddExDate(LocalDateTime.Create(2024, 1, 5, 9, 0, 0));

        LocalDateTime[] result = set.ExpandBounded(10);

        Assert.That(result.Length, Is.EqualTo(2));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 6, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_IncludesDtStartWithoutRulesAndExDateCanRemoveIt()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceSet included = RecurrenceSet.Create(start);
        RecurrenceSet excluded = RecurrenceSet.Create(start).AddExDate(start);

        LocalDateTime[] includedResult = included.ExpandBounded(10);
        LocalDateTime[] excludedResult = excluded.ExpandBounded(10);

        Assert.That(includedResult.Length, Is.EqualTo(1));
        AssertFields(includedResult[0], 2024, 1, 1, 9, 0, 0);
        Assert.That(excludedResult, Is.Empty);
    }

    [Test]
    public void ExpandBounded_ExDateRemovesDtStartFromEveryOverlappingInclusionSource()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(CreateRule(count: 2));
        set.AddRDate(start);
        set.AddExDate(start);

        LocalDateTime[] result = set.ExpandBounded(10);

        Assert.That(result.Length, Is.EqualTo(1));
        AssertFields(result[0], 2024, 1, 2, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_CountIncludesDtStartWhenItDoesNotMatchTheRuleFilter()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(RecurrenceRule.Parse("FREQ=DAILY;COUNT=2;BYDAY=TU"));

        LocalDateTime[] result = set.ExpandBounded(20);

        Assert.That(result.Length, Is.EqualTo(2));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_AppliesEachRulesInclusiveUntilBeforeMergingSources()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(RecurrenceRule.Parse("FREQ=DAILY;UNTIL=20240103T090000"));
        set.AddRule(RecurrenceRule.Parse("FREQ=WEEKLY;UNTIL=20240115T090000"));
        set.AddRDate(LocalDateTime.Create(2024, 1, 20, 9, 0, 0));
        set.AddExDate(LocalDateTime.Create(2024, 1, 8, 9, 0, 0));

        LocalDateTime[] result = set.ExpandBounded(30);

        Assert.That(result.Length, Is.EqualTo(5));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 3, 9, 0, 0);
        AssertFields(result[3], 2024, 1, 15, 9, 0, 0);
        AssertFields(result[4], 2024, 1, 20, 9, 0, 0);
    }

    [Test]
    public void ExpandThrough_AppliesInclusiveLocalEndToInfiniteRulesAndExplicitDates()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(RecurrenceRule.Parse("FREQ=DAILY"));
        set.AddRDate(LocalDateTime.Create(2024, 1, 2, 9, 0, 0));
        set.AddRDate(LocalDateTime.Create(2024, 1, 10, 9, 0, 0));

        LocalDateTime[] result = set.ExpandThrough(LocalDateTime.Create(2024, 1, 3, 9, 0, 0), 20);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 3, 9, 0, 0);
    }

    [Test]
    public void ExpandBounded_SafetyLimitIsAppliedAfterGlobalOrdering()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 10, 9, 0, 0));
        set.AddRule(RecurrenceRule.Parse("FREQ=DAILY"));
        set.AddRDate(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));

        LocalDateTime[] result = set.ExpandBounded(3);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 10, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 11, 9, 0, 0);
    }

    [Test]
    public void Constructor_RequiresValues()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceRule rule = CreateRule(count: 1);
        LocalDateTime date = LocalDateTime.Create(2024, 1, 2, 9, 0, 0);

        Assert.That(Assert.Throws<ArgumentNullException>(() => new RecurrenceSet(null!, new RecurrenceRule[0], new LocalDateTime[0], new LocalDateTime[0]))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new RecurrenceSet(start, null!, new LocalDateTime[0], new LocalDateTime[0]))!.ParamName, Is.EqualTo("rules"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new RecurrenceSet(start, new RecurrenceRule[0], null!, new LocalDateTime[0]))!.ParamName, Is.EqualTo("rDates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new RecurrenceSet(start, new RecurrenceRule[0], new LocalDateTime[0], null!))!.ParamName, Is.EqualTo("exDates"));
        Assert.That(Assert.Throws<ArgumentException>(() => new RecurrenceSet(start, new RecurrenceRule[] { null! }, new LocalDateTime[0], new LocalDateTime[0]))!.ParamName, Is.EqualTo("rules"));
        Assert.That(Assert.Throws<ArgumentException>(() => new RecurrenceSet(start, new RecurrenceRule[] { rule }, new LocalDateTime[] { null! }, new LocalDateTime[0]))!.ParamName, Is.EqualTo("rDates"));
        Assert.That(Assert.Throws<ArgumentException>(() => new RecurrenceSet(start, new RecurrenceRule[] { rule }, new LocalDateTime[] { date }, new LocalDateTime[] { null! }))!.ParamName, Is.EqualTo("exDates"));
    }

    [Test]
    public void MutatorsAndExpansion_ValidateInputs()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));

        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceSet.Create(null!))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => set.AddRule(null!))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => set.AddRDate(null!))!.ParamName, Is.EqualTo("date"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => set.AddExDate(null!))!.ParamName, Is.EqualTo("date"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => set.ExpandBounded(0))!.ParamName, Is.EqualTo("maximumCandidates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => set.ExpandThrough(null!, 1))!.ParamName, Is.EqualTo("end"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => set.ExpandThrough(LocalDateTime.Create(2024, 1, 2, 9, 0, 0), 0))!.ParamName, Is.EqualTo("maximumCandidates"));
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
        int count = 0)
    {
        return new RecurrenceRule(
            frequency,
            interval,
            count,
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
    }
}
