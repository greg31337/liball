using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlModelTests
{
    [Test]
    public void StreamAndDocument_StoreDocumentsRootDirectivesAndDiagnostics()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode root = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        global::yaml.YamlDirective directive = global::yaml.YamlDirective.YamlVersionDirective(global::yaml.YamlVersion.Version12, "1.2", global::yaml.YamlSourceSpan.Empty());

        document.SetRoot(root);
        document.AddDirective(directive);
        document.AddLeadingComment("# document");
        document.AddLeadingBlankLine("");
        document.AddDiagnostic(global::yaml.YamlDiagnostic.Warning("YAMLW", "warn", global::yaml.YamlSourceSpan.Empty()));
        document.Anchors.Register("root", root, global::yaml.YamlSourceSpan.Empty());
        stream.AddDocument(document);
        stream.AddDiagnostic(global::yaml.YamlDiagnostic.Info("YAMLI", "info", global::yaml.YamlSourceSpan.Empty()));

        Assert.That(stream.DocumentCount(), Is.EqualTo(1));
        Assert.That(stream.HasDiagnostics(), Is.True);
        Assert.That(document.HasRoot(), Is.True);
        Assert.That(document.Root, Is.SameAs(root));
        Assert.That(document.Directives.Single(), Is.SameAs(directive));
        Assert.That(document.LeadingComments.Single(), Is.EqualTo("# document"));
        Assert.That(document.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(document.Diagnostics.Single().Code, Is.EqualTo("YAMLW"));
        Assert.That(document.Anchors.Find("root"), Is.SameAs(root));
    }

    [Test]
    public void NodeFactories_CreateScalarSequenceMappingAliasAndEmptyNodes()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(1, 1, 2, 3, 1, 4);

        global::yaml.YamlNode empty = global::yaml.YamlNode.Empty(span);
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.SingleQuoted, span);
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, span);
        global::yaml.YamlNode mapping = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, span);
        global::yaml.YamlNode alias = global::yaml.YamlNode.Alias("target", span);

        sequence.AddChild(scalar);
        mapping.AddPair(global::yaml.YamlPair.Create(global::yaml.YamlNode.Scalar("key", global::yaml.YamlScalarStyle.Plain, span), scalar, span));
        scalar.AddLeadingComment("# before");
        scalar.AddTrailingComment("# after");
        scalar.AddLeadingBlankLine("");
        scalar.AddTrailingBlankLine("");

        Assert.That(empty.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(scalar.IsScalar(), Is.True);
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
        Assert.That(sequence.IsSequence(), Is.True);
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(sequence.Children.Single(), Is.SameAs(scalar));
        Assert.That(mapping.IsMapping(), Is.True);
        Assert.That(mapping.Pairs.Single().Value, Is.SameAs(scalar));
        Assert.That(alias.IsAlias(), Is.True);
        Assert.That(alias.AliasName, Is.EqualTo("target"));
        Assert.That(scalar.LeadingComments.Single(), Is.EqualTo("# before"));
        Assert.That(scalar.TrailingComments.Single(), Is.EqualTo("# after"));
        Assert.That(scalar.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(scalar.TrailingBlankLines.Count, Is.EqualTo(1));
    }

    [Test]
    public void NodeFactoryHelpers_CreateCommonNodeShapesWithDefaults()
    {
        global::yaml.YamlNode empty = global::yaml.YamlNodeFactory.Empty();
        global::yaml.YamlNode plain = global::yaml.YamlNodeFactory.Scalar("value");
        global::yaml.YamlNode single = global::yaml.YamlNodeFactory.SingleQuotedScalar("value");
        global::yaml.YamlNode literal = global::yaml.YamlNodeFactory.LiteralScalar("line\n");
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.SequenceWithChild(plain);
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.MappingWithPair(global::yaml.YamlNodeFactory.Pair("key", single));
        global::yaml.YamlNode flowSequence = global::yaml.YamlNodeFactory.FlowSequence();
        global::yaml.YamlNode flowMapping = global::yaml.YamlNodeFactory.FlowMapping();

        Assert.That(empty.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(plain.Value, Is.EqualTo("value"));
        Assert.That(plain.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Plain));
        Assert.That(single.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
        Assert.That(literal.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Literal));
        Assert.That(sequence.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(sequence.Children.Single(), Is.SameAs(plain));
        Assert.That(mapping.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(mapping.Pairs.Single().Key!.Value, Is.EqualTo("key"));
        Assert.That(mapping.Pairs.Single().Value, Is.SameAs(single));
        Assert.That(flowSequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(flowMapping.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
    }

    [Test]
    public void NodeFactoryHelpers_CreateTaggedAnchoredAliasesAndPairs()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(2, 1, 3, 8, 1, 10);
        global::yaml.YamlNode key = global::yaml.YamlNodeFactory.TaggedScalar("id", "!key");
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.WithAnchor(
            global::yaml.YamlNodeFactory.Scalar("42", global::yaml.YamlScalarStyle.Plain, span),
            "answer");
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.AliasTo("answer", value);
        global::yaml.YamlPair pair = global::yaml.YamlNodeFactory.Pair(key, alias, span);
        global::yaml.YamlPair taggedKeyPair = global::yaml.YamlNodeFactory.PairWithTaggedKey("kind", "!kind", value);

        Assert.That(key.Tag, Is.EqualTo("!key"));
        Assert.That(value.Anchor, Is.EqualTo("answer"));
        Assert.That(value.Span, Is.SameAs(span));
        Assert.That(alias.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(alias.AliasName, Is.EqualTo("answer"));
        Assert.That(alias.AliasTarget, Is.SameAs(value));
        Assert.That(pair.Key, Is.SameAs(key));
        Assert.That(pair.Value, Is.SameAs(alias));
        Assert.That(pair.Span, Is.SameAs(span));
        Assert.That(taggedKeyPair.Key!.Tag, Is.EqualTo("!kind"));
        Assert.That(taggedKeyPair.Value, Is.SameAs(value));
    }

    [Test]
    public void NodeFactoryHelpers_PairUsesEmptyNodesForNullInputsAndTracksComplexKeys()
    {
        global::yaml.YamlNode complexKey = global::yaml.YamlNodeFactory.SequenceWithChild(global::yaml.YamlNodeFactory.Scalar("a"));
        global::yaml.YamlPair complexPair = global::yaml.YamlNodeFactory.Pair(complexKey, global::yaml.YamlNodeFactory.Scalar("value"));
        global::yaml.YamlPair nullPair = global::yaml.YamlNodeFactory.Pair((global::yaml.YamlNode)null!, (global::yaml.YamlNode)null!);

        Assert.That(complexPair.IsComplexKey(), Is.True);
        Assert.That(nullPair.Key!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(nullPair.Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
    }

    [Test]
    public void Pair_StoresKeyValueSpanAndComments()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(0, 1, 1, 4, 1, 5);
        global::yaml.YamlNode key = global::yaml.YamlNode.Scalar("key", global::yaml.YamlScalarStyle.Plain, span);
        global::yaml.YamlNode value = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, span);

        global::yaml.YamlPair pair = global::yaml.YamlPair.Create(key, value, span);
        pair.AddLeadingComment("# key");
        pair.AddTrailingComment("# value");
        pair.AddLeadingBlankLine("");
        pair.AddTrailingBlankLine("");

        Assert.That(pair.HasKey(), Is.True);
        Assert.That(pair.HasValue(), Is.True);
        Assert.That(pair.Key, Is.SameAs(key));
        Assert.That(pair.Value, Is.SameAs(value));
        Assert.That(pair.Span, Is.SameAs(span));
        Assert.That(pair.IsComplexKey(), Is.False);
        Assert.That(pair.LeadingComments.Single(), Is.EqualTo("# key"));
        Assert.That(pair.TrailingComments.Single(), Is.EqualTo("# value"));
        Assert.That(pair.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(pair.TrailingBlankLines.Count, Is.EqualTo(1));
    }

    [Test]
    public void Pair_TracksComplexCollectionKeys()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.Empty();
        global::yaml.YamlNode scalarKey = global::yaml.YamlNode.Scalar("key", global::yaml.YamlScalarStyle.Plain, span);
        global::yaml.YamlNode sequenceKey = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, span);
        sequenceKey.AddChild(global::yaml.YamlNode.Scalar("a", global::yaml.YamlScalarStyle.Plain, span));
        global::yaml.YamlNode value = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, span);

        global::yaml.YamlPair pair = global::yaml.YamlPair.Create(sequenceKey, value, span);

        Assert.That(pair.IsComplexKey(), Is.True);
        Assert.That(global::yaml.YamlPair.KeyIsComplex(sequenceKey), Is.True);
        Assert.That(global::yaml.YamlPair.KeyIsComplex(scalarKey), Is.False);

        pair.Set(scalarKey, value);

        Assert.That(pair.IsComplexKey(), Is.False);
    }

    [Test]
    public void NodeClone_DeepCopiesNodesPairsMetadataAndSpans()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(1, 1, 1, 5, 1, 5);
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.Tag = "!root";
        root.Anchor = "root";
        root.Span = span;
        root.AddLeadingComment("# root");
        global::yaml.YamlNode value = global::yaml.YamlNodeFactory.Scalar("Ada");
        value.Tag = "!!str";
        global::yaml.YamlPair pair = global::yaml.YamlNodeFactory.Pair("name", value);
        pair.AddTrailingComment("# name");
        root.AddMappingPair(pair);

        global::yaml.YamlNode clone = root.Clone();

        Assert.That(clone, Is.Not.SameAs(root));
        Assert.That(clone.Kind, Is.EqualTo(root.Kind));
        Assert.That(clone.Tag, Is.EqualTo("!root"));
        Assert.That(clone.Anchor, Is.EqualTo("root"));
        Assert.That(clone.Span, Is.Not.SameAs(root.Span));
        Assert.That(clone.Span.StartIndex, Is.EqualTo(root.Span.StartIndex));
        Assert.That(clone.LeadingComments, Is.Not.SameAs(root.LeadingComments));
        Assert.That(clone.LeadingComments.Single(), Is.EqualTo("# root"));
        Assert.That(clone.Pairs.Single(), Is.Not.SameAs(pair));
        Assert.That(clone.Pairs.Single().TrailingComments.Single(), Is.EqualTo("# name"));
        Assert.That(clone.FindMappingValue("name"), Is.Not.SameAs(value));
        Assert.That(clone.FindMappingValue("name")!.Value, Is.EqualTo("Ada"));

        clone.FindMappingValue("name")!.Value = "Grace";

        Assert.That(value.Value, Is.EqualTo("Ada"));
    }

    [Test]
    public void NodeClone_RetargetsAliasesToClonedNodes()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("value"), "item");
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.AliasTo("item", target);
        root.AddMappingValue("target", target);
        root.AddMappingValue("alias", alias);

        global::yaml.YamlNode clone = root.Clone();
        global::yaml.YamlNode clonedTarget = clone.FindMappingValue("target")!;
        global::yaml.YamlNode clonedAlias = clone.FindMappingValue("alias")!;

        Assert.That(clonedTarget, Is.Not.SameAs(target));
        Assert.That(clonedAlias, Is.Not.SameAs(alias));
        Assert.That(clonedAlias.AliasTarget, Is.SameAs(clonedTarget));
        Assert.That(clonedAlias.AliasTarget, Is.Not.SameAs(target));
    }

    [Test]
    public void NodeClone_HandlesRecursiveAliasTargets()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.Anchor = "root";
        global::yaml.YamlNode self = global::yaml.YamlNodeFactory.AliasTo("root", root);
        root.AddMappingValue("self", self);

        global::yaml.YamlNode clone = root.Clone();
        global::yaml.YamlNode clonedSelf = clone.FindMappingValue("self")!;

        Assert.That(clone, Is.Not.SameAs(root));
        Assert.That(clonedSelf.AliasTarget, Is.SameAs(clone));
    }

    [Test]
    public void PairClone_CopiesNullsCommentsAndComplexKeyState()
    {
        global::yaml.YamlPair empty = new global::yaml.YamlPair();
        empty.AddLeadingComment("# before");
        global::yaml.YamlPair emptyClone = empty.Clone();
        global::yaml.YamlNode key = global::yaml.YamlNodeFactory.SequenceWithChild(global::yaml.YamlNodeFactory.Scalar("a"));
        global::yaml.YamlPair complex = global::yaml.YamlNodeFactory.Pair(key, global::yaml.YamlNodeFactory.Scalar("value"));

        global::yaml.YamlPair complexClone = complex.Clone();

        Assert.That(emptyClone, Is.Not.SameAs(empty));
        Assert.That(emptyClone.Key, Is.Null);
        Assert.That(emptyClone.Value, Is.Null);
        Assert.That(emptyClone.LeadingComments.Single(), Is.EqualTo("# before"));
        Assert.That(complexClone.IsComplexKey(), Is.True);
        Assert.That(complexClone.Key, Is.Not.SameAs(key));
    }

    [Test]
    public void AliasHelpers_RenameAndRetargetSingleAliasNodes()
    {
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.Alias("old");
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("value"), "new");
        global::yaml.YamlNode scalar = global::yaml.YamlNodeFactory.Scalar("not-alias");

        bool renamed = alias.RenameAlias("renamed");
        bool retargeted = alias.RetargetAlias(target);
        bool scalarRenamed = scalar.RenameAlias("no");
        bool scalarRetargeted = scalar.RetargetAlias(target);

        Assert.That(renamed, Is.True);
        Assert.That(retargeted, Is.True);
        Assert.That(alias.AliasName, Is.EqualTo("new"));
        Assert.That(alias.AliasTarget, Is.SameAs(target));
        Assert.That(scalarRenamed, Is.False);
        Assert.That(scalarRetargeted, Is.False);
    }

    [Test]
    public void AliasHelpers_RenameAnchorsAndAliasesAcrossGraph()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("value"), "old");
        global::yaml.YamlNode firstAlias = global::yaml.YamlNodeFactory.AliasTo("old", target);
        global::yaml.YamlNode secondAlias = global::yaml.YamlNodeFactory.AliasTo("old", target);
        root.AddMappingValue("target", target);
        root.AddMappingValue("first", firstAlias);
        root.AddMappingValue("second", secondAlias);

        int changed = root.RenameAnchorAndAliases("old", "new");

        Assert.That(changed, Is.EqualTo(3));
        Assert.That(target.Anchor, Is.EqualTo("new"));
        Assert.That(firstAlias.AliasName, Is.EqualTo("new"));
        Assert.That(secondAlias.AliasName, Is.EqualTo("new"));
        Assert.That(firstAlias.AliasTarget, Is.SameAs(target));
    }

    [Test]
    public void AliasHelpers_RenameAliasesOnlyLeavesAnchorsUnchanged()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("value"), "old");
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.AliasTo("old", target);
        root.AddMappingValue("target", target);
        root.AddMappingValue("alias", alias);

        int changed = root.RenameAliases("old", "new");

        Assert.That(changed, Is.EqualTo(1));
        Assert.That(target.Anchor, Is.EqualTo("old"));
        Assert.That(alias.AliasName, Is.EqualTo("new"));
    }

    [Test]
    public void AliasHelpers_RetargetAliasesByNameAndTargetAcrossGraph()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode oldTarget = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("old"), "old");
        global::yaml.YamlNode newTarget = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("new"), "new");
        global::yaml.YamlNode firstAlias = global::yaml.YamlNodeFactory.AliasTo("old", oldTarget);
        global::yaml.YamlNode secondAlias = global::yaml.YamlNodeFactory.AliasTo("old", oldTarget);
        global::yaml.YamlNode nested = global::yaml.YamlNodeFactory.SequenceWithChild(secondAlias);
        global::yaml.YamlNode selfAlias = global::yaml.YamlNodeFactory.AliasTo("root", root);
        root.Anchor = "root";
        root.AddMappingValue("old", oldTarget);
        root.AddMappingValue("new", newTarget);
        root.AddMappingValue("first", firstAlias);
        root.AddMappingValue("nested", nested);
        root.AddMappingValue("self", selfAlias);

        int byName = root.RetargetAliases("old", newTarget);
        int byTarget = root.RetargetAliasesByTarget(newTarget, oldTarget);

        Assert.That(byName, Is.EqualTo(2));
        Assert.That(firstAlias.AliasTarget, Is.SameAs(oldTarget));
        Assert.That(secondAlias.AliasTarget, Is.SameAs(oldTarget));
        Assert.That(firstAlias.AliasName, Is.EqualTo("old"));
        Assert.That(secondAlias.AliasName, Is.EqualTo("old"));
        Assert.That(byTarget, Is.EqualTo(2));
    }

    [Test]
    public void MergeKeys_MergeAliasMappingAndRemoveMergePair()
    {
        global::yaml.YamlNode defaults = global::yaml.YamlNodeFactory.Mapping();
        defaults.Anchor = "defaults";
        defaults.AddMappingValue("a", global::yaml.YamlNodeFactory.Scalar("1"));
        defaults.AddMappingValue("b", global::yaml.YamlNodeFactory.Scalar("base"));
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingValue("before", global::yaml.YamlNodeFactory.Scalar("first"));
        mapping.AddMappingPair(global::yaml.YamlNodeFactory.Pair(
            global::yaml.YamlNodeFactory.Scalar("<<"),
            global::yaml.YamlNodeFactory.AliasTo("defaults", defaults)));
        mapping.AddMappingValue("b", global::yaml.YamlNodeFactory.Scalar("explicit"));

        int changed = mapping.ApplyMergeKeys();

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(mapping.FindMappingPair("<<"), Is.Null);
        Assert.That(mapping.Pairs.Select(pair => pair.Key!.Value), Is.EqualTo(new string[]
        {
            "before",
            "a",
            "b"
        }));
        Assert.That(mapping.FindMappingValue("a")!.Value, Is.EqualTo("1"));
        Assert.That(mapping.FindMappingValue("a"), Is.Not.SameAs(defaults.FindMappingValue("a")));
        Assert.That(mapping.FindMappingValue("b")!.Value, Is.EqualTo("explicit"));
    }

    [Test]
    public void MergeKeys_SequenceMergesEarlierMappingsBeforeLaterMappings()
    {
        global::yaml.YamlNode first = global::yaml.YamlNodeFactory.Mapping();
        first.AddMappingValue("shared", global::yaml.YamlNodeFactory.Scalar("first"));
        first.AddMappingValue("firstOnly", global::yaml.YamlNodeFactory.Scalar("1"));
        global::yaml.YamlNode second = global::yaml.YamlNodeFactory.Mapping();
        second.AddMappingValue("shared", global::yaml.YamlNodeFactory.Scalar("second"));
        second.AddMappingValue("secondOnly", global::yaml.YamlNodeFactory.Scalar("2"));
        global::yaml.YamlNode sources = global::yaml.YamlNodeFactory.Sequence();
        sources.AddSequenceItem(first);
        sources.AddSequenceItem(second);
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        mapping.AddMappingPair(global::yaml.YamlNodeFactory.Pair(
            global::yaml.YamlNodeFactory.Scalar("<<"),
            sources));

        mapping.ApplyMergeKeys();

        Assert.That(mapping.Pairs.Select(pair => pair.Key!.Value), Is.EqualTo(new string[]
        {
            "shared",
            "firstOnly",
            "secondOnly"
        }));
        Assert.That(mapping.FindMappingValue("shared")!.Value, Is.EqualTo("first"));
    }

    [Test]
    public void MergeKeys_ProcessNestedMappingsAndAvoidRecursiveAliasLoops()
    {
        global::yaml.YamlNode defaults = global::yaml.YamlNodeFactory.Mapping();
        defaults.AddMappingValue("enabled", global::yaml.YamlNodeFactory.Scalar("true"));
        global::yaml.YamlNode nested = global::yaml.YamlNodeFactory.Mapping();
        nested.AddMappingPair(global::yaml.YamlNodeFactory.Pair(
            global::yaml.YamlNodeFactory.Scalar("<<"),
            global::yaml.YamlNodeFactory.AliasTo("defaults", defaults)));
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.Anchor = "root";
        root.AddMappingValue("defaults", defaults);
        root.AddMappingValue("nested", nested);
        root.AddMappingValue("self", global::yaml.YamlNodeFactory.AliasTo("root", root));

        int changed = root.ApplyMergeKeys();

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(nested.FindMappingPair("<<"), Is.Null);
        Assert.That(nested.FindMappingValue("enabled")!.Value, Is.EqualTo("true"));
    }

    [Test]
    public void MergeKeys_ComposedParserResult_UsesSchemaTaggedMergeKeys()
    {
        string text = "defaults: &defaults { a: 1, b: base }\nitem:\n  <<: *defaults\n  b: explicit\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlSchemaOptions options = new global::yaml.YamlSchemaOptions();
        options.AllowMergeKeys = true;
        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, options);
        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;
        global::yaml.YamlNode item = root.FindMappingValue("item")!;

        int changed = item.ApplyMergeKeys();

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(item.FindMappingPair("<<"), Is.Null);
        Assert.That(item.FindMappingValue("a")!.Value, Is.EqualTo("1"));
        Assert.That(item.FindMappingValue("b")!.Value, Is.EqualTo("explicit"));
    }

    [Test]
    public void NodePathLookup_TraversesMappingKeysAndSequenceIndexes()
    {
        global::yaml.YamlNode root = BuildSampleGraph();

        global::yaml.YamlNode? name = root.FindPath("users[0].name");
        global::yaml.YamlNode? secondName = root.FindPath("$.users[1].name");
        global::yaml.YamlNode? missing = root.FindPath("users[9].name");
        global::yaml.YamlNode? invalid = root.FindPath("users[]");

        Assert.That(name!.Value, Is.EqualTo("Ada"));
        Assert.That(secondName!.Value, Is.EqualTo("Grace"));
        Assert.That(missing, Is.Null);
        Assert.That(invalid, Is.Null);
        Assert.That(root.FindPath("$"), Is.SameAs(root));
    }

    [Test]
    public void NodePathOperations_ReplaceUpdateAndRemoveExistingNodes()
    {
        global::yaml.YamlNode root = BuildSampleGraph();
        global::yaml.YamlNode replacement = global::yaml.YamlNodeFactory.Scalar("Augusta");

        bool replaced = root.ReplacePath("users[0].name", replacement);
        bool updated = root.UpdatePath("users[1].name", global::yaml.YamlNodeFactory.Scalar("Rear Admiral Hopper"));
        bool removed = root.RemovePath("users[0].name");
        bool missing = root.ReplacePath("users[9].name", global::yaml.YamlNodeFactory.Scalar("none"));

        Assert.That(replaced, Is.True);
        Assert.That(updated, Is.True);
        Assert.That(removed, Is.True);
        Assert.That(missing, Is.False);
        Assert.That(root.FindPath("users[0].name"), Is.Null);
        Assert.That(root.FindPath("users[1].name")!.Value, Is.EqualTo("Rear Admiral Hopper"));
    }

    [Test]
    public void SequenceOperations_AddInsertUpdateReplaceAndRemoveItems()
    {
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();

        bool addedA = sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("a"));
        bool addedC = sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("c"));
        bool insertedB = sequence.InsertSequenceItem(1, global::yaml.YamlNodeFactory.Scalar("b"));
        bool updated = sequence.UpdateSequenceItem(2, global::yaml.YamlNodeFactory.Scalar("updated"));
        bool replaced = sequence.ReplaceSequenceItem(0, global::yaml.YamlNodeFactory.Scalar("first"));
        bool removed = sequence.RemoveSequenceItem(1);
        bool badInsert = sequence.InsertSequenceItem(9, global::yaml.YamlNodeFactory.Scalar("bad"));
        bool badRemove = sequence.RemoveSequenceItem(9);

        Assert.That(addedA, Is.True);
        Assert.That(addedC, Is.True);
        Assert.That(insertedB, Is.True);
        Assert.That(updated, Is.True);
        Assert.That(replaced, Is.True);
        Assert.That(removed, Is.True);
        Assert.That(badInsert, Is.False);
        Assert.That(badRemove, Is.False);
        Assert.That(sequence.Children.Select(child => child.Value), Is.EqualTo(new string[]
        {
            "first",
            "updated"
        }));
        Assert.That(global::yaml.YamlNodeFactory.Mapping().AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("no")), Is.False);
    }

    [Test]
    public void MappingOperations_AddInsertUpdateReplaceAndRemovePairs()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlPair b = global::yaml.YamlNodeFactory.Pair("b", "2");
        global::yaml.YamlPair a = global::yaml.YamlNodeFactory.Pair("a", "1");

        bool added = mapping.AddMappingPair(b);
        bool inserted = mapping.InsertMappingPair(0, a);
        bool addedValue = mapping.AddMappingValue("c", global::yaml.YamlNodeFactory.Scalar("3"));
        bool updated = mapping.UpdateMappingValue("b", global::yaml.YamlNodeFactory.Scalar("20"));
        bool replaced = mapping.ReplaceMappingPair("a", global::yaml.YamlNodeFactory.Pair("alpha", "10"));
        bool removed = mapping.RemoveMappingKey("c");
        bool missingUpdate = mapping.UpdateMappingValue("missing", global::yaml.YamlNodeFactory.Scalar("x"));
        bool badAdd = global::yaml.YamlNodeFactory.Sequence().AddMappingPair(global::yaml.YamlNodeFactory.Pair("no", "no"));

        Assert.That(added, Is.True);
        Assert.That(inserted, Is.True);
        Assert.That(addedValue, Is.True);
        Assert.That(updated, Is.True);
        Assert.That(replaced, Is.True);
        Assert.That(removed, Is.True);
        Assert.That(missingUpdate, Is.False);
        Assert.That(badAdd, Is.False);
        Assert.That(mapping.Pairs.Select(pair => pair.Key!.Value), Is.EqualTo(new string[]
        {
            "alpha",
            "b"
        }));
        Assert.That(mapping.FindMappingValue("b")!.Value, Is.EqualTo("20"));
        Assert.That(mapping.FindMappingPair("c"), Is.Null);
    }

    [Test]
    public void DirectiveFactories_CreateYamlAndTagDirectives()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.Empty();

        global::yaml.YamlDirective yamlDirective = global::yaml.YamlDirective.YamlVersionDirective(global::yaml.YamlVersion.Version12, "1.2", span);
        global::yaml.YamlDirective tagDirective = global::yaml.YamlDirective.TagDirective("!e!", "tag:example.com,2026:", span);
        global::yaml.YamlTagDirective tag = global::yaml.YamlTagDirective.Create("!!", "tag:yaml.org,2002:", span);

        Assert.That(yamlDirective.IsYamlVersion(), Is.True);
        Assert.That(yamlDirective.Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(yamlDirective.Value, Is.EqualTo("1.2"));
        Assert.That(tagDirective.IsTag(), Is.True);
        Assert.That(tagDirective.Handle, Is.EqualTo("!e!"));
        Assert.That(tagDirective.Prefix, Is.EqualTo("tag:example.com,2026:"));
        Assert.That(tag.Handle, Is.EqualTo("!!"));
        Assert.That(tag.Prefix, Is.EqualTo("tag:yaml.org,2002:"));
    }

    [Test]
    public void AnchorTable_RegistersFindsReplacesDuplicatesAndClears()
    {
        global::yaml.YamlAnchorTable table = new global::yaml.YamlAnchorTable();
        global::yaml.YamlNode node = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        global::yaml.YamlNode replacement = global::yaml.YamlNode.Scalar("replacement", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());

        bool registered = table.Register("main", node, global::yaml.YamlSourceSpan.Empty());
        bool duplicate = table.Register("main", replacement, global::yaml.YamlSourceSpan.Empty());
        bool empty = table.Register("", node, global::yaml.YamlSourceSpan.Empty());

        Assert.That(registered, Is.True);
        Assert.That(duplicate, Is.True);
        Assert.That(empty, Is.False);
        Assert.That(table.Count(), Is.EqualTo(1));
        Assert.That(table.Contains("main"), Is.True);
        Assert.That(table.Find("main"), Is.SameAs(replacement));
        Assert.That(table.Find("missing"), Is.Null);

        table.Clear();

        Assert.That(table.Count(), Is.EqualTo(0));
    }

    static global::yaml.YamlNode BuildSampleGraph()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode users = global::yaml.YamlNodeFactory.Sequence();

        global::yaml.YamlNode first = global::yaml.YamlNodeFactory.Mapping();
        first.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));

        global::yaml.YamlNode second = global::yaml.YamlNodeFactory.Mapping();
        second.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Grace"));

        users.AddSequenceItem(first);
        users.AddSequenceItem(second);
        root.AddMappingValue("users", users);
        return root;
    }
}
