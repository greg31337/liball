using System;
using System.Collections.Generic;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class UiValueTests
{
    private const int MaximumDepth = 16;
    private const int MaximumCollectionCount = 100;
    private const int MaximumStringLength = 100;
    private const int MaximumBinaryLength = 100;

    [Test]
    public void DataModelsHaveValidPortableDefaults()
    {
        UiValue value = new UiValue();
        UiValueEntry entry = new UiValueEntry();

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(value.BooleanValue, Is.False);
            Assert.That(value.IntegerValue, Is.Zero);
            Assert.That(value.FloatingPointValue, Is.Zero);
            Assert.That(value.DecimalValue, Is.Zero);
            Assert.That(value.StringValue, Is.Null);
            Assert.That(value.DateTimeValue, Is.Null);
            Assert.That(value.BinaryValue, Is.Null);
            Assert.That(value.ArrayValue, Is.Null);
            Assert.That(value.ObjectValue, Is.Null);
            Assert.That(entry.Key, Is.Empty);
            Assert.That(entry.Value.Kind, Is.EqualTo(UiValueKind.Null));
        });

        Assert.DoesNotThrow(() => Validate(value));
    }

    [Test]
    public void ScalarKindsSerializeCanonically()
    {
        UiValue nullValue = new UiValue();
        UiValue booleanValue = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true };
        UiValue integerValue = new UiValue { Kind = UiValueKind.Integer, IntegerValue = -42L };
        UiValue floatingValue = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 1.25 };
        UiValue negativeZero = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = -0.0 };
        UiValue decimalValue = new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 123.4500m };
        UiValue stringValue = TextValue("</script>");
        UiValue dateTimeValue = new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14T12:34:56.789Z" };
        UiValue binaryValue = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 0, 1, 2, 255 } };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(nullValue), Is.EqualTo("{\"kind\":\"null\"}"));
            Assert.That(Serialize(booleanValue), Is.EqualTo("{\"kind\":\"boolean\",\"value\":true}"));
            Assert.That(Serialize(integerValue), Is.EqualTo("{\"kind\":\"integer\",\"value\":-42}"));
            Assert.That(Serialize(floatingValue), Is.EqualTo("{\"kind\":\"floatingPoint\",\"value\":1.25}"));
            Assert.That(Serialize(negativeZero), Is.EqualTo("{\"kind\":\"floatingPoint\",\"value\":0}"));
            Assert.That(Serialize(decimalValue), Is.EqualTo("{\"kind\":\"decimal\",\"value\":\"123.45\"}"));
            Assert.That(Serialize(stringValue), Is.EqualTo("{\"kind\":\"string\",\"value\":\"\\u003C/script\\u003E\"}"));
            Assert.That(Serialize(dateTimeValue), Is.EqualTo("{\"kind\":\"dateTime\",\"value\":\"2026-09-14T12:34:56.789Z\"}"));
            Assert.That(Serialize(binaryValue), Is.EqualTo("{\"kind\":\"binary\",\"value\":\"AAEC/w==\"}"));
        });
    }

    [Test]
    public void NestedValuesSerializeWithOrdinalObjectKeyOrder()
    {
        UiValue value = ObjectValue(
            Entry("z", TextValue("last")),
            Entry("A", IntegerValue(1)),
            Entry("a", ArrayValue(new UiValue(), BooleanValue(false))));

        string json = Serialize(value);

        Assert.That(json, Is.EqualTo("{\"kind\":\"object\",\"value\":{\"A\":{\"kind\":\"integer\",\"value\":1},\"a\":{\"kind\":\"array\",\"value\":[{\"kind\":\"null\"},{\"kind\":\"boolean\",\"value\":false}]},\"z\":{\"kind\":\"string\",\"value\":\"last\"}}}"));
        Assert.That(value.ObjectValue?[0].Key, Is.EqualTo("z"), "Serialization must not reorder the input model.");
    }

    [Test]
    public void EveryKindRoundTripsWithoutLosingItsTag()
    {
        UiValue[] values = new UiValue[]
        {
            new UiValue(),
            BooleanValue(true),
            IntegerValue(long.MinValue),
            new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 1.0 / 3.0 },
            new UiValue { Kind = UiValueKind.Decimal, DecimalValue = decimal.MaxValue },
            TextValue(string.Empty),
            new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "0001-01-01T00:00:00" },
            new UiValue { Kind = UiValueKind.Binary, BinaryValue = Array.Empty<byte>() },
            ArrayValue(TextValue("item")),
            ObjectValue(Entry("key", IntegerValue(7)))
        };

        foreach (UiValue value in values)
        {
            string json = Serialize(value);
            UiValue result = Deserialize(json);

            Assert.That(result.Kind, Is.EqualTo(value.Kind), json);
            Assert.That(AreEqual(value, result), Is.True, json);
            Assert.That(Serialize(result), Is.EqualTo(json));
        }
    }

    [Test]
    public void DeepComparisonUsesArrayOrderAndOrdinalObjectKeys()
    {
        UiValue first = ObjectValue(Entry("b", BooleanValue(true)), Entry("a", IntegerValue(1)));
        UiValue reordered = ObjectValue(Entry("a", IntegerValue(1)), Entry("b", BooleanValue(true)));
        UiValue wrongCase = ObjectValue(Entry("A", IntegerValue(1)), Entry("b", BooleanValue(true)));
        UiValue array = ArrayValue(IntegerValue(1), IntegerValue(2));
        UiValue reversedArray = ArrayValue(IntegerValue(2), IntegerValue(1));
        UiValue binary = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 3 } };
        UiValue changedBinary = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 4 } };

        Assert.Multiple(() =>
        {
            Assert.That(AreEqual(first, reordered), Is.True);
            Assert.That(AreEqual(first, wrongCase), Is.False);
            Assert.That(AreEqual(array, reversedArray), Is.False);
            Assert.That(AreEqual(binary, changedBinary), Is.False);
            Assert.That(AreEqual(IntegerValue(1), new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 1m }), Is.False);
        });
    }

    [Test]
    public void CloneIsDeepAndPreservesEntryOrder()
    {
        UiValue source = ObjectValue(
            Entry("bytes", new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2 } }),
            Entry("array", ArrayValue(TextValue("original"))));

        UiValue clone = ValueJsonCodec.CloneValue(source, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);

        Assert.That(AreEqual(source, clone), Is.True);
        Assert.That(clone, Is.Not.SameAs(source));
        Assert.That(clone.ObjectValue, Is.Not.SameAs(source.ObjectValue));
        UiValueEntry[] cloneEntries = clone.ObjectValue ?? Array.Empty<UiValueEntry>();
        UiValueEntry[] sourceEntries = source.ObjectValue ?? Array.Empty<UiValueEntry>();
        Assert.That(cloneEntries[0].Key, Is.EqualTo("bytes"));
        Assert.That(cloneEntries[0], Is.Not.SameAs(sourceEntries[0]));
        Assert.That(cloneEntries[0].Value.BinaryValue, Is.Not.SameAs(sourceEntries[0].Value.BinaryValue));

        byte[] cloneBytes = cloneEntries[0].Value.BinaryValue ?? Array.Empty<byte>();
        cloneBytes[0] = 9;
        UiValue[] cloneItems = cloneEntries[1].Value.ArrayValue ?? Array.Empty<UiValue>();
        cloneItems[0].StringValue = "changed";

        Assert.That(sourceEntries[0].Value.BinaryValue, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(sourceEntries[1].Value.ArrayValue?[0].StringValue, Is.EqualTo("original"));
        Assert.That(AreEqual(source, clone), Is.False);
    }

    [Test]
    public void InvalidTaggedShapesAreRejected()
    {
        UiValue invalidNull = new UiValue { StringValue = "data" };
        UiValue missingString = new UiValue { Kind = UiValueKind.String };
        UiValue mixedString = new UiValue { Kind = UiValueKind.String, StringValue = "data", BooleanValue = true };
        UiValue invalidKind = new UiValue { Kind = (UiValueKind)100 };
        UiValue notANumber = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = double.NaN };
        UiValue infinity = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = double.PositiveInfinity };
        UiValue nullArrayItem = new UiValue { Kind = UiValueKind.Array, ArrayValue = new UiValue[] { null! } };
        UiValue nullEntry = new UiValue { Kind = UiValueKind.Object, ObjectValue = new UiValueEntry[] { null! } };
        UiValue nullKey = ObjectValue(Entry("key", new UiValue()));
        UiValueEntry[] nullKeyEntries = nullKey.ObjectValue ?? Array.Empty<UiValueEntry>();
        nullKeyEntries[0].Key = null!;
        UiValue nullEntryValue = ObjectValue(Entry("key", new UiValue()));
        UiValueEntry[] nullValueEntries = nullEntryValue.ObjectValue ?? Array.Empty<UiValueEntry>();
        nullValueEntries[0].Value = null!;
        UiValue duplicateKeys = ObjectValue(Entry("same", new UiValue()), Entry("same", BooleanValue(true)));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => ValueJsonCodec.ValidateValue(null, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength));
            Assert.Throws<ArgumentException>(() => Validate(invalidNull));
            Assert.Throws<ArgumentException>(() => Validate(missingString));
            Assert.Throws<ArgumentException>(() => Validate(mixedString));
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate(invalidKind));
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate(notANumber));
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate(infinity));
            Assert.Throws<ArgumentNullException>(() => Validate(nullArrayItem));
            Assert.Throws<ArgumentException>(() => Validate(nullEntry));
            Assert.Throws<ArgumentException>(() => Validate(nullKey));
            Assert.Throws<ArgumentNullException>(() => Validate(nullEntryValue));
            Assert.Throws<ArgumentException>(() => Validate(duplicateKeys));
        });
    }

    [Test]
    public void CyclesAndConfiguredBoundsAreRejected()
    {
        UiValue cycle = ArrayValue();
        cycle.ArrayValue = new UiValue[] { cycle };
        UiValue nested = ArrayValue(ArrayValue(new UiValue()));
        UiValue aggregate = ArrayValue(ArrayValue(new UiValue()), new UiValue());
        UiValue longText = TextValue("four");
        UiValue longKey = ObjectValue(Entry("four", new UiValue()));
        UiValue binary = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2 } };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Validate(cycle));
            Assert.Throws<ArgumentException>(() => ValueJsonCodec.ValidateValue(nested, 1, 10, 10, 10));
            Assert.Throws<ArgumentException>(() => ValueJsonCodec.ValidateValue(aggregate, 4, 2, 10, 10));
            Assert.Throws<ArgumentException>(() => ValueJsonCodec.ValidateValue(longText, 1, 1, 3, 1));
            Assert.Throws<ArgumentException>(() => ValueJsonCodec.ValidateValue(longKey, 1, 1, 3, 1));
            Assert.Throws<ArgumentException>(() => ValueJsonCodec.ValidateValue(binary, 1, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ValueJsonCodec.ValidateValue(new UiValue(), 129, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ValueJsonCodec.ValidateValue(new UiValue(), 0, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ValueJsonCodec.ValidateValue(new UiValue(), 0, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ValueJsonCodec.ValidateValue(new UiValue(), 0, 0, 0, -1));
        });
    }

    [Test]
    public void MalformedOrOutOfBoundsJsonIsRejected()
    {
        string duplicateObjectKeys = "{\"kind\":\"object\",\"value\":{\"a\":{\"kind\":\"null\"},\"a\":{\"kind\":\"null\"}}}";
        string tooDeep = "{\"kind\":\"array\",\"value\":[{\"kind\":\"null\"}]}";
        string tooMany = "{\"kind\":\"array\",\"value\":[{\"kind\":\"null\"},{\"kind\":\"null\"}]}";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => DeserializeWithLimits(null, 2, 2, 10, 10));
            Assert.Throws<ArgumentException>(() => DeserializeWithLimits(string.Empty, 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("null", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"null\",\"value\":null}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"string\"}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"future\",\"value\":0}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"boolean\",\"value\":1}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"null\",\"extra\":0}", 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits(duplicateObjectKeys, 2, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits(tooDeep, 0, 2, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits(tooMany, 2, 1, 10, 10));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"string\",\"value\":\"four\"}", 1, 1, 3, 1));
            Assert.Throws<JsonException>(() => DeserializeWithLimits("{\"kind\":\"binary\",\"value\":\"AQI=\"}", 1, 1, 0, 1));
            Assert.Throws<FormatException>(() => DeserializeWithLimits("{\"kind\":\"binary\",\"value\":\"!\"}", 1, 1, 10, 10));
        });
    }

    private static void Validate(UiValue value)
    {
        ValueJsonCodec.ValidateValue(value, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);
    }

    private static string Serialize(UiValue value)
    {
        return ValueJsonCodec.SerializeValue(value, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);
    }

    private static UiValue Deserialize(string json)
    {
        return DeserializeWithLimits(json, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);
    }

    private static UiValue DeserializeWithLimits(string? json, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        return ValueJsonCodec.DeserializeValue(json, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
    }

    private static bool AreEqual(UiValue left, UiValue right)
    {
        return ValueJsonCodec.ValuesEqual(left, right, MaximumDepth, MaximumCollectionCount, MaximumStringLength, MaximumBinaryLength);
    }

    private static UiValue BooleanValue(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ArrayValue(params UiValue[] values)
    {
        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    private static UiValue ObjectValue(params UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }
}
