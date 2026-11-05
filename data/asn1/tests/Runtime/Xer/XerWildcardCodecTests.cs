using asn1;

namespace asn1.Tests;

public class XerWildcardCodecTests
{
    [Test]
    public void AnyElementReplacesTheComponentWrapperAndRetainsNamespaces()
    {
        AsnTypeDescriptor wildcard = AnyElementType(
            "FROM",
            "\"urn:outer\"");
        AsnTypeDescriptor envelope = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Envelope",
            Fields = [Field("payload", wildcard)]
        };

        XerValueResult decoded = DecodeXml(
            envelope,
            "<Envelope xmlns:q=\"urn:outer\"><q:item code=\"x\">"
                + "<q:child/></q:item></Envelope>");

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.TextValue,
                Is.EqualTo("<q:item xmlns:q=\"urn:outer\" code=\"x\">"
                    + "<q:child/></q:item>"));
        });
        Assert.That(EncodeXml(envelope, decoded.Value!), Is.EqualTo(
            "<Envelope><q:item xmlns:q=\"urn:outer\" code=\"x\">"
                + "<q:child/></q:item></Envelope>"));
    }

    [Test]
    public void AnyElementValidatesItsXmlAndRootNamespaceRestriction()
    {
        AsnTypeDescriptor wildcard = AnyElementType(
            "EXCEPT",
            "\"urn:blocked\"",
            "ABSENT");
        AsnRuntimeValue allowed = Text(
            "<ok xmlns=\"urn:allowed\"><nested/></ok>");
        AsnRuntimeValue blocked = Text(
            "<bad xmlns=\"urn:blocked\"/>");
        AsnRuntimeValue absent = Text("<local/>");
        AsnRuntimeValue malformed = Text("<broken>");

        Assert.Multiple(() =>
        {
            Assert.That(EncodeResult(wildcard, allowed).Success, Is.True);
            Assert.That(EncodeResult(wildcard, blocked).ErrorCode,
                Is.EqualTo("ASNXER0303"));
            Assert.That(EncodeResult(wildcard, absent).ErrorCode,
                Is.EqualTo("ASNXER0303"));
            Assert.That(EncodeResult(wildcard, malformed).ErrorCode,
                Is.EqualTo("ASNXER0301"));
        });
    }

    [Test]
    public void AnyElementWorksAsARepeatedCollectionComponent()
    {
        AsnTypeDescriptor items = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            Name = "Items",
            ElementType = AnyElementType("FROM", "\"urn:item\"")
        };

        XerValueResult decoded = DecodeXml(
            items,
            "<Items xmlns:i=\"urn:item\"><i:first/><i:second/></Items>");

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Items.Select(item => item.TextValue),
                Is.EqualTo(new[]
                {
                    "<i:first xmlns:i=\"urn:item\"/>",
                    "<i:second xmlns:i=\"urn:item\"/>"
                }));
        });
    }

    [Test]
    public void AnyAttributesRoundTripQualifiedAndUnqualifiedAttributeFormats()
    {
        AsnTypeDescriptor attributes = AnyAttributesType(
            "FROM",
            "\"urn:meta\"",
            "ABSENT");
        AsnTypeDescriptor product = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Product",
            Fields =
            [
                Field("attributes", attributes),
                Field("name", StringType())
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("attributes", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items =
                    [
                        Text("urn:meta code=\"A&amp;B\""),
                        Text("plain=\"say &quot;hi&quot;\"")
                    ]
                }),
                Present("name", Text("shirt"))
            ]
        };

        string xml = EncodeXml(product, value);
        Assert.That(xml, Is.EqualTo(
            "<Product xmlns:ns1=\"urn:meta\" ns1:code=\"A&amp;B\" "
                + "plain=\"say &quot;hi&quot;\"><name>shirt</name></Product>"));

        XerValueResult decoded = DecodeXml(
            product,
            "<Product xmlns:m=\"urn:meta\" plain=\"say &quot;hi&quot;\" "
                + "m:code=\"A&amp;B\"><name>shirt</name></Product>");
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Items
                .Select(item => item.TextValue), Is.EqualTo(new[]
                {
                    "plain=\"say &quot;hi&quot;\"",
                    "urn:meta code=\"A&amp;B\""
                }));
            Assert.That(decoded.Value?.Fields[1].Value?.TextValue,
                Is.EqualTo("shirt"));
        });
    }

    [Test]
    public void AnyAttributesRejectInvalidRestrictionsFormatsAndDuplicates()
    {
        AsnTypeDescriptor attributes = AnyAttributesType(
            "FROM",
            "\"urn:allowed\"");
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Container",
            Fields = [Field("attributes", attributes)]
        };
        AsnRuntimeValue duplicate = AttributeContainer(
            "urn:allowed id=\"one\"",
            "urn:allowed id=\"two\"");
        AsnRuntimeValue malformed = AttributeContainer("id=unquoted");

        XerValueResult duplicateResult = EncodeResult(container, duplicate);
        XerValueResult malformedResult = EncodeResult(container, malformed);
        XerValueResult disallowed = DecodeXml(
            container,
            "<Container xmlns:x=\"urn:other\" x:id=\"one\"/>");

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.ErrorCode, Is.EqualTo("ASNXER0313"));
            Assert.That(malformedResult.ErrorCode, Is.EqualTo("ASNXER0312"));
            Assert.That(disallowed.ErrorCode, Is.EqualTo("ASNXER0314"));
        });
    }

    [Test]
    public void AnyAttributesSupportSetOfValuesOnAnEnclosingSet()
    {
        AsnTypeDescriptor attributes = WithInstruction(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SetOf,
                ElementType = StringType()
            },
            AsnInstructionKind.AnyAttributes,
            "ANY-ATTRIBUTES",
            ["FROM", "ABSENT"]);
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Name = "Container",
            Fields =
            [
                Field("attributes", attributes),
                Field("name", StringType())
            ]
        };

        XerValueResult decoded = DecodeXml(
            container,
            "<Container enabled=\"yes\"><name>value</name></Container>");

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.SetOf));
            Assert.That(decoded.Value?.Fields[0].Value?.Items[0].TextValue,
                Is.EqualTo("enabled=\"yes\""));
        });
    }

    private static AsnTypeDescriptor AnyElementType(params string[] arguments)
    {
        return WithInstruction(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Wildcard"
            },
            AsnInstructionKind.AnyElement,
            "ANY-ELEMENT",
            arguments);
    }

    private static AsnTypeDescriptor AnyAttributesType(params string[] arguments)
    {
        return WithInstruction(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SequenceOf,
                ElementType = StringType()
            },
            AsnInstructionKind.AnyAttributes,
            "ANY-ATTRIBUTES",
            arguments);
    }

    private static AsnTypeDescriptor WithInstruction(
        AsnTypeDescriptor type,
        AsnInstructionKind kind,
        string name,
        string[] arguments)
    {
        type.Encodings = [new AsnEncodingDescriptor
        {
            EncodingRules = AsnEncodingRules.ExtendedXer,
            EncodingReference = "XER",
            Instructions = [new AsnEncodingInstruction
            {
                Name = name,
                Kind = kind,
                Arguments = arguments
            }]
        }];
        return type;
    }

    private static AsnRuntimeValue AttributeContainer(params string[] values)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("attributes", new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = values.Select(Text).ToArray()
            })]
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
        XerValueCodec codec = new XerValueCodec(limits);
        codec.Extended = true;
        XerValueResult encoded = codec.EncodeValue(type, value);
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnTypeDescriptor StringType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String };
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
