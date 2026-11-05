using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class InterfaceAbstractionTests
{
    [Test]
    public void ApplicationCode_CanUseAPlainLoggerFake()
    {
        CapturingLogger logger = new CapturingLogger();
        ApplicationComponent component = new ApplicationComponent(logger);

        LogResult result = component.Run(4);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.Level, Is.EqualTo(LogLevel.Info));
        Assert.That(logger.MessageTemplate, Is.EqualTo("Processed {Count}"));
        Assert.That(logger.Values, Is.EqualTo(new object[] { 4 }));
    }

    [Test]
    public void Logger_ImplementsNarrowApplicationContract()
    {
        ILogSink sink = new RecordingSink();
        ILogger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .AddInterfaceSink(sink)
            .CreateLogger();

        LogResult result = logger.WriteWithValues(LogLevel.Info, "Processed {Count}", new object[] { 3 });
        RecordingSink recording = (RecordingSink)sink;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(recording.Events.Length, Is.EqualTo(1));
        Assert.That(recording.Events[0].RenderedMessage, Is.EqualTo("Processed 3"));
    }

    [Test]
    public void InterfaceSink_ParticipatesInLevelChecksAndLifecycle()
    {
        RecordingSink sink = new RecordingSink();
        sink.MinimumLevel = LogLevel.Warn;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddInterfaceSink((ILogSink)sink);

        LogResult info = logger.Info("info");
        LogResult warn = logger.Warn("warn");
        LogResult flush = logger.Flush();
        LogResult close = logger.Close();

        Assert.That(info.IsDropped, Is.True);
        Assert.That(warn.IsSuccess, Is.True);
        Assert.That(sink.Events.Length, Is.EqualTo(1));
        Assert.That(sink.DrainCount, Is.EqualTo(2));
        Assert.That(sink.FlushCount, Is.EqualTo(1));
        Assert.That(sink.CloseCount, Is.EqualTo(1));
        Assert.That(flush.IsSuccess, Is.True);
        Assert.That(close.IsSuccess, Is.True);
    }

    [Test]
    public void InterfaceSink_CanBeUsedByConcreteDecorators()
    {
        RecordingSink sink = new RecordingSink();
        LogSink batch = BatchLogSink.WithSize(LogSink.Wrap(sink), 2);
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(batch);

        logger.Info("first");
        logger.Info("second");

        Assert.That(sink.Events.Length, Is.EqualTo(2));
        Assert.That(sink.Events[0].RenderedMessage, Is.EqualTo("first"));
        Assert.That(sink.Events[1].RenderedMessage, Is.EqualTo("second"));
    }

    [Test]
    public void InterfaceFilter_CanBeUsedAtLoggerAndSinkBoundaries()
    {
        RecordingSink sink = new RecordingSink();
        PrefixFilter loggerFilter = new PrefixFilter("keep");
        PrefixFilter sinkFilter = new PrefixFilter("keep this");
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddInterfaceFilter((ILogFilter)loggerFilter);
        logger.AddInterfaceSink((ILogSink)sink);
        logger.Pipeline.Sinks[0].AddInterfaceFilter((ILogFilter)sinkFilter);

        LogResult first = logger.Info("drop");
        LogResult second = logger.Info("keep but drop at sink");
        LogResult third = logger.Info("keep this event");

        Assert.That(first.IsDropped, Is.True);
        Assert.That(second.IsDropped, Is.True);
        Assert.That(third.IsSuccess, Is.True);
        Assert.That(sink.Events.Length, Is.EqualTo(1));
        Assert.That(loggerFilter.DecisionCount, Is.EqualTo(3));
        Assert.That(sinkFilter.DecisionCount, Is.EqualTo(2));
    }

    [Test]
    public void InterfaceFormatter_WritesWithoutDelegateFactory()
    {
        StringWriter writer = new StringWriter();
        ILogFormatter formatter = new LevelFormatter();
        LogSink sink = TextWriterLogSink.WithFormatter(writer, formatter);
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);

        logger.Warn("careful");

        Assert.That(writer.ToString(), Is.EqualTo("Warn|careful"));
    }

    [Test]
    public void InterfaceEnricher_AddsDynamicFieldsAndFlowsToChildren()
    {
        RecordingSink sink = new RecordingSink();
        CapturingEnricher enricher = new CapturingEnricher();
        Logger root = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .AddInterfaceSink((ILogSink)sink)
            .AddInterfaceEnricher((ILogEnricher)enricher)
            .CreateLogger();
        Logger child = root.For("Worker");

        child.Error("failed");

        Assert.That(enricher.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(enricher.LoggerName, Is.EqualTo("Worker"));
        Assert.That(sink.Events[0].Fields[0].Name, Is.EqualTo("Component"));
        Assert.That(sink.Events[0].Fields[0].Value.TextValue, Is.EqualTo("test"));
    }

    [Test]
    public void InterfaceClock_MakesTimestampsAndRateLimitsDeterministic()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        TestClock clock = new TestClock(start);
        RecordingSink sink = new RecordingSink();
        Logger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .Clock(clock)
            .AddInterfaceSink((ILogSink)sink)
            .AddFilter(LogFilter.RateLimitWithClock(1, TimeSpan.FromSeconds(10), clock))
            .CreateLogger();

        LogResult first = logger.Info("first");
        LogResult second = logger.Info("second");
        clock.Current = start.AddSeconds(11);
        LogResult third = logger.Info("third");

        Assert.That(first.IsSuccess, Is.True);
        Assert.That(second.IsDropped, Is.True);
        Assert.That(third.IsSuccess, Is.True);
        Assert.That(sink.Events[0].Timestamp, Is.EqualTo(start));
        Assert.That(sink.Events[1].Timestamp, Is.EqualTo(start.AddSeconds(11)));
    }

    private sealed class RecordingSink : ILogSink
    {
        public LogEvent[] Events = Array.Empty<LogEvent>();
        public LogLevel MinimumLevel = LogLevel.Trace;
        public int DrainCount;
        public int FlushCount;
        public int CloseCount;

        public string GetName()
        {
            return "recording";
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return level != LogLevel.Off && (int)level >= (int)MinimumLevel;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return LogResult.Failure("log event cannot be null");
            }

            LogEvent[] copy = new LogEvent[Events.Length + 1];
            Array.Copy(Events, copy, Events.Length);
            copy[Events.Length] = logEvent;
            Events = copy;
            return LogResult.Success();
        }

        public LogResult Flush()
        {
            FlushCount++;
            return LogResult.Success();
        }

        public LogResult Drain()
        {
            DrainCount++;
            return LogResult.Success();
        }

        public LogResult Close()
        {
            CloseCount++;
            return LogResult.Success();
        }
    }

    private sealed class ApplicationComponent
    {
        public ILogger Logger;

        public ApplicationComponent(ILogger logger)
        {
            Logger = logger;
        }

        public LogResult Run(int count)
        {
            return Logger.WriteWithValues(LogLevel.Info, "Processed {Count}", new object[] { count });
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public LogLevel Level;
        public string MessageTemplate = "";
        public object?[] Values = Array.Empty<object?>();

        public bool IsEnabled(LogLevel level)
        {
            return true;
        }

        public LogResult WriteWithValues(LogLevel level, string? messageTemplate, object?[]? values)
        {
            Level = level;
            MessageTemplate = LogResult.NormalizeText(messageTemplate);
            Values = Logger.NormalizeValues(values);
            return LogResult.Success();
        }

        public LogResult WriteWithExceptionAndValues(LogLevel level, Exception? exceptionValue, string? messageTemplate, object?[]? values)
        {
            return WriteWithValues(level, messageTemplate, values);
        }

        public LogResult WriteEvent(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return LogResult.Failure("log event cannot be null");
            }

            Level = logEvent.Level;
            MessageTemplate = logEvent.MessageTemplate;
            return LogResult.Success();
        }
    }

    private sealed class PrefixFilter : ILogFilter
    {
        public string Prefix;
        public int DecisionCount;

        public PrefixFilter(string prefix)
        {
            Prefix = prefix;
        }

        public LogDecision Decide(LogEvent? logEvent)
        {
            DecisionCount++;
            if (logEvent == null)
            {
                return LogDecision.Neutral;
            }

            if (logEvent.RenderedMessage.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return LogDecision.Include;
            }

            return LogDecision.Exclude;
        }
    }

    private sealed class LevelFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return logEvent.Level.ToString() + "|" + logEvent.RenderedMessage;
        }
    }

    private sealed class CapturingEnricher : ILogEnricher
    {
        public LogLevel Level;
        public string LoggerName = "";

        public LogField[] Enrich(LogLevel level, string loggerName)
        {
            Level = level;
            LoggerName = loggerName;
            return new LogField[] { LogField.String("Component", "test") };
        }
    }

    private sealed class TestClock : ILogClock
    {
        public DateTimeOffset Current;

        public TestClock(DateTimeOffset current)
        {
            Current = current;
        }

        public DateTimeOffset UtcNow()
        {
            return Current;
        }
    }
}
