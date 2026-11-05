using asn1;

namespace asn1.Tests;

public class XerExtendedRepresentationInstructionTests
{
    [Test]
    public void NameNamespaceAttributeTextAndListRoundTripTogether()
    {
        AsnTypeDescriptor active = Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Boolean },
            Instruction(AsnInstructionKind.Attribute, "ATTRIBUTE", [], []),
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "\"enabled\""], []),
            Instruction(AsnInstructionKind.Namespace, "NAMESPACE",
                ["AS", "\"urn:meta\"", "PREFIX", "\"m\""], []),
            Instruction(AsnInstructionKind.Text, "TEXT",
                ["AS", "\"yes\""], ["true"]));
        AsnTypeDescriptor values = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SequenceOf,
                ElementType = new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }
            },
            Instruction(AsnInstructionKind.List, "LIST", [], []),
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "\"numbers\""], []));
        AsnTypeDescriptor order = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "Order",
                Fields = [Field("active", active), Field("values", values)]
            },
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "LOWERCASED"], []),
            Instruction(AsnInstructionKind.Namespace, "NAMESPACE",
                ["AS", "\"urn:order\"", "PREFIX", "\"o\""], []));
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("active", Boolean(true)),
                Present("values", SequenceOf(Integer(1), Integer(20), Integer(-3)))
            ]
        };

        string xml = EncodeXml(order, value);
        XerValueResult decoded = DecodeXml(order, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<o:order xmlns:o=\"urn:order\" xmlns:m=\"urn:meta\" "
                    + "m:enabled=\"yes\"><numbers>1 20 -3</numbers></o:order>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.BooleanValue, Is.True);
            Assert.That(decoded.Value?.Fields[1].Value?.Items
                .Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 20, -3 }));
        });
    }

    [Test]
    public void UntaggedStructuredComponentKeepsFollowingSiblingBoundary()
    {
        AsnTypeDescriptor payload = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Fields = [Field("code", new AsnTypeDescriptor
                {
                    Kind = AsnTypeKind.Integer
                })]
            },
            Instruction(AsnInstructionKind.Untagged, "UNTAGGED", [], []));
        AsnTypeDescriptor envelope = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Envelope",
            Fields =
            [
                Field("payload", payload),
                Field("tail", new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String })
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("payload", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields = [Present("code", Integer(7))]
                }),
                Present("tail", Text("done"))
            ]
        };

        string xml = EncodeXml(envelope, value);
        XerValueResult decoded = DecodeXml(envelope, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<Envelope><code>7</code><tail>done</tail></Envelope>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Fields[0]
                .Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(decoded.Value?.Fields[1].Value?.TextValue,
                Is.EqualTo("done"));
        });
    }

    [Test]
    public void AttributeAndElementCollisionsUseExpandedNames()
    {
        AsnTypeDescriptor first = AttributeType("same", "urn:one", "a");
        AsnTypeDescriptor second = AttributeType("same", "urn:one", "b");
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Container",
            Fields = [Field("first", first), Field("second", second)]
        };
        XerValueResult attributeCollision = EncodeResult(
            container,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("first", Integer(1)),
                    Present("second", Integer(2))
                ]
            });

        AsnTypeDescriptor renamed = Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "\"same\""], []));
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Name = "Choice",
            Fields = [Field("one", renamed), Field("two", renamed)]
        };
        XerValueResult elementCollision = DecodeXml(
            choice,
            "<Choice><same>1</same></Choice>");

        Assert.Multiple(() =>
        {
            Assert.That(attributeCollision.ErrorCode, Is.EqualTo("ASNXER0334"));
            Assert.That(elementCollision.ErrorCode, Is.EqualTo("ASNXER0333"));
        });
    }

    [Test]
    public void InvalidNamesNamespacesAndListItemsAreRejected()
    {
        AsnTypeDescriptor badName = Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer, Name = "Number" },
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "\"bad:name\""], []));
        AsnTypeDescriptor badNamespace = Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer, Name = "Number" },
            Instruction(AsnInstructionKind.Namespace, "NAMESPACE",
                ["AS", "\"urn:x\"", "PREFIX", "\"bad:name\""], []));
        AsnTypeDescriptor list = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SequenceOf,
                Name = "Words",
                ElementType = new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String }
            },
            Instruction(AsnInstructionKind.List, "LIST", [], []));
        AsnTypeDescriptor integerList = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SequenceOf,
                Name = "Numbers",
                ElementType = new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }
            },
            Instruction(AsnInstructionKind.List, "LIST", [], []));

        Assert.Multiple(() =>
        {
            Assert.That(EncodeResult(badName, Integer(1)).ErrorCode,
                Is.EqualTo("ASNXER0320"));
            Assert.That(EncodeResult(badNamespace, Integer(1)).ErrorCode,
                Is.EqualTo("ASNXER0320"));
            Assert.That(EncodeResult(list, SequenceOf(Text("two words"))).ErrorCode,
                Is.EqualTo("ASNXER0325"));
            Assert.That(DecodeXml(integerList,
                "<Numbers>1 nope</Numbers>").Success, Is.False);
        });
    }

    [Test]
    public void TextMapsBooleanNamedIntegerAndNamedBitsInElementContent()
    {
        AsnTypeDescriptor flag = Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Boolean, Name = "Flag" },
            Instruction(AsnInstructionKind.Text, "TEXT",
                ["AS", "\"ON\""], ["true"]));
        AsnTypeDescriptor count = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Integer,
                Name = "Count",
                NamedValues = [Named("one", 1)]
            },
            Instruction(AsnInstructionKind.Text, "TEXT",
                ["AS", "\"single\""], ["one"]));
        AsnTypeDescriptor bits = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.BitString,
                Name = "Bits",
                NamedValues = [Named("red", 0), Named("blue", 2)]
            },
            Instruction(AsnInstructionKind.Text, "TEXT",
                ["AS", "\"R\""], ["red"]));
        AsnRuntimeValue bitValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = [0xa0],
                BitLength = 3
            }
        };

        XerValueResult decodedFlag = DecodeXml(flag, EncodeXml(flag, Boolean(true)));
        XerValueResult decodedCount = DecodeXml(count, EncodeXml(count, Integer(1)));
        string bitXml = EncodeXml(bits, bitValue);
        XerValueResult decodedBits = DecodeXml(bits, bitXml);

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(flag, Boolean(true)), Is.EqualTo("<Flag>ON</Flag>"));
            Assert.That(decodedFlag.Value?.BooleanValue, Is.True);
            Assert.That(EncodeXml(count, Integer(1)),
                Is.EqualTo("<Count>single</Count>"));
            Assert.That(decodedCount.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(bitXml, Is.EqualTo("<Bits>R blue</Bits>"));
            Assert.That(decodedBits.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(decodedBits.Value?.BitStringValue?.BitLength,
                Is.EqualTo(3));
        });
    }

    private static AsnTypeDescriptor AttributeType(
        string name,
        string namespaceUri,
        string prefix)
    {
        return Instructions(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            Instruction(AsnInstructionKind.Attribute, "ATTRIBUTE", [], []),
            Instruction(AsnInstructionKind.Name, "NAME",
                ["AS", "\"" + name + "\""], []),
            Instruction(AsnInstructionKind.Namespace, "NAMESPACE",
                ["AS", "\"" + namespaceUri + "\"", "PREFIX",
                    "\"" + prefix + "\""], []));
    }

    private static AsnValueDescriptor Named(string name, int value)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                HasIntegerValue = true,
                IntegerValue = value
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
        string name,
        string[] arguments,
        string[] targets)
    {
        return new AsnEncodingInstruction
        {
            Kind = kind,
            Name = name,
            Arguments = arguments,
            Targets = targets
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
            XerXmlWriteResult result = writer.WriteToken(token);
            Assert.That(result.Success, Is.True,
                result.ErrorMessage + " token=" + token.Kind + " name="
                    + token.Name + " prefix=" + token.Prefix + " uri="
                    + token.NamespaceUri);
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

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
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

    private static AsnRuntimeValue SequenceOf(params AsnRuntimeValue[] values)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = values
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

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 64,
            MaximumCollectionElements = 1024,
            MaximumTextCharacters = 65536,
            MaximumOpenTypeBytes = 65536,
            MaximumTotalAllocationBytes = 1048576
        };
    }
}
