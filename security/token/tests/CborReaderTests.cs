using token;

namespace token.Tests;

public class CborReaderTests
{
    [TestCase(new byte[] { 0x00 }, 0UL)]
    [TestCase(new byte[] { 0x17 }, 23UL)]
    [TestCase(new byte[] { 0x18, 0x18 }, 24UL)]
    [TestCase(new byte[] { 0x18, 0xFF }, 255UL)]
    [TestCase(new byte[] { 0x19, 0x01, 0x00 }, 256UL)]
    [TestCase(new byte[] { 0x1A, 0x00, 0x01, 0x00, 0x00 }, 65536UL)]
    [TestCase(new byte[] { 0x1B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, ulong.MaxValue)]
    public void ReadDecodesUnsignedIntegers(byte[] encoded, ulong expected)
    {
        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Item.Kind, Is.EqualTo(TokenCborValueKind.UnsignedInteger));
            Assert.That(result.Item.IntegerArgument, Is.EqualTo(expected));
        });
    }

    [TestCase(new byte[] { 0x20 }, 0UL)]
    [TestCase(new byte[] { 0x37 }, 23UL)]
    [TestCase(new byte[] { 0x38, 0x18 }, 24UL)]
    [TestCase(new byte[] { 0x3B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, ulong.MaxValue)]
    public void ReadDecodesNegativeIntegerArguments(byte[] encoded, ulong expectedArgument)
    {
        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Item.Kind, Is.EqualTo(TokenCborValueKind.NegativeInteger));
            Assert.That(result.Item.IntegerArgument, Is.EqualTo(expectedArgument));
        });
    }

    [Test]
    public void ReadDecodesDefiniteByteAndTextStrings()
    {
        CborReader reader = CborReader.Create(new byte[]
        {
            0x43, 1, 2, 3,
            0x67, 0x68, 0xC3, 0xA9, 0xF0, 0x9F, 0x98, 0x80
        });

        CborReadResult bytes = reader.Read();
        CborReadResult text = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Item.Kind, Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(bytes.Item.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(text.Item.Kind, Is.EqualTo(TokenCborValueKind.TextString));
            Assert.That(text.Item.Text, Is.EqualTo("hé😀"));
            Assert.That(reader.GetRemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void ReadConcatenatesIndefiniteByteAndTextStrings()
    {
        CborReader byteReader = CborReader.Create(new byte[] { 0x5F, 0x42, 1, 2, 0x41, 3, 0xFF });
        CborReader textReader = CborReader.Create(new byte[] { 0x7F, 0x61, 0x68, 0x62, 0xC3, 0xA9, 0xFF });

        CborReadResult bytes = byteReader.Read();
        CborReadResult text = textReader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Item.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(text.Item.Text, Is.EqualTo("hé"));
        });
    }

    [Test]
    public void ReadDecodesBooleansNullUndefinedAndSimpleValuesSequentially()
    {
        CborReader reader = CborReader.Create(new byte[] { 0xF4, 0xF5, 0xF6, 0xF7, 0xF0, 0xF8, 0xFF });

        CborReadResult falseValue = reader.Read();
        CborReadResult trueValue = reader.Read();
        CborReadResult nullValue = reader.Read();
        CborReadResult undefinedValue = reader.Read();
        CborReadResult simple16 = reader.Read();
        CborReadResult simple255 = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(falseValue.Item.Kind, Is.EqualTo(TokenCborValueKind.Boolean));
            Assert.That(falseValue.Item.BooleanValue, Is.False);
            Assert.That(trueValue.Item.BooleanValue, Is.True);
            Assert.That(nullValue.Item.Kind, Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(undefinedValue.Item.Kind, Is.EqualTo(TokenCborValueKind.Undefined));
            Assert.That(simple16.Item.SimpleValue, Is.EqualTo(16));
            Assert.That(simple255.Item.SimpleValue, Is.EqualTo(255));
            Assert.That(reader.GetPosition(), Is.EqualTo(7));
        });
    }

    [Test]
    public void ReadDecodesHalfSingleAndDoublePrecisionValues()
    {
        CborReader reader = CborReader.Create(new byte[]
        {
            0xF9, 0x3E, 0x00,
            0xFA, 0x47, 0xC3, 0x50, 0x00,
            0xFB, 0x3F, 0xF1, 0x99, 0x99, 0x99, 0x99, 0x99, 0x9A
        });

        CborReadResult half = reader.Read();
        CborReadResult single = reader.Read();
        CborReadResult doubleValue = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(half.Item.FloatingPointValue, Is.EqualTo(1.5));
            Assert.That(half.Item.FloatingPointWidth, Is.EqualTo(2));
            Assert.That(single.Item.FloatingPointValue, Is.EqualTo(100000.0));
            Assert.That(single.Item.FloatingPointWidth, Is.EqualTo(4));
            Assert.That(doubleValue.Item.FloatingPointValue, Is.EqualTo(1.1));
            Assert.That(doubleValue.Item.FloatingPointWidth, Is.EqualTo(8));
        });
    }

    [Test]
    public void ReadPreservesFloatingPointSpecialValues()
    {
        CborReadResult positiveInfinity = CborReader.Create(new byte[] { 0xF9, 0x7C, 0x00 }).Read();
        CborReadResult nan = CborReader.Create(new byte[] { 0xF9, 0x7E, 0x00 }).Read();

        Assert.Multiple(() =>
        {
            Assert.That(double.IsPositiveInfinity(positiveInfinity.Item.FloatingPointValue), Is.True);
            Assert.That(double.IsNaN(nan.Item.FloatingPointValue), Is.True);
        });
    }

    [TestCase(new byte[] { }, "unexpected_end_of_cbor", 0)]
    [TestCase(new byte[] { 0x18 }, "unexpected_end_of_cbor", 1)]
    [TestCase(new byte[] { 0x1C }, "invalid_cbor", 0)]
    [TestCase(new byte[] { 0x1F }, "invalid_cbor", 0)]
    [TestCase(new byte[] { 0x43, 1, 2 }, "unexpected_end_of_cbor", 3)]
    [TestCase(new byte[] { 0xFF }, "invalid_cbor", 0)]
    [TestCase(new byte[] { 0xF9, 0 }, "unexpected_end_of_cbor", 2)]
    [TestCase(new byte[] { 0xFA, 0, 0, 0 }, "unexpected_end_of_cbor", 4)]
    [TestCase(new byte[] { 0xFB, 0, 0, 0, 0, 0, 0, 0 }, "unexpected_end_of_cbor", 8)]
    public void ReadRejectsMalformedAndTruncatedPrimitives(byte[] encoded, string code, int errorPosition)
    {
        CborReader reader = CborReader.Create(encoded);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Item, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(errorPosition));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReadRejectsInvalidUtf8AtEncodedBytePosition()
    {
        CborReadResult definite = CborReader.Create(new byte[] { 0x63, 0x61, 0xC0, 0x80 }).Read();
        CborReadResult chunked = CborReader.Create(new byte[] { 0x7F, 0x62, 0xC0, 0x80, 0xFF }).Read();

        Assert.Multiple(() =>
        {
            Assert.That(definite.Error.Code, Is.EqualTo("invalid_cbor_utf8"));
            Assert.That(definite.Error.Position, Is.EqualTo(2));
            Assert.That(chunked.Error.Code, Is.EqualTo("invalid_cbor_utf8"));
            Assert.That(chunked.Error.Position, Is.EqualTo(2));
        });
    }

    [TestCase(new byte[] { 0x5F, 0x61, 0x61, 0xFF }, 1)]
    [TestCase(new byte[] { 0x7F, 0x41, 0x61, 0xFF }, 1)]
    [TestCase(new byte[] { 0x5F, 0x5F, 0xFF, 0xFF }, 1)]
    [TestCase(new byte[] { 0x7F, 0x61, 0x61 }, 3)]
    public void ReadRejectsMalformedIndefiniteStrings(byte[] encoded, int errorPosition)
    {
        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Position, Is.EqualTo(errorPosition));
        });
    }

    [Test]
    public void ReadHandlesDefiniteNestedArraysMapsAndTags()
    {
        byte[] encoded = new byte[]
        {
            0x83,
            0x01,
            0xA2, 0x61, 0x61, 0xF5, 0x02, 0x82, 0xF6, 0x03,
            0xD8, 0x18, 0x42, 0x01, 0x02
        };

        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Item.Kind, Is.EqualTo(TokenCborValueKind.Array));
            Assert.That(result.Item.ArrayValue, Has.Length.EqualTo(3));
            Assert.That(result.Item.ArrayValue[0].IntegerArgument, Is.EqualTo(1));
            Assert.That(result.Item.ArrayValue[1].Kind, Is.EqualTo(TokenCborValueKind.Map));
            Assert.That(result.Item.ArrayValue[1].MapValue, Has.Length.EqualTo(2));
            Assert.That(result.Item.ArrayValue[1].MapValue[0].Key.Text, Is.EqualTo("a"));
            Assert.That(result.Item.ArrayValue[1].MapValue[0].Value.BooleanValue, Is.True);
            Assert.That(result.Item.ArrayValue[1].MapValue[1].Value.ArrayValue[1].IntegerArgument, Is.EqualTo(3));
            Assert.That(result.Item.ArrayValue[2].Kind, Is.EqualTo(TokenCborValueKind.Tag));
            Assert.That(result.Item.ArrayValue[2].TagValue, Is.EqualTo(24));
            Assert.That(result.Item.ArrayValue[2].TaggedValue.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void ReadHandlesIndefiniteArraysAndMaps()
    {
        byte[] encoded = new byte[]
        {
            0x9F, 0x01,
            0xBF, 0x61, 0x61, 0x02, 0x61, 0x62, 0x9F, 0x03, 0xFF, 0xFF,
            0xFF
        };

        CborReadResult result = CborReader.Create(encoded).Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Item.ArrayValue, Has.Length.EqualTo(2));
            Assert.That(result.Item.ArrayValue[1].MapValue, Has.Length.EqualTo(2));
            Assert.That(result.Item.ArrayValue[1].MapValue[1].Value.ArrayValue[0].IntegerArgument, Is.EqualTo(3));
        });
    }

    [TestCase(new byte[] { 0x82, 0x01 }, 2)]
    [TestCase(new byte[] { 0x9F, 0x01 }, 2)]
    [TestCase(new byte[] { 0xA1, 0x01 }, 2)]
    [TestCase(new byte[] { 0xBF, 0x01, 0xFF }, 2)]
    [TestCase(new byte[] { 0xC1 }, 1)]
    [TestCase(new byte[] { 0xDF, 0x01 }, 0)]
    public void ReadRejectsTruncatedOrInvalidContainersAndTags(byte[] encoded, int errorPosition)
    {
        CborReader reader = CborReader.Create(encoded);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Position, Is.EqualTo(errorPosition));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReaderEnforcesInputLimitBeforeReading()
    {
        CborReader reader = CborReader.Create(new byte[] { 0x01, 0x02 }, 1);

        CborReadResult result = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Code, Is.EqualTo("cbor_size_exceeded"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ConstructorRejectsInvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => CborReader.Create(null), Throws.ArgumentNullException);
            Assert.That(() => CborReader.Create(null, 0), Throws.ArgumentNullException);
            Assert.That(() => CborReader.Create(Array.Empty<byte>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
