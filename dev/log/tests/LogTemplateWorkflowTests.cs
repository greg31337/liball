using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateWorkflowTests
{
    [Test]
    public void Parse_TracksTokenPositionsAcrossEscapedBracesAndPlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Start {{literal}} {User:000} end");

        Assert.That(template.IsValid, Is.True);
        Assert.That(template.Tokens.Length, Is.EqualTo(3));
        Assert.That(template.Tokens[0].IsLiteral, Is.True);
        Assert.That(template.Tokens[0].Text, Is.EqualTo("Start {literal} "));
        Assert.That(template.Tokens[0].StartIndex, Is.EqualTo(0));
        Assert.That(template.Tokens[0].Length, Is.EqualTo(18));
        Assert.That(template.Tokens[1].IsPlaceholder, Is.True);
        Assert.That(template.Tokens[1].Name, Is.EqualTo("User"));
        Assert.That(template.Tokens[1].Format, Is.EqualTo("000"));
        Assert.That(template.Tokens[1].StartIndex, Is.EqualTo(18));
        Assert.That(template.Tokens[2].Text, Is.EqualTo(" end"));
    }

    [Test]
    public void Parse_MarksNestedOpeningBraceAsInvalidPlaceholder()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Hello {Outer {Inner}} tail");

        Assert.That(template.IsValid, Is.False);
        Assert.That(template.InvalidTokenCount, Is.EqualTo(1));
        Assert.That(template.Tokens[1].IsPlaceholder, Is.True);
        Assert.That(template.Tokens[1].IsValid, Is.False);
    }

    [Test]
    public void Bind_DoesNotConsumeValuesForInvalidPlaceholders()
    {
        LogTemplate template = new LogTemplate(
            "{bad name} {Good}",
            new LogTemplateToken[]
            {
                LogTemplateToken.Placeholder("bad name", 0, 0, 10),
                LogTemplateToken.Placeholder("Good", 1, 11, 6)
            });

        LogField[] fields = LogTemplateBinder.BindTemplate(template, new object[] { "first", "second" });

        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("Good"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("first"));
    }

    [Test]
    public void BindAndRender_SkipMultipleInvalidPlaceholdersWithoutConsumingValues()
    {
        LogTemplate template = new LogTemplate(
            "bad {bad name} ok {Good} empty {} tail {Also_Good}",
            new LogTemplateToken[]
            {
                LogTemplateToken.Literal("bad ", 0),
                LogTemplateToken.Placeholder("bad name", 0, 4, 10),
                LogTemplateToken.Literal(" ok ", 14),
                LogTemplateToken.Placeholder("Good", 1, 18, 6),
                LogTemplateToken.Literal(" empty ", 24),
                LogTemplateToken.Placeholder("", 2, 31, 2),
                LogTemplateToken.Literal(" tail ", 33),
                LogTemplateToken.Placeholder("Also_Good", 3, 39, 11)
            });
        LogTemplateRenderer renderer = new LogTemplateRenderer();

        LogField[] fields = LogTemplateBinder.BindTemplate(template, new object[] { "first", "second", "ignored" });
        string rendered = renderer.RenderFields(template, fields);

        Assert.That(template.IsValid, Is.False);
        Assert.That(template.InvalidTokenCount, Is.EqualTo(2));
        Assert.That(fields.Length, Is.EqualTo(2));
        Assert.That(fields[0].Name, Is.EqualTo("Good"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("first"));
        Assert.That(fields[1].Name, Is.EqualTo("Also_Good"));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("second"));
        Assert.That(rendered, Is.EqualTo("bad {bad name} ok first empty {} tail second"));
    }

    [Test]
    public void Bind_ResolvesDuplicateNamesAndExistingSuffixCollisions()
    {
        LogField[] fields = LogTemplateBinder.BindParsedTemplate(
            "{User} {User_1} {User} {User_1}",
            new object[] { "first", "existing", "second", "third" });

        Assert.That(fields.Length, Is.EqualTo(4));
        Assert.That(fields[0].Name, Is.EqualTo("User"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("first"));
        Assert.That(fields[1].Name, Is.EqualTo("User_1"));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("existing"));
        Assert.That(fields[2].Name, Is.EqualTo("User_2"));
        Assert.That(fields[2].Value.TextValue, Is.EqualTo("second"));
        Assert.That(fields[3].Name, Is.EqualTo("User_1_1"));
        Assert.That(fields[3].Value.TextValue, Is.EqualTo("third"));
    }

    [Test]
    public void Hints_DoNotPreventBindingOrRendering()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Order {@Order} as {$Text} total {Total:0.00}");
        LogField[] fields = LogTemplateBinder.BindTemplate(template, new object[] { "A123", "shown", 12.5m });
        LogTemplateRenderer renderer = new LogTemplateRenderer();

        string rendered = renderer.RenderFields(template, fields);

        Assert.That(template.Tokens[1].IsDestructured, Is.True);
        Assert.That(template.Tokens[3].IsStringified, Is.True);
        Assert.That(template.Tokens[5].HasFormat, Is.True);
        Assert.That(fields[0].Name, Is.EqualTo("Order"));
        Assert.That(fields[1].Name, Is.EqualTo("Text"));
        Assert.That(fields[2].Name, Is.EqualTo("Total"));
        Assert.That(rendered, Is.EqualTo("Order A123 as shown total 12.5"));
    }

    [Test]
    public void Render_PreservesEscapedAndUnresolvedPlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("{{Start}} {Known} {Missing} {{End}}");
        LogField[] fields = new LogField[]
        {
            LogField.String("Known", "value")
        };
        LogTemplateRenderer renderer = new LogTemplateRenderer();

        string rendered = renderer.RenderFields(template, fields);

        Assert.That(rendered, Is.EqualTo("{Start} value {Missing} {End}"));
    }

    [Test]
    public void Logger_Write_BindsRendersAndStoresEscapedTemplateFields()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);

        LogResult result = logger.InfoWithValues("{{Start}} {User} {User} {Count:000}", new object[] { "Ada", "Grace", 7 });
        LogEvent logEvent = MemoryLogSink.Events(sink)[0];

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("{{Start}} {User} {User} {Count:000}"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("{Start} Ada Grace 7"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(3));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("User"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("User_1"));
        Assert.That(logEvent.Fields[2].Name, Is.EqualTo("Count"));
    }
}
