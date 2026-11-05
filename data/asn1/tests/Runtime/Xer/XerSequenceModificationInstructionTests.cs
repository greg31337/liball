using asn1;

namespace asn1.Tests;

public class XerSequenceModificationInstructionTests
{
    [Test]
    public void DefaultForEmptyRoundTripsScalarAndEmbeddedDefaults()
    {
        AsnTypeDescriptor text = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Text"
            },
            Instruction(AsnInstructionKind.DefaultForEmpty,
                ["AS", "\"missing\""]));
        AsnTypeDescriptor embedded = EmbeddedType(true);
        AsnRuntimeValue embeddedValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("text", SequenceOf(Text("absent"))),
                Missing("item")
            ]
        };

        string scalarXml = EncodeXml(text, Text("missing"));
        XerValueResult scalarDecoded = DecodeXml(text, scalarXml);
        string embeddedXml = EncodeXml(embedded, embeddedValue);
        XerValueResult embeddedDecoded = DecodeXml(embedded, embeddedXml);

        Assert.Multiple(() =>
        {
            Assert.That(scalarXml, Is.EqualTo("<Text/>"));
            Assert.That(scalarDecoded.Value?.TextValue, Is.EqualTo("missing"));
            Assert.That(EncodeXml(text, Text("present")),
                Is.EqualTo("<Text>present</Text>"));
            Assert.That(embeddedXml, Is.EqualTo("<Embedded/>"));
            Assert.That(embeddedDecoded.Value?.Fields[0].Value?.Items[0].TextValue,
                Is.EqualTo("absent"));
            Assert.That(embeddedDecoded.Value?.Fields[1].Present, Is.False);
        });
    }

    [Test]
    public void EmbedValuesInterleavesAndRecoversMixedContent()
    {
        AsnTypeDescriptor type = EmbeddedType(false);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("text", SequenceOf(
                    Text("before"), Text("middle"), Text("after"))),
                Present("item", Integer(7)),
                Present("tail", Text("done"))
            ]
        };

        string xml = EncodeXml(type, value);
        XerValueResult decoded = DecodeXml(type, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<Embedded>before<item>7</item>middle<tail>done</tail>after</Embedded>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Items
                .Select(item => item.TextValue),
                Is.EqualTo(new string[] { "before", "middle", "after" }));
            Assert.That(decoded.Value?.Fields[1].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
        });
    }

    [Test]
    public void UseNilDistinguishesMissingFromPresentEmptyContent()
    {
        AsnTypeDescriptor type = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "Maybe",
                Fields =
                [
                    new AsnFieldDescriptor
                    {
                        Name = "value",
                        Optional = true,
                        Type = new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String }
                    }
                ]
            },
            Instruction(AsnInstructionKind.UseNil, []));
        AsnRuntimeValue missing = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Missing("value")]
        };
        AsnRuntimeValue present = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("value", Text("yes"))]
        };

        string missingXml = EncodeXml(type, missing);
        string presentXml = EncodeXml(type, present);
        XerValueResult decodedMissing = DecodeXml(type, missingXml);
        XerValueResult decodedPresent = DecodeXml(type, presentXml);
        AsnTypeDescriptor choiceType = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "MaybeChoice",
                Fields =
                [
                    new AsnFieldDescriptor
                    {
                        Name = "payload",
                        Optional = true,
                        Type = new AsnTypeDescriptor
                        {
                            Kind = AsnTypeKind.Choice,
                            Fields =
                            [
                                Field("number", new AsnTypeDescriptor
                                {
                                    Kind = AsnTypeKind.Integer
                                })
                            ]
                        }
                    }
                ]
            },
            Instruction(AsnInstructionKind.UseNil, []));
        string choiceXml = EncodeXml(choiceType, new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("payload", new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [Present("number", Integer(9))]
            })]
        });
        XerValueResult decodedChoice = DecodeXml(choiceType, choiceXml);

        Assert.Multiple(() =>
        {
            Assert.That(missingXml, Is.EqualTo(
                "<Maybe xmlns:asn1=\"urn:oid:2.1.5.2.0.1\" asn1:nil=\"true\"/>"));
            Assert.That(presentXml, Is.EqualTo("<Maybe>yes</Maybe>"));
            Assert.That(decodedMissing.Value?.Fields[0].Present, Is.False);
            Assert.That(decodedPresent.Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("yes"));
            Assert.That(choiceXml,
                Is.EqualTo("<MaybeChoice><number>9</number></MaybeChoice>"));
            Assert.That(decodedChoice.Value?.Fields[0].Value?.Fields[0]
                .Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
        });
    }

    [Test]
    public void UseOrderEncodesSemanticComponentOrderAndRecoversCarrier()
    {
        AsnTypeDescriptor order = OrderCarrier("first", "second");
        AsnTypeDescriptor type = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "Ordered",
                Fields =
                [
                    Field("order", order),
                    Field("first", new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }),
                    Field("second", new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String })
                ]
            },
            Instruction(AsnInstructionKind.UseOrder, []));
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("order", SequenceOf(
                    Enumerated("second", 1), Enumerated("first", 0))),
                Present("first", Integer(3)),
                Present("second", Text("two"))
            ]
        };

        string xml = EncodeXml(type, value);
        XerValueResult decoded = DecodeXml(type, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<Ordered><second>two</second><first>3</first></Ordered>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Items
                .Select(item => item.TextValue),
                Is.EqualTo(new string[] { "second", "first" }));
            Assert.That(decoded.Value?.Fields[1].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void UseNilAndUseOrderComposeForOptionalSequenceContent()
    {
        AsnTypeDescriptor order = OrderCarrier("first", "second");
        AsnTypeDescriptor payload = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                Field("first", new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }),
                Field("second", new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String })
            ]
        };
        AsnTypeDescriptor type = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "OrderedMaybe",
                Fields =
                [
                    Field("order", order),
                    new AsnFieldDescriptor
                    {
                        Name = "payload",
                        Optional = true,
                        Type = payload
                    }
                ]
            },
            Instruction(AsnInstructionKind.UseNil, []),
            Instruction(AsnInstructionKind.UseOrder, []));
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("order", SequenceOf(
                    Enumerated("second", 1), Enumerated("first", 0))),
                Present("payload", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("first", Integer(3)),
                        Present("second", Text("two"))
                    ]
                })
            ]
        };

        string xml = EncodeXml(type, value);
        XerValueResult decoded = DecodeXml(type, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<OrderedMaybe><second>two</second><first>3</first></OrderedMaybe>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Items
                .Select(item => item.TextValue),
                Is.EqualTo(new string[] { "second", "first" }));
            Assert.That(decoded.Value?.Fields[1].Value?.Fields[0]
                .Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void PiOrCommentSupportsAllTagAndValueBoundaries()
    {
        string[] positions =
            ["BEFORE-TAG", "BEFORE-VALUE", "AFTER-VALUE", "AFTER-TAG"];
        string[] expected =
        [
            "<!--note--><Number>4</Number>",
            "<Number><!--note-->4</Number>",
            "<Number>4<!--note--></Number>",
            "<Number>4</Number><!--note-->"
        ];

        int index = 0;
        while (index < positions.Length)
        {
            AsnTypeDescriptor type = Instructions(
                new AsnTypeDescriptor
                {
                    Kind = AsnTypeKind.Integer,
                    Name = "Number"
                },
                Instruction(AsnInstructionKind.PiOrComment,
                    ["AS", "\"<!--note-->\"", positions[index]]));
            string xml = EncodeXml(type, Integer(4));
            XerValueResult decoded = DecodeXml(type, xml);

            Assert.That(xml, Is.EqualTo(expected[index]));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(4)));
            index = index + 1;
        }

        AsnTypeDescriptor processingInstruction = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Integer,
                Name = "Number"
            },
            Instruction(AsnInstructionKind.PiOrComment,
                ["AS", "\"<?work now?>\"", "BEFORE-VALUE"]));
        string processingXml = EncodeXml(processingInstruction, Integer(5));
        XerValueResult processingDecoded = DecodeXml(
            processingInstruction,
            processingXml);

        Assert.That(processingXml,
            Is.EqualTo("<Number><?work now?>5</Number>"));
        Assert.That(processingDecoded.Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(5)));
    }

    private static AsnTypeDescriptor EmbeddedType(bool withDefault)
    {
        System.Collections.Generic.List<AsnFieldDescriptor> fields =
            new System.Collections.Generic.List<AsnFieldDescriptor>
            {
                Field("text", new AsnTypeDescriptor
                {
                    Kind = AsnTypeKind.SequenceOf,
                    ElementType = new AsnTypeDescriptor
                    {
                        Kind = AsnTypeKind.Utf8String
                    }
                }),
                new AsnFieldDescriptor
                {
                    Name = "item",
                    Optional = withDefault,
                    Type = new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }
                }
            };
        if (!withDefault)
        {
            fields.Add(Field("tail", new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String
            }));
        }

        System.Collections.Generic.List<AsnEncodingInstruction> instructions =
            new System.Collections.Generic.List<AsnEncodingInstruction>
            {
                Instruction(AsnInstructionKind.EmbedValues, [])
            };
        if (withDefault)
        {
            instructions.Add(Instruction(
                AsnInstructionKind.DefaultForEmpty,
                ["AS", "\"absent\""]));
        }

        return Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "Embedded",
                Fields = fields.ToArray()
            },
            instructions.ToArray());
    }

    private static AsnTypeDescriptor OrderCarrier(params string[] names)
    {
        AsnValueDescriptor[] values = new AsnValueDescriptor[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            values[index] = new AsnValueDescriptor
            {
                Name = names[index],
                Value = new AsnValue
                {
                    HasIntegerValue = true,
                    IntegerValue = index
                }
            };
            index = index + 1;
        }

        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Enumerated,
                NamedValues = values
            }
        };
    }

    private static AsnTypeDescriptor Instructions(
        AsnTypeDescriptor type,
        params AsnEncodingInstruction[] instructions)
    {
        type.Encodings = [new AsnEncodingDescriptor
        {
            EncodingRules = AsnEncodingRules.ExtendedXer,
            EncodingReference = "XER",
            Instructions = instructions
        }];
        return type;
    }

    private static AsnEncodingInstruction Instruction(
        AsnInstructionKind kind,
        string[] arguments)
    {
        return new AsnEncodingInstruction
        {
            Kind = kind,
            Name = kind.ToString().ToUpperInvariant(),
            Arguments = arguments
        };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor { Name = name, Type = type };
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

    private static AsnRuntimeFieldValue Missing(string name)
    {
        return new AsnRuntimeFieldValue { Name = name };
    }

    private static AsnRuntimeValue SequenceOf(params AsnRuntimeValue[] items)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = items
        };
    }

    private static AsnRuntimeValue Enumerated(string name, int number)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = name,
            HasIntegerValue = true,
            IntegerValue = number
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static XerValueResult EncodeResult(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        XerValueCodec codec = new XerValueCodec(Limits());
        codec.Extended = true;
        return codec.EncodeValue(type, value);
    }

    private static string EncodeXml(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnResourceLimits limits = Limits();
        XerValueResult encoded = EncodeResult(type, value);
        Assert.That(encoded.Success, Is.True, encoded.ErrorMessage);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, limits);
        foreach (XerXmlToken token in encoded.Tokens)
        {
            XerXmlWriteResult written = writer.WriteToken(token);
            Assert.That(written.Success, Is.True, written.ErrorMessage);
        }

        Assert.That(writer.CompleteWriter().Success, Is.True);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static XerValueResult DecodeXml(
        AsnTypeDescriptor type,
        string xml)
    {
        AsnResourceLimits limits = Limits();
        XerXmlReadResult read = new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
        Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done),
            read.ErrorMessage);
        XerValueCodec codec = new XerValueCodec(limits);
        codec.Extended = true;
        return codec.DecodeValue(type, read.Tokens, 0, read.Tokens.Length);
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 64,
            MaximumCollectionElements = 1024,
            MaximumTextCharacters = 65536,
            MaximumOpenTypeBytes = 65536,
            MaximumIntegerBits = 65536,
            MaximumTotalAllocationBytes = 1048576
        };
    }
}
