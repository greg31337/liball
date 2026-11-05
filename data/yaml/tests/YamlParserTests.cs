using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlParserTests
{
    [Test]
    public void Parse_EmptyScannerStream_EmitsStreamStartAndEnd()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_ExplicitDocumentMarkers_EmitsDocumentStartAndEndEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("---\n...\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[1].Span.StartLine, Is.EqualTo(1));
        Assert.That(result.Events[3].Span.StartLine, Is.EqualTo(2));
    }

    [Test]
    public void Parse_OpenDocument_IsClosedAtStreamEnd()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("---\nkey: value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_DocumentEndWithoutStart_DoesNotCreateADocument()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("...\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_MultipleDocuments_EmitsOrderedDocumentBoundaries()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("---\n...\n---\n...\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_BlockMappingAfterCompletedRootSequence_ReturnsDiagnostic()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- item1\n- item2\ninvalid: value\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(scanner.Success, Is.True);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("YAMLP021"));
    }

    [Test]
    public void Parse_CommentsAndBlankLines_EmitCommentEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("# document\n\nkey: value # trailing\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Comment).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "# document",
            "",
            "# trailing"
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void ParseScannerResult_PropagatesScannerDiagnostics()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("bad\u0001\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP002", "YAML013" }));
        Assert.That(result.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart));
        Assert.That(result.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
    }

    [Test]
    public void Parse_BlockSequence_EmitsSequenceAndScalarEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- one\n- two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[3].Value, Is.EqualTo("one"));
        Assert.That(result.Events[4].Value, Is.EqualTo("two"));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_BlockSequenceInsideExplicitDocument_ClosesBeforeDocumentEnd()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("---\n- one\n- two\n...\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[5].Span.StartLine, Is.EqualTo(4));
        Assert.That(result.Events[6].Span.StartLine, Is.EqualTo(4));
    }

    [Test]
    public void Parse_BlockSequence_PreservesScalarStyles()
    {
        string text = "- plain\n- 'single'\n- \"double\"\n- |\n  literal\n- >\n  folded\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.ScalarStyle), Is.EqualTo(new global::yaml.YamlScalarStyle[]
        {
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.SingleQuoted,
            global::yaml.YamlScalarStyle.DoubleQuoted,
            global::yaml.YamlScalarStyle.Literal,
            global::yaml.YamlScalarStyle.Folded
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "plain",
            "single",
            "double",
            "literal\n",
            "folded\n"
        }));
    }

    [Test]
    public void Parse_NestedBlockSequences_CloseByIndentation()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- top\n  - nested\n- next\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "top",
            "nested",
            "next"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_BlockMapping_EmitsMappingAndScalarEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("key: value\nother: two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "other",
            "two"
        }));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_BlockMappingInsideExplicitDocument_ClosesBeforeDocumentEnd()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("---\nkey: value\n...\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[5].Span.StartLine, Is.EqualTo(3));
        Assert.That(result.Events[6].Span.StartLine, Is.EqualTo(3));
    }

    [Test]
    public void Parse_NestedBlockMappings_CloseByIndentation()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("parent:\n  child: value\nnext: item\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "parent",
            "child",
            "value",
            "next",
            "item"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_BlockMapping_PreservesScalarStyles()
    {
        string text = "plain: 'single'\nquoted: \"double\"\nblock: |\n  literal\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.ScalarStyle), Is.EqualTo(new global::yaml.YamlScalarStyle[]
        {
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.SingleQuoted,
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.DoubleQuoted,
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.Literal
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "plain",
            "single",
            "quoted",
            "double",
            "block",
            "literal\n"
        }));
    }

    [Test]
    public void Parse_CompactMappingInSequence_EmitsMappingPerSequenceItem()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- key: value\n- other: two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "other",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_CompactMappingInSequence_AllowsContinuationKeys()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- key: value\n  other: two\n- next: item\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "other",
            "two",
            "next",
            "item"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_CompactMappingInSequence_KeepsOuterMappingOpenAfterNestedValue()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- parent:\n    child: value\n  other: two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "parent",
            "child",
            "value",
            "other",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_CompactMappingInMappingValue_EmitsSequenceMappingItem()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("root:\n  - key: value\n    other: two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "root",
            "key",
            "value",
            "other",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_SequenceMappingWithNestedMapping_ClosesBlockCollectionsInStackOrder()
    {
        string text = "root:\n  - name: api\n    config:\n      ports: [80]\n...\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "root",
            "name",
            "api",
            "config",
            "ports",
            "80"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_ExplicitMappingKey_EmitsKeyAndValue()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("? explicit\n: value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "explicit",
            "value"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_ExplicitMappingKeyWithSameLineMapping_EmitsComplexKeyAndEmptyValue()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("? explicit: value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "explicit",
            "value",
            ""
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_ExplicitMappingKey_PreservesScalarStyles()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("? 'quoted'\n: \"value\"\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.ScalarStyle), Is.EqualTo(new global::yaml.YamlScalarStyle[]
        {
            global::yaml.YamlScalarStyle.SingleQuoted,
            global::yaml.YamlScalarStyle.DoubleQuoted
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "quoted",
            "value"
        }));
    }

    [Test]
    public void Parse_ExplicitMappingKey_CanMixWithImplicitKeys()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("implicit: one\n? explicit\n: two\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "implicit",
            "one",
            "explicit",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_EmptySequenceEntries_EmitEmptyScalarNodes()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("-\n- value\n-\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "",
            "value",
            ""
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.ScalarStyle), Is.EqualTo(new global::yaml.YamlScalarStyle[]
        {
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.Plain,
            global::yaml.YamlScalarStyle.Plain
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_EmptyMappingValue_EmitsEmptyScalarBeforeNextKey()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("empty:\nnext: value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "empty",
            "",
            "next",
            "value"
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_EmptyExplicitMappingKey_EmitsEmptyKeyScalar()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("?\n: value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "",
            "value"
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_ExplicitMappingKeyWithEmptyValue_EmitsEmptyValueScalar()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("? key\n:\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            ""
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_EmptyMappingKey_EmitsEmptyKeyScalar()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(": value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "",
            "value"
        }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_FlowSequence_EmitsFlowSequenceAndScalars()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("[one, two, []]\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events[5].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "one",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_FlowMapping_EmitsFlowMappingAndPairs()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("{key: value, other: two}\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "other",
            "two"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_FlowMappingWithNestedSequence_EmitsNestedFlowEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("{key: value, other: [1, 2]}\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events[6].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "key",
            "value",
            "other",
            "1",
            "2"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_FlowSequenceAsFlowMappingKey_EmitsCollectionKeyAndValue()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("{[a, b]: value}\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "a",
            "b",
            "value"
        }));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_FlowSequenceAsBlockMappingValue_EmitsNestedCollection()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("items: [one, two]\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events[4].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "items",
            "one",
            "two"
        }));
    }

    [Test]
    public void Parse_FlowMapping_EmitsEmptyKeyAndValueScalars()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("{: value, empty: }\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[]
        {
            "",
            "value",
            "empty",
            ""
        }));
    }

    [Test]
    public void Parse_TagAndAnchorOnScalar_AttachesPropertiesToScalarEvent()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("- &main !<tag:yaml.org,2002:str> value\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlEvent scalar = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar);
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.Anchor, Is.EqualTo("main"));
        Assert.That(scalar.Tag, Is.EqualTo("tag:yaml.org,2002:str"));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_Alias_EmitsAliasEventWithAliasName()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("ref: *main\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Alias,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Alias).AliasName, Is.EqualTo("main"));
        Assert.That(EventsFollowOrderingRules(result), Is.True);
    }

    [Test]
    public void Parse_TagAndAnchorOnFlowSequence_AttachesPropertiesToCollectionStart()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("items: &list !seq [one]\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        global::yaml.YamlEvent sequenceStart = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.SequenceStart);
        Assert.That(sequenceStart.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(sequenceStart.Anchor, Is.EqualTo("list"));
        Assert.That(sequenceStart.Tag, Is.EqualTo("!seq"));
    }

    [Test]
    public void Parse_DirectivesBeforeDocument_AreAcceptedAndPropertiesStillApply()
    {
        string text = "%YAML 1.2\n%TAG !e! tag:example.com,2026:\n---\n- !e!thing &a value\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.YamlVersionDirective,
            global::yaml.YamlEventKind.TagDirective,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        global::yaml.YamlEvent scalar = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar);
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.Tag, Is.EqualTo("tag:example.com,2026:thing"));
        Assert.That(scalar.Anchor, Is.EqualTo("a"));
    }

    [Test]
    public void Parse_MalformedFlowWithoutRecovery_StopsAfterFirstParserDiagnostic()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("]}\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP003" }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.Diagnostic,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_MalformedFlowWithRecovery_CollectsMultipleParserDiagnostics()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("]}\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP003", "YAMLP004" }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Diagnostic).Count(), Is.EqualTo(2));
        Assert.That(result.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart));
        Assert.That(result.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
    }

    [Test]
    public void Parse_MalformedFlowWithRecovery_HonorsMaxDiagnostics()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        options.MaxDiagnostics = 1;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("]}\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP003" }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Diagnostic).Count(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_DiagnosticLimitStopsRecoveryBeforeLaterNodes()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        options.MaxDiagnostics = 1;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("]\nlater\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP003" }));
        Assert.That(result.Events.Any(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar), Is.False);
        Assert.That(result.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart));
        Assert.That(result.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
    }

    [Test]
    public void Parse_SourceBackedMalformedInputUsesBuiltInStructuredDiagnostic()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("[one,,two]\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLP016"));
        Assert.That(result.Diagnostics[0].Span.StartLine, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Span.StartColumn, Is.EqualTo(6));
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "YAMLP100"), Is.False);
    }

    [Test]
    public void Parse_MismatchedFlowCloserWithRecovery_AddsDiagnosticAndClosesOpenCollection()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("[}\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP005" }));
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Diagnostic,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    static bool EventsFollowOrderingRules(global::yaml.YamlParserResult result)
    {
        if (result.Events.Count == 0)
        {
            return false;
        }

        if (!global::yaml.YamlEventRules.CanStart(result.Events[0].Kind))
        {
            return false;
        }

        int index = 1;
        while (index < result.Events.Count)
        {
            if (!global::yaml.YamlEventRules.CanFollow(result.Events[index - 1].Kind, result.Events[index].Kind))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
