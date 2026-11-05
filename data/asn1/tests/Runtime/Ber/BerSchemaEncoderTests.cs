using asn1;

namespace asn1.Tests;

public class BerSchemaEncoderTests
{
    [Test]
    public void RequestEncodingResolvesTheSchemaTypeAndWritesPrimitiveData()
    {
        AsnTypeDescriptor integer = IntegerType("type.number", "Number");
        BerSchemaEncoder encoder = new BerSchemaEncoder(CatalogWithType(
            "Module.Number",
            integer));

        AsnEncodeResult result = encoder.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Number",
            Value = new AsnRuntimeValue
            {
                HasIntegerValue = true,
                IntegerValue = 42
            },
            Options = Options(AsnEncodingRules.Ber)
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
            Assert.That(result.WrittenCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void StructuredEncodingAppliesTagsAndCanonicalDefaultOmission()
    {
        AsnFieldDescriptor id = Field("id", IntegerType("type.id", "Id"), 0);
        AsnFieldDescriptor note = Field(
            "note",
            StringType("type.note", "Note"),
            1);
        note.Optional = true;
        AsnFieldDescriptor count = Field(
            "count",
            IntegerType("type.count", "Count"),
            2);
        count.HasDefaultValue = true;
        count.DefaultValue = IntegerDefault(3);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [id, note, count]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("id", IntegerValue(5)),
                Present("count", IntegerValue(3))
            ]
        };
        BerSchemaEncoder encoder = EmptyEncoder();

        byte[] ber = Write(encoder.EncodeTlv(
            sequence,
            value,
            Options(AsnEncodingRules.Ber)),
            AsnEncodingRules.Ber);
        byte[] der = Write(encoder.EncodeTlv(
            sequence,
            value,
            Options(AsnEncodingRules.Der)),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(ber, Is.EqualTo(new byte[]
            {
                0x30, 0x06, 0x80, 0x01, 0x05, 0x82, 0x01, 0x03
            }));
            Assert.That(der, Is.EqualTo(new byte[]
            {
                0x30, 0x03, 0x80, 0x01, 0x05
            }));
        });
    }

    [Test]
    public void CanonicalDefaultComparisonUsesTheCompleteAbstractValue()
    {
        AsnFieldDescriptor bits = Field(
            "bits",
            new AsnTypeDescriptor { Kind = AsnTypeKind.BitString },
            0);
        bits.HasDefaultValue = true;
        bits.DefaultValue = new AsnValueDescriptor
        {
            Kind = AsnValueKind.BitString,
            Value = new AsnValue
            {
                Kind = AsnValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0x80],
                    BitLength = 1
                }
            }
        };
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [bits]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("bits", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.BitString,
                    BitStringValue = new AsnBitStringValue
                    {
                        Bytes = [0x00],
                        BitLength = 1
                    }
                })
            ]
        };

        byte[] encoded = Write(EmptyEncoder().EncodeTlv(
            sequence,
            value,
            Options(AsnEncodingRules.Der)),
            AsnEncodingRules.Der);

        Assert.That(encoded, Is.EqualTo(new byte[]
        {
            0x30, 0x04, 0x80, 0x02, 0x07, 0x00
        }));
    }

    [Test]
    public void ChoiceSetAndCollectionsEncodeFromRuntimeShape()
    {
        AsnFieldDescriptor number = Field(
            "number",
            IntegerType("type.number", "Number"),
            0);
        AsnFieldDescriptor text = Field(
            "text",
            StringType("type.text", "Text"),
            1);
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [number, text]
        };
        AsnTypeDescriptor set = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Fields = [number, text]
        };
        AsnTypeDescriptor list = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = IntegerType("type.item", "Item")
        };
        BerSchemaEncoder encoder = EmptyEncoder();

        BerSchemaEncodeResult encodedChoice = encoder.EncodeTlv(
            choice,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [Present("text", TextValue("ok"))]
            },
            Options(AsnEncodingRules.Ber));
        BerSchemaEncodeResult encodedSet = encoder.EncodeTlv(
            set,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Set,
                Fields =
                [
                    Present("text", TextValue("set")),
                    Present("number", IntegerValue(7))
                ]
            },
            Options(AsnEncodingRules.Ber));
        BerSchemaEncodeResult encodedList = encoder.EncodeTlv(
            list,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = [IntegerValue(1), IntegerValue(2)]
            },
            Options(AsnEncodingRules.Ber));

        Assert.Multiple(() =>
        {
            Assert.That(encodedChoice.Tlv?.Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(encodedChoice.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(encodedSet.Tlv?.Children.Select(item => item.Tag.Number),
                Is.EqualTo(new System.Numerics.BigInteger[] { 0, 1 }));
            Assert.That(encodedList.Tlv?.Children.Select(item => item.Content[0]),
                Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void DerSchemaEncodingCanonicalizesSetAndSetOfOrder()
    {
        AsnFieldDescriptor high = Field(
            "high",
            IntegerType("type.high", "High"),
            2);
        AsnFieldDescriptor low = Field(
            "low",
            IntegerType("type.low", "Low"),
            0);
        AsnTypeDescriptor set = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Fields = [high, low]
        };
        AsnTypeDescriptor setOf = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SetOf,
            ElementType = IntegerType("type.item", "Item")
        };
        BerSchemaEncoder encoder = EmptyEncoder();

        byte[] encodedSet = Write(encoder.EncodeTlv(
            set,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Set,
                Fields =
                [
                    Present("high", IntegerValue(2)),
                    Present("low", IntegerValue(1))
                ]
            },
            Options(AsnEncodingRules.Der)),
            AsnEncodingRules.Der);
        byte[] encodedSetOf = Write(encoder.EncodeTlv(
            setOf,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SetOf,
                Items = [IntegerValue(2), IntegerValue(1)]
            },
            Options(AsnEncodingRules.Der)),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSet, Is.EqualTo(new byte[]
            {
                0x31, 0x06,
                0x80, 0x01, 0x01,
                0x82, 0x01, 0x02
            }));
            Assert.That(encodedSetOf, Is.EqualTo(new byte[]
            {
                0x31, 0x06,
                0x02, 0x01, 0x01,
                0x02, 0x01, 0x02
            }));
        });
    }

    [Test]
    public void CerSchemaEncodingSegmentsStringsOmitsDefaultsAndUsesEoc()
    {
        AsnFieldDescriptor data = Field(
            "data",
            new AsnTypeDescriptor { Kind = AsnTypeKind.OctetString },
            0);
        AsnFieldDescriptor count = Field(
            "count",
            IntegerType("type.count", "Count"),
            1);
        count.HasDefaultValue = true;
        count.DefaultValue = IntegerDefault(3);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [data, count]
        };
        BerSchemaEncodeResult encoded = EmptyEncoder().EncodeTlv(
            sequence,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("data", new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.OctetString,
                        Octets = new byte[1001]
                    }),
                    Present("count", IntegerValue(3))
                ]
            },
            Options(AsnEncodingRules.Cer));
        BerTlv root = encoded.Tlv ?? throw new InvalidOperationException();
        byte[] wire = Write(encoded, AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            Assert.That(root.IndefiniteLength, Is.True);
            Assert.That(root.Children.Length, Is.EqualTo(1));
            Assert.That(root.Children[0].Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(root.Children[0].Constructed, Is.True);
            Assert.That(root.Children[0].IndefiniteLength, Is.True);
            Assert.That(root.Children[0].Children.Select(
                child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(wire.Take(8), Is.EqualTo(new byte[]
            {
                0x30, 0x80,
                0xa0, 0x80,
                0x04, 0x82, 0x03, 0xe8
            }));
            Assert.That(wire.TakeLast(4),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x00 }));
        });
    }

    [Test]
    public void SelectedIndefiniteLengthsIncludeExplicitWrappersAndRejectDer()
    {
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                Field("id", IntegerType("type.id", "Id"), 0)
            ],
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Explicit,
                    Number = 5
                }
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("id", IntegerValue(1))]
        };
        AsnCodecOptions berOptions = Options(AsnEncodingRules.Ber);
        berOptions.UseIndefiniteLengths = true;
        AsnCodecOptions derOptions = Options(AsnEncodingRules.Der);
        derOptions.UseIndefiniteLengths = true;
        BerSchemaEncoder encoder = EmptyEncoder();

        BerSchemaEncodeResult ber = encoder.EncodeTlv(
            sequence,
            value,
            berOptions);
        BerSchemaEncodeResult der = encoder.EncodeTlv(
            sequence,
            value,
            derOptions);

        Assert.Multiple(() =>
        {
            Assert.That(Write(ber, AsnEncodingRules.Ber), Is.EqualTo(new byte[]
            {
                0xa5, 0x80, 0x30, 0x80, 0x80, 0x01, 0x01,
                0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(der.Success, Is.False);
            Assert.That(der.Diagnostics.Single().Code, Is.EqualTo("ASNE0075"));
        });
    }

    [Test]
    public void SpanAndStreamSinksReportExactProgressAndDoNotPartiallyWrite()
    {
        AsnTypeDescriptor integer = IntegerType("type.number", "Number");
        BerSchemaEncoder encoder = new BerSchemaEncoder(CatalogWithType(
            "Module.Number",
            integer));
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Module.Number",
            Value = IntegerValue(9),
            Options = Options(AsnEncodingRules.Ber)
        };
        byte[] destination = [0xcc, 0xcc, 0xcc];
        AsnEncodeResult span = encoder.EncodeSpan(request, destination);
        byte[] tooSmall = [0xcc, 0xcc];
        AsnEncodeResult failedSpan = encoder.EncodeSpan(request, tooSmall);
        MemoryStream output = new MemoryStream();
        AsnEncodeResult stream = encoder.EncodeStream(request, output);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[] { 0x02, 0x01, 0x09 }));
            Assert.That(span.StreamProgress?.WrittenCount, Is.EqualTo(3));
            Assert.That(span.StreamProgress?.Complete, Is.True);
            Assert.That(failedSpan.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0073"));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 0xcc, 0xcc }));
            Assert.That(output.ToArray(),
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x09 }));
            Assert.That(stream.StreamProgress?.Complete, Is.True);
        });
    }

    [Test]
    public void RequiredChoiceAndUnknownShapeErrorsAreStable()
    {
        AsnFieldDescriptor required = Field(
            "required",
            IntegerType("type.required", "Required"),
            0);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [required]
        };
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [required]
        };
        BerSchemaEncoder encoder = EmptyEncoder();

        BerSchemaEncodeResult missing = encoder.EncodeTlv(
            sequence,
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Sequence },
            Options(AsnEncodingRules.Ber));
        BerSchemaEncodeResult noChoice = encoder.EncodeTlv(
            choice,
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Choice },
            Options(AsnEncodingRules.Ber));
        BerSchemaEncodeResult extra = encoder.EncodeTlv(
            sequence,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = [Present("other", IntegerValue(1))]
            },
            Options(AsnEncodingRules.Ber));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("ASNE0062"));
            Assert.That(noChoice.Diagnostics.Single().Code, Is.EqualTo("ASNE0055"));
            Assert.That(extra.Diagnostics.Single().Code, Is.EqualTo("ASNE0096"));
        });
    }

    [Test]
    public void OpenTypeTablesEncodeResolvedValuesAndPreservedPayloads()
    {
        AsnTypeDescriptor payloadType = IntegerType("type.payload", "Payload");
        AsnTypeDescriptor openType = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType,
            ObjectTables =
            [
                new AsnObjectTableDescriptor
                {
                    Entries =
                    [
                        new AsnObjectTableEntryDescriptor
                        {
                            Key = IntegerDefault(7),
                            Type = payloadType
                        }
                    ]
                }
            ]
        };
        BerSchemaEncoder encoder = EmptyEncoder();
        AsnRuntimeValue resolved = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = "type.payload",
                Value = IntegerValue(42),
                DispatchKey = IntegerValue(7)
            }
        };
        AsnRuntimeValue unresolved = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                EncodingRules = AsnEncodingRules.Ber,
                HasEncodingRules = true,
                HasOriginalData = true,
                OriginalData = [0x0c, 0x01, 0x41]
            }
        };
        AsnCodecOptions preserve = Options(AsnEncodingRules.Ber);
        preserve.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;

        byte[] resolvedData = Write(encoder.EncodeTlv(
            openType,
            resolved,
            Options(AsnEncodingRules.Ber)),
            AsnEncodingRules.Ber);
        byte[] unresolvedData = Write(encoder.EncodeTlv(
            openType,
            unresolved,
            preserve),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(resolvedData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
            Assert.That(unresolvedData,
                Is.EqualTo(new byte[] { 0x0c, 0x01, 0x41 }));
        });
    }

    [Test]
    public void ConstraintValidationCanBeRequiredOrExplicitlyDisabled()
    {
        AsnTypeDescriptor integer = IntegerType("type.number", "Number");
        integer.Constraints =
        [
            new AsnConstraintDescriptor
            {
                StableId = "constraint.application",
                Kind = AsnConstraintKind.UserDefined,
                Constraint = new AsnConstraint
                {
                    Kind = AsnConstraintKind.UserDefined,
                    RequiresApplicationValidation = true
                }
            }
        ];
        BerSchemaEncoder encoder = EmptyEncoder();
        AsnCodecOptions validate = Options(AsnEncodingRules.Ber);
        AsnCodecOptions skip = Options(AsnEncodingRules.Ber);
        skip.ValidateValues = false;

        BerSchemaEncodeResult rejected = encoder.EncodeTlv(
            integer,
            IntegerValue(1),
            validate);
        BerSchemaEncodeResult accepted = encoder.EncodeTlv(
            integer,
            IntegerValue(1),
            skip);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("ASNC0032"));
            Assert.That(accepted.Success, Is.True);
        });
    }

    private static BerSchemaEncoder EmptyEncoder()
    {
        return new BerSchemaEncoder(new AsnReflectionCatalog());
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject
        };
    }

    private static AsnReflectionCatalog CatalogWithType(
        string qualifiedName,
        AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Name = type.Name,
                    QualifiedName = qualifiedName,
                    Type = type
                }
            ]
        };
    }

    private static AsnTypeDescriptor IntegerType(string stableId, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = stableId,
            Name = name,
            Kind = AsnTypeKind.Integer
        };
    }

    private static AsnTypeDescriptor StringType(string stableId, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = stableId,
            Name = name,
            Kind = AsnTypeKind.Utf8String
        };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int contextTag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = contextTag
                }
            ]
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

    private static AsnRuntimeValue TextValue(string value)
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

    private static AsnValueDescriptor IntegerDefault(int value)
    {
        return new AsnValueDescriptor
        {
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
        };
    }

    private static byte[] Write(
        BerSchemaEncodeResult result,
        AsnEncodingRules rules)
    {
        BerTlv tlv = result.Tlv ?? throw new InvalidOperationException(
            string.Join(" | ", result.Diagnostics.Select(item =>
                item.Code + ":" + item.Message)));
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = rules
            });
        return written.Success
            ? written.Data
            : throw new InvalidOperationException(string.Join(
                " | ",
                written.Diagnostics.Select(item => item.Code + ":" + item.Message)));
    }
}
