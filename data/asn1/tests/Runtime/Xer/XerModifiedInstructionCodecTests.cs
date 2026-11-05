using asn1;

namespace asn1.Tests;

public class XerModifiedInstructionCodecTests
{
    [Test]
    public void Base64RoundTripsOctetsAndUtf8StringsAndRejectsMalformedText()
    {
        AsnTypeDescriptor octets = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.OctetString,
                Name = "Payload"
            },
            AsnInstructionKind.Base64);
        AsnTypeDescriptor text = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Text"
            },
            AsnInstructionKind.Base64);
        AsnTypeDescriptor open = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.OpenType,
                Name = "Open"
            },
            AsnInstructionKind.Base64);
        AsnRuntimeValue octetValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [0, 1, 254, 255]
        };

        string octetXml = EncodeXml(octets, octetValue);
        XerValueResult decodedOctets = DecodeXml(
            octets,
            "<Payload>AAH+\n/w==</Payload>");
        string textXml = EncodeXml(text, Text("hé"));
        XerValueResult decodedText = DecodeXml(text, textXml);
        XerValueResult malformed = DecodeXml(
            octets,
            "<Payload>not*base64</Payload>");
        string openXml = EncodeXml(open, new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.ExtendedXer,
                HasOriginalData = true,
                OriginalData = [1, 2, 3]
            }
        });
        XerValueResult decodedOpen = DecodeXml(open, openXml);

        Assert.Multiple(() =>
        {
            Assert.That(octetXml, Is.EqualTo("<Payload>AAH+/w==</Payload>"));
            Assert.That(decodedOctets.Success, Is.True, decodedOctets.ErrorMessage);
            Assert.That(decodedOctets.Value?.Octets,
                Is.EqualTo(new byte[] { 0, 1, 254, 255 }));
            Assert.That(textXml, Is.EqualTo("<Text>aMOp</Text>"));
            Assert.That(decodedText.Value?.TextValue, Is.EqualTo("hé"));
            Assert.That(malformed.ErrorCode, Is.EqualTo("ASNXER0341"));
            Assert.That(openXml, Is.EqualTo("<Open>AQID</Open>"));
            Assert.That(decodedOpen.Value?.OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void DecimalUsesPlainBaseTenAndTreatsNegativeZeroAsPlusZero()
    {
        AsnTypeDescriptor type = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Real,
                Name = "Amount"
            },
            AsnInstructionKind.Decimal);
        AsnRuntimeValue value = Real(12345, -2);

        string xml = EncodeXml(type, value);
        XerValueResult decoded = DecodeXml(type, "<Amount>000123.4500</Amount>");
        XerValueResult negativeZero = DecodeXml(type, "<Amount>-000.00</Amount>");
        XerValueResult exponent = DecodeXml(type, "<Amount>1E2</Amount>");
        XerValueResult forbidden = EncodeResult(type, new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Normalized = true,
                Kind = AsnRealKind.MinusZero
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo("<Amount>123.45</Amount>"));
            Assert.That(decoded.Value?.RealValue?.DecimalCoefficient,
                Is.EqualTo(new System.Numerics.BigInteger(12345)));
            Assert.That(decoded.Value?.RealValue?.DecimalExponent,
                Is.EqualTo(new System.Numerics.BigInteger(-2)));
            Assert.That(negativeZero.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.Zero));
            Assert.That(exponent.ErrorCode, Is.EqualTo("ASNXER0344"));
            Assert.That(forbidden.ErrorCode, Is.EqualTo("ASNXER0343"));
        });
    }

    [Test]
    public void UseNumberRoundTripsEnumerationNumbers()
    {
        AsnTypeDescriptor type = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Enumerated,
                Name = "State",
                NamedValues = [Named("off", -2), Named("on", 7)]
            },
            AsnInstructionKind.UseNumber);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = "on",
            HasIntegerValue = true,
            IntegerValue = 7
        };

        string xml = EncodeXml(type, value);
        XerValueResult decoded = DecodeXml(type, "<State>-2</State>");
        XerValueResult unknown = DecodeXml(type, "<State>8</State>");

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo("<State>7</State>"));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo("off"));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(-2)));
            Assert.That(unknown.ErrorCode, Is.EqualTo("ASNXER0346"));
        });
    }

    [Test]
    public void UseQNameRoundTripsQualifiedAndUnqualifiedNamesIncludingAttributes()
    {
        AsnTypeDescriptor qname = QNameType();
        AsnRuntimeValue qualified = QNameValue("urn:item", "part");

        string qualifiedXml = EncodeXml(qname, qualified);
        XerValueResult decodedQualified = DecodeXml(qname, qualifiedXml);
        XerValueResult decodedUnqualified = DecodeXml(
            qname,
            "<QName>local</QName>");

        AsnTypeDescriptor attributeQName = QNameType();
        attributeQName.Encodings[0].Instructions =
        [
            Instruction(AsnInstructionKind.UseQName, []),
            Instruction(AsnInstructionKind.Attribute, []),
            Instruction(AsnInstructionKind.Name, ["AS", "\"kind\""])
        ];
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Container",
            Fields = [Field("value", attributeQName)]
        };
        string attributeXml = EncodeXml(container, new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("value", qualified)]
        });
        XerValueResult decodedAttribute = DecodeXml(container, attributeXml);

        Assert.Multiple(() =>
        {
            Assert.That(qualifiedXml, Is.EqualTo(
                "<QName xmlns:ns1=\"urn:item\">ns1:part</QName>"));
            Assert.That(decodedQualified.Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("urn:item"));
            Assert.That(decodedQualified.Value?.Fields[1].Value?.TextValue,
                Is.EqualTo("part"));
            Assert.That(decodedUnqualified.Value?.Fields[0].Present, Is.False);
            Assert.That(decodedUnqualified.Value?.Fields[1].Value?.TextValue,
                Is.EqualTo("local"));
            Assert.That(attributeXml, Is.EqualTo(
                "<Container xmlns:ns1=\"urn:item\" kind=\"ns1:part\"/>"));
            Assert.That(decodedAttribute.Value?.Fields[0].Value?.Fields[0]
                .Value?.TextValue, Is.EqualTo("urn:item"));
        });
    }

    [Test]
    public void UseTypeRemovesAlternativeTagsAndUsesControlTypeIdentification()
    {
        AsnTypeDescriptor type = ChoiceType(AsnInstructionKind.UseType);
        AsnRuntimeValue first = Choice("number", Integer(7));
        AsnRuntimeValue second = Choice("label", Text("hello"));

        string firstXml = EncodeXml(type, first);
        string secondXml = EncodeXml(type, second);
        XerValueResult decodedFirst = DecodeXml(type, firstXml);
        XerValueResult decodedSecond = DecodeXml(type, secondXml);
        XerValueResult unexpected = DecodeXml(
            type,
            "<Selection xmlns:a=\"urn:oid:2.1.5.2.0.1\" "
                + "a:type=\"unknown\">9</Selection>");

        Assert.Multiple(() =>
        {
            Assert.That(firstXml, Is.EqualTo("<Selection>7</Selection>"));
            Assert.That(secondXml, Is.EqualTo(
                "<Selection xmlns:asn1=\"urn:oid:2.1.5.2.0.1\" "
                    + "asn1:type=\"label\">hello</Selection>"));
            Assert.That(decodedFirst.Value?.Fields[0].Name, Is.EqualTo("number"));
            Assert.That(decodedSecond.Value?.Fields[0].Name, Is.EqualTo("label"));
            Assert.That(decodedSecond.Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("hello"));
            Assert.That(unexpected.Value?.Fields[0].Name, Is.EqualTo("number"));
        });
    }

    [Test]
    public void UseUnionUsesCharacterContentAndRecoversTheSelectedAlternative()
    {
        AsnTypeDescriptor type = ChoiceType(AsnInstructionKind.UseUnion);
        AsnRuntimeValue selected = Choice("label", Text("42"));

        string xml = EncodeXml(type, selected);
        XerValueResult decoded = DecodeXml(type, xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<Selection xmlns:asn1=\"urn:oid:2.1.5.2.0.1\" "
                    + "asn1:type=\"label\">42</Selection>"));
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Name, Is.EqualTo("label"));
            Assert.That(decoded.Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("42"));
        });
    }

    [Test]
    public void WhitespaceReplaceAndCollapseNormalizeAcceptedLexicalVariants()
    {
        AsnTypeDescriptor replace = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Replace"
            },
            AsnInstructionKind.Whitespace,
            ["REPLACE"]);
        AsnTypeDescriptor collapse = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Collapse"
            },
            AsnInstructionKind.Whitespace,
            ["COLLAPSE"]);

        XerValueResult replaced = DecodeXml(
            replace,
            "<Replace>a&#x9;b&#xA;c</Replace>");
        XerValueResult collapsed = DecodeXml(
            collapse,
            "<Collapse> &#x9; a  &#xA; b &#xD; </Collapse>");

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(replace, Text("a b")),
                Is.EqualTo("<Replace>a b</Replace>"));
            Assert.That(replaced.Value?.TextValue, Is.EqualTo("a b c"));
            Assert.That(collapsed.Value?.TextValue, Is.EqualTo("a b"));
        });
    }

    private static AsnTypeDescriptor QNameType()
    {
        return Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "QName",
                Fields =
                [
                    new AsnFieldDescriptor
                    {
                        Name = "namespace",
                        Optional = true,
                        Type = new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String }
                    },
                    Field("name", new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String })
                ]
            },
            AsnInstructionKind.UseQName);
    }

    private static AsnTypeDescriptor ChoiceType(AsnInstructionKind instruction)
    {
        return Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Choice,
                Name = "Selection",
                Fields =
                [
                    Field("number", new AsnTypeDescriptor { Kind = AsnTypeKind.Integer }),
                    Field("label", new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String })
                ]
            },
            instruction);
    }

    private static AsnTypeDescriptor Instructions(
        AsnTypeDescriptor type,
        AsnInstructionKind instruction,
        params string[] arguments)
    {
        type.Encodings = [new AsnEncodingDescriptor
        {
            EncodingRules = AsnEncodingRules.ExtendedXer,
            EncodingReference = "XER",
            Instructions = [Instruction(instruction, arguments)]
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

    private static AsnValueDescriptor Named(string name, int number)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                HasIntegerValue = true,
                IntegerValue = number
            }
        };
    }

    private static AsnRuntimeValue Real(int coefficient, int exponent)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Normalized = true,
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = coefficient + "E" + exponent,
                DecimalCoefficient = coefficient,
                DecimalExponent = exponent
            }
        };
    }

    private static AsnRuntimeValue QNameValue(string namespaceUri, string name)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("namespace", Text(namespaceUri)),
                Present("name", Text(name))
            ]
        };
    }

    private static AsnRuntimeValue Choice(string name, AsnRuntimeValue value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [Present(name, value)]
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
