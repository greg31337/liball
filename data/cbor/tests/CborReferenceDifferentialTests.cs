using System.Numerics;
using cbor;
using ReferenceConformanceMode = System.Formats.Cbor.CborConformanceMode;
using ReferenceContentException = System.Formats.Cbor.CborContentException;
using ReferenceReader = System.Formats.Cbor.CborReader;
using ReferenceSimpleValue = System.Formats.Cbor.CborSimpleValue;
using ReferenceTag = System.Formats.Cbor.CborTag;
using ReferenceWriter = System.Formats.Cbor.CborWriter;

namespace cbor.Tests;

[TestFixture]
public class CborReferenceDifferentialTests
{
    [Test]
    public void CommonDataModelValuesEncodeAndDecodeIdentically()
    {
        ReferenceWriter reference = new ReferenceWriter(
            ReferenceConformanceMode.Strict,
            false,
            false);
        reference.WriteStartArray(18);
        reference.WriteUInt64(0);
        reference.WriteUInt64(23);
        reference.WriteUInt64(24);
        reference.WriteUInt64(ulong.MaxValue);
        reference.WriteInt64(-1);
        reference.WriteCborNegativeIntegerRepresentation(ulong.MaxValue);
        reference.WriteByteString([0x00, 0xab, 0xff]);
        reference.WriteTextString("IETF 水");
        reference.WriteStartArray(2);
        reference.WriteBoolean(true);
        reference.WriteNull();
        reference.WriteEndArray();
        reference.WriteStartMap(2);
        reference.WriteTextString("a");
        reference.WriteUInt64(1);
        reference.WriteTextString("b");
        reference.WriteUInt64(2);
        reference.WriteEndMap();
        reference.WriteHalf((Half)1.5);
        reference.WriteSingle(100000.0f);
        reference.WriteDouble(1.1);
        reference.WriteBoolean(false);
        reference.WriteBoolean(true);
        reference.WriteNull();
        reference.WriteSimpleValue((ReferenceSimpleValue)23);
        reference.WriteSimpleValue((ReferenceSimpleValue)255);
        reference.WriteEndArray();
        byte[] referenceEncoding = reference.Encode();

        CborValue value = ArrayValue(
        [
            Integer(BigInteger.Zero),
            Integer(new BigInteger(23)),
            Integer(new BigInteger(24)),
            Integer(new BigInteger(ulong.MaxValue)),
            Integer(new BigInteger(-1)),
            Integer(BigInteger.Parse("-18446744073709551616")),
            Bytes([0x00, 0xab, 0xff]),
            Text("IETF 水"),
            ArrayValue([Simple(CborSimpleValues.True), Simple(CborSimpleValues.Null)]),
            MapValue(
            [
                Pair(Text("a"), Integer(BigInteger.One)),
                Pair(Text("b"), Integer(new BigInteger(2)))
            ]),
            Float(CborFloatWidth.Half, 1.5, 0x3e00),
            Float(CborFloatWidth.Single, 100000.0, 0x47c35000),
            Float(CborFloatWidth.Double, 1.1, 0x3ff199999999999a),
            Simple(CborSimpleValues.False),
            Simple(CborSimpleValues.True),
            Simple(CborSimpleValues.Null),
            Simple(CborSimpleValues.Undefined),
            Simple(255)
        ]);
        byte[] libraryEncoding = new CborEncoder().Encode(
            value,
            new CborEncodeOptions { ReduceFloatWidth = false });

        Assert.That(libraryEncoding, Is.EqualTo(referenceEncoding));
        AssertReferenceCommonValue(referenceEncoding);

        CborReadResult libraryDecoded = Decode(referenceEncoding);
        Assert.Multiple(() =>
        {
            Assert.That(libraryDecoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(libraryDecoded.Value?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(libraryDecoded.Value?.ArrayItems, Has.Count.EqualTo(18));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[3].IntegerValue,
                Is.EqualTo(new BigInteger(ulong.MaxValue)));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[5].IntegerValue,
                Is.EqualTo(BigInteger.Parse("-18446744073709551616")));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[7].TextStringValue,
                Is.EqualTo("IETF 水"));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[9].MapEntries,
                Has.Count.EqualTo(2));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[10].FloatWidth,
                Is.EqualTo(CborFloatWidth.Half));
            Assert.That(
                libraryDecoded.Value?.ArrayItems?[17].SimpleValue,
                Is.EqualTo(255));
        });
    }

    [Test]
    public void GenericTagsAndBignumsEncodeAndDecodeIdentically()
    {
        BigInteger positive = BigInteger.Parse("18446744073709551616");
        BigInteger negative = BigInteger.Parse("-18446744073709551617");
        ReferenceWriter reference = new ReferenceWriter(
            ReferenceConformanceMode.Strict,
            false,
            false);
        reference.WriteStartArray(3);
        reference.WriteTag((ReferenceTag)1000);
        reference.WriteTextString("tagged");
        reference.WriteBigInteger(positive);
        reference.WriteBigInteger(negative);
        reference.WriteEndArray();
        byte[] referenceEncoding = reference.Encode();

        CborValue libraryValue = ArrayValue(
        [
            Tag(1000, Text("tagged")),
            Bignum(CborTagNumbers.PositiveBignum),
            Bignum(CborTagNumbers.NegativeBignum)
        ]);
        byte[] libraryEncoding = new CborEncoder().Encode(
            libraryValue,
            new CborEncodeOptions());

        ReferenceReader reader = new ReferenceReader(
            libraryEncoding,
            ReferenceConformanceMode.Strict,
            false);
        Assert.Multiple(() =>
        {
            Assert.That(libraryEncoding, Is.EqualTo(referenceEncoding));
            Assert.That(reader.ReadStartArray(), Is.EqualTo(3));
            Assert.That(reader.ReadTag(), Is.EqualTo((ReferenceTag)1000));
            Assert.That(reader.ReadTextString(), Is.EqualTo("tagged"));
            Assert.That(reader.ReadBigInteger(), Is.EqualTo(positive));
            Assert.That(reader.ReadBigInteger(), Is.EqualTo(negative));
        });
        reader.ReadEndArray();
        Assert.That(reader.BytesRemaining, Is.Zero);

        CborReadResult decoded = Decode(referenceEncoding);
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Value?.ArrayItems?[0].TagNumber, Is.EqualTo(1000));
            Assert.That(decoded.Value?.ArrayItems?[1].TagNumber,
                Is.EqualTo(CborTagNumbers.PositiveBignum));
            Assert.That(decoded.Value?.ArrayItems?[2].TagNumber,
                Is.EqualTo(CborTagNumbers.NegativeBignum));
        });
    }

    [Test]
    public void LengthFirstDeterminismMatchesTheReferenceWhileCoreOrderIsIntentional()
    {
        ReferenceWriter reference = new ReferenceWriter(
            ReferenceConformanceMode.Canonical,
            false,
            false);
        reference.WriteStartMap(2);
        reference.WriteUInt64(100);
        reference.WriteNull();
        reference.WriteInt64(-1);
        reference.WriteNull();
        reference.WriteEndMap();
        byte[] referenceEncoding = reference.Encode();

        CborValue map = MapValue(
        [
            Pair(Integer(new BigInteger(100)), Simple(CborSimpleValues.Null)),
            Pair(Integer(new BigInteger(-1)), Simple(CborSimpleValues.Null))
        ]);
        byte[] lengthFirst = new CborEncoder().Encode(
            map,
            DeterministicOptions(CborEncodingProfile.LengthFirstCoreDeterministic));
        byte[] core = new CborEncoder().Encode(
            map,
            DeterministicOptions(CborEncodingProfile.CoreDeterministic));

        ReferenceReader referenceReader = new ReferenceReader(
            lengthFirst,
            ReferenceConformanceMode.Canonical,
            false);
        ReadOnlyMemory<byte> accepted = referenceReader.ReadEncodedValue();
        CborValidationResult lengthFirstValidation = ValidateDeterministic(
            referenceEncoding,
            CborEncodingProfile.LengthFirstCoreDeterministic);
        CborValidationResult coreValidation = ValidateDeterministic(
            core,
            CborEncodingProfile.CoreDeterministic);
        CborValidationResult intentionalDifference = ValidateDeterministic(
            referenceEncoding,
            CborEncodingProfile.CoreDeterministic);

        Assert.Multiple(() =>
        {
            Assert.That(referenceEncoding, Is.EqualTo(lengthFirst));
            Assert.That(referenceEncoding, Is.EqualTo(Convert.FromHexString("A220F61864F6")));
            Assert.That(core, Is.EqualTo(Convert.FromHexString("A21864F620F6")));
            Assert.That(core, Is.Not.EqualTo(referenceEncoding));
            Assert.That(accepted.ToArray(), Is.EqualTo(lengthFirst));
            Assert.That(referenceReader.BytesRemaining, Is.Zero);
            Assert.That(lengthFirstValidation.IsValid, Is.True);
            Assert.That(coreValidation.IsValid, Is.True);
            Assert.That(intentionalDifference.IsValid, Is.False);
            Assert.That(
                intentionalDifference.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_DETERMINISTIC_MAP_KEY_ORDER"));
        });
    }

    [Test]
    public void IndefiniteAndConvertedDefiniteRepresentationsMatchTheReference()
    {
        ReferenceWriter lax = new ReferenceWriter(
            ReferenceConformanceMode.Lax,
            false,
            false);
        WriteIndefiniteValue(lax);
        byte[] referenceIndefinite = lax.Encode();

        ReferenceWriter canonical = new ReferenceWriter(
            ReferenceConformanceMode.Canonical,
            true,
            false);
        WriteIndefiniteValue(canonical);
        byte[] referenceDefinite = canonical.Encode();

        CborValue value = IndefiniteValue();
        byte[] libraryIndefinite = new CborEncoder().Encode(
            value,
            new CborEncodeOptions { PreserveRepresentation = true });
        byte[] libraryDefinite = new CborEncoder().Encode(
            value,
            DeterministicOptions(CborEncodingProfile.LengthFirstCoreDeterministic));

        Assert.Multiple(() =>
        {
            Assert.That(libraryIndefinite, Is.EqualTo(referenceIndefinite));
            Assert.That(libraryIndefinite,
                Is.EqualTo(Convert.FromHexString("9F5F4101420203FF7F61616162FFFF")));
            Assert.That(libraryDefinite, Is.EqualTo(referenceDefinite));
            Assert.That(libraryDefinite,
                Is.EqualTo(Convert.FromHexString("8243010203626162")));
        });
    }

    [Test]
    public void CborSequencesMatchTheReferenceMultipleRootValueMode()
    {
        ReferenceWriter reference = new ReferenceWriter(
            ReferenceConformanceMode.Strict,
            false,
            true);
        reference.WriteUInt64(1);
        reference.WriteTextString("sequence");
        reference.WriteTag((ReferenceTag)1000);
        reference.WriteBoolean(false);
        byte[] referenceEncoding = reference.Encode();

        List<CborValue> values =
        [
            Integer(BigInteger.One),
            Text("sequence"),
            Tag(1000, Simple(CborSimpleValues.False))
        ];
        byte[] libraryEncoding = new CborEncoder().EncodeSequence(
            values,
            new CborEncodeOptions());

        ReferenceReader reader = new ReferenceReader(
            libraryEncoding,
            ReferenceConformanceMode.Strict,
            true);
        Assert.Multiple(() =>
        {
            Assert.That(libraryEncoding, Is.EqualTo(referenceEncoding));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(1));
            Assert.That(reader.ReadTextString(), Is.EqualTo("sequence"));
            Assert.That(reader.ReadTag(), Is.EqualTo((ReferenceTag)1000));
            Assert.That(reader.ReadBoolean(), Is.False);
            Assert.That(reader.BytesRemaining, Is.Zero);
        });

        List<CborReadResult> decoded = new CborDecoder().DecodeSequence(
            referenceEncoding,
            DecodeOptions());
        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Count.EqualTo(3));
            Assert.That(decoded.All(result => result.Status == CborReadStatus.Done), Is.True);
            Assert.That(decoded.Select(result => result.ItemIndex),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(decoded[2].Value?.TagNumber, Is.EqualTo(1000));
            Assert.That(decoded[2].Remaining, Is.Zero);
        });
    }

    [Test]
    public void DuplicateKeyPolicyDifferenceIsExplicitOnBothImplementations()
    {
        byte[] duplicateMap = Convert.FromHexString("A201000101");
        ReferenceReader strict = new ReferenceReader(
            duplicateMap,
            ReferenceConformanceMode.Strict,
            false);
        ReferenceReader lax = new ReferenceReader(
            duplicateMap,
            ReferenceConformanceMode.Lax,
            false);

        Assert.Throws<ReferenceContentException>(() => strict.ReadEncodedValue());
        ReadOnlyMemory<byte> laxValue = lax.ReadEncodedValue();

        CborReadResult rejected = new CborDecoder().Decode(
            duplicateMap,
            new CborDecodeOptions());
        CborReadResult preserved = new CborDecoder().Decode(
            duplicateMap,
            new CborDecodeOptions
            {
                DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve
            });

        Assert.Multiple(() =>
        {
            Assert.That(laxValue.ToArray(), Is.EqualTo(duplicateMap));
            Assert.That(lax.BytesRemaining, Is.Zero);
            Assert.That(rejected.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(rejected.ErrorCode, Is.EqualTo("CBOR_DUPLICATE_MAP_KEY"));
            Assert.That(preserved.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(preserved.Value?.MapEntries, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void EveryLibraryDuplicateKeyPolicyAppliesDuringHighLevelDecode()
    {
        byte[] duplicateMap = Convert.FromHexString(
            "A4616101616202616103616304");
        CborReadResult rejected = DecodeDuplicateMap(
            duplicateMap,
            CborDuplicateKeyPolicy.Reject);
        CborReadResult preserved = DecodeDuplicateMap(
            duplicateMap,
            CborDuplicateKeyPolicy.Preserve);
        CborReadResult first = DecodeDuplicateMap(
            duplicateMap,
            CborDuplicateKeyPolicy.KeepFirst);
        CborReadResult last = DecodeDuplicateMap(
            duplicateMap,
            CborDuplicateKeyPolicy.KeepLast);
        CborReadResult invalid = DecodeDuplicateMap(
            duplicateMap,
            (CborDuplicateKeyPolicy)int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(rejected.ErrorCode, Is.EqualTo("CBOR_DUPLICATE_MAP_KEY"));
            Assert.That(rejected.ErrorOffset, Is.EqualTo(7));
            Assert.That(rejected.ErrorPath, Is.EqualTo("$[2].key"));
            Assert.That(rejected.Value, Is.Null);
            Assert.That(rejected.Consumed, Is.Zero);
            Assert.That(rejected.Remaining, Is.EqualTo(duplicateMap.Length));
            Assert.That(preserved.Value?.MapEntries, Has.Count.EqualTo(4));
            Assert.That(first.Value?.MapEntries, Has.Count.EqualTo(3));
            Assert.That(first.Value?.MapEntries?[0].Key?.TextStringValue, Is.EqualTo("a"));
            Assert.That(first.Value?.MapEntries?[0].Value?.IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(first.Value?.MapEntries?[1].Key?.TextStringValue, Is.EqualTo("b"));
            Assert.That(first.Value?.MapEntries?[2].Key?.TextStringValue, Is.EqualTo("c"));
            Assert.That(last.Value?.MapEntries, Has.Count.EqualTo(3));
            Assert.That(last.Value?.MapEntries?[0].Key?.TextStringValue, Is.EqualTo("b"));
            Assert.That(last.Value?.MapEntries?[1].Key?.TextStringValue, Is.EqualTo("a"));
            Assert.That(last.Value?.MapEntries?[1].Value?.IntegerValue,
                Is.EqualTo(new BigInteger(3)));
            Assert.That(last.Value?.MapEntries?[2].Key?.TextStringValue, Is.EqualTo("c"));
            Assert.That(invalid.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_DUPLICATE_KEY_POLICY_INVALID"));
            Assert.That(invalid.ErrorOffset, Is.EqualTo(-1));
            Assert.That(invalid.ErrorPath, Is.EqualTo("$"));
        });
    }

    private static void AssertReferenceCommonValue(byte[] encoding)
    {
        ReferenceReader reader = new ReferenceReader(
            encoding,
            ReferenceConformanceMode.Strict,
            false);
        Assert.That(reader.ReadStartArray(), Is.EqualTo(18));
        Assert.That(reader.ReadUInt64(), Is.Zero);
        Assert.That(reader.ReadUInt64(), Is.EqualTo(23));
        Assert.That(reader.ReadUInt64(), Is.EqualTo(24));
        Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
        Assert.That(reader.ReadInt64(), Is.EqualTo(-1));
        Assert.That(reader.ReadCborNegativeIntegerRepresentation(), Is.EqualTo(ulong.MaxValue));
        Assert.That(reader.ReadByteString(), Is.EqualTo(new byte[] { 0x00, 0xab, 0xff }));
        Assert.That(reader.ReadTextString(), Is.EqualTo("IETF 水"));
        Assert.That(reader.ReadStartArray(), Is.EqualTo(2));
        Assert.That(reader.ReadBoolean(), Is.True);
        reader.ReadNull();
        reader.ReadEndArray();
        Assert.That(reader.ReadStartMap(), Is.EqualTo(2));
        Assert.That(reader.ReadTextString(), Is.EqualTo("a"));
        Assert.That(reader.ReadUInt64(), Is.EqualTo(1));
        Assert.That(reader.ReadTextString(), Is.EqualTo("b"));
        Assert.That(reader.ReadUInt64(), Is.EqualTo(2));
        reader.ReadEndMap();
        Assert.That(reader.ReadDouble(), Is.EqualTo(1.5));
        Assert.That(reader.ReadDouble(), Is.EqualTo(100000.0));
        Assert.That(reader.ReadDouble(), Is.EqualTo(1.1));
        Assert.That(reader.ReadBoolean(), Is.False);
        Assert.That(reader.ReadBoolean(), Is.True);
        reader.ReadNull();
        Assert.That(reader.ReadSimpleValue(), Is.EqualTo((ReferenceSimpleValue)23));
        Assert.That(reader.ReadSimpleValue(), Is.EqualTo((ReferenceSimpleValue)255));
        reader.ReadEndArray();
        Assert.That(reader.BytesRemaining, Is.Zero);
    }

    private static void WriteIndefiniteValue(ReferenceWriter writer)
    {
        writer.WriteStartArray(null);
        writer.WriteStartIndefiniteLengthByteString();
        writer.WriteByteString([0x01]);
        writer.WriteByteString([0x02, 0x03]);
        writer.WriteEndIndefiniteLengthByteString();
        writer.WriteStartIndefiniteLengthTextString();
        writer.WriteTextString("a");
        writer.WriteTextString("b");
        writer.WriteEndIndefiniteLengthTextString();
        writer.WriteEndArray();
    }

    private static CborValue IndefiniteValue()
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Indefinite,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.ByteString,
                    LengthKind = CborLengthKind.Indefinite,
                    ByteStringValue = [0x01, 0x02, 0x03],
                    Representation = new CborRepresentation
                    {
                        LengthKind = CborLengthKind.Indefinite,
                        ChunkByteLengths = [1, 2]
                    }
                },
                new CborValue
                {
                    Kind = CborValueKind.TextString,
                    LengthKind = CborLengthKind.Indefinite,
                    TextStringValue = "ab",
                    Representation = new CborRepresentation
                    {
                        LengthKind = CborLengthKind.Indefinite,
                        ChunkByteLengths = [1, 1]
                    }
                }
            ]
        };
    }

    private static CborValidationResult ValidateDeterministic(
        byte[] source,
        CborEncodingProfile profile)
    {
        return new CborValidator().ValidateEncodingWithOptions(
            source,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Deterministic,
                DeterministicEncodingProfile = profile
            });
    }

    private static CborEncodeOptions DeterministicOptions(CborEncodingProfile profile)
    {
        return new CborEncodeOptions { EncodingProfile = profile };
    }

    private static CborReadResult Decode(byte[] source)
    {
        return new CborDecoder().Decode(source, DecodeOptions());
    }

    private static CborReadResult DecodeDuplicateMap(
        byte[] source,
        CborDuplicateKeyPolicy policy)
    {
        return new CborDecoder().Decode(
            source,
            new CborDecodeOptions
            {
                DuplicateKeyPolicy = policy,
                TagPolicy = CborTagPolicy.Preserve,
                RetainSourceRanges = true
            });
    }

    private static CborDecodeOptions DecodeOptions()
    {
        return new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            TagPolicy = CborTagPolicy.Preserve,
            SharedValuePolicy = CborSharedValuePolicy.Preserve
        };
    }

    private static CborValue Integer(BigInteger value)
    {
        return new CborValue
        {
            Kind = value.Sign < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
        };
    }

    private static CborValue Bytes(byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue ArrayValue(List<CborValue> values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = values
        };
    }

    private static CborValue MapValue(List<CborMapEntry> entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = entries
        };
    }

    private static CborMapEntry Pair(CborValue key, CborValue value)
    {
        return new CborMapEntry { Key = key, Value = value };
    }

    private static CborValue Simple(byte value)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = value
        };
    }

    private static CborValue Float(CborFloatWidth width, double value, ulong bits)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = width,
            FloatValue = new CborFloatValue
            {
                Width = width,
                Value = value,
                RawBits = bits
            }
        };
    }

    private static CborValue Tag(ulong tag, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tag,
            TaggedValue = value
        };
    }

    private static CborValue Bignum(ulong tag)
    {
        return Tag(tag, Bytes([0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));
    }
}
