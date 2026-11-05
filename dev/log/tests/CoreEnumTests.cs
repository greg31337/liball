using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class CoreEnumTests
{
    [Test]
    public void LogLevel_OrdersBySeverityAndPlacesOffLast()
    {
        Assert.That((int)LogLevel.Trace, Is.EqualTo(0));
        Assert.That((int)LogLevel.Debug, Is.EqualTo(1));
        Assert.That((int)LogLevel.Info, Is.EqualTo(2));
        Assert.That((int)LogLevel.Warn, Is.EqualTo(3));
        Assert.That((int)LogLevel.Error, Is.EqualTo(4));
        Assert.That((int)LogLevel.Fatal, Is.EqualTo(5));
        Assert.That((int)LogLevel.Off, Is.EqualTo(6));
    }

    [Test]
    public void LogValueKind_CoversNormalizedValueCategories()
    {
        LogValueKind[] expectedKinds = new LogValueKind[]
        {
            LogValueKind.Null,
            LogValueKind.String,
            LogValueKind.Bool,
            LogValueKind.Integer,
            LogValueKind.Decimal,
            LogValueKind.FloatingPoint,
            LogValueKind.DateTime,
            LogValueKind.Duration,
            LogValueKind.Exception,
            LogValueKind.ObjectText,
            LogValueKind.Array,
            LogValueKind.Dictionary,
            LogValueKind.Raw
        };

        Assert.That(Enum.GetValues<LogValueKind>(), Is.EqualTo(expectedKinds));
    }

    [Test]
    public void LogDecision_RepresentsFilterOutcomes()
    {
        Assert.That(LogDecision.Neutral.ToString(), Is.EqualTo("Neutral"));
        Assert.That(LogDecision.Include.ToString(), Is.EqualTo("Include"));
        Assert.That(LogDecision.Exclude.ToString(), Is.EqualTo("Exclude"));
    }

    [Test]
    public void LogBackpressureMode_RepresentsQueuePressureStrategies()
    {
        LogBackpressureMode[] expectedModes = new LogBackpressureMode[]
        {
            LogBackpressureMode.Block,
            LogBackpressureMode.DropNewest,
            LogBackpressureMode.DropOldest
        };

        Assert.That(Enum.GetValues<LogBackpressureMode>(), Is.EqualTo(expectedModes));
    }

    [Test]
    public void LogRollingInterval_RepresentsBuiltInFileRollBoundaries()
    {
        LogRollingInterval[] expectedIntervals = new LogRollingInterval[]
        {
            LogRollingInterval.None,
            LogRollingInterval.Hour,
            LogRollingInterval.Day,
            LogRollingInterval.Month,
            LogRollingInterval.Year
        };

        Assert.That(Enum.GetValues<LogRollingInterval>(), Is.EqualTo(expectedIntervals));
    }
}
