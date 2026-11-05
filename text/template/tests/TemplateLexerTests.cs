using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateLexerTests
{
    [Test]
    public void Lex_PlainText_EmitsTextAndEof()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("Hello world");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Tokens.Count, Is.EqualTo(2));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "Hello world", 0, 11);
        AssertToken(result.Tokens[1], TemplateTokenKind.EndOfFile, "", 11, 0);
    }

    [Test]
    public void Lex_OutputTag_EmitsTextDelimitersIdentifierAndEof()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("Hello {{ name }}!");

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "Hello ", 0, 6);
        AssertToken(result.Tokens[1], TemplateTokenKind.OutputStartDelimiter, "{{", 6, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.Identifier, "name", 9, 4);
        AssertToken(result.Tokens[3], TemplateTokenKind.OutputEndDelimiter, "}}", 14, 2);
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, "!", 16, 1);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 17, 0);
    }

    [Test]
    public void Lex_StatementTags_EmitsStatementDelimitersAndTextBetweenTags()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{% if enabled %}yes{% endif %}");

        Assert.That(result.Tokens.Count, Is.EqualTo(9));
        AssertToken(result.Tokens[0], TemplateTokenKind.StatementStartDelimiter, "{%", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.Identifier, "if", 3, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.Identifier, "enabled", 6, 7);
        AssertToken(result.Tokens[3], TemplateTokenKind.StatementEndDelimiter, "%}", 14, 2);
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, "yes", 16, 3);
        AssertToken(result.Tokens[5], TemplateTokenKind.StatementStartDelimiter, "{%", 19, 2);
        AssertToken(result.Tokens[6], TemplateTokenKind.Identifier, "endif", 22, 5);
        AssertToken(result.Tokens[7], TemplateTokenKind.StatementEndDelimiter, "%}", 28, 2);
        AssertToken(result.Tokens[8], TemplateTokenKind.EndOfFile, "", 30, 0);
    }

    [Test]
    public void Lex_CommentDelimiters_AreRecognizedAsNativeTagDelimiters()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("a{# note #}b");

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "a", 0, 1);
        AssertToken(result.Tokens[1], TemplateTokenKind.CommentStartDelimiter, "{#", 1, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.CommentText, " note ", 3, 6);
        AssertToken(result.Tokens[3], TemplateTokenKind.CommentEndDelimiter, "#}", 9, 2);
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, "b", 11, 1);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 12, 0);
    }

    [Test]
    public void Lex_TagContents_EmitsLiteralsPunctuationAndOperators()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{ value | default: \"x\" == true }}");

        Assert.That(result.Tokens.Count, Is.EqualTo(10));
        AssertToken(result.Tokens[0], TemplateTokenKind.OutputStartDelimiter, "{{", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.Identifier, "value", 3, 5);
        AssertToken(result.Tokens[2], TemplateTokenKind.Pipe, "|", 9, 1);
        AssertToken(result.Tokens[3], TemplateTokenKind.Identifier, "default", 11, 7);
        AssertToken(result.Tokens[4], TemplateTokenKind.Colon, ":", 18, 1);
        AssertToken(result.Tokens[5], TemplateTokenKind.StringLiteral, "\"x\"", 20, 3);
        AssertToken(result.Tokens[6], TemplateTokenKind.Operator, "==", 24, 2);
        AssertToken(result.Tokens[7], TemplateTokenKind.BooleanLiteral, "true", 27, 4);
        AssertToken(result.Tokens[8], TemplateTokenKind.OutputEndDelimiter, "}}", 32, 2);
        AssertToken(result.Tokens[9], TemplateTokenKind.EndOfFile, "", 34, 0);
    }

    [Test]
    public void Lex_NumberNullEmptyAndOperatorTokens()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{ 12.5 + null != empty }}");

        Assert.That(result.Tokens.Count, Is.EqualTo(8));
        AssertToken(result.Tokens[0], TemplateTokenKind.OutputStartDelimiter, "{{", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.NumberLiteral, "12.5", 3, 4);
        AssertToken(result.Tokens[2], TemplateTokenKind.Operator, "+", 8, 1);
        AssertToken(result.Tokens[3], TemplateTokenKind.NullLiteral, "null", 10, 4);
        AssertToken(result.Tokens[4], TemplateTokenKind.Operator, "!=", 15, 2);
        AssertToken(result.Tokens[5], TemplateTokenKind.EmptyLiteral, "empty", 18, 5);
        AssertToken(result.Tokens[6], TemplateTokenKind.OutputEndDelimiter, "}}", 24, 2);
        AssertToken(result.Tokens[7], TemplateTokenKind.EndOfFile, "", 26, 0);
    }

    [Test]
    public void Lex_OutputTagWithTrimMarkers_EmitsTrimTokensAndMetadata()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("A {{- name -}} B");

        Assert.That(result.Tokens.Count, Is.EqualTo(8));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "A ", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.OutputStartDelimiter, "{{", 2, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.LeftTrimMarker, "-", 4, 1);
        AssertToken(result.Tokens[3], TemplateTokenKind.Identifier, "name", 6, 4);
        AssertToken(result.Tokens[4], TemplateTokenKind.RightTrimMarker, "-", 11, 1);
        AssertToken(result.Tokens[5], TemplateTokenKind.OutputEndDelimiter, "}}", 12, 2);
        AssertToken(result.Tokens[6], TemplateTokenKind.Text, " B", 14, 2);
        AssertToken(result.Tokens[7], TemplateTokenKind.EndOfFile, "", 16, 0);
        Assert.That(result.Tokens[1].TrimLeft, Is.True);
        Assert.That(result.Tokens[2].TrimLeft, Is.True);
        Assert.That(result.Tokens[4].TrimRight, Is.True);
        Assert.That(result.Tokens[5].TrimRight, Is.True);
        Assert.That(result.Tokens[1].StandaloneLine, Is.False);
    }

    [Test]
    public void Lex_StatementTagOnWhitespaceOnlyLine_MarksTagTokensStandalone()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("before\n  {% if ok %}  \nafter");

        Assert.That(result.Tokens.Count, Is.EqualTo(7));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "before\n  ", 0, 9);
        AssertToken(result.Tokens[1], TemplateTokenKind.StatementStartDelimiter, "{%", 9, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.Identifier, "if", 12, 2);
        AssertToken(result.Tokens[3], TemplateTokenKind.Identifier, "ok", 15, 2);
        AssertToken(result.Tokens[4], TemplateTokenKind.StatementEndDelimiter, "%}", 18, 2);
        AssertToken(result.Tokens[5], TemplateTokenKind.Text, "  \nafter", 20, 8);
        AssertToken(result.Tokens[6], TemplateTokenKind.EndOfFile, "", 28, 0);
        Assert.That(result.Tokens[1].StandaloneLine, Is.True);
        Assert.That(result.Tokens[2].StandaloneLine, Is.True);
        Assert.That(result.Tokens[3].StandaloneLine, Is.True);
        Assert.That(result.Tokens[4].StandaloneLine, Is.True);
        Assert.That(result.Tokens[0].StandaloneLine, Is.False);
        Assert.That(result.Tokens[5].StandaloneLine, Is.False);
    }

    [Test]
    public void Lex_CommentTagOnWhitespaceOnlyLine_MarksTagTokensStandalone()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("x\n\t{# note #}\r\ny");

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "x\n\t", 0, 3);
        AssertToken(result.Tokens[1], TemplateTokenKind.CommentStartDelimiter, "{#", 3, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.CommentText, " note ", 5, 6);
        AssertToken(result.Tokens[3], TemplateTokenKind.CommentEndDelimiter, "#}", 11, 2);
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, "\r\ny", 13, 3);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 16, 0);
        Assert.That(result.Tokens[1].StandaloneLine, Is.True);
        Assert.That(result.Tokens[2].StandaloneLine, Is.True);
        Assert.That(result.Tokens[3].StandaloneLine, Is.True);
    }

    [Test]
    public void Lex_StatementTagWithSameLineContent_IsNotStandalone()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("x {% if ok %}\n");

        Assert.That(result.Tokens[1].Kind, Is.EqualTo(TemplateTokenKind.StatementStartDelimiter));
        Assert.That(result.Tokens[1].StandaloneLine, Is.False);
        Assert.That(result.Tokens[2].StandaloneLine, Is.False);
        Assert.That(result.Tokens[4].StandaloneLine, Is.False);
    }

    [Test]
    public void Lex_WhenProfileDisablesTrimMarkers_TreatsDashesAsOperators()
    {
        TemplateSource source = TemplateSource.FromString("{{- name -}}");
        TemplateProfile profile = NativeProfile.Create();
        profile.SupportsWhitespaceTrim = false;
        TemplateLexer lexer = TemplateLexer.Create(source, profile);

        TemplateLexerResult result = lexer.Lex();

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.OutputStartDelimiter, "{{", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.Operator, "-", 2, 1);
        AssertToken(result.Tokens[2], TemplateTokenKind.Identifier, "name", 4, 4);
        AssertToken(result.Tokens[3], TemplateTokenKind.Operator, "-", 9, 1);
        AssertToken(result.Tokens[4], TemplateTokenKind.OutputEndDelimiter, "}}", 10, 2);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 12, 0);
    }

    [Test]
    public void Lex_CommentWithTrimMarkers_EmitsCommentTextAndTrimMetadata()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{#- {{ ignored }} -#}");

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.CommentStartDelimiter, "{#", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.LeftTrimMarker, "-", 2, 1);
        AssertToken(result.Tokens[2], TemplateTokenKind.CommentText, " {{ ignored }} ", 3, 15);
        AssertToken(result.Tokens[3], TemplateTokenKind.RightTrimMarker, "-", 18, 1);
        AssertToken(result.Tokens[4], TemplateTokenKind.CommentEndDelimiter, "#}", 19, 2);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 21, 0);
        Assert.That(result.Tokens[0].TrimLeft, Is.True);
        Assert.That(result.Tokens[1].TrimLeft, Is.True);
        Assert.That(result.Tokens[3].TrimRight, Is.True);
        Assert.That(result.Tokens[4].TrimRight, Is.True);
    }

    [Test]
    public void Lex_RawBlock_EmitsRawTextWithoutScanningNestedDelimiters()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("A {% raw %}{{ name }}{% endraw %} B");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "A ", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.RawBlockStartDelimiter, "{% raw %}", 2, 9);
        AssertToken(result.Tokens[2], TemplateTokenKind.RawText, "{{ name }}", 11, 10);
        AssertToken(result.Tokens[3], TemplateTokenKind.RawBlockEndDelimiter, "{% endraw %}", 21, 12);
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, " B", 33, 2);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 35, 0);
    }

    [Test]
    public void Lex_RawBlockWithTrimMarkers_PreservesTrimMetadataOnRawTags()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{%- raw -%}x{%- endraw -%}");

        Assert.That(result.Tokens.Count, Is.EqualTo(4));
        AssertToken(result.Tokens[0], TemplateTokenKind.RawBlockStartDelimiter, "{%- raw -%}", 0, 11);
        AssertToken(result.Tokens[1], TemplateTokenKind.RawText, "x", 11, 1);
        AssertToken(result.Tokens[2], TemplateTokenKind.RawBlockEndDelimiter, "{%- endraw -%}", 12, 14);
        AssertToken(result.Tokens[3], TemplateTokenKind.EndOfFile, "", 26, 0);
        Assert.That(result.Tokens[0].TrimLeft, Is.True);
        Assert.That(result.Tokens[0].TrimRight, Is.True);
        Assert.That(result.Tokens[2].TrimLeft, Is.True);
        Assert.That(result.Tokens[2].TrimRight, Is.True);
    }

    [Test]
    public void Lex_EscapedDelimiters_EmitTextAndDoNotOpenTags()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("A \\{{ name }} and \\{% if ok %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "A ", 0, 2);
        Assert.That(result.Tokens[1].Kind, Is.EqualTo(TemplateTokenKind.Text));
        Assert.That(result.Tokens[1].Text, Is.EqualTo("{{"));
        Assert.That(result.Tokens[1].Span.Start, Is.EqualTo(2));
        Assert.That(result.Tokens[1].Span.Length, Is.EqualTo(3));
        Assert.That(result.Tokens[1].Span.GetText(), Is.EqualTo("\\{{"));
        AssertToken(result.Tokens[2], TemplateTokenKind.Text, " name }} and ", 5, 13);
        Assert.That(result.Tokens[3].Kind, Is.EqualTo(TemplateTokenKind.Text));
        Assert.That(result.Tokens[3].Text, Is.EqualTo("{%"));
        Assert.That(result.Tokens[3].Span.Start, Is.EqualTo(18));
        Assert.That(result.Tokens[3].Span.Length, Is.EqualTo(3));
        Assert.That(result.Tokens[3].Span.GetText(), Is.EqualTo("\\{%"));
        AssertToken(result.Tokens[4], TemplateTokenKind.Text, " if ok %}", 21, 9);
        AssertToken(result.Tokens[5], TemplateTokenKind.EndOfFile, "", 30, 0);
        Assert.That(result.Tokens[1].Escaped, Is.True);
        Assert.That(result.Tokens[3].Escaped, Is.True);
    }

    [Test]
    public void Lex_EscapedDelimiters_UseOddBackslashParity()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("\\\\{{ name }}|\\\\\\{{ name }}");

        Assert.That(CountKind(result, TemplateTokenKind.OutputStartDelimiter), Is.EqualTo(1));
        Assert.That(CountEscapedTokens(result), Is.EqualTo(1));
        TemplateToken escaped = FindEscapedToken(result);
        Assert.That(escaped.Text, Is.EqualTo("{{"));
        Assert.That(escaped.Span.GetText(), Is.EqualTo("\\{{"));
    }

    [Test]
    public void Lex_VerbatimBlock_UsesRawTokensAndDoesNotScanNestedTags()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("A {% verbatim %}{{ name }}{# note #}{% endverbatim %} B");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(CountKind(result, TemplateTokenKind.RawBlockStartDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.RawBlockEndDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.OutputStartDelimiter), Is.EqualTo(0));
        Assert.That(FindToken(result, TemplateTokenKind.RawText, 0).Text, Is.EqualTo("{{ name }}{# note #}"));
    }

    [Test]
    public void Lex_GreedyAndNonGreedyTrimMarkers_RecordDistinctKinds()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{- first -}} {{~ second ~}}");
        TemplateToken firstStart = FindToken(result, TemplateTokenKind.OutputStartDelimiter, 0);
        TemplateToken firstLeft = FindToken(result, TemplateTokenKind.LeftTrimMarker, 0);
        TemplateToken firstRight = FindToken(result, TemplateTokenKind.RightTrimMarker, 0);
        TemplateToken secondStart = FindToken(result, TemplateTokenKind.OutputStartDelimiter, 1);
        TemplateToken secondLeft = FindToken(result, TemplateTokenKind.LeftTrimMarker, 1);
        TemplateToken secondRight = FindToken(result, TemplateTokenKind.RightTrimMarker, 1);

        Assert.That(firstStart.LeftTrimKind, Is.EqualTo(TemplateTrimKind.Greedy));
        Assert.That(firstLeft.LeftTrimKind, Is.EqualTo(TemplateTrimKind.Greedy));
        Assert.That(firstRight.RightTrimKind, Is.EqualTo(TemplateTrimKind.Greedy));
        Assert.That(secondStart.LeftTrimKind, Is.EqualTo(TemplateTrimKind.NonGreedy));
        Assert.That(secondLeft.LeftTrimKind, Is.EqualTo(TemplateTrimKind.NonGreedy));
        Assert.That(secondRight.RightTrimKind, Is.EqualTo(TemplateTrimKind.NonGreedy));
        Assert.That(FindToken(result, TemplateTokenKind.OutputEndDelimiter, 1).RightTrimKind, Is.EqualTo(TemplateTrimKind.NonGreedy));
    }

    [Test]
    public void Lex_ProfileCanDisableStandaloneTagMetadata()
    {
        TemplateProfile profile = new TemplateProfileCatalog().CreateNativeProfile();
        profile.StandaloneStatementTrimming = false;
        TemplateLexer lexer = new TemplateLexer(new TemplateSource("standalone", null, null, null, "  {% if ok %}  \n"));
        lexer.Configure(profile);

        TemplateLexerResult result = lexer.Lex();

        Assert.That(FindToken(result, TemplateTokenKind.StatementStartDelimiter, 0).StandaloneLine, Is.False);
        Assert.That(FindToken(result, TemplateTokenKind.Identifier, 0).StandaloneLine, Is.False);
        Assert.That(FindToken(result, TemplateTokenKind.StatementEndDelimiter, 0).StandaloneLine, Is.False);
    }

    [Test]
    public void Lex_MustacheDelimiterChange_UpdatesSubsequentOutputAndCommentDelimiters()
    {
        TemplateProfile profile = new TemplateProfileCatalog().CreateMustacheProfile();
        TemplateLexer lexer = new TemplateLexer(new TemplateSource("mustache", null, null, null, "{{=<% %>=}}<% name %><%! note %>"));
        lexer.Configure(profile);

        TemplateLexerResult result = lexer.Lex();

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertToken(result.Tokens[0], TemplateTokenKind.DelimiterChange, "{{=<% %>=}}", 0, 11);
        Assert.That(result.Tokens[0].StandaloneLine, Is.False);
        Assert.That(FindToken(result, TemplateTokenKind.OutputStartDelimiter, 0).Text, Is.EqualTo("<%"));
        Assert.That(FindToken(result, TemplateTokenKind.OutputEndDelimiter, 0).Text, Is.EqualTo("%>"));
        Assert.That(FindToken(result, TemplateTokenKind.CommentStartDelimiter, 0).Text, Is.EqualTo("<%!"));
        Assert.That(FindToken(result, TemplateTokenKind.CommentText, 0).Text, Is.EqualTo(" note "));
    }

    [Test]
    public void Lex_InvalidMustacheDelimiterChange_ReportsDiagnosticAndRecoversToEof()
    {
        TemplateProfile profile = new TemplateProfileCatalog().CreateMustacheProfile();
        TemplateLexer lexer = new TemplateLexer(new TemplateSource("mustache", null, null, null, "{{= <% =}}tail"));
        lexer.Configure(profile);

        TemplateLexerResult result = lexer.Lex();

        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX003"));
        Assert.That(result.Tokens[0].Kind, Is.EqualTo(TemplateTokenKind.DelimiterChange));
        Assert.That(FindToken(result, TemplateTokenKind.Text, 0).Text, Is.EqualTo("tail"));
        Assert.That(result.Tokens[result.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));
    }

    [Test]
    public void Lex_UnicodeScalars_AreNeverSplitAcrossTokens()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{ 𐐀name 😀 }}");
        TemplateToken identifier = FindToken(result, TemplateTokenKind.Identifier, 0);
        TemplateToken unknown = FindToken(result, TemplateTokenKind.Unknown, 0);

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(identifier.Text, Is.EqualTo("𐐀name"));
        Assert.That(identifier.Span.Length, Is.EqualTo(6));
        Assert.That(unknown.Text, Is.EqualTo("😀"));
        Assert.That(unknown.Span.Length, Is.EqualTo(2));
    }

    [Test]
    public void Lex_InvalidSurrogates_ReportDiagnosticsAndContinue()
    {
        string text = "{{ \ud800 value \udc00 }}";

        TemplateLexerResult result = TemplateLexer.LexNative(text);

        Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX004"));
        Assert.That(result.Diagnostics[1].Code, Is.EqualTo("LEX004"));
        Assert.That(CountKind(result, TemplateTokenKind.Unknown), Is.EqualTo(2));
        Assert.That(FindToken(result, TemplateTokenKind.Identifier, 0).Text, Is.EqualTo("value"));
        Assert.That(result.Tokens[result.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));
    }

    [Test]
    public void Lex_MultipleMalformedTags_ReportMultipleDiagnosticsAndContinue()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{ first {% second {# third");

        Assert.That(result.Diagnostics.Count, Is.EqualTo(3));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        Assert.That(result.Diagnostics[1].Code, Is.EqualTo("LEX001"));
        Assert.That(result.Diagnostics[2].Code, Is.EqualTo("LEX001"));
        Assert.That(CountKind(result, TemplateTokenKind.OutputStartDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.StatementStartDelimiter), Is.EqualTo(1));
        Assert.That(CountKind(result, TemplateTokenKind.CommentStartDelimiter), Is.EqualTo(1));
        Assert.That(result.Tokens[result.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));
    }

    [Test]
    public void Lex_PublicTokenKinds_AreProducedByBehavioralInputs()
    {
        List<TemplateLexerResult> results = new List<TemplateLexerResult>();
        results.Add(TemplateLexer.LexNative("text {{- value.name, : ; | ? ( ) [ ] { } = \"x\" 1 true null empty + 😀 -}} {% if ok %} {# note #} {% raw %}raw{% endraw %}"));
        TemplateProfile mustache = new TemplateProfileCatalog().CreateMustacheProfile();
        TemplateLexer mustacheLexer = new TemplateLexer(new TemplateSource("mustache", null, null, null, "{{=<% %>=}}"));
        mustacheLexer.Configure(mustache);
        results.Add(mustacheLexer.Lex());
        HashSet<TemplateTokenKind> produced = new HashSet<TemplateTokenKind>();
        int resultIndex = 0;
        while (resultIndex < results.Count)
        {
            int tokenIndex = 0;
            while (tokenIndex < results[resultIndex].Tokens.Count)
            {
                produced.Add(results[resultIndex].Tokens[tokenIndex].Kind);
                tokenIndex = tokenIndex + 1;
            }

            resultIndex = resultIndex + 1;
        }

        TemplateTokenKind[] kinds = Enum.GetValues<TemplateTokenKind>();
        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            Assert.That(produced.Contains(kinds[kindIndex]), Is.True, "No behavioral input produced " + kinds[kindIndex].ToString());
            kindIndex = kindIndex + 1;
        }
    }

    [Test]
    public void Lex_UnterminatedOutputTag_AddsDiagnosticAndEof()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("Hello {{ name");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(TemplateDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Span!.GetText(), Is.EqualTo("{{ name"));
        Assert.That(result.Tokens[result.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));
    }

    [Test]
    public void Lex_UnterminatedComment_AddsDiagnosticAndKeepsCommentText()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("A {# no close");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        AssertToken(result.Tokens[0], TemplateTokenKind.Text, "A ", 0, 2);
        AssertToken(result.Tokens[1], TemplateTokenKind.CommentStartDelimiter, "{#", 2, 2);
        AssertToken(result.Tokens[2], TemplateTokenKind.CommentText, " no close", 4, 9);
        AssertToken(result.Tokens[3], TemplateTokenKind.EndOfFile, "", 13, 0);
    }

    [Test]
    public void Lex_UnterminatedRawBlock_AddsDiagnosticAndKeepsRawText()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{% raw %}{{ name }}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX002"));
        AssertToken(result.Tokens[0], TemplateTokenKind.RawBlockStartDelimiter, "{% raw %}", 0, 9);
        AssertToken(result.Tokens[1], TemplateTokenKind.RawText, "{{ name }}", 9, 10);
        AssertToken(result.Tokens[2], TemplateTokenKind.EndOfFile, "", 19, 0);
    }

    [Test]
    public void Constructor_RejectsMissingSourceOrProfile()
    {
        TemplateSource source = TemplateSource.FromString("");

        Assert.Throws<ArgumentNullException>(() => new TemplateLexer(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateLexer.Create(source, null!));
    }

    static void AssertToken(TemplateToken token, TemplateTokenKind kind, string text, int start, int length)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
        Assert.That(token.Span.Start, Is.EqualTo(start));
        Assert.That(token.Span.Length, Is.EqualTo(length));
        Assert.That(token.Span.GetText(), Is.EqualTo(text));
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

    static TemplateToken FindToken(TemplateLexerResult result, TemplateTokenKind kind, int occurrence)
    {
        int found = 0;
        int index = 0;
        while (index < result.Tokens.Count)
        {
            if (result.Tokens[index].Kind == kind)
            {
                if (found == occurrence)
                {
                    return result.Tokens[index];
                }

                found = found + 1;
            }

            index = index + 1;
        }

        Assert.Fail("Token kind " + kind.ToString() + " occurrence " + occurrence.ToString() + " was not found.");
        return result.Tokens[0];
    }

    static int CountEscapedTokens(TemplateLexerResult result)
    {
        int count = 0;
        int index = 0;
        while (index < result.Tokens.Count)
        {
            if (result.Tokens[index].Escaped)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static TemplateToken FindEscapedToken(TemplateLexerResult result)
    {
        int index = 0;
        while (index < result.Tokens.Count)
        {
            if (result.Tokens[index].Escaped)
            {
                return result.Tokens[index];
            }

            index = index + 1;
        }

        Assert.Fail("Escaped token was not found.");
        return result.Tokens[0];
    }
}
