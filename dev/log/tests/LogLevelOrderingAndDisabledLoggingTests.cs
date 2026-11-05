using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogLevelOrderingAndDisabledLoggingTests
{
    [Test]
    public void LevelSwitch_UsesSeverityOrderingForEveryMinimumLevel()
    {
        LogLevel[] minimumLevels = AllLevels();
        LogLevel[] eventLevels = AllLevels();

        foreach (LogLevel minimumLevel in minimumLevels)
        {
            LogLevelSwitch levelSwitch = LogLevelSwitch.Create(minimumLevel);
            foreach (LogLevel eventLevel in eventLevels)
            {
                Assert.That(levelSwitch.IsEnabled(eventLevel), Is.EqualTo(ExpectedEnabled(minimumLevel, eventLevel)), minimumLevel.ToString() + " should decide " + eventLevel.ToString());
            }
        }
    }

    [Test]
    public void Logger_WritesOnlyLevelsEnabledByMinimumLevel()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Warn);
        logger.AddSink(sink);

        LogResult trace = logger.Write(LogLevel.Trace, "trace");
        LogResult debug = logger.Write(LogLevel.Debug, "debug");
        LogResult info = logger.Write(LogLevel.Info, "info");
        LogResult warn = logger.Write(LogLevel.Warn, "warn");
        LogResult error = logger.Write(LogLevel.Error, "error");
        LogResult fatal = logger.Write(LogLevel.Fatal, "fatal");
        LogResult off = logger.Write(LogLevel.Off, "off");

        Assert.That(trace.IsDropped, Is.True);
        Assert.That(debug.IsDropped, Is.True);
        Assert.That(info.IsDropped, Is.True);
        Assert.That(warn.IsSuccess, Is.True);
        Assert.That(error.IsSuccess, Is.True);
        Assert.That(fatal.IsSuccess, Is.True);
        Assert.That(off.IsDropped, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(3));
        Assert.That(MemoryLogSink.Events(sink)[0].Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(MemoryLogSink.Events(sink)[1].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(MemoryLogSink.Events(sink)[2].Level, Is.EqualTo(LogLevel.Fatal));
    }

    [Test]
    public void DisabledByLevelSwitch_DoesNotReplaceLastEventOrReachSink()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Trace);
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithSwitch(levelSwitch);
        logger.AddSink(sink);
        logger.Info("first");
        LogEvent firstEvent = logger.LastEvent!;

        levelSwitch.SetLevel(LogLevel.Error);
        LogResult result = logger.Info("second");

        Assert.That(result.IsDropped, Is.True);
        Assert.That(result.Message, Is.EqualTo("log event disabled"));
        Assert.That(logger.LastEvent, Is.SameAs(firstEvent));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(logger.DroppedCount, Is.EqualTo(1));
    }

    [Test]
    public void DisabledBySinkThreshold_DoesNotReplaceLastEventOrReachSink()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);
        logger.Info("first");
        LogEvent firstEvent = logger.LastEvent!;

        logger.SetSinkMinimumLevel(LogLevel.Fatal);
        LogResult result = logger.Error("second");

        Assert.That(result.IsDropped, Is.True);
        Assert.That(logger.LastEvent, Is.SameAs(firstEvent));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.True);
    }

    [Test]
    public void OffLevel_DisablesLoggerAndSinkThresholds()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);

        LogResult offEvent = logger.Write(LogLevel.Off, "off");
        logger.SetSinkMinimumLevel(LogLevel.Off);
        LogResult fatal = logger.Fatal("fatal");

        Assert.That(offEvent.IsDropped, Is.True);
        Assert.That(fatal.IsDropped, Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(0));
    }

    [Test]
    public void CategoryOff_DisablesOnlyMatchingChildLoggers()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        root.AddSink(sink);
        root.SetCategoryLevel("Application.Noisy", LogLevel.Off);

        LogResult disabled = root.For("Application.Noisy.Worker").Fatal("fatal");
        LogResult enabled = root.For("Application.Quiet.Worker").Info("info");

        Assert.That(disabled.IsDropped, Is.True);
        Assert.That(enabled.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0].LoggerName, Is.EqualTo("Application.Quiet.Worker"));
    }

    static LogLevel[] AllLevels()
    {
        return new LogLevel[]
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal,
            LogLevel.Off
        };
    }

    static bool ExpectedEnabled(LogLevel minimumLevel, LogLevel eventLevel)
    {
        if (minimumLevel == LogLevel.Off)
        {
            return false;
        }

        if (eventLevel == LogLevel.Off)
        {
            return false;
        }

        return (int)eventLevel >= (int)minimumLevel;
    }
}
