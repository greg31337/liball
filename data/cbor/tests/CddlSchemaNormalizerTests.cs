using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlSchemaNormalizerTests
{
    [Test]
    public void RemovesParenthesesAndFlattensEquivalentTypeChoices()
    {
        string source = "root = ((1 / 2) / (3 / 4))";
        CddlNormalizationResult result = Normalize(source, NoPrelude());
        CddlNormalizedNode root = result.RootRule!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(root.Children, Has.Count.EqualTo(4));
            Assert.That(root.Children.Select(item => item.IntegerValue),
                Is.EqualTo(new BigInteger[]
                {
                    new BigInteger(1),
                    new BigInteger(2),
                    new BigInteger(3),
                    new BigInteger(4)
                }));
            Assert.That(result.Nodes, Has.Count.EqualTo(5));
            Assert.That(result.SourceMap, Has.Count.EqualTo(result.Nodes.Count));
            Assert.That(result.SourceMap.Select(item => item.Start),
                Is.EqualTo(result.Nodes.Select(item => item.Span.Start)));
            Assert.That(root.Children.Select(item => item.Span.Start),
                Is.EqualTo(new int[]
                {
                    source.IndexOf("1", StringComparison.Ordinal),
                    source.IndexOf("2", StringComparison.Ordinal),
                    source.IndexOf("3", StringComparison.Ordinal),
                    source.IndexOf("4", StringComparison.Ordinal)
                }));
            Assert.That(result.Nodes.Select(item => item.Kind),
                Does.Not.Contain(CddlNodeKind.ParenthesizedType));
        });
    }

    [Test]
    public void PreservesGroupChoicesIncludingEmptyAlternatives()
    {
        CddlNormalizationResult result = Normalize(
            "root = [a, b // c //]\na = 1\nb = 2\nc = 3",
            NoPrelude());
        CddlNormalizedNode array = result.RootRule!.Value!;
        CddlNormalizedNode choice = array.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(choice.Children, Has.Count.EqualTo(3));
            Assert.That(choice.Children.Select(item => item.Kind),
                Is.All.EqualTo(CddlNodeKind.Group));
            Assert.That(choice.Children[0].Children, Has.Count.EqualTo(2));
            Assert.That(choice.Children[1].Children, Has.Count.EqualTo(1));
            Assert.That(choice.Children[2].Children, Is.Empty);
        });
    }

    [Test]
    public void PreservesOccurrencesLabelsCutsAndRepresentationConstraints()
    {
        string source =
            "root = {? label: #6.24(tstr), 2*3 tstr ^ => #2.1}";
        CddlNormalizationResult result = Normalize(
            source,
            new CddlCompileOptions());
        CddlNormalizedNode map = result.RootRule!.Value!;
        CddlNormalizedNode group = map.Children[0];
        CddlNormalizedNode labeled = group.Children[0];
        CddlNormalizedNode cut = group.Children[1];
        CddlNormalizedNode tag = labeled.Children[0];
        CddlNormalizedNode representation = cut.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(labeled.Name, Is.EqualTo("label"));
            Assert.That(labeled.Occurrence,
                Is.EqualTo(CddlOccurrenceKind.Optional));
            Assert.That(labeled.MinimumOccurrence, Is.Zero);
            Assert.That(labeled.MaximumOccurrence, Is.EqualTo(1));
            Assert.That(cut.IsCut, Is.True);
            Assert.That(cut.Occurrence,
                Is.EqualTo(CddlOccurrenceKind.Bounded));
            Assert.That(cut.MinimumOccurrence, Is.EqualTo(2));
            Assert.That(cut.MaximumOccurrence, Is.EqualTo(3));
            Assert.That(tag.Kind, Is.EqualTo(CddlNodeKind.Tag));
            Assert.That(tag.MajorType, Is.EqualTo(6));
            Assert.That(tag.ArgumentValue, Is.EqualTo(new BigInteger(24)));
            Assert.That(representation.Kind,
                Is.EqualTo(CddlNodeKind.Representation));
            Assert.That(representation.MajorType, Is.EqualTo(2));
            Assert.That(representation.ArgumentValue,
                Is.EqualTo(new BigInteger(1)));
            Assert.That(cut.Span.Start,
                Is.EqualTo(source.IndexOf("2*3", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void LinksReferencesToOrdinaryAndMemoizedGenericRules()
    {
        CddlNormalizationResult result = Normalize(
            "root = [plain, box<1>, box<1>, ~box<2>]\n"
            + "plain = 7\n"
            + "box<t> = [t]",
            NoPrelude());
        CddlNormalizedNode group = result.RootRule!.Value!.Children[0];
        CddlNormalizedNode plain = EntryValue(group.Children[0]);
        CddlNormalizedNode firstBox = EntryValue(group.Children[1]);
        CddlNormalizedNode secondBox = EntryValue(group.Children[2]);
        CddlNormalizedNode unwrappedBox = EntryValue(group.Children[3]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Rules, Has.Count.EqualTo(4));
            Assert.That(plain.ReferencedRule!.Name, Is.EqualTo("plain"));
            Assert.That(firstBox.ReferencedRule, Is.SameAs(
                secondBox.ReferencedRule));
            Assert.That(firstBox.ReferencedRule!.IsGenericInstantiation,
                Is.True);
            Assert.That(firstBox.Children, Is.Empty);
            Assert.That(secondBox.Children, Is.Empty);
            Assert.That(unwrappedBox.Kind, Is.EqualTo(CddlNodeKind.Unwrap));
            Assert.That(unwrappedBox.ReferencedRule!.IsGenericInstantiation,
                Is.True);
            Assert.That(unwrappedBox.ReferencedRule,
                Is.Not.SameAs(firstBox.ReferencedRule));
            Assert.That(unwrappedBox.Children, Is.Empty);
            Assert.That(result.Rules.Count(item => item.IsGenericInstantiation),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void StopsAtTheConfiguredNormalizedNodeLimit()
    {
        CddlCompileOptions options = NoPrelude();
        options.MaxNodes = 2;
        CddlNormalizationResult result = Normalize(
            "root = [1, 2, 3]",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Nodes, Has.Count.EqualTo(2));
            Assert.That(result.SourceMap, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_NODE_LIMIT"));
            Assert.That(result.Diagnostics[0].RuleName, Is.EqualTo("root"));
            Assert.That(result.Diagnostics[0].Span.SourceName,
                Is.EqualTo("normalize.cddl"));
        });
    }

    private static CddlNormalizedNode EntryValue(CddlNormalizedNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static CddlNormalizationResult Normalize(
        string source,
        CddlCompileOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "normalize.cddl" });
        Assert.That(parse.Diagnostics, Is.Empty);
        CddlDocument document = parse.Document!;
        CddlRule[] loaded = new CddlPreludeLoader().LoadRules(document, options);
        CddlRuleTable table = new CddlRuleTable(loaded, options);
        Assert.That(table.Diagnostics, Is.Empty);
        CddlResolutionResult resolution =
            new CddlReferenceResolver(table, document, options).ResolveDocument();
        Assert.That(resolution.Diagnostics, Is.Empty);
        CddlGenericExpansionResult expansion = new CddlGenericExpander(
            table,
            resolution,
            options
        ).ExpandDocument();
        Assert.That(expansion.Diagnostics, Is.Empty);
        return new CddlSchemaNormalizer(
            table,
            resolution,
            expansion,
            options
        ).NormalizeDocument();
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }

}
