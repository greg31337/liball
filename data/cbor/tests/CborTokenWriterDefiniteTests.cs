using System.Buffers;
using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenWriterDefiniteTests
{
    [Test]
    public void NestedDefiniteArraysMapsAndTagsProduceOneWellFormedValue()
    {
        CborToken[] tokens =
        [
            StartArray(3),
            Unsigned(1),
            StartMap(1),
            Text("a"),
            StartArray(2),
            Negative(-1),
            Simple(CborSimpleValues.True),
            EndArray(),
            EndMap(),
            Tag(1),
            Unsigned(2),
            EndArray()
        ];
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();

        foreach (CborToken token in tokens)
        {
            CborWriteResult result = writer.WriteTokenToBuffer(token, output);
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Done), result.ErrorMessage);
        }
        CborWriteResult completion = writer.CompleteWriter([]);
        byte[] encoded = output.WrittenSpan.ToArray();
        CborReadResult decoded = new CborDecoder().Decode(encoded, new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString("8301a161618220f5c102")));
            Assert.That(completion.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(completion.Written, Is.Zero);
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Value?.ArrayItems, Has.Count.EqualTo(3));
            Assert.That(decoded.Value?.ArrayItems?[1].MapEntries, Has.Count.EqualTo(1));
            Assert.That(decoded.Value?.ArrayItems?[2].TagNumber, Is.EqualTo(1));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(encoded.Length));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(9));
            Assert.That(writer.Core.Faulted, Is.False);
        });
    }

    [Test]
    public void EmptyContainersCountAsSingleParentValuesAndEndsWriteNoBytes()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        byte[] destination = [0xa5];

        CborWriteResult root = writer.WriteToken(StartArray(2), destination);
        CborWriteResult array = writer.WriteToken(StartArray(0), destination);
        CborWriteResult arrayEnd = writer.WriteToken(EndArray(), destination);
        CborWriteResult map = writer.WriteToken(StartMap(0), destination);
        CborWriteResult mapEnd = writer.WriteToken(EndMap(), destination);
        CborWriteResult rootEnd = writer.WriteToken(EndArray(), destination);
        CborWriteResult complete = writer.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(root.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(array.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(arrayEnd.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(arrayEnd.Written, Is.Zero);
            Assert.That(arrayEnd.Required, Is.Zero);
            Assert.That(map.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(mapEnd.Written, Is.Zero);
            Assert.That(rootEnd.Written, Is.Zero);
            Assert.That(complete.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(3));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(3));
        });
    }

    [Test]
    public void DestinationFailureOnContainerStartCanBeRetriedWithoutStateChanges()
    {
        CborTokenWriter writer = new CborTokenWriter(new CborEncodeOptions());
        byte[] empty = [];

        CborWriteResult tooSmall = writer.WriteTokenToSpan(StartArray(1), empty.AsSpan());
        byte[] startDestination = [0xa5];
        CborWriteResult retried = writer.WriteTokenToSpan(
            StartArray(1),
            startDestination.AsSpan());
        CborWriteResult item = writer.WriteToken(Unsigned(0), new byte[1]);
        CborWriteResult end = writer.WriteToken(EndArray(), []);

        Assert.Multiple(() =>
        {
            Assert.That(tooSmall.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(tooSmall.Required, Is.EqualTo(1));
            Assert.That(retried.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(startDestination, Is.EqualTo(new byte[] { 0x81 }));
            Assert.That(item.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(end.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(writer.Core.Frames, Is.Empty);
            Assert.That(writer.Core.TotalBytes, Is.EqualTo(2));
            Assert.That(writer.Core.TotalItems, Is.EqualTo(2));
            Assert.That(writer.Core.Faulted, Is.False);
        });
    }

    [Test]
    public void EarlyAndExcessArrayItemsFaultTheWriterUntilReset()
    {
        CborTokenWriter earlyWriter = new CborTokenWriter(new CborEncodeOptions());
        earlyWriter.WriteToken(StartArray(2), new byte[1]);
        earlyWriter.WriteToken(Unsigned(0), new byte[1]);
        CborWriteResult earlyEnd = earlyWriter.WriteToken(EndArray(), []);
        CborWriteResult afterEarlyEnd = earlyWriter.WriteToken(Unsigned(1), new byte[1]);

        CborTokenWriter excessWriter = new CborTokenWriter(new CborEncodeOptions());
        excessWriter.WriteToken(StartArray(1), new byte[1]);
        excessWriter.WriteToken(Unsigned(0), new byte[1]);
        byte[] untouched = [0xa5];
        CborWriteResult excess = excessWriter.WriteToken(Unsigned(1), untouched);

        earlyWriter.ResetWriter();
        CborWriteResult afterReset = earlyWriter.WriteToken(Unsigned(2), new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(earlyEnd.ErrorCode, Is.EqualTo("CBOR_ARRAY_ITEM_COUNT_MISMATCH"));
            Assert.That(earlyEnd.Written, Is.Zero);
            Assert.That(afterEarlyEnd.ErrorCode, Is.EqualTo("CBOR_WRITER_FAULTED"));
            Assert.That(excess.ErrorCode, Is.EqualTo("CBOR_ARRAY_ITEM_COUNT_EXCEEDED"));
            Assert.That(excess.Written, Is.Zero);
            Assert.That(untouched, Is.EqualTo(new byte[] { 0xa5 }));
            Assert.That(excessWriter.Core.TotalBytes, Is.EqualTo(2));
            Assert.That(excessWriter.Core.TotalItems, Is.EqualTo(2));
            Assert.That(excessWriter.Core.Frames, Has.Count.EqualTo(1));
            Assert.That(excessWriter.Core.Faulted, Is.True);
            Assert.That(afterReset.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(earlyWriter.Core.Frames, Is.Empty);
            Assert.That(earlyWriter.Core.TotalItems, Is.EqualTo(1));
            Assert.That(earlyWriter.Core.Faulted, Is.False);
        });
    }

    [Test]
    public void MapsRejectMissingValuesEarlyEndsAndExcessPairs()
    {
        CborTokenWriter missingValueWriter = new CborTokenWriter(new CborEncodeOptions());
        missingValueWriter.WriteToken(StartMap(1), new byte[1]);
        missingValueWriter.WriteToken(Unsigned(0), new byte[1]);
        CborWriteResult missingValue = missingValueWriter.WriteToken(EndMap(), []);

        CborTokenWriter earlyWriter = new CborTokenWriter(new CborEncodeOptions());
        earlyWriter.WriteToken(StartMap(1), new byte[1]);
        CborWriteResult earlyEnd = earlyWriter.WriteToken(EndMap(), []);

        CborTokenWriter excessWriter = new CborTokenWriter(new CborEncodeOptions());
        excessWriter.WriteToken(StartMap(1), new byte[1]);
        excessWriter.WriteToken(Unsigned(0), new byte[1]);
        excessWriter.WriteToken(Unsigned(1), new byte[1]);
        CborWriteResult excess = excessWriter.WriteToken(Unsigned(2), new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(missingValue.ErrorCode, Is.EqualTo("CBOR_MAP_VALUE_MISSING"));
            Assert.That(missingValueWriter.Core.Faulted, Is.True);
            Assert.That(earlyEnd.ErrorCode, Is.EqualTo("CBOR_MAP_PAIR_COUNT_MISMATCH"));
            Assert.That(earlyWriter.Core.Faulted, Is.True);
            Assert.That(excess.ErrorCode, Is.EqualTo("CBOR_MAP_PAIR_COUNT_EXCEEDED"));
            Assert.That(excessWriter.Core.Faulted, Is.True);
            Assert.That(excessWriter.Core.TotalItems, Is.EqualTo(3));
        });
    }

    [Test]
    public void TagsRequireExactlyOneCompleteContentValue()
    {
        CborTokenWriter missingWriter = new CborTokenWriter(new CborEncodeOptions());
        CborWriteResult tag = missingWriter.WriteToken(Tag(1000), new byte[3]);
        CborWriteResult missing = missingWriter.CompleteWriter([]);
        CborWriteResult faulted = missingWriter.WriteToken(Unsigned(0), new byte[1]);

        CborTokenWriter nestedWriter = new CborTokenWriter(new CborEncodeOptions());
        ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
        CborWriteResult outerTag = nestedWriter.WriteTokenToBuffer(Tag(1), output);
        CborWriteResult innerTag = nestedWriter.WriteTokenToBuffer(Tag(2), output);
        CborWriteResult content = nestedWriter.WriteTokenToBuffer(StartArray(0), output);
        CborWriteResult contentEnd = nestedWriter.WriteTokenToBuffer(EndArray(), output);
        CborWriteResult complete = nestedWriter.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(missing.ErrorCode, Is.EqualTo("CBOR_TAG_CONTENT_MISSING"));
            Assert.That(faulted.ErrorCode, Is.EqualTo("CBOR_WRITER_FAULTED"));
            Assert.That(outerTag.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(innerTag.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(content.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(contentEnd.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(complete.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(Convert.FromHexString("c1c280")));
            Assert.That(nestedWriter.Core.Frames, Is.Empty);
            Assert.That(nestedWriter.Core.TotalItems, Is.EqualTo(3));
        });
    }

    [Test]
    public void UnexpectedMismatchedAndMissingEndsFaultPredictably()
    {
        CborTokenWriter unexpectedWriter = new CborTokenWriter(new CborEncodeOptions());
        CborWriteResult unexpected = unexpectedWriter.WriteToken(EndArray(), []);

        CborTokenWriter mismatchWriter = new CborTokenWriter(new CborEncodeOptions());
        mismatchWriter.WriteToken(StartArray(0), new byte[1]);
        CborWriteResult mismatch = mismatchWriter.WriteToken(EndMap(), []);

        CborTokenWriter missingEndWriter = new CborTokenWriter(new CborEncodeOptions());
        missingEndWriter.WriteToken(StartArray(0), new byte[1]);
        CborWriteResult missingEnd = missingEndWriter.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(unexpected.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_END_TOKEN"));
            Assert.That(unexpectedWriter.Core.Faulted, Is.True);
            Assert.That(mismatch.ErrorCode, Is.EqualTo("CBOR_MISMATCHED_END_TOKEN"));
            Assert.That(mismatchWriter.Core.Faulted, Is.True);
            Assert.That(missingEnd.ErrorCode, Is.EqualTo("CBOR_CONTAINER_END_MISSING"));
            Assert.That(missingEndWriter.Core.Faulted, Is.True);
        });
    }

    [Test]
    public void NestingAndItemLimitsFailWithoutFaultingOrChangingOpenFrames()
    {
        CborTokenWriter depthWriter = new CborTokenWriter(
            new CborEncodeOptions { MaxNestingDepth = 1 });
        CborWriteResult root = depthWriter.WriteToken(StartArray(1), new byte[1]);
        byte[] untouched = [0xa5];
        CborWriteResult tooDeep = depthWriter.WriteToken(StartArray(0), untouched);
        CborWriteResult fallback = depthWriter.WriteToken(Unsigned(0), untouched);
        CborWriteResult rootEnd = depthWriter.WriteToken(EndArray(), []);

        CborTokenWriter itemWriter = new CborTokenWriter(
            new CborEncodeOptions { MaxTotalItems = 1 });
        CborWriteResult first = itemWriter.WriteToken(Unsigned(0), new byte[1]);
        CborWriteResult limited = itemWriter.WriteToken(Unsigned(1), untouched);

        Assert.Multiple(() =>
        {
            Assert.That(root.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(tooDeep.ErrorCode, Is.EqualTo("CBOR_MAX_NESTING_DEPTH"));
            Assert.That(depthWriter.Core.Faulted, Is.False);
            Assert.That(fallback.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(rootEnd.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(depthWriter.Core.Frames, Is.Empty);
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(limited.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(itemWriter.Core.Faulted, Is.False);
            Assert.That(itemWriter.Core.TotalItems, Is.EqualTo(1));
        });
    }

    private static CborToken StartArray(ulong length)
    {
        return new CborToken
        {
            Kind = CborTokenKind.StartArray,
            LengthKind = CborLengthKind.Definite,
            Length = length
        };
    }

    private static CborToken EndArray()
    {
        return new CborToken
        {
            Kind = CborTokenKind.EndArray,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static CborToken StartMap(ulong length)
    {
        return new CborToken
        {
            Kind = CborTokenKind.StartMap,
            LengthKind = CborLengthKind.Definite,
            Length = length
        };
    }

    private static CborToken EndMap()
    {
        return new CborToken
        {
            Kind = CborTokenKind.EndMap,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static CborToken Tag(ulong number)
    {
        return new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = number
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

    private static CborToken Negative(int value)
    {
        return new CborToken
        {
            Kind = CborTokenKind.NegativeInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborToken Text(string value)
    {
        int byteLength = System.Text.Encoding.UTF8.GetByteCount(value);
        return new CborToken
        {
            Kind = CborTokenKind.TextString,
            LengthKind = CborLengthKind.Definite,
            Length = (ulong)byteLength,
            TextStringValue = value
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
}
