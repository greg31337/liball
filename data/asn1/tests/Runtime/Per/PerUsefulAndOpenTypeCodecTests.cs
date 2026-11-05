using asn1;

namespace asn1.Tests;

public class PerUsefulAndOpenTypeCodecTests
{
    [TestCase(AsnTypeKind.External, AsnRuntimeValueKind.External, "data-value")]
    [TestCase(AsnTypeKind.EmbeddedPdv, AsnRuntimeValueKind.EmbeddedPdv, "data-value")]
    [TestCase(AsnTypeKind.CharacterString, AsnRuntimeValueKind.CharacterString, "string-value")]
    public void AssociatedTypesRoundTripIdentificationDescriptorAndData(
        AsnTypeKind typeKind,
        AsnRuntimeValueKind runtimeKind,
        string dataName)
    {
        PerUsefulTypeCodec codec = CreateUsefulCodec();
        AsnRuntimeFieldValue[] fields = typeKind == AsnTypeKind.CharacterString
            ?
            [
                Field("identification", IdentificationSyntax(1, 2, 3)),
                Field(dataName, Octets(0xaa, 0xbb))
            ]
            :
            [
                Field("identification", IdentificationSyntax(1, 2, 3)),
                Field("data-value-descriptor", Text("descriptor")),
                Field(dataName, Octets(0xaa, 0xbb))
            ];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = runtimeKind,
            Fields = fields
        };
        PerBitWriter writer = CreateWriter();
        PerUsefulTypeResult encoded = codec.WriteAssociatedValue(
            writer,
            typeKind,
            value,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerUsefulTypeResult decoded = codec.ReadAssociatedValue(
            CreateReader(output),
            typeKind,
            false);

        AsnRuntimeValue decodedValue = decoded.Value
            ?? throw new InvalidOperationException();
        AsnRuntimeValue identification = ValueField(
            decodedValue,
            "identification");
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decodedValue.Kind, Is.EqualTo(runtimeKind));
            if (typeKind != AsnTypeKind.CharacterString)
            {
                Assert.That(ValueField(decodedValue, "data-value-descriptor")
                    .TextValue, Is.EqualTo("descriptor"));
            }
            Assert.That(ValueField(decodedValue, dataName).Octets,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(ValueField(identification, "syntax")
                .ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void IdentificationContextNegotiationRoundTripsBothComponents()
    {
        PerUsefulTypeCodec codec = CreateUsefulCodec();
        AsnRuntimeValue pair = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Field("presentation-context-id", Integer(12)),
                Field("transfer-syntax", ObjectIdentifier(2, 5, 4, 3))
            ]
        };
        AsnRuntimeValue identification = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [Field("context-negotiation", pair)]
        };
        PerBitWriter writer = CreateWriter();
        PerUsefulTypeResult encoded = codec.WriteIdentification(
            writer,
            identification,
            false);
        PerUsefulTypeResult decoded = codec.ReadIdentification(
            CreateReader(writer.GetOutput()),
            false);
        AsnRuntimeValue decodedPair = ValueField(
            decoded.Value ?? throw new InvalidOperationException(),
            "context-negotiation");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(ValueField(decodedPair, "presentation-context-id")
                .IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(12)));
            Assert.That(ValueField(decodedPair, "transfer-syntax")
                .ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 2, 5, 4, 3 }));
        });
    }

    [Test]
    public void InstanceOfWrapsValueAndPreservesUnresolvedPayload()
    {
        PerUsefulTypeCodec codec = CreateUsefulCodec();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.InstanceOf,
            Fields =
            [
                Field("type-id", ObjectIdentifier(1, 2, 840, 113549)),
                Field("value", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = new AsnOpenTypeValue
                    {
                        Unresolved = true
                    }
                })
            ]
        };
        PerBitWriter writer = CreateWriter();
        PerUsefulTypeResult encoded = codec.WriteInstanceOfValue(
            writer,
            value,
            [0xa0],
            3,
            false);
        PerUsefulTypeResult decoded = codec.ReadInstanceOfValue(
            CreateReader(writer.GetOutput()),
            false,
            AsnUnknownDataPolicy.Preserve,
            AsnEncodingRules.Uper,
            "",
            null);
        AsnOpenTypeValue open = ValueField(
            decoded.Value ?? throw new InvalidOperationException(),
            "value").OpenTypeValue ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(open.Unresolved, Is.True);
            Assert.That(open.HasOriginalData, Is.True);
            Assert.That(open.OriginalData, Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(open.EncodingRules, Is.EqualTo(AsnEncodingRules.Uper));
        });
    }

    [Test]
    public void ContentsConstraintKeepsBitsOrPadsToCompleteOctets()
    {
        PerContentsCodec codec = CreateContentsCodec();
        PerBitWriter bitWriter = CreateWriter();
        PerContentsResult bitEncoded = codec.WriteContents(
            bitWriter,
            [0xff],
            3,
            false,
            false);
        PerContentsResult bitDecoded = codec.ReadContents(
            CreateReader(bitWriter.GetOutput()),
            false,
            false);

        PerBitWriter octetWriter = CreateWriter();
        PerContentsResult octetEncoded = codec.WriteContents(
            octetWriter,
            [0xff],
            3,
            true,
            false);
        PerContentsResult octetDecoded = codec.ReadContents(
            CreateReader(octetWriter.GetOutput()),
            true,
            false);
        PerBitWriter emptyWriter = CreateWriter();
        PerContentsResult empty = codec.WriteContents(
            emptyWriter,
            [],
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(bitEncoded.Data, Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(bitDecoded.ContentBitCount, Is.EqualTo(3));
            Assert.That(bitDecoded.Data, Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(octetEncoded.Data, Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(octetDecoded.ContentBitCount, Is.EqualTo(8));
            Assert.That(octetDecoded.Data, Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(empty.Data, Is.Empty);
            Assert.That(emptyWriter.GetOutput().Data,
                Is.EqualTo(new byte[] { 0x00 }));
        });
    }

    [Test]
    public void ContentsConstraintAppliesResolvedAndUnknownPolicies()
    {
        PerContentsCodec codec = CreateContentsCodec();
        AsnRuntimeValue decodedValue = Integer(42);
        PerContentsResult resolved = codec.CreateContainedValue(
            [0x2a],
            AsnEncodingRules.Uper,
            "Payload",
            decodedValue,
            AsnUnknownDataPolicy.Reject);
        PerContentsResult preserved = codec.CreateContainedValue(
            [0x2a],
            AsnEncodingRules.Uper,
            "",
            null,
            AsnUnknownDataPolicy.Preserve);
        PerContentsResult skipped = codec.CreateContainedValue(
            [0x2a],
            AsnEncodingRules.Uper,
            "",
            null,
            AsnUnknownDataPolicy.Skip);
        PerContentsResult rejected = codec.CreateContainedValue(
            [0x2a],
            AsnEncodingRules.Uper,
            "",
            null,
            AsnUnknownDataPolicy.Reject);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.OpenValue?.Resolved, Is.True);
            Assert.That(resolved.OpenValue?.Value, Is.SameAs(decodedValue));
            Assert.That(preserved.OpenValue?.HasOriginalData, Is.True);
            Assert.That(skipped.OpenValue?.HasOriginalData, Is.False);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNP0079"));
        });
    }

    [Test]
    public void TableConstrainedOpenTypeResolvesAndAppliesUnknownPolicies()
    {
        PerTableOpenTypeCodec codec = CreateTableCodec();
        AsnObjectTableDescriptor table = TableWithIntegerKey(7);
        AsnRuntimeValue decodedValue = Integer(42);
        PerTableOpenTypeResult resolved = codec.ResolveTablePayload(
            table,
            Integer(7),
            [0x2a],
            AsnUnknownDataPolicy.Reject,
            AsnEncodingRules.Uper,
            decodedValue);
        PerTableOpenTypeResult preserved = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Preserve,
            AsnEncodingRules.Uper,
            null);
        PerTableOpenTypeResult skipped = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Skip,
            AsnEncodingRules.Uper,
            null);
        PerTableOpenTypeResult rejected = codec.ResolveTablePayload(
            table,
            Integer(8),
            [0xca, 0xfe],
            AsnUnknownDataPolicy.Reject,
            AsnEncodingRules.Uper,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.OpenValue?.Resolved, Is.True);
            Assert.That(resolved.OpenValue?.ResolvedTypeId,
                Is.EqualTo("type.payload"));
            Assert.That(resolved.OpenValue?.Value, Is.SameAs(decodedValue));
            Assert.That(preserved.OpenValue?.Unresolved, Is.True);
            Assert.That(preserved.OpenValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(skipped.OpenValue?.HasOriginalData, Is.False);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNP0080"));
        });
    }

    [Test]
    public void PreservedTablePayloadIsReEmittedAsAnOpenType()
    {
        PerTableOpenTypeCodec codec = CreateTableCodec();
        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Unresolved = true,
            DispatchKey = Integer(8),
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Uper,
            HasOriginalData = true,
            OriginalData = [0xca, 0xfe]
        };
        PerBitWriter writer = CreateWriter();
        PerTableOpenTypeResult encoded = codec.WriteTableOpenType(
            writer,
            TableWithIntegerKey(7),
            open,
            [],
            0,
            AsnUnknownDataPolicy.Preserve,
            AsnEncodingRules.Uper,
            false);
        PerBitWriteResult output = writer.GetOutput();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x02, 0xca, 0xfe }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(24));
        });
    }

    private static PerUsefulTypeCodec CreateUsefulCodec()
    {
        return new PerUsefulTypeCodec(new AsnResourceLimits());
    }

    private static PerContentsCodec CreateContentsCodec()
    {
        return new PerContentsCodec(new AsnResourceLimits());
    }

    private static PerTableOpenTypeCodec CreateTableCodec()
    {
        return new PerTableOpenTypeCodec(new AsnResourceLimits());
    }

    private static PerBitWriter CreateWriter()
    {
        return new PerBitWriter(new AsnResourceLimits());
    }

    private static PerBitReader CreateReader(PerBitWriteResult output)
    {
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        return reader;
    }

    private static AsnRuntimeValue IdentificationSyntax(params int[] arcs)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [Field("syntax", ObjectIdentifier(arcs))]
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(params int[] arcs)
    {
        System.Numerics.BigInteger[] values =
            new System.Numerics.BigInteger[arcs.Length];
        int index = 0;
        while (index < arcs.Length)
        {
            values[index] = new System.Numerics.BigInteger(arcs[index]);
            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = values
            }
        };
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnRuntimeValue Octets(params byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
        };
    }

    private static AsnRuntimeFieldValue Field(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    private static AsnRuntimeValue ValueField(
        AsnRuntimeValue value,
        string name)
    {
        int index = 0;
        while (index < value.Fields.Length)
        {
            if (value.Fields[index].Present
                && value.Fields[index].Name == name
                && value.Fields[index].Value != null)
            {
                return value.Fields[index].Value
                    ?? throw new InvalidOperationException();
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Missing field " + name);
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
