using System.Buffers;
using System.Numerics;
using System.Text;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenWriterIndefiniteTests
{
    [Test]
    public void NestedIndefiniteContainersAndStringsProduceWellFormedOutput()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        CborToken[] tokens =
        [
            Start(CborTokenKind.StartArray),
            Start(CborTokenKind.StartByteString),
            ByteChunk([1, 2]),
            ByteChunk([3]),
            End(CborTokenKind.EndByteString),
            Start(CborTokenKind.StartTextString),
            TextChunk("¢"),
            TextChunk("€"),
            End(CborTokenKind.EndTextString),
            Start(CborTokenKind.StartMap),
            Unsigned(1),
            Start(CborTokenKind.StartArray),
            Unsigned(2),
            End(CborTokenKind.EndArray),
            End(CborTokenKind.EndMap),
            End(CborTokenKind.EndArray)
        ];
        foreach (CborToken token in tokens)
        {
            Write(writer, output, token);
        }

        CborWriteResult completed = writer.CompleteWriter([]);
        byte[] encoded = output.WrittenSpan.ToArray();
        CborReadResult decoded = new CborDecoder().Decode(encoded, new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(completed.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString(
                "9F5F4201024103FF7F62C2A263E282ACFFBF019F02FFFFFF")));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(7));
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(encoded.Length));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Value?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(decoded.Value?.ArrayItems?[0].ByteStringValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(decoded.Value?.ArrayItems?[1].TextStringValue, Is.EqualTo("¢€"));
            Assert.That(
                decoded.Value?.ArrayItems?[2].MapEntries?[0].Value?.ArrayItems?[0].IntegerValue,
                Is.EqualTo(new BigInteger(2)));
        });
    }

    [Test]
    public void EmptyIndefiniteValuesWriteHeadsAndBreaks()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        CborToken[] tokens =
        [
            Start(CborTokenKind.StartByteString), End(CborTokenKind.EndByteString),
            Start(CborTokenKind.StartTextString), End(CborTokenKind.EndTextString),
            Start(CborTokenKind.StartArray), End(CborTokenKind.EndArray),
            Start(CborTokenKind.StartMap), End(CborTokenKind.EndMap)
        ];
        foreach (CborToken token in tokens)
        {
            Assert.That(Write(writer, output, token).Written, Is.EqualTo(1));
        }

        Assert.Multiple(() =>
        {
            Assert.That(writer.CompleteWriter([]).Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(Convert.FromHexString("5FFF7FFF9FFFBFFF")));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(4));
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(8));
        });
    }

    [TestCase(CborTokenKind.StartByteString)]
    [TestCase(CborTokenKind.StartTextString)]
    [TestCase(CborTokenKind.StartArray)]
    [TestCase(CborTokenKind.StartMap)]
    public void DisabledIndefiniteLengthsRejectStartsAtomically(CborTokenKind kind)
    {
        CborTokenWriter writer = new CborTokenWriter(
            new CborEncodeOptions { AllowIndefiniteLength = false });
        byte[] destination = [0xa5];

        CborWriteResult result = writer.WriteToken(Start(kind), destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_INDEFINITE_LENGTH_DISABLED"));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xa5 }));
            Assert.That(writer.Core.TotalBytes, Is.Zero);
            Assert.That(writer.Core.TotalItems, Is.Zero);
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.Faulted, Is.False);
        });
    }

    [Test]
    public void StringChunksMustAppearInsideTheMatchingString()
    {
        AssertFaults(null, ByteChunk([1]), "CBOR_UNEXPECTED_STRING_CHUNK");
        AssertFaults(Start(CborTokenKind.StartByteString), TextChunk("x"), "CBOR_INVALID_BYTE_STRING_CHUNK");
        AssertFaults(Start(CborTokenKind.StartTextString), ByteChunk([1]), "CBOR_INVALID_TEXT_STRING_CHUNK");
        AssertFaults(
            Start(CborTokenKind.StartByteString),
            Start(CborTokenKind.StartByteString),
            "CBOR_NESTED_INDEFINITE_BYTE_STRING");
        AssertFaults(
            Start(CborTokenKind.StartTextString),
            Start(CborTokenKind.StartTextString),
            "CBOR_NESTED_INDEFINITE_TEXT_STRING");
    }

    [Test]
    public void IndefiniteMapRejectsBreakAfterKeyAndResetRestoresWriter()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        Write(writer, output, Start(CborTokenKind.StartMap));
        Write(writer, output, Unsigned(0));
        int bytesBeforeEnd = output.WrittenCount;

        CborWriteResult invalid = writer.WriteTokenToBuffer(End(CborTokenKind.EndMap), output);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_MAP_VALUE_MISSING"));
            Assert.That(output.WrittenCount, Is.EqualTo(bytesBeforeEnd));
            Assert.That(writer.Core.Faulted, Is.True);
        });

        writer.ResetWriter();
        ArrayBufferWriter<byte> resetOutput = new ArrayBufferWriter<byte>();
        Write(writer, resetOutput, Start(CborTokenKind.StartMap));
        Write(writer, resetOutput, End(CborTokenKind.EndMap));
        Assert.That(writer.CompleteWriter([]).Status, Is.EqualTo(CborWriteStatus.Done));
        Assert.That(resetOutput.WrittenSpan.ToArray(), Is.EqualTo(Convert.FromHexString("BFFF")));
    }

    [TestCase(CborLengthKind.Indefinite, CborLengthKind.Definite)]
    [TestCase(CborLengthKind.Definite, CborLengthKind.Indefinite)]
    public void ExplicitEndLengthKindMustMatchOpenFrame(
        CborLengthKind startLengthKind,
        CborLengthKind endLengthKind)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        CborToken start = new CborToken
        {
            Kind = CborTokenKind.StartArray,
            LengthKind = startLengthKind,
            Length = 0
        };
        Write(writer, output, start);
        CborToken end = End(CborTokenKind.EndArray);
        end.LengthKind = endLengthKind;
        int bytesBeforeEnd = output.WrittenCount;

        CborWriteResult result = writer.WriteTokenToBuffer(end, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_CONTAINER_LENGTH_KIND_MISMATCH"));
            Assert.That(output.WrittenCount, Is.EqualTo(bytesBeforeEnd));
            Assert.That(writer.Core.Frames, Has.Count.EqualTo(1));
            Assert.That(writer.Core.Faulted, Is.True);
        });
    }

    [Test]
    public void BreakDestinationFailureDoesNotCloseFrameAndCanBeRetried()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        writer.WriteToken(Start(CborTokenKind.StartArray), new byte[1]);

        CborWriteResult tooSmall = writer.WriteToken(End(CborTokenKind.EndArray), []);

        Assert.Multiple(() =>
        {
            Assert.That(tooSmall.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(tooSmall.Required, Is.EqualTo(1));
            Assert.That(writer.Core.Frames, Has.Count.EqualTo(1));
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(1));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(1));
            Assert.That(writer.Core.Faulted, Is.False);
        });

        byte[] destination = [0xa5];
        CborWriteResult retried = writer.WriteToken(End(CborTokenKind.EndArray), destination);

        Assert.Multiple(() =>
        {
            Assert.That(retried.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xff }));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(2));
            Assert.That(writer.CompleteWriter([]).Status, Is.EqualTo(CborWriteStatus.Done));
        });
    }

    [TestCase(CborTokenKind.StartByteString)]
    [TestCase(CborTokenKind.StartTextString)]
    [TestCase(CborTokenKind.StartArray)]
    [TestCase(CborTokenKind.StartMap)]
    public void CompletionRequiresEveryIndefiniteValueToBeClosed(CborTokenKind kind)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        writer.WriteToken(Start(kind), new byte[1]);

        CborWriteResult result = writer.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_INDEFINITE_END_MISSING"));
            Assert.That(writer.Core.Faulted, Is.True);
        });
    }

    [Test]
    public void IndefiniteChildCountsOnceTowardDefiniteParent()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        Write(writer, output, new CborToken
        {
            Kind = CborTokenKind.StartArray,
            LengthKind = CborLengthKind.Definite,
            Length = 1
        });
        Write(writer, output, Start(CborTokenKind.StartArray));
        Write(writer, output, End(CborTokenKind.EndArray));
        Write(writer, output, End(CborTokenKind.EndArray));

        Assert.That(writer.CompleteWriter([]).Status, Is.EqualTo(CborWriteStatus.Done));
        Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(Convert.FromHexString("819FFF")));
        Assert.That(writer.Core.TotalItems, Is.EqualTo(2));
    }

    private static CborWriteResult Write(
        CborTokenWriter writer,
        IBufferWriter<byte> output,
        CborToken token)
    {
        CborWriteResult result = writer.WriteTokenToBuffer(token, output);
        Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Done), result.ErrorMessage);
        return result;
    }

    private static CborToken Start(CborTokenKind kind)
    {
        return new CborToken { Kind = kind, LengthKind = CborLengthKind.Indefinite };
    }

    private static CborToken End(CborTokenKind kind)
    {
        return new CborToken { Kind = kind };
    }

    private static CborToken ByteChunk(byte[] bytes)
    {
        return new CborToken
        {
            Kind = CborTokenKind.ByteStringChunk,
            LengthKind = CborLengthKind.Definite,
            ByteStringValue = bytes,
            Length = (ulong)bytes.Length
        };
    }

    private static CborToken TextChunk(string text)
    {
        return new CborToken
        {
            Kind = CborTokenKind.TextStringChunk,
            LengthKind = CborLengthKind.Definite,
            TextStringValue = text,
            Length = (ulong)Encoding.UTF8.GetByteCount(text)
        };
    }

    private static CborToken Unsigned(int value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static void AssertFaults(CborToken? start, CborToken invalid, string expectedCode)
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        if (start != null)
        {
            Write(writer, output, start);
        }
        int bytesBeforeInvalid = output.WrittenCount;

        CborWriteResult result = writer.WriteTokenToBuffer(invalid, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(output.WrittenCount, Is.EqualTo(bytesBeforeInvalid));
            Assert.That(writer.Core.Faulted, Is.True);
        });
    }
}
