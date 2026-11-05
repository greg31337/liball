using token;

namespace token.Tests;

public class CborWriterTests
{
    [TestCase(0UL, new byte[] { 0x00 })]
    [TestCase(23UL, new byte[] { 0x17 })]
    [TestCase(24UL, new byte[] { 0x18, 0x18 })]
    [TestCase(255UL, new byte[] { 0x18, 0xFF })]
    [TestCase(256UL, new byte[] { 0x19, 0x01, 0x00 })]
    [TestCase(65535UL, new byte[] { 0x19, 0xFF, 0xFF })]
    [TestCase(65536UL, new byte[] { 0x1A, 0x00, 0x01, 0x00, 0x00 })]
    [TestCase(4294967295UL, new byte[] { 0x1A, 0xFF, 0xFF, 0xFF, 0xFF })]
    [TestCase(4294967296UL, new byte[] { 0x1B, 0, 0, 0, 1, 0, 0, 0, 0 })]
    [TestCase(ulong.MaxValue, new byte[] { 0x1B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void WriteUnsignedIntegerUsesPreferredEncoding(ulong value, byte[] expected)
    {
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteUnsignedInteger(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        });
    }

    [TestCase(0UL, new byte[] { 0x20 })]
    [TestCase(23UL, new byte[] { 0x37 })]
    [TestCase(24UL, new byte[] { 0x38, 0x18 })]
    [TestCase(ulong.MaxValue, new byte[] { 0x3B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void WriteNegativeIntegerUsesPreferredArgumentEncoding(ulong argument, byte[] expected)
    {
        CborWriter writer = CborWriter.Create();

        writer.WriteNegativeInteger(argument);

        Assert.That(writer.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void WriteInt64HandlesSignedBoundaries()
    {
        CborWriter writer = CborWriter.Create();

        writer.WriteInt64(long.MinValue);
        writer.WriteInt64(-1);
        writer.WriteInt64(0);
        writer.WriteInt64(long.MaxValue);

        CborReader reader = CborReader.Create(writer.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read().Item.IntegerArgument, Is.EqualTo((ulong)long.MaxValue));
            Assert.That(reader.Read().Item.IntegerArgument, Is.Zero);
            Assert.That(reader.Read().Item.IntegerArgument, Is.Zero);
            Assert.That(reader.Read().Item.IntegerArgument, Is.EqualTo((ulong)long.MaxValue));
        });
    }

    [Test]
    public void WriteByteStringsUsePreferredDefiniteLengthsAndCopyInput()
    {
        byte[] shortValue = Enumerable.Range(0, 23).Select(value => (byte)value).ToArray();
        byte[] longValue = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        CborWriter writer = CborWriter.Create();

        writer.WriteByteString(shortValue);
        writer.WriteByteStringSpan(longValue.AsSpan());
        shortValue[0] = 99;
        longValue[0] = 99;

        byte[] encoded = writer.ToArray();
        CborReader reader = CborReader.Create(encoded);
        CborReadResult first = reader.Read();
        CborReadResult second = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(encoded[0], Is.EqualTo(0x57));
            Assert.That(encoded[24], Is.EqualTo(0x59));
            Assert.That(encoded[25], Is.EqualTo(0x01));
            Assert.That(encoded[26], Is.EqualTo(0x00));
            Assert.That(first.Item.Bytes[0], Is.Zero);
            Assert.That(second.Item.Bytes[0], Is.Zero);
            Assert.That(second.Item.Bytes, Has.Length.EqualTo(256));
        });
    }

    [Test]
    public void WriteTextStringUsesStrictUtf8AndByteLength()
    {
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteTextString("hé😀");
        byte[] encoded = writer.ToArray();
        CborReadResult decoded = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(encoded[0], Is.EqualTo(0x67));
            Assert.That(decoded.Item.Text, Is.EqualTo("hé😀"));
        });
    }

    [Test]
    public void WriteTextStringRejectsInvalidUtf16Atomically()
    {
        CborWriter writer = CborWriter.Create();
        writer.WriteUnsignedInteger(1);
        string invalid = new string(new char[] { 'a', '\uD800' });

        TokenResult result = writer.WriteTextString(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_utf16"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void WriteBooleanNullUndefinedAndSimpleValues()
    {
        CborWriter writer = CborWriter.Create();

        writer.WriteBoolean(false);
        writer.WriteBoolean(true);
        writer.WriteNull();
        writer.WriteUndefined();
        writer.WriteSimple(0);
        writer.WriteSimple(19);
        writer.WriteSimple(24);
        writer.WriteSimple(255);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[]
        {
            0xF4, 0xF5, 0xF6, 0xF7, 0xE0, 0xF3, 0xF8, 0x18, 0xF8, 0xFF
        }));
    }

    [TestCase(20)]
    [TestCase(21)]
    [TestCase(22)]
    [TestCase(23)]
    public void WriteSimpleRejectsValuesWithDedicatedSemantics(byte value)
    {
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteSimple(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_cbor_simple"));
            Assert.That(writer.GetLength(), Is.Zero);
        });
    }

    [Test]
    public void WriteFloatingPointValuesUsesRequestedWidths()
    {
        CborWriter writer = CborWriter.Create();

        writer.WriteHalf((Half)1.5);
        writer.WriteSingle(100000.0f);
        writer.WriteDouble(1.1);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[]
        {
            0xF9, 0x3E, 0x00,
            0xFA, 0x47, 0xC3, 0x50, 0x00,
            0xFB, 0x3F, 0xF1, 0x99, 0x99, 0x99, 0x99, 0x99, 0x9A
        }));
    }

    [Test]
    public void WriteFloatingPointSpecialValuesRoundTrip()
    {
        CborWriter writer = CborWriter.Create();
        writer.WriteHalf(Half.PositiveInfinity);
        writer.WriteSingle(float.NegativeInfinity);
        writer.WriteDouble(double.NaN);

        CborReader reader = CborReader.Create(writer.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(double.IsPositiveInfinity(reader.Read().Item.FloatingPointValue), Is.True);
            Assert.That(double.IsNegativeInfinity(reader.Read().Item.FloatingPointValue), Is.True);
            Assert.That(double.IsNaN(reader.Read().Item.FloatingPointValue), Is.True);
        });
    }

    [Test]
    public void OutputLimitFailuresAreAtomicAndWriterCanRecover()
    {
        CborWriter writer = CborWriter.Create(4);
        writer.WriteUnsignedInteger(1);

        TokenResult failed = writer.WriteByteString(new byte[] { 2, 3, 4 });
        TokenResult recovered = writer.WriteByteString(new byte[] { 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(recovered.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x01, 0x42, 0x02, 0x03 }));
            Assert.That(writer.GetRemainingCapacity(), Is.Zero);
        });
    }

    [Test]
    public void PrimitiveWriteFailureAtLimitDoesNotAppendPartialBytes()
    {
        CborWriter writer = CborWriter.Create(1);
        writer.WriteNull();

        TokenResult result = writer.WriteUnsignedInteger(24);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void WriterRejectsInvalidArguments()
    {
        CborWriter writer = CborWriter.Create();

        Assert.Multiple(() =>
        {
            Assert.That(() => CborWriter.Create(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => writer.WriteByteString((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => writer.WriteTextString(null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void WriteItemEncodesNestedArraysMapsAndTagsWithDefiniteLengths()
    {
        CborItem one = Unsigned(1);
        CborItem key = Text("a");
        CborItem yes = Boolean(true);
        CborItem bytes = ByteStringItem(new byte[] { 1, 2 });
        CborItem tagged = TagItem(24, bytes);
        CborItem map = MapItem(new CborMapEntry[] { new CborMapEntry(key, yes) });
        CborItem array = ArrayItem(new CborItem[] { one, map, tagged });
        CborWriter writer = CborWriter.Create();

        TokenResult result = writer.WriteItem(array);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[]
            {
                0x83, 0x01, 0xA1, 0x61, 0x61, 0xF5, 0xD8, 0x18, 0x42, 0x01, 0x02
            }));
        });
    }

    [Test]
    public void WriteArrayMapAndTagConvenienceMethodsRoundTrip()
    {
        CborWriter writer = CborWriter.Create();
        writer.WriteArray(new CborItem[] { Unsigned(1), Text("two") });
        writer.WriteMap(new CborMapEntry[]
        {
            new CborMapEntry(Text("b"), Unsigned(2)),
            new CborMapEntry(Text("a"), Unsigned(1))
        });
        writer.WriteTag(1, Text("date"));

        CborReader reader = CborReader.Create(writer.ToArray());
        CborReadResult array = reader.Read();
        CborReadResult map = reader.Read();
        CborReadResult tag = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(array.Item.ArrayValue[1].Text, Is.EqualTo("two"));
            Assert.That(map.Item.MapValue[0].Key.Text, Is.EqualTo("b"));
            Assert.That(map.Item.MapValue[1].Key.Text, Is.EqualTo("a"));
            Assert.That(tag.Item.TagValue, Is.EqualTo(1));
            Assert.That(tag.Item.TaggedValue.Text, Is.EqualTo("date"));
            Assert.That(reader.GetRemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void ContainerWriteFailuresAreAtomic()
    {
        CborWriter writer = CborWriter.Create(3);
        writer.WriteUnsignedInteger(9);

        TokenResult result = writer.WriteArray(new CborItem[] { Unsigned(1), Unsigned(2), Unsigned(3) });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void WriteItemRejectsIncompleteModelsWithoutChangingOutput()
    {
        CborWriter writer = CborWriter.Create();
        writer.WriteNull();
        CborItem missingArray = new CborItem();
        missingArray.Kind = TokenCborValueKind.Array;
        CborItem missingTag = new CborItem();
        missingTag.Kind = TokenCborValueKind.Tag;
        CborItem invalidFloat = new CborItem();
        invalidFloat.Kind = TokenCborValueKind.FloatingPoint;
        invalidFloat.FloatingPointWidth = 3;

        TokenResult arrayResult = writer.WriteItem(missingArray);
        TokenResult tagResult = writer.WriteItem(missingTag);
        TokenResult floatResult = writer.WriteItem(invalidFloat);

        Assert.Multiple(() =>
        {
            Assert.That(arrayResult.Error.Code, Is.EqualTo("invalid_cbor_item"));
            Assert.That(tagResult.Error.Code, Is.EqualTo("invalid_cbor_item"));
            Assert.That(floatResult.Error.Code, Is.EqualTo("invalid_cbor_item"));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xF6 }));
        });
    }

    [Test]
    public void ContainerMethodsRejectNullArguments()
    {
        CborWriter writer = CborWriter.Create();

        Assert.Multiple(() =>
        {
            Assert.That(() => writer.WriteItem(null), Throws.ArgumentNullException);
            Assert.That(() => writer.WriteArray(null), Throws.ArgumentNullException);
            Assert.That(() => writer.WriteMap(null), Throws.ArgumentNullException);
            Assert.That(() => writer.WriteTag(1, null), Throws.ArgumentNullException);
            Assert.That(() => new CborMapEntry(null, Unsigned(1)), Throws.ArgumentNullException);
            Assert.That(() => new CborMapEntry(Unsigned(1), null), Throws.ArgumentNullException);
        });
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

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem ByteStringItem(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem ArrayItem(CborItem[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = value;
        return item;
    }

    static CborItem MapItem(CborMapEntry[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = value;
        return item;
    }

    static CborItem TagItem(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }
}
