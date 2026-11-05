using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceBuilderTests
{
    [Test]
    public void FactoryMethods_CreateCommonFrequencies()
    {
        Assert.That(RecurrenceBuilder.Daily().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.That(RecurrenceBuilder.Weekly().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Weekly));
        Assert.That(RecurrenceBuilder.Monthly().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Monthly));
        Assert.That(RecurrenceBuilder.Yearly().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Yearly));
    }

    [TestCase(RecurrenceFrequency.Secondly)]
    [TestCase(RecurrenceFrequency.Minutely)]
    [TestCase(RecurrenceFrequency.Hourly)]
    [TestCase(RecurrenceFrequency.Daily)]
    [TestCase(RecurrenceFrequency.Weekly)]
    [TestCase(RecurrenceFrequency.Monthly)]
    [TestCase(RecurrenceFrequency.Yearly)]
    public void PublicFactory_CreatesEverySupportedFrequency(RecurrenceFrequency frequency)
    {
        RecurrenceBuilder builder = TimeApi.Factory.CreateRecurrenceBuilder(frequency);

        Assert.That(builder.Build().GetFrequency(), Is.EqualTo(frequency));
    }

    [Test]
    public void Build_UsesDefaultRuleValues()
    {
        RecurrenceRule rule = RecurrenceBuilder.Daily().Build();

        Assert.That(rule.GetInterval(), Is.EqualTo(1));
        Assert.That(rule.GetCount(), Is.EqualTo(0));
        Assert.That(rule.GetUntil(), Is.Null);
        Assert.That(rule.GetWeekStart(), Is.EqualTo(TimeConstants.IsoMonday));
        Assert.That(rule.GetByDay().Length, Is.EqualTo(0));
        Assert.That(rule.GetByMonthDay().Length, Is.EqualTo(0));
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=DAILY"));
    }

    [Test]
    public void Setters_ReturnSameBuilderForChaining()
    {
        RecurrenceBuilder builder = RecurrenceBuilder.Weekly();
        Instant until = Instant.Parse("2024-12-31T00:00:00Z");

        Assert.That(builder.SetInterval(2), Is.SameAs(builder));
        Assert.That(builder.SetUntil(until), Is.SameAs(builder));
        Assert.That(builder.SetLocalUntil(LocalDateTime.Create(2024, 12, 31, 0, 0, 0)), Is.SameAs(builder));
        Assert.That(builder.SetWeekStart(TimeConstants.IsoSunday), Is.SameAs(builder));
        Assert.That(builder.AddBySecond(1), Is.SameAs(builder));
        Assert.That(builder.AddByMinute(2), Is.SameAs(builder));
        Assert.That(builder.AddByHour(3), Is.SameAs(builder));
        Assert.That(builder.AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday)), Is.SameAs(builder));
        Assert.That(builder.AddByMonthDay(1), Is.SameAs(builder));
        Assert.That(builder.AddByYearDay(2), Is.SameAs(builder));
        Assert.That(builder.AddByWeekNo(3), Is.SameAs(builder));
        Assert.That(builder.AddByMonth(4), Is.SameAs(builder));
        Assert.That(builder.AddBySetPos(1), Is.SameAs(builder));
    }

    [Test]
    public void BuilderContract_PopulatesEverySupportedFilter()
    {
        IRecurrenceBuilder builder = new RecurrenceBuilder(RecurrenceFrequency.Yearly);
        RecurrenceRule rule = builder
            .AddBySecond(5)
            .AddByMinute(10)
            .AddByHour(15)
            .AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday))
            .AddByMonthDay(-1)
            .AddByYearDay(100)
            .AddByWeekNo(20)
            .AddByMonth(6)
            .AddBySetPos(-1)
            .Build();

        Assert.That(rule.GetBySecond(), Is.EqualTo(new int[] { 5 }));
        Assert.That(rule.GetByMinute(), Is.EqualTo(new int[] { 10 }));
        Assert.That(rule.GetByHour(), Is.EqualTo(new int[] { 15 }));
        Assert.That(rule.GetByDay()[0].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday)), Is.True);
        Assert.That(rule.GetByMonthDay(), Is.EqualTo(new int[] { -1 }));
        Assert.That(rule.GetByYearDay(), Is.EqualTo(new int[] { 100 }));
        Assert.That(rule.GetByWeekNo(), Is.EqualTo(new int[] { 20 }));
        Assert.That(rule.GetByMonth(), Is.EqualTo(new int[] { 6 }));
        Assert.That(rule.GetBySetPos(), Is.EqualTo(new int[] { -1 }));
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=YEARLY;BYSECOND=5;BYMINUTE=10;BYHOUR=15;BYDAY=MO;BYMONTHDAY=-1;BYYEARDAY=100;BYWEEKNO=20;BYMONTH=6;BYSETPOS=-1"));
    }

    [Test]
    public void Setters_PopulateBuiltRule()
    {
        Instant until = Instant.Parse("2024-12-31T00:00:00Z");
        RecurrenceRule rule = RecurrenceBuilder.Monthly()
            .SetInterval(2)
            .SetUntil(until)
            .SetWeekStart(TimeConstants.IsoSunday)
            .AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday))
            .AddByDay(RecurrenceDay.Create(TimeConstants.IsoFriday, -1))
            .AddByMonthDay(1)
            .AddByMonthDay(-1)
            .Build();

        Assert.That(rule.GetFrequency(), Is.EqualTo(RecurrenceFrequency.Monthly));
        Assert.That(rule.GetInterval(), Is.EqualTo(2));
        Assert.That(rule.GetUntil()!.EqualsValue(until), Is.True);
        Assert.That(rule.GetWeekStart(), Is.EqualTo(TimeConstants.IsoSunday));
        Assert.That(rule.GetByDay()[0].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday)), Is.True);
        Assert.That(rule.GetByDay()[1].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoFriday, -1)), Is.True);
        Assert.That(rule.GetByMonthDay(), Is.EqualTo(new int[] { 1, -1 }));
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;INTERVAL=2;UNTIL=20241231T000000Z;WKST=SU;BYDAY=MO,-1FR;BYMONTHDAY=1,-1"));
    }

    [Test]
    public void SetCountAndSetUntil_ClearEachOther()
    {
        Instant until = Instant.Parse("2024-12-31T00:00:00Z");
        RecurrenceRule countRule = RecurrenceBuilder.Daily().SetUntil(until).SetCount(5).Build();
        RecurrenceRule untilRule = RecurrenceBuilder.Daily().SetCount(5).SetUntil(until).Build();

        Assert.That(countRule.GetCount(), Is.EqualTo(5));
        Assert.That(countRule.GetUntil(), Is.Null);
        Assert.That(untilRule.GetCount(), Is.EqualTo(0));
        Assert.That(untilRule.GetUntil()!.EqualsValue(until), Is.True);
    }

    [Test]
    public void SetLocalUntil_ClearsCountAndUtcUntilAndCanBeReplaced()
    {
        Instant utcUntil = Instant.Parse("2024-12-31T00:00:00Z");
        LocalDateTime localUntil = LocalDateTime.Create(2024, 12, 31, 9, 30, 0);
        RecurrenceBuilder builder = TimeApi.Factory.CreateDailyRecurrenceBuilder();
        RecurrenceRule localRule = builder.SetCount(5).SetUntil(utcUntil).SetLocalUntil(localUntil).Build();
        RecurrenceRule utcRule = builder.SetUntil(utcUntil).Build();
        RecurrenceRule countRule = builder.SetLocalUntil(localUntil).SetCount(2).Build();

        Assert.That(localRule.GetCount(), Is.EqualTo(0));
        Assert.That(localRule.GetUntil(), Is.Null);
        Assert.That(localRule.GetLocalUntil()!.EqualsValue(localUntil), Is.True);
        Assert.That(utcRule.GetUntil()!.EqualsValue(utcUntil), Is.True);
        Assert.That(utcRule.GetLocalUntil(), Is.Null);
        Assert.That(countRule.GetCount(), Is.EqualTo(2));
        Assert.That(countRule.GetUntil(), Is.Null);
        Assert.That(countRule.GetLocalUntil(), Is.Null);
    }

    [Test]
    public void Build_AppliesRuleCompatibilityValidation()
    {
        ArgumentException monthDay = Assert.Throws<ArgumentException>(() => RecurrenceBuilder.Weekly().AddByMonthDay(1).Build())!;
        ArgumentException ordinalDay = Assert.Throws<ArgumentException>(() => RecurrenceBuilder.Daily().AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday, 1)).Build())!;

        Assert.That(monthDay.ParamName, Is.EqualTo("byMonthDay"));
        Assert.That(ordinalDay.ParamName, Is.EqualTo("byDay"));
    }

    [Test]
    public void Build_CreatesIndependentFilterSnapshots()
    {
        RecurrenceBuilder builder = RecurrenceBuilder.Yearly()
            .AddBySecond(1)
            .AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday));
        RecurrenceRule first = builder.Build();

        builder.AddBySecond(2).AddByDay(RecurrenceDay.Create(TimeConstants.IsoFriday));
        RecurrenceRule second = builder.Build();

        Assert.That(first.GetBySecond(), Is.EqualTo(new int[] { 1 }));
        Assert.That(first.GetByDay().Length, Is.EqualTo(1));
        Assert.That(second.GetBySecond(), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(second.GetByDay().Length, Is.EqualTo(2));
    }

    [Test]
    public void AddFilters_IgnoresDuplicateValuesAndPreservesInsertionOrder()
    {
        RecurrenceDay monday = RecurrenceDay.Create(TimeConstants.IsoMonday);
        RecurrenceBuilder builder = RecurrenceBuilder.Yearly()
            .AddBySecond(30).AddBySecond(10).AddBySecond(30)
            .AddByMinute(20).AddByMinute(20)
            .AddByHour(8).AddByHour(8)
            .AddByDay(monday).AddByDay(RecurrenceDay.Create(TimeConstants.IsoMonday))
            .AddByMonthDay(-1).AddByMonthDay(-1)
            .AddByYearDay(100).AddByYearDay(100)
            .AddByWeekNo(5).AddByWeekNo(5)
            .AddByMonth(6).AddByMonth(6)
            .AddBySetPos(1).AddBySetPos(1);
        RecurrenceRule rule = builder.Build();

        Assert.That(rule.GetBySecond(), Is.EqualTo(new int[] { 30, 10 }));
        Assert.That(rule.GetByMinute(), Is.EqualTo(new int[] { 20 }));
        Assert.That(rule.GetByHour(), Is.EqualTo(new int[] { 8 }));
        Assert.That(rule.GetByDay().Length, Is.EqualTo(1));
        Assert.That(rule.GetByMonthDay(), Is.EqualTo(new int[] { -1 }));
        Assert.That(rule.GetByYearDay(), Is.EqualTo(new int[] { 100 }));
        Assert.That(rule.GetByWeekNo(), Is.EqualTo(new int[] { 5 }));
        Assert.That(rule.GetByMonth(), Is.EqualTo(new int[] { 6 }));
        Assert.That(rule.GetBySetPos(), Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void AddFilters_AcceptsSupportedRangeBoundaries()
    {
        RecurrenceRule rule = RecurrenceBuilder.Yearly()
            .AddBySecond(0).AddBySecond(59)
            .AddByMinute(0).AddByMinute(59)
            .AddByHour(0).AddByHour(23)
            .AddByMonthDay(-31).AddByMonthDay(31)
            .AddByYearDay(-366).AddByYearDay(366)
            .AddByWeekNo(-53).AddByWeekNo(53)
            .AddByMonth(1).AddByMonth(12)
            .AddBySetPos(-366).AddBySetPos(366)
            .Build();

        Assert.That(rule.GetBySecond(), Is.EqualTo(new int[] { 0, 59 }));
        Assert.That(rule.GetByMinute(), Is.EqualTo(new int[] { 0, 59 }));
        Assert.That(rule.GetByHour(), Is.EqualTo(new int[] { 0, 23 }));
        Assert.That(rule.GetByMonthDay(), Is.EqualTo(new int[] { -31, 31 }));
        Assert.That(rule.GetByYearDay(), Is.EqualTo(new int[] { -366, 366 }));
        Assert.That(rule.GetByWeekNo(), Is.EqualTo(new int[] { -53, 53 }));
        Assert.That(rule.GetByMonth(), Is.EqualTo(new int[] { 1, 12 }));
        Assert.That(rule.GetBySetPos(), Is.EqualTo(new int[] { -366, 366 }));
    }

    [Test]
    public void Build_RejectsSetPositionWithoutAnotherByFilter()
    {
        RecurrenceBuilder builder = RecurrenceBuilder.Monthly().AddBySetPos(-1);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void Setters_RejectInvalidValues()
    {
        RecurrenceBuilder builder = RecurrenceBuilder.Daily();

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetInterval(0))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCount(0))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => builder.SetUntil(null!))!.ParamName, Is.EqualTo("until"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => builder.SetLocalUntil(null!))!.ParamName, Is.EqualTo("until"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetWeekStart(0))!.ParamName, Is.EqualTo("weekStart"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => builder.AddByDay(null!))!.ParamName, Is.EqualTo("day"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBySecond(-1))!.ParamName, Is.EqualTo("second"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBySecond(60))!.ParamName, Is.EqualTo("second"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMinute(-1))!.ParamName, Is.EqualTo("minute"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMinute(60))!.ParamName, Is.EqualTo("minute"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByHour(-1))!.ParamName, Is.EqualTo("hour"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByHour(24))!.ParamName, Is.EqualTo("hour"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMonthDay(-32))!.ParamName, Is.EqualTo("monthDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMonthDay(0))!.ParamName, Is.EqualTo("monthDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMonthDay(32))!.ParamName, Is.EqualTo("monthDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByYearDay(-367))!.ParamName, Is.EqualTo("yearDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByYearDay(0))!.ParamName, Is.EqualTo("yearDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByYearDay(367))!.ParamName, Is.EqualTo("yearDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByWeekNo(-54))!.ParamName, Is.EqualTo("weekNumber"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByWeekNo(0))!.ParamName, Is.EqualTo("weekNumber"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByWeekNo(54))!.ParamName, Is.EqualTo("weekNumber"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMonth(0))!.ParamName, Is.EqualTo("month"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddByMonth(13))!.ParamName, Is.EqualTo("month"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBySetPos(-367))!.ParamName, Is.EqualTo("position"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBySetPos(0))!.ParamName, Is.EqualTo("position"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBySetPos(367))!.ParamName, Is.EqualTo("position"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new RecurrenceBuilder((RecurrenceFrequency)0))!.ParamName, Is.EqualTo("frequency"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new RecurrenceBuilder((RecurrenceFrequency)8))!.ParamName, Is.EqualTo("frequency"));
    }
}
