using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicLoggerWithContextTests
{
    [Test]
    public void WithMethods_PreserveCategoryLevelFilterEnricherSinkAndScopeBehavior()
    {
        LogFactory factory = new LogFactory();
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        levelSwitch.SetLevel(LogLevel.Error);
        Logger parent = factory.CreateNamedLoggerWithSwitch("Application.Worker", levelSwitch);
        LogSink sink = factory.CreateMemorySink("memory", 0);
        parent.AddSink(sink);
        parent.AddFilter(factory.CreateFieldExistsFilter("TenantId"));
        parent.AddEnricher(new StaticEnricher(factory.CreateStringField("Service", "api")));

        ILoggerContext context = parent;
        Logger tenant = context.WithField("TenantId", "tenant-1");
        Logger worker = tenant.WithLogField(factory.CreateIntField("WorkerId", 7));
        Logger child = worker.WithFields(new LogField[]
        {
            factory.CreateStringField("CorrelationId", "correlation-1")
        });
        LogScope scope = parent.BeginScopeField("RequestId", "request-1");

        LogResult disabled = child.WriteWithValues(LogLevel.Warn, "Processed {Count}", new object?[] { 3 });
        levelSwitch.SetLevel(LogLevel.Trace);
        LogResult accepted = child.WriteWithValues(LogLevel.Info, "Processed {Count}", new object?[] { 3 });
        LogResult parentResult = parent.WriteWithValues(LogLevel.Info, "parent", null);
        scope.Dispose();

        LogEvent[] events = factory.GetMemorySinkEvents(sink);
        Assert.That(disabled.IsDropped, Is.True);
        Assert.That(accepted.IsSuccess, Is.True);
        Assert.That(parentResult.IsDropped, Is.True);
        Assert.That(events.Length, Is.EqualTo(1));
        Assert.That(events[0].LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(events[0].RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(events[0].Fields.Length, Is.EqualTo(6));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(events[0].Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
        Assert.That(events[0].Fields[1].Name, Is.EqualTo("WorkerId"));
        Assert.That(events[0].Fields[1].Value.IntegerValue, Is.EqualTo(7));
        Assert.That(events[0].Fields[2].Name, Is.EqualTo("CorrelationId"));
        Assert.That(events[0].Fields[3].Name, Is.EqualTo("RequestId"));
        Assert.That(events[0].Fields[4].Name, Is.EqualTo("Count"));
        Assert.That(events[0].Fields[5].Name, Is.EqualTo("Service"));
        Assert.That(parent.GetLastEvent(), Is.Null);
    }

    [Test]
    public void WithMethods_KeepParentFieldsIndependentAndNormalizeNullInputs()
    {
        LogFactory factory = new LogFactory();
        Logger parent = factory.CreateRootLoggerWithLevel(LogLevel.Trace).WithField("Application", "api");
        Logger nullValue = parent.WithField("Optional", null);
        Logger noAdditionalFields = nullValue.WithFields(null);
        Logger invalidField = noAdditionalFields.WithLogField(null);

        parent.WriteWithValues(LogLevel.Info, "parent", null);
        nullValue.WriteWithValues(LogLevel.Info, "child", null);
        noAdditionalFields.WriteWithValues(LogLevel.Info, "copy", null);
        invalidField.WriteWithValues(LogLevel.Info, "invalid", null);

        Assert.That(parent.GetLastEvent(), Is.Not.Null);
        Assert.That(parent.GetLastEvent()!.Fields.Length, Is.EqualTo(1));
        Assert.That(parent.GetLastEvent()!.Fields[0].Name, Is.EqualTo("Application"));
        Assert.That(nullValue.GetLastEvent()!.Fields.Length, Is.EqualTo(2));
        Assert.That(nullValue.GetLastEvent()!.Fields[1].Name, Is.EqualTo("Optional"));
        Assert.That(nullValue.GetLastEvent()!.Fields[1].Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(noAdditionalFields.GetLastEvent()!.Fields.Length, Is.EqualTo(2));
        Assert.That(invalidField.GetLastEvent()!.Fields.Length, Is.EqualTo(2));
        Assert.That(invalidField.GetLastEvent()!.InvalidFieldCount, Is.EqualTo(1));
    }

    private sealed class StaticEnricher : ILogEnricher
    {
        public LogField Field;

        public StaticEnricher(LogField field)
        {
            Field = field;
        }

        public LogField[] Enrich(LogLevel level, string loggerName)
        {
            return new LogField[] { Field };
        }
    }
}
