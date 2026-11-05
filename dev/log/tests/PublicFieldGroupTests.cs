using System.Text.Json;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicFieldGroupTests
{
    [Test]
    public void GroupFactory_CreatesNestedValuesAndResolvesChildNames()
    {
        LogFactory factory = new LogFactory();
        ILogFieldGroupFactory groups = factory;
        LogField client = groups.CreateFieldGroup("client", new LogField[]
        {
            factory.CreateStringField("ip", "127.0.0.1")
        });

        LogField request = groups.CreateFieldGroup("request", new LogField[]
        {
            factory.CreateStringField("method", "GET"),
            factory.CreateStringField("method", "POST"),
            factory.CreateStringField("bad name", "ignored"),
            client,
            null!
        });

        Assert.That(request.IsValid, Is.True);
        Assert.That(request.Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(request.Value.DictionaryValue.Count, Is.EqualTo(3));
        Assert.That(request.Value.DictionaryValue["method"].TextValue, Is.EqualTo("GET"));
        Assert.That(request.Value.DictionaryValue["method_1"].TextValue, Is.EqualTo("POST"));
        Assert.That(request.Value.DictionaryValue["client"].Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(request.Value.DictionaryValue["client"].DictionaryValue["ip"].TextValue, Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void NamespaceFactory_ExpandsDottedNamesIntoNestedGroups()
    {
        LogFactory factory = new LogFactory();
        ILogFieldGroupFactory groups = factory;

        LogField fieldNamespace = groups.CreateFieldNamespace("http.request", new LogField[]
        {
            factory.CreateStringField("method", "GET"),
            factory.CreateIntField("status", 200)
        });
        LogField invalid = groups.CreateFieldNamespace("http..request", null);
        LogField empty = groups.CreateFieldGroup("empty", null);

        Assert.That(fieldNamespace.Name, Is.EqualTo("http"));
        Assert.That(fieldNamespace.Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(fieldNamespace.Value.DictionaryValue["request"].Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(fieldNamespace.Value.DictionaryValue["request"].DictionaryValue["method"].TextValue, Is.EqualTo("GET"));
        Assert.That(fieldNamespace.Value.DictionaryValue["request"].DictionaryValue["status"].IntegerValue, Is.EqualTo(200));
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(empty.IsValid, Is.True);
        Assert.That(empty.Value.DictionaryValue, Is.Empty);
    }

    [Test]
    public void LoggerGroupBindings_PreserveJsonNestingAndStableTextRendering()
    {
        LogFactory factory = new LogFactory();
        Logger root = factory.CreateNamedLoggerWithLevel("Application.Worker", LogLevel.Trace);
        Logger grouped = root.WithFieldGroup("request", new LogField[]
        {
            factory.CreateStringField("method", "GET"),
            factory.CreateStringField("path", "/items")
        });
        Logger namespaced = grouped.WithFieldNamespace("client.network", new LogField[]
        {
            factory.CreateStringField("ip", "127.0.0.1")
        });
        LogScope groupScope = namespaced.BeginScopeFieldGroup("job", new LogField[]
        {
            factory.CreateIntField("attempt", 2)
        });
        LogScope namespaceScope = namespaced.BeginScopeFieldNamespace("operation.details", new LogField[]
        {
            factory.CreateStringField("step", "sync")
        });

        LogResult result = namespaced.WriteWithValues(LogLevel.Info, "processed", null);
        LogEvent logEvent = namespaced.GetLastEvent()!;
        namespaceScope.Dispose();
        groupScope.Dispose();

        string json = factory.CreateCompactJsonFormatter().Format(logEvent);
        string text = factory.CreateTextFormatter().Format(logEvent);
        string keyValue = factory.CreateKeyValueFormatter().Format(logEvent);
        JsonElement fields = JsonDocument.Parse(json).RootElement.GetProperty("fields");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(fields.GetProperty("request").GetProperty("method").GetString(), Is.EqualTo("GET"));
        Assert.That(fields.GetProperty("request").GetProperty("path").GetString(), Is.EqualTo("/items"));
        Assert.That(fields.GetProperty("client").GetProperty("network").GetProperty("ip").GetString(), Is.EqualTo("127.0.0.1"));
        Assert.That(fields.GetProperty("job").GetProperty("attempt").GetInt64(), Is.EqualTo(2));
        Assert.That(fields.GetProperty("operation").GetProperty("details").GetProperty("step").GetString(), Is.EqualTo("sync"));
        Assert.That(text, Does.Contain("request={method=GET, path=/items}"));
        Assert.That(text, Does.Contain("client={network={ip=127.0.0.1}}"));
        Assert.That(text, Does.Contain("job={attempt=2}"));
        Assert.That(text, Does.Contain("operation={details={step=sync}}"));
        Assert.That(keyValue, Does.Contain("request=\"{method=GET, path=/items}\""));
        Assert.That(keyValue, Does.Contain("client=\"{network={ip=127.0.0.1}}\""));
        Assert.That(keyValue, Does.Contain("job=\"{attempt=2}\""));
        Assert.That(keyValue, Does.Contain("operation=\"{details={step=sync}}\""));
    }
}
