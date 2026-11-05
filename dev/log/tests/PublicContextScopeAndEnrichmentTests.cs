using System.Diagnostics;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicContextScopeAndEnrichmentTests
{
    [Test]
    public void ForOperations_CreateIndependentContextualChildren()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger root = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application", LogLevel.Trace);
        LogSink sink = logFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)root).AddSink(sink);
        ILoggerContext context = root;

        Logger category = context.ForWithFields("Worker", new LogField[]
        {
            factory.CreateStringField("Component", "queue")
        });
        Logger tenant = ((ILoggerContext)category).ForField("TenantId", "tenant-1");
        Logger worker = ((ILoggerContext)tenant).ForLogField(factory.CreateIntField("WorkerId", 7));
        Logger contextual = ((ILoggerContext)worker).ForFields(new LogField[]
        {
            factory.CreateStringField("CorrelationId", "correlation-1")
        });

        LogResult childResult = contextual.WriteWithValues(LogLevel.Info, "processed", null);
        LogResult rootResult = root.WriteWithValues(LogLevel.Info, "root", null);
        LogEvent[] events = logFactory.GetMemorySinkEvents(sink);

        Assert.That(childResult.IsSuccess, Is.True);
        Assert.That(rootResult.IsSuccess, Is.True);
        Assert.That(contextual.GetName(), Is.EqualTo("Application.Worker"));
        Assert.That(events, Has.Length.EqualTo(2));
        Assert.That(events[0].LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(events[0].Fields, Has.Length.EqualTo(4));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("Component"));
        Assert.That(events[0].Fields[1].Name, Is.EqualTo("TenantId"));
        Assert.That(events[0].Fields[2].Name, Is.EqualTo("WorkerId"));
        Assert.That(events[0].Fields[3].Name, Is.EqualTo("CorrelationId"));
        Assert.That(events[1].LoggerName, Is.EqualTo("Application"));
        Assert.That(events[1].Fields, Is.Empty);
    }

    [Test]
    public async Task Scopes_FlowAcrossAwaitAndSupportNestedOutOfOrderDisposal()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink sink = logFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)logger).AddSink(sink);
        ILoggerContext context = logger;

        LogScope outer = context.BeginScope(new LogField[]
        {
            factory.CreateStringField("RequestId", "request-1"),
            factory.CreateStringField("TenantId", "tenant-1")
        });
        await Task.Yield();
        LogScope inner = context.BeginScopeLogField(factory.CreateStringField("Operation", "sync"));
        await Task.Yield();
        logger.WriteWithValues(LogLevel.Info, "nested", null);

        outer.Dispose();
        await Task.Yield();
        logger.WriteWithValues(LogLevel.Info, "outer disposed", null);

        inner.Dispose();
        await Task.Yield();
        logger.WriteWithValues(LogLevel.Info, "all disposed", null);

        LogEvent[] events = logFactory.GetMemorySinkEvents(sink);
        Assert.That(events, Has.Length.EqualTo(3));
        Assert.That(events[0].Fields, Has.Length.EqualTo(3));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(events[0].Fields[1].Name, Is.EqualTo("TenantId"));
        Assert.That(events[0].Fields[2].Name, Is.EqualTo("Operation"));
        Assert.That(events[1].Fields, Has.Length.EqualTo(1));
        Assert.That(events[1].Fields[0].Name, Is.EqualTo("Operation"));
        Assert.That(events[2].Fields, Is.Empty);
    }

    [Test]
    public void Configuration_ContextActivityEnrichmentClockAndDurationCompose()
    {
        Activity.Current = null;
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 25, 12, 30, 0, TimeSpan.Zero);
        FixedClock clock = new FixedClock(timestamp);
        CapturingEnricher enricher = new CapturingEnricher(factory.CreateStringField("EnrichedBy", "consumer"));
        LogSink sink = logFactory.CreateMemorySink("memory", 0);
        LogField[] globalFields = new LogField[]
        {
            factory.CreateStringField("Environment", "production")
        };
        LogConfiguration configuration = new LogConfiguration()
            .Name("Application")
            .MinimumLevel(LogLevel.Trace)
            .RuntimeMetadata(false)
            .Activity(true)
            .Clock(clock)
            .SetGlobalFields(globalFields)
            .AddGlobalField("Deployment", "blue")
            .AddGlobalLogField(factory.CreateFieldGroup("service", new LogField[]
            {
                factory.CreateStringField("name", "billing")
            }))
            .AddInterfaceEnricher(enricher)
            .AddSink(sink);
        globalFields[0] = factory.CreateStringField("Environment", "mutated");
        Logger root = configuration.CreateLogger();
        Logger logger = ((ILoggerContext)root)
            .ForTypeName("Company.Handlers.JobHandler`1[System.String]")
            .WithFieldNamespace("request.http", new LogField[]
            {
                factory.CreateStringField("method", "POST")
            });
        ManualTimeProvider timeProvider = new ManualTimeProvider(1000);
        Activity parent = new Activity("parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();
        Activity child = new Activity("child");
        child.SetIdFormat(ActivityIdFormat.W3C);
        child.Start();

        ILogDuration duration = ((ILoggerDuration)logger).BeginDuration(
            LogLevel.Warn,
            "Processed {Count}",
            new object?[] { 4 },
            "Elapsed",
            timeProvider);
        timeProvider.Advance(TimeSpan.FromMilliseconds(125));
        LogResult result = duration.Complete();
        LogEvent[] events = logFactory.GetMemorySinkEvents(sink);

        child.Stop();
        parent.Stop();
        Activity.Current = null;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(events, Has.Length.EqualTo(1));
        LogEvent logEvent = events[0];
        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(clock.CallCount, Is.EqualTo(1));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Company.Handlers.JobHandler"));
        Assert.That(logEvent.TraceId, Is.EqualTo(child.TraceId.ToString()));
        Assert.That(logEvent.SpanId, Is.EqualTo(child.SpanId.ToString()));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo(parent.SpanId.ToString()));
        Assert.That(enricher.CallCount, Is.EqualTo(1));
        Assert.That(enricher.Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(enricher.LoggerName, Is.EqualTo("Application.Company.Handlers.JobHandler"));
        Assert.That(FindField(logEvent, "Environment").Value.TextValue, Is.EqualTo("production"));
        Assert.That(FindField(logEvent, "Deployment").Value.TextValue, Is.EqualTo("blue"));
        Assert.That(FindField(logEvent, "service").Value.DictionaryValue["name"].TextValue, Is.EqualTo("billing"));
        Assert.That(FindField(logEvent, "request").Value.DictionaryValue["http"].DictionaryValue["method"].TextValue, Is.EqualTo("POST"));
        Assert.That(FindField(logEvent, "Count").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(FindField(logEvent, "Elapsed").Value.DurationValue, Is.EqualTo(TimeSpan.FromMilliseconds(125)));
        Assert.That(FindField(logEvent, "EnrichedBy").Value.TextValue, Is.EqualTo("consumer"));
    }

    private static LogField FindField(LogEvent logEvent, string name)
    {
        foreach (LogField field in logEvent.Fields)
        {
            if (field.Name == name)
            {
                return field;
            }
        }

        throw new InvalidOperationException("field not found: " + name);
    }

    private sealed class FixedClock : ILogClock
    {
        public DateTimeOffset Timestamp;
        public int CallCount;

        public FixedClock(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
            CallCount = 0;
        }

        public DateTimeOffset UtcNow()
        {
            CallCount = CallCount + 1;
            return Timestamp;
        }
    }

    private sealed class CapturingEnricher : ILogEnricher
    {
        public LogField Field;
        public int CallCount;
        public LogLevel Level;
        public string LoggerName;

        public CapturingEnricher(LogField field)
        {
            Field = field;
            CallCount = 0;
            Level = LogLevel.Off;
            LoggerName = "";
        }

        public LogField[] Enrich(LogLevel level, string loggerName)
        {
            CallCount = CallCount + 1;
            Level = level;
            LoggerName = loggerName;
            return new LogField[] { Field };
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly long frequency;
        private long timestamp;

        public ManualTimeProvider(long timestampFrequency)
        {
            frequency = timestampFrequency;
            timestamp = 0;
        }

        public override long TimestampFrequency
        {
            get
            {
                return frequency;
            }
        }

        public override long GetTimestamp()
        {
            return timestamp;
        }

        public void Advance(TimeSpan elapsed)
        {
            timestamp = timestamp + (long)(elapsed.TotalSeconds * frequency);
        }
    }
}
