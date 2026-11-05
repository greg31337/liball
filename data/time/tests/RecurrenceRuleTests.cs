using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceRuleTests
{
    [Test]
    public void Create_StoresFrequencyAndDefaults()
    {
        RecurrenceRule rule = RecurrenceRule.Create(RecurrenceFrequency.Daily);

        Assert.That(rule.GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.That(rule.GetInterval(), Is.EqualTo(1));
        Assert.That(rule.GetCount(), Is.EqualTo(0));
        Assert.That(rule.GetUntil(), Is.Null);
        Assert.That(rule.GetLocalUntil(), Is.Null);
        Assert.That(rule.GetWeekStart(), Is.EqualTo(TimeConstants.IsoMonday));
        Assert.That(rule.GetBySecond().Length, Is.EqualTo(0));
        Assert.That(rule.GetByMinute().Length, Is.EqualTo(0));
        Assert.That(rule.GetByHour().Length, Is.EqualTo(0));
        Assert.That(rule.GetByDay().Length, Is.EqualTo(0));
        Assert.That(rule.GetByMonthDay().Length, Is.EqualTo(0));
        Assert.That(rule.GetByYearDay().Length, Is.EqualTo(0));
        Assert.That(rule.GetByWeekNo().Length, Is.EqualTo(0));
        Assert.That(rule.GetByMonth().Length, Is.EqualTo(0));
        Assert.That(rule.GetBySetPos().Length, Is.EqualTo(0));
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=DAILY"));
    }

    [Test]
    public void Constructor_StoresValuesAndCopiesArrays()
    {
        int[] bySecond = new int[] { 1, 2 };
        int[] byMinute = new int[] { 3, 4 };
        int[] byHour = new int[] { 5, 6 };
        RecurrenceDay[] byDay = new RecurrenceDay[] { RecurrenceDay.Create(TimeConstants.IsoMonday), RecurrenceDay.Create(TimeConstants.IsoFriday) };
        int[] byMonthDay = new int[] { 1, -1 };
        int[] byYearDay = new int[] { 100, -100 };
        int[] byWeekNo = new int[] { 1, -1 };
        int[] byMonth = new int[] { 1, 12 };
        int[] bySetPos = new int[] { 1, -1 };
        Instant until = Instant.Parse("2024-12-31T00:00:00Z");
        RecurrenceRule rule = new RecurrenceRule(
            RecurrenceFrequency.Yearly,
            2,
            0,
            until,
            null,
            TimeConstants.IsoSunday,
            bySecond,
            byMinute,
            byHour,
            byDay,
            byMonthDay,
            byYearDay,
            byWeekNo,
            byMonth,
            bySetPos);

        bySecond[0] = 59;
        byDay[0] = RecurrenceDay.Create(TimeConstants.IsoTuesday);
        int[] returnedSeconds = rule.GetBySecond();
        returnedSeconds[1] = 59;
        RecurrenceDay[] returnedDays = rule.GetByDay();
        returnedDays[1] = RecurrenceDay.Create(TimeConstants.IsoWednesday);

        Assert.That(rule.GetFrequency(), Is.EqualTo(RecurrenceFrequency.Yearly));
        Assert.That(rule.GetInterval(), Is.EqualTo(2));
        Assert.That(rule.GetCount(), Is.EqualTo(0));
        Assert.That(rule.GetUntil()!.ToIsoString(), Is.EqualTo("2024-12-31T00:00:00Z"));
        Assert.That(rule.GetWeekStart(), Is.EqualTo(TimeConstants.IsoSunday));
        Assert.That(rule.GetBySecond()[0], Is.EqualTo(1));
        Assert.That(rule.GetBySecond()[1], Is.EqualTo(2));
        Assert.That(rule.GetByMinute()[0], Is.EqualTo(3));
        Assert.That(rule.GetByHour()[1], Is.EqualTo(6));
        Assert.That(rule.GetByDay()[0].GetDayOfWeek(), Is.EqualTo(TimeConstants.IsoMonday));
        Assert.That(rule.GetByDay()[1].GetOrdinal(), Is.EqualTo(0));
        Assert.That(rule.GetByMonthDay()[1], Is.EqualTo(-1));
        Assert.That(rule.GetByYearDay()[0], Is.EqualTo(100));
        Assert.That(rule.GetByWeekNo()[1], Is.EqualTo(-1));
        Assert.That(rule.GetByMonth()[1], Is.EqualTo(12));
        Assert.That(rule.GetBySetPos()[1], Is.EqualTo(-1));
    }

    [Test]
    public void Constructor_RejectsInvalidScalarValues()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRule((RecurrenceFrequency)99, 1, 0, null, TimeConstants.IsoMonday))!.ParamName, Is.EqualTo("frequency"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRule(RecurrenceFrequency.Daily, 0, 0, null, TimeConstants.IsoMonday))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRule(RecurrenceFrequency.Daily, 1, -1, null, TimeConstants.IsoMonday))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRule(RecurrenceFrequency.Daily, 1, 0, null, 0))!.ParamName, Is.EqualTo("weekStart"));
        Assert.That(Assert.Throws<ArgumentException>(() => CreateRule(RecurrenceFrequency.Daily, 1, 1, Instant.UnixEpoch(), TimeConstants.IsoMonday))!.ParamName, Is.EqualTo("until"));
    }

    [Test]
    public void Constructor_RequiresArrayValues()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(null!, new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("bySecond"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], null!, new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byMinute"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], null!, new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byHour"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], null!, new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byDay"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], null!, new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byMonthDay"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], null!, new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byYearDay"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], null!, new int[0], new int[0]))!.ParamName, Is.EqualTo("byWeekNo"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], null!, new int[0]))!.ParamName, Is.EqualTo("byMonth"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], null!))!.ParamName, Is.EqualTo("bySetPos"));
    }

    [Test]
    public void Constructor_RejectsInvalidListValues()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[] { 60 }, new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("bySecond"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[] { 60 }, new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byMinute"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[] { 24 }, new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byHour"));
        Assert.That(Assert.Throws<ArgumentException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[] { null! }, new int[0], new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[] { 0 }, new int[0], new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byMonthDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[] { 367 }, new int[0], new int[0], new int[0]))!.ParamName, Is.EqualTo("byYearDay"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[] { 0 }, new int[0], new int[0]))!.ParamName, Is.EqualTo("byWeekNo"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[] { 13 }, new int[0]))!.ParamName, Is.EqualTo("byMonth"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuleWithArrays(new int[0], new int[0], new int[0], new RecurrenceDay[0], new int[0], new int[0], new int[0], new int[0], new int[] { 0 }))!.ParamName, Is.EqualTo("bySetPos"));
    }

    [Test]
    public void Parse_ReadsSupportedRRuleSubset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;INTERVAL=2;COUNT=5;WKST=SU;BYSECOND=1,2;BYMINUTE=3,4;BYHOUR=5,6;BYDAY=MO,FR;BYMONTHDAY=1,-1;BYYEARDAY=100,-100;BYWEEKNO=1,-1;BYMONTH=1,12;BYSETPOS=1,-1");

        Assert.That(rule.GetFrequency(), Is.EqualTo(RecurrenceFrequency.Yearly));
        Assert.That(rule.GetInterval(), Is.EqualTo(2));
        Assert.That(rule.GetCount(), Is.EqualTo(5));
        Assert.That(rule.GetWeekStart(), Is.EqualTo(TimeConstants.IsoSunday));
        Assert.That(rule.GetBySecond(), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(rule.GetByMinute(), Is.EqualTo(new int[] { 3, 4 }));
        Assert.That(rule.GetByHour(), Is.EqualTo(new int[] { 5, 6 }));
        Assert.That(rule.GetByDay()[0].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday)), Is.True);
        Assert.That(rule.GetByDay()[1].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoFriday)), Is.True);
        Assert.That(rule.GetByMonthDay(), Is.EqualTo(new int[] { 1, -1 }));
        Assert.That(rule.GetByYearDay(), Is.EqualTo(new int[] { 100, -100 }));
        Assert.That(rule.GetByWeekNo(), Is.EqualTo(new int[] { 1, -1 }));
        Assert.That(rule.GetByMonth(), Is.EqualTo(new int[] { 1, 12 }));
        Assert.That(rule.GetBySetPos(), Is.EqualTo(new int[] { 1, -1 }));
    }

    [Test]
    public void Parse_ReadsPrefixAndUntil()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:FREQ=DAILY;UNTIL=20240131T120000Z");

        Assert.That(rule.GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.That(rule.GetUntil()!.ToIsoString(), Is.EqualTo("2024-01-31T12:00:00Z"));
        Assert.That(rule.GetLocalUntil(), Is.Null);
    }

    [Test]
    public void ParseAndFormat_RepresentLocalAndUtcUntilSeparately()
    {
        RecurrenceRule local = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240131T120000");
        RecurrenceRule utc = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240131T120000Z");

        Assert.That(local.GetLocalUntil()!.ToIsoString(), Is.EqualTo("2024-01-31T12:00:00"));
        Assert.That(local.GetUntil(), Is.Null);
        Assert.That(local.ToRRuleString(), Is.EqualTo("FREQ=DAILY;UNTIL=20240131T120000"));
        Assert.That(utc.GetUntil()!.ToIsoString(), Is.EqualTo("2024-01-31T12:00:00Z"));
        Assert.That(utc.GetLocalUntil(), Is.Null);
        Assert.That(utc.ToRRuleString(), Is.EqualTo("FREQ=DAILY;UNTIL=20240131T120000Z"));
    }

    [Test]
    public void Parse_RejectsInvalidRules()
    {
        Assert.Throws<ArgumentNullException>(() => RecurrenceRule.Parse(null!));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse(""));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse("INTERVAL=2"));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse("FREQ=BAD"));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse("FREQ=DAILY;BAD=1"));
        Assert.Throws<ArgumentException>(() => RecurrenceRule.Parse("FREQ=DAILY;COUNT=1;UNTIL=20240131T120000Z"));
        Assert.Throws<ArgumentException>(() => RecurrenceRule.Parse("FREQ=DAILY;COUNT=1;UNTIL=20240131T120000"));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceRule.Parse("FREQ=DAILY;BYMONTH=13"));
    }

    [Test]
    public void Constructor_RejectsBothUntilRepresentationsAndCountWithLocalUntil()
    {
        Instant utcUntil = Instant.Parse("2024-01-31T12:00:00Z");
        LocalDateTime localUntil = LocalDateTime.Create(2024, 1, 31, 12, 0, 0);

        ArgumentException both = Assert.Throws<ArgumentException>(() => CreateRuleWithUntilValues(0, utcUntil, localUntil))!;
        ArgumentException countAndLocal = Assert.Throws<ArgumentException>(() => CreateRuleWithUntilValues(1, null, localUntil))!;

        Assert.That(both.ParamName, Is.EqualTo("localUntil"));
        Assert.That(countAndLocal.ParamName, Is.EqualTo("until"));
    }

    [Test]
    public void Constructor_RejectsFractionalUntilValuesThatCannotRoundTrip()
    {
        Instant utcUntil = Instant.Parse("2024-01-31T12:00:00.1Z");
        LocalDateTime localUntil = LocalDateTime.Parse("2024-01-31T12:00:00.1");

        ArgumentException utc = Assert.Throws<ArgumentException>(() => CreateRuleWithUntilValues(0, utcUntil, null))!;
        ArgumentException local = Assert.Throws<ArgumentException>(() => CreateRuleWithUntilValues(0, null, localUntil))!;

        Assert.That(utc.ParamName, Is.EqualTo("until"));
        Assert.That(local.ParamName, Is.EqualTo("localUntil"));
    }

    [TestCase("FREQ=SECONDLY;BYMONTH=1;BYYEARDAY=1;BYMONTHDAY=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=MINUTELY;BYMONTH=1;BYYEARDAY=1;BYMONTHDAY=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=HOURLY;BYMONTH=1;BYYEARDAY=1;BYMONTHDAY=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=DAILY;BYMONTH=1;BYMONTHDAY=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=WEEKLY;BYMONTH=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=MONTHLY;BYMONTH=1;BYMONTHDAY=1;BYDAY=1MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    [TestCase("FREQ=YEARLY;BYMONTH=1;BYWEEKNO=1;BYYEARDAY=1;BYMONTHDAY=1;BYDAY=MO;BYHOUR=1;BYMINUTE=1;BYSECOND=1;BYSETPOS=1")]
    public void Parse_AcceptsEveryFrequencyWithItsCompatibleFilters(string text)
    {
        RecurrenceRule rule = TimeApi.Factory.ParseRecurrenceRule(text);

        Assert.That(rule.GetBySetPos(), Is.EqualTo(new int[] { 1 }));
    }

    [TestCase("FREQ=SECONDLY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=MINUTELY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=HOURLY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=DAILY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=WEEKLY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=MONTHLY;BYWEEKNO=1", "byWeekNo")]
    [TestCase("FREQ=DAILY;BYYEARDAY=1", "byYearDay")]
    [TestCase("FREQ=WEEKLY;BYYEARDAY=1", "byYearDay")]
    [TestCase("FREQ=MONTHLY;BYYEARDAY=1", "byYearDay")]
    [TestCase("FREQ=WEEKLY;BYMONTHDAY=1", "byMonthDay")]
    [TestCase("FREQ=SECONDLY;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=MINUTELY;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=HOURLY;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=DAILY;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=WEEKLY;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=YEARLY;BYWEEKNO=1;BYDAY=1MO", "byDay")]
    [TestCase("FREQ=MONTHLY;BYSETPOS=1", "bySetPos")]
    public void Parse_RejectsEveryIncompatibleFrequencyFilterCombination(string text, string expectedParamName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TimeApi.Factory.ParseRecurrenceRule(text))!;

        Assert.That(exception.ParamName, Is.EqualTo(expectedParamName));
    }

    [TestCase("FREQ=DAILY;BYHOUR=9,,17")]
    [TestCase("FREQ=DAILY;BYSECOND=1=2")]
    [TestCase("FREQ=MONTHLY;BYDAY=+MO")]
    public void Parse_RejectsMalformedCompositeParts(string text)
    {
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse(text));
    }

    [TestCase("FREQ=DAILY;FREQ=WEEKLY")]
    [TestCase("FREQ=DAILY;INTERVAL=1;INTERVAL=2")]
    [TestCase("FREQ=DAILY;COUNT=1;COUNT=2")]
    [TestCase("FREQ=DAILY;UNTIL=20240131T120000;UNTIL=20240201T120000")]
    [TestCase("FREQ=WEEKLY;WKST=MO;WKST=SU")]
    [TestCase("FREQ=DAILY;BYSECOND=1;BYSECOND=2")]
    [TestCase("FREQ=DAILY;BYMINUTE=1;BYMINUTE=2")]
    [TestCase("FREQ=DAILY;BYHOUR=1;BYHOUR=2")]
    [TestCase("FREQ=DAILY;BYDAY=MO;BYDAY=TU")]
    [TestCase("FREQ=MONTHLY;BYMONTHDAY=1;BYMONTHDAY=2")]
    [TestCase("FREQ=YEARLY;BYYEARDAY=1;BYYEARDAY=2")]
    [TestCase("FREQ=YEARLY;BYWEEKNO=1;BYWEEKNO=2")]
    [TestCase("FREQ=YEARLY;BYMONTH=1;BYMONTH=2")]
    [TestCase("FREQ=MONTHLY;BYDAY=MO;BYSETPOS=1;BYSETPOS=2")]
    public void Parse_RejectsDuplicateRuleParts(string text)
    {
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse(text));
    }

    [TestCase("DTSTART:20240131T120000")]
    [TestCase("RDATE:20240131T120000")]
    [TestCase("EXDATE:20240131T120000")]
    [TestCase("EXRULE:FREQ=DAILY")]
    [TestCase("RRULE;TZID=America/New_York:FREQ=DAILY")]
    [TestCase("RRULE;VALUE=RECUR:FREQ=DAILY")]
    [TestCase("RRULE:FREQ=DAILY\r\nRDATE:20240131T120000")]
    [TestCase("RRULE:RRULE:FREQ=DAILY")]
    public void Parse_RejectsUnsupportedICalendarPropertiesAndContentForms(string text)
    {
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse(text));
    }

    [TestCase("FREQ=DAILY;UNTIL=2024-01-31T12:00:00")]
    [TestCase("FREQ=DAILY;UNTIL=2024-01-31T12:00:00Z")]
    [TestCase("FREQ=DAILY;UNTIL=20240131")]
    [TestCase("FREQ=DAILY;UNTIL=20240131T120000.5")]
    [TestCase("FREQ=DAILY;UNTIL=20240131T120000.5Z")]
    [TestCase("FREQ=DAILY;UNTIL=20240131T120000z")]
    [TestCase("FREQ=DAILY;UNTIL=20240131T120000+0100")]
    [TestCase("FREQ=DAILY;COUNT=+1")]
    [TestCase("FREQ=DAILY;COUNT=0")]
    [TestCase("FREQ=DAILY;INTERVAL=-1")]
    [TestCase("FREQ=DAILY;INTERVAL=0")]
    [TestCase("FREQ=DAILY;COUNT=2147483648")]
    [TestCase("FREQ=YEARLY;BYMONTH=4294967297")]
    [TestCase("FREQ=MONTHLY;BYDAY=M")]
    [TestCase("FREQ=MONTHLY;BYDAY=XX")]
    public void Parse_RejectsUnsupportedValueForms(string text)
    {
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse(text));
    }

    [TestCase("FREQ=MONTHLY;BYDAY=54MO", "ordinal")]
    [TestCase("FREQ=YEARLY;BYWEEKNO=-54", "byWeekNo")]
    [TestCase("FREQ=MONTHLY;BYSETPOS=367", "bySetPos")]
    public void Parse_RejectsCompositeValuesOutsideSupportedRanges(string text, string expectedParamName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceRule.Parse(text))!;

        Assert.That(exception.ParamName, Is.EqualTo(expectedParamName));
    }

    [Test]
    public void ToRRuleString_FormatsCanonicalSubset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:FREQ=MONTHLY;INTERVAL=2;COUNT=5;WKST=SU;BYDAY=MO,-1FR;BYMONTHDAY=1,-1;BYMONTH=1,12");

        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;INTERVAL=2;COUNT=5;WKST=SU;BYDAY=MO,-1FR;BYMONTHDAY=1,-1;BYMONTH=1,12"));
    }

    [Test]
    public void ToRRuleString_FormatsUntilAsCompactUtc()
    {
        RecurrenceRule rule = new RecurrenceRule(
            RecurrenceFrequency.Daily,
            1,
            0,
            Instant.Parse("2024-01-31T12:00:00Z"),
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

        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=DAILY;UNTIL=20240131T120000Z"));
    }

    [TestCase("SECONDLY", RecurrenceFrequency.Secondly)]
    [TestCase("MINUTELY", RecurrenceFrequency.Minutely)]
    [TestCase("HOURLY", RecurrenceFrequency.Hourly)]
    [TestCase("DAILY", RecurrenceFrequency.Daily)]
    [TestCase("WEEKLY", RecurrenceFrequency.Weekly)]
    [TestCase("MONTHLY", RecurrenceFrequency.Monthly)]
    [TestCase("YEARLY", RecurrenceFrequency.Yearly)]
    public void ParseAndFormat_CoversEveryFrequency(string frequencyText, RecurrenceFrequency expectedFrequency)
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:FREQ=" + frequencyText);

        Assert.That(rule.GetFrequency(), Is.EqualTo(expectedFrequency));
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=" + frequencyText));
    }

    [Test]
    public void ParseAndFormat_CoversIntervalCountAndUntilParts()
    {
        RecurrenceRule interval = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=3");
        RecurrenceRule count = RecurrenceRule.Parse("FREQ=DAILY;COUNT=4");
        RecurrenceRule compactUntil = RecurrenceRule.Parse("FREQ=DAILY;UNTIL=20240131T120000Z");

        Assert.That(interval.GetInterval(), Is.EqualTo(3));
        Assert.That(interval.ToRRuleString(), Is.EqualTo("FREQ=DAILY;INTERVAL=3"));
        Assert.That(count.GetCount(), Is.EqualTo(4));
        Assert.That(count.ToRRuleString(), Is.EqualTo("FREQ=DAILY;COUNT=4"));
        Assert.That(compactUntil.GetUntil()!.ToIsoString(), Is.EqualTo("2024-01-31T12:00:00Z"));
        Assert.That(compactUntil.ToRRuleString(), Is.EqualTo("FREQ=DAILY;UNTIL=20240131T120000Z"));
    }

    [Test]
    public void ParseAndFormat_PreservesEverySupportedRulePart()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:BYSETPOS=-1,1;BYMONTH=1,12;BYWEEKNO=1,-1;BYYEARDAY=1,-1;BYMONTHDAY=1,-1;BYDAY=MO,FR;BYHOUR=0,23;BYMINUTE=0,59;BYSECOND=0,59;WKST=SU;UNTIL=20241231T235959Z;INTERVAL=2;FREQ=YEARLY");

        string formatted = rule.ToRRuleString();
        RecurrenceRule reparsed = RecurrenceRule.Parse(formatted);

        Assert.That(formatted, Is.EqualTo("FREQ=YEARLY;INTERVAL=2;UNTIL=20241231T235959Z;WKST=SU;BYSECOND=0,59;BYMINUTE=0,59;BYHOUR=0,23;BYDAY=MO,FR;BYMONTHDAY=1,-1;BYYEARDAY=1,-1;BYWEEKNO=1,-1;BYMONTH=1,12;BYSETPOS=-1,1"));
        Assert.That(reparsed.ToRRuleString(), Is.EqualTo(formatted));
    }

    [Test]
    public void ParseAndFormat_CanonicalizesRfcSignedIntegerForms()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=+1MO,-1FR;BYMONTHDAY=+1,-1;BYSETPOS=+1,-1");

        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;BYDAY=1MO,-1FR;BYMONTHDAY=1,-1;BYSETPOS=1,-1"));
    }

    [TestCase("FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30")]
    [TestCase("FREQ=DAILY;COUNT=10")]
    [TestCase("FREQ=DAILY;UNTIL=19971224T000000Z")]
    [TestCase("FREQ=DAILY;INTERVAL=10;COUNT=5")]
    [TestCase("FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA")]
    [TestCase("FREQ=WEEKLY;INTERVAL=2;UNTIL=19971224T000000Z;WKST=SU;BYDAY=MO,WE,FR")]
    [TestCase("FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU")]
    [TestCase("FREQ=MONTHLY;COUNT=6;BYDAY=-2MO")]
    [TestCase("FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1")]
    [TestCase("FREQ=MONTHLY;INTERVAL=18;COUNT=10;BYMONTHDAY=10,11,12,13,14,15")]
    [TestCase("FREQ=YEARLY;INTERVAL=3;COUNT=10;BYYEARDAY=1,100,200")]
    [TestCase("FREQ=YEARLY;BYDAY=20MO")]
    [TestCase("FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO")]
    [TestCase("FREQ=YEARLY;BYMONTH=3;BYDAY=TH")]
    [TestCase("FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13")]
    [TestCase("FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8")]
    [TestCase("FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3")]
    [TestCase("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2")]
    [TestCase("FREQ=HOURLY;INTERVAL=3;UNTIL=19970902T170000Z")]
    [TestCase("FREQ=MINUTELY;INTERVAL=15;COUNT=6")]
    [TestCase("FREQ=MINUTELY;INTERVAL=90;COUNT=4")]
    [TestCase("FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40")]
    [TestCase("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO")]
    [TestCase("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU")]
    public void ParseAndFormat_CoversRfc5545NormativeAndIllustrativeRules(string text)
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:" + text);
        string formatted = rule.ToRRuleString();
        RecurrenceRule reparsed = RecurrenceRule.Parse(formatted);

        Assert.That(reparsed.ToRRuleString(), Is.EqualTo(formatted));
    }

    [TestCase("MO", TimeConstants.IsoMonday, "FREQ=WEEKLY")]
    [TestCase("TU", TimeConstants.IsoTuesday, "FREQ=WEEKLY;WKST=TU")]
    [TestCase("WE", TimeConstants.IsoWednesday, "FREQ=WEEKLY;WKST=WE")]
    [TestCase("TH", TimeConstants.IsoThursday, "FREQ=WEEKLY;WKST=TH")]
    [TestCase("FR", TimeConstants.IsoFriday, "FREQ=WEEKLY;WKST=FR")]
    [TestCase("SA", TimeConstants.IsoSaturday, "FREQ=WEEKLY;WKST=SA")]
    [TestCase("SU", TimeConstants.IsoSunday, "FREQ=WEEKLY;WKST=SU")]
    public void ParseAndFormat_CoversEveryWeekStart(string weekStartText, int expectedWeekStart, string expectedRRule)
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;WKST=" + weekStartText);

        Assert.That(rule.GetWeekStart(), Is.EqualTo(expectedWeekStart));
        Assert.That(rule.ToRRuleString(), Is.EqualTo(expectedRRule));
    }

    [Test]
    public void ParseAndFormat_CoversEveryIntegerByPart()
    {
        RecurrenceRule bySecond = RecurrenceRule.Parse("FREQ=DAILY;BYSECOND=0,30,59");
        RecurrenceRule byMinute = RecurrenceRule.Parse("FREQ=DAILY;BYMINUTE=0,15,59");
        RecurrenceRule byHour = RecurrenceRule.Parse("FREQ=DAILY;BYHOUR=0,12,23");
        RecurrenceRule byMonthDay = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1,15,-1");
        RecurrenceRule byYearDay = RecurrenceRule.Parse("FREQ=YEARLY;BYYEARDAY=1,60,-1");
        RecurrenceRule byWeekNo = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=1,20,-1");
        RecurrenceRule byMonth = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=1,6,12");
        RecurrenceRule bySetPos = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1;BYSETPOS=1,2,-1");

        Assert.That(bySecond.GetBySecond(), Is.EqualTo(new int[] { 0, 30, 59 }));
        Assert.That(bySecond.ToRRuleString(), Is.EqualTo("FREQ=DAILY;BYSECOND=0,30,59"));
        Assert.That(byMinute.GetByMinute(), Is.EqualTo(new int[] { 0, 15, 59 }));
        Assert.That(byMinute.ToRRuleString(), Is.EqualTo("FREQ=DAILY;BYMINUTE=0,15,59"));
        Assert.That(byHour.GetByHour(), Is.EqualTo(new int[] { 0, 12, 23 }));
        Assert.That(byHour.ToRRuleString(), Is.EqualTo("FREQ=DAILY;BYHOUR=0,12,23"));
        Assert.That(byMonthDay.GetByMonthDay(), Is.EqualTo(new int[] { 1, 15, -1 }));
        Assert.That(byMonthDay.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;BYMONTHDAY=1,15,-1"));
        Assert.That(byYearDay.GetByYearDay(), Is.EqualTo(new int[] { 1, 60, -1 }));
        Assert.That(byYearDay.ToRRuleString(), Is.EqualTo("FREQ=YEARLY;BYYEARDAY=1,60,-1"));
        Assert.That(byWeekNo.GetByWeekNo(), Is.EqualTo(new int[] { 1, 20, -1 }));
        Assert.That(byWeekNo.ToRRuleString(), Is.EqualTo("FREQ=YEARLY;BYWEEKNO=1,20,-1"));
        Assert.That(byMonth.GetByMonth(), Is.EqualTo(new int[] { 1, 6, 12 }));
        Assert.That(byMonth.ToRRuleString(), Is.EqualTo("FREQ=YEARLY;BYMONTH=1,6,12"));
        Assert.That(bySetPos.GetBySetPos(), Is.EqualTo(new int[] { 1, 2, -1 }));
        Assert.That(bySetPos.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;BYMONTHDAY=1;BYSETPOS=1,2,-1"));
    }

    [Test]
    public void ParseAndFormat_CoversByDayWeekdaysAndOrdinals()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,1WE,-1FR,53SU,-53SA");
        RecurrenceDay[] days = rule.GetByDay();

        Assert.That(days.Length, Is.EqualTo(6));
        Assert.That(days[0].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday)), Is.True);
        Assert.That(days[1].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoTuesday)), Is.True);
        Assert.That(days[2].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoWednesday, 1)), Is.True);
        Assert.That(days[3].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoFriday, -1)), Is.True);
        Assert.That(days[4].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoSunday, 53)), Is.True);
        Assert.That(days[5].EqualsValue(RecurrenceDay.Create(TimeConstants.IsoSaturday, -53)), Is.True);
        Assert.That(rule.ToRRuleString(), Is.EqualTo("FREQ=MONTHLY;BYDAY=MO,TU,1WE,-1FR,53SU,-53SA"));
    }

    static RecurrenceRule CreateRule(RecurrenceFrequency frequency, int interval, int count, Instant? until, int weekStart)
    {
        return new RecurrenceRule(
            frequency,
            interval,
            count,
            until,
            null,
            weekStart,
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

    static RecurrenceRule CreateRuleWithArrays(
        int[] bySecond,
        int[] byMinute,
        int[] byHour,
        RecurrenceDay[] byDay,
        int[] byMonthDay,
        int[] byYearDay,
        int[] byWeekNo,
        int[] byMonth,
        int[] bySetPos)
    {
        return new RecurrenceRule(
            RecurrenceFrequency.Daily,
            1,
            0,
            null,
            null,
            TimeConstants.IsoMonday,
            bySecond,
            byMinute,
            byHour,
            byDay,
            byMonthDay,
            byYearDay,
            byWeekNo,
            byMonth,
            bySetPos);
    }

    static RecurrenceRule CreateRuleWithUntilValues(int count, Instant? until, LocalDateTime? localUntil)
    {
        return new RecurrenceRule(
            RecurrenceFrequency.Daily,
            1,
            count,
            until,
            localUntil,
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
