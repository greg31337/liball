using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlPhase3ParserTests
{
    [Test]
    public void TryParse_BlockMappingWithRepeatedMissingKeys_PreservesBothPairs()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse(": a\n: b\n", result);

        Assert.That(success, Is.True, result.FormatDiagnostics());
        global::yaml.YamlNode root = ((global::yaml.YamlDocument)result.Value!).Root!;
        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(root.Pairs.Count, Is.EqualTo(2));
        Assert.That(root.Pairs[0].Key!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(root.Pairs[0].Value!.Value, Is.EqualTo("a"));
        Assert.That(root.Pairs[1].Key!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(root.Pairs[1].Value!.Value, Is.EqualTo("b"));
    }

    [Test]
    public void TryParse_NestedImplicitComplexFlowKeys_ComposesExpectedGraph()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("---\n[\n  [ a, [ [[b,c]]: d, e]]: 23\n]\n", result);

        Assert.That(success, Is.True, result.FormatDiagnostics());
        global::yaml.YamlNode root = ((global::yaml.YamlDocument)result.Value!).Root!;
        global::yaml.YamlNode outerPair = root.Children[0];
        global::yaml.YamlNode outerKey = outerPair.Pairs[0].Key!;
        global::yaml.YamlNode innerSequence = outerKey.Children[1];
        global::yaml.YamlNode innerPair = innerSequence.Children[0];
        global::yaml.YamlNode innerKey = innerPair.Pairs[0].Key!;

        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(outerPair.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(outerKey.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(outerKey.Children[0].Value, Is.EqualTo("a"));
        Assert.That(innerPair.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(innerKey.Children[0].Children.Select(node => node.Value), Is.EqualTo(new string[] { "b", "c" }));
        Assert.That(innerPair.Pairs[0].Value!.Value, Is.EqualTo("d"));
        Assert.That(innerSequence.Children[1].Value, Is.EqualTo("e"));
        Assert.That(outerPair.Pairs[0].Value!.Value, Is.EqualTo("23"));
    }

    [Test]
    public void TryParse_MultilineFlowKeysWithoutValues_FoldsAndPreservesEmptyValues()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("---\n- { \"single line\", a: b}\n- { \"multi\n  line\", a: b}\n", result);

        Assert.That(success, Is.True, result.FormatDiagnostics());
        global::yaml.YamlNode root = ((global::yaml.YamlDocument)result.Value!).Root!;
        Assert.That(root.Children[0].Pairs[0].Key!.Value, Is.EqualTo("single line"));
        Assert.That(root.Children[0].Pairs[0].Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(root.Children[1].Pairs[0].Key!.Value, Is.EqualTo("multi line"));
        Assert.That(root.Children[1].Pairs[0].Value!.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(root.Children[1].Pairs[1].Value!.Value, Is.EqualTo("b"));
    }

    [Test]
    public void TryParse_PrimaryTagDirective_ExpandsGlobalTagsOnCollections()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("%TAG ! tag:clarkevans.com,2002:\n--- !shape\n- !circle {radius: 7}\n", result);

        Assert.That(success, Is.True, result.FormatDiagnostics());
        global::yaml.YamlNode root = ((global::yaml.YamlDocument)result.Value!).Root!;
        Assert.That(root.Tag, Is.EqualTo("tag:clarkevans.com,2002:shape"));
        Assert.That(root.Children[0].Tag, Is.EqualTo("tag:clarkevans.com,2002:circle"));
    }

    [Test]
    public void Load_FlowQuotedLineBreaks_UsesYamlFoldingRules()
    {
        Assert.That(global::yaml.Yaml.Load("'one\n  two'\n"), Is.EqualTo("one two"));
        Assert.That(global::yaml.Yaml.Load("\"one\\\n  two\"\n"), Is.EqualTo("onetwo"));
    }

    [Test]
    public void Load_CoreHexadecimalAndOctalIntegers_ReturnsNumericValues()
    {
        Assert.That(global::yaml.Yaml.Load("0xFFEEBB\n"), Is.EqualTo(16772795L));
        Assert.That(global::yaml.Yaml.Load("0o17\n"), Is.EqualTo(15L));
    }
}
