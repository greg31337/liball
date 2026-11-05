using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborAppendixADecoderTests
{
    private static readonly string[] AppendixAEncodings =
    [
        "00",
        "01",
        "0a",
        "17",
        "1818",
        "1819",
        "1864",
        "1903e8",
        "1a000f4240",
        "1b000000e8d4a51000",
        "1bffffffffffffffff",
        "c249010000000000000000",
        "3bffffffffffffffff",
        "c349010000000000000000",
        "20",
        "29",
        "3863",
        "3903e7",
        "f90000",
        "f98000",
        "f93c00",
        "fb3ff199999999999a",
        "f93e00",
        "f97bff",
        "fa47c35000",
        "fa7f7fffff",
        "fb7e37e43c8800759c",
        "f90001",
        "f90400",
        "f9c400",
        "fbc010666666666666",
        "f97c00",
        "f97e00",
        "f9fc00",
        "fa7f800000",
        "fa7fc00000",
        "faff800000",
        "fb7ff0000000000000",
        "fb7ff8000000000000",
        "fbfff0000000000000",
        "f4",
        "f5",
        "f6",
        "f7",
        "f0",
        "f8ff",
        "c074323031332d30332d32315432303a30343a30305a",
        "c11a514b67b0",
        "c1fb41d452d9ec200000",
        "d74401020304",
        "d818456449455446",
        "d82076687474703a2f2f7777772e6578616d706c652e636f6d",
        "40",
        "4401020304",
        "60",
        "6161",
        "6449455446",
        "62225c",
        "62c3bc",
        "63e6b0b4",
        "64f0908591",
        "80",
        "83010203",
        "8301820203820405",
        "98190102030405060708090a0b0c0d0e0f101112131415161718181819",
        "a0",
        "a201020304",
        "a26161016162820203",
        "826161a161626163",
        "a56161614161626142616361436164614461656145",
        "5f42010243030405ff",
        "7f657374726561646d696e67ff",
        "9fff",
        "9f018202039f0405ffff",
        "9f01820203820405ff",
        "83018202039f0405ff",
        "83019f0203ff820405",
        "9f0102030405060708090a0b0c0d0e0f101112131415161718181819ff",
        "bf61610161629f0203ffff",
        "826161bf61626163ff",
        "bf6346756ef563416d7421ff"
    ];

    [TestCaseSource(nameof(AppendixAEncodings))]
    public void EveryRfc8949AppendixAVectorDecodesAsOneCompleteDom(string hex)
    {
        byte[] source = Convert.FromHexString(hex);
        CborDecodeOptions options = new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            TagPolicy = CborTagPolicy.Preserve,
            SharedValuePolicy = CborSharedValuePolicy.Preserve,
            RetainRepresentation = true,
            RetainSourceRanges = true
        };

        CborReadResult result = new CborDecoder().Decode(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done), hex);
            Assert.That(result.Value, Is.Not.Null, hex);
            Assert.That(result.Consumed, Is.EqualTo(source.Length), hex);
            Assert.That(result.Remaining, Is.Zero, hex);
            Assert.That(result.ErrorCode, Is.Null, hex);
            Assert.That(result.Value?.Representation, Is.Not.Null, hex);
            Assert.That(result.Value?.Representation?.HasSourceRange, Is.True, hex);
            Assert.That(result.Value?.Representation?.SourceOffset, Is.Zero, hex);
            Assert.That(result.Value?.Representation?.SourceLength, Is.EqualTo(source.Length), hex);
        });
    }

    [TestCase("f98000", CborFloatWidth.Half, 0x8000UL)]
    [TestCase("f97e00", CborFloatWidth.Half, 0x7e00UL)]
    [TestCase("fa47c35000", CborFloatWidth.Single, 0x47c35000UL)]
    [TestCase("fa7fc00000", CborFloatWidth.Single, 0x7fc00000UL)]
    [TestCase("fb3ff199999999999a", CborFloatWidth.Double, 0x3ff199999999999aUL)]
    [TestCase("fb7ff8000000000000", CborFloatWidth.Double, 0x7ff8000000000000UL)]
    public void AppendixAFloatsReachTheDomWithTheirExactWidthAndBits(
        string hex,
        CborFloatWidth width,
        ulong bits)
    {
        CborValue value = DecodeValue(hex, new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(CborValueKind.Float));
            Assert.That(value.FloatWidth, Is.EqualTo(width));
            Assert.That(value.FloatValue?.Width, Is.EqualTo(width));
            Assert.That(value.FloatValue?.RawBits, Is.EqualTo(bits));
        });
    }

    [Test]
    public void NestedRepresentationMetadataKeepsWidthsChunksBitsAndExactRanges()
    {
        byte[] source =
        [
            0x82,
            0x1b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xbf,
            0x78, 0x01, 0x61,
            0xfa, 0x3f, 0xc0, 0x00, 0x00,
            0x61, 0x62,
            0x5f, 0x41, 0xaa, 0x42, 0xbb, 0xcc, 0xff,
            0xff
        ];
        CborValue root = DecodeValue(
            Convert.ToHexString(source),
            new CborDecodeOptions
            {
                RetainRepresentation = true,
                RetainSourceRanges = true
            });
        CborValue integer = root.ArrayItems![0];
        CborValue map = root.ArrayItems[1];
        CborMapEntry first = map.MapEntries![0];
        CborMapEntry second = map.MapEntries[1];

        Assert.Multiple(() =>
        {
            Assert.That(root.Representation?.SourceOffset, Is.Zero);
            Assert.That(root.Representation?.SourceLength, Is.EqualTo(source.Length));
            Assert.That(integer.IntegerValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(integer.Representation?.HeadArgumentWidth, Is.EqualTo(8));
            Assert.That(integer.Representation?.SourceOffset, Is.EqualTo(1));
            Assert.That(integer.Representation?.SourceLength, Is.EqualTo(9));
            Assert.That(map.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(map.Representation?.SourceOffset, Is.EqualTo(10));
            Assert.That(map.Representation?.SourceLength, Is.EqualTo(19));
            Assert.That(first.Key?.Representation?.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(first.Key?.Representation?.SourceOffset, Is.EqualTo(11));
            Assert.That(first.Key?.Representation?.SourceLength, Is.EqualTo(3));
            Assert.That(first.Value?.FloatWidth, Is.EqualTo(CborFloatWidth.Single));
            Assert.That(first.Value?.Representation?.FloatBits, Is.EqualTo(0x3fc00000UL));
            Assert.That(first.Value?.Representation?.SourceOffset, Is.EqualTo(14));
            Assert.That(first.Value?.Representation?.SourceLength, Is.EqualTo(5));
            Assert.That(second.Key?.TextStringValue, Is.EqualTo("b"));
            Assert.That(second.Value?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(second.Value?.ByteStringValue, Is.EqualTo(new byte[] { 0xaa, 0xbb, 0xcc }));
            Assert.That(second.Value?.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(second.Value?.Representation?.SourceOffset, Is.EqualTo(21));
            Assert.That(second.Value?.Representation?.SourceLength, Is.EqualTo(7));
        });
    }

    [Test]
    public void DuplicateMapKeysRemainDistinctAndOrderedWhenPreserved()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        CborValue map = DecodeValue("bf01616118016162016163ff", options);
        List<CborMapEntry> entries = map.MapEntries!;

        Assert.Multiple(() =>
        {
            Assert.That(map.Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(map.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(entries[0].Key?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(entries[1].Key?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(entries[2].Key?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(entries[0].Value?.TextStringValue, Is.EqualTo("a"));
            Assert.That(entries[1].Value?.TextStringValue, Is.EqualTo("b"));
            Assert.That(entries[2].Value?.TextStringValue, Is.EqualTo("c"));
            Assert.That(entries[0].Key, Is.Not.SameAs(entries[1].Key));
            Assert.That(entries[1].Key, Is.Not.SameAs(entries[2].Key));
            Assert.That(entries[0].Key?.Representation?.HeadArgumentWidth, Is.Zero);
            Assert.That(entries[1].Key?.Representation?.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(entries[2].Key?.Representation?.HeadArgumentWidth, Is.Zero);
        });
    }

    [Test]
    public void UnknownSimpleValuesAndTagsArePreservedOrRejectedByDecodeOptions()
    {
        CborDecodeOptions preserveOptions = new CborDecodeOptions
        {
            AllowUnknownSimpleValues = true,
            TagPolicy = CborTagPolicy.Preserve,
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        CborReadResult preserved = new CborDecoder().Decode(
            [0x83, 0xf0, 0xf8, 0xff, 0xd9, 0xff, 0xff, 0x01],
            preserveOptions);
        List<CborValue> items = preserved.Value!.ArrayItems!;

        CborReadResult rejected = new CborDecoder().Decode(
            [0x82, 0x00, 0xf0],
            new CborDecodeOptions { AllowUnknownSimpleValues = false });

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(items[0].Kind, Is.EqualTo(CborValueKind.SimpleValue));
            Assert.That(items[0].SimpleValue, Is.EqualTo(16));
            Assert.That(items[1].SimpleValue, Is.EqualTo(255));
            Assert.That(items[2].Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(items[2].TagNumber, Is.EqualTo(65535));
            Assert.That(items[2].TaggedValue?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(items[2].Representation?.SourceOffset, Is.EqualTo(4));
            Assert.That(items[2].Representation?.SourceLength, Is.EqualTo(4));
            AssertAtomicFailure(rejected, "CBOR_UNKNOWN_SIMPLE_VALUE", 2, 3);
        });
    }

    [Test]
    public void ExactAndFirstDecodingDivergeOnlyAtTheCompleteItemBoundary()
    {
        byte[] source = [0x82, 0x01, 0x02, 0x1c];
        CborDecoder decoder = new CborDecoder();

        CborReadResult exact = decoder.Decode(source, new CborDecodeOptions());
        CborReadResult first = decoder.DecodeFirst(
            source,
            new CborDecodeOptions { RetainSourceRanges = true });

        Assert.Multiple(() =>
        {
            AssertAtomicFailure(exact, "CBOR_TRAILING_DATA", 3, source.Length);
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Value?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(first.Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(first.Value?.Representation?.SourceOffset, Is.Zero);
            Assert.That(first.Value?.Representation?.SourceLength, Is.EqualTo(3));
            Assert.That(first.Consumed, Is.EqualTo(3));
            Assert.That(first.Remaining, Is.EqualTo(1));
        });
    }

    [TestCase("8201636162", "CBOR_UNEXPECTED_END_OF_DATA", 5L)]
    [TestCase("820162c220", "CBOR_UTF8_INVALID_CONTINUATION", 4L)]
    [TestCase("81bf01ff", "CBOR_ODD_INDEFINITE_MAP", 3L)]
    [TestCase("815f6161", "CBOR_INVALID_BYTE_STRING_CHUNK", 2L)]
    [TestCase("810001", "CBOR_TRAILING_DATA", 2L)]
    public void NestedFailuresReportAtomicResultsWithExactOffsetsAndRootPaths(
        string hex,
        string errorCode,
        long errorOffset)
    {
        byte[] source = Convert.FromHexString(hex);

        CborReadResult result = new CborDecoder().Decode(source, new CborDecodeOptions());

        AssertAtomicFailure(result, errorCode, errorOffset, source.Length);
    }

    [Test]
    public void DomBuilderRejectsMismatchedChunksEndsMapsAndInvalidTokens()
    {
        CborDomBuilder chunkBuilder = new CborDomBuilder();
        bool strayChunk = chunkBuilder.AddToken(
            new CborToken { Kind = CborTokenKind.ByteStringChunk });

        CborDomBuilder endBuilder = new CborDomBuilder();
        endBuilder.AddToken(new CborToken { Kind = CborTokenKind.StartArray });
        bool mismatchedEnd = endBuilder.AddToken(new CborToken { Kind = CborTokenKind.EndMap });

        CborDomBuilder mapBuilder = new CborDomBuilder();
        mapBuilder.AddToken(new CborToken { Kind = CborTokenKind.StartMap });
        mapBuilder.AddToken(new CborToken { Kind = CborTokenKind.UnsignedInteger });
        bool incompleteMap = mapBuilder.AddToken(new CborToken { Kind = CborTokenKind.EndMap });

        CborDomBuilder invalidBuilder = new CborDomBuilder();
        bool invalid = invalidBuilder.AddToken(new CborToken { Kind = CborTokenKind.None });

        CborDomBuilder stringBuilder = new CborDomBuilder();
        stringBuilder.AddToken(new CborToken { Kind = CborTokenKind.StartTextString });
        bool valueInString = stringBuilder.AddToken(
            new CborToken { Kind = CborTokenKind.UnsignedInteger });

        Assert.Multiple(() =>
        {
            Assert.That(strayChunk, Is.False);
            Assert.That(chunkBuilder.ErrorCode, Is.EqualTo("CBOR_DOM_UNEXPECTED_STRING_CHUNK"));
            Assert.That(mismatchedEnd, Is.False);
            Assert.That(endBuilder.ErrorCode, Is.EqualTo("CBOR_DOM_MISMATCHED_END_TOKEN"));
            Assert.That(incompleteMap, Is.False);
            Assert.That(mapBuilder.ErrorCode, Is.EqualTo("CBOR_DOM_INCOMPLETE_MAP_ENTRY"));
            Assert.That(invalid, Is.False);
            Assert.That(invalidBuilder.ErrorCode, Is.EqualTo("CBOR_DOM_INVALID_TOKEN"));
            Assert.That(valueInString, Is.False);
            Assert.That(stringBuilder.ErrorCode, Is.EqualTo("CBOR_DOM_UNEXPECTED_VALUE"));
        });
    }

    private static CborValue DecodeValue(string hex, CborDecodeOptions options)
    {
        CborReadResult result = new CborDecoder().Decode(Convert.FromHexString(hex), options);
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done), result.ErrorMessage);
        Assert.That(result.Value, Is.Not.Null);
        return result.Value!;
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
}
