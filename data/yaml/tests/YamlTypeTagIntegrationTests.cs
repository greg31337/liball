using System.Collections.Generic;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTypeTagIntegrationTests
{
    [Test]
    public void UseTypeTags_SerializesAndInfersRegisteredRootTypes()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(PersonOptions(true));
        TaggedPerson source = new TaggedPerson();
        source.Age = 37;
        source.Name = "Ada";

        string text = processor.Serialize(source);
        object? loaded = processor.Load(text);
        object? deserialized = processor.Deserialize(text, typeof(object));

        Assert.That(text, Is.EqualTo("!person\nAge: 37\nName: Ada\n"));
        Assert.That(loaded, Is.TypeOf<TaggedPerson>());
        Assert.That(((TaggedPerson)loaded!).Name, Is.EqualTo("Ada"));
        Assert.That(deserialized, Is.TypeOf<TaggedPerson>());
        Assert.That(((TaggedPerson)deserialized!).Age, Is.EqualTo(37));
    }

    [Test]
    public void UseTypeTags_AppliesMappingsInsideNestedObjectGraphs()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(PersonOptions(true));
        TaggedEnvelope source = new TaggedEnvelope();
        TaggedPerson person = new TaggedPerson();
        person.Age = 42;
        person.Name = "Grace";
        source.Payload = person;

        string text = processor.Serialize(source);
        TaggedEnvelope converted = (TaggedEnvelope)processor.Deserialize(text, typeof(TaggedEnvelope))!;

        Assert.That(text, Is.EqualTo("Payload: !person\n  Age: 42\n  Name: Grace\n"));
        Assert.That(converted.Payload, Is.TypeOf<TaggedPerson>());
        Assert.That(((TaggedPerson)converted.Payload).Name, Is.EqualTo("Grace"));
    }

    [Test]
    public void UseTypeTagsFalse_DoesNotEmitOrInferRegisteredTypes()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(PersonOptions(false));
        TaggedPerson source = new TaggedPerson();
        source.Age = 5;
        source.Name = "Lin";

        string text = processor.Dump(source);
        object? loaded = processor.Load("!person { Age: 5, Name: Lin }\n");
        TaggedPerson explicitTarget = (TaggedPerson)processor.Deserialize(
            "!person { Age: 5, Name: Lin }\n",
            typeof(TaggedPerson))!;

        Assert.That(text, Is.EqualTo("Age: 5\nName: Lin\n"));
        Assert.That(loaded, Is.TypeOf<Dictionary<string, object?>>());
        Assert.That(explicitTarget.Name, Is.EqualTo("Lin"));
    }

    [Test]
    public void TypeTags_ComposeWithCustomTypeConvertersInBothDirections()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.UseTypeTags = true;
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlTagRegistration tag = new global::yaml.YamlTagRegistration();
        tag.Tag = "!point";
        tag.NodeKind = global::yaml.YamlNodeKind.Scalar;
        tag.ClrType = typeof(TaggedPoint);
        options.Schema.CustomTags.Registrations.Add(tag);
        global::yaml.YamlTypeConverterRegistration converter = new global::yaml.YamlTypeConverterRegistration();
        converter.ClrType = typeof(TaggedPoint);
        converter.WriteValue = WritePoint;
        converter.ReadValue = ReadPoint;
        options.Object.TypeConverters.Registrations.Add(converter);
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        string text = processor.Serialize(new TaggedPoint(2, 3));
        object? loaded = processor.Load(text);

        Assert.That(text, Is.EqualTo("!point '2,3'\n"));
        Assert.That(loaded, Is.TypeOf<TaggedPoint>());
        Assert.That(((TaggedPoint)loaded!).X, Is.EqualTo(2));
        Assert.That(((TaggedPoint)loaded).Y, Is.EqualTo(3));
    }

    [Test]
    public void TypeTagMappings_ObeyUnknownTagPolicy()
    {
        global::yaml.YamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(PersonOptions(true));

        Assert.That(
            () => processor.Load("!missing { Age: 1, Name: Unknown }\n"),
            Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(
            processor.Load("!person { Age: 1, Name: Known }\n"),
            Is.TypeOf<TaggedPerson>());
    }

    public static global::yaml.YamlOptions PersonOptions(bool useTypeTags)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.UseTypeTags = useTypeTags;
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlTagRegistration registration = new global::yaml.YamlTagRegistration();
        registration.Tag = "!person";
        registration.NodeKind = global::yaml.YamlNodeKind.Mapping;
        registration.ClrType = typeof(TaggedPerson);
        options.Schema.CustomTags.Registrations.Add(registration);
        return options;
    }

    public static object WritePoint(object value)
    {
        TaggedPoint point = (TaggedPoint)value;
        return point.X.ToString() + "," + point.Y.ToString();
    }

    public static object ReadPoint(object value)
    {
        string[] parts = value.ToString()!.Split(',');
        return new TaggedPoint(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}

public class TaggedPerson
{
    public int Age;
    public string Name;

    public TaggedPerson()
    {
        Age = 0;
        Name = "";
    }
}

public class TaggedEnvelope
{
    public object Payload;

    public TaggedEnvelope()
    {
        Payload = new object();
    }
}

public class TaggedPoint
{
    public int X;
    public int Y;

    public TaggedPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}
