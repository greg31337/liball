using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlReferenceGraphBuilderTests
{
    [Test]
    public void BuildsStableEdgesAndStronglyConnectedComponents()
    {
        CddlReferenceGraphResult result = Build(
            "root = alias\n"
            + "alias = leaf / [* leaf]\n"
            + "leaf = 1",
            NoPrelude());
        CddlReferenceGraphNode root = FindRule(result, "root");
        CddlReferenceGraphNode alias = FindRule(result, "alias");
        CddlReferenceGraphNode leaf = FindRule(result, "leaf");

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Nodes.Select(item => item.Rule.Name),
                Is.EqualTo(new string[] { "root", "alias", "leaf" }));
            Assert.That(result.Edges, Has.Count.EqualTo(3));
            Assert.That(root.Outgoing.Single().Target, Is.SameAs(alias));
            Assert.That(root.Outgoing.Single().IsRequired, Is.True);
            Assert.That(alias.Outgoing.Select(item => item.Target),
                Is.All.SameAs(leaf));
            Assert.That(alias.Outgoing.Select(item => item.IsRequired),
                Is.All.False);
            Assert.That(result.Components, Has.Count.EqualTo(3));
            Assert.That(result.Nodes.Select(item => item.IsRecursive),
                Is.All.False);
            Assert.That(result.Nodes.Select(item => item.IsProductive),
                Is.All.True);
        });
    }

    [Test]
    public void AcceptsProductiveDirectAndMutualRecursion()
    {
        CddlReferenceGraphResult result = Build(
            "root = list\n"
            + "list = [* list]\n"
            + "first = second / 1\n"
            + "second = first",
            NoPrelude());
        CddlReferenceGraphNode list = FindRule(result, "list");
        CddlReferenceGraphNode first = FindRule(result, "first");
        CddlReferenceGraphNode second = FindRule(result, "second");

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(list.IsRecursive, Is.True);
            Assert.That(list.IsProductive, Is.True);
            Assert.That(first.IsRecursive, Is.True);
            Assert.That(second.IsRecursive, Is.True);
            Assert.That(first.ComponentIndex, Is.EqualTo(second.ComponentIndex));
            Assert.That(first.IsProductive, Is.True);
            Assert.That(second.IsProductive, Is.True);
            Assert.That(result.Components.Count(item => item.IsRecursive),
                Is.EqualTo(2));
            Assert.That(result.Components.Where(item => item.IsRecursive)
                    .Select(item => item.IsProductive),
                Is.All.True);
        });
    }

    [Test]
    public void ReportsEachNonProductiveRecursiveComponentOnce()
    {
        string source =
            "root = first\n"
            + "first = [second]\n"
            + "second = first\n"
            + "dead = dead";
        CddlReferenceGraphResult result = Build(source, NoPrelude());
        CddlReferenceGraphNode root = FindRule(result, "root");
        CddlReferenceGraphNode first = FindRule(result, "first");
        CddlReferenceGraphNode second = FindRule(result, "second");
        CddlReferenceGraphNode dead = FindRule(result, "dead");

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_NON_PRODUCTIVE_CYCLE",
                    "CDDL_COMPILE_NON_PRODUCTIVE_CYCLE"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "first", "dead" }));
            Assert.That(result.Diagnostics.Select(item => item.Span.Start),
                Is.EqualTo(new int[]
                {
                    source.IndexOf("first =", StringComparison.Ordinal),
                    source.IndexOf("dead =", StringComparison.Ordinal)
                }));
            Assert.That(root.IsRecursive, Is.False);
            Assert.That(root.IsProductive, Is.False);
            Assert.That(first.IsRecursive, Is.True);
            Assert.That(second.IsRecursive, Is.True);
            Assert.That(first.IsProductive, Is.False);
            Assert.That(second.IsProductive, Is.False);
            Assert.That(dead.IsRecursive, Is.True);
            Assert.That(dead.IsProductive, Is.False);
        });
    }

    [Test]
    public void CalculatesEmptyMatchingAndMinimumGroupItems()
    {
        CddlReferenceGraphResult result = Build(
            "root = [empty, optional, fixed]\n"
            + "empty = ()\n"
            + "optional = (? item,)\n"
            + "fixed = (first, 2*3 second,)\n"
            + "item = 1\n"
            + "first = 2\n"
            + "second = 3",
            NoPrelude());
        CddlReferenceGraphNode root = FindRule(result, "root");
        CddlReferenceGraphNode empty = FindRule(result, "empty");
        CddlReferenceGraphNode optional = FindRule(result, "optional");
        CddlReferenceGraphNode fixedGroup = FindRule(result, "fixed");

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.MinimumItems, Is.EqualTo(1));
            Assert.That(root.CanMatchEmpty, Is.False);
            Assert.That(empty.IsProductive, Is.True);
            Assert.That(empty.CanMatchEmpty, Is.True);
            Assert.That(empty.MinimumItems, Is.Zero);
            Assert.That(optional.CanMatchEmpty, Is.True);
            Assert.That(optional.MinimumItems, Is.Zero);
            Assert.That(fixedGroup.CanMatchEmpty, Is.False);
            Assert.That(fixedGroup.MinimumItems, Is.EqualTo(3));
        });
    }

    [Test]
    public void ClassifiesMemoizedGenericRecursion()
    {
        CddlReferenceGraphResult productive = Build(
            "root = chain<1>\nchain<t> = t / [chain<t>]",
            NoPrelude());
        CddlReferenceGraphNode chain = productive.Nodes.Single(item =>
            item.Rule.IsGenericInstantiation);
        CddlReferenceGraphResult nonProductive = Build(
            "root = loop<1>\nloop<t> = loop<t>",
            NoPrelude());
        CddlReferenceGraphNode loop = nonProductive.Nodes.Single(item =>
            item.Rule.IsGenericInstantiation);

        Assert.Multiple(() =>
        {
            Assert.That(productive.Diagnostics, Is.Empty);
            Assert.That(chain.IsRecursive, Is.True);
            Assert.That(chain.IsProductive, Is.True);
            Assert.That(nonProductive.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(nonProductive.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_NON_PRODUCTIVE_CYCLE"));
            Assert.That(loop.IsRecursive, Is.True);
            Assert.That(loop.IsProductive, Is.False);
        });
    }

    [Test]
    public void CalculatesPropertiesAcrossTheStandardPrelude()
    {
        CddlReferenceGraphResult result = Build(
            "root = uint",
            new CddlCompileOptions());
        CddlReferenceGraphNode root = FindRule(result, "root");
        CddlReferenceGraphNode unsignedInteger = FindRule(result, "uint");

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.IsProductive, Is.True);
            Assert.That(unsignedInteger.IsProductive, Is.True);
            Assert.That(result.Nodes.Select(item => item.MinimumItems),
                Is.All.GreaterThanOrEqualTo(0));
        });
    }

    private static CddlReferenceGraphNode FindRule(
        CddlReferenceGraphResult result,
        string name)
    {
        return result.Nodes.Single(item => item.Rule.Name == name);
    }

    private static CddlReferenceGraphResult Build(
        string source,
        CddlCompileOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "graph.cddl" });
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
        CddlNormalizationResult normalization = new CddlSchemaNormalizer(
            table,
            resolution,
            expansion,
            options
        ).NormalizeDocument();
        Assert.That(normalization.Diagnostics, Is.Empty);
        return new CddlReferenceGraphBuilder(
            normalization,
            options
        ).BuildGraph();
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }
}
