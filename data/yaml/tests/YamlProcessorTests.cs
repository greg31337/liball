using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlProcessorTests
{
    [Test]
    public void Processor_ImplementsInjectableContract()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();

        object? value = processor.Load("count: 3\n");

        Dictionary<string, object> mapping = (Dictionary<string, object>)value!;
        Assert.That(mapping["count"], Is.EqualTo(3L));
    }

    [Test]
    public void Constructor_CopiesOptionsFromCaller()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Emitter.SortStringKeys = true;
        global::yaml.YamlProcessor processor = global::yaml.YamlProcessor.Create(options);
        options.Emitter.SortStringKeys = false;
        Dictionary<string, object> value = new Dictionary<string, object>();
        value["zeta"] = "last";
        value["alpha"] = "first";

        string text = processor.Dump(value);

        Assert.That(text, Is.EqualTo("alpha: first\nzeta: last\n"));
    }

    [Test]
    public void ProcessorOptions_CanConfigureRepeatedOperations()
    {
        global::yaml.YamlProcessor processor = new global::yaml.YamlProcessor();
        processor.Options.Emitter.WriteDocumentStart = true;

        string first = processor.Serialize("one");
        string second = processor.Serialize("two");

        Assert.That(first, Is.EqualTo("---\none\n"));
        Assert.That(second, Is.EqualTo("---\ntwo\n"));
    }

    [Test]
    public void StaticFacade_AndProcessor_ProduceEquivalentResults()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.Schema = global::yaml.YamlSchemaKind.Json;
        global::yaml.YamlProcessor processor = global::yaml.YamlProcessor.Create(options);

        global::yaml.YamlDocument expected = global::yaml.Yaml.Parse("value: true\n", options);
        global::yaml.YamlDocument actual = processor.Parse("value: true\n");

        Assert.That(actual.Root!.FindMappingValue("value")!.Tag, Is.EqualTo(expected.Root!.FindMappingValue("value")!.Tag));
    }
}
