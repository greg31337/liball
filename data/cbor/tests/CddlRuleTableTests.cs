using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlRuleTableTests
{
    [Test]
    public void PreservesFirstNameAndDefinitionOrderWhileMergingTypes()
    {
        CddlDocument document = ParseDone(
            "alpha /= 2\n"
            + "beta = bstr\n"
            + "alpha = 1\n"
            + "alpha /= 3\n"
            + "gamma = (item,)");
        CddlRuleTable table = BuildWithoutPrelude(document);
        CddlRuleTableEntry alpha = table.Entries[0];
        CddlNode merged = alpha.MergedRule!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(table.Diagnostics, Is.Empty);
            Assert.That(table.Entries.Select(entry => entry.Name),
                Is.EqualTo(new string[] { "alpha", "beta", "gamma" }));
            Assert.That(alpha.Definitions.Select(rule => rule.SourceOrder),
                Is.EqualTo(new int[] { 0, 2, 3 }));
            Assert.That(alpha.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(alpha.BaseRule, Is.SameAs(document.Rules[2]));
            Assert.That(alpha.MergedRule.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(merged.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(merged.Children.Select(node => node.IntegerValue),
                Is.EqualTo(new BigInteger[] { 2, 1, 3 }));
        });
    }

    [Test]
    public void FlattensExistingTypeChoicesInExtensionOrder()
    {
        CddlDocument document = ParseDone(
            "choice = 1 / 2\nchoice /= 3 / 4");
        CddlRuleTableEntry choice = BuildWithoutPrelude(document).Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(choice.MergedRule!.Value!.Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(choice.MergedRule.Value.Children
                    .Select(node => node.IntegerValue),
                Is.EqualTo(new BigInteger[] { 1, 2, 3, 4 }));
            Assert.That(document.Rules[0].Value!.Children, Has.Count.EqualTo(2));
            Assert.That(document.Rules[1].Value!.Children, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void MergesGroupAlternativesWithoutReorderingDefinitions()
    {
        CddlDocument document = ParseDone(
            "fields //= first\n"
            + "fields = (second,)\n"
            + "fields //= third");
        CddlRuleTable table = BuildWithoutPrelude(document);
        CddlRuleTableEntry fields = table.Entries[0];
        CddlNode choice = fields.MergedRule!.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(table.Diagnostics, Is.Empty);
            Assert.That(fields.Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(choice.Children, Has.Count.EqualTo(3));
            Assert.That(choice.Children[0].Children[0],
                Is.SameAs(document.Rules[0].Value));
            Assert.That(choice.Children[1].Children[0],
                Is.SameAs(document.Rules[1].Value));
            Assert.That(choice.Children[2].Children[0],
                Is.SameAs(document.Rules[2].Value));
        });
    }

    [Test]
    public void ReportsDuplicateBaseDefinitionsAtTheLaterDefinition()
    {
        CddlDocument document = ParseDone("root = a\nroot = b");
        CddlRuleTable table = BuildWithoutPrelude(document);
        CddlDiagnostic diagnostic = table.Diagnostics[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.HasErrors, Is.True);
            Assert.That(table.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostic.Code,
                Is.EqualTo("CDDL_COMPILE_DUPLICATE_BASE_RULE"));
            Assert.That(diagnostic.RuleName, Is.EqualTo("root"));
            Assert.That(diagnostic.Span.Start,
                Is.EqualTo(document.Rules[1].Span.Start));
            Assert.That(table.Entries[0].MergedRule, Is.Null);
        });
    }

    [Test]
    public void ReportsIncompatibleTypeAndGroupExtensions()
    {
        CddlDocument document = ParseDone(
            "type = value\ntype //= member\n"
            + "group = (member,)\ngroup /= value\n"
            + "mixed /= one\nmixed //= two");
        CddlRuleTable table = BuildWithoutPrelude(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_RULE_KIND_MISMATCH",
                    "CDDL_COMPILE_RULE_KIND_MISMATCH",
                    "CDDL_COMPILE_RULE_KIND_MISMATCH"
                }));
            Assert.That(table.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "type", "group", "mixed" }));
            Assert.That(table.Entries.Select(entry => entry.MergedRule),
                Is.All.Null);
        });
    }

    [Test]
    public void TreatsSocketPlugsAsCompatibleTypeAndGroupFamilies()
    {
        CddlDocument document = ParseDone(
            "$message /= [0]\n$message /= [1]\n"
            + "$$option //= (name: tstr,)\n"
            + "$$option //= (count: uint,)");
        CddlRuleTable table = BuildWithoutPrelude(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.Diagnostics, Is.Empty);
            Assert.That(table.Entries.Select(entry => entry.Name),
                Is.EqualTo(new string[] { "message", "option" }));
            Assert.That(table.Entries[0].Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(table.Entries[0].MergedRule!.Value!.Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(table.Entries[1].Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(table.Entries[1].MergedRule!.Value!.Kind,
                Is.EqualTo(CddlNodeKind.GroupChoice));
        });
    }

    [Test]
    public void KeepsSourceExtensionsBeforeAnAppendedPreludeBase()
    {
        CddlDocument document = ParseDone("uint /= 42\nint //= extra");
        CddlCompileOptions options = new CddlCompileOptions();
        CddlRule[] loaded = new CddlPreludeLoader().LoadRules(document, options);
        CddlRuleTable table = new CddlRuleTable(loaded, options);
        CddlRuleTableEntry uintEntry = table.FindEntry("uint")!;
        CddlRuleTableEntry intEntry = table.FindEntry("int")!;

        Assert.Multiple(() =>
        {
            Assert.That(uintEntry.MergedRule!.Value!.Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(uintEntry.MergedRule.Value.Children[0].IntegerValue,
                Is.EqualTo(new BigInteger(42)));
            Assert.That(uintEntry.MergedRule.Value.Children[1].Kind,
                Is.EqualTo(CddlNodeKind.Representation));
            Assert.That(uintEntry.MergedRule.IsPrelude, Is.False);
            Assert.That(intEntry.MergedRule, Is.Null);
            Assert.That(table.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(table.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_RULE_KIND_MISMATCH"));
            Assert.That(table.Diagnostics[0].Span.SourceName,
                Is.EqualTo("rules.cddl"));
            Assert.That(table.Diagnostics[0].Span.Start,
                Is.EqualTo(document.Rules[1].Span.Start));
        });
    }

    [Test]
    public void RejectsInvalidKindsAndMissingValuesWithBoundedDiagnostics()
    {
        CddlRule invalidKind = new CddlRule
        {
            Name = "invalid",
            Span = new CddlSourceSpan { SourceName = "manual.cddl", Start = 4 }
        };
        CddlRule missingValue = new CddlRule
        {
            Name = "missing",
            Kind = CddlRuleKind.Type,
            Span = new CddlSourceSpan { SourceName = "manual.cddl", Start = 8 }
        };
        CddlRuleTable table = new CddlRuleTable(
            new CddlRule[] { invalidKind, missingValue },
            new CddlCompileOptions { MaxDiagnostics = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(table.HasErrors, Is.True);
            Assert.That(table.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(table.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_RULE_KIND_INVALID"));
            Assert.That(table.Entries[0].MergedRule, Is.Null);
            Assert.That(table.Entries[1].MergedRule, Is.Null);
        });
    }

    private static CddlRuleTable BuildWithoutPrelude(CddlDocument document)
    {
        CddlCompileOptions options = new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
        CddlRule[] rules = new CddlPreludeLoader().LoadRules(document, options);
        return new CddlRuleTable(rules, options);
    }

    private static CddlDocument ParseDone(string source)
    {
        CddlParseResult result = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "rules.cddl" });
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }
}
