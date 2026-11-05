using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogLevelSwitchTests
{
    [Test]
    public void DefaultConstructor_UsesInfoMinimumLevel()
    {
        LogLevelSwitch levelSwitch = new LogLevelSwitch();

        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Info));
    }

    [Test]
    public void CreateAndSetLevel_NormalizeAndStoreMinimumLevel()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Warn);

        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Warn));

        levelSwitch.SetLevel(LogLevel.Debug);

        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Debug));
    }

    [Test]
    public void IsEnabled_UsesCurrentMinimumLevel()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Warn);

        Assert.That(levelSwitch.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Error), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Fatal), Is.True);
    }

    [Test]
    public void IsEnabled_TreatsOffAsDisablingAllLevels()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Off);

        Assert.That(levelSwitch.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Off), Is.False);
    }

    [Test]
    public void NormalizeLevel_ClampsUndefinedEnumValues()
    {
        Assert.That(LogLevelSwitch.NormalizeLevel((LogLevel)(-1)), Is.EqualTo(LogLevel.Trace));
        Assert.That(LogLevelSwitch.NormalizeLevel((LogLevel)99), Is.EqualTo(LogLevel.Off));
    }

    [Test]
    public void SetLevel_UpdatesRuntimeDecision()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Error);

        Assert.That(levelSwitch.IsEnabled(LogLevel.Warn), Is.False);

        levelSwitch.SetLevel(LogLevel.Debug);

        Assert.That(levelSwitch.IsEnabled(LogLevel.Warn), Is.True);
    }
}
