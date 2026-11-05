using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlScalarConverterIntegrationTests
{
    [Test]
    public void Load_UsesRegisteredTagReaderAndTreatsConverterTagAsKnownSchemaTag()
    {
        global::yaml.YamlOptions options = OptionsWithDelegates();
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        global::yaml.YamlDocument parsed = processor.Parse("!token alpha\n");
        ConverterValue loaded = (ConverterValue)processor.Load("!token alpha\n")!;
        ConverterValue loadedBytes = (ConverterValue)processor.LoadBytes(Encoding.UTF8.GetBytes("!token bravo\n"))!;

        Assert.That(parsed.Root!.Tag, Is.EqualTo("!token"));
        Assert.That(loaded.Text, Is.EqualTo("read:alpha"));
        Assert.That(loadedBytes.Text, Is.EqualTo("read:bravo"));
    }

    [Test]
    public void DumpAndSerialize_UseRegisteredWriterTagAndPreferredStyle()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(OptionsWithDelegates());
        ConverterValue value = new ConverterValue("MiXeD");

        string dumped = processor.Dump(value);
        string serialized = processor.Serialize(value);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        bool success = processor.TryDump(value, result);

        Assert.That(dumped, Is.EqualTo("!token \"write:mixed\"\n"));
        Assert.That(serialized, Is.EqualTo(dumped));
        Assert.That(success, Is.True);
        Assert.That(result.Value, Is.EqualTo(dumped));
    }

    [Test]
    public void SerializeAndDeserialize_ApplyScalarConvertersInsideObjectGraphs()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(OptionsWithDelegates());
        ConverterContainer source = new ConverterContainer();
        source.Item = new ConverterValue("Nested");

        string text = processor.Serialize(source);
        ConverterContainer converted = (ConverterContainer)processor.Deserialize(text, typeof(ConverterContainer))!;

        Assert.That(text, Is.EqualTo("Item: !token \"write:nested\"\n"));
        Assert.That(converted.Item.Text, Is.EqualTo("read:write:nested"));
    }

    [Test]
    public void Deserialize_UsesTargetTypeConverterForUntaggedTextAndBytes()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(OptionsWithDelegates());

        ConverterValue textValue = (ConverterValue)processor.Deserialize("plain\n", typeof(ConverterValue))!;
        ConverterValue byteValue = (ConverterValue)processor.DeserializeBytes(
            Encoding.UTF8.GetBytes("bytes\n"),
            typeof(ConverterValue))!;

        Assert.That(textValue.Text, Is.EqualTo("read:plain"));
        Assert.That(byteValue.Text, Is.EqualTo("read:bytes"));
    }

    [Test]
    public void InterfaceConverter_TakesPrecedenceEndToEnd()
    {
        global::yaml.YamlScalarConverterRegistration registration = new global::yaml.YamlScalarConverterRegistration();
        registration.Tag = "!interface";
        registration.ClrType = typeof(ConverterValue);
        registration.PreferredStyle = global::yaml.YamlScalarStyle.SingleQuoted;
        registration.ReadScalar = value => new ConverterValue("delegate-read:" + value);
        registration.WriteScalar = value => "delegate-write";
        registration.Converter = new ConverterValueInterfaceConverter();
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.ScalarConverters.Registrations.Add(registration);
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        string emitted = processor.Dump(new ConverterValue("value"));
        ConverterValue loaded = (ConverterValue)processor.Load("!interface value\n")!;

        Assert.That(emitted, Is.EqualTo("!interface 'interface-write:value'\n"));
        Assert.That(loaded.Text, Is.EqualTo("interface-read:value"));
    }

    public static global::yaml.YamlOptions OptionsWithDelegates()
    {
        global::yaml.YamlScalarConverterRegistration registration = new global::yaml.YamlScalarConverterRegistration();
        registration.Tag = "!token";
        registration.ClrType = typeof(ConverterValue);
        registration.PreferredStyle = global::yaml.YamlScalarStyle.DoubleQuoted;
        registration.ReadScalar = value => new ConverterValue("read:" + value);
        registration.WriteScalar = value => "write:" + ((ConverterValue)value).Text.ToLowerInvariant();
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.ScalarConverters.Registrations.Add(registration);
        return options;
    }
}

public class ConverterValue
{
    public string Text;

    public ConverterValue(string text)
    {
        Text = text;
    }
}

public class ConverterContainer
{
    public ConverterValue Item;

    public ConverterContainer()
    {
        Item = new ConverterValue("");
    }
}

public class ConverterValueInterfaceConverter : global::yaml.IYamlScalarConverter
{
    public ConverterValueInterfaceConverter()
    {
    }

    public object Read(string value)
    {
        return new ConverterValue("interface-read:" + value);
    }

    public string Write(object value)
    {
        return "interface-write:" + ((ConverterValue)value).Text;
    }
}
