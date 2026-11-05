using token;

namespace token.Tests;

public class CborMapKeyComparerTests
{
    [Test]
    public void ValidateRejectsTheSecondEquivalentKey()
    {
        CborMapEntry[] entries = new CborMapEntry[]
        {
            Entry(Text("name"), Unsigned(1)),
            Entry(Text("name"), Unsigned(2))
        };

        TokenResult result = CborMapKeyComparer.Validate(entries);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void EqualityUsesGenericDataModelTypeDistinctions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CborMapKeyComparer.AreEqual(Unsigned(1), Floating(1.0, 8)), Is.False);
            Assert.That(CborMapKeyComparer.AreEqual(Text("a"), Bytes(new byte[] { 0x61 })), Is.False);
            Assert.That(CborMapKeyComparer.AreEqual(Text("a"), Tag(1, Text("a"))), Is.False);
            Assert.That(CborMapKeyComparer.AreEqual(Simple(2), Unsigned(2)), Is.False);
        });
    }

    [Test]
    public void EqualityHandlesFloatingPointRepresentationsAndNanSignificands()
    {
        double positiveNan = BitConverter.Int64BitsToDouble(unchecked((long)0x7FF8000000001234UL));
        double negativeNan = BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8000000001234UL));
        double otherNan = BitConverter.Int64BitsToDouble(unchecked((long)0x7FF8000000005678UL));

        Assert.Multiple(() =>
        {
            Assert.That(CborMapKeyComparer.AreEqual(Floating(1.5, 2), Floating(1.5, 8)), Is.True);
            Assert.That(CborMapKeyComparer.AreEqual(Floating(0.0, 8), Floating(-0.0, 8)), Is.True);
            Assert.That(CborMapKeyComparer.AreEqual(Floating(positiveNan, 8), Floating(negativeNan, 8)), Is.True);
            Assert.That(CborMapKeyComparer.AreEqual(Floating(positiveNan, 8), Floating(otherNan, 8)), Is.False);
        });
    }

    [Test]
    public void EqualityComparesArraysByOrderAndMapsWithoutOrder()
    {
        CborItem firstMap = Map(new CborMapEntry[]
        {
            Entry(Text("a"), Array(Unsigned(1), Unsigned(2))),
            Entry(Text("b"), Boolean(true))
        });
        CborItem reorderedMap = Map(new CborMapEntry[]
        {
            Entry(Text("b"), Boolean(true)),
            Entry(Text("a"), Array(Unsigned(1), Unsigned(2)))
        });

        Assert.Multiple(() =>
        {
            Assert.That(CborMapKeyComparer.AreEqual(firstMap, reorderedMap), Is.True);
            Assert.That(
                CborMapKeyComparer.AreEqual(Array(Unsigned(1), Unsigned(2)), Array(Unsigned(2), Unsigned(1))),
                Is.False);
        });
    }

    [TestCase(new byte[] { 0xA2, 0x01, 0xF4, 0x18, 0x01, 0xF5 }, 3)]
    [TestCase(new byte[] { 0xA2, 0x61, 0x61, 0x00, 0x7F, 0x61, 0x61, 0xFF, 0x01 }, 4)]
    [TestCase(new byte[] { 0xBF, 0x41, 0x61, 0x00, 0x5F, 0x41, 0x61, 0xFF, 0x01, 0xFF }, 4)]
    [TestCase(new byte[] { 0xA2, 0xF9, 0x00, 0x00, 0x00, 0xF9, 0x80, 0x00, 0x01 }, 5)]
    [TestCase(new byte[] { 0xA2, 0xF9, 0x7E, 0x01, 0x00, 0xFA, 0x7F, 0xC0, 0x20, 0x00, 0x01 }, 5)]
    public void ReaderRejectsEquivalentKeysWithDifferentWireEncodings(byte[] encoded, int duplicatePosition)
    {
        CborReader reader = CborReader.Create(encoded);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Item, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(result.Error.Position, Is.EqualTo(duplicatePosition));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReaderRejectsDuplicatesInsideNestedMapsAtTheirEncodedPosition()
    {
        byte[] encoded = new byte[] { 0x81, 0xA2, 0x61, 0x61, 0x00, 0x61, 0x61, 0x01 };
        CborReader reader = CborReader.Create(encoded);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(result.Error.Position, Is.EqualTo(5));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void WritersRejectDuplicateKeysWithoutChangingOutput()
    {
        CborMapEntry[] entries = new CborMapEntry[]
        {
            Entry(Array(Unsigned(1), Text("a")), Unsigned(1)),
            Entry(Array(Unsigned(1), Text("a")), Unsigned(2))
        };
        CborWriter ordinary = CborWriter.Create();
        CborWriter deterministic = CborWriter.Create();
        ordinary.WriteNull();
        deterministic.WriteNull();

        TokenResult ordinaryResult = ordinary.WriteMap(entries);
        TokenResult deterministicResult = deterministic.WriteDeterministicMap(entries);

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryResult.Error.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(deterministicResult.Error.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(ordinary.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
            Assert.That(deterministic.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void PublicValidationMethodsRejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => CborMapKeyComparer.Validate(null), Throws.ArgumentNullException);
            Assert.That(() => CborMapKeyComparer.AreEqual(null, Unsigned(1)), Throws.ArgumentNullException);
            Assert.That(() => CborMapKeyComparer.AreEqual(Unsigned(1), null), Throws.ArgumentNullException);
        });
    }

    static CborMapEntry Entry(CborItem key, CborItem value)
    {
        return new CborMapEntry(key, value);
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Simple(byte value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Simple;
        item.SimpleValue = value;
        return item;
    }

    static CborItem Floating(double value, int width)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.FloatingPoint;
        item.FloatingPointValue = value;
        item.FloatingPointWidth = width;
        return item;
    }

    static CborItem Array(CborItem first, CborItem second)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = new CborItem[] { first, second };
        return item;
    }

    static CborItem Map(CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static CborItem Tag(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }
}
