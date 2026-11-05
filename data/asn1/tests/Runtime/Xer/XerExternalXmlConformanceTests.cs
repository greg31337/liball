using asn1;

namespace asn1.Tests;

public class XerExternalXmlConformanceTests
{
    [Test]
    public void EveryXerModeProducesXmlAcceptedByDotNetReaders()
    {
        AsnTypeDescriptor packet = PacketType();
        AsnReflectionCatalog catalog = Catalog(packet);
        AsnRuntimeValue value = PacketValue(7, "A&B < C \"quoted\"");
        AsnEncodingRules[] rules =
        [
            AsnEncodingRules.BasicXer,
            AsnEncodingRules.CanonicalXer,
            AsnEncodingRules.ExtendedXer
        ];

        int index = 0;
        while (index < rules.Length)
        {
            AsnEncodeResult encoded = Encode(
                catalog,
                rules[index],
                value,
                Limits());
            System.Xml.XmlReaderSettings settings =
                new System.Xml.XmlReaderSettings
                {
                    DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                    XmlResolver = null
                };
            System.IO.MemoryStream readerInput =
                new System.IO.MemoryStream(encoded.Data);
            System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
                readerInput,
                settings);
            reader.MoveToContent();
            string readerLocalName = reader.LocalName;
            string readerNamespace = reader.NamespaceURI;
            while (reader.Read())
            {
            }
            reader.Dispose();
            readerInput.Dispose();

            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
            document.XmlResolver = null;
            System.IO.MemoryStream documentInput =
                new System.IO.MemoryStream(encoded.Data);
            document.Load(documentInput);
            documentInput.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Success,
                    Is.True,
                    Format(encoded.Diagnostics));
                Assert.That(readerLocalName, Is.EqualTo("Packet"));
                Assert.That(readerNamespace,
                    Is.EqualTo(rules[index] == AsnEncodingRules.ExtendedXer
                        ? "urn:packet"
                        : ""));
                Assert.That(document.DocumentElement?.LocalName,
                    Is.EqualTo("Packet"));
                Assert.That(document.DocumentElement?.InnerText,
                    Does.Contain("A&B < C \"quoted\""));
            });
            index = index + 1;
        }
    }

    [Test]
    public void IndependentXmlFixturesDecodeInEveryXerMode()
    {
        AsnTypeDescriptor packet = PacketType();
        AsnReflectionCatalog catalog = Catalog(packet);
        string ordinary = "<Packet><code>7</code>"
            + "<message>A&amp;B &lt; C</message></Packet>";
        string extended = "<p:Packet xmlns:p=\"urn:packet\" code=\"7\">"
            + "<message>A&amp;B &lt; C</message></p:Packet>";

        AsnDecodeResult basic = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            ordinary,
            Limits());
        AsnDecodeResult canonical = Decode(
            catalog,
            AsnEncodingRules.CanonicalXer,
            ordinary,
            Limits());
        AsnDecodeResult xer = Decode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            extended,
            Limits());
        AsnEncodeResult reproduced = Encode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            xer.Value ?? throw new AssertionException(
                "The independent EXTENDED-XER fixture did not decode."),
            Limits());

        Assert.Multiple(() =>
        {
            AssertPacket(basic);
            AssertPacket(canonical);
            AssertPacket(xer);
            Assert.That(reproduced.Success,
                Is.True,
                Format(reproduced.Diagnostics));
            AssertExternalDocument(reproduced.Data, "urn:packet");
        });
    }

    [Test]
    public void DecoderRejectsEntityAmplificationWithoutExpandingEntities()
    {
        string xml = "<!DOCTYPE Packet ["
            + "<!ENTITY a \"1234567890\">"
            + "<!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">"
            + "<!ENTITY c \"&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;\">"
            + "]><Packet><code>7</code><message>&c;</message></Packet>";

        AsnDecodeResult result = Decode(
            Catalog(PacketType()),
            AsnEncodingRules.BasicXer,
            xml,
            Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNXER0004"));
            Assert.That(result.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Decoding));
            Assert.That(result.Diagnostics.Single().BitOffset,
                Is.EqualTo(0));
        });
    }

    [Test]
    public void DecoderLimitsRejectStructuralAndTextAmplification()
    {
        AsnReflectionCatalog catalog = Catalog(PacketType());
        AsnResourceLimits depthLimits = Limits();
        depthLimits.MaximumNestingDepth = 2;
        AsnDecodeResult depth = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            "<Packet><a><b><c/></b></a></Packet>",
            depthLimits);

        AsnResourceLimits textLimits = Limits();
        textLimits.MaximumTextCharacters = 64;
        AsnDecodeResult text = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            "<Packet><code>7</code><message>"
                + new string('x', 128)
                + "</message></Packet>",
            textLimits);

        AsnResourceLimits attributeLimits = Limits();
        attributeLimits.MaximumCollectionElements = 2;
        AsnDecodeResult attributes = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            "<Packet a=\"1\" b=\"2\" c=\"3\"/>",
            attributeLimits);

        AsnResourceLimits namespaceLimits = Limits();
        namespaceLimits.MaximumCollectionElements = 2;
        AsnDecodeResult namespaces = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            "<Packet xmlns:a=\"urn:a\" xmlns:b=\"urn:b\" "
                + "xmlns:c=\"urn:c\"/>",
            namespaceLimits);

        Assert.Multiple(() =>
        {
            AssertResourceFailure(depth, "ASNR0001");
            AssertResourceFailure(text, "ASNR0009");
            AssertResourceFailure(attributes, "ASNR0011");
            AssertResourceFailure(namespaces, "ASNR0011");
        });
    }

    private static void AssertPacket(AsnDecodeResult result)
    {
        Assert.That(result.Status,
            Is.EqualTo(AsnDecodeStatus.Done),
            Format(result.Diagnostics));
        Assert.That(result.Value?.Fields[0].Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(7)));
        Assert.That(result.Value?.Fields[1].Value?.TextValue,
            Is.EqualTo("A&B < C"));
    }

    private static void AssertExternalDocument(
        byte[] data,
        string namespaceUri)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.XmlResolver = null;
        System.IO.MemoryStream input = new System.IO.MemoryStream(data);
        document.Load(input);
        input.Dispose();
        Assert.That(document.DocumentElement?.NamespaceURI,
            Is.EqualTo(namespaceUri));
    }

    private static void AssertResourceFailure(
        AsnDecodeResult result,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Resource));
    }

    private static AsnEncodeResult Encode(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules,
        AsnRuntimeValue value,
        AsnResourceLimits limits)
    {
        return new XerSchemaCodec(catalog, rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = value,
                Options = Options(rules, limits)
            });
    }

    private static AsnDecodeResult Decode(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules,
        string xml,
        AsnResourceLimits limits)
    {
        return new XerSchemaCodec(catalog, rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Packet",
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                Options = Options(rules, limits)
            });
    }

    private static AsnCodecOptions Options(
        AsnEncodingRules rules,
        AsnResourceLimits limits)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            ResourceLimits = limits,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false
        };
    }

    private static AsnTypeDescriptor PacketType()
    {
        AsnTypeDescriptor code = WithInstructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Integer,
                Name = "code"
            },
            Instruction(AsnInstructionKind.Attribute, []));
        AsnTypeDescriptor message = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Utf8String,
            Name = "message"
        };
        return WithInstructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Sequence,
                Name = "Packet",
                Fields =
                [
                    new AsnFieldDescriptor
                    {
                        Name = "code",
                        Index = 0,
                        Type = code
                    },
                    new AsnFieldDescriptor
                    {
                        Name = "message",
                        Index = 1,
                        Type = message
                    }
                ]
            },
            Instruction(AsnInstructionKind.Namespace,
                ["AS", "\"urn:packet\"", "PREFIX", "\"p\""]));
    }

    private static AsnTypeDescriptor WithInstructions(
        AsnTypeDescriptor type,
        AsnEncodingInstruction instruction)
    {
        type.Encodings =
        [
            new AsnEncodingDescriptor
            {
                EncodingRules = AsnEncodingRules.ExtendedXer,
                EncodingReference = "XER",
                Instructions = [instruction]
            }
        ];
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

    private static AsnRuntimeValue PacketValue(int code, string message)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "code",
                    Present = true,
                    Value = new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = code
                    }
                },
                new AsnRuntimeFieldValue
                {
                    Name = "message",
                    Present = true,
                    Value = new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.CharacterString,
                        TextValue = message
                    }
                }
            ]
        };
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Packet",
                    Name = "Packet",
                    QualifiedName = "Module.Packet",
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 16,
            MaximumCollectionElements = 64,
            MaximumTextCharacters = 4096,
            MaximumOpenTypeBytes = 4096,
            MaximumTotalAllocationBytes = 65536
        };
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(
            item => item.Code + " " + item.Message));
    }
}
