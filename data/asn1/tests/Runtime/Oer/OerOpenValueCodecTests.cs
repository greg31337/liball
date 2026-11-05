using asn1;

namespace asn1.Tests;

public class OerOpenValueCodecTests
{
    [Test]
    public void TableDispatchDecodesKnownPayloadAndAppliesUnknownPolicies()
    {
        OerTableOpenTypeCodec codec = Codec();
        AsnObjectTableDescriptor table = TableWithIntegerKey(7);

        OerOpenValueResult resolved = codec.ResolveTablePayload(
            table,
            Integer(7),
            [0x01, 0x2a],
            AsnUnknownDataPolicy.Reject,
            false);
        OerOpenValueResult preserved = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Preserve,
            false);
        OerOpenValueResult skipped = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Skip,
            false);
        OerOpenValueResult rejected = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Reject,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.OpenValue?.Resolved, Is.True);
            Assert.That(resolved.OpenValue?.ResolvedTypeId,
                Is.EqualTo("type.payload"));
            Assert.That(resolved.OpenValue?.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(resolved.OpenValue?.DispatchKey?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(preserved.OpenValue?.Unresolved, Is.True);
            Assert.That(preserved.OpenValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(preserved.OpenValue?.EncodingRules,
                Is.EqualTo(AsnEncodingRules.Oer));
            Assert.That(skipped.OpenValue?.HasOriginalData, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNO0095"));
        });
    }

    [Test]
    public void ResolvedAndPreservedTableValuesAreWrappedForEmission()
    {
        OerTableOpenTypeCodec codec = Codec();
        AsnObjectTableDescriptor table = TableWithIntegerKey(7);
        AsnOpenTypeValue resolved = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "type.payload",
            TypeName = "Payload",
            Value = Integer(42),
            DispatchKey = Integer(7)
        };
        AsnOpenTypeValue preserved = new AsnOpenTypeValue
        {
            Unresolved = true,
            DispatchKey = Integer(8),
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Oer,
            HasOriginalData = true,
            OriginalData = [0xca, 0xfe]
        };

        OerOpenValueResult resolvedEncoding = codec.EncodeTableOpenType(
            table,
            resolved,
            AsnUnknownDataPolicy.Reject,
            false);
        OerOpenValueResult preservedEncoding = codec.EncodeTableOpenType(
            table,
            preserved,
            AsnUnknownDataPolicy.Preserve,
            false);
        OerOpenValueResult incompatible = codec.EncodeTableOpenType(
            table,
            preserved,
            AsnUnknownDataPolicy.Preserve,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(resolvedEncoding.Data,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
            Assert.That(preservedEncoding.Data,
                Is.EqualTo(new byte[] { 0x02, 0xca, 0xfe }));
            Assert.That(incompatible.ErrorCode, Is.EqualTo("ASNO0093"));
        });
    }

    [Test]
    public void PlainOpenValuesPreserveSkipOrRejectUnresolvedContents()
    {
        OerOpenValueCodec codec = new OerOpenValueCodec(Limits());

        OerOpenValueResult preserved = codec.DecodeOpenValue(
            null,
            null,
            [0x02, 0xca, 0xfe],
            0,
            3,
            AsnUnknownDataPolicy.Preserve,
            true);
        OerOpenValueResult skipped = codec.DecodeOpenValue(
            null,
            null,
            [0x02, 0xca, 0xfe],
            0,
            3,
            AsnUnknownDataPolicy.Skip,
            false);
        OerOpenValueResult rejected = codec.DecodeOpenValue(
            null,
            null,
            [0x02, 0xca, 0xfe],
            0,
            3,
            AsnUnknownDataPolicy.Reject,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.OpenValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(preserved.OpenValue?.EncodingRules,
                Is.EqualTo(AsnEncodingRules.Coer));
            Assert.That(preserved.OpenValue?.EncodedOffset, Is.EqualTo(1));
            Assert.That(preserved.OpenValue?.EncodedLength, Is.EqualTo(2));
            Assert.That(skipped.OpenValue?.Unresolved, Is.True);
            Assert.That(skipped.OpenValue?.HasOriginalData, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNO0091"));
        });
    }

    [Test]
    public void OpenTypesParticipateInNestedStructuredDispatch()
    {
        AsnTypeDescriptor openType = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType
        };
        AsnFieldDescriptor payload = new AsnFieldDescriptor
        {
            Name = "payload",
            Type = openType
        };
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [payload],
            RootFields = [payload]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "payload",
                    Present = true,
                    Value = new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.OpenType,
                        OpenTypeValue = new AsnOpenTypeValue
                        {
                            Unresolved = true,
                            HasEncodingRules = true,
                            EncodingRules = AsnEncodingRules.Oer,
                            HasOriginalData = true,
                            OriginalData = [0xff]
                        }
                    }
                }
            ]
        };
        OerCollectionCodec codec = new OerCollectionCodec(Limits());

        OerCollectionResult encoded = codec.EncodeCollection(
            sequence,
            value,
            false);
        OerCollectionResult decoded = codec.DecodeCollection(
            sequence,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x01, 0xff }));
            Assert.That(decoded.Value?.Fields[0].Value?.OpenTypeValue
                ?.OriginalData, Is.EqualTo(new byte[] { 0xff }));
        });
    }

    [Test]
    public void DuplicateKeysAndOpenTypeLimitsAreRejected()
    {
        AsnObjectTableDescriptor duplicate = TableWithIntegerKey(7);
        duplicate.Entries = [duplicate.Entries[0], duplicate.Entries[0]];
        OerOpenValueResult ambiguity = Codec().ResolveEntry(
            duplicate,
            Integer(7));
        OerOpenValueResult limited = new OerOpenValueCodec(
            new AsnResourceLimits { MaximumOpenTypeBytes = 1 })
            .DecodeOpenValue(
                null,
                null,
                [0x02, 0xca, 0xfe],
                0,
                3,
                AsnUnknownDataPolicy.Preserve,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguity.ErrorCode, Is.EqualTo("ASNO0098"));
            Assert.That(limited.LimitExceeded, Is.True);
            Assert.That(limited.ErrorCode, Is.EqualTo("ASNR0020"));
        });
    }

    private static OerTableOpenTypeCodec Codec()
    {
        return new OerTableOpenTypeCodec(Limits());
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits();
    }

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static AsnObjectTableDescriptor TableWithIntegerKey(int key)
    {
        return new AsnObjectTableDescriptor
        {
            Entries =
            [
                new AsnObjectTableEntryDescriptor
                {
                    Key = new AsnValueDescriptor
                    {
                        Value = new AsnValue
                        {
                            Kind = AsnValueKind.Integer,
                            HasIntegerValue = true,
                            IntegerValue = new System.Numerics.BigInteger(key)
                        }
                    },
                    Type = new AsnTypeDescriptor
                    {
                        StableId = "type.payload",
                        Name = "Payload",
                        Kind = AsnTypeKind.Integer
                    }
                }
            ]
        };
    }
}
