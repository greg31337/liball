using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateParserTests
{
    [Test]
    public void Parse_ReturnsSingleLiteralForPlainText()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("plain message");

        Assert.That(template.IsValid, Is.True);
        Assert.That(template.Tokens.Length, Is.EqualTo(1));
        Assert.That(template.Tokens[0].IsLiteral, Is.True);
        Assert.That(template.Tokens[0].Text, Is.EqualTo("plain message"));
        Assert.That(template.PlaceholderCount, Is.EqualTo(0));
    }

    [Test]
    public void Parse_SplitsLiteralAndPlaceholderTokens()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Hello {User}");

        Assert.That(template.IsValid, Is.True);
        Assert.That(template.Tokens.Length, Is.EqualTo(2));
        Assert.That(template.Tokens[0].Text, Is.EqualTo("Hello "));
        Assert.That(template.Tokens[1].IsPlaceholder, Is.True);
        Assert.That(template.Tokens[1].Name, Is.EqualTo("User"));
        Assert.That(template.Tokens[1].Position, Is.EqualTo(0));
        Assert.That(template.PlaceholderCount, Is.EqualTo(1));
    }

    [Test]
    public void Parse_HandlesEscapedLiteralBraces()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("{{Hello}} {User}");

        Assert.That(template.IsValid, Is.True);
        Assert.That(template.Tokens.Length, Is.EqualTo(2));
        Assert.That(template.Tokens[0].IsLiteral, Is.True);
        Assert.That(template.Tokens[0].Text, Is.EqualTo("{Hello} "));
        Assert.That(template.Tokens[1].Name, Is.EqualTo("User"));
    }

    [Test]
    public void Parse_StoresFormatHints()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Order {OrderId:0000}");

        Assert.That(template.IsValid, Is.True);
        Assert.That(template.Tokens[1].Name, Is.EqualTo("OrderId"));
        Assert.That(template.Tokens[1].Format, Is.EqualTo("0000"));
        Assert.That(template.Tokens[1].HasFormat, Is.True);
    }

    [Test]
    public void Parse_StoresDestructureAndStringifyHints()
    {
        LogTemplate destructured = LogTemplateParser.ParseTemplate("Order {@Order}");
        LogTemplate stringified = LogTemplateParser.ParseTemplate("Order {$Order}");

        Assert.That(destructured.Tokens[1].Name, Is.EqualTo("Order"));
        Assert.That(destructured.Tokens[1].IsDestructured, Is.True);
        Assert.That(destructured.Tokens[1].IsStringified, Is.False);
        Assert.That(stringified.Tokens[1].Name, Is.EqualTo("Order"));
        Assert.That(stringified.Tokens[1].IsStringified, Is.True);
        Assert.That(stringified.Tokens[1].IsDestructured, Is.False);
    }

    [Test]
    public void Parse_MarksMalformedTemplatesInvalid()
    {
        LogTemplate missingClose = LogTemplateParser.ParseTemplate("Hello {User");
        LogTemplate emptyName = LogTemplateParser.ParseTemplate("Hello {}");
        LogTemplate strayClose = LogTemplateParser.ParseTemplate("Hello }");

        Assert.That(missingClose.IsValid, Is.False);
        Assert.That(emptyName.IsValid, Is.False);
        Assert.That(strayClose.IsValid, Is.False);
        Assert.That(missingClose.InvalidTokenCount, Is.EqualTo(1));
        Assert.That(emptyName.InvalidTokenCount, Is.EqualTo(1));
        Assert.That(strayClose.InvalidTokenCount, Is.EqualTo(1));
    }

    [Test]
    public void Parse_MarksMultipleMalformedSegmentsInvalidWhileKeepingValidPlaceholders()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Start {Bad Name} ok {Good} stray } empty {} nested {Outer {Inner}}");
        LogTemplateToken[] placeholders = template.GetPlaceholders();

        Assert.That(template.IsValid, Is.False);
        Assert.That(template.ErrorMessage, Is.EqualTo("template contains invalid tokens"));
        Assert.That(template.InvalidTokenCount, Is.EqualTo(4));
        Assert.That(template.PlaceholderCount, Is.EqualTo(4));
        Assert.That(placeholders.Length, Is.EqualTo(4));
        Assert.That(placeholders[0].Name, Is.EqualTo("Bad Name"));
        Assert.That(placeholders[0].IsValid, Is.False);
        Assert.That(placeholders[1].Name, Is.EqualTo("Good"));
        Assert.That(placeholders[1].IsValid, Is.True);
        Assert.That(placeholders[2].Name, Is.EqualTo(""));
        Assert.That(placeholders[2].IsValid, Is.False);
        Assert.That(placeholders[3].Name, Is.EqualTo(""));
        Assert.That(placeholders[3].IsValid, Is.False);
    }
}
