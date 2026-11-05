using System;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateFormatterTests
{
    [Test]
    public void Format_RewritesBlocksWithStableIndentation()
    {
        TemplateFormatter formatter = new TemplateFormatter();

        string formatted = formatter.Format("{% if ok %}Hello {{ name }}{% else %}Bye{% endif %}");

        Assert.That(formatted, Is.EqualTo(
            "{% if ok %}\n" +
            "  Hello\n" +
            "  {{ name }}\n" +
            "{% else %}\n" +
            "  Bye\n" +
            "{% endif %}\n"));
    }

    [Test]
    public void Format_UsesConfiguredIndentAndNewLine()
    {
        TemplateFormatterOptions options = new TemplateFormatterOptions();
        options.IndentText = "    ";
        options.NewLine = "\r\n";
        options.WhitespaceMode = TemplateWhitespaceMode.Preserve;
        TemplateFormatter formatter = TemplateFormatter.Create(options);

        string formatted = formatter.Format("{% for item in items %}{{ item }}{% endfor %}");

        Assert.That(formatted, Is.EqualTo(
            "{% for item in items %}\r\n" +
            "    {{ item }}\r\n" +
            "{% endfor %}\r\n"));
    }

    [Test]
    public void Format_RawBlocksCommentsAndRawOutputsUseCanonicalTags()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateDocumentSyntax document = TemplateParser.ParseNative("{#note#}{% raw %}{{ name }}{% endraw %}").Document;
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Variable, "html");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(0, 1), expression, true, false);
        document.AddChild(TemplateSyntaxNode.FromOutput(output));
        TemplateFormatter formatter = new TemplateFormatter();

        string formatted = formatter.FormatDocument(document);

        Assert.That(formatted, Is.EqualTo(
            "{# note #}\n" +
            "{% raw %}\n" +
            "  {{ name }}\n" +
            "{% endraw %}\n" +
            "{{ html | safe }}\n"));
    }

    [Test]
    public void WhitespaceNormalizer_ConvertsLineEndings()
    {
        TemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();

        Assert.That(normalizer.Normalize("a\r\nb\rc", TemplateWhitespaceMode.NormalizeLineFeeds), Is.EqualTo("a\nb\nc"));
        Assert.That(normalizer.Normalize("a\r\nb\rc\n", TemplateWhitespaceMode.NormalizeCarriageReturnLineFeeds), Is.EqualTo("a\r\nb\r\nc\r\n"));
    }

    [Test]
    public void WhitespaceNormalizer_MinimizesTrailingWhitespaceAndRepeatedBlankLines()
    {
        TemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();

        string normalized = normalizer.Normalize("a  \n  \n\n\nb\t \n", TemplateWhitespaceMode.Minimize);

        Assert.That(normalized, Is.EqualTo("a\n\nb\n"));
    }

    [Test]
    public void FormatterOptions_CloneAndValidate()
    {
        TemplateFormatterOptions options = new TemplateFormatterOptions();
        options.IndentText = "\t";
        options.NewLine = "\r\n";
        options.WhitespaceMode = TemplateWhitespaceMode.Preserve;

        TemplateFormatterOptions clone = options.Clone();
        clone.IndentText = " ";

        Assert.That(options.IndentText, Is.EqualTo("\t"));
        Assert.That(clone.IndentText, Is.EqualTo(" "));
        Assert.That(clone.NewLine, Is.EqualTo("\r\n"));
        Assert.That(clone.WhitespaceMode, Is.EqualTo(TemplateWhitespaceMode.Preserve));
        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void FormatterAndNormalizer_RejectInvalidArguments()
    {
        TemplateFormatter formatter = new TemplateFormatter();
        TemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();
        TemplateFormatterOptions nullIndent = new TemplateFormatterOptions();
        TemplateFormatterOptions nullNewLine = new TemplateFormatterOptions();
        TemplateFormatterOptions emptyNewLine = new TemplateFormatterOptions();
        TemplateDocumentSyntax document = TemplateParser.ParseNative("x").Document;
        nullIndent.IndentText = null!;
        nullNewLine.NewLine = null!;
        emptyNewLine.NewLine = "";

        Assert.Throws<ArgumentNullException>(() => TemplateFormatter.Create(null!));
        Assert.Throws<ArgumentNullException>(() => formatter.Format((string)null!));
        Assert.Throws<ArgumentNullException>(() => formatter.FormatDocument((TemplateDocumentSyntax)null!));
        Assert.Throws<ArgumentNullException>(() => normalizer.Normalize(null!, TemplateWhitespaceMode.Preserve));
        Assert.Throws<ArgumentNullException>(() => normalizer.NormalizeWithOptions("x", null!));
        Assert.Throws<ArgumentNullException>(() => normalizer.ConvertToLineFeeds(null!));
        Assert.Throws<ArgumentNullException>(() => normalizer.ConvertToCarriageReturnLineFeeds(null!));
        Assert.Throws<ArgumentNullException>(() => normalizer.TrimTrailingLineWhitespace(null!));
        Assert.Throws<ArgumentNullException>(() => normalizer.MinimizeWhitespace(null!));
        Assert.Throws<ArgumentNullException>(() => nullIndent.Validate());
        Assert.Throws<ArgumentNullException>(() => nullNewLine.Validate());
        Assert.Throws<ArgumentException>(() => emptyNewLine.Validate());
        Assert.That(document.Children.Count, Is.EqualTo(1));
    }
}
