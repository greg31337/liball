using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlPreludeLoaderTests
{
    [Test]
    public void ParsesAndLoadsEveryStandardPreludeRuleInNormativeOrder()
    {
        CddlPreludeLoader loader = new CddlPreludeLoader();
        CddlRule[] rules = loader.LoadRules(
            new CddlDocument(),
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(CddlPrelude.RuleNames.Length));
            Assert.That(rules.Select(rule => rule.Name),
                Is.EqualTo(CddlPrelude.RuleNames));
            Assert.That(rules.Select(rule => rule.SourceOrder),
                Is.EqualTo(Enumerable.Range(0, CddlPrelude.RuleNames.Length)));
            Assert.That(rules.Select(rule => rule.Kind),
                Is.All.EqualTo(CddlRuleKind.Type));
            Assert.That(rules.Select(rule => rule.IsPrelude), Is.All.True);
            Assert.That(rules.Select(rule => rule.Value), Is.All.Not.Null);
            Assert.That(rules.Select(rule => rule.Span.SourceName),
                Is.All.EqualTo(CddlPreludeLoader.PreludeSourceName));
        });
    }

    [Test]
    public void AppendsBuiltInsWithoutChangingSourceOrderOrRootSelection()
    {
        CddlDocument source = ParseDone(
            "root = custom\ncustom = tstr",
            "source.cddl");
        CddlRule root = source.Rules[0];
        CddlRule custom = source.Rules[1];
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(
            source,
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(42));
            Assert.That(rules[0], Is.SameAs(root));
            Assert.That(rules[1], Is.SameAs(custom));
            Assert.That(rules[0].IsPrelude, Is.False);
            Assert.That(rules[1].IsPrelude, Is.False);
            Assert.That(rules[2].Name, Is.EqualTo("any"));
            Assert.That(rules[2].IsPrelude, Is.True);
            Assert.That(source.Rules, Has.Count.EqualTo(2));
            Assert.That(source.RootRuleName, Is.EqualTo("root"));
        });
    }

    [Test]
    public void SourceBaseDefinitionsReplaceOnlyTheirMatchingBuiltIns()
    {
        CddlDocument source = ParseDone(
            "uint = 0..10\nroot = uint",
            "override.cddl");
        CddlRule sourceUint = source.Rules[0];
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(
            source,
            new CddlCompileOptions());
        CddlRule[] uintRules = rules
            .Where(rule => rule.Name == "uint")
            .ToArray();
        CddlRule preludeInt = rules.Single(rule =>
            rule.Name == "int" && rule.IsPrelude);

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(41));
            Assert.That(uintRules, Has.Length.EqualTo(1));
            Assert.That(uintRules[0], Is.SameAs(sourceUint));
            Assert.That(uintRules[0].IsPrelude, Is.False);
            Assert.That(preludeInt.Value!.Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(preludeInt.Value.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(preludeInt.Span.SourceName,
                Is.EqualTo(CddlPreludeLoader.PreludeSourceName));
        });
    }

    [Test]
    public void SourceGroupBasesAlsoPreventAConflictingBuiltInBase()
    {
        CddlDocument source = ParseDone(
            "uint = (value: int,)",
            "group-override.cddl");
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(
            source,
            new CddlCompileOptions());
        CddlRule[] uintRules = rules
            .Where(rule => rule.Name == "uint")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(source.Rules[0].Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(uintRules, Has.Length.EqualTo(1));
            Assert.That(uintRules[0], Is.SameAs(source.Rules[0]));
            Assert.That(rules, Has.Length.EqualTo(40));
        });
    }

    [Test]
    public void ExtensionOnlyDefinitionsRetainTheStandardBaseForLaterMerging()
    {
        CddlDocument source = ParseDone(
            "uint /= 42\nint //= extra",
            "extensions.cddl");
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(
            source,
            new CddlCompileOptions());
        CddlRule[] uintRules = rules
            .Where(rule => rule.Name == "uint")
            .ToArray();
        CddlRule[] intRules = rules
            .Where(rule => rule.Name == "int")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(42));
            Assert.That(uintRules, Has.Length.EqualTo(2));
            Assert.That(uintRules[0].Kind,
                Is.EqualTo(CddlRuleKind.TypeExtension));
            Assert.That(uintRules[0].IsPrelude, Is.False);
            Assert.That(uintRules[1].Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(uintRules[1].IsPrelude, Is.True);
            Assert.That(intRules, Has.Length.EqualTo(2));
            Assert.That(intRules[0].Kind,
                Is.EqualTo(CddlRuleKind.GroupExtension));
            Assert.That(intRules[1].IsPrelude, Is.True);
        });
    }

    [Test]
    public void DisablingThePreludeReturnsOnlyTheOriginalSourceRules()
    {
        CddlDocument source = ParseDone("root = uint", "plain.cddl");
        CddlCompileOptions options = new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(1));
            Assert.That(rules[0], Is.SameAs(source.Rules[0]));
            Assert.That(rules[0].IsPrelude, Is.False);
        });
    }

    [Test]
    public void SeparateLoadsHaveIndependentPreludeSyntaxTrees()
    {
        CddlPreludeLoader loader = new CddlPreludeLoader();
        CddlCompileOptions options = new CddlCompileOptions();
        CddlRule[] first = loader.LoadRules(new CddlDocument(), options);
        CddlRule[] second = loader.LoadRules(new CddlDocument(), options);

        first[0].Name = "changed";
        first[0].Value!.Name = "changed-value";

        Assert.Multiple(() =>
        {
            Assert.That(second[0], Is.Not.SameAs(first[0]));
            Assert.That(second[0].Value, Is.Not.SameAs(first[0].Value));
            Assert.That(second[0].Name, Is.EqualTo("any"));
            Assert.That(second[0].Value!.Name, Is.Null);
        });
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
