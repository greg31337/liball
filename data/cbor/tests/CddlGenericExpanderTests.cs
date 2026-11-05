using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlGenericExpanderTests
{
    [Test]
    public void ReportsMissingAndUnexpectedGenericArgumentsAtUseSites()
    {
        string source =
            "root = [box, plain<1>]\n"
            + "box<t> = [t]\n"
            + "plain = 2";
        CddlGenericExpansionResult result = Expand(source, NoPrelude());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_GENERIC_ARITY_MISMATCH",
                    "CDDL_COMPILE_GENERIC_ARITY_MISMATCH"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.All.EqualTo("root"));
            Assert.That(result.Diagnostics.Select(item => item.Span.Start),
                Is.EqualTo(new int[]
                {
                    source.IndexOf("box", StringComparison.Ordinal),
                    source.IndexOf("plain<", StringComparison.Ordinal)
                }));
            Assert.That(result.Instantiations, Is.Empty);
        });
    }

    [Test]
    public void RejectsArgumentsAppliedToATypeParameter()
    {
        CddlGenericExpansionResult result = Expand(
            "root = higher<1>\nhigher<t> = t<2>",
            NoPrelude());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_GENERIC_ARITY_MISMATCH"));
            Assert.That(result.Diagnostics[0].RuleName, Is.EqualTo("higher"));
            Assert.That(result.Diagnostics[0].Message,
                Does.Contain("expects 0 generic argument(s)"));
        });
    }

    [Test]
    public void SubstitutionDoesNotCaptureNamesInsideArguments()
    {
        string source =
            "root = pair<u, 1>\n"
            + "u = 7\n"
            + "pair<t,u> = t / u";
        CddlGenericExpansionResult result = Expand(source, NoPrelude());
        CddlRule instantiated = result.Instantiations.Single().InstantiatedRule;
        CddlNode value = instantiated.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(value.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(value.Children[0].Kind,
                Is.EqualTo(CddlNodeKind.RuleReference));
            Assert.That(value.Children[0].Name, Is.EqualTo("u"));
            Assert.That(value.Children[0].Span.Start,
                Is.EqualTo(source.IndexOf("u, 1", StringComparison.Ordinal)));
            Assert.That(value.Children[1].Kind,
                Is.EqualTo(CddlNodeKind.Literal));
            Assert.That(value.Children[1].IntegerValue,
                Is.EqualTo(new BigInteger(1)));
        });
    }

    [Test]
    public void ExpandsNestedApplicationsAndMemoizesStructuralArguments()
    {
        string source =
            "root = [outer<1>, outer<1>]\n"
            + "outer<t> = box<box<t>>\n"
            + "box<t> = [t]";
        CddlGenericExpansionResult result = Expand(source, NoPrelude());

        CddlGenericInstantiation outer = result.Instantiations.Single(item =>
            item.GenericRule.Name == "outer");
        CddlGenericInstantiation[] boxes = result.Instantiations
            .Where(item => item.GenericRule.Name == "box")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Instantiations, Has.Count.EqualTo(3));
            Assert.That(outer.Applications, Has.Count.EqualTo(2));
            Assert.That(boxes, Has.Length.EqualTo(2));
            Assert.That(boxes.Select(item => item.Arguments[0].Kind),
                Does.Contain(CddlNodeKind.Literal));
            Assert.That(boxes.Select(item => item.Arguments[0].Kind),
                Does.Contain(CddlNodeKind.RuleReference));
            Assert.That(result.Instantiations.Select(item =>
                    item.InstantiatedRule.Span.SourceName),
                Is.All.EqualTo("generic.cddl"));
        });
    }

    [Test]
    public void RecursiveApplicationReusesThePendingInstantiation()
    {
        CddlGenericExpansionResult result = Expand(
            "root = loop<1>\nloop<t> = [t, loop<t>]",
            NoPrelude());
        CddlGenericInstantiation loop = result.Instantiations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(loop.GenericRule.Name, Is.EqualTo("loop"));
            Assert.That(loop.Applications, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void EnforcesInstantiationAndExpansionDepthLimits()
    {
        string source =
            "root = outer<1>\n"
            + "outer<t> = middle<t>\n"
            + "middle<t> = inner<t>\n"
            + "inner<t> = [t]";
        CddlCompileOptions countOptions = NoPrelude();
        countOptions.MaxGenericInstantiations = 2;
        CddlGenericExpansionResult countResult = Expand(source, countOptions);
        CddlCompileOptions depthOptions = NoPrelude();
        depthOptions.MaxReferenceDepth = 1;
        CddlGenericExpansionResult depthResult = Expand(source, depthOptions);

        Assert.Multiple(() =>
        {
            Assert.That(countResult.Instantiations, Has.Count.EqualTo(2));
            Assert.That(countResult.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_GENERIC_INSTANTIATION_LIMIT"
                }));
            Assert.That(depthResult.Instantiations, Has.Count.EqualTo(1));
            Assert.That(depthResult.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_GENERIC_DEPTH_LIMIT"
                }));
        });
    }

    private static CddlGenericExpansionResult Expand(
        string source,
        CddlCompileOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "generic.cddl" });
        Assert.That(parse.Diagnostics, Is.Empty);
        CddlDocument document = parse.Document!;
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(document, options);
        CddlRuleTable table = new CddlRuleTable(rules, options);
        Assert.That(table.Diagnostics, Is.Empty);
        CddlResolutionResult resolution =
            new CddlReferenceResolver(table, document, options).ResolveDocument();
        Assert.That(resolution.Diagnostics, Is.Empty);
        return new CddlGenericExpander(
            table,
            resolution,
            options
        ).ExpandDocument();
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }
}
