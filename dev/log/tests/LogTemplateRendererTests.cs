using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateRendererTests
{
    [Test]
    public void RenderParsedTemplate_RendersLiteralsAndBoundValues()
    {
        string rendered = LogTemplateRenderer.RenderParsedTemplate(
            "Hello {User}, count {Count}",
            new object?[]
            {
                "Ada",
                3
            });

        Assert.That(rendered, Is.EqualTo("Hello Ada, count 3"));
    }

    [Test]
    public void RenderParsedTemplate_RendersEscapedBraces()
    {
        string rendered = LogTemplateRenderer.RenderParsedTemplate("{{Hello}} {User}", new object[] { "Ada" });

        Assert.That(rendered, Is.EqualTo("{Hello} Ada"));
    }

    [Test]
    public void RenderParsedTemplate_RendersMissingValuesAsNullThroughBinder()
    {
        string rendered = LogTemplateRenderer.RenderParsedTemplate("Hello {User} from {Region}", new object[] { "Ada" });

        Assert.That(rendered, Is.EqualTo("Hello Ada from null"));
    }

    [Test]
    public void RenderFields_PreservesUnresolvedPlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Hello {User} from {Region}");
        LogField[] fields = new LogField[]
        {
            LogField.String("User", "Ada")
        };

        LogTemplateRenderer renderer = new LogTemplateRenderer();
        string rendered = renderer.RenderFields(template, fields);

        Assert.That(rendered, Is.EqualTo("Hello Ada from {Region}"));
    }

    [Test]
    public void RenderFields_UsesPositionalFieldsForDuplicatePlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("{User} then {User}");
        LogField[] fields = new LogField[]
        {
            LogField.String("User", "first"),
            LogField.String("User_1", "second")
        };

        LogTemplateRenderer renderer = new LogTemplateRenderer();
        string rendered = renderer.RenderFields(template, fields);

        Assert.That(rendered, Is.EqualTo("first then second"));
    }

    [Test]
    public void RenderFields_UsesReservedFieldNamesForPlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("{level}");
        LogField[] fields = new LogField[]
        {
            LogField.String("field_level", "user-level")
        };

        LogTemplateRenderer renderer = new LogTemplateRenderer();
        string rendered = renderer.RenderFields(template, fields);

        Assert.That(rendered, Is.EqualTo("user-level"));
    }

    [Test]
    public void FormatValue_RendersCommonKindsWithStableText()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 18, 30, 0, TimeSpan.Zero);
        LogValue[] values = new LogValue[]
        {
            LogValue.Null(),
            LogValue.String("text"),
            LogValue.Bool(true),
            LogValue.Integer(5),
            LogValue.Decimal(1.25m),
            LogValue.FloatingPoint(2.5d),
            LogValue.DateTime(timestamp),
            LogValue.Duration(TimeSpan.FromSeconds(3)),
            LogValue.Raw("{\"ok\":true}")
        };

        Assert.That(LogTemplateRenderer.FormatValue(values[0]), Is.EqualTo("null"));
        Assert.That(LogTemplateRenderer.FormatValue(values[1]), Is.EqualTo("text"));
        Assert.That(LogTemplateRenderer.FormatValue(values[2]), Is.EqualTo("True"));
        Assert.That(LogTemplateRenderer.FormatValue(values[3]), Is.EqualTo("5"));
        Assert.That(LogTemplateRenderer.FormatValue(values[4]), Is.EqualTo("1.25"));
        Assert.That(LogTemplateRenderer.FormatValue(values[5]), Is.EqualTo("2.5"));
        Assert.That(LogTemplateRenderer.FormatValue(values[6]), Is.EqualTo("2026-08-02T18:30:00.0000000+00:00"));
        Assert.That(LogTemplateRenderer.FormatValue(values[7]), Is.EqualTo("00:00:03"));
        Assert.That(LogTemplateRenderer.FormatValue(values[8]), Is.EqualTo("{\"ok\":true}"));
    }

    [Test]
    public void FormatValue_RendersArraysAndDictionaries()
    {
        Dictionary<string, LogValue> dictionary = new Dictionary<string, LogValue>();
        dictionary["name"] = LogValue.String("Ada");
        dictionary["count"] = LogValue.Integer(2);

        LogValue array = LogValue.Array(new LogValue[] { LogValue.String("a"), LogValue.Integer(1), LogValue.Null() });
        LogValue map = LogValue.Dictionary(dictionary);

        Assert.That(LogTemplateRenderer.FormatValue(array), Is.EqualTo("[a, 1, null]"));
        Assert.That(LogTemplateRenderer.FormatValue(map), Is.EqualTo("{name: Ada, count: 2}"));
    }

    [Test]
    public void RenderParsedTemplate_RendersNestedValidValues()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 8, 13, 45, 30, TimeSpan.Zero);
        Dictionary<string, object> shipment = new Dictionary<string, object>();
        shipment["id"] = "A123";
        shipment["priority"] = 2;
        shipment["flags"] = new object[] { "fragile", true };

        string rendered = LogTemplateRenderer.RenderParsedTemplate(
            "Shipment {Shipment} tags {Tags} at {Timestamp}",
            new object[]
            {
                shipment,
                new object?[] { "cold", 3, null },
                timestamp
            });

        Assert.That(rendered, Is.EqualTo("Shipment {id: A123, priority: 2, flags: [fragile, True]} tags [cold, 3, null] at 2026-08-08T13:45:30.0000000+00:00"));
    }
}
