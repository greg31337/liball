using asn1;

namespace asn1.Tests;

public class BerSchemaWrapperVectorTests
{
    [Test]
    public void EveryTransparentSchemaTypeRoundTripsItsEffectiveValueAndTags()
    {
        AsnTypeDescriptor integer = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer
        };
        AsnTypeDescriptor[] types =
        [
            Wrapper(AsnTypeKind.Defined, integer, []),
            Wrapper(AsnTypeKind.Selection, integer, []),
            Wrapper(AsnTypeKind.Prefixed, integer,
            [
                Tag(4, AsnTaggingMode.Implicit),
                UniversalIntegerTag()
            ]),
            Wrapper(AsnTypeKind.Tagged, integer,
            [
                Tag(3, AsnTaggingMode.Explicit),
                UniversalIntegerTag()
            ])
        ];
        byte[][] expected =
        [
            [0x02, 0x01, 0x2a],
            [0x02, 0x01, 0x2a],
            [0x84, 0x01, 0x2a],
            [0xa3, 0x03, 0x02, 0x01, 0x2a]
        ];
        BerSchemaEncoder encoder = EmptyEncoder();
        BerSchemaDecoder decoder = EmptyDecoder();

        int index = 0;
        while (index < types.Length)
        {
            BerSchemaEncodeResult encoded = encoder.EncodeTlv(
                types[index],
                IntegerValue(42),
                Options());
            byte[] data = Write(encoded);
            BerReadResult read = Read(data);
            BerSchemaDecodeResult decoded = decoder.DecodeTlv(
                types[index],
                read.Tlv ?? throw new InvalidOperationException(),
                Options());

            Assert.That(encoded.Success, Is.True, Format(encoded.Diagnostics));
            Assert.That(data, Is.EqualTo(expected[index]));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Success, Is.True, Format(decoded.Diagnostics));
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Integer));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            index++;
        }
    }

    [Test]
    public void WrappedConstructedAlternativeNormalizesAndParticipatesInSetDispatch()
    {
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields =
            [
                Field("number", AsnTypeKind.Integer, 0),
                Field("text", AsnTypeKind.Utf8String, 1)
            ]
        };
        AsnTypeDescriptor wrappedChoice = Wrapper(
            AsnTypeKind.Defined,
            choice,
            []);
        AsnTypeDescriptor set = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    Name = "payload",
                    Type = wrappedChoice
                }
            ]
        };
        AsnRuntimeValue source = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Set,
            Fields =
            [
                Present("payload", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields =
                    [
                        Present("text", new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.CharacterString,
                            TextValue = "x"
                        })
                    ]
                })
            ]
        };
        AsnRuntimeValue normalized = new ValueAccess().NormalizeValue(
            source,
            set,
            new AsnReflectionCatalog());

        byte[] data = Write(EmptyEncoder().EncodeTlv(
            set,
            normalized,
            Options()));
        BerSchemaDecodeResult decoded = EmptyDecoder().DecodeTlv(
            set,
            Read(data).Tlv ?? throw new InvalidOperationException(),
            Options());

        Assert.Multiple(() =>
        {
            Assert.That(data, Is.EqualTo(new byte[]
            {
                0x31, 0x03, 0x81, 0x01, 0x78
            }));
            Assert.That(decoded.Success, Is.True, Format(decoded.Diagnostics));
            Assert.That(decoded.Value?.Fields[0].Value?.Fields[0].Name,
                Is.EqualTo("text"));
            Assert.That(decoded.Value?.Fields[0].Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("x"));
        });
    }

    [Test]
    public void InvalidTransparentSchemaChainsReturnDiagnostics()
    {
        AsnTypeDescriptor missing = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Defined
        };
        AsnTypeDescriptor cyclic = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Tagged
        };
        cyclic.ElementType = cyclic;

        BerSchemaEncodeResult missingResult = EmptyEncoder().EncodeTlv(
            missing,
            IntegerValue(1),
            Options());
        BerSchemaEncodeResult cyclicResult = EmptyEncoder().EncodeTlv(
            cyclic,
            IntegerValue(1),
            Options());

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(missingResult.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0077"));
            Assert.That(cyclicResult.Success, Is.False);
            Assert.That(cyclicResult.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0077"));
        });
    }

    private static AsnTypeDescriptor Wrapper(
        AsnTypeKind kind,
        AsnTypeDescriptor elementType,
        AsnTagDescriptor[] tags)
    {
        return new AsnTypeDescriptor
        {
            Kind = kind,
            ElementType = elementType,
            Tags = tags
        };
    }

    private static AsnTagDescriptor Tag(int number, AsnTaggingMode mode)
    {
        return new AsnTagDescriptor
        {
            Class = AsnTagClass.ContextSpecific,
            Mode = mode,
            Number = number
        };
    }

    private static AsnTagDescriptor UniversalIntegerTag()
    {
        return new AsnTagDescriptor
        {
            Class = AsnTagClass.Universal,
            Mode = AsnTaggingMode.Implicit,
            Number = AsnUniversalTags.Integer,
            Universal = true
        };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeKind kind,
        int tag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = new AsnTypeDescriptor { Kind = kind },
            Tags = [Tag(tag, AsnTaggingMode.Implicit)]
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

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnCodecOptions Options()
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Ber,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject
        };
    }

    private static BerSchemaEncoder EmptyEncoder()
    {
        return new BerSchemaEncoder(new AsnReflectionCatalog());
    }

    private static BerSchemaDecoder EmptyDecoder()
    {
        return new BerSchemaDecoder(new AsnReflectionCatalog());
    }

    private static byte[] Write(BerSchemaEncodeResult result)
    {
        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = result.Tlv,
                EncodingRules = AsnEncodingRules.Ber
            });
        Assert.That(written.Success, Is.True, Format(written.Diagnostics));
        return written.Data;
    }

    private static BerReadResult Read(byte[] data)
    {
        return new BerTlvReaderImplementation(new AsnResourceLimits()).Read(
            new BerReadRequest
            {
                Data = data,
                EncodingRules = AsnEncodingRules.Ber
            });
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }
}
