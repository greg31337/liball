using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlComposerTests
{
    [Test]
    public void Compose_EmptyEventList_CreatesEmptyStream()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[] { }, new global::yaml.YamlComposerOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Stream.DocumentCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Compose_EmptyDocument_CreatesEmptyRoot()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(2)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(3))
        }, new global::yaml.YamlComposerOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Stream.DocumentCount(), Is.EqualTo(1));
        Assert.That(result.Stream.Documents[0].Root, Is.Not.Null);
        Assert.That(result.Stream.Documents[0].Root!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
    }

    [Test]
    public void Compose_SequenceEvents_BuildOrderedChildren()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.Scalar("two", global::yaml.YamlScalarStyle.SingleQuoted, Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(7))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(root.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(root.Children.Select(child => child.Value), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(root.Children[1].ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
    }

    [Test]
    public void Compose_MappingEvents_BuildOrderedPairsAndNestedCollections()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Scalar("items", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Flow, Span(4)),
            global::yaml.YamlEvent.Scalar("a", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Scalar("b", global::yaml.YamlScalarStyle.Plain, Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(7)),
            global::yaml.YamlEvent.Scalar("name", global::yaml.YamlScalarStyle.Plain, Span(8)),
            global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.DoubleQuoted, Span(9)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(10)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(11)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(12))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;
        global::yaml.YamlPair first = root.Pairs[0];
        global::yaml.YamlPair second = root.Pairs[1];

        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(root.Pairs.Count, Is.EqualTo(2));
        Assert.That(first.Key!.Value, Is.EqualTo("items"));
        Assert.That(first.Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(first.Value!.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(first.Value!.Children.Select(child => child.Value), Is.EqualTo(new string[] { "a", "b" }));
        Assert.That(second.Key!.Value, Is.EqualTo("name"));
        Assert.That(second.Value!.Value, Is.EqualTo("value"));
        Assert.That(second.Value!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
    }

    [Test]
    public void Compose_MappingEvents_StoreSequenceNodeAsComplexPairKey()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Flow, Span(3)),
            global::yaml.YamlEvent.Scalar("a", global::yaml.YamlScalarStyle.Plain, Span(4)),
            global::yaml.YamlEvent.Scalar("b", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(6)),
            global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, Span(7)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(8)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(9)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(10))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlPair pair = result.Stream.Documents[0].Root!.Pairs.Single();

        Assert.That(result.Success, Is.True);
        Assert.That(pair.IsComplexKey(), Is.True);
        Assert.That(pair.Key!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(pair.Key!.Children.Select(child => child.Value), Is.EqualTo(new string[] { "a", "b" }));
        Assert.That(pair.Value!.Value, Is.EqualTo("value"));
    }

    [Test]
    public void Compose_CommentEvents_AttachToDocumentsPairsNodesAndTrailingComments()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Comment("# document", Span(1)),
            global::yaml.YamlEvent.Comment("", Span(2)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(3)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(4)),
            global::yaml.YamlEvent.Comment("# pair", Span(5)),
            global::yaml.YamlEvent.Scalar("key", global::yaml.YamlScalarStyle.Plain, Span(6)),
            global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, Span(7)),
            global::yaml.YamlEvent.Comment("# trailing", SpanOnLine(8, 8)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(9)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(10)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(11))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlPair pair = document.Root!.Pairs.Single();

        Assert.That(document.LeadingComments.Single(), Is.EqualTo("# document"));
        Assert.That(document.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(pair.LeadingComments.Single(), Is.EqualTo("# pair"));
        Assert.That(pair.Key!.LeadingComments.Single(), Is.EqualTo("# pair"));
        Assert.That(pair.Value!.TrailingComments.Single(), Is.EqualTo("# trailing"));
        Assert.That(pair.TrailingComments.Single(), Is.EqualTo("# trailing"));
    }

    [Test]
    public void Compose_BlankLineBeforeSequenceChild_AttachesAsLeadingBlankLine()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.Comment("", Span(4)),
            global::yaml.YamlEvent.Scalar("two", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(7)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(8))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(root.Children[0].LeadingBlankLines, Is.Empty);
        Assert.That(root.Children[1].LeadingBlankLines.Count, Is.EqualTo(1));
    }

    [Test]
    public void Compose_SourceSpans_PropagateToStreamDocumentCollectionsAndPairs()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Scalar("items", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Flow, Span(4)),
            global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(7)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(8)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(9))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlNode root = document.Root!;
        global::yaml.YamlPair pair = root.Pairs.Single();
        global::yaml.YamlNode sequence = pair.Value!;

        Assert.That(result.Stream.Span.StartIndex, Is.EqualTo(0));
        Assert.That(result.Stream.Span.EndIndex, Is.EqualTo(10));
        Assert.That(document.Span.StartIndex, Is.EqualTo(1));
        Assert.That(document.Span.EndIndex, Is.EqualTo(9));
        Assert.That(root.Span.StartIndex, Is.EqualTo(2));
        Assert.That(root.Span.EndIndex, Is.EqualTo(8));
        Assert.That(sequence.Span.StartIndex, Is.EqualTo(4));
        Assert.That(sequence.Span.EndIndex, Is.EqualTo(7));
        Assert.That(pair.Span.StartIndex, Is.EqualTo(3));
        Assert.That(pair.Span.EndIndex, Is.EqualTo(7));
    }

    [Test]
    public void Compose_IntegratedComposerFeatures_PreserveComplexKeysRecursiveAliasesCommentsAndSpans()
    {
        global::yaml.YamlEvent valueMapping = global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Flow, Span(7));
        valueMapping.Anchor = "value";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Comment("# complex pair", Span(3)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Flow, Span(4)),
            global::yaml.YamlEvent.Scalar("key", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(6)),
            valueMapping,
            global::yaml.YamlEvent.Scalar("self", global::yaml.YamlScalarStyle.Plain, Span(8)),
            global::yaml.YamlEvent.Alias("value", Span(9)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(10)),
            global::yaml.YamlEvent.Comment("# trailing", SpanOnLine(11, 11)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(12)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(13)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(14))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlPair pair = document.Root!.Pairs.Single();
        global::yaml.YamlNode value = pair.Value!;

        Assert.That(result.Success, Is.True);
        Assert.That(pair.IsComplexKey(), Is.True);
        Assert.That(pair.Key!.Children.Single().Value, Is.EqualTo("key"));
        Assert.That(pair.LeadingComments.Single(), Is.EqualTo("# complex pair"));
        Assert.That(pair.TrailingComments.Single(), Is.EqualTo("# trailing"));
        Assert.That(value.Anchor, Is.EqualTo("value"));
        Assert.That(document.Anchors.Find("value"), Is.SameAs(value));
        Assert.That(value.Pairs.Single().Value!.AliasTarget, Is.SameAs(value));
        Assert.That(value.TrailingComments.Single(), Is.EqualTo("# trailing"));
        Assert.That(value.Span.StartIndex, Is.EqualTo(7));
        Assert.That(value.Span.EndIndex, Is.EqualTo(11));
        Assert.That(pair.Span.StartIndex, Is.EqualTo(4));
        Assert.That(pair.Span.EndIndex, Is.EqualTo(11));
    }

    [Test]
    public void Compose_PreserveCommentsFalse_DropsCommentEvents()
    {
        global::yaml.YamlComposerOptions options = new global::yaml.YamlComposerOptions();
        options.PreserveComments = false;

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Comment("# document", Span(1)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(2)),
            global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.Comment("# trailing", SpanOnLine(4, 4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(6))
        }, options);

        global::yaml.YamlDocument document = result.Stream.Documents[0];

        Assert.That(document.LeadingComments, Is.Empty);
        Assert.That(document.Root!.TrailingComments, Is.Empty);
    }

    [Test]
    public void Compose_NodeEvents_PreserveTagsAnchorsAndAliases()
    {
        global::yaml.YamlEvent mapping = global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2));
        mapping.Anchor = "root";
        mapping.Tag = "!root";
        global::yaml.YamlEvent value = global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, Span(4));
        value.Anchor = "item";
        value.Tag = "!item";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            mapping,
            global::yaml.YamlEvent.Scalar("key", global::yaml.YamlScalarStyle.Plain, Span(3)),
            value,
            global::yaml.YamlEvent.Scalar("copy", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Alias("item", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(7)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(8)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(9))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(root.Anchor, Is.EqualTo("root"));
        Assert.That(root.Tag, Is.EqualTo("!root"));
        Assert.That(root.Pairs[0].Value!.Anchor, Is.EqualTo("item"));
        Assert.That(root.Pairs[0].Value!.Tag, Is.EqualTo("!item"));
        Assert.That(root.Pairs[1].Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(root.Pairs[1].Value!.AliasName, Is.EqualTo("item"));
    }

    [Test]
    public void Compose_AnchoredNodes_RegisterInDocumentAnchorTable()
    {
        global::yaml.YamlEvent sequence = global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2));
        sequence.Anchor = "items";
        global::yaml.YamlEvent scalar = global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3));
        scalar.Anchor = "first";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            sequence,
            scalar,
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(6))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlNode root = document.Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(document.Anchors.Count(), Is.EqualTo(2));
        Assert.That(document.Anchors.Find("items"), Is.SameAs(root));
        Assert.That(document.Anchors.Find("first"), Is.SameAs(root.Children[0]));
    }

    [Test]
    public void Compose_DuplicateAnchors_ReplaceEarlierBinding()
    {
        global::yaml.YamlEvent first = global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3));
        first.Anchor = "same";
        global::yaml.YamlEvent second = global::yaml.YamlEvent.Scalar("two", global::yaml.YamlScalarStyle.Plain, Span(4));
        second.Anchor = "same";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            first,
            second,
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(7))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlNode root = document.Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Anchors.Count(), Is.EqualTo(1));
        Assert.That(document.Anchors.Find("same"), Is.SameAs(root.Children[1]));
        Assert.That(root.Children[1].Anchor, Is.EqualTo("same"));
    }

    [Test]
    public void Compose_AliasNodes_ResolveToRegisteredAnchorTarget()
    {
        global::yaml.YamlEvent scalar = global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3));
        scalar.Anchor = "first";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            scalar,
            global::yaml.YamlEvent.Alias("first", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(7))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(root.Children[1].Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(root.Children[1].AliasTarget, Is.SameAs(root.Children[0]));
    }

    [Test]
    public void Compose_UnknownAlias_AddsDiagnostic()
    {
        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.Alias("missing", Span(2)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(3)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(4))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLC007"));
        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(root.AliasTarget, Is.Null);
    }

    [Test]
    public void Compose_RecursiveAlias_ResolvesToOpenCollectionWhenAllowed()
    {
        global::yaml.YamlEvent sequence = global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2));
        sequence.Anchor = "self";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            sequence,
            global::yaml.YamlEvent.Alias("self", Span(3)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(6))
        }, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(root.Children.Single().AliasTarget, Is.SameAs(root));
    }

    [Test]
    public void Compose_RecursiveAlias_AddsDiagnosticWhenDisabled()
    {
        global::yaml.YamlComposerOptions options = new global::yaml.YamlComposerOptions();
        options.AllowRecursiveAliases = false;
        global::yaml.YamlEvent sequence = global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2));
        sequence.Anchor = "self";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            sequence,
            global::yaml.YamlEvent.Alias("self", Span(3)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(6))
        }, options);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLC008"));
        Assert.That(root.Children.Single().AliasTarget, Is.Null);
    }

    [Test]
    public void Compose_AliasResolutionCanBeDisabled()
    {
        global::yaml.YamlComposerOptions options = new global::yaml.YamlComposerOptions();
        options.ResolveAliases = false;
        global::yaml.YamlEvent scalar = global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(3));
        scalar.Anchor = "first";

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            scalar,
            global::yaml.YamlEvent.Alias("first", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.SequenceEnd, "", Span(5)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(7))
        }, options);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(root.Children[1].AliasTarget, Is.Null);
    }

    [Test]
    public void Compose_DuplicateScalarKeys_DefaultPolicyAddsErrorAndPreservesPairs()
    {
        global::yaml.YamlComposerResult result = ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy.Error);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLC009"));
        Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Error));
        Assert.That(root.Pairs.Select(pair => pair.Value!.Value), Is.EqualTo(new string[] { "one", "two" }));
    }

    [Test]
    public void Compose_DuplicateScalarKeys_WarnPolicyAddsWarningAndPreservesPairs()
    {
        global::yaml.YamlComposerResult result = ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy.Warn);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLC009"));
        Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Warning));
        Assert.That(root.Pairs.Select(pair => pair.Value!.Value), Is.EqualTo(new string[] { "one", "two" }));
    }

    [Test]
    public void Compose_DuplicateScalarKeys_FirstWinsPolicyKeepsFirstPair()
    {
        global::yaml.YamlComposerResult result = ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy.FirstWins);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(root.Pairs.Count, Is.EqualTo(1));
        Assert.That(root.Pairs[0].Value!.Value, Is.EqualTo("one"));
    }

    [Test]
    public void Compose_DuplicateScalarKeys_LastWinsPolicyKeepsLastPair()
    {
        global::yaml.YamlComposerResult result = ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy.LastWins);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(root.Pairs.Count, Is.EqualTo(1));
        Assert.That(root.Pairs[0].Value!.Value, Is.EqualTo("two"));
    }

    [Test]
    public void Compose_DuplicateScalarKeys_PreserveAllPolicyKeepsBothPairsWithoutDiagnostic()
    {
        global::yaml.YamlComposerResult result = ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy.PreserveAll);

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(root.Pairs.Select(pair => pair.Value!.Value), Is.EqualTo(new string[] { "one", "two" }));
    }

    [Test]
    public void Compose_DiagnosticEvents_PropagateToResultStreamAndDocument()
    {
        global::yaml.YamlDiagnostic diagnostic = global::yaml.YamlDiagnostic.Error("YAMLT", "test", Span(2));

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.DiagnosticEvent(diagnostic),
            global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(4)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(5))
        }, new global::yaml.YamlComposerOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single(), Is.SameAs(diagnostic));
        Assert.That(result.Stream.Diagnostics.Single(), Is.SameAs(diagnostic));
        Assert.That(result.Stream.Documents[0].Diagnostics.Single(), Is.SameAs(diagnostic));
    }

    [Test]
    public void ComposeParserResult_ConvertsParsedMappingIntoDocumentGraph()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("name: Ada\nitems:\n  - one\n  - two\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(root.Pairs.Select(pair => pair.Key!.Value), Is.EqualTo(new string[] { "name", "items" }));
        Assert.That(root.Pairs[0].Value!.Value, Is.EqualTo("Ada"));
        Assert.That(root.Pairs[1].Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(root.Pairs[1].Value!.Children.Select(child => child.Value), Is.EqualTo(new string[] { "one", "two" }));
    }

    [Test]
    public void ComposeParserResult_RegistersLatestAnchorBinding()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("first: &same one\nsecond: &same two\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlNode root = document.Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(document.Anchors.Count(), Is.EqualTo(1));
        Assert.That(document.Anchors.Find("same"), Is.SameAs(root.Pairs[1].Value));
        Assert.That(root.Pairs[1].Value!.Anchor, Is.EqualTo("same"));
    }

    [Test]
    public void ComposeParserResult_ResolvesAliasesFromParsedEvents()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("first: &item one\nsecond: *item\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.True);
        Assert.That(root.Pairs[1].Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(root.Pairs[1].Value!.AliasTarget, Is.SameAs(root.Pairs[0].Value));
    }

    [Test]
    public void ComposeParserResult_StoresFlowSequenceAsComplexMappingKey()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("{[a, b]: value}\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlPair pair = result.Stream.Documents[0].Root!.Pairs.Single();

        Assert.That(result.Success, Is.True);
        Assert.That(pair.IsComplexKey(), Is.True);
        Assert.That(pair.Key!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(pair.Key!.Children.Select(child => child.Value), Is.EqualTo(new string[] { "a", "b" }));
        Assert.That(pair.Value!.Value, Is.EqualTo("value"));
    }

    [Test]
    public void ComposeParserResult_AttachesParsedCommentsAndBlankLines()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("# document\n\nkey: value # trailing\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlDocument document = result.Stream.Documents[0];
        global::yaml.YamlPair pair = document.Root!.Pairs.Single();

        Assert.That(result.Success, Is.True);
        Assert.That(document.LeadingComments.Single(), Is.EqualTo("# document"));
        Assert.That(document.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(pair.Value!.TrailingComments.Single(), Is.EqualTo("# trailing"));
        Assert.That(pair.TrailingComments.Single(), Is.EqualTo("# trailing"));
    }

    [Test]
    public void ComposeParserResult_PropagatesScannerTokenSpansToNodesAndPairs()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("key: value\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlPair pair = result.Stream.Documents[0].Root!.Pairs.Single();

        Assert.That(pair.Key!.Span.StartLine, Is.EqualTo(1));
        Assert.That(pair.Key!.Span.StartColumn, Is.EqualTo(1));
        Assert.That(pair.Value!.Span.StartLine, Is.EqualTo(1));
        Assert.That(pair.Value!.Span.StartColumn, Is.EqualTo(6));
        Assert.That(pair.Span.StartColumn, Is.EqualTo(pair.Key!.Span.StartColumn));
        Assert.That(pair.Span.EndColumn, Is.EqualTo(pair.Value!.Span.EndColumn));
    }

    [Test]
    public void ComposeParserResult_DetectsDuplicateScalarKeys()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("key: one\nkey: two\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        global::yaml.YamlComposerResult result = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlNode root = result.Stream.Documents[0].Root!;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLC009" }));
        Assert.That(root.Pairs.Count, Is.EqualTo(2));
        Assert.That(root.Pairs.Select(pair => pair.Value!.Value), Is.EqualTo(new string[] { "one", "two" }));
    }

    static global::yaml.YamlComposerResult ComposeDuplicateScalarMapping(global::yaml.YamlDuplicateKeyPolicy policy)
    {
        global::yaml.YamlComposerOptions options = new global::yaml.YamlComposerOptions();
        options.DuplicateKeyPolicy = policy;

        return global::yaml.YamlComposer.Compose(new global::yaml.YamlEvent[]
        {
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamStart, "", Span(0)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentStart, "", Span(1)),
            global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, Span(2)),
            global::yaml.YamlEvent.Scalar("key", global::yaml.YamlScalarStyle.Plain, Span(3)),
            global::yaml.YamlEvent.Scalar("one", global::yaml.YamlScalarStyle.Plain, Span(4)),
            global::yaml.YamlEvent.Scalar("key", global::yaml.YamlScalarStyle.Plain, Span(5)),
            global::yaml.YamlEvent.Scalar("two", global::yaml.YamlScalarStyle.Plain, Span(6)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.MappingEnd, "", Span(7)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.DocumentEnd, "", Span(8)),
            global::yaml.YamlEvent.Create(global::yaml.YamlEventKind.StreamEnd, "", Span(9))
        }, options);
    }

    static global::yaml.YamlSourceSpan Span(int index)
    {
        return global::yaml.YamlSourceSpan.FromBounds(index, index + 1, 1, index + 1, index + 1, 2);
    }

    static global::yaml.YamlSourceSpan SpanOnLine(int index, int line)
    {
        return global::yaml.YamlSourceSpan.FromBounds(index, line, 1, index + 1, line, 2);
    }
}
