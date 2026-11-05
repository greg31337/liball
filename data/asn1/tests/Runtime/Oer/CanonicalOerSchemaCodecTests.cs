using asn1;

namespace asn1.Tests;

public class CanonicalOerSchemaCodecTests
{
    [Test]
    public void CoerUsesMinimalIntegerAndLengthForms()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        OerSchemaCodec codec = Codec(integer, AsnEncodingRules.Coer);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Number",
            Value = Integer(128),
            Options = Options(AsnEncodingRules.Coer)
        });
        AsnDecodeResult rejected = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Number",
            Data = [0x81, 0x02, 0x00, 0x80],
            Options = Options(AsnEncodingRules.Coer)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x02, 0x00, 0x80 }));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0004"));
        });
    }

    [Test]
    public void CoerOmitsDefaultsAndRejectsPresentDefaultValues()
    {
        AsnFieldDescriptor enabled = Field(
            "enabled",
            0,
            Type(AsnTypeKind.Boolean, "Enabled"));
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "TRUE"
            }
        };
        AsnTypeDescriptor settings = Type(
            AsnTypeKind.Sequence,
            "Settings");
        settings.Fields = [enabled];
        settings.RootFields = [enabled];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("enabled", Boolean(true))]
        };
        OerSchemaCodec canonical = Codec(
            settings,
            AsnEncodingRules.Coer);
        OerSchemaCodec basic = Codec(settings, AsnEncodingRules.Oer);

        AsnEncodeResult canonicalEncoding = canonical.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Settings",
                Value = value,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnEncodeResult basicEncoding = basic.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Settings",
                Value = value,
                Options = Options(AsnEncodingRules.Oer)
            });
        AsnDecodeResult rejected = canonical.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Settings",
                Data = basicEncoding.Data,
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(canonicalEncoding.Data,
                Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(basicEncoding.Data,
                Is.EqualTo(new byte[] { 0x80, 0xff }));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0063"));
        });
    }

    [Test]
    public void CoerSortsSetOfAndRejectsNonminimalQuantitiesAndOrder()
    {
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Numbers");
        set.ElementType = Type(AsnTypeKind.Integer, "Item");
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items = [Integer(2), Integer(1)]
        };
        OerSchemaCodec canonical = Codec(set, AsnEncodingRules.Coer);
        OerSchemaCodec basic = Codec(set, AsnEncodingRules.Oer);

        AsnEncodeResult canonicalEncoding = canonical.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Numbers",
                Value = value,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnEncodeResult basicEncoding = basic.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Numbers",
                Value = value,
                Options = Options(AsnEncodingRules.Oer)
            });
        AsnDecodeResult order = canonical.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Numbers",
                Data = basicEncoding.Data,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnDecodeResult quantity = canonical.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Numbers",
                Data = [0x02, 0x00, 0x02, 0x01, 0x01, 0x01, 0x02],
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(canonicalEncoding.Data, Is.EqualTo(new byte[]
            {
                0x01, 0x02, 0x01, 0x01, 0x01, 0x02
            }));
            Assert.That(basicEncoding.Data, Is.EqualTo(new byte[]
            {
                0x01, 0x02, 0x01, 0x02, 0x01, 0x01
            }));
            Assert.That(order.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0085"));
            Assert.That(quantity.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0084"));
        });
    }

    [Test]
    public void CoerExtensionBitmapEndsAtLastAdditionAndContentsAreCanonical()
    {
        AsnFieldDescriptor first = Field(
            "first",
            0,
            Type(AsnTypeKind.Boolean, "First"));
        first.Extension = true;
        first.Optional = true;
        AsnFieldDescriptor second = Field(
            "second",
            1,
            Type(AsnTypeKind.Boolean, "Second"));
        second.Extension = true;
        second.Optional = true;
        AsnTypeDescriptor sequence = Type(
            AsnTypeKind.Sequence,
            "Extensions");
        sequence.Extensible = true;
        sequence.Fields = [first, second];
        sequence.ExtensionFields = [first, second];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("first", Boolean(true))]
        };
        OerSchemaCodec canonical = Codec(
            sequence,
            AsnEncodingRules.Coer);
        OerSchemaCodec basic = Codec(sequence, AsnEncodingRules.Oer);

        AsnEncodeResult canonicalEncoding = canonical.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Extensions",
                Value = value,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnEncodeResult basicEncoding = basic.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Extensions",
                Value = value,
                Options = Options(AsnEncodingRules.Oer)
            });
        AsnDecodeResult bitmap = canonical.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Extensions",
                Data = basicEncoding.Data,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnDecodeResult contents = canonical.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Extensions",
                Data = [0x80, 0x02, 0x07, 0x80, 0x01, 0x01],
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(canonicalEncoding.Data, Is.EqualTo(new byte[]
            {
                0x80, 0x02, 0x07, 0x80, 0x01, 0xff
            }));
            Assert.That(basicEncoding.Data, Is.EqualTo(new byte[]
            {
                0x80, 0x02, 0x06, 0x80, 0x01, 0xff
            }));
            Assert.That(bitmap.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0058"));
            Assert.That(contents.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0023"));
        });
    }

    [Test]
    public void CoerUsesCanonicalRealAndStringFormsAndRejectsUnusedBits()
    {
        AsnTypeDescriptor real = Type(AsnTypeKind.Real, "Number");
        AsnRuntimeValue realValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = 3,
                Radix = 2,
                Exponent = 1
            }
        };
        AsnEncodeResult encodedReal = Codec(
            real,
            AsnEncodingRules.Coer).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Number",
                Value = realValue,
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnDecodeResult realForm = Codec(
            real,
            AsnEncodingRules.Coer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Number",
                Data = [0x05, 0x01, (byte)'3', (byte)'.', (byte)'0', (byte)'0'],
                Options = Options(AsnEncodingRules.Coer)
            });

        AsnTypeDescriptor text = Type(AsnTypeKind.Utf8String, "Text");
        AsnEncodeResult encodedText = Codec(
            text,
            AsnEncodingRules.Coer).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Text",
                Value = Text("Aé"),
                Options = Options(AsnEncodingRules.Coer)
            });
        AsnDecodeResult stringForm = Codec(
            text,
            AsnEncodingRules.Coer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Text",
                Data = [0x81, 0x03, 0x41, 0xc3, 0xa9],
                Options = Options(AsnEncodingRules.Coer)
            });

        AsnTypeDescriptor bits = Type(AsnTypeKind.BitString, "Bits");
        AsnDecodeResult unused = Codec(
            bits,
            AsnEncodingRules.Coer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Bits",
                Data = [0x02, 0x04, 0xff],
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(encodedReal.Data,
                Is.EqualTo(new byte[] { 0x03, 0x80, 0x01, 0x03 }));
            Assert.That(realForm.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0026"));
            Assert.That(encodedText.Data,
                Is.EqualTo(new byte[] { 0x03, 0x41, 0xc3, 0xa9 }));
            Assert.That(stringForm.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0004"));
            Assert.That(unused.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0031"));
        });
    }

    [Test]
    public void CoerRejectsNonzeroUnusedPreambleBits()
    {
        AsnFieldDescriptor optional = Field(
            "optional",
            0,
            Type(AsnTypeKind.Boolean, "Optional"));
        optional.Optional = true;
        AsnTypeDescriptor sequence = Type(
            AsnTypeKind.Sequence,
            "Packet");
        sequence.Fields = [optional];
        sequence.RootFields = [optional];

        AsnDecodeResult result = Codec(
            sequence,
            AsnEncodingRules.Coer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Packet",
                Data = [0x01],
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.That(result.Diagnostics.Single().Code,
            Is.EqualTo("ASNO0051"));
    }

    private static OerSchemaCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        return new OerSchemaCodec(
            new AsnReflectionCatalog
            {
                Assignments =
                [
                    new AsnAssignmentDescriptor
                    {
                        Name = type.Name,
                        QualifiedName = "Module." + type.Name,
                        Type = type
                    }
                ]
            },
            rules);
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false
        };
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            Kind = kind
        };
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

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
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
}
