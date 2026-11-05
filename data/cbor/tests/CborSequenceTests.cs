using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CborSequenceTests
{
    [Test]
    public void EmptySequenceDecodesAndEncodesAsZeroItemsAndBytes()
    {
        ICborDecoder decoder = new CborDecoder();
        ICborEncoder encoder = new CborEncoder();
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            MaxSequenceItems = 0,
            MaxSequenceBytes = 0
        };
        CborEncodeOptions encodeOptions = new CborEncodeOptions
        {
            MaxSequenceItems = 0,
            MaxSequenceBytes = 0
        };

        List<CborReadResult> decoded = decoder.DecodeSequence([], decodeOptions);
        byte[] encoded = encoder.EncodeSequence([], encodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.Empty);
            Assert.That(encoded, Is.Empty);
        });
    }

    [Test]
    public void MultipleItemsRoundTripWithIndexesOffsetsAndRemainingBytes()
    {
        byte[] sequence =
        [
            0x01,
            0x82, 0x02, 0x03,
            0x64, 0x74, 0x65, 0x73, 0x74
        ];
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            RetainSourceRanges = true
        };
        CborDecoder decoder = new CborDecoder();
        List<CborReadResult> decoded = decoder.DecodeSequence(sequence, decodeOptions);
        List<CborValue> values = decoded.Select(result => result.Value!).ToList();
        byte[] reencoded = new CborEncoder().EncodeSequence(values, new CborEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Count.EqualTo(3));
            Assert.That(decoded.All(result => result.Status == CborReadStatus.Done), Is.True);
            Assert.That(decoded.Select(result => result.ItemIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(decoded.Select(result => result.SourceOffset), Is.EqualTo(new long[] { 0, 1, 4 }));
            Assert.That(decoded.Select(result => result.Consumed), Is.EqualTo(new[] { 1, 3, 5 }));
            Assert.That(decoded.Select(result => result.Remaining), Is.EqualTo(new[] { 8, 5, 0 }));
            Assert.That(decoded[0].Value?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(decoded[1].Value?.ArrayItems?[1].IntegerValue, Is.EqualTo(new BigInteger(3)));
            Assert.That(decoded[2].Value?.TextStringValue, Is.EqualTo("test"));
            Assert.That(decoded[0].Value?.Representation?.SourceOffset, Is.EqualTo(0));
            Assert.That(decoded[1].Value?.Representation?.SourceOffset, Is.EqualTo(1));
            Assert.That(decoded[2].Value?.Representation?.SourceOffset, Is.EqualTo(4));
            Assert.That(reencoded, Is.EqualTo(sequence));
        });
    }

    [Test]
    public void ConcatenatingEncodedSequencesConcatenatesTheirValues()
    {
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();
        List<CborValue> firstValues = [CreateUnsigned(1), CreateUnsigned(2)];
        List<CborValue> secondValues =
        [
            new CborValue
            {
                Kind = CborValueKind.TextString,
                TextStringValue = "x"
            }
        ];

        byte[] first = encoder.EncodeSequence(firstValues, options);
        byte[] second = encoder.EncodeSequence(secondValues, options);
        byte[] concatenated = [.. first, .. second];
        byte[] combined = encoder.EncodeSequence(
            [.. firstValues, .. secondValues],
            options
        );
        List<CborReadResult> decoded = new CborDecoder().DecodeSequence(
            concatenated,
            new CborDecodeOptions()
        );

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new byte[] { 0x01, 0x02 }));
            Assert.That(second, Is.EqualTo(new byte[] { 0x61, 0x78 }));
            Assert.That(combined, Is.EqualTo(concatenated));
            Assert.That(decoded, Has.Count.EqualTo(3));
            Assert.That(decoded[2].Value?.TextStringValue, Is.EqualTo("x"));
            Assert.That(decoded[2].SourceOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void SequenceProcessingContinuesAcrossInternalBufferBoundaries()
    {
        byte[] sequence = new byte[5000];
        List<CborReadResult> decoded = new CborDecoder().DecodeSequence(
            sequence,
            new CborDecodeOptions
            {
                MaxSequenceItems = sequence.Length,
                MaxSequenceBytes = sequence.Length
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Count.EqualTo(sequence.Length));
            Assert.That(decoded[4095].ItemIndex, Is.EqualTo(4095));
            Assert.That(decoded[4095].SourceOffset, Is.EqualTo(4095));
            Assert.That(decoded[4096].ItemIndex, Is.EqualTo(4096));
            Assert.That(decoded[4096].SourceOffset, Is.EqualTo(4096));
            Assert.That(decoded[^1].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded[^1].SourceOffset, Is.EqualTo(sequence.Length - 1));
            Assert.That(decoded[^1].Remaining, Is.Zero);
        });
    }

    [Test]
    public void TruncatedFinalItemPreservesEarlierResultsAndUsesAbsoluteOffsets()
    {
        List<CborReadResult> results = new CborDecoder().DecodeSequence(
            [0x00, 0x82, 0x01],
            new CborDecodeOptions()
        );

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results[0].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(results[0].ItemIndex, Is.Zero);
            Assert.That(results[0].SourceOffset, Is.Zero);
            Assert.That(results[1].Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(results[1].ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_END_OF_DATA"));
            Assert.That(results[1].ItemIndex, Is.EqualTo(1));
            Assert.That(results[1].SourceOffset, Is.EqualTo(1));
            Assert.That(results[1].ErrorOffset, Is.EqualTo(3));
        });
    }

    [Test]
    public void MalformedLaterItemReportsItsSequenceIndexAndAbsoluteOffset()
    {
        List<CborReadResult> results = new CborDecoder().DecodeSequence(
            [0x00, 0x01, 0x1c],
            new CborDecodeOptions()
        );

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results[2].Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(results[2].ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
            Assert.That(results[2].ItemIndex, Is.EqualTo(2));
            Assert.That(results[2].SourceOffset, Is.EqualTo(2));
            Assert.That(results[2].ErrorOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecodeSequenceEnforcesItemAndByteLimitsAtItemBoundaries()
    {
        CborDecoder decoder = new CborDecoder();
        List<CborReadResult> itemLimited = decoder.DecodeSequence(
            [0x00, 0x01, 0x02],
            new CborDecodeOptions { MaxSequenceItems = 2 }
        );
        List<CborReadResult> byteLimited = decoder.DecodeSequence(
            [0x00, 0x82, 0x01, 0x02],
            new CborDecodeOptions { MaxSequenceBytes = 3 }
        );

        Assert.Multiple(() =>
        {
            Assert.That(itemLimited, Has.Count.EqualTo(3));
            Assert.That(itemLimited[0].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(itemLimited[1].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(itemLimited[2].ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(itemLimited[2].ItemIndex, Is.EqualTo(2));
            Assert.That(itemLimited[2].SourceOffset, Is.EqualTo(2));
            Assert.That(itemLimited[2].Remaining, Is.EqualTo(1));
            Assert.That(byteLimited, Has.Count.EqualTo(2));
            Assert.That(byteLimited[0].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(byteLimited[1].ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(byteLimited[1].ItemIndex, Is.EqualTo(1));
            Assert.That(byteLimited[1].SourceOffset, Is.EqualTo(1));
            Assert.That(byteLimited[1].Remaining, Is.EqualTo(3));
        });
    }

    [Test]
    public void EncodeSequenceEnforcesLimitsWithItemPathsAndByteOffsets()
    {
        CborEncoder encoder = new CborEncoder();
        CborEncodeException itemFailure = Assert.Throws<CborEncodeException>(() =>
            encoder.EncodeSequence(
                [CreateUnsigned(0), CreateUnsigned(1)],
                new CborEncodeOptions { MaxSequenceItems = 1 }
            ))!;
        CborEncodeException byteFailure = Assert.Throws<CborEncodeException>(() =>
            encoder.EncodeSequence(
                [
                    CreateUnsigned(0),
                    new CborValue
                    {
                        Kind = CborValueKind.TextString,
                        TextStringValue = "x"
                    }
                ],
                new CborEncodeOptions { MaxSequenceBytes = 2 }
            ))!;
        CborEncodeException valueFailure = Assert.Throws<CborEncodeException>(() =>
            encoder.EncodeSequence(
                [
                    CreateUnsigned(0),
                    new CborValue
                    {
                        Kind = CborValueKind.Array,
                        ArrayItems = [new CborValue()]
                    }
                ],
                new CborEncodeOptions()
            ))!;

        Assert.Multiple(() =>
        {
            Assert.That(itemFailure.ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(itemFailure.ErrorOffset, Is.EqualTo(1));
            Assert.That(itemFailure.ErrorPath, Is.EqualTo("$[1]"));
            Assert.That(byteFailure.ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(byteFailure.ErrorOffset, Is.EqualTo(1));
            Assert.That(byteFailure.ErrorPath, Is.EqualTo("$[1]"));
            Assert.That(valueFailure.ErrorCode, Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(valueFailure.ErrorOffset, Is.EqualTo(1));
            Assert.That(valueFailure.ErrorPath, Is.EqualTo("$[1][0]"));
        });
    }

    [Test]
    public void StreamReaderReportsSequenceMetadataAndLimitsAdditionalItems()
    {
        CborStreamReader reader = new CborStreamReader(
            new MemoryStream([0x00, 0x01, 0x02]),
            new CborDecodeOptions { MaxSequenceItems = 2 }
        );

        CborReadResult first = reader.ReadNext();
        CborReadResult second = reader.ReadNext();
        CborReadResult limit = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.ItemIndex, Is.Zero);
            Assert.That(first.SourceOffset, Is.Zero);
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(second.ItemIndex, Is.EqualTo(1));
            Assert.That(second.SourceOffset, Is.EqualTo(1));
            Assert.That(limit.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(limit.ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(limit.ItemIndex, Is.EqualTo(2));
            Assert.That(limit.SourceOffset, Is.EqualTo(2));
            Assert.That(limit.Consumed, Is.Zero);
        });
    }

    [Test]
    public void StreamWriterRejectsItemsBeyondSequenceLimitsWithoutWritingThem()
    {
        MemoryStream output = new MemoryStream();
        CborStreamWriter writer = new CborStreamWriter(
            output,
            new CborEncodeOptions
            {
                LeaveOpen = true,
                MaxSequenceBytes = 2,
                MaxSequenceItems = 2
            }
        );

        CborWriteResult first = writer.WriteNext(CreateUnsigned(0));
        CborWriteResult second = writer.WriteNext(CreateUnsigned(1));
        CborWriteResult limit = writer.WriteNext(CreateUnsigned(2));
        writer.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(second.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(limit.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(limit.ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(limit.ErrorPath, Is.EqualTo("$[2]"));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x01 }));
        });
    }

    [Test]
    public void NegativeSequenceLimitsRejectEvenAnEmptySequence()
    {
        List<CborReadResult> decoded = new CborDecoder().DecodeSequence(
            [],
            new CborDecodeOptions { MaxSequenceItems = -1 }
        );
        CborEncodeException encoded = Assert.Throws<CborEncodeException>(() =>
            new CborEncoder().EncodeSequence(
                [],
                new CborEncodeOptions { MaxSequenceBytes = -1 }
            ))!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Count.EqualTo(1));
            Assert.That(decoded[0].ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(decoded[0].ItemIndex, Is.Zero);
            Assert.That(decoded[0].SourceOffset, Is.Zero);
            Assert.That(encoded.ErrorCode, Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(encoded.ErrorOffset, Is.Zero);
        });
    }

    private static CborValue CreateUnsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }
}
