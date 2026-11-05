using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlFacadeTests
{
    [Test]
    public void Parse_ReturnsSingleDocumentWithResolvedSchemaTags()
    {
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("enabled: true\ncount: 37\n");

        global::yaml.YamlNode root = document.Root!;

        Assert.That(root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(root.FindMappingValue("enabled")!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(root.FindMappingValue("count")!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
    }

    [Test]
    public void ParseAll_ReturnsStreamWithAllDocuments()
    {
        global::yaml.YamlStream stream = global::yaml.Yaml.ParseAll("---\none\n---\ntwo\n");

        Assert.That(stream.DocumentCount(), Is.EqualTo(2));
        Assert.That(stream.Documents[0].Root!.Value, Is.EqualTo("one"));
        Assert.That(stream.Documents[1].Root!.Value, Is.EqualTo("two"));
    }

    [Test]
    public void TryParse_ReportsDiagnosticsWithoutThrowing()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("\u0001\n", result);

        Assert.That(success, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Count, Is.GreaterThan(0));
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void Parse_ThrowsForInvalidYaml()
    {
        Assert.Throws<InvalidOperationException>(() => global::yaml.Yaml.Parse("\u0001\n"));
    }

    [Test]
    public void Load_ReturnsJsonCompatibleValues()
    {
        object? value = global::yaml.Yaml.Load("name: Ada\nage: 37\nadmin: true\nitems:\n  - one\n  - 2\n");

        Dictionary<string, object> mapping = (Dictionary<string, object>)value!;
        List<object> items = (List<object>)mapping["items"]!;

        Assert.That(mapping["name"], Is.EqualTo("Ada"));
        Assert.That(mapping["age"], Is.EqualTo(37L));
        Assert.That(mapping["admin"], Is.EqualTo(true));
        Assert.That(items[0], Is.EqualTo("one"));
        Assert.That(items[1], Is.EqualTo(2L));
    }

    [Test]
    public void LoadAll_ReturnsJsonCompatibleValuesForEveryDocument()
    {
        List<object?> values = global::yaml.Yaml.LoadAll("---\none\n---\n2\n");

        Assert.That(values.Count, Is.EqualTo(2));
        Assert.That(values[0], Is.EqualTo("one"));
        Assert.That(values[1], Is.EqualTo(2L));
    }

    [Test]
    public void TryLoad_ReportsLoadedValueThroughResult()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryLoad("count: 2\n", result);

        Dictionary<string, object> mapping = (Dictionary<string, object>)result.Value!;
        Assert.That(success, Is.True);
        Assert.That(mapping["count"], Is.EqualTo(2L));
    }

    [Test]
    public void Dump_EmitsJsonCompatibleValues()
    {
        Dictionary<string, object> value = new Dictionary<string, object>();
        value["name"] = "Ada";
        value["age"] = 37;
        value["admin"] = true;

        string text = global::yaml.Yaml.Dump(value);

        Assert.That(text, Is.EqualTo("name: Ada\nage: 37\nadmin: true\n"));
    }

    [Test]
    public void Dump_UsesEmitterOptions()
    {
        Dictionary<string, object> value = new Dictionary<string, object>();
        value["zeta"] = "last";
        value["alpha"] = "first";
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Emitter.SortStringKeys = true;

        string text = global::yaml.Yaml.Dump(value, options);

        Assert.That(text, Is.EqualTo("alpha: first\nzeta: last\n"));
    }

    [Test]
    public void TryDump_ReportsEmittedYamlThroughResult()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryDump("Ada", result);

        Assert.That(success, Is.True);
        Assert.That(result.Value, Is.EqualTo("Ada\n"));
    }

    [Test]
    public void Serialize_UsesDumpPipeline()
    {
        string text = global::yaml.Yaml.Serialize("Ada");

        Assert.That(text, Is.EqualTo("Ada\n"));
    }

    [Test]
    public void Deserialize_ConvertsLoadedScalarToRequestedType()
    {
        object? value = global::yaml.Yaml.Deserialize("37\n", typeof(int));

        Assert.That(value, Is.EqualTo(37));
    }
}
