using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFilterTests
{
    [Test]
    public void MinimumLevel_IncludesAtAndAboveThreshold()
    {
        LogFilter filter = LogFilter.MinimumLevel(LogLevel.Warn);
        LogEvent warnEvent = LogEvent.Create(LogLevel.Warn, "Application", "warn", "warn", new LogField[0]);
        LogEvent errorEvent = LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]);

        Assert.That(filter.Decide(warnEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.Decide(errorEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void MinimumLevel_ExcludesBelowThresholdAndOff()
    {
        LogFilter filter = LogFilter.MinimumLevel(LogLevel.Warn);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent offEvent = LogEvent.Create(LogLevel.Off, "Application", "off", "off", new LogField[0]);

        Assert.That(filter.Decide(infoEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(offEvent), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void MinimumLevel_OffExcludesAllEvents()
    {
        LogFilter filter = LogFilter.MinimumLevel(LogLevel.Off);
        LogEvent fatalEvent = LogEvent.Create(LogLevel.Fatal, "Application", "fatal", "fatal", new LogField[0]);

        Assert.That(filter.Decide(fatalEvent), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void Decide_ReturnsNeutralForUnknownFilterOrNullEvent()
    {
        LogFilter filter = new LogFilter();
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        Assert.That(filter.Decide(infoEvent), Is.EqualTo(LogDecision.Neutral));
        Assert.That(filter.Decide(null), Is.EqualTo(LogDecision.Neutral));
    }

    [Test]
    public void DecideAll_ExcludesWhenAnyFilterExcludes()
    {
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        LogDecision decision = LogFilter.DecideAll(
            new LogFilter[]
            {
                LogFilter.MinimumLevel(LogLevel.Trace),
                LogFilter.MinimumLevel(LogLevel.Error)
            },
            infoEvent);

        Assert.That(decision, Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void DecideAll_ReturnsIncludeWhenAnyFilterIncludesAndNoneExclude()
    {
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        LogDecision decision = LogFilter.DecideAll(
            new LogFilter[]
            {
                new LogFilter(),
                LogFilter.MinimumLevel(LogLevel.Debug)
            },
            infoEvent);

        Assert.That(decision, Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void CategoryPrefix_AppliesMinimumLevelToExactAndChildLoggerNames()
    {
        LogFilter filter = LogFilter.CategoryPrefix("Application", LogLevel.Error);
        LogEvent exactInfoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent childErrorEvent = LogEvent.Create(LogLevel.Error, "Application.Worker", "error", "error", new LogField[0]);

        Assert.That(filter.Decide(exactInfoEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(childErrorEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void CategoryPrefix_ReturnsNeutralForNonMatchingLoggerNames()
    {
        LogFilter filter = LogFilter.CategoryPrefix("App", LogLevel.Error);
        LogEvent applicationEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent otherEvent = LogEvent.Create(LogLevel.Info, "Other", "info", "info", new LogField[0]);

        Assert.That(filter.Decide(applicationEvent), Is.EqualTo(LogDecision.Neutral));
        Assert.That(filter.Decide(otherEvent), Is.EqualTo(LogDecision.Neutral));
    }

    [Test]
    public void CategoryPrefix_EmptyPrefixAppliesToAllLoggers()
    {
        LogFilter filter = LogFilter.CategoryPrefix("", LogLevel.Warn);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent warnEvent = LogEvent.Create(LogLevel.Warn, "Other", "warn", "warn", new LogField[0]);

        Assert.That(filter.Decide(infoEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(warnEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void CategoryMatchesPrefix_UsesDotBoundary()
    {
        Assert.That(LogFilter.CategoryMatchesPrefix("App", "App"), Is.True);
        Assert.That(LogFilter.CategoryMatchesPrefix("App.Worker", "App"), Is.True);
        Assert.That(LogFilter.CategoryMatchesPrefix("Application", "App"), Is.False);
    }

    [Test]
    public void FieldExists_IncludesWhenFieldIsPresent()
    {
        LogFilter filter = LogFilter.FieldExists("User");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "hello", "hello", new LogField[] { LogField.String("User", "Ada") });

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void FieldExists_ExcludesWhenFieldIsMissing()
    {
        LogFilter filter = LogFilter.FieldExists("User");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "hello", "hello", new LogField[0]);

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void FieldEquals_IncludesWhenFieldValueMatches()
    {
        LogFilter filter = LogFilter.FieldEquals("Count", 3);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "hello", "hello", new LogField[] { LogField.Int("Count", 3) });

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void FieldEquals_ExcludesWhenFieldValueDiffers()
    {
        LogFilter filter = LogFilter.FieldEquals("Count", 3);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "hello", "hello", new LogField[] { LogField.Int("Count", 4) });

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void FieldEquals_MatchesReservedFieldRenames()
    {
        LogFilter filter = LogFilter.FieldEquals("level", "custom");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "hello", "hello", new LogField[] { LogField.String("level", "custom") });

        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("field_level"));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void ValuesEqual_ComparesArraysAndDictionaries()
    {
        Dictionary<string, LogValue> leftDictionary = new Dictionary<string, LogValue>();
        Dictionary<string, LogValue> rightDictionary = new Dictionary<string, LogValue>();
        leftDictionary["Count"] = LogValue.Integer(3);
        rightDictionary["Count"] = LogValue.Integer(3);

        Assert.That(LogFilter.ValuesEqual(LogValue.Array(new LogValue[] { LogValue.String("a"), LogValue.Integer(1) }), LogValue.Array(new LogValue[] { LogValue.String("a"), LogValue.Integer(1) })), Is.True);
        Assert.That(LogFilter.ValuesEqual(LogValue.Dictionary(leftDictionary), LogValue.Dictionary(rightDictionary)), Is.True);
    }

    [Test]
    public void SampleEvery_IncludesEveryNthEvent()
    {
        LogFilter filter = LogFilter.SampleEvery(3);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.SeenCount, Is.EqualTo(4));
    }

    [Test]
    public void SampleEvery_NormalizesNonPositiveCountsToOne()
    {
        LogFilter filter = LogFilter.SampleEvery(0);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        Assert.That(filter.Count, Is.EqualTo(1));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
    }

    [Test]
    public void RateLimit_IncludesUntilLimitAndResetsAfterWindow()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 8, 2, 21, 0, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(start);
        LogFilter filter = LogFilter.RateLimitWithClock(2, TimeSpan.FromSeconds(10), clock);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));

        clock.SetFixedUtcNow(start.AddSeconds(11));

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.SeenCount, Is.EqualTo(1));
    }

    [Test]
    public void RateLimit_ZeroLimitExcludesAllEvents()
    {
        LogFilter filter = LogFilter.RateLimitWithClock(0, TimeSpan.FromSeconds(10), LogClock.Fixed(DateTimeOffset.UtcNow));
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
    }
}
