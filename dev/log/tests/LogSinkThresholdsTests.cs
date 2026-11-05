using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogSinkThresholdsTests
{
    [Test]
    public void DefaultThreshold_AllowsAllConcreteLevels()
    {
        LogSinkThresholds thresholds = new LogSinkThresholds();

        Assert.That(thresholds.IsEnabled(LogLevel.Trace), Is.True);
        Assert.That(thresholds.IsEnabled(LogLevel.Debug), Is.True);
        Assert.That(thresholds.IsEnabled(LogLevel.Fatal), Is.True);
        Assert.That(thresholds.IsEnabled(LogLevel.Off), Is.False);
    }

    [Test]
    public void SetMinimumLevel_FiltersBelowThreshold()
    {
        LogSinkThresholds thresholds = new LogSinkThresholds();

        thresholds.SetMinimumLevel(LogLevel.Warn);

        Assert.That(thresholds.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(thresholds.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(thresholds.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void SetMinimumLevel_OffDisablesAllLevels()
    {
        LogSinkThresholds thresholds = new LogSinkThresholds();

        thresholds.SetMinimumLevel(LogLevel.Off);

        Assert.That(thresholds.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(thresholds.IsEnabled(LogLevel.Off), Is.False);
    }

    [Test]
    public void ClearMinimumLevel_RemovesThreshold()
    {
        LogSinkThresholds thresholds = new LogSinkThresholds();

        thresholds.SetMinimumLevel(LogLevel.Error);
        thresholds.ClearMinimumLevel();

        Assert.That(thresholds.IsEnabled(LogLevel.Debug), Is.True);
        Assert.That(thresholds.HasMinimumLevel, Is.False);
    }

    [Test]
    public void IsLevelEnabled_NormalizesUndefinedLevels()
    {
        Assert.That(LogSinkThresholds.IsLevelEnabled((LogLevel)(-1), LogLevel.Debug), Is.False);
        Assert.That(LogSinkThresholds.IsLevelEnabled(LogLevel.Fatal, (LogLevel)99), Is.False);
    }
}
