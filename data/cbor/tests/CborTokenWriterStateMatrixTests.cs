using System.Buffers;
using System.Numerics;
using System.Text;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenWriterStateMatrixTests
{
    [Test]
    public void EveryValueShapeTransitionsThroughRootArrayMapAndTagContexts()
    {
        foreach (CborToken[] value in ValueSequences())
        {
            AssertWellFormed([], value, []);
            AssertWellFormed([StartArray(1, CborLengthKind.Definite)], value, [EndArray()]);
            AssertWellFormed([StartArray(0, CborLengthKind.Indefinite)], value, [EndArray()]);
            AssertWellFormed(
                [StartMap(1, CborLengthKind.Definite)],
                value,
                [Simple(CborSimpleValues.Null), EndMap()]);
            AssertWellFormed(
                [
                    StartMap(1, CborLengthKind.Definite),
                    Simple(CborSimpleValues.Null)
                ],
                value,
                [EndMap()]);
            AssertWellFormed(
                [StartMap(0, CborLengthKind.Indefinite)],
                value,
                [Simple(CborSimpleValues.Null), EndMap()]);
            AssertWellFormed(
                [
                    StartMap(0, CborLengthKind.Indefinite),
                    Simple(CborSimpleValues.Null)
                ],
                value,
                [EndMap()]);
            AssertWellFormed([Tag(100)], value, []);
        }
    }

    [Test]
    public void EveryTokenKindHonorsItsExactAndOneByteShortBoundary()
    {
        BoundaryCase[] cases =
        [
            new BoundaryCase([], Unsigned(24), "1818"),
            new BoundaryCase([], Negative(-25), "3818"),
            new BoundaryCase([], Bytes([1, 2]), "420102"),
            new BoundaryCase([], Text("¢"), "62C2A2"),
            new BoundaryCase([], StartString(CborTokenKind.StartByteString), "5F"),
            new BoundaryCase(
                [StartString(CborTokenKind.StartByteString)],
                ByteChunk([1, 2]),
                "420102"),
            new BoundaryCase(
                [StartString(CborTokenKind.StartByteString)],
                EndByteString(),
                "FF"),
            new BoundaryCase([], StartString(CborTokenKind.StartTextString), "7F"),
            new BoundaryCase(
                [StartString(CborTokenKind.StartTextString)],
                TextChunk("¢"),
                "62C2A2"),
            new BoundaryCase(
                [StartString(CborTokenKind.StartTextString)],
                EndTextString(),
                "FF"),
            new BoundaryCase([], StartArray(24, CborLengthKind.Definite), "9818"),
            new BoundaryCase([], StartArray(0, CborLengthKind.Indefinite), "9F"),
            new BoundaryCase(
                [StartArray(0, CborLengthKind.Definite)],
                EndArray(),
                ""),
            new BoundaryCase(
                [StartArray(0, CborLengthKind.Indefinite)],
                EndArray(),
                "FF"),
            new BoundaryCase([], StartMap(24, CborLengthKind.Definite), "B818"),
            new BoundaryCase([], StartMap(0, CborLengthKind.Indefinite), "BF"),
            new BoundaryCase(
                [StartMap(0, CborLengthKind.Definite)],
                EndMap(),
                ""),
            new BoundaryCase(
                [StartMap(0, CborLengthKind.Indefinite)],
                EndMap(),
                "FF"),
            new BoundaryCase([], Tag(256), "D90100"),
            new BoundaryCase([], Simple(255), "F8FF"),
            new BoundaryCase([], Half(0x3e00), "F93E00")
        ];

        foreach (BoundaryCase item in cases)
        {
            CborTokenWriter writer = CreateWriterWithSetup(item.Setup);
            long bytesBefore = writer.Core.TotalBytes;
            int itemsBefore = writer.Core.TotalItems;
            int framesBefore = writer.Core.Frames.Count;
            byte[] expected = Convert.FromHexString(item.ExpectedHex);

            if (expected.Length > 0)
            {
                byte[] shortDestination = Enumerable.Repeat(
                    (byte)0xa5,
                    expected.Length - 1).ToArray();
                byte[] original = shortDestination.ToArray();
                CborWriteResult shortResult = writer.WriteToken(
                    item.Token,
                    shortDestination);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        shortResult.Status,
                        Is.EqualTo(CborWriteStatus.DestinationTooSmall));
                    Assert.That(shortResult.Required, Is.EqualTo(expected.Length));
                    Assert.That(shortDestination, Is.EqualTo(original));
                    Assert.That(writer.Core.TotalBytes, Is.EqualTo(bytesBefore));
                    Assert.That(writer.Core.TotalItems, Is.EqualTo(itemsBefore));
                    Assert.That(writer.Core.Frames, Has.Count.EqualTo(framesBefore));
                    Assert.That(writer.Core.Faulted, Is.False);
                });
            }

            byte[] exactDestination = Enumerable.Repeat(
                (byte)0xa5,
                expected.Length).ToArray();
            CborWriteResult exactResult = writer.WriteToken(
                item.Token,
                exactDestination);

            Assert.Multiple(() =>
            {
                Assert.That(exactResult.Status, Is.EqualTo(CborWriteStatus.Done));
                Assert.That(exactResult.Written, Is.EqualTo(expected.Length));
                Assert.That(exactResult.Required, Is.EqualTo(expected.Length));
                Assert.That(exactDestination, Is.EqualTo(expected));
            });
        }
    }

    [Test]
    public void EveryMismatchedEndPairFaultsWithoutWriting()
    {
        CborToken[] starts =
        [
            StartString(CborTokenKind.StartByteString),
            StartString(CborTokenKind.StartTextString),
            StartArray(0, CborLengthKind.Definite),
            StartMap(0, CborLengthKind.Definite)
        ];
        CborToken[] ends = [EndByteString(), EndTextString(), EndArray(), EndMap()];

        int startIndex = 0;
        while (startIndex < starts.Length)
        {
            int endIndex = 0;
            while (endIndex < ends.Length)
            {
                if (startIndex != endIndex)
                {
                    CborTokenWriter writer = CreateWriterWithSetup([starts[startIndex]]);
                    byte[] destination = [0xa5];

                    CborWriteResult result = writer.WriteToken(
                        ends[endIndex],
                        destination);
                    CborWriteResult afterFault = writer.WriteToken(
                        Unsigned(0),
                        destination);

                    Assert.Multiple(() =>
                    {
                        Assert.That(
                            result.ErrorCode,
                            Is.EqualTo("CBOR_MISMATCHED_END_TOKEN"));
                        Assert.That(result.Written, Is.Zero);
                        Assert.That(destination, Is.EqualTo(new byte[] { 0xa5 }));
                        Assert.That(writer.Core.Frames, Has.Count.EqualTo(1));
                        Assert.That(writer.Core.Faulted, Is.True);
                        Assert.That(
                            afterFault.ErrorCode,
                            Is.EqualTo("CBOR_WRITER_FAULTED"));
                    });
                }
                endIndex = endIndex + 1;
            }
            startIndex = startIndex + 1;
        }
    }

    [Test]
    public void StringFramesAcceptOnlyTheirChunksAndMatchingEnds()
    {
        CborToken[] nonStringItems =
        [
            Unsigned(0),
            Negative(-1),
            Bytes([]),
            Text(""),
            StartArray(0, CborLengthKind.Definite),
            StartMap(0, CborLengthKind.Definite),
            Tag(1),
            Simple(CborSimpleValues.False),
            Half(0x0000)
        ];

        foreach (CborToken item in nonStringItems)
        {
            AssertIllegalStringItem(
                CborTokenKind.StartByteString,
                item,
                "CBOR_INVALID_BYTE_STRING_CHUNK");
            AssertIllegalStringItem(
                CborTokenKind.StartTextString,
                item,
                "CBOR_INVALID_TEXT_STRING_CHUNK");
        }

        AssertIllegalStringItem(
            CborTokenKind.StartByteString,
            StartString(CborTokenKind.StartByteString),
            "CBOR_NESTED_INDEFINITE_BYTE_STRING");
        AssertIllegalStringItem(
            CborTokenKind.StartTextString,
            StartString(CborTokenKind.StartTextString),
            "CBOR_NESTED_INDEFINITE_TEXT_STRING");
        AssertIllegalStringItem(
            CborTokenKind.StartByteString,
            TextChunk("x"),
            "CBOR_INVALID_BYTE_STRING_CHUNK");
        AssertIllegalStringItem(
            CborTokenKind.StartTextString,
            ByteChunk([1]),
            "CBOR_INVALID_TEXT_STRING_CHUNK");

        AssertStringSequence(
            CborTokenKind.StartByteString,
            [ByteChunk([]), ByteChunk([1])],
            EndByteString());
        AssertStringSequence(
            CborTokenKind.StartTextString,
            [TextChunk(""), TextChunk("x")],
            EndTextString());
    }

    [Test]
    public void ChunksAreIllegalAtRootAndInsideEveryNonStringFrame()
    {
        CborToken[][] setups =
        [
            [],
            [StartArray(1, CborLengthKind.Definite)],
            [StartMap(1, CborLengthKind.Definite)],
            [Tag(1)]
        ];
        CborToken[] chunks = [ByteChunk([1]), TextChunk("x")];

        foreach (CborToken[] setup in setups)
        {
            foreach (CborToken chunk in chunks)
            {
                CborTokenWriter writer = CreateWriterWithSetup(setup);
                CborWriteResult result = writer.WriteToken(chunk, new byte[2]);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        result.ErrorCode,
                        Is.EqualTo("CBOR_UNEXPECTED_STRING_CHUNK"));
                    Assert.That(result.Written, Is.Zero);
                    Assert.That(writer.Core.Faulted, Is.True);
                });
            }
        }
    }

    [Test]
    public void EveryIncompleteStateFaultsAndResetRestoresInitialState()
    {
        IncompleteCase[] cases =
        [
            new IncompleteCase([Tag(1)], "CBOR_TAG_CONTENT_MISSING"),
            new IncompleteCase(
                [StartArray(1, CborLengthKind.Definite)],
                "CBOR_ARRAY_ITEM_COUNT_MISMATCH"),
            new IncompleteCase(
                [StartArray(0, CborLengthKind.Definite)],
                "CBOR_CONTAINER_END_MISSING"),
            new IncompleteCase(
                [StartArray(1, CborLengthKind.Definite), Unsigned(0)],
                "CBOR_CONTAINER_END_MISSING"),
            new IncompleteCase(
                [StartMap(1, CborLengthKind.Definite)],
                "CBOR_MAP_PAIR_COUNT_MISMATCH"),
            new IncompleteCase(
                [StartMap(1, CborLengthKind.Definite), Unsigned(0)],
                "CBOR_MAP_VALUE_MISSING"),
            new IncompleteCase(
                [
                    StartMap(1, CborLengthKind.Definite),
                    Unsigned(0),
                    Unsigned(1)
                ],
                "CBOR_CONTAINER_END_MISSING"),
            new IncompleteCase(
                [StartString(CborTokenKind.StartByteString)],
                "CBOR_INDEFINITE_END_MISSING"),
            new IncompleteCase(
                [StartString(CborTokenKind.StartTextString)],
                "CBOR_INDEFINITE_END_MISSING"),
            new IncompleteCase(
                [StartArray(0, CborLengthKind.Indefinite)],
                "CBOR_INDEFINITE_END_MISSING"),
            new IncompleteCase(
                [StartMap(0, CborLengthKind.Indefinite)],
                "CBOR_INDEFINITE_END_MISSING"),
            new IncompleteCase(
                [StartMap(0, CborLengthKind.Indefinite), Unsigned(0)],
                "CBOR_MAP_VALUE_MISSING")
        ];

        foreach (IncompleteCase item in cases)
        {
            CborTokenWriter writer = CreateWriterWithSetup(item.Setup);

            CborWriteResult incomplete = writer.CompleteWriter([]);
            CborWriteResult faulted = writer.WriteToken(Unsigned(0), new byte[1]);
            writer.ResetWriter();
            byte[] destination = [0xa5];
            CborWriteResult resetWrite = writer.WriteToken(Unsigned(0), destination);
            CborWriteResult resetComplete = writer.CompleteWriter([]);

            Assert.Multiple(() =>
            {
                Assert.That(incomplete.Status, Is.EqualTo(CborWriteStatus.Error));
                Assert.That(incomplete.ErrorCode, Is.EqualTo(item.ErrorCode));
                Assert.That(faulted.ErrorCode, Is.EqualTo("CBOR_WRITER_FAULTED"));
                Assert.That(resetWrite.Status, Is.EqualTo(CborWriteStatus.Done));
                Assert.That(destination, Is.EqualTo(new byte[] { 0x00 }));
                Assert.That(resetComplete.Status, Is.EqualTo(CborWriteStatus.Done));
                Assert.That(writer.Core.TotalBytes, Is.EqualTo(1));
                Assert.That(writer.Core.TotalItems, Is.EqualTo(1));
                Assert.That(writer.Core.Frames, Is.Empty);
                Assert.That(writer.Core.Faulted, Is.False);
            });
        }
    }

    private static void AssertWellFormed(
        CborToken[] prefix,
        CborToken[] value,
        CborToken[] suffix)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        WriteAll(writer, output, prefix);
        WriteAll(writer, output, value);
        WriteAll(writer, output, suffix);

        CborWriteResult complete = writer.CompleteWriter([]);
        CborReadResult decoded = new CborDecoder().Decode(
            output.WrittenSpan.ToArray(),
            new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.Faulted, Is.False);
        });
    }

    private static void WriteAll(
        CborTokenWriter writer,
        IBufferWriter<byte> output,
        CborToken[] tokens)
    {
        foreach (CborToken token in tokens)
        {
            CborWriteResult result = writer.WriteTokenToBuffer(token, output);
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Done), result.ErrorMessage);
        }
    }

    private static CborTokenWriter CreateWriterWithSetup(CborToken[] setup)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        foreach (CborToken token in setup)
        {
            CborWriteResult result = writer.WriteToken(token, new byte[16]);
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Done), result.ErrorMessage);
        }
        return writer;
    }

    private static void AssertIllegalStringItem(
        CborTokenKind startKind,
        CborToken token,
        string errorCode)
    {
        CborTokenWriter writer = CreateWriterWithSetup([StartString(startKind)]);
        byte[] destination = Enumerable.Repeat((byte)0xa5, 16).ToArray();
        byte[] original = destination.ToArray();

        CborWriteResult result = writer.WriteToken(token, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.Written, Is.Zero);
            Assert.That(destination, Is.EqualTo(original));
            Assert.That(writer.Core.Frames, Has.Count.EqualTo(1));
            Assert.That(writer.Core.Faulted, Is.True);
        });
    }

    private static void AssertStringSequence(
        CborTokenKind startKind,
        CborToken[] chunks,
        CborToken end)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        CborWriteResult startResult = writer.WriteTokenToBuffer(
            StartString(startKind),
            output);
        WriteAll(writer, output, chunks);
        CborWriteResult endResult = writer.WriteTokenToBuffer(end, output);

        Assert.Multiple(() =>
        {
            Assert.That(startResult.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(endResult.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(writer.CompleteWriter([]).Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(writer.Core.Frames, Is.Empty);
        });
    }

    private static CborToken[][] ValueSequences()
    {
        return
        [
            [Unsigned(0)],
            [Negative(-1)],
            [Bytes([1])],
            [Text("x")],
            [Simple(CborSimpleValues.True)],
            [Half(0x3e00)],
            [Tag(1), Unsigned(0)],
            [
                StartString(CborTokenKind.StartByteString),
                ByteChunk([1]),
                EndByteString()
            ],
            [
                StartString(CborTokenKind.StartTextString),
                TextChunk("x"),
                EndTextString()
            ],
            [StartArray(0, CborLengthKind.Definite), EndArray()],
            [StartArray(0, CborLengthKind.Indefinite), EndArray()],
            [StartMap(0, CborLengthKind.Definite), EndMap()],
            [StartMap(0, CborLengthKind.Indefinite), EndMap()]
        ];
    }

    private static CborToken StartString(CborTokenKind kind)
    {
        return new CborToken
        {
            Kind = kind,
            LengthKind = CborLengthKind.Indefinite
        };
    }

    private static CborToken StartArray(ulong length, CborLengthKind lengthKind)
    {
        return new CborToken
        {
            Kind = CborTokenKind.StartArray,
            LengthKind = lengthKind,
            Length = length
        };
    }

    private static CborToken StartMap(ulong length, CborLengthKind lengthKind)
    {
        return new CborToken
        {
            Kind = CborTokenKind.StartMap,
            LengthKind = lengthKind,
            Length = length
        };
    }

    private static CborToken EndByteString()
    {
        return new CborToken { Kind = CborTokenKind.EndByteString };
    }

    private static CborToken EndTextString()
    {
        return new CborToken { Kind = CborTokenKind.EndTextString };
    }

    private static CborToken EndArray()
    {
        return new CborToken { Kind = CborTokenKind.EndArray };
    }

    private static CborToken EndMap()
    {
        return new CborToken { Kind = CborTokenKind.EndMap };
    }

    private static CborToken Unsigned(int value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborToken Negative(int value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.NegativeInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborToken Bytes(byte[] value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.ByteString,
            LengthKind = CborLengthKind.Definite,
            Length = (ulong)value.Length,
            ByteStringValue = value
        };
    }

    private static CborToken Text(string value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.TextString,
            LengthKind = CborLengthKind.Definite,
            Length = (ulong)Encoding.UTF8.GetByteCount(value),
            TextStringValue = value
        };
    }

    private static CborToken ByteChunk(byte[] value)
    {
        CborToken token = Bytes(value);
        token.Kind = CborTokenKind.ByteStringChunk;
        return token;
    }

    private static CborToken TextChunk(string value)
    {
        CborToken token = Text(value);
        token.Kind = CborTokenKind.TextStringChunk;
        return token;
    }

    private static CborToken Tag(ulong value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = value
        };
    }

    private static CborToken Simple(byte value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.SimpleValue,
            SimpleValue = value
        };
    }

    private static CborToken Half(ushort bits)
    {
        return new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = CborFloatWidth.Half,
            FloatValue = CborHalfPrecision.CreateHalfFloat(bits)
        };
    }

    private class BoundaryCase
    {
        public CborToken[] Setup;
        public CborToken Token;
        public string ExpectedHex;

        public BoundaryCase(CborToken[] setup, CborToken token, string expectedHex)
        {
            Setup = setup;
            Token = token;
            ExpectedHex = expectedHex;
        }
    }

    private class IncompleteCase
    {
        public CborToken[] Setup;
        public string ErrorCode;

        public IncompleteCase(CborToken[] setup, string errorCode)
        {
            Setup = setup;
            ErrorCode = errorCode;
        }
    }
}
