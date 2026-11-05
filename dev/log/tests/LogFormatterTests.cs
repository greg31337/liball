using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFormatterTests
{
    [Test]
    public void Format_UsesRenderedMessageByDefault()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "Hello {Name}", "Hello Ada", new LogField[0]);

        string output = formatter.Format(logEvent);

        Assert.That(output, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void Format_UsesMessageTemplateWhenRenderedMessageIsEmpty()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "template", "", new LogField[0]);

        string output = formatter.Format(logEvent);

        Assert.That(output, Is.EqualTo("template"));
    }

    [Test]
    public void Format_ReturnsEmptyTextForNullEvent()
    {
        LogFormatter formatter = new LogFormatter();

        string output = formatter.Format(null);

        Assert.That(output, Is.EqualTo(""));
    }

    [Test]
    public void Custom_UsesConfiguredFormatAction()
    {
        LogFormatter formatter = LogFormatter.Custom("key-value", logEvent => logEvent.Level.ToString() + ":" + logEvent.RenderedMessage);
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Format(logEvent);

        Assert.That(formatter.Name, Is.EqualTo(LogFormatter.KeyValueName));
        Assert.That(output, Is.EqualTo("Warn:rendered"));
    }

    [Test]
    public void Dispatch_UsesMatchingChildFormatter()
    {
        LogFormatter dispatcher = new LogFormatter();
        LogFormatter json = LogFormatter.Custom("json", logEvent => "json:" + logEvent.RenderedMessage);
        LogFormatter text = LogFormatter.Custom("text", logEvent => "text:" + logEvent.RenderedMessage);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        dispatcher.SetFormatters(new LogFormatter[] { json, text });
        string output = dispatcher.Dispatch("JSON", logEvent);

        Assert.That(output, Is.EqualTo("json:rendered"));
    }

    [Test]
    public void Dispatch_UsesDefaultFormatterWhenNameIsUnknown()
    {
        LogFormatter dispatcher = LogFormatter.Custom("text", logEvent => "default:" + logEvent.RenderedMessage);
        LogFormatter json = LogFormatter.Custom("json", logEvent => "json:" + logEvent.RenderedMessage);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        dispatcher.AddFormatter(json);
        string output = dispatcher.Dispatch("missing", logEvent);

        Assert.That(output, Is.EqualTo("default:rendered"));
    }

    [Test]
    public void DispatchLine_UsesSelectedFormatterNewLineSetting()
    {
        LogFormatter dispatcher = new LogFormatter();
        LogFormatter json = LogFormatter.Custom("json", logEvent => logEvent.RenderedMessage);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        json.AppendNewLine = true;
        json.NewLine = "\n";
        dispatcher.AddFormatter(json);
        string output = dispatcher.DispatchLine("json", logEvent);

        Assert.That(output, Is.EqualTo("rendered\n"));
    }

    [Test]
    public void SetFormatters_CopiesFormatterArray()
    {
        LogFormatter dispatcher = new LogFormatter();
        LogFormatter json = LogFormatter.Named("json");
        LogFormatter text = LogFormatter.Named("text");
        LogFormatter[] formatters = new LogFormatter[] { json };

        dispatcher.SetFormatters(formatters);
        formatters[0] = text;

        Assert.That(dispatcher.Formatters[0], Is.SameAs(json));
    }

    [Test]
    public void NormalizeName_HandlesAliasesAndDefaults()
    {
        Assert.That(LogFormatter.NormalizeName(null), Is.EqualTo(LogFormatter.TextName));
        Assert.That(LogFormatter.NormalizeName(" NDJSON "), Is.EqualTo(LogFormatter.CompactJsonName));
        Assert.That(LogFormatter.NormalizeName("compactjson"), Is.EqualTo(LogFormatter.CompactJsonName));
        Assert.That(LogFormatter.NormalizeName("console"), Is.EqualTo(LogFormatter.ConsoleTextName));
        Assert.That(LogFormatter.NormalizeName("kv"), Is.EqualTo(LogFormatter.KeyValueName));
    }

    [Test]
    public void IsKnownName_ReturnsTrueForPlannedFormatterNames()
    {
        Assert.That(LogFormatter.IsKnownName("text"), Is.True);
        Assert.That(LogFormatter.IsKnownName("json"), Is.True);
        Assert.That(LogFormatter.IsKnownName("compact-json"), Is.True);
        Assert.That(LogFormatter.IsKnownName("console-text"), Is.True);
        Assert.That(LogFormatter.IsKnownName("key-value"), Is.True);
        Assert.That(LogFormatter.IsKnownName("template"), Is.True);
        Assert.That(LogFormatter.IsKnownName("exception"), Is.True);
        Assert.That(LogFormatter.IsKnownName("missing"), Is.False);
    }
}
