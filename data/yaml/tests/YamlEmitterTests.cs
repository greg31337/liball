using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlEmitterTests
{
    [Test]
    public void Emit_SingleScalarDocument_OmitsMarkersByDefault()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("value")));

        string text = global::yaml.YamlEmitter.Emit(stream);

        Assert.That(text, Is.EqualTo("value\n"));
    }

    [Test]
    public void Emit_DocumentMarkers_CanBeForcedWithOptions()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("value")));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.WriteDocumentStart = true;
        options.WriteDocumentEnd = true;

        string text = global::yaml.YamlEmitter.Emit(stream, options);

        Assert.That(text, Is.EqualTo("---\nvalue\n...\n"));
    }

    [Test]
    public void Emit_Directives_PrecedeDocumentStartMarker()
    {
        global::yaml.YamlDocument document = DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("value"));
        document.AddDirective(global::yaml.YamlDirective.YamlVersionDirective(
            global::yaml.YamlVersion.Version12,
            "",
            global::yaml.YamlSourceSpan.Empty()));
        document.AddDirective(global::yaml.YamlDirective.TagDirective(
            "!e!",
            "tag:example.com,2026:",
            global::yaml.YamlSourceSpan.Empty()));
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(document);

        string text = global::yaml.YamlEmitter.Emit(stream);

        Assert.That(text, Is.EqualTo("%YAML 1.2\n%TAG !e! tag:example.com,2026:\n---\nvalue\n"));
    }

    [Test]
    public void Emit_MultipleDocuments_UsesDocumentStartMarkers()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("one")));
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("two")));

        string text = global::yaml.YamlEmitter.Emit(stream);

        Assert.That(text, Is.EqualTo("---\none\n---\ntwo\n"));
    }

    [Test]
    public void Emit_MultipleDocuments_CanForceDocumentEndMarkers()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("one")));
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("two")));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.WriteDocumentEnd = true;

        string text = global::yaml.YamlEmitter.Emit(stream, options);

        Assert.That(text, Is.EqualTo("---\none\n...\n---\ntwo\n...\n"));
    }

    [Test]
    public void Emit_RootNullAliasAndEmptyNodes_UsesMinimalRootRepresentation()
    {
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("value"), "target");
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.AliasTo("target", target);

        Assert.That(global::yaml.YamlEmitter.EmitNode(global::yaml.YamlNodeFactory.FromJsonValue(null)), Is.EqualTo("null"));
        Assert.That(global::yaml.YamlEmitter.EmitNode(alias), Is.EqualTo("*target"));
        Assert.That(global::yaml.YamlEmitter.EmitNode(global::yaml.YamlNodeFactory.Empty()), Is.EqualTo(""));
    }

    [Test]
    public void Emit_PlainScalarSafety_QuotesSyntacticallyUnsafeValues()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("empty", global::yaml.YamlNodeFactory.Scalar(""));
        mapping.AddMappingValue("leading", global::yaml.YamlNodeFactory.Scalar("- item"));
        mapping.AddMappingValue("colon", global::yaml.YamlNodeFactory.Scalar("a: b"));
        mapping.AddMappingValue("comment", global::yaml.YamlNodeFactory.Scalar("value # comment"));
        mapping.AddMappingValue("space", global::yaml.YamlNodeFactory.Scalar(" padded "));
        mapping.AddMappingValue("apostrophe", global::yaml.YamlNodeFactory.Scalar("'Ada's"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("empty: ''\nleading: '- item'\ncolon: 'a: b'\ncomment: 'value # comment'\nspace: ' padded '\napostrophe: '''Ada''s'\n"));
    }

    [Test]
    public void Emit_PlainScalarSafety_QuotesFlowDelimitersInsideFlowCollections()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("alpha"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("bravo, charlie"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("[delta]"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("[alpha, 'bravo, charlie', '[delta]']\n"));
    }

    [Test]
    public void Emit_PlainScalarSafety_PreservesTaggedJsonScalarsAsPlain()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(true));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(12));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(1.5d));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(null));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("[true, 12, 1.5, null]\n"));
    }

    [Test]
    public void Emit_PlainScalarSafety_QuotesStringTaggedScalarsThatResolveAsNonStrings()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("truth", global::yaml.YamlNodeFactory.FromJsonValue("true"));
        mapping.AddMappingValue("number", global::yaml.YamlNodeFactory.FromJsonValue("12"));
        mapping.AddMappingValue("nil", global::yaml.YamlNodeFactory.FromJsonValue("null"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("truth: 'true'\nnumber: '12'\nnil: 'null'\n"));
    }

    [Test]
    public void Emit_SingleQuotedScalar_EscapesSingleQuotes()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.SingleQuotedScalar("Ada's notes");

        string text = global::yaml.YamlEmitter.EmitNode(scalar);

        Assert.That(text, Is.EqualTo("'Ada''s notes'"));
    }

    [Test]
    public void Emit_DoubleQuotedScalar_EscapesYamlCharacters()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.DoubleQuotedScalar("line\nquote \" slash \\ tab\t");

        string text = global::yaml.YamlEmitter.EmitNode(scalar);

        Assert.That(text, Is.EqualTo("\"line\\nquote \\\" slash \\\\ tab\\t\""));
    }

    [Test]
    public void Emit_DefaultScalarStyle_AppliesToAnyStyleScalars()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar(
            "value",
            global::yaml.YamlScalarStyle.Any,
            global::yaml.YamlSourceSpan.Empty());
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.DefaultScalarStyle = global::yaml.YamlScalarStyle.DoubleQuoted;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar), options);

        Assert.That(text, Is.EqualTo("\"value\"\n"));
    }

    [Test]
    public void Emit_UnsafePlainFallback_UsesDefaultDoubleQuotedStyle()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.Scalar("a: b");
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.DefaultScalarStyle = global::yaml.YamlScalarStyle.DoubleQuoted;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar), options);

        Assert.That(text, Is.EqualTo("\"a: b\"\n"));
    }

    [Test]
    public void Emit_QuotedScalarsInMapping_CanBeParsedBack()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("single", global::yaml.YamlNodeFactory.SingleQuotedScalar("Ada's"));
        mapping.AddMappingValue("double", global::yaml.YamlNodeFactory.DoubleQuotedScalar("line\nbreak"));
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.FindMappingValue("single")!.Value, Is.EqualTo("Ada's"));
        Assert.That(root.FindMappingValue("double")!.Value, Is.EqualTo("line\nbreak"));
    }

    [Test]
    public void Emit_LiteralScalar_EmitsIndentedBlockContent()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.LiteralScalar("line one\nline two\n");

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar));

        Assert.That(text, Is.EqualTo("|\n  line one\n  line two\n"));
    }

    [Test]
    public void Emit_FoldedScalar_EmitsFoldedBlockIndicator()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.FoldedScalar("folded line\n");

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar));

        Assert.That(text, Is.EqualTo(">\n  folded line\n"));
    }

    [Test]
    public void Emit_BlockScalarMappingValues_KeepIndicatorBesideKey()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("literal", global::yaml.YamlNodeFactory.LiteralScalar("a\nb"));
        mapping.AddMappingValue("folded", global::yaml.YamlNodeFactory.FoldedScalar("single folded line"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("literal: |\n  a\n  b\nfolded: >\n  single folded line\n"));
    }

    [Test]
    public void Emit_BlockScalarSequenceItems_KeepIndicatorBesideDash()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.LiteralScalar("a\nb"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.FoldedScalar("c"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("- |\n  a\n  b\n- >\n  c\n"));
    }

    [Test]
    public void Emit_BlockScalars_UseConfiguredIndent()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("literal", global::yaml.YamlNodeFactory.LiteralScalar("a\nb"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.Indent = 4;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo("literal: |\n    a\n    b\n"));
    }

    [Test]
    public void Emit_BlockScalarsInsideFlowCollections_FallBackToDoubleQuotedScalars()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.LiteralScalar("a\nb"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("[\"a\\nb\"]\n"));
    }

    [Test]
    public void Emit_LiteralScalarInMapping_CanBeParsedBack()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("literal", global::yaml.YamlNodeFactory.LiteralScalar("a\nb\n"));
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.FindMappingValue("literal")!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Literal));
        Assert.That(root.FindMappingValue("literal")!.Value, Is.EqualTo("a\nb\n"));
    }

    [Test]
    public void Emit_DocumentCommentsAndBlankLines_PrecedeDirectivesAndRoot()
    {
        global::yaml.YamlDocument document = DocumentWithRoot(global::yaml.YamlNodeFactory.Scalar("value"));
        document.AddLeadingBlankLine("");
        document.AddLeadingComment("# document");
        document.AddDirective(global::yaml.YamlDirective.YamlVersionDirective(
            global::yaml.YamlVersion.Version12,
            "",
            global::yaml.YamlSourceSpan.Empty()));

        string text = global::yaml.YamlEmitter.EmitDocument(document);

        Assert.That(text, Is.EqualTo("\n# document\n%YAML 1.2\n---\nvalue\n"));
    }

    [Test]
    public void Emit_MappingCommentsAndBlankLines_EmitAroundPairs()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddLeadingComment("# root");
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.Scalar("Ada");
        global::yaml.YamlPair pair = global::yaml.YamlNodeFactory.Pair("name", value);
        pair.AddLeadingBlankLine("");
        pair.AddLeadingComment("# pair");
        pair.AddTrailingComment("# inline pair");
        mapping.AddMappingPair(pair);

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("# root\n\n# pair\nname: Ada # inline pair\n"));
    }

    [Test]
    public void Emit_SequenceCommentsAndBlankLines_EmitAroundItems()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        global::yaml.YamlNode first = global::yaml.YamlNodeFactory.Scalar("one");
        first.AddLeadingComment("first item");
        first.AddTrailingComment("inline item");
        global::yaml.YamlNode second = global::yaml.YamlNodeFactory.Scalar("two");
        second.AddLeadingBlankLine("");
        sequence.AddSequenceItem(first);
        sequence.AddSequenceItem(second);

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("# first item\n- one # inline item\n\n- two\n"));
    }

    [Test]
    public void Emit_CommentsDisabled_OmitsCommentsAndBlankLines()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddLeadingComment("# root");
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.Scalar("Ada");
        value.AddTrailingComment("# inline");
        value.AddLeadingBlankLine("");
        mapping.AddMappingValue("name", value);
        global::yaml.YamlDocument document = DocumentWithRoot(mapping);
        document.AddLeadingComment("# document");
        document.AddLeadingBlankLine("");
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.EmitComments = false;

        string text = global::yaml.YamlEmitter.EmitDocument(document, options);

        Assert.That(text, Is.EqualTo("name: Ada\n"));
    }

    [Test]
    public void Emit_CommentsInMapping_CanBeParsedBack()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.Scalar("Ada");
        value.AddTrailingComment("# inline");
        global::yaml.YamlPair pair = global::yaml.YamlNodeFactory.Pair("name", value);
        pair.AddLeadingComment("# pair");
        mapping.AddMappingPair(pair);
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlDocument parsedDocument = composer.Stream.Documents[0];
        global::yaml.YamlPair parsedPair = composer.Stream.Documents[0].Root!.Pairs[0];

        Assert.That(parsedDocument.LeadingComments.Single(), Is.EqualTo("# pair"));
        Assert.That(parsedPair.Value!.TrailingComments.Single(), Is.EqualTo("# inline"));
    }

    [Test]
    public void Emit_TagsAndAnchorsOnScalars_PrecedeScalarValue()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.Scalar("Ada");
        scalar.Anchor = "person";
        scalar.Tag = "!person";

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar));

        Assert.That(text, Is.EqualTo("&person !person Ada\n"));
    }

    [Test]
    public void Emit_UriTags_UseVerbatimTagProperty()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.Scalar("value");
        scalar.Tag = "tag:example.com,2026:thing";

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(scalar));

        Assert.That(text, Is.EqualTo("!<tag:example.com,2026:thing> value\n"));
    }

    [Test]
    public void Emit_BuiltInTags_AreSuppressedByDefault()
    {
        global::yaml.YamlNode truth = global::yaml.YamlNodeFactory.TaggedScalar("true", global::yaml.YamlScalarResolver.BoolTag);

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(truth));

        Assert.That(text, Is.EqualTo("true\n"));
    }

    [Test]
    public void Emit_AnchoredTaggedBlockCollectionAndAlias_UseNodeProperties()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.Sequence();
        items.Anchor = "items";
        items.Tag = "!items";
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        root.AddMappingValue("items", items);
        root.AddMappingValue("copy", global::yaml.YamlNodeFactory.AliasTo("items", items));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(root));

        Assert.That(text, Is.EqualTo("items: &items !items\n  - one\ncopy: *items\n"));
    }

    [Test]
    public void Emit_AnchoredTaggedFlowCollection_UsesInlineNodeProperties()
    {
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.FlowSequence();
        items.Anchor = "items";
        items.Tag = "!items";
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(items));

        Assert.That(text, Is.EqualTo("&items !items [one]\n"));
    }

    [Test]
    public void Emit_RecursiveAliasGraph_EmitsAnchorAndAliasReference()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.Anchor = "self";
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.AliasTo("self", sequence));

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("&self\n- *self\n"));
    }

    [Test]
    public void Emit_TagsAnchorsAndAliases_CanBeParsedBack()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.Scalar("Ada");
        value.Anchor = "person";
        value.Tag = "!person";
        root.AddMappingValue("name", value);
        root.AddMappingValue("copy", global::yaml.YamlNodeFactory.AliasTo("person", value));
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(root));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode parsedRoot = composer.Stream.Documents[0].Root!;
        global::yaml.YamlNode parsedValue = parsedRoot.FindMappingValue("name")!;
        global::yaml.YamlNode parsedAlias = parsedRoot.FindMappingValue("copy")!;

        Assert.That(parsedValue.Anchor, Is.EqualTo("person"));
        Assert.That(parsedValue.Tag, Is.EqualTo("!person"));
        Assert.That(parsedAlias.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(parsedAlias.AliasTarget, Is.SameAs(parsedValue));
    }

    [Test]
    public void Emit_RecursiveAliasGraph_CanBeParsedBack()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.Anchor = "self";
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.AliasTo("self", sequence));
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode parsedRoot = composer.Stream.Documents[0].Root!;

        Assert.That(parsedRoot.Anchor, Is.EqualTo("self"));
        Assert.That(parsedRoot.Children[0].Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(parsedRoot.Children[0].AliasTarget, Is.SameAs(parsedRoot));
    }

    [Test]
    public void Emit_BlockSequence_EmitsNestedItemsWithDefaultIndent()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        sequence.AddSequenceItem(mapping);
        global::yaml.YamlNode nested = global::yaml.YamlNodeFactory.Sequence();
        nested.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("deep"));
        sequence.AddSequenceItem(nested);
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Sequence());

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("- one\n-\n  name: Ada\n-\n  - deep\n- []\n"));
    }

    [Test]
    public void Emit_BlockMapping_EmitsNestedSequencesAndMappings()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        global::yaml.YamlNode roles = global::yaml.YamlNodeFactory.Sequence();
        roles.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("admin"));
        roles.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("writer"));
        mapping.AddMappingValue("roles", roles);
        global::yaml.YamlNode metadata = global::yaml.YamlNodeFactory.Mapping();
        metadata.AddMappingValue("enabled", global::yaml.YamlNodeFactory.FromJsonValue(true));
        mapping.AddMappingValue("metadata", metadata);
        mapping.AddMappingValue("empty", global::yaml.YamlNodeFactory.Mapping());

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("name: Ada\nroles:\n  - admin\n  - writer\nmetadata:\n  enabled: true\nempty: {}\n"));
    }

    [Test]
    public void Emit_BlockCollections_UseConfiguredIndent()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.Sequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        mapping.AddMappingValue("items", items);
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.Indent = 4;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo("items:\n    - one\n"));
    }

    [Test]
    public void Emit_BlockCollections_NormalizeConfiguredIndent()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.Sequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        mapping.AddMappingValue("items", items);
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.Indent = 0;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo("items:\n - one\n"));
    }

    [Test]
    public void Emit_FlowSequence_EmitsInlineItemsAndNestedCollections()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.FlowMapping();
        mapping.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        sequence.AddSequenceItem(mapping);
        global::yaml.YamlNode nested = global::yaml.YamlNodeFactory.FlowSequence();
        nested.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(null));
        nested.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(true));
        sequence.AddSequenceItem(nested);

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        Assert.That(text, Is.EqualTo("[one, {name: Ada}, [null, true]]\n"));
    }

    [Test]
    public void Emit_FlowMapping_EmitsInlinePairsAndEmptyValues()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.FlowMapping();
        mapping.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.FlowSequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("two"));
        mapping.AddMappingValue("items", items);
        mapping.AddMappingValue("empty", global::yaml.YamlNodeFactory.Empty());

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("{name: Ada, items: [one, two], empty: null}\n"));
    }

    [Test]
    public void Emit_BlockMapping_KeepsFlowCollectionsInline()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.FlowSequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("two"));
        mapping.AddMappingValue("items", items);
        global::yaml.YamlNode metadata = global::yaml.YamlNodeFactory.FlowMapping();
        metadata.AddMappingValue("enabled", global::yaml.YamlNodeFactory.FromJsonValue(true));
        mapping.AddMappingValue("metadata", metadata);

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping));

        Assert.That(text, Is.EqualTo("items: [one, two]\nmetadata: {enabled: true}\n"));
    }

    [Test]
    public void Emit_AnyCollectionStyle_UsesDefaultFlowCollectionOption()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(
            global::yaml.YamlCollectionStyle.Any,
            global::yaml.YamlSourceSpan.Empty());
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("two"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.DefaultCollectionStyle = global::yaml.YamlCollectionStyle.Flow;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence), options);

        Assert.That(text, Is.EqualTo("[one, two]\n"));
    }

    [Test]
    public void Emit_FlowSequence_WrapsWhenLineWidthIsExceeded()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("alpha"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("bravo"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("charlie"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.LineWidth = 12;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence), options);

        Assert.That(text, Is.EqualTo("[\n  alpha,\n  bravo,\n  charlie\n]\n"));
    }

    [Test]
    public void Emit_FlowSequence_LineWidthZeroDisablesWrapping()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.FlowSequence();
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("alpha"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("bravo"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("charlie"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.LineWidth = 0;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence), options);

        Assert.That(text, Is.EqualTo("[alpha, bravo, charlie]\n"));
    }

    [Test]
    public void Emit_BlockMapping_CanSortStringKeys()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("zeta", global::yaml.YamlNodeFactory.Scalar("last"));
        global::yaml.YamlNode complexKey = global::yaml.YamlNodeFactory.FlowSequence();
        complexKey.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("key"));
        mapping.AddMappingPair(global::yaml.YamlNodeFactory.Pair(complexKey, global::yaml.YamlNodeFactory.Scalar("complex")));
        mapping.AddMappingValue("alpha", global::yaml.YamlNodeFactory.Scalar("first"));
        mapping.AddMappingValue("bravo", global::yaml.YamlNodeFactory.Scalar("second"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.SortStringKeys = true;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo("alpha: first\nbravo: second\nzeta: last\n[key]: complex\n"));
    }

    [Test]
    public void Emit_FlowMapping_CanSortStringKeysAndWrap()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.FlowMapping();
        mapping.AddMappingValue("zeta", global::yaml.YamlNodeFactory.Scalar("last"));
        mapping.AddMappingValue("alpha", global::yaml.YamlNodeFactory.Scalar("first"));
        mapping.AddMappingValue("bravo", global::yaml.YamlNodeFactory.Scalar("second"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.SortStringKeys = true;
        options.LineWidth = 18;

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo("{\n  alpha: first,\n  bravo: second,\n  zeta: last\n}\n"));
    }

    [Test]
    public void Emit_EmptyMappingDocument_CanBeParsedBackIntoDocumentRoot()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.AddDocument(DocumentWithRoot(global::yaml.YamlNodeFactory.Mapping()));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.WriteDocumentStart = true;
        string text = global::yaml.YamlEmitter.Emit(stream, options);

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        Assert.That(composer.Stream.DocumentCount(), Is.EqualTo(1));
        Assert.That(composer.Stream.Documents[0].Root!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(composer.Stream.Documents[0].Root!.Pairs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Emit_BlockMappingDocument_CanBeParsedBackIntoNestedCollections()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.Sequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("two"));
        root.AddMappingValue("items", items);
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(root));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode parsedRoot = composer.Stream.Documents[0].Root!;
        global::yaml.YamlNode parsedItems = parsedRoot.FindMappingValue("items")!;

        Assert.That(parsedRoot.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(parsedItems.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(parsedItems.Children[0].Value, Is.EqualTo("one"));
        Assert.That(parsedItems.Children[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void Emit_BlockSequenceDocument_CanBeParsedBackIntoNestedMappings()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        global::yaml.YamlNode first = global::yaml.YamlNodeFactory.Mapping();
        first.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        sequence.AddSequenceItem(first);
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(sequence));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode parsedRoot = composer.Stream.Documents[0].Root!;

        Assert.That(parsedRoot.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(parsedRoot.Children[0].Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(parsedRoot.Children[0].FindMappingValue("name")!.Value, Is.EqualTo("Ada"));
    }

    [Test]
    public void Emit_FlowMappingDocument_CanBeParsedBackIntoNestedCollections()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.FlowMapping();
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.FlowSequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        items.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("two"));
        root.AddMappingValue("items", items);
        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(root));

        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlNode parsedRoot = composer.Stream.Documents[0].Root!;
        global::yaml.YamlNode parsedItems = parsedRoot.FindMappingValue("items")!;

        Assert.That(parsedRoot.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(parsedItems.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(parsedItems.Children[0].Value, Is.EqualTo("one"));
        Assert.That(parsedItems.Children[1].Value, Is.EqualTo("two"));
    }

    static global::yaml.YamlDocument DocumentWithRoot(global::yaml.YamlNode root)
    {
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.SetRoot(root);
        return document;
    }
}
