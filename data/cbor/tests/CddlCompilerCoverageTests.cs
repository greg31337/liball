using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlCompilerCoverageTests
{
    private sealed class TestRuleResolver : ICddlRuleResolver
    {
        public List<CddlRule> Rules = new List<CddlRule>();

        public CddlRule? ResolveRule(string name, CddlRuleKind kind)
        {
            return Rules.FirstOrDefault(rule =>
                rule.Name == name && rule.Kind == kind);
        }
    }

    [TestCaseSource(typeof(CddlPrelude), nameof(CddlPrelude.RuleNames))]
    public void CompilesEveryStandardPreludeType(string preludeName)
    {
        CddlCompileResult result = Compile(
            "root = " + preludeName,
            new CddlCompileOptions(),
            "prelude-" + preludeName + ".cddl");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Schema!.RootRule!.Value!.Name,
                Is.EqualTo(preludeName));
            Assert.That(result.Schema.Rules.Single(rule =>
                rule.Name == preludeName).IsPrelude, Is.True);
        });
    }

    [Test]
    public void CompilesTypeAndGroupExtensionsAndSocketPlugs()
    {
        CddlCompileResult result = Compile(
            "root = [ordinary, * ordinary-group, $types, * $$entries]\n"
            + "ordinary = 1\n"
            + "ordinary /= 2\n"
            + "ordinary-group = (base: 7,)\n"
            + "ordinary-group //= (extended: 8,)\n"
            + "$types /= 3\n"
            + "$types /= 4\n"
            + "$$entries //= (first: 5,)\n"
            + "$$entries //= (second: 6,)",
            NoPrelude(),
            "extensions.cddl");
        CddlSchema schema = result.Schema!;
        CddlRule ordinary = FindRule(schema, "ordinary");
        CddlRule ordinaryGroup = FindRule(schema, "ordinary-group");
        CddlRule types = FindRule(schema, "types");
        CddlRule entries = FindRule(schema, "entries");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(schema.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[]
                {
                    "root", "ordinary", "ordinary-group", "types", "entries"
                }));
            Assert.That(ordinary.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(ordinary.Value!.Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(ordinary.Value.Children.Select(node =>
                    node.IntegerValue),
                Is.EqualTo(new BigInteger[]
                {
                    new BigInteger(1), new BigInteger(2)
                }));
            Assert.That(ordinaryGroup.Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(ordinaryGroup.Value!.Kind,
                Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(ordinaryGroup.Value.Children, Has.Count.EqualTo(2));
            Assert.That(types.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(types.Value!.Children.Select(node =>
                    node.IntegerValue),
                Is.EqualTo(new BigInteger[]
                {
                    new BigInteger(3), new BigInteger(4)
                }));
            Assert.That(entries.Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(entries.Value!.Kind,
                Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(entries.Value.Children, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void CompilesEmptySocketsAndMemoizesGenericInstantiations()
    {
        CddlCompileResult result = Compile(
            "root = [$optional, * $$optional, pair<1, 2>, pair<1, 2>, "
            + "pair<3, 4>]\n"
            + "pair<left, right> = [left, right]",
            NoPrelude(),
            "generic-sockets.cddl");
        CddlSchema schema = result.Schema!;
        CddlNode group = schema.RootRule!.Value!.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(schema.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "root", "pair<0>", "pair<1>" }));
            Assert.That(EntryValue(group.Children[0]).Name,
                Is.EqualTo("$optional"));
            Assert.That(EntryValue(group.Children[1]).Name,
                Is.EqualTo("$$optional"));
            Assert.That(EntryValue(group.Children[2]).Name,
                Is.EqualTo("pair<0>"));
            Assert.That(EntryValue(group.Children[3]).Name,
                Is.EqualTo("pair<0>"));
            Assert.That(EntryValue(group.Children[4]).Name,
                Is.EqualTo("pair<1>"));
        });
    }

    [TestCase("root = 1 / [root]", true)]
    [TestCase("root = first\nfirst = 1 / second\nsecond = first", true)]
    [TestCase("root = root", false)]
    [TestCase("root = first\nfirst = second\nsecond = first", false)]
    public void ClassifiesProductiveAndNonProductiveRecursion(
        string source,
        bool expectedSuccess)
    {
        CddlCompileResult result = Compile(
            source,
            NoPrelude(),
            "recursion.cddl");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.EqualTo(expectedSuccess));
            Assert.That(result.Schema, expectedSuccess
                ? Is.Not.Null
                : Is.Null);
            Assert.That(result.Diagnostics.Select(diagnostic =>
                    diagnostic.Code),
                expectedSuccess
                    ? Is.Empty
                    : Does.Contain("CDDL_COMPILE_NON_PRODUCTIVE_CYCLE"));
        });
    }

    [Test]
    public void CompilesTransitiveExternalTypeAndGroupRules()
    {
        TestRuleResolver resolver = new TestRuleResolver();
        resolver.Rules.Add(ParseDone(
            "remote = [dependent]",
            "external-type.cddl").Rules[0]);
        resolver.Rules.Add(ParseDone(
            "remote-group = (field: dependent,)",
            "external-group.cddl").Rules[0]);
        resolver.Rules.Add(ParseDone(
            "dependent = 7",
            "external-dependent.cddl").Rules[0]);
        CddlCompileOptions options = NoPrelude();
        options.RuleResolver = resolver;
        CddlCompileResult result = Compile(
            "root = [remote, remote-group]",
            options,
            "external-root.cddl");
        CddlSchema schema = result.Schema!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(schema.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[]
                {
                    "root", "remote", "remote-group", "dependent"
                }));
            Assert.That(schema.SourceMap.Select(span => span.SourceName),
                Does.Contain("external-root.cddl"));
            Assert.That(schema.SourceMap.Select(span => span.SourceName),
                Does.Contain("external-type.cddl"));
            Assert.That(schema.SourceMap.Select(span => span.SourceName),
                Does.Contain("external-group.cddl"));
            Assert.That(schema.SourceMap.Select(span => span.SourceName),
                Does.Contain("external-dependent.cddl"));
        });
    }

    [TestCase("root = [3*2 1]", "CDDL_COMPILE_OCCURRENCE_INVALID")]
    [TestCase("root = {int}", "CDDL_COMPILE_MAP_LABEL_MISSING")]
    [TestCase("root = tstr .size \"four\"",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = #0.31",
        "CDDL_COMPILE_REPRESENTATION_ARGUMENT_INVALID")]
    public void ReportsSemanticErrorsThroughThePublicCompiler(
        string source,
        string expectedCode)
    {
        CddlCompileResult result = Compile(
            source,
            new CddlCompileOptions(),
            "semantic-error.cddl");
        CddlDiagnostic diagnostic = result.Diagnostics.Single(item =>
            item.Code == expectedCode);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Schema, Is.Null);
            Assert.That(diagnostic.Severity,
                Is.EqualTo(CddlDiagnosticSeverity.Error));
            Assert.That(diagnostic.RuleName, Is.EqualTo("root"));
            Assert.That(diagnostic.Span.SourceName,
                Is.EqualTo("semantic-error.cddl"));
        });
    }

    [Test]
    public void PreservesExactSourceMappingForEveryNormalizedNode()
    {
        string source =
            "root = [\n"
            + "  item,\n"
            + "  2\n"
            + "]\n"
            + "item = 1";
        CddlCompileResult result = Compile(
            source,
            NoPrelude(),
            "mapping.cddl");
        CddlSchema schema = result.Schema!;
        CddlNode two = schema.NormalizedNodes.Single(node =>
            node.HasIntegerValue && node.IntegerValue == new BigInteger(2));
        CddlNode one = schema.NormalizedNodes.Single(node =>
            node.HasIntegerValue && node.IntegerValue == new BigInteger(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(schema.SourceMap, Has.Length.EqualTo(
                schema.NormalizedNodes.Length));
            Assert.That(schema.SourceMap.Select(span => span.SourceName),
                Is.All.EqualTo("mapping.cddl"));
            Assert.That(schema.SourceMap.Select(span => span.Start),
                Is.EqualTo(schema.NormalizedNodes.Select(node =>
                    node.Span.Start)));
            Assert.That(schema.SourceMap.Select(span => span.Length),
                Is.EqualTo(schema.NormalizedNodes.Select(node =>
                    node.Span.Length)));
            Assert.That(schema.SourceMap.Zip(schema.NormalizedNodes,
                    (span, node) => ReferenceEquals(span, node.Span)),
                Is.All.False);
            Assert.That(two.Span.Start,
                Is.EqualTo(source.IndexOf("2", StringComparison.Ordinal)));
            Assert.That(one.Span.Start,
                Is.EqualTo(source.LastIndexOf("1", StringComparison.Ordinal)));
        });
    }

    private static CddlRule FindRule(CddlSchema schema, string name)
    {
        return schema.Rules.Single(rule => rule.Name == name);
    }

    private static CddlNode EntryValue(CddlNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static CddlCompileResult Compile(
        string source,
        CddlCompileOptions options,
        string sourceName)
    {
        return new CddlCompiler().TryCompileSchema(
            ParseDone(source, sourceName),
            options);
    }

    private static CddlDocument ParseDone(string source, string sourceName)
    {
        CddlParseResult result = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = sourceName });
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }
}
