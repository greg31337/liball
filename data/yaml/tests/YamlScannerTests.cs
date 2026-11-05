using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlScannerTests
{
    [Test]
    public void Scan_EmptyInput_EmitsStreamStartAndEnd()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Tokens.Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.StreamStart,
            global::yaml.YamlTokenKind.StreamEnd
        }));
    }

    [Test]
    public void Scan_DocumentMarkersAndComments_EmitsExpectedTokensAndSpans()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("---\n# top\n...\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.StreamStart,
            global::yaml.YamlTokenKind.DocumentStart,
            global::yaml.YamlTokenKind.Comment,
            global::yaml.YamlTokenKind.DocumentEnd,
            global::yaml.YamlTokenKind.StreamEnd
        }));

        global::yaml.YamlToken documentStart = result.Tokens[1];
        global::yaml.YamlToken comment = result.Tokens[2];
        global::yaml.YamlToken documentEnd = result.Tokens[3];

        Assert.That(documentStart.Value, Is.EqualTo("---"));
        Assert.That(documentStart.Span.StartLine, Is.EqualTo(1));
        Assert.That(documentStart.Span.StartColumn, Is.EqualTo(1));
        Assert.That(documentStart.Span.EndColumn, Is.EqualTo(4));
        Assert.That(comment.Value, Is.EqualTo("# top"));
        Assert.That(comment.Span.StartLine, Is.EqualTo(2));
        Assert.That(documentEnd.Value, Is.EqualTo("..."));
        Assert.That(documentEnd.Span.StartLine, Is.EqualTo(3));
    }

    [Test]
    public void Scan_WhitespaceAndCommentOption_PreservesWhitespaceAndCanOmitComments()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("key  # hidden\n", options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.Comment), Is.False);
        global::yaml.YamlToken whitespace = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Whitespace);
        Assert.That(whitespace.Value, Is.EqualTo("  "));
        Assert.That(whitespace.Span.StartLine, Is.EqualTo(1));
        Assert.That(whitespace.Span.StartColumn, Is.EqualTo(4));
    }

    [Test]
    public void Scan_BlankLines_EmitsBlankLineTokens()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("\n  \nvalue\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlankLine).Count(), Is.EqualTo(2));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlankLine).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void Scan_BlockSequenceEntries_EmitsDashIndicatorTokens()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("- one\n- two\n-\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockSequenceEntry).Select(token => token.Value), Is.EqualTo(new string[] { "-", "-", "-" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockSequenceEntry).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockSequenceEntry).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 1, 1, 1 }));
    }

    [Test]
    public void Scan_BlockMappingIndicators_EmitsQuestionAndColonTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "? explicit",
                ": value",
                "key: value",
                "empty:",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingKey).Select(token => token.Value), Is.EqualTo(new string[] { "?" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingValue).Select(token => token.Value), Is.EqualTo(new string[] { ":", ":", ":" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingValue).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingValue).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 1, 4, 6 }));
    }

    [Test]
    public void Scan_BlockIndicators_RequireTokenBoundary()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "not-a-sequence",
                "foo?bar",
                "url:http://example.test",
                "----",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.BlockSequenceEntry), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingKey), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingValue), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.DocumentStart), Is.False);
    }

    [Test]
    public void Scan_FlowSequenceIndicators_EmitsBracketsAndEntries()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("[one, two, []]\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(IsFlowIndicator).Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.FlowSequenceStart,
            global::yaml.YamlTokenKind.FlowEntry,
            global::yaml.YamlTokenKind.FlowEntry,
            global::yaml.YamlTokenKind.FlowSequenceStart,
            global::yaml.YamlTokenKind.FlowSequenceEnd,
            global::yaml.YamlTokenKind.FlowSequenceEnd
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.FlowSequenceStart).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 1, 12 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.FlowSequenceEnd).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 13, 14 }));
    }

    [Test]
    public void Scan_FlowMappingIndicators_EmitsBracesEntriesAndNestedFlowTokens()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("{key: value, other: [1, 2]}\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(IsFlowIndicator).Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.FlowMappingStart,
            global::yaml.YamlTokenKind.FlowEntry,
            global::yaml.YamlTokenKind.FlowSequenceStart,
            global::yaml.YamlTokenKind.FlowEntry,
            global::yaml.YamlTokenKind.FlowSequenceEnd,
            global::yaml.YamlTokenKind.FlowMappingEnd
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.BlockMappingValue).Count(), Is.EqualTo(2));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FlowMappingStart).Span.StartColumn, Is.EqualTo(1));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FlowMappingEnd).Span.StartColumn, Is.EqualTo(27));
    }

    [Test]
    public void Scan_Directives_EmitsLineOrientedDirectiveTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "%YAML 1.2",
                "%TAG !e! tag:example.com,2026:",
                "---",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Directive).Select(token => token.Value), Is.EqualTo(new string[]
        {
            "%YAML 1.2",
            "%TAG !e! tag:example.com,2026:"
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Directive).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.DocumentStart), Is.True);
    }

    [Test]
    public void Scan_DirectivesDisabled_AddsDiagnostic()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.AllowDirectives = false;

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("%YAML 1.2\n", options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Directive).Value, Is.EqualTo("%YAML 1.2"));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAML004"));
    }

    [Test]
    public void Scan_AnchorsAliasesAndTags_EmitsPropertyTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "- &main !<tag:yaml.org,2002:str> value",
                "ref: *main",
                "local: !custom value",
                "handle: !!str text",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Anchor).Select(token => token.Value), Is.EqualTo(new string[] { "&main" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Alias).Select(token => token.Value), Is.EqualTo(new string[] { "*main" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Tag).Select(token => token.Value), Is.EqualTo(new string[]
        {
            "!<tag:yaml.org,2002:str>",
            "!custom",
            "!!str"
        }));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Anchor).Span.StartColumn, Is.EqualTo(3));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Alias).Span.StartColumn, Is.EqualTo(6));
    }

    [Test]
    public void Scan_MalformedAnchorAliasAndTag_AddsDiagnosticsAndErrorTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "& ",
                "*]",
                "!<missing",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Error).Select(token => token.Value), Is.EqualTo(new string[] { "&", "*", "!<missing" }));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML005", "YAML006", "YAML007" }));
    }

    [Test]
    public void Scan_PlainScalars_EmitsUnquotedScalarChunks()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "key: value",
                "- item",
                "flow: [one, two]",
                "url: http://example.test/a-b?x=1",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "item",
            "flow",
            "one",
            "two",
            "url",
            "http://example.test/a-b?x=1"
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 1, 1, 2, 3, 3, 3, 4, 4 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 1, 6, 3, 1, 8, 13, 1, 6 }));
    }

    [Test]
    public void Scan_PlainScalars_StopBeforeCommentsAndKeepWhitespaceTokens()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("alpha beta # comment\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value), Is.EqualTo(new string[] { "alpha beta" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Whitespace).Select(token => token.Value), Is.EqualTo(new string[] { " " }));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Comment).Value, Is.EqualTo("# comment"));
    }

    [Test]
    public void Scan_SingleQuotedScalars_EmitsDecodedScalarTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "name: 'Alice'",
                "quote: 'it''s ok'",
                "empty: ''",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar).Select(token => token.Value), Is.EqualTo(new string[]
        {
            "Alice",
            "it's ok",
            ""
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar).Select(token => token.Span.StartLine), Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar).Select(token => token.Span.StartColumn), Is.EqualTo(new int[] { 7, 8, 8 }));
    }

    [Test]
    public void Scan_SingleQuotedScalars_DoNotTokenizeIndicatorsOrCommentsInside()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("value: '[not, flow] # no comment'\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar).Value, Is.EqualTo("[not, flow] # no comment"));
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.FlowSequenceStart), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.FlowEntry), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.Comment), Is.False);
    }

    [Test]
    public void Scan_UnterminatedSingleQuotedScalar_AddsDiagnostic()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("bad: 'missing\nnext: value\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Error).Value, Is.EqualTo("'missing\nnext: value\n"));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAML008"));
    }

    [Test]
    public void Scan_DoubleQuotedScalars_EmitsDecodedEscapes()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("text: \"line\\n tab\\t quote\\\" slash\\\\ solidus\\/\"\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo("line\n tab\t quote\" slash\\ solidus/"));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Span.StartColumn, Is.EqualTo(7));
    }

    [Test]
    public void Scan_DoubleQuotedScalars_EmitsDecodedHexEscapes()
    {
        string expected = "A" + ((char)65).ToString() + " " + ((char)9786).ToString() + " " + char.ConvertFromUtf32(128512);
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("hex: \"A\\x41 \\u263A \\U0001F600\"\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo(expected));
    }

    [Test]
    public void Scan_DoubleQuotedScalars_DoNotTokenizeIndicatorsOrCommentsInside()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("value: \"[not, flow] # no comment\"\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo("[not, flow] # no comment"));
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.FlowSequenceStart), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.FlowEntry), Is.False);
        Assert.That(result.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.Comment), Is.False);
    }

    [Test]
    public void Scan_InvalidDoubleQuotedEscapes_AddDiagnosticsAndRecover()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("bad: \"x\\q y\\xG1 z\\U00110000\"\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo("x yG1 z"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML010", "YAML011", "YAML011" }));
    }

    [Test]
    public void Scan_UnterminatedDoubleQuotedScalar_AddsDiagnostic()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("bad: \"missing\nnext: value\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.Error).Value, Is.EqualTo("\"missing\nnext: value\n"));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAML009"));
    }

    [Test]
    public void Scan_BlockScalarHeaders_EmitsLiteralAndFoldedHeaderTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "literal: |-",
                "  text",
                "folded: >+2 # keep",
                "  text",
                "plain: |",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar).Select(token => token.Header), Is.EqualTo(new string[] { "|-", "|" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar).Select(token => token.Value), Is.EqualTo(new string[] { "text", "" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar).Select(token => token.Header), Is.EqualTo(new string[] { ">+2" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar).Select(token => token.Value), Is.EqualTo(new string[] { "text\n" }));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar).Indent, Is.EqualTo(2));
    }

    [Test]
    public void Scan_BlockScalarHeaders_AcceptsIndentBeforeChomping()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("literal: |2+\n  text\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken header = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar);
        Assert.That(header.Header, Is.EqualTo("|2+"));
        Assert.That(header.Value, Is.EqualTo("text\n"));
        Assert.That(header.Indent, Is.EqualTo(2));
        Assert.That(header.Span.StartColumn, Is.EqualTo(10));
    }

    [Test]
    public void Scan_InvalidBlockScalarHeaders_AddDiagnostics()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "bad: |0",
                "alsoBad: >--",
                "suffix: |abc",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Error).Select(token => token.Value), Is.EqualTo(new string[] { "|0", ">--", "|abc" }));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML012", "YAML012", "YAML012" }));
    }

    [Test]
    public void Scan_LiteralBlockScalars_DetectIndentationAndStripChomping()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "literal: |-",
                "  a",
                "  b",
                "",
                "next: value",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken literal = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar);
        Assert.That(literal.Header, Is.EqualTo("|-"));
        Assert.That(literal.Value, Is.EqualTo("a\nb"));
        Assert.That(literal.Indent, Is.EqualTo(2));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value), Does.Contain("next"));
    }

    [Test]
    public void Scan_LiteralBlockScalars_ClipAndKeepChomping()
    {
        string clipText = string.Join(
            "\n",
            new string[]
            {
                "clip: |",
                "  a",
                "",
                "next: value",
                ""
            });
        string keepText = string.Join(
            "\n",
            new string[]
            {
                "keep: |+",
                "  a",
                "",
                "next: value",
                ""
            });

        global::yaml.YamlScannerResult clip = global::yaml.YamlScanner.Scan(clipText, new global::yaml.YamlParserOptions());
        global::yaml.YamlScannerResult keep = global::yaml.YamlScanner.Scan(keepText, new global::yaml.YamlParserOptions());

        Assert.That(clip.Success, Is.True);
        Assert.That(keep.Success, Is.True);
        Assert.That(clip.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar).Value, Is.EqualTo("a\n"));
        Assert.That(keep.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar).Value, Is.EqualTo("a\n\n"));
    }

    [Test]
    public void Scan_LiteralBlockScalars_ExplicitIndentPreservesAdditionalIndentation()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("literal: |2\n    deep\nnext: value\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken literal = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.LiteralScalar);
        Assert.That(literal.Header, Is.EqualTo("|2"));
        Assert.That(literal.Value, Is.EqualTo("  deep\n"));
        Assert.That(literal.Indent, Is.EqualTo(2));
    }

    [Test]
    public void Scan_FoldedBlockScalars_FoldAdjacentTextLines()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "fold: >",
                "  a",
                "  b",
                "next: value",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken folded = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar);
        Assert.That(folded.Header, Is.EqualTo(">"));
        Assert.That(folded.Value, Is.EqualTo("a b\n"));
        Assert.That(folded.Indent, Is.EqualTo(2));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value), Does.Contain("next"));
    }

    [Test]
    public void Scan_FoldedBlockScalars_PreserveBlankLinesAndMoreIndentedLines()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "fold: >-",
                "  a",
                "",
                "  b",
                "    code",
                "  c",
                "next: value",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken folded = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar);
        Assert.That(folded.Header, Is.EqualTo(">-"));
        Assert.That(folded.Value, Is.EqualTo("a\nb\n  code\nc"));
    }

    [Test]
    public void Scan_FoldedBlockScalars_KeepChompingAndExplicitIndent()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("fold: >2+\n    deep\nnext: value\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlToken folded = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.FoldedScalar);
        Assert.That(folded.Header, Is.EqualTo(">2+"));
        Assert.That(folded.Value, Is.EqualTo("  deep\n"));
        Assert.That(folded.Indent, Is.EqualTo(2));
    }

    [Test]
    public void Scan_IndentationChanges_EmitsIndentAndDedentTokens()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "root:",
                "  child:",
                "    grand: value",
                "  peer: value",
                "end: value",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Indent || token.Kind == global::yaml.YamlTokenKind.Dedent).Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.Indent,
            global::yaml.YamlTokenKind.Indent,
            global::yaml.YamlTokenKind.Dedent,
            global::yaml.YamlTokenKind.Dedent
        }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Indent).Select(token => token.Indent), Is.EqualTo(new int[] { 2, 4 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Dedent).Select(token => token.Indent), Is.EqualTo(new int[] { 2, 0 }));
    }

    [Test]
    public void Scan_ReservedIndicators_AddsErrorTokensAndDiagnostics()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("@\n`\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Error).Select(token => token.Value), Is.EqualTo(new string[] { "@", "`" }));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML003", "YAML003" }));
        Assert.That(result.Diagnostics[0].Span.StartLine, Is.EqualTo(1));
        Assert.That(result.Diagnostics[1].Span.StartLine, Is.EqualTo(2));
    }

    [Test]
    public void Scan_DiagnosticLimitStopsRecoveryAndCapsDiagnostics()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.MaxDiagnostics = 1;

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("@\n`\nlater\n", options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML003" }));
        Assert.That(result.Tokens.Any(token => token.Value == "later"), Is.False);
        Assert.That(result.Tokens.First().Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamStart));
        Assert.That(result.Tokens.Last().Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamEnd));
    }

    [Test]
    public void Scan_InvalidControlCharacters_AddsDiagnosticsAndErrorTokens()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("ok\u0001bad\u007F\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value), Is.EqualTo(new string[] { "ok", "bad" }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Error).Select(token => token.Value), Is.EqualTo(new string[] { ((char)1).ToString(), ((char)127).ToString() }));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAML013", "YAML013" }));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Span.StartColumn), Is.EqualTo(new int[] { 3, 7 }));
    }

    [Test]
    public void Scan_IndentationMismatch_AddsDiagnostic()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "root:",
                "  child: value",
                " bad: value",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAML002"));
        Assert.That(result.Diagnostics.Single().Span.StartLine, Is.EqualTo(3));
    }

    [Test]
    public void Scan_CompactNestedMappingInSequence_RecordsImplicitIndent()
    {
        string text = "- parent:\n    child: value\n  other: two\n";

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Indent).Select(token => token.Indent), Is.EqualTo(new int[] { 2, 4 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Dedent).Select(token => token.Indent), Is.EqualTo(new int[] { 2, 0 }));
    }

    [Test]
    public void Scan_TabIndentation_AddsDiagnostic()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("\tbad\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAML001"));
        Assert.That(result.Diagnostics.Single().Message, Does.Contain("Tabs are not allowed"));
    }

    [Test]
    public void Scan_TabOnlyBlockScalarLineReportsItsSourceLocation()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("value: |\n\t\nnext: one\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        global::yaml.YamlDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == "YAML012");
        Assert.That(diagnostic.Message, Does.Contain("block scalar indentation"));
        Assert.That(diagnostic.Span.StartLine, Is.EqualTo(2));
        Assert.That(diagnostic.Span.StartColumn, Is.EqualTo(1));
    }

    [Test]
    public void Scan_TabIndentedQuotedScalarContinuationReportsItsSourceLocation()
    {
        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("value: \"one\n\ttwo\"\n", new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        global::yaml.YamlDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == "YAML001");
        Assert.That(diagnostic.Message, Does.Contain("quoted scalar indentation"));
        Assert.That(diagnostic.Span.StartLine, Is.EqualTo(2));
        Assert.That(diagnostic.Span.StartColumn, Is.EqualTo(1));
    }

    static bool IsFlowIndicator(global::yaml.YamlToken token)
    {
        return token.Kind == global::yaml.YamlTokenKind.FlowSequenceStart
            || token.Kind == global::yaml.YamlTokenKind.FlowSequenceEnd
            || token.Kind == global::yaml.YamlTokenKind.FlowMappingStart
            || token.Kind == global::yaml.YamlTokenKind.FlowMappingEnd
            || token.Kind == global::yaml.YamlTokenKind.FlowEntry;
    }
}
