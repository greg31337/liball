using System.Text;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicSinkCoverageTests
{
    [Test]
    public void NullAndMemorySinks_ExposeStateLimitsFiltersThresholdsAndClear()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent info = logFactory.CreateEvent(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent errorWithoutTenant = logFactory.CreateEvent(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent errorWithTenant = logFactory.CreateEvent(LogLevel.Error, "Application", "present", "present", new LogField[]
        {
            factory.CreateStringField("TenantId", "tenant-1")
        });
        LogEvent second = logFactory.CreateEvent(LogLevel.Info, "Application", "second", "second", new LogField[0]);
        LogEvent third = logFactory.CreateEvent(LogLevel.Warn, "Application", "third", "third", new LogField[0]);
        LogSink nullSink = logFactory.CreateNullSink("discard");
        LogSink memorySink = logFactory.CreateMemorySink("recent", 2);
        ILogSinkState nullState = nullSink;
        ILogSinkState memoryState = memorySink;
        ILogSinkConfiguration configuration = memorySink;

        LogResult discarded = nullSink.Write(info);
        logFactory.SetSinkMinimumLevel(memorySink, LogLevel.Error);
        logFactory.AddSinkFilter(memorySink, new RequiredFieldFilter("TenantId"));
        LogResult belowThreshold = memorySink.Write(info);
        LogResult filtered = memorySink.Write(errorWithoutTenant);
        LogResult accepted = memorySink.Write(errorWithTenant);
        logFactory.ClearSinkMinimumLevel(memorySink);
        configuration.ClearFilters();
        memorySink.Write(second);
        memorySink.Write(third);
        LogEvent[] snapshot = logFactory.GetMemorySinkEvents(memorySink);
        snapshot[0] = info;

        Assert.That(discarded.IsSuccess, Is.True);
        Assert.That(nullSink.GetName(), Is.EqualTo("discard"));
        Assert.That(nullState.GetLastEvent(), Is.Null);
        Assert.That(nullState.GetWrittenCount(), Is.EqualTo(1));
        Assert.That(belowThreshold.IsDropped, Is.True);
        Assert.That(filtered.IsDropped, Is.True);
        Assert.That(accepted.IsSuccess, Is.True);
        Assert.That(memorySink.GetName(), Is.EqualTo("recent"));
        Assert.That(memoryState.GetIsEnabled(), Is.True);
        Assert.That(memoryState.GetWrittenCount(), Is.EqualTo(3));
        Assert.That(memoryState.GetDroppedCount(), Is.EqualTo(2));
        Assert.That(memoryState.GetFailureCount(), Is.EqualTo(0));
        Assert.That(memoryState.GetDiagnosticCount(), Is.EqualTo(0));
        Assert.That(memoryState.GetLastEvent(), Is.SameAs(third));
        Assert.That(memoryState.GetLastResult().IsSuccess, Is.True);
        Assert.That(logFactory.GetMemorySinkEvents(memorySink), Is.EqualTo(new LogEvent[] { second, third }));

        logFactory.ClearMemorySink(memorySink);
        Assert.That(logFactory.GetMemorySinkEvents(memorySink), Is.Empty);
        Assert.That(memoryState.GetWrittenCount(), Is.EqualTo(0));
        Assert.That(memoryState.GetDroppedCount(), Is.EqualTo(0));
        Assert.That(memoryState.GetLastEvent(), Is.Null);

        LogResult nullEvent = memorySink.Write(null);
        Assert.That(nullEvent.IsFailure, Is.True);
        Assert.That(memoryState.GetFailureCount(), Is.EqualTo(1));
        memoryState.ClearFailureCount();
        memoryState.ClearDroppedCount();
        memoryState.ClearDiagnosticCount();
        memoryState.ClearCounters();
        Assert.That(memoryState.GetFailureCount(), Is.EqualTo(0));
    }

    [Test]
    public void TextConsoleDebugAndTraceSinks_WriteFlushAndCloseThroughPublicContracts()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        MessageFormatter formatter = new MessageFormatter();
        TrackingWriter textWriter = new TrackingWriter();
        TrackingWriter consoleOutput = new TrackingWriter();
        TrackingWriter consoleError = new TrackingWriter();
        StringBuilder debugOutput = new StringBuilder();
        StringBuilder traceOutput = new StringBuilder();
        LogSink textSink = logFactory.CreateTextWriterSink(textWriter, formatter, true);
        LogSink consoleSink = logFactory.CreateConsoleSink(consoleOutput, consoleError, null, true);
        LogSink debugSink = logFactory.CreateDebugSink(value => debugOutput.Append(value), formatter, true);
        LogSink traceSink = logFactory.CreateTraceSink(value => traceOutput.Append(value), formatter, true);
        LogEvent info = logFactory.CreateEvent(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent error = logFactory.CreateEvent(LogLevel.Error, "Application", "error", "error", new LogField[0]);

        Assert.That(textSink.Write(info).IsSuccess, Is.True);
        Assert.That(consoleSink.Write(info).IsSuccess, Is.True);
        Assert.That(consoleSink.Write(error).IsSuccess, Is.True);
        Assert.That(debugSink.Write(info).IsSuccess, Is.True);
        Assert.That(traceSink.Write(error).IsSuccess, Is.True);
        Assert.That(textWriter.ToString(), Is.EqualTo("Info:info"));
        Assert.That(textWriter.FlushCount, Is.EqualTo(1));
        Assert.That(consoleOutput.ToString(), Does.StartWith("\u001b[32m"));
        Assert.That(consoleOutput.ToString(), Does.Contain("[Info] Application: info"));
        Assert.That(consoleError.ToString(), Does.StartWith("\u001b[31m"));
        Assert.That(consoleError.ToString(), Does.Contain("[Error] Application: error"));
        Assert.That(debugOutput.ToString(), Is.EqualTo("Info:info"));
        Assert.That(traceOutput.ToString(), Is.EqualTo("Error:error"));

        LogSink[] sinks = new LogSink[] { textSink, consoleSink, debugSink, traceSink };
        foreach (LogSink sink in sinks)
        {
            Assert.That(sink.Flush().IsSuccess, Is.True);
            Assert.That(sink.Flush().IsSuccess, Is.True);
            Assert.That(sink.Drain().IsSuccess, Is.True);
            Assert.That(sink.Drain().IsSuccess, Is.True);
            Assert.That(sink.Close().IsSuccess, Is.True);
            Assert.That(sink.Close().IsSuccess, Is.True);
            Assert.That(((ILogSinkState)sink).GetIsClosed(), Is.True);
            Assert.That(((ILogSinkState)sink).GetIsEnabled(), Is.False);
            Assert.That(sink.Write(info).Code, Is.EqualTo("sink.closed"));
        }

        Assert.That(textWriter.FlushCount, Is.GreaterThan(1));
        Assert.That(consoleOutput.FlushCount, Is.GreaterThan(0));
        Assert.That(consoleError.FlushCount, Is.GreaterThan(0));
    }

    [Test]
    public void EventSourceAndDelegateSinks_ExposePayloadResultsAndLifecycleState()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEventSourceEntry? capturedEntry = null;
        LogEvent? capturedEvent = null;
        MessageFormatter formatter = new MessageFormatter();
        LogSink eventSourceSink = logFactory.CreateEventSourceSink(
            "Sample.Public.Sink." + Guid.NewGuid().ToString("N"),
            formatter,
            entry => capturedEntry = entry);
        LogSink delegateSink = logFactory.CreateDelegateSink(
            "callback",
            logEvent =>
            {
                capturedEvent = logEvent;
                return new LogResult(true, false, false, true, "delegate.observed", "observed", 0, 0, 1);
            });
        LogEvent logEvent = new LogEvent(
            new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero),
            LogLevel.Warn,
            "Application.Worker",
            17,
            "Processed",
            "Processed {Count}",
            "Processed 3",
            new LogField[] { factory.CreateIntField("Count", 3) },
            new InvalidOperationException("failed"),
            "trace-1",
            "span-1",
            "parent-1",
            1,
            2,
            "machine",
            "application",
            "",
            "",
            0);

        LogResult eventSourceResult = eventSourceSink.Write(logEvent);
        LogResult delegateResult = delegateSink.Write(logEvent);

        Assert.That(eventSourceResult.IsSuccess, Is.True);
        Assert.That(capturedEntry, Is.Not.Null);
        Assert.That(capturedEntry!.Timestamp, Is.EqualTo("2026-08-25T20:00:00.0000000Z"));
        Assert.That(capturedEntry.Level, Is.EqualTo("Warn"));
        Assert.That(capturedEntry.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(capturedEntry.EventId, Is.EqualTo(17));
        Assert.That(capturedEntry.EventName, Is.EqualTo("Processed"));
        Assert.That(capturedEntry.Message, Is.EqualTo("Processed 3"));
        Assert.That(capturedEntry.Payload, Is.EqualTo("Warn:Processed 3"));
        Assert.That(capturedEntry.Exception, Does.Contain("System.InvalidOperationException: failed"));
        Assert.That(delegateResult.IsDiagnostic, Is.True);
        Assert.That(capturedEvent, Is.SameAs(logEvent));
        Assert.That(delegateSink.GetName(), Is.EqualTo("callback"));
        Assert.That(((ILogSinkState)delegateSink).GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(((ILogSinkState)delegateSink).GetLastResult(), Is.SameAs(delegateResult));

        Assert.That(eventSourceSink.Flush().IsSuccess, Is.True);
        Assert.That(eventSourceSink.Drain().IsSuccess, Is.True);
        Assert.That(eventSourceSink.Close().IsSuccess, Is.True);
        Assert.That(eventSourceSink.Close().IsSuccess, Is.True);
        Assert.That(delegateSink.Close().IsSuccess, Is.True);
        Assert.That(delegateSink.Close().IsSuccess, Is.True);

        LogSink missingDelegate = logFactory.CreateDelegateSink("missing", null);
        LogResult missingResult = missingDelegate.Write(logEvent);
        Assert.That(missingResult.Code, Is.EqualTo("sink.delegate.missing"));
        Assert.That(((ILogSinkState)missingDelegate).GetFailureCount(), Is.EqualTo(1));
    }

    [Test]
    public void CustomInterfaceSink_UsesLevelChecksStateAndIdempotentLifecycleCalls()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        RecordingSink destination = new RecordingSink("external", LogLevel.Warn);
        LogSink sink = logFactory.WrapSink(destination);
        ILogSinkState state = sink;
        LogEvent info = logFactory.CreateEvent(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent warn = logFactory.CreateEvent(LogLevel.Warn, "Application", "warn", "warn", new LogField[0]);
        bool infoEnabled = sink.IsEnabledForLevel(LogLevel.Info);
        bool warnEnabled = sink.IsEnabledForLevel(LogLevel.Warn);

        LogResult dropped = sink.Write(info);
        LogResult accepted = sink.Write(warn);
        LogResult flush = sink.Flush();
        LogResult repeatedFlush = sink.Flush();
        LogResult drain = sink.Drain();
        LogResult repeatedDrain = sink.Drain();
        LogResult close = sink.Close();
        LogResult repeatedClose = sink.Close();

        Assert.That(sink.GetName(), Is.EqualTo("external"));
        Assert.That(infoEnabled, Is.False);
        Assert.That(warnEnabled, Is.True);
        Assert.That(dropped.IsDropped, Is.True);
        Assert.That(accepted.IsSuccess, Is.True);
        Assert.That(destination.Events, Is.EqualTo(new LogEvent[] { warn }));
        Assert.That(state.GetWrittenCount(), Is.EqualTo(1));
        Assert.That(state.GetDroppedCount(), Is.EqualTo(1));
        Assert.That(state.GetLastEvent(), Is.SameAs(warn));
        Assert.That(flush.IsSuccess, Is.True);
        Assert.That(repeatedFlush.IsSuccess, Is.True);
        Assert.That(drain.IsSuccess, Is.True);
        Assert.That(repeatedDrain.IsSuccess, Is.True);
        Assert.That(close.IsSuccess, Is.True);
        Assert.That(repeatedClose.IsSuccess, Is.True);
        Assert.That(destination.FlushCount, Is.EqualTo(2));
        Assert.That(destination.DrainCount, Is.EqualTo(5));
        Assert.That(destination.CloseCount, Is.EqualTo(1));
        Assert.That(state.GetIsClosed(), Is.True);
    }

    [Test]
    public void FileSink_OverwritesFlushesDrainsAndClosesRepeatedly()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        string directory = NewTempDirectory("file");
        string path = Path.Combine(directory, "events.log");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "old");
        LogSink sink = logFactory.CreateFileSink(path, new MessageFormatter(), false, null, null, true);
        LogEvent first = logFactory.CreateEvent(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = logFactory.CreateEvent(LogLevel.Error, "Application", "second", "second", new LogField[0]);

        Assert.That(sink.Write(first).IsSuccess, Is.True);
        Assert.That(sink.Write(second).IsSuccess, Is.True);
        Assert.That(File.ReadAllText(path), Is.EqualTo("Info:firstError:second"));
        Assert.That(sink.Flush().IsSuccess, Is.True);
        Assert.That(sink.Flush().IsSuccess, Is.True);
        Assert.That(sink.Drain().IsSuccess, Is.True);
        Assert.That(sink.Drain().IsSuccess, Is.True);
        Assert.That(sink.Close().IsSuccess, Is.True);
        Assert.That(sink.Close().IsSuccess, Is.True);
        Assert.That(((ILogSinkState)sink).GetIsClosed(), Is.True);
        Assert.That(sink.Write(first).Code, Is.EqualTo("sink.closed"));

        Directory.Delete(directory, true);
    }

    [Test]
    public void RollingFileSink_AppliesSizeRollingAndAgeAndCountRetention()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        string directory = NewTempDirectory("rolling");
        string basePath = Path.Combine(directory, "events.log");
        string expiredPath = Path.Combine(directory, "events-expired.log");
        string firstRolledPath = Path.Combine(directory, "events-1.log");
        string secondRolledPath = Path.Combine(directory, "events-2.log");
        Directory.CreateDirectory(directory);
        File.WriteAllText(expiredPath, "expired");
        File.SetLastWriteTimeUtc(expiredPath, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        LogRollingPolicy rolling = logFactory.CreateRollingPolicy(LogRollingInterval.None, 5);
        LogRetentionPolicy retention = logFactory.CreateRetentionPolicy(2, TimeSpan.FromDays(1));
        LogSink sink = logFactory.CreateFileSink(basePath, new RenderedMessageFormatter(), true, rolling, retention, true);
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);

        Assert.That(sink.Write(EventAt(timestamp, "first")).IsSuccess, Is.True);
        Assert.That(File.Exists(expiredPath), Is.False);
        File.SetLastWriteTimeUtc(basePath, timestamp.UtcDateTime.AddHours(-1));
        Assert.That(sink.Write(EventAt(timestamp, "second")).IsSuccess, Is.True);
        Assert.That(sink.Write(EventAt(timestamp, "third")).IsSuccess, Is.True);
        Assert.That(sink.Close().IsSuccess, Is.True);

        Assert.That(File.Exists(basePath), Is.False);
        Assert.That(File.Exists(firstRolledPath), Is.True);
        Assert.That(File.Exists(secondRolledPath), Is.True);
        Assert.That(File.ReadAllText(firstRolledPath), Is.EqualTo("second"));
        Assert.That(File.ReadAllText(secondRolledPath), Is.EqualTo("third"));
        Assert.That(Directory.GetFiles(directory, "events*.log"), Has.Length.EqualTo(2));

        Directory.Delete(directory, true);
    }

    private static LogEvent EventAt(DateTimeOffset timestamp, string message)
    {
        return new LogEvent(
            timestamp,
            LogLevel.Info,
            "Application",
            0,
            "",
            message,
            message,
            new LogField[0],
            null,
            "",
            "",
            "",
            0,
            0,
            "",
            "",
            "",
            "",
            0);
    }

    private static string NewTempDirectory(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-public-sink-tests", Guid.NewGuid().ToString("N"), name);
    }

    private sealed class MessageFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return logEvent.Level.ToString() + ":" + logEvent.RenderedMessage;
        }
    }

    private sealed class RenderedMessageFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return logEvent.RenderedMessage;
        }
    }

    private sealed class TrackingWriter : StringWriter
    {
        public int FlushCount;

        public override void Flush()
        {
            FlushCount = FlushCount + 1;
            base.Flush();
        }
    }

    private sealed class RequiredFieldFilter : ILogFilter
    {
        public string Name;

        public RequiredFieldFilter(string name)
        {
            Name = name;
        }

        public LogDecision Decide(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return LogDecision.Neutral;
            }

            foreach (LogField field in logEvent.Fields)
            {
                if (field.Name == Name)
                {
                    return LogDecision.Include;
                }
            }

            return LogDecision.Exclude;
        }
    }

    private sealed class RecordingSink : ILogSink
    {
        private readonly string name;
        private readonly LogLevel minimumLevel;
        public LogEvent[] Events;
        public int FlushCount;
        public int DrainCount;
        public int CloseCount;

        public RecordingSink(string sinkName, LogLevel level)
        {
            name = sinkName;
            minimumLevel = level;
            Events = new LogEvent[0];
            FlushCount = 0;
            DrainCount = 0;
            CloseCount = 0;
        }

        public string GetName()
        {
            return name;
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return level != LogLevel.Off && (int)level >= (int)minimumLevel;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return new LogResult(false, true, false, false, "external.null", "event is null", 0, 1, 0);
            }

            LogEvent[] copy = new LogEvent[Events.Length + 1];
            int index = 0;
            while (index < Events.Length)
            {
                copy[index] = Events[index];
                index = index + 1;
            }

            copy[Events.Length] = logEvent;
            Events = copy;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Flush()
        {
            FlushCount = FlushCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Drain()
        {
            DrainCount = DrainCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Close()
        {
            CloseCount = CloseCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
