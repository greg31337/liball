using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTriviaTests
{
    [Test]
    public void Parse_DistinguishesBlankBeforeInlineAndFollowingTrivia()
    {
        string source = "# document\n\nname: Ada # inline\n# following\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(source, new global::yaml.YamlParserOptions());

        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());

        List<global::yaml.YamlTrivia> trivia = parser.Events
            .Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Comment)
            .Select(yamlEvent => yamlEvent.Trivia)
            .ToList();
        Assert.That(trivia.Select(item => item.Kind), Is.EqualTo(new global::yaml.YamlTriviaKind[]
        {
            global::yaml.YamlTriviaKind.Comment,
            global::yaml.YamlTriviaKind.BlankLine,
            global::yaml.YamlTriviaKind.Comment,
            global::yaml.YamlTriviaKind.Comment
        }));
        Assert.That(trivia.Select(item => item.Placement), Is.EqualTo(new global::yaml.YamlTriviaPlacement[]
        {
            global::yaml.YamlTriviaPlacement.Before,
            global::yaml.YamlTriviaPlacement.Before,
            global::yaml.YamlTriviaPlacement.Inline,
            global::yaml.YamlTriviaPlacement.Following
        }));
        Assert.That(trivia[2].Span.StartLine, Is.EqualTo(3));
    }

    [Test]
    public void Parse_AttachesPlacedTriviaToDocumentsNodesAndPairs()
    {
        string source = "# document\n\nfirst: one # inline\n# before second\nsecond: two\n# following\n";

        global::yaml.YamlDocument document = global::yaml.Yaml.Parse(source);

        global::yaml.YamlPair first = document.Root!.Pairs[0];
        global::yaml.YamlPair second = document.Root.Pairs[1];
        Assert.That(document.Trivia.Select(item => item.Kind), Is.EqualTo(new global::yaml.YamlTriviaKind[]
        {
            global::yaml.YamlTriviaKind.Comment,
            global::yaml.YamlTriviaKind.BlankLine
        }));
        Assert.That(first.Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Inline));
        Assert.That(first.Value!.Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Inline));
        Assert.That(second.Trivia.Select(item => item.Placement), Is.EqualTo(new global::yaml.YamlTriviaPlacement[]
        {
            global::yaml.YamlTriviaPlacement.Before,
            global::yaml.YamlTriviaPlacement.Following
        }));
        Assert.That(second.Key!.Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Before));
        Assert.That(second.Value!.Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Following));
    }

    [Test]
    public void Emit_PublicTriviaEdits_PreserveEveryPlacement()
    {
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("name: Ada\n");
        global::yaml.YamlPair pair = document.Root!.Pairs[0];
        pair.Trivia.Add(Trivia(global::yaml.YamlTriviaKind.Comment, global::yaml.YamlTriviaPlacement.Before, "# before"));
        pair.Trivia.Add(Trivia(global::yaml.YamlTriviaKind.BlankLine, global::yaml.YamlTriviaPlacement.Before, ""));
        pair.Trivia.Add(Trivia(global::yaml.YamlTriviaKind.Comment, global::yaml.YamlTriviaPlacement.Inline, "# inline"));
        pair.Trivia.Add(Trivia(global::yaml.YamlTriviaKind.Comment, global::yaml.YamlTriviaPlacement.Following, "# following"));

        string emitted = global::yaml.YamlEmitter.EmitDocument(document);

        Assert.That(emitted, Is.EqualTo("# before\n\nname: Ada # inline\n# following\n"));
    }

    [Test]
    public void CloneNode_DeepCopiesNodeAndPairTrivia()
    {
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("# before\nname: Ada # inline\n# following\n");

        global::yaml.YamlNode clone = global::yaml.YamlApi.Model.CloneNode(document.Root);

        global::yaml.YamlPair originalPair = document.Root!.Pairs[0];
        global::yaml.YamlPair clonedPair = clone.Pairs[0];
        Assert.That(clonedPair.Trivia, Is.Not.SameAs(originalPair.Trivia));
        Assert.That(clonedPair.Trivia[0], Is.Not.SameAs(originalPair.Trivia[0]));
        Assert.That(clonedPair.Trivia.Select(item => item.Placement), Is.EqualTo(originalPair.Trivia.Select(item => item.Placement)));
        Assert.That(clonedPair.Value!.Trivia[0], Is.Not.SameAs(originalPair.Value!.Trivia[0]));
    }

    [Test]
    public void EventCopy_DeepCopiesTriviaMetadata()
    {
        global::yaml.YamlEvent original = global::yaml.YamlEvent.Comment("# inline", global::yaml.YamlSourceSpan.FromBounds(2, 1, 3, 10, 1, 11));
        original.Trivia.Placement = global::yaml.YamlTriviaPlacement.Inline;

        global::yaml.YamlEvent copy = original.Copy();

        Assert.That(copy.Trivia, Is.Not.SameAs(original.Trivia));
        Assert.That(copy.Trivia.Kind, Is.EqualTo(global::yaml.YamlTriviaKind.Comment));
        Assert.That(copy.Trivia.Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Inline));
        Assert.That(copy.Trivia.Span, Is.Not.SameAs(original.Trivia.Span));
    }

    [Test]
    public void Emit_FlowCollectionTrivia_UsesValidMultilineFlowSyntax()
    {
        string source = "items: [one, # first\n  # before two\n  two]\n";

        global::yaml.YamlDocument document = global::yaml.Yaml.Parse(source);
        string emitted = global::yaml.YamlEmitter.EmitDocument(document);
        global::yaml.YamlDocument reparsed = global::yaml.Yaml.Parse(emitted);

        Assert.That(emitted, Is.EqualTo("items: [\n  one, # first\n  # before two\n  two\n]\n"));
        Assert.That(reparsed.Root!.Pairs[0].Value!.Children[0].Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Inline));
        Assert.That(reparsed.Root.Pairs[0].Value!.Children[1].Trivia.Single().Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Before));
    }

    public static global::yaml.YamlTrivia Trivia(global::yaml.YamlTriviaKind kind, global::yaml.YamlTriviaPlacement placement, string text)
    {
        global::yaml.YamlTrivia trivia = new global::yaml.YamlTrivia();
        trivia.Kind = kind;
        trivia.Placement = placement;
        trivia.Text = text;
        return trivia;
    }
}
