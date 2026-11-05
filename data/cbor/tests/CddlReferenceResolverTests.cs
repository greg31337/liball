using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlReferenceResolverTests
{
    private sealed class TestRuleResolver : ICddlRuleResolver
    {
        public List<CddlRule> Rules = new List<CddlRule>();
        public int CallCount;

        public CddlRule? ResolveRule(string name, CddlRuleKind kind)
        {
            CallCount = CallCount + 1;
            return Rules.FirstOrDefault(rule =>
                rule.Name == name && rule.Kind == kind);
        }
    }

    [Test]
    public void ResolvesLocalTypeAndGroupReferencesAndSelectsTheDefaultRoot()
    {
        CddlDocument document = ParseDone(
            "root = [scalar, fields]\n"
            + "scalar = 1\n"
            + "fields = (item: 2,)",
            "local.cddl");
        CddlResolutionResult result = Resolve(document, NoPrelude());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.RootRule!.Name, Is.EqualTo("root"));
            Assert.That(result.References.Select(item => item.Node.Name),
                Is.EqualTo(new string[] { "scalar", "fields" }));
            Assert.That(result.References.Select(item => item.Kind),
                Is.EqualTo(new CddlRuleKind[]
                {
                    CddlRuleKind.Type,
                    CddlRuleKind.Group
                }));
            Assert.That(result.References.Select(item => item.Rule!.Name),
                Is.EqualTo(new string[] { "scalar", "fields" }));
        });
    }

    [Test]
    public void RecordsGenericParametersWithoutGlobalNameLookup()
    {
        CddlDocument document = ParseDone(
            "root = pair<1>\n"
            + "pair<t> = [t, value: t]",
            "generic-references.cddl");
        CddlResolutionResult result = Resolve(document, NoPrelude());
        CddlResolvedReference[] parameters = result.References
            .Where(item => item.IsGenericParameter)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.References.Select(item => item.Node.Name),
                Is.EqualTo(new string[] { "pair", "t", "t" }));
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters.Select(item => item.Node.Name),
                Is.All.EqualTo("t"));
            Assert.That(parameters.Select(item => item.Kind),
                Is.All.EqualTo(CddlRuleKind.Type));
            Assert.That(parameters.Select(item => item.Rule), Is.All.Null);
        });
    }

    [Test]
    public void ReportsUndefinedAndWrongKindReferencesAtTheirUseSites()
    {
        string source =
            "root = [missing, key: unknown]\n"
            + "wrong = group\n"
            + "group = (item: 1,)";
        CddlDocument document = ParseDone(source, "errors.cddl");
        CddlResolutionResult result = Resolve(document, NoPrelude());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_REFERENCE_UNDEFINED",
                    "CDDL_COMPILE_REFERENCE_UNDEFINED",
                    "CDDL_COMPILE_REFERENCE_KIND_MISMATCH"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "root", "root", "wrong" }));
            Assert.That(result.Diagnostics[0].Span.Start,
                Is.EqualTo(source.IndexOf("missing", StringComparison.Ordinal)));
            Assert.That(result.Diagnostics[1].Span.Start,
                Is.EqualTo(source.IndexOf("unknown", StringComparison.Ordinal)));
            Assert.That(result.Diagnostics[2].Span.Start,
                Is.EqualTo(source.IndexOf("group", StringComparison.Ordinal)));
            Assert.That(result.Diagnostics.Select(item => item.Span.SourceName),
                Is.All.EqualTo("errors.cddl"));
        });
    }

    [Test]
    public void KeepsOrdinaryTypeAndGroupSocketNamespacesDistinct()
    {
        CddlDocument document = ParseDone(
            "root = [$choice, * $$choice, choice]\n"
            + "choice = 9\n"
            + "$choice /= 1\n"
            + "$$choice //= (two: 2,)",
            "sockets.cddl");
        CddlCompileOptions options = NoPrelude();
        CddlRuleTable table = BuildTable(document, options);
        CddlResolutionResult result =
            new CddlReferenceResolver(table, document, options).ResolveDocument();

        Assert.Multiple(() =>
        {
            Assert.That(table.Entries.Select(entry => entry.Name),
                Is.EqualTo(new string[]
                {
                    "root", "choice", "choice", "choice"
                }));
            Assert.That(table.Entries.Select(entry => entry.SocketKind),
                Is.EqualTo(new CddlRuleKind[]
                {
                    CddlRuleKind.None,
                    CddlRuleKind.None,
                    CddlRuleKind.TypeSocket,
                    CddlRuleKind.GroupSocket
                }));
            Assert.That(table.Diagnostics, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.References.Select(item => item.Node.Name),
                Is.EqualTo(new string[] { "$choice", "$$choice", "choice" }));
            Assert.That(result.References.Select(item => item.Kind),
                Is.EqualTo(new CddlRuleKind[]
                {
                    CddlRuleKind.Type,
                    CddlRuleKind.Group,
                    CddlRuleKind.Type
                }));
            Assert.That(result.References.Select(item => item.IsEmptySocket),
                Is.All.False);
            Assert.That(result.References.Select(item => item.Rule),
                Is.All.Not.Null);
        });
    }

    [Test]
    public void UndefinedSocketsResolveAsDistinctEmptyChoices()
    {
        CddlDocument document = ParseDone(
            "root = [$optional, * $$optional]",
            "empty-sockets.cddl");
        CddlResolutionResult result = Resolve(document, NoPrelude());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.References, Has.Count.EqualTo(2));
            Assert.That(result.References.Select(item => item.Kind),
                Is.EqualTo(new CddlRuleKind[]
                {
                    CddlRuleKind.Type,
                    CddlRuleKind.Group
                }));
            Assert.That(result.References.Select(item => item.IsEmptySocket),
                Is.All.True);
            Assert.That(result.References.Select(item => item.Rule), Is.All.Null);
        });
    }

    [Test]
    public void ResolvesAndCachesTransitiveExternalTypeAndGroupRules()
    {
        TestRuleResolver external = new TestRuleResolver();
        external.Rules.Add(ParseDone(
            "remote = dependent",
            "external-type.cddl").Rules[0]);
        external.Rules.Add(ParseDone(
            "remote-group = (field: 1,)",
            "external-group.cddl").Rules[0]);
        external.Rules.Add(ParseDone(
            "dependent = 7",
            "external-dependent.cddl").Rules[0]);
        CddlDocument document = ParseDone(
            "root = [remote, remote-group]",
            "external-use.cddl");
        CddlCompileOptions options = NoPrelude();
        options.RuleResolver = external;
        CddlResolutionResult result = Resolve(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.ExternalRules.Select(rule => rule.Name),
                Is.EqualTo(new string[]
                {
                    "remote", "remote-group", "dependent"
                }));
            Assert.That(result.References.Select(item => item.Node.Name),
                Is.EqualTo(new string[]
                {
                    "remote", "remote-group", "dependent"
                }));
            Assert.That(result.References.Select(item => item.Kind),
                Is.EqualTo(new CddlRuleKind[]
                {
                    CddlRuleKind.Type,
                    CddlRuleKind.Group,
                    CddlRuleKind.Type
                }));
            Assert.That(external.CallCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void ReportsAmbiguousExternalReferencesInGroupEntryContexts()
    {
        TestRuleResolver external = new TestRuleResolver();
        external.Rules.Add(ParseDone(
            "both = 1",
            "external-type.cddl").Rules[0]);
        external.Rules.Add(ParseDone(
            "both = (field: 1,)",
            "external-group.cddl").Rules[0]);
        CddlDocument document = ParseDone(
            "root = [both]",
            "ambiguous.cddl");
        CddlCompileOptions options = NoPrelude();
        options.RuleResolver = external;
        CddlResolutionResult result = Resolve(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_REFERENCE_AMBIGUOUS"));
            Assert.That(result.Diagnostics[0].RuleName, Is.EqualTo("root"));
            Assert.That(result.Diagnostics[0].Span.Start,
                Is.EqualTo("root = [".Length));
            Assert.That(result.References, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidExternalResultsAtTheReferenceSpan()
    {
        TestRuleResolver external = new TestRuleResolver();
        CddlRule wrongName = ParseDone(
            "different = 1",
            "invalid-external.cddl").Rules[0];
        external.Rules.Add(wrongName);
        CddlDocument document = ParseDone(
            "root = {field: bad}",
            "invalid-use.cddl");
        CddlCompileOptions options = NoPrelude();
        options.RuleResolver = new WrongNameResolver(wrongName);
        CddlResolutionResult result = Resolve(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_EXTERNAL_RULE_INVALID"));
            Assert.That(result.Diagnostics[0].RuleName, Is.EqualTo("root"));
            Assert.That(result.Diagnostics[0].Span.SourceName,
                Is.EqualTo("invalid-use.cddl"));
            Assert.That(result.Diagnostics[0].Span.Start,
                Is.EqualTo("root = {field: ".Length));
            Assert.That(result.References, Is.Empty);
        });
    }

    private sealed class WrongNameResolver : ICddlRuleResolver
    {
        public CddlRule Rule;

        public WrongNameResolver(CddlRule rule)
        {
            Rule = rule;
        }

        public CddlRule? ResolveRule(string name, CddlRuleKind kind)
        {
            if (kind == CddlRuleKind.Type)
            {
                return Rule;
            }
            return null;
        }
    }

    [Test]
    public void AppliesExplicitDefaultEmptyExternalAndInvalidRootSelection()
    {
        CddlDocument document = ParseDone(
            "first = 1\nsecond = 2\ngroup = (item: 3,)",
            "roots.cddl");
        CddlCompileOptions explicitOptions = NoPrelude();
        explicitOptions.RootRuleName = "second";
        CddlResolutionResult explicitRoot = Resolve(document, explicitOptions);
        CddlResolutionResult defaultRoot = Resolve(document, NoPrelude());
        CddlResolutionResult empty = Resolve(
            ParseDone("", "empty.cddl"),
            NoPrelude());
        CddlCompileOptions groupOptions = NoPrelude();
        groupOptions.RootRuleName = "group";
        CddlResolutionResult groupRoot = Resolve(document, groupOptions);
        CddlCompileOptions missingOptions = NoPrelude();
        missingOptions.RootRuleName = "missing";
        CddlResolutionResult missingRoot = Resolve(document, missingOptions);
        TestRuleResolver external = new TestRuleResolver();
        external.Rules.Add(ParseDone(
            "remote = 9",
            "external-root.cddl").Rules[0]);
        CddlCompileOptions externalOptions = NoPrelude();
        externalOptions.RootRuleName = "remote";
        externalOptions.RuleResolver = external;
        CddlResolutionResult externalRoot = Resolve(
            ParseDone("", "external-root-use.cddl"),
            externalOptions);

        Assert.Multiple(() =>
        {
            Assert.That(defaultRoot.RootRule!.Name, Is.EqualTo("first"));
            Assert.That(explicitRoot.RootRule!.Name, Is.EqualTo("second"));
            Assert.That(empty.RootRule, Is.Null);
            Assert.That(empty.Diagnostics, Is.Empty);
            Assert.That(groupRoot.RootRule, Is.Null);
            Assert.That(groupRoot.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_ROOT_KIND_INVALID"));
            Assert.That(groupRoot.Diagnostics[0].Span.SourceName,
                Is.EqualTo("roots.cddl"));
            Assert.That(missingRoot.RootRule, Is.Null);
            Assert.That(missingRoot.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_ROOT_UNDEFINED"));
            Assert.That(missingRoot.Diagnostics[0].Span.SourceName,
                Is.EqualTo("roots.cddl"));
            Assert.That(externalRoot.RootRule!.Name, Is.EqualTo("remote"));
            Assert.That(externalRoot.ExternalRules, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void BoundsReferenceDiagnosticsWithoutHidingFailureState()
    {
        CddlDocument document = ParseDone(
            "root = [one, two, three]",
            "bounded.cddl");
        CddlCompileOptions options = NoPrelude();
        options.MaxDiagnostics = 2;
        CddlResolutionResult result = Resolve(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.All.EqualTo("CDDL_COMPILE_REFERENCE_UNDEFINED"));
        });
    }

    private static CddlResolutionResult Resolve(
        CddlDocument document,
        CddlCompileOptions options)
    {
        CddlRuleTable table = BuildTable(document, options);
        return new CddlReferenceResolver(
            table,
            document,
            options
        ).ResolveDocument();
    }

    private static CddlRuleTable BuildTable(
        CddlDocument document,
        CddlCompileOptions options)
    {
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(document, options);
        return new CddlRuleTable(rules, options);
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions { IncludeStandardPrelude = false };
    }

    private static CddlDocument ParseDone(string source, string sourceName)
    {
        CddlParseResult result = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = sourceName });
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }
}
