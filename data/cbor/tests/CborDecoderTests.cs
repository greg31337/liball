using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CborDecoderTests
{
    [Test]
    public void DecoderBuildsExactlyOneCompleteValue()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.Preserve,
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        CborReadResult result = new CborDecoder().Decode(
            [0x82, 0x01, 0xc2, 0x61, 0x78],
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(5));
            Assert.That(result.Remaining, Is.Zero);
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.Value?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(result.Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(result.Value?.ArrayItems?[0].IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(result.Value?.ArrayItems?[1].TagNumber, Is.EqualTo(2));
            Assert.That(result.Value?.ArrayItems?[1].TaggedValue?.TextStringValue, Is.EqualTo("x"));
            Assert.That(result.Value?.Representation?.SourceOffset, Is.Zero);
            Assert.That(result.Value?.Representation?.SourceLength, Is.EqualTo(5));
        });
    }

    [Test]
    public void ExactAndFirstItemOperationsApplyTrailingDataPolicy()
    {
        byte[] source = [0x00, 0x01, 0x02];
        CborDecoder decoder = new CborDecoder();
        CborReadResult exact = decoder.Decode(source, new CborDecodeOptions());
        CborReadResult first = decoder.DecodeFirst(source, new CborDecodeOptions());
        CborReadResult allowed = decoder.Decode(
            source,
            new CborDecodeOptions { AllowTrailingData = true });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(exact.ErrorCode, Is.EqualTo("CBOR_TRAILING_DATA"));
            Assert.That(exact.ErrorOffset, Is.EqualTo(1));
            Assert.That(exact.Value, Is.Null);
            Assert.That(exact.Consumed, Is.Zero);
            Assert.That(exact.Remaining, Is.EqualTo(3));
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Value?.IntegerValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(first.Consumed, Is.EqualTo(1));
            Assert.That(first.Remaining, Is.EqualTo(2));
            Assert.That(allowed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(allowed.Consumed, Is.EqualTo(1));
            Assert.That(allowed.Remaining, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecodeFailuresAreAtomicForEmptyTruncatedMalformedAndLimitedInput()
    {
        CborDecoder decoder = new CborDecoder();
        CborReadResult empty = decoder.Decode([], new CborDecodeOptions());
        CborReadResult truncated = decoder.Decode([0x82, 0x01], new CborDecodeOptions());
        CborReadResult malformed = decoder.Decode(
            [0x82, 0x00, 0x1c],
            new CborDecodeOptions());
        CborReadResult limited = decoder.Decode(
            [0x82, 0x00, 0x01],
            new CborDecodeOptions { MaxTotalItems = 2 });

        Assert.Multiple(() =>
        {
            AssertAtomicFailure(empty, "CBOR_UNEXPECTED_END_OF_DATA", 0, 0);
            AssertAtomicFailure(truncated, "CBOR_UNEXPECTED_END_OF_DATA", 2, 2);
            AssertAtomicFailure(malformed, "CBOR_RESERVED_ADDITIONAL_INFORMATION", 2, 3);
            AssertAtomicFailure(limited, "CBOR_MAX_TOTAL_ITEMS", 2, 3);
        });
    }

    [Test]
    public void DecoderCanBeReusedAfterSuccessAndFailureWithoutStateLeakage()
    {
        CborDecoder decoder = new CborDecoder();
        CborReadResult failure = decoder.Decode([0x81], new CborDecodeOptions());
        CborReadResult firstSuccess = decoder.Decode([0x01], new CborDecodeOptions());
        CborReadResult trailingFailure = decoder.Decode(
            [0x02, 0x03],
            new CborDecodeOptions());
        CborReadResult secondSuccess = decoder.Decode(
            [0x81, 0x04],
            new CborDecodeOptions { RetainSourceRanges = true });

        Assert.Multiple(() =>
        {
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(firstSuccess.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(firstSuccess.Value?.IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(trailingFailure.ErrorCode, Is.EqualTo("CBOR_TRAILING_DATA"));
            Assert.That(secondSuccess.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(secondSuccess.Value?.ArrayItems, Has.Count.EqualTo(1));
            Assert.That(secondSuccess.Value?.ArrayItems?[0].IntegerValue, Is.EqualTo(new BigInteger(4)));
            Assert.That(secondSuccess.Value?.Representation?.SourceOffset, Is.Zero);
            Assert.That(secondSuccess.Value?.Representation?.SourceLength, Is.EqualTo(2));
            Assert.That(decoder.Core.Active, Is.False);
        });
    }

    [Test]
    public void DomBuilderConstructsEveryScalarKindAndNestedTags()
    {
        byte[] encoding =
        [
            0x87,
            0x01,
            0x20,
            0x42, 0xaa, 0xbb,
            0x61, 0x78,
            0xf9, 0x3e, 0x00,
            0xf5,
            0xc1, 0xc2, 0x00
        ];
        CborValue value = Build(
            encoding,
            new CborDecodeOptions
            {
                RetainRepresentation = true,
                RetainSourceRanges = true
            });
        List<CborValue> items = value.ArrayItems!;
        CborValue outerTag = items[6];
        CborValue innerTag = outerTag.TaggedValue!;

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(value.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(value.Representation?.SourceOffset, Is.Zero);
            Assert.That(value.Representation?.SourceLength, Is.EqualTo(encoding.Length));
            Assert.That(items, Has.Count.EqualTo(7));
            Assert.That(items[0].Kind, Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(items[0].IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(items[1].Kind, Is.EqualTo(CborValueKind.NegativeInteger));
            Assert.That(items[1].IntegerValue, Is.EqualTo(new BigInteger(-1)));
            Assert.That(items[2].ByteStringValue, Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(items[3].TextStringValue, Is.EqualTo("x"));
            Assert.That(items[4].Kind, Is.EqualTo(CborValueKind.Float));
            Assert.That(items[4].FloatWidth, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(items[4].FloatValue?.Value, Is.EqualTo(1.5));
            Assert.That(items[4].Representation?.HasFloatBits, Is.True);
            Assert.That(items[5].Kind, Is.EqualTo(CborValueKind.SimpleValue));
            Assert.That(items[5].SimpleValue, Is.EqualTo(CborSimpleValues.True));
            Assert.That(outerTag.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(outerTag.TagNumber, Is.EqualTo(1));
            Assert.That(outerTag.Representation?.SourceOffset, Is.EqualTo(12));
            Assert.That(outerTag.Representation?.SourceLength, Is.EqualTo(3));
            Assert.That(innerTag.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(innerTag.TagNumber, Is.EqualTo(2));
            Assert.That(innerTag.Representation?.SourceOffset, Is.EqualTo(13));
            Assert.That(innerTag.Representation?.SourceLength, Is.EqualTo(2));
            Assert.That(innerTag.TaggedValue?.IntegerValue, Is.EqualTo(BigInteger.Zero));
        });
    }

    [Test]
    public void DomBuilderPreservesOrderedDuplicateAndArbitraryMapKeys()
    {
        byte[] encoding =
        [
            0xa3,
            0x01, 0x61, 0x61,
            0x01, 0x61, 0x62,
            0x81, 0x00, 0xf4
        ];
        CborValue value = Build(
            encoding,
            new CborDecodeOptions { RetainSourceRanges = true });
        List<CborMapEntry> entries = value.MapEntries!;

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(value.Representation?.SourceLength, Is.EqualTo(encoding.Length));
            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(entries[0].Key?.IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(entries[0].Value?.TextStringValue, Is.EqualTo("a"));
            Assert.That(entries[1].Key?.IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(entries[1].Value?.TextStringValue, Is.EqualTo("b"));
            Assert.That(entries[0].Key, Is.Not.SameAs(entries[1].Key));
            Assert.That(entries[2].Key?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(entries[2].Key?.ArrayItems, Has.Count.EqualTo(1));
            Assert.That(entries[2].Key?.Representation?.SourceOffset, Is.EqualTo(7));
            Assert.That(entries[2].Key?.Representation?.SourceLength, Is.EqualTo(2));
            Assert.That(entries[2].Value?.SimpleValue, Is.EqualTo(CborSimpleValues.False));
        });
    }

    [Test]
    public void DomBuilderConcatenatesIndefiniteStringsAndRetainsChunkDetails()
    {
        byte[] encoding =
        [
            0x9f,
            0x5f, 0x41, 0xaa, 0x42, 0xbb, 0xcc, 0xff,
            0x7f, 0x61, 0x61, 0x62, 0xc2, 0xa2, 0xff,
            0xff
        ];
        CborValue value = Build(
            encoding,
            new CborDecodeOptions
            {
                RetainRepresentation = true,
                RetainSourceRanges = true
            });
        CborValue bytes = value.ArrayItems![0];
        CborValue text = value.ArrayItems[1];

        Assert.Multiple(() =>
        {
            Assert.That(value.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(value.Representation?.SourceLength, Is.EqualTo(encoding.Length));
            Assert.That(bytes.Kind, Is.EqualTo(CborValueKind.ByteString));
            Assert.That(bytes.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(bytes.ByteStringValue, Is.EqualTo(new byte[] { 0xaa, 0xbb, 0xcc }));
            Assert.That(bytes.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(bytes.Representation?.SourceOffset, Is.EqualTo(1));
            Assert.That(bytes.Representation?.SourceLength, Is.EqualTo(7));
            Assert.That(text.Kind, Is.EqualTo(CborValueKind.TextString));
            Assert.That(text.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(text.TextStringValue, Is.EqualTo("a¢"));
            Assert.That(text.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(text.Representation?.SourceOffset, Is.EqualTo(8));
            Assert.That(text.Representation?.SourceLength, Is.EqualTo(7));
        });
    }

    [Test]
    public void RepresentationAndSourceRangeRetentionRemainIndependentlyConfigurable()
    {
        CborValue representationOnly = Build(
            [0x18, 0x18],
            new CborDecodeOptions { RetainRepresentation = true });
        CborValue sourceRangeOnly = Build(
            [0x18, 0x18],
            new CborDecodeOptions { RetainSourceRanges = true });
        CborValue neither = Build([0x00], new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(representationOnly.Representation, Is.Not.Null);
            Assert.That(representationOnly.Representation?.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(representationOnly.Representation?.HasSourceRange, Is.False);
            Assert.That(sourceRangeOnly.Representation, Is.Not.Null);
            Assert.That(sourceRangeOnly.Representation?.HasSourceRange, Is.True);
            Assert.That(sourceRangeOnly.Representation?.SourceOffset, Is.Zero);
            Assert.That(sourceRangeOnly.Representation?.SourceLength, Is.EqualTo(2));
            Assert.That(neither.Representation, Is.Null);
        });
    }

    [Test]
    public void DomBuilderRejectsInvalidSequencesAndCanBeReset()
    {
        CborDomBuilder builder = new CborDomBuilder();
        bool invalid = builder.AddToken(new CborToken { Kind = CborTokenKind.EndArray });
        string? errorCode = builder.ErrorCode;

        builder.Reset();
        bool accepted = builder.AddToken(
            new CborToken
            {
                Kind = CborTokenKind.TextString,
                LengthKind = CborLengthKind.Definite,
                TextStringValue = "ready"
            });
        bool afterCompletion = builder.AddToken(
            new CborToken { Kind = CborTokenKind.UnsignedInteger });

        Assert.Multiple(() =>
        {
            Assert.That(invalid, Is.False);
            Assert.That(errorCode, Is.EqualTo("CBOR_DOM_UNEXPECTED_END_TOKEN"));
            Assert.That(accepted, Is.True);
            Assert.That(builder.Completed, Is.True);
            Assert.That(builder.Value?.TextStringValue, Is.EqualTo("ready"));
            Assert.That(afterCompletion, Is.False);
            Assert.That(builder.ErrorCode, Is.EqualTo("CBOR_DOM_ALREADY_FINISHED"));
        });
    }

    private static void AssertAtomicFailure(
        CborReadResult result,
        string errorCode,
        long errorOffset,
        int sourceLength)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
        Assert.That(result.ErrorPath, Is.EqualTo("$"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Consumed, Is.Zero);
        Assert.That(result.Remaining, Is.EqualTo(sourceLength));
    }

    private static CborValue Build(byte[] source, CborDecodeOptions options)
    {
        CborTokenizerCore tokenizer = new CborTokenizerCore(options, null);
        CborDomBuilder builder = new CborDomBuilder();
        int sourceOffset = 0;
        int tokenCount = 0;

        while (!builder.Completed && !builder.Failed && tokenCount <= source.Length * 2 + 1)
        {
            byte[] remaining = source.AsSpan(sourceOffset).ToArray();
            CborTokenReadResult tokenResult = tokenizer.ReadToken(remaining);
            Assert.That(tokenResult.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(tokenResult.Token, Is.Not.Null);
            Assert.That(builder.AddToken(tokenResult.Token!), Is.True);
            sourceOffset = sourceOffset + tokenResult.Consumed;
            tokenCount = tokenCount + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(builder.Failed, Is.False, builder.ErrorMessage);
            Assert.That(builder.Completed, Is.True);
            Assert.That(builder.Value, Is.Not.Null);
            Assert.That(sourceOffset, Is.EqualTo(source.Length));
        });
        return builder.Value!;
    }
}
