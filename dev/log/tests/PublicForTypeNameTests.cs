using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicForTypeNameTests
{
    [Test]
    public void ForTypeName_NormalizesClrAndCSharpTypeNameForms()
    {
        ILoggerFactory factory = new LogFactory();
        ILoggerContext context = factory.CreateNamedLogger("Root");

        Logger assemblyQualified = context.ForTypeName(" global::Company.Products.Processor`1[[System.String, System.Private.CoreLib]], Company.App ");
        Logger nested = context.ForTypeName("Company.Outer`1+Inner[]");
        Logger csharpGeneric = context.ForTypeName("Company.Worker<System.String>");
        Logger generated = context.ForTypeName("123 Bad-Type");

        Assert.That(assemblyQualified.GetName(), Is.EqualTo("Root.Company.Products.Processor"));
        Assert.That(nested.GetName(), Is.EqualTo("Root.Company.Outer.Inner"));
        Assert.That(csharpGeneric.GetName(), Is.EqualTo("Root.Company.Worker"));
        Assert.That(generated.GetName(), Is.EqualTo("Root._123_Bad_Type"));
        Assert.That(assemblyQualified.GetIsValid(), Is.True);
        Assert.That(nested.GetIsValid(), Is.True);
        Assert.That(csharpGeneric.GetIsValid(), Is.True);
        Assert.That(generated.GetIsValid(), Is.True);
    }

    [Test]
    public void ForTypeName_EmptyInputKeepsTheParentCategory()
    {
        ILoggerContext context = new LogFactory().CreateNamedLogger("Application");

        Logger fromNull = context.ForTypeName(null);
        Logger fromWhitespace = context.ForTypeName("   ");

        Assert.That(fromNull.GetName(), Is.EqualTo("Application"));
        Assert.That(fromWhitespace.GetName(), Is.EqualTo("Application"));
    }

    [Test]
    public void ForTypeName_PreservesHierarchyAndInheritedContextPipelineAndScope()
    {
        LogFactory factory = new LogFactory();
        Logger root = factory.CreateNamedLoggerWithLevel("Application", LogLevel.Trace).WithField("TenantId", "tenant-1");
        LogSink sink = factory.CreateMemorySink("memory", 0);
        root.AddSink(sink);
        root.AddFilter(factory.CreateFieldExistsFilter("TenantId"));
        root.AddEnricher(new StaticEnricher(factory.CreateStringField("Service", "api")));
        root.SetCategoryLevel("Application.Company.Services", LogLevel.Error);
        Logger logger = root.ForTypeName("Company.Services.Worker`1[System.String]");
        LogScope scope = root.BeginScopeField("RequestId", "request-1");

        LogResult warn = logger.WriteWithValues(LogLevel.Warn, "warn", null);
        LogResult error = logger.WriteWithValues(LogLevel.Error, "failed", null);
        scope.Dispose();

        LogEvent[] events = factory.GetMemorySinkEvents(sink);
        Assert.That(logger.GetName(), Is.EqualTo("Application.Company.Services.Worker"));
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(warn.IsDropped, Is.True);
        Assert.That(error.IsSuccess, Is.True);
        Assert.That(events.Length, Is.EqualTo(1));
        Assert.That(events[0].LoggerName, Is.EqualTo("Application.Company.Services.Worker"));
        Assert.That(events[0].Fields.Length, Is.EqualTo(3));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(events[0].Fields[1].Name, Is.EqualTo("RequestId"));
        Assert.That(events[0].Fields[2].Name, Is.EqualTo("Service"));
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
