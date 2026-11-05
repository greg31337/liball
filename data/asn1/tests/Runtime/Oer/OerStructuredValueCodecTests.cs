using asn1;

namespace asn1.Tests;

public class OerStructuredValueCodecTests
{
    [Test]
    public void TagsUseSixBitShortFormAndBase128LongForm()
    {
        OerTagCodec codec = new OerTagCodec(new AsnResourceLimits());

        OerTagResult shortTag = codec.EncodeTag(new AsnTag
        {
            Class = AsnTagClass.ContextSpecific,
            Number = 3
        });
        OerTagResult longTag = codec.EncodeTag(new AsnTag
        {
            Class = AsnTagClass.Application,
            Number = 201
        });
        OerTagResult decoded = codec.DecodeTag(
            longTag.Data,
            0,
            longTag.Data.Length);

        Assert.Multiple(() =>
        {
            Assert.That(shortTag.Data, Is.EqualTo(new byte[] { 0x83 }));
            Assert.That(longTag.Data,
                Is.EqualTo(new byte[] { 0x7f, 0x81, 0x49 }));
            Assert.That(decoded.Tag?.Class,
                Is.EqualTo(AsnTagClass.Application));
            Assert.That(decoded.Tag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(201)));
        });
    }

    [Test]
    public void ChoiceWritesTheSelectedOutermostTagAndPayload()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            0,
            Type(AsnTypeKind.Boolean));
        flag.Tags = [Tag(AsnTagClass.ContextSpecific, 2)];
        AsnFieldDescriptor large = Field(
            "large",
            1,
            Type(AsnTypeKind.Boolean));
        large.Tags = [Tag(AsnTagClass.Application, 201)];
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [flag, large],
            RootFields = [flag, large]
        };
        AsnRuntimeValue value = Choice("large", Boolean(true));

        OerCollectionResult encoded = new OerAlternativeCodec(Limits())
            .EncodeChoice(choice, value, false);
        OerCollectionResult decoded = new OerAlternativeCodec(Limits())
            .DecodeChoice(
                choice,
                encoded.Data,
                0,
                encoded.Data.Length,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x7f, 0x81, 0x49, 0xff }));
            Assert.That(decoded.Value?.Fields.Single().Name,
                Is.EqualTo("large"));
            Assert.That(decoded.Value?.Fields.Single().Value?.BooleanValue,
                Is.True);
        });
    }

    [Test]
    public void UntaggedNestedChoiceRepeatsTheSelectedOutermostTag()
    {
        AsnFieldDescriptor innerFlag = Field(
            "flag",
            0,
            Type(AsnTypeKind.Boolean));
        innerFlag.Tags = [Tag(AsnTagClass.ContextSpecific, 1)];
        AsnTypeDescriptor inner = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [innerFlag],
            RootFields = [innerFlag]
        };
        AsnFieldDescriptor nested = Field("nested", 0, inner);
        AsnTypeDescriptor outer = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [nested],
            RootFields = [nested]
        };
        AsnRuntimeValue value = Choice(
            "nested",
            Choice("flag", Boolean(false)));

        OerCollectionResult encoded = new OerAlternativeCodec(Limits())
            .EncodeChoice(outer, value, false);
        OerCollectionResult decoded = new OerAlternativeCodec(Limits())
            .DecodeChoice(
                outer,
                encoded.Data,
                0,
                encoded.Data.Length,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x81, 0x81, 0x00 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.Fields.Single().Value?.Fields
                .Single().Name, Is.EqualTo("flag"));
        });
    }

    [Test]
    public void SequenceOfUsesQuantityThenElementsAndSupportsZero()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = Type(AsnTypeKind.Boolean)
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = [Boolean(true), Boolean(false), Boolean(true)]
        };
        OerCollectionOfCodec codec = new OerCollectionOfCodec(Limits());

        OerCollectionResult encoded = codec.EncodeCollectionOf(
            type,
            value,
            false);
        OerCollectionResult empty = codec.EncodeCollectionOf(
            type,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf
            },
            false);
        OerCollectionResult decoded = codec.DecodeCollectionOf(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x01, 0x03, 0xff, 0x00, 0xff }));
            Assert.That(empty.Data, Is.EqualTo(new byte[] { 0x01, 0x00 }));
            Assert.That(decoded.Value?.Items, Has.Length.EqualTo(3));
            Assert.That(decoded.Value?.Items[1].BooleanValue, Is.False);
            Assert.That(decoded.ConsumedCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void SetOfUsesTheSameQuantityFormAndSetRuntimeKind()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SetOf,
            ElementType = Type(AsnTypeKind.Boolean)
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items = [Boolean(false), Boolean(true)]
        };
        OerCollectionOfCodec codec = new OerCollectionOfCodec(Limits());

        OerCollectionResult encoded = codec.EncodeCollectionOf(
            type,
            value,
            false);
        OerCollectionResult decoded = codec.DecodeCollectionOf(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x01, 0x02, 0x00, 0xff }));
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.SetOf));
            Assert.That(decoded.Value?.Items, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void StructuredDispatchAllowsChoiceAndCollectionOfComponents()
    {
        AsnFieldDescriptor selected = Field(
            "selected",
            0,
            Type(AsnTypeKind.Boolean));
        selected.Tags = [Tag(AsnTagClass.ContextSpecific, 1)];
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [selected],
            RootFields = [selected]
        };
        AsnTypeDescriptor booleans = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = Type(AsnTypeKind.Boolean)
        };
        AsnFieldDescriptor choiceField = Field("choice", 0, choice);
        AsnFieldDescriptor listField = Field("list", 1, booleans);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [choiceField, listField],
            RootFields = [choiceField, listField]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("choice", Choice("selected", Boolean(false))),
                Present("list", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [Boolean(true)]
                })
            ]
        };

        OerCollectionResult encoded = new OerCollectionCodec(Limits())
            .EncodeCollection(sequence, value, false);
        OerCollectionResult decoded = new OerCollectionCodec(Limits())
            .DecodeCollection(
                sequence,
                encoded.Data,
                0,
                encoded.Data.Length,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x81, 0x00, 0x01, 0x01, 0xff }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.Fields[1].Value?.Items.Single()
                .BooleanValue, Is.True);
        });
    }

    [Test]
    public void ContentsConstraintsCarryCompleteBitOrOctetEncodings()
    {
        OerContentsCodec codec = new OerContentsCodec(Limits());

        OerContentsResult bits = codec.EncodeContents(
            [0xe0],
            3,
            false);
        OerContentsResult octets = codec.EncodeContents(
            [0xaa, 0xbb],
            16,
            true);
        OerContentsResult decodedBits = codec.DecodeContents(
            bits.Data,
            0,
            bits.Data.Length,
            false,
            false);
        OerContentsResult invalidOctets = codec.EncodeContents(
            [0xe0],
            3,
            true);
        OerContentsResult contained = codec.CreateContainedValue(
            [0x2a],
            AsnEncodingRules.Oer,
            "Payload",
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 42
            });

        Assert.Multiple(() =>
        {
            Assert.That(bits.Data,
                Is.EqualTo(new byte[] { 0x02, 0x05, 0xe0 }));
            Assert.That(octets.Data,
                Is.EqualTo(new byte[] { 0x02, 0xaa, 0xbb }));
            Assert.That(decodedBits.ContentBitCount, Is.EqualTo(3));
            Assert.That(decodedBits.Data, Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(invalidOctets.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(contained.OpenValue?.Resolved, Is.True);
            Assert.That(contained.OpenValue?.OriginalData,
                Is.EqualTo(new byte[] { 0x2a }));
        });
    }

    [TestCase(AsnTypeKind.External, AsnRuntimeValueKind.External, "data-value")]
    [TestCase(AsnTypeKind.EmbeddedPdv, AsnRuntimeValueKind.EmbeddedPdv, "data-value")]
    [TestCase(AsnTypeKind.CharacterString, AsnRuntimeValueKind.CharacterString, "string-value")]
    public void AssociatedUsefulTypesRoundTripTheirDefinedSequence(
        AsnTypeKind typeKind,
        AsnRuntimeValueKind runtimeKind,
        string dataName)
    {
        AsnRuntimeValue identification = Choice(
            "syntax",
            ObjectIdentifier(1, 2, 3));
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        fields.Add(Present("identification", identification));
        if (typeKind == AsnTypeKind.External)
        {
            fields.Add(Present("data-value-descriptor", Text("d")));
        }
        fields.Add(Present(dataName, Octets(0xaa, 0xbb)));
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = runtimeKind,
            Fields = fields.ToArray()
        };
        OerUsefulTypeCodec codec = new OerUsefulTypeCodec(Limits());

        OerCollectionResult encoded = codec.EncodeUseful(
            typeKind,
            value,
            false);
        OerCollectionResult decoded = codec.DecodeUseful(
            typeKind,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data[0], Is.EqualTo(
                typeKind == AsnTypeKind.External ? 0x80 : 0x81));
            Assert.That(decoded.Value?.Kind, Is.EqualTo(runtimeKind));
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == dataName).Value?.Octets,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "identification").Value?.Fields
                .Single().Name, Is.EqualTo("syntax"));
        });
    }

    [Test]
    public void InstanceOfEncodesTypeIdThenOneOpenType()
    {
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.InstanceOf,
            Fields =
            [
                Present("type-id", ObjectIdentifier(1, 2, 3)),
                Present("value", new AsnRuntimeValue
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
                })
            ]
        };
        OerUsefulTypeCodec codec = new OerUsefulTypeCodec(Limits());

        OerCollectionResult encoded = codec.EncodeUseful(
            AsnTypeKind.InstanceOf,
            value,
            false);
        OerCollectionResult decoded = codec.DecodeUseful(
            AsnTypeKind.InstanceOf,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);
        AsnOpenTypeValue? open = decoded.Value?.Fields.Single(
            field => field.Name == "value").Value?.OpenTypeValue;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x02, 0x2a, 0x03, 0x01, 0xff }));
            Assert.That(open?.Unresolved, Is.True);
            Assert.That(open?.OriginalData,
                Is.EqualTo(new byte[] { 0xff }));
            Assert.That(open?.DispatchKey?.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void MalformedTagsQuantitiesAndTruncatedElementsFail()
    {
        OerTagResult malformedTag = new OerTagCodec(Limits()).DecodeTag(
            [0x3f, 0x00],
            0,
            2);
        AsnTypeDescriptor collection = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = Type(AsnTypeKind.Boolean)
        };
        OerCollectionResult emptyQuantity = new OerCollectionOfCodec(Limits())
            .DecodeCollectionOf(collection, [0x00], 0, 1, false);
        OerCollectionResult truncated = new OerCollectionOfCodec(Limits())
            .DecodeCollectionOf(collection, [0x01, 0x02, 0xff], 0, 3, false);

        Assert.Multiple(() =>
        {
            Assert.That(malformedTag.ErrorCode, Is.EqualTo("ASNO0071"));
            Assert.That(emptyQuantity.ErrorCode, Is.EqualTo("ASNO0079"));
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
        });
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits();
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    private static AsnFieldDescriptor Field(
        string name,
        int index,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Index = index,
            Type = type
        };
    }

    private static AsnTagDescriptor Tag(
        AsnTagClass tagClass,
        int number)
    {
        return new AsnTagDescriptor
        {
            Class = tagClass,
            Number = number
        };
    }

    private static AsnRuntimeValue Choice(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [Present(name, value)]
        };
    }

    private static AsnRuntimeFieldValue Present(
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

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(
        params System.Numerics.BigInteger[] arcs)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = arcs
            }
        };
    }

    private static AsnRuntimeValue Octets(params byte[] data)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = data
        };
    }

    private static AsnRuntimeValue Text(string text)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = text
        };
    }
}
