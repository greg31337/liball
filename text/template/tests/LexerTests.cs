using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class LexerTests
{
    [Test]
    public void NativeSyntax_MixedTemplate_ProducesExpectedTokenKindSequence()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("Hello {{ user.name }}\n{% if ok %}yes{% endif %}\n{# comment #}");

        TemplateTokenKind[] expected = new TemplateTokenKind[]
        {
            TemplateTokenKind.Text,
            TemplateTokenKind.OutputStartDelimiter,
            TemplateTokenKind.Identifier,
            TemplateTokenKind.Dot,
            TemplateTokenKind.Identifier,
            TemplateTokenKind.OutputEndDelimiter,
            TemplateTokenKind.Text,
            TemplateTokenKind.StatementStartDelimiter,
            TemplateTokenKind.Identifier,
            TemplateTokenKind.Identifier,
            TemplateTokenKind.StatementEndDelimiter,
            TemplateTokenKind.Text,
            TemplateTokenKind.StatementStartDelimiter,
            TemplateTokenKind.Identifier,
            TemplateTokenKind.StatementEndDelimiter,
            TemplateTokenKind.Text,
            TemplateTokenKind.CommentStartDelimiter,
            TemplateTokenKind.CommentText,
            TemplateTokenKind.CommentEndDelimiter,
            TemplateTokenKind.EndOfFile
        };

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertKinds(result, expected);
        Assert.That(result.Tokens[2].Text, Is.EqualTo("user"));
        Assert.That(result.Tokens[4].Text, Is.EqualTo("name"));
        Assert.That(result.Tokens[17].Text, Is.EqualTo(" comment "));
    }

    [Test]
    public void Errors_UnterminatedTagsAndRawBlocks_ReportDiagnostics()
    {
        TemplateLexerResult output = TemplateLexer.LexNative("Hello {{ name");
        TemplateLexerResult comment = TemplateLexer.LexNative("Hello {# comment");
        TemplateLexerResult raw = TemplateLexer.LexNative("{% raw %}{{ still raw }}");

        Assert.That(output.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(output.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        Assert.That(output.Diagnostics[0].Message, Does.Contain("}}"));
        Assert.That(output.Tokens[output.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));

        Assert.That(comment.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(comment.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        Assert.That(comment.Diagnostics[0].Message, Does.Contain("#}"));
        Assert.That(CountKind(comment, TemplateTokenKind.CommentText), Is.EqualTo(1));

        Assert.That(raw.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(raw.Diagnostics[0].Code, Is.EqualTo("LEX002"));
        Assert.That(CountKind(raw, TemplateTokenKind.RawText), Is.EqualTo(1));
    }

    [Test]
    public void TrimMarkers_OutputStatementAndCommentTags_RecordTrimMetadata()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{%- if ok -%}\n{{- value -}}\n{#- comment -#}");

        Assert.That(CountKind(result, TemplateTokenKind.LeftTrimMarker), Is.EqualTo(3));
        Assert.That(CountKind(result, TemplateTokenKind.RightTrimMarker), Is.EqualTo(3));
        Assert.That(result.Tokens[0].Kind, Is.EqualTo(TemplateTokenKind.StatementStartDelimiter));
        Assert.That(result.Tokens[0].TrimLeft, Is.True);
        Assert.That(result.Tokens[5].Kind, Is.EqualTo(TemplateTokenKind.StatementEndDelimiter));
        Assert.That(result.Tokens[5].TrimRight, Is.True);
        Assert.That(result.Tokens[0].StandaloneLine, Is.True);
        Assert.That(result.Tokens[1].StandaloneLine, Is.True);
        Assert.That(result.Tokens[5].StandaloneLine, Is.True);
        Assert.That(result.Tokens[7].Kind, Is.EqualTo(TemplateTokenKind.OutputStartDelimiter));
        Assert.That(result.Tokens[7].StandaloneLine, Is.False);
    }

    [Test]
    public void RawBlocks_DoNotScanNestedOutputStatementOrCommentDelimiters()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{% raw %}{{ value }}{% if ok %}{# note #}{% endraw %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(CountKind(result, TemplateTokenKind.RawBlockStartDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.RawText), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.RawBlockEndDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.OutputStartDelimiter), Is.EqualTo(0));
        Assert.That(CountKind(result, TemplateTokenKind.CommentStartDelimiter), Is.EqualTo(0));
        Assert.That(result.Tokens[1].Text, Is.EqualTo("{{ value }}{% if ok %}{# note #}"));
    }

    [Test]
    public void Comments_DoNotScanNestedOutputOrStatementDelimiters()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("a{# {{ value }} {% if ok %} #}b");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(CountKind(result, TemplateTokenKind.CommentStartDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.CommentText), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.CommentEndDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.OutputStartDelimiter), Is.EqualTo(0));
        Assert.That(CountKind(result, TemplateTokenKind.StatementStartDelimiter), Is.EqualTo(0));
        Assert.That(result.Tokens[2].Text, Is.EqualTo(" {{ value }} {% if ok %} "));
    }

    static void AssertKinds(TemplateLexerResult result, TemplateTokenKind[] expected)
    {
        Assert.That(result.Tokens.Count, Is.EqualTo(expected.Length));

        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(result.Tokens[index].Kind, Is.EqualTo(expected[index]));
            index = index + 1;
        }
    }

    static int CountKind(TemplateLexerResult result, TemplateTokenKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < result.Tokens.Count)
        {
            if (result.Tokens[index].Kind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
