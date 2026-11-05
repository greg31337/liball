using asn1;

namespace asn1.Tests;

public class XerFailureDiagnosticTests
{
    [TestCase("<a><b></a>", "ASNXER0043", 6)]
    [TestCase("<p:Number/>", "ASNXER0029", 1)]
    public void XmlAndNamespaceFailuresExposeExactInputOffsets(
        string xml,
        string code,
        int byteOffset)
    {
        AsnDecodeResult result = Decode(
            Type(AsnTypeKind.Integer, "Number"),
            xml,
            Options(AsnEncodingRules.BasicXer));

        AssertDiagnostic(
            result,
            code,
            AsnDiagnosticCategory.Decoding,
            byteOffset,
            "$");
    }

    [Test]
    public void CanonicalXmlFailureRetainsTheParserOffset()
    {
        string xml = "<Number><part></Number>";

        AsnDecodeResult result = Decode(
            Type(AsnTypeKind.Integer, "Number"),
            xml,
            Options(AsnEncodingRules.CanonicalXer));

        AssertDiagnostic(
            result,
            "ASNXER0043",
            AsnDiagnosticCategory.Decoding,
            xml.IndexOf("</Number>", System.StringComparison.Ordinal),
            "$");
    }

    [Test]
    public void MisorderedAndUnexpectedSequenceElementsIdentifyTheOffendingTag()
    {
        AsnTypeDescriptor packet = Collection(
            AsnTypeKind.Sequence,
            "Packet",
            Field("first", 0),
            Field("second", 1));
        string misorderedXml =
            "<Packet><second>2</second><first>1</first></Packet>";
        string unexpectedXml =
            "<Packet><first>1</first><second>2</second><extra/></Packet>";

        AsnDecodeResult misordered = Decode(
            packet,
            misorderedXml,
            Options(AsnEncodingRules.BasicXer));
        AsnDecodeResult unexpected = Decode(
            packet,
            unexpectedXml,
            Options(AsnEncodingRules.BasicXer));

        Assert.Multiple(() =>
        {
            AssertDiagnostic(
                misordered,
                "ASNXER0122",
                AsnDiagnosticCategory.Decoding,
                misorderedXml.IndexOf(
                    "<second>",
                    System.StringComparison.Ordinal),
                "$.Packet");
            AssertDiagnostic(
                unexpected,
                "ASNXER0127",
                AsnDiagnosticCategory.Decoding,
                unexpectedXml.IndexOf(
                    "<extra/>",
                    System.StringComparison.Ordinal),
                "$.Packet");
        });
    }

    [Test]
    public void DuplicateSetElementIdentifiesItsSecondOccurrence()
    {
        AsnTypeDescriptor bag = Collection(
            AsnTypeKind.Set,
            "Bag",
            Field("id", 0));
        string xml = "<Bag><id>1</id><id>2</id></Bag>";

        AsnDecodeResult result = Decode(
            bag,
            xml,
            Options(AsnEncodingRules.BasicXer));

        AssertDiagnostic(
            result,
            "ASNXER0130",
            AsnDiagnosticCategory.Decoding,
            xml.LastIndexOf("<id>", System.StringComparison.Ordinal),
            "$.Bag");
    }

    [Test]
    public void InvalidLexicalValueReportsTheNestedValuePathAndTextOffset()
    {
        AsnTypeDescriptor packet = Collection(
            AsnTypeKind.Sequence,
            "Packet",
            Field("number", 0));
        string xml = "<Packet><number>not-an-integer</number></Packet>";

        AsnDecodeResult result = Decode(
            packet,
            xml,
            Options(AsnEncodingRules.BasicXer));

        AssertDiagnostic(
            result,
            "ASNXER0108",
            AsnDiagnosticCategory.Decoding,
            xml.IndexOf("not-an-integer", System.StringComparison.Ordinal),
            "$.Packet.number");
    }

    [Test]
    public void IncrementalValueFailureRetainsTheValuePathAndOffset()
    {
        AsnTypeDescriptor packet = Collection(
            AsnTypeKind.Sequence,
            "Packet",
            Field("number", 0));
        string xml = "<Packet><number>wrong</number></Packet>";
        XerIncrementalSchemaDecoder decoder =
            new XerIncrementalSchemaDecoder(
                Catalog(packet),
                "Module.Packet",
                Options(AsnEncodingRules.BasicXer));

        AsnIncrementalResult result = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
        AsnDiagnostic diagnostic = result.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo("ASNXER0108"));
            Assert.That(diagnostic.Category,
                Is.EqualTo(AsnDiagnosticCategory.Decoding));
            Assert.That(diagnostic.ValuePath,
                Is.EqualTo("$.Packet.number"));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(
                xml.IndexOf("wrong", System.StringComparison.Ordinal) * 8));
        });
    }

    [Test]
    public void IllegalExtendedInstructionReturnsAnEncodingDiagnostic()
    {
        AsnTypeDescriptor number = Type(AsnTypeKind.Integer, "Number");
        number.Encodings =
        [
            new AsnEncodingDescriptor
            {
                EncodingRules = AsnEncodingRules.ExtendedXer,
                EncodingReference = "XER",
                Instructions =
                [
                    new AsnEncodingInstruction
                    {
                        Kind = AsnInstructionKind.List,
                        Name = "LIST"
                    }
                ]
            }
        ];

        AsnEncodeResult result = new XerSchemaCodec(
            Catalog(number),
            AsnEncodingRules.ExtendedXer).EncodeRequest(
                new AsnEncodeRequest
                {
                    TypeName = "Module.Number",
                    Value = new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.Integer,
                        IntegerValue = 1
                    },
                    Options = Options(AsnEncodingRules.ExtendedXer)
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNXER0324"));
            Assert.That(result.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Encoding));
            Assert.That(result.Diagnostics.Single().ValuePath,
                Is.EqualTo("$.Number"));
            Assert.That(result.Diagnostics.Single().HasBitOffset, Is.False);
        });
    }

    [Test]
    public void RejectPolicyDiagnosesAnUnresolvedOpenType()
    {
        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        string xml = "<Open><Future/></Open>";
        AsnCodecOptions options = Options(AsnEncodingRules.BasicXer);
        options.UnknownDataPolicy = AsnUnknownDataPolicy.Reject;

        AsnDecodeResult result = Decode(open, xml, options);

        AssertDiagnostic(
            result,
            "ASNXER0154",
            AsnDiagnosticCategory.Decoding,
            xml.IndexOf("<Future/>", System.StringComparison.Ordinal),
            "$.Open");
    }

    [Test]
    public void SkipPolicyDoesNotRetainOrChargeAnUnresolvedOpenType()
    {
        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        AsnCodecOptions options = Options(AsnEncodingRules.BasicXer);
        options.UnknownDataPolicy = AsnUnknownDataPolicy.Skip;
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 1,
            MaximumTotalAllocationBytes = 1024
        };

        AsnDecodeResult result = Decode(
            open,
            "<Open><Future/></Open>",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(result.Value?.OpenTypeValue?.HasOriginalData, Is.False);
            Assert.That(result.Value?.OpenTypeValue?.OriginalData, Is.Empty);
            Assert.That(result.Value?.OpenTypeValue?.HasEncodedLocation,
                Is.False);
        });
    }

    [Test]
    public void TextResourceLimitReportsTheInputLocationAndResourceCategory()
    {
        string xml = "<Number>1234</Number>";
        AsnCodecOptions options = Options(AsnEncodingRules.BasicXer);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumTextCharacters = 3,
            MaximumTotalAllocationBytes = 1024
        };

        AsnDecodeResult result = Decode(
            Type(AsnTypeKind.Integer, "Number"),
            xml,
            options);

        AssertDiagnostic(
            result,
            "ASNR0009",
            AsnDiagnosticCategory.Resource,
            3,
            "$");
    }

    private static void AssertDiagnostic(
        AsnDecodeResult result,
        string code,
        AsnDiagnosticCategory category,
        int byteOffset,
        string valuePath)
    {
        AsnDiagnostic diagnostic = result.Diagnostics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Category, Is.EqualTo(category));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(byteOffset * 8));
            Assert.That(diagnostic.ValuePath, Is.EqualTo(valuePath));
            Assert.That(diagnostic.Message,
                Does.Contain("Byte offset: " + byteOffset + "."));
        });
    }

    private static AsnDecodeResult Decode(
        AsnTypeDescriptor type,
        string xml,
        AsnCodecOptions options)
    {
        return new XerSchemaCodec(
            Catalog(type),
            options.EncodingRules).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module." + type.Name,
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                Options = options
            });
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
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
        };
    }

    private static AsnTypeDescriptor Collection(
        AsnTypeKind kind,
        string name,
        params AsnFieldDescriptor[] fields)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            Kind = kind,
            Fields = fields,
            RootFields = fields
        };
    }

    private static AsnFieldDescriptor Field(string name, int index)
    {
        return new AsnFieldDescriptor
        {
            StableId = "field." + name,
            Name = name,
            Index = index,
            Type = Type(AsnTypeKind.Integer, name)
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

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false
        };
    }
}
