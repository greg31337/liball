using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlJsonConversionTests
{
    [Test]
    public void FromJsonValue_ConvertsPrimitiveJsonValuesToTaggedScalars()
    {
        global::yaml.YamlNode nil = global::yaml.YamlJsonConverter.FromJsonValue(null);
        global::yaml.YamlNode truth = global::yaml.YamlJsonConverter.FromJsonValue(true);
        global::yaml.YamlNode integer = global::yaml.YamlJsonConverter.FromJsonValue(42);
        global::yaml.YamlNode floating = global::yaml.YamlJsonConverter.FromJsonValue(1.5d);
        global::yaml.YamlNode text = global::yaml.YamlJsonConverter.FromJsonValue("true");

        Assert.That(nil.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Scalar));
        Assert.That(nil.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(nil.Value, Is.EqualTo(""));
        Assert.That(truth.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(truth.Value, Is.EqualTo("true"));
        Assert.That(integer.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(integer.Value, Is.EqualTo("42"));
        Assert.That(floating.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(floating.Value, Is.EqualTo("1.5"));
        Assert.That(text.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(text.Value, Is.EqualTo("true"));
    }

    [Test]
    public void IntegerConversion_PreservesSignedAndUnsignedBoundaries()
    {
        global::yaml.YamlNode minimum = global::yaml.YamlNodeFactory.TaggedScalar("-9223372036854775808", global::yaml.YamlScalarResolver.IntTag);
        global::yaml.YamlNode maximum = global::yaml.YamlJsonConverter.FromJsonValue(ulong.MaxValue);
        global::yaml.YamlNode negativeOverflow = global::yaml.YamlNodeFactory.TaggedScalar("-9223372036854775809", global::yaml.YamlScalarResolver.IntTag);
        global::yaml.YamlNode positiveOverflow = global::yaml.YamlNodeFactory.TaggedScalar("18446744073709551616", global::yaml.YamlScalarResolver.IntTag);

        Assert.That(minimum.ToJsonValue(), Is.EqualTo(long.MinValue));
        Assert.That(maximum.Value, Is.EqualTo("18446744073709551615"));
        Assert.That(maximum.ToJsonValue(), Is.EqualTo(ulong.MaxValue));
        Assert.That(negativeOverflow.ToJsonValue(), Is.EqualTo("-9223372036854775809"));
        Assert.That(positiveOverflow.ToJsonValue(), Is.EqualTo("18446744073709551616"));
    }

    [Test]
    public void FromJsonValue_ConvertsListsAndDictionariesToCollections()
    {
        Dictionary<string, object?> input = new Dictionary<string, object?>();
        List<object?> items = new List<object?>();
        items.Add(true);
        items.Add(null);
        input["name"] = "Ada";
        input["count"] = 3;
        input["items"] = items;

        global::yaml.YamlNode node = global::yaml.YamlNodeFactory.FromJsonValue(input);
        global::yaml.YamlNode nameNode = node.FindPath("name")!;
        global::yaml.YamlNode countNode = node.FindPath("count")!;
        global::yaml.YamlNode itemNode = node.FindPath("items")!;

        Assert.That(node.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(node.Pairs.Count, Is.EqualTo(3));
        Assert.That(nameNode.Value, Is.EqualTo("Ada"));
        Assert.That(countNode.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(itemNode.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(itemNode.Children[0].Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(itemNode.Children[1].Tag, Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
    }

    [Test]
    public void FromJsonValue_ConvertsArraysAndNestedDictionaries()
    {
        Dictionary<string, object> nested = new Dictionary<string, object>();
        nested["enabled"] = false;
        object[] input = new object[] { "first", nested };

        global::yaml.YamlNode node = global::yaml.YamlJsonConverter.FromJsonValue(input);
        global::yaml.YamlNode mapping = node.Children[1];
        global::yaml.YamlNode enabled = mapping.FindPath("enabled")!;

        Assert.That(node.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(node.Children[0].Value, Is.EqualTo("first"));
        Assert.That(mapping.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(enabled.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(enabled.Value, Is.EqualTo("false"));
    }

    [Test]
    public void FromJsonValue_RoundTripsThroughToJsonValueForJsonCompatibleGraph()
    {
        Dictionary<string, object?> input = new Dictionary<string, object?>();
        List<object?> values = new List<object?>();
        values.Add(1);
        values.Add("2");
        values.Add(null);
        input["values"] = values;
        input["ready"] = true;

        global::yaml.YamlNode node = global::yaml.YamlJsonConverter.FromJsonValue(input);
        Dictionary<string, object> output = (Dictionary<string, object>)node.ToJsonValue()!;
        List<object> outputValues = (List<object>)output["values"]!;

        Assert.That(output["ready"], Is.EqualTo(true));
        Assert.That(outputValues[0], Is.EqualTo(1L));
        Assert.That(outputValues[1], Is.EqualTo("2"));
        Assert.That(outputValues[2], Is.Null);
    }

    [Test]
    public void FromJsonValue_HandlesSpecialFloatingPointValues()
    {
        global::yaml.YamlNode nan = global::yaml.YamlJsonConverter.FromJsonValue(double.NaN);
        global::yaml.YamlNode positive = global::yaml.YamlJsonConverter.FromJsonValue(double.PositiveInfinity);
        global::yaml.YamlNode negative = global::yaml.YamlJsonConverter.FromJsonValue(double.NegativeInfinity);

        Assert.That(nan.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(nan.Value, Is.EqualTo(".nan"));
        Assert.That(positive.Value, Is.EqualTo(".inf"));
        Assert.That(negative.Value, Is.EqualTo("-.inf"));
        Assert.That(double.IsNaN((double)nan.ToJsonValue()!), Is.EqualTo(true));
        Assert.That(positive.ToJsonValue(), Is.EqualTo(double.PositiveInfinity));
        Assert.That(negative.ToJsonValue(), Is.EqualTo(double.NegativeInfinity));
    }

    [Test]
    public void ToJsonValue_ConvertsTaggedScalarsToJsonCompatibleValues()
    {
        global::yaml.YamlNode nil = global::yaml.YamlNodeFactory.TaggedScalar("", global::yaml.YamlScalarResolver.NullTag);
        global::yaml.YamlNode truth = global::yaml.YamlNodeFactory.TaggedScalar("YES", global::yaml.YamlScalarResolver.BoolTag);
        global::yaml.YamlNode integer = global::yaml.YamlNodeFactory.TaggedScalar("-0x10", global::yaml.YamlScalarResolver.IntTag);
        global::yaml.YamlNode binary = global::yaml.YamlNodeFactory.TaggedScalar("SGVsbG8=", global::yaml.YamlScalarResolver.BinaryTag);
        global::yaml.YamlNode timestamp = global::yaml.YamlNodeFactory.TaggedScalar("2026-08-01", global::yaml.YamlScalarResolver.TimestampTag);

        Assert.That(nil.ToJsonValue(), Is.Null);
        Assert.That(truth.ToJsonValue(), Is.EqualTo(true));
        Assert.That(integer.ToJsonValue(), Is.EqualTo(-16L));
        Assert.That(binary.ToJsonValue(), Is.EqualTo("SGVsbG8="));
        Assert.That(timestamp.ToJsonValue(), Is.EqualTo("2026-08-01"));
    }

    [Test]
    public void ToJsonValue_ConvertsIntegersFloatsAndQuotedStrings()
    {
        global::yaml.YamlNode binaryInteger = global::yaml.YamlNodeFactory.TaggedScalar("0b101", global::yaml.YamlScalarResolver.IntTag);
        global::yaml.YamlNode octalInteger = global::yaml.YamlNodeFactory.TaggedScalar("0o17", global::yaml.YamlScalarResolver.IntTag);
        global::yaml.YamlNode floatScalar = global::yaml.YamlNodeFactory.TaggedScalar("1.5", global::yaml.YamlScalarResolver.FloatTag);
        global::yaml.YamlNode infinity = global::yaml.YamlNodeFactory.TaggedScalar("-.INF", global::yaml.YamlScalarResolver.FloatTag);
        global::yaml.YamlNode quoted = global::yaml.YamlNodeFactory.SingleQuotedScalar("123");

        Assert.That(binaryInteger.ToJsonValue(), Is.EqualTo(5L));
        Assert.That(octalInteger.ToJsonValue(), Is.EqualTo(15L));
        Assert.That(floatScalar.ToJsonValue(), Is.EqualTo(1.5d));
        Assert.That(infinity.ToJsonValue(), Is.EqualTo(double.NegativeInfinity));
        Assert.That(quoted.ToJsonValue(), Is.EqualTo("123"));
    }

    [Test]
    public void ToJsonValue_ConvertsSequencesAndMappings()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.AddMappingValue("name", global::yaml.YamlNodeFactory.Scalar("Ada"));
        root.AddMappingValue("count", global::yaml.YamlNodeFactory.TaggedScalar("3", global::yaml.YamlScalarResolver.IntTag));
        global::yaml.YamlNode items = global::yaml.YamlNodeFactory.Sequence();
        items.AddSequenceItem(global::yaml.YamlNodeFactory.TaggedScalar("true", global::yaml.YamlScalarResolver.BoolTag));
        items.AddSequenceItem(global::yaml.YamlNodeFactory.TaggedScalar("", global::yaml.YamlScalarResolver.NullTag));
        root.AddMappingValue("items", items);

        object? converted = root.ToJsonValue();
        Dictionary<string, object> mapping = (Dictionary<string, object>)converted!;
        List<object> list = (List<object>)mapping["items"]!;

        Assert.That(mapping["name"], Is.EqualTo("Ada"));
        Assert.That(mapping["count"], Is.EqualTo(3L));
        Assert.That(list[0], Is.EqualTo(true));
        Assert.That(list[1], Is.Null);
    }

    [Test]
    public void ToJsonValue_ResolvesAliasesAndBreaksRecursiveAliasesWithNull()
    {
        global::yaml.YamlNode target = global::yaml.YamlNodeFactory.SequenceWithChild(global::yaml.YamlNodeFactory.TaggedScalar("1", global::yaml.YamlScalarResolver.IntTag));
        target.Anchor = "items";
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.Anchor = "root";
        root.AddMappingValue("items", target);
        root.AddMappingValue("copy", global::yaml.YamlNodeFactory.AliasTo("items", target));
        root.AddMappingValue("self", global::yaml.YamlNodeFactory.AliasTo("root", root));

        Dictionary<string, object> mapping = (Dictionary<string, object>)root.ToJsonValue()!;
        List<object> items = (List<object>)mapping["items"]!;
        List<object> copy = (List<object>)mapping["copy"]!;

        Assert.That(items[0], Is.EqualTo(1L));
        Assert.That(copy[0], Is.EqualTo(1L));
        Assert.That(mapping["self"], Is.Null);
    }

    [Test]
    public void ToJsonValue_ComposedParserResult_UsesResolvedSchemaTags()
    {
        string text = "name: Ada\nactive: true\ncount: 3\nratio: 1.5\nitems: [1, null, 'false']\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, new global::yaml.YamlSchemaOptions());

        Dictionary<string, object> mapping = (Dictionary<string, object>)composer.Stream.Documents[0].Root!.ToJsonValue()!;
        List<object> items = (List<object>)mapping["items"]!;

        Assert.That(mapping["name"], Is.EqualTo("Ada"));
        Assert.That(mapping["active"], Is.EqualTo(true));
        Assert.That(mapping["count"], Is.EqualTo(3L));
        Assert.That(mapping["ratio"], Is.EqualTo(1.5d));
        Assert.That(items[0], Is.EqualTo(1L));
        Assert.That(items[1], Is.Null);
        Assert.That(items[2], Is.EqualTo("false"));
    }
}
