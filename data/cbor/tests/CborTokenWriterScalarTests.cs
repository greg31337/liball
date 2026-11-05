using System.Buffers;
using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenWriterScalarTests
{
    [Test]
    public void IntegerBoundariesUseTheShortestWellFormedHeads()
    {
        string[][] unsignedCases =
        [
            ["0", "00"],
            ["23", "17"],
            ["24", "1818"],
            ["255", "18ff"],
            ["256", "190100"],
            ["65535", "19ffff"],
            ["65536", "1a00010000"],
            ["4294967295", "1affffffff"],
            ["4294967296", "1b0000000100000000"],
            ["18446744073709551615", "1bffffffffffffffff"]
        ];
        string[][] negativeCases =
        [
            ["-1", "20"],
            ["-24", "37"],
            ["-25", "3818"],
            ["-256", "38ff"],
            ["-257", "390100"],
            ["-65536", "39ffff"],
            ["-65537", "3a00010000"],
            ["-4294967296", "3affffffff"],
            ["-4294967297", "3b0000000100000000"],
            ["-18446744073709551616", "3bffffffffffffffff"]
        ];

        foreach (string[] item in unsignedCases)
        {
            CborToken token = new CborToken
            {
                Kind = CborTokenKind.UnsignedInteger,
                IntegerValue = BigInteger.Parse(item[0])
            };
            AssertEncoding(token, item[1]);
        }
        foreach (string[] item in negativeCases)
        {
            CborToken token = new CborToken
            {
                Kind = CborTokenKind.NegativeInteger,
                IntegerValue = BigInteger.Parse(item[0])
            };
            AssertEncoding(token, item[1]);
        }
    }

    [Test]
    public void ByteTextSimpleAndFloatPayloadsPreserveTheirExactValues()
    {
        CborToken bytes = new CborToken
        {
            Kind = CborTokenKind.ByteString,
            LengthKind = CborLengthKind.Definite,
            Length = 4,
            ByteStringValue = [0x01, 0x02, 0x03, 0x04]
        };
        CborToken text = new CborToken
        {
            Kind = CborTokenKind.TextString,
            LengthKind = CborLengthKind.Definite,
            Length = 10,
            TextStringValue = "a¢€𐅑"
        };
        CborToken inlineSimple = new CborToken
        {
            Kind = CborTokenKind.SimpleValue,
            SimpleValue = 16
        };
        CborToken extendedSimple = new CborToken
        {
            Kind = CborTokenKind.SimpleValue,
            SimpleValue = 255
        };
        CborToken half = CreateRetainedFloat(
            CborFloatWidth.Half,
            CborHalfPrecision.CreateHalfFloat(0x7e55));
        CborToken single = CreateRetainedFloat(
            CborFloatWidth.Single,
            CborHalfPrecision.CreateSingleFloat(0x7fa12345));
        CborToken doubleToken = CreateRetainedFloat(
            CborFloatWidth.Double,
            CborHalfPrecision.CreateDoubleFloat(0xfff8123456789abc));

        AssertEncoding(bytes, "4401020304");
        AssertEncoding(text, "6a61c2a2e282acf0908591");
        AssertEncoding(inlineSimple, "f0");
        AssertEncoding(extendedSimple, "f8ff");
        AssertEncoding(half, "f97e55");
        AssertEncoding(single, "fa7fa12345");
        AssertEncoding(doubleToken, "fbfff8123456789abc");
    }

    [Test]
    public void NumericFloatValuesConvertToTheExplicitRequestedWidth()
    {
        CborFloatValue numeric = new CborFloatValue { Value = 1.5 };
        CborToken half = CreateRetainedFloat(CborFloatWidth.Half, numeric);
        CborToken single = CreateRetainedFloat(CborFloatWidth.Single, numeric);
        CborToken doubleToken = CreateRetainedFloat(CborFloatWidth.Double, numeric);

        AssertEncoding(half, "f93e00");
        AssertEncoding(single, "fa3fc00000");
        AssertEncoding(doubleToken, "fb3ff8000000000000");
    }

    [Test]
    public void EveryUndersizedSpanLeavesDestinationAndWriterStateUnchanged()
    {
        CborToken[] tokens =
        [
            new CborToken
            {
                Kind = CborTokenKind.UnsignedInteger,
                IntegerValue = new BigInteger(24)
            },
            new CborToken
            {
                Kind = CborTokenKind.ByteString,
                LengthKind = CborLengthKind.Definite,
                Length = 3,
                ByteStringValue = [0x01, 0x02, 0x03]
            },
            new CborToken
            {
                Kind = CborTokenKind.TextString,
                LengthKind = CborLengthKind.Definite,
                Length = 3,
                TextStringValue = "€"
            },
            CreateRetainedFloat(
                CborFloatWidth.Double,
                CborHalfPrecision.CreateDoubleFloat(0x3ff8000000000000))
        ];

        foreach (CborToken token in tokens)
        {
            CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
            byte[] measurement = new byte[32];
            CborWriteResult measured = writer.WriteToken(token, measurement);
            writer.ResetWriter();
            byte[] destination = Enumerable.Repeat((byte)0xa5, measured.Required - 1).ToArray();
            byte[] original = destination.ToArray();

            CborWriteResult failure = writer.WriteTokenToSpan(token, destination.AsSpan());

            Assert.Multiple(() =>
            {
                Assert.That(failure.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
                Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_DESTINATION_TOO_SMALL"));
                Assert.That(failure.Written, Is.Zero);
                Assert.That(failure.Required, Is.EqualTo(measured.Required));
                Assert.That(destination, Is.EqualTo(original));
                Assert.That(writer.Core.TotalBytes, Is.Zero);
                Assert.That(writer.Core.TotalItems, Is.Zero);
            });
        }
    }

    [Test]
    public void BufferWriterOutputAppendsTokensAndAdvancesOnlyAfterSuccess()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> destination = new ArrayBufferWriter<byte>();
        CborToken integer = new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = new BigInteger(1000)
        };
        CborToken text = new CborToken
        {
            Kind = CborTokenKind.TextString,
            LengthKind = CborLengthKind.Definite,
            Length = 1,
            TextStringValue = "x"
        };

        CborWriteResult first = writer.WriteTokenToBuffer(integer, destination);
        CborWriteResult second = writer.WriteTokenToBuffer(text, destination);

        ConstrainedBufferWriter constrained = new ConstrainedBufferWriter(2, 0xa5);
        CborTokenWriter constrainedWriter = new CborTokenWriter(new CborEncodeOptions());
        CborWriteResult rejected = constrainedWriter.WriteTokenToBuffer(integer, constrained);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(second.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(destination.WrittenSpan.ToArray(), Is.EqualTo(Convert.FromHexString("1903e86178")));
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(5));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(2));
            Assert.That(rejected.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(rejected.Required, Is.EqualTo(3));
            Assert.That(constrained.Advanced, Is.Zero);
            Assert.That(constrained.Storage, Is.EqualTo(new byte[] { 0xa5, 0xa5 }));
            Assert.That(constrainedWriter.Core.TotalBytes, Is.Zero);
            Assert.That(constrainedWriter.Core.TotalItems, Is.Zero);
        });
    }

    [Test]
    public void InvalidScalarTokensReturnStableErrorsWithoutWriting()
    {
        CborToken[][] cases =
        [
            [
                new CborToken
                {
                    Kind = CborTokenKind.UnsignedInteger,
                    IntegerValue = new BigInteger(-1)
                },
                ErrorToken("CBOR_UNSIGNED_INTEGER_OUT_OF_RANGE")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.NegativeInteger,
                    IntegerValue = BigInteger.Zero
                },
                ErrorToken("CBOR_NEGATIVE_INTEGER_OUT_OF_RANGE")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.ByteString,
                    LengthKind = CborLengthKind.Definite,
                    Length = 2,
                    ByteStringValue = [0x00]
                },
                ErrorToken("CBOR_BYTE_STRING_LENGTH_MISMATCH")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.TextString,
                    LengthKind = CborLengthKind.Definite,
                    Length = 1,
                    TextStringValue = "\ud800"
                },
                ErrorToken("CBOR_TEXT_STRING_INVALID_UTF16")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.SimpleValue,
                    SimpleValue = 24
                },
                ErrorToken("CBOR_SIMPLE_VALUE_RESERVED")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.Float,
                    FloatWidth = CborFloatWidth.Half
                },
                ErrorToken("CBOR_FLOAT_VALUE_MISSING")
            ],
            [
                new CborToken
                {
                    Kind = CborTokenKind.Float,
                    FloatWidth = CborFloatWidth.Single,
                    FloatValue = CborHalfPrecision.CreateHalfFloat(0x3c00)
                },
                ErrorToken("CBOR_FLOAT_WIDTH_MISMATCH")
            ]
        ];

        foreach (CborToken[] item in cases)
        {
            CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
            byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5];

            CborWriteResult result = writer.WriteToken(item[0], destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo(item[1].TextStringValue));
                Assert.That(result.Written, Is.Zero);
                Assert.That(result.ErrorPath, Is.EqualTo("$"));
                Assert.That(destination, Is.EqualTo(new byte[] { 0xa5, 0xa5, 0xa5, 0xa5 }));
                Assert.That(writer.Core.TotalBytes, Is.Zero);
                Assert.That(writer.Core.TotalItems, Is.Zero);
            });
        }
    }

    [Test]
    public void OutputLimitsAndResetApplyOnlyToCommittedTokens()
    {
        CborToken twoByteToken = new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = new BigInteger(24)
        };
        CborToken oneByteToken = new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = BigInteger.Zero
        };
        CborTokenWriter byteWriter = new CborTokenWriter(
            new CborEncodeOptions { MaxOutputBytes = 1 });
        CborWriteResult byteFailure = byteWriter.WriteToken(twoByteToken, new byte[2]);
        CborWriteResult byteSuccess = byteWriter.WriteToken(oneByteToken, new byte[1]);

        CborTokenWriter itemWriter = new CborTokenWriter(
            new CborEncodeOptions { MaxTotalItems = 1 });
        CborWriteResult itemSuccess = itemWriter.WriteToken(oneByteToken, new byte[1]);
        CborWriteResult itemFailure = itemWriter.WriteToken(oneByteToken, new byte[1]);
        itemWriter.ResetWriter();
        CborWriteResult afterReset = itemWriter.WriteToken(oneByteToken, new byte[1]);
        CborWriteResult completed = itemWriter.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(byteFailure.ErrorCode, Is.EqualTo("CBOR_MAX_OUTPUT_BYTES"));
            Assert.That(byteSuccess.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(byteWriter.Core.TotalBytes, Is.EqualTo(1));
            Assert.That(byteWriter.Core.TotalItems, Is.EqualTo(1));
            Assert.That(itemSuccess.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(itemFailure.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(afterReset.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(itemWriter.Core.TotalItems, Is.EqualTo(1));
            Assert.That(completed.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(completed.Written, Is.Zero);
            Assert.That(completed.Required, Is.Zero);
        });
    }

    private static void AssertEncoding(CborToken token, string expectedHex)
    {
        byte[] expected = Convert.FromHexString(expectedHex);
        byte[] destination = new byte[expected.Length];
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());

        CborWriteResult result = writer.WriteTokenToSpan(token, destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(result.Written, Is.EqualTo(expected.Length));
            Assert.That(result.Required, Is.EqualTo(expected.Length));
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(expected.Length));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(1));
        });
    }

    private static CborToken CreateRetainedFloat(
        CborFloatWidth width,
        CborFloatValue value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = width,
            FloatValue = value
        };
    }

    private static CborToken ErrorToken(string errorCode)
    {
        return new CborToken
        {
            Kind = CborTokenKind.TextString,
            TextStringValue = errorCode
        };
    }

    private class ConstrainedBufferWriter : IBufferWriter<byte>
    {
        public byte[] Storage;
        public int Advanced;

        public ConstrainedBufferWriter(int capacity, byte fill)
        {
            Storage = Enumerable.Repeat(fill, capacity).ToArray();
        }

        public void Advance(int count)
        {
            Advanced = Advanced + count;
        }

        public Memory<byte> GetMemory(int sizeHint)
        {
            return Storage;
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            return Storage;
        }
    }
}
