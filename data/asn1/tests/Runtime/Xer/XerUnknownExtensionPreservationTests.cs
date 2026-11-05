using asn1;

namespace asn1.Tests;

public class XerUnknownExtensionPreservationTests
{
    [Test]
    public void UnknownElementRetainsNamespaceScopeAndSiblingPosition()
    {
        AsnTypeDescriptor type = RecordType("p");
        AsnReflectionCatalog catalog = Catalog(type);
        byte[] input = Bytes(
            "<p:record xmlns:p=\"urn:parent\" xmlns:q=\"urn:future\">"
                + "<id>1</id><q:future code=\"q:item\"><q:child/>"
                + "</q:future><tail>last</tail></p:record>");

        AsnDecodeResult decoded = Decode(
            catalog,
            input,
            AsnUnknownDataPolicy.Preserve);
        AsnUnknownField field = decoded.Value == null
            ? throw new AssertionException("No runtime value was decoded.")
            : decoded.Value.UnknownFields.Single(item => !item.XmlAttribute);
        SetRootPrefix(type, "q");
        AsnEncodeResult encoded = Encode(
            catalog,
            decoded.Value,
            AsnUnknownDataPolicy.Preserve);
        AsnDecodeResult roundTrip = Decode(
            catalog,
            encoded.Data,
            AsnUnknownDataPolicy.Preserve);
        AsnUnknownField roundTripField = roundTrip.Value == null
            ? throw new AssertionException("The re-encoded value did not decode.")
            : roundTrip.Value.UnknownFields.Single(
                item => !item.XmlAttribute);
        string xml = Text(encoded.Data);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(field.Name, Is.EqualTo("future"));
            Assert.That(field.XmlPrefix, Is.EqualTo("q"));
            Assert.That(field.XmlNamespaceUri, Is.EqualTo("urn:future"));
            Assert.That(field.TextData, Does.Contain("xmlns:q=\"urn:future\""));
            Assert.That(field.HasEncodedLocation, Is.True);
            Assert.That(encoded.Success, Is.True);
            Assert.That(xml.IndexOf("<id>", StringComparison.Ordinal),
                Is.LessThan(xml.IndexOf("<q:future", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("<q:future", StringComparison.Ordinal),
                Is.LessThan(xml.IndexOf("<tail>", StringComparison.Ordinal)));
            Assert.That(roundTripField.XmlNamespaceUri,
                Is.EqualTo("urn:future"));
            Assert.That(roundTripField.TextData, Does.Contain("q:item"));
        });
    }

    [Test]
    public void UnknownAttributesRetainExpandedNamesAndQNameValueBindings()
    {
        AsnTypeDescriptor type = RecordType("p");
        AsnReflectionCatalog catalog = Catalog(type);
        AsnDecodeResult decoded = Decode(
            catalog,
            Bytes(
                "<p:record xmlns:p=\"urn:parent\" xmlns:m=\"urn:meta\" "
                    + "xmlns:v=\"urn:value\" m:flag=\"v:item\" "
                    + "plain=\"A&amp;B\"><id>2</id><tail>x</tail>"
                    + "</p:record>"),
            AsnUnknownDataPolicy.Preserve);
        AsnRuntimeValue value = decoded.Value == null
            ? throw new AssertionException("No runtime value was decoded.")
            : decoded.Value;
        SetRootPrefix(type, "v");

        AsnEncodeResult encoded = Encode(
            catalog,
            value,
            AsnUnknownDataPolicy.Preserve);
        Assert.That(encoded.Success, Is.True,
            encoded.Diagnostics.Length == 0
                ? "Encoding failed without a diagnostic."
                : encoded.Diagnostics[0].Code + ": "
                    + encoded.Diagnostics[0].Message);
        XerXmlReadResult read = Read(encoded.Data);
        XerXmlToken root = read.Tokens[0];
        XerXmlAttribute qualified = root.Attributes.Single(attribute =>
            !attribute.NamespaceDeclaration
                && attribute.LocalName == "flag");
        XerXmlAttribute plain = root.Attributes.Single(attribute =>
            !attribute.NamespaceDeclaration
                && attribute.LocalName == "plain");
        XerXmlNamespaceBinding valueBinding = root.NamespaceBindings.Single(
            binding => binding.Prefix == "v");

        Assert.Multiple(() =>
        {
            Assert.That(value.UnknownFields.Count(field => field.XmlAttribute),
                Is.EqualTo(2));
            Assert.That(value.UnknownFields.Single(field =>
                field.Name == "flag").XmlNamespaceUri,
                Is.EqualTo("urn:meta"));
            Assert.That(encoded.Success, Is.True);
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(root.NamespaceUri, Is.EqualTo("urn:parent"));
            Assert.That(root.Prefix, Is.Not.EqualTo("v"));
            Assert.That(qualified.NamespaceUri, Is.EqualTo("urn:meta"));
            Assert.That(qualified.Value, Is.EqualTo("v:item"));
            Assert.That(valueBinding.NamespaceUri, Is.EqualTo("urn:value"));
            Assert.That(plain.Value, Is.EqualTo("A&B"));
        });
    }

    [Test]
    public void UnknownDataPolicyControlsElementsAndAttributesTogether()
    {
        AsnTypeDescriptor type = RecordType("p");
        AsnReflectionCatalog catalog = Catalog(type);
        byte[] input = Bytes(
            "<p:record xmlns:p=\"urn:parent\" extra=\"yes\">"
                + "<id>3</id><future/><tail>x</tail></p:record>");

        AsnDecodeResult preserved = Decode(
            catalog,
            input,
            AsnUnknownDataPolicy.Preserve);
        AsnDecodeResult skipped = Decode(
            catalog,
            input,
            AsnUnknownDataPolicy.Skip);
        AsnDecodeResult rejected = Decode(
            catalog,
            input,
            AsnUnknownDataPolicy.Reject);
        AsnRuntimeValue skippedValue = skipped.Value == null
            ? throw new AssertionException("Skip did not produce a value.")
            : skipped.Value;
        AsnEncodeResult skipEncoded = Encode(
            catalog,
            skippedValue,
            AsnUnknownDataPolicy.Skip);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Value?.UnknownFields, Has.Length.EqualTo(2));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.Value?.UnknownFields, Is.Empty);
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0330"));
            Assert.That(Text(skipEncoded.Data),
                Is.EqualTo("<p:record xmlns:p=\"urn:parent\"><id>3</id>"
                    + "<tail>x</tail></p:record>"));
        });
    }

    [Test]
    public void RuntimeCloneRetainsXerFragmentIdentity()
    {
        AsnUnknownField source = new AsnUnknownField
        {
            Name = "flag",
            XmlAttribute = true,
            XmlPrefix = "m",
            XmlNamespaceUri = "urn:meta",
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "<fragment xmlns:m=\"urn:meta\" m:flag=\"yes\"/>"
        };

        AsnUnknownField clone = new ValueAccess()
            .CloneUnknownFields([source])[0];

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.XmlAttribute, Is.True);
            Assert.That(clone.XmlPrefix, Is.EqualTo("m"));
            Assert.That(clone.XmlNamespaceUri, Is.EqualTo("urn:meta"));
            Assert.That(clone.TextData, Is.EqualTo(source.TextData));
        });
    }

    private static AsnTypeDescriptor RecordType(string prefix)
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            StableId = "type.Record",
            Name = "Record",
            Kind = AsnTypeKind.Set,
            Extensible = true,
            Fields =
            [
                Field("id", AsnTypeKind.Integer, false),
                Field("tail", AsnTypeKind.Utf8String, true)
            ]
        };
        type.Encodings =
        [
            new AsnEncodingDescriptor
            {
                EncodingRules = AsnEncodingRules.ExtendedXer,
                EncodingReference = "XER",
                Instructions =
                [
                    Instruction(
                        AsnInstructionKind.Name,
                        ["AS", "LOWERCASED"]),
                    Instruction(
                        AsnInstructionKind.Namespace,
                        ["AS", "\"urn:parent\"", "PREFIX",
                            "\"" + prefix + "\""])
                ]
            }
        ];
        return type;
    }

    private static void SetRootPrefix(
        AsnTypeDescriptor type,
        string prefix)
    {
        type.Encodings[0].Instructions[1].Arguments =
        [
            "AS",
            "\"urn:parent\"",
            "PREFIX",
            "\"" + prefix + "\""
        ];
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeKind kind,
        bool optional)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Optional = optional,
            Type = new AsnTypeDescriptor { Kind = kind }
        };
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

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Record",
                    Name = type.Name,
                    QualifiedName = "Module.Record",
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnDecodeResult Decode(
        AsnReflectionCatalog catalog,
        byte[] data,
        AsnUnknownDataPolicy policy)
    {
        return new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer).DecodeRequest(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Record",
                    Data = data,
                    Options = Options(policy)
                });
    }

    private static AsnEncodeResult Encode(
        AsnReflectionCatalog catalog,
        AsnRuntimeValue value,
        AsnUnknownDataPolicy policy)
    {
        return new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer).EncodeRequest(
                new AsnEncodeRequest
                {
                    TypeName = "Module.Record",
                    Value = value,
                    Options = Options(policy)
                });
    }

    private static AsnCodecOptions Options(AsnUnknownDataPolicy policy)
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.ExtendedXer,
            UnknownDataPolicy = policy,
            ValidateValues = false
        };
    }

    private static XerXmlReadResult Read(byte[] data)
    {
        return new XerXmlTokenReader(new AsnResourceLimits()).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = data,
                IsFinalBlock = true
            });
    }

    private static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    private static string Text(byte[] data)
    {
        return System.Text.Encoding.UTF8.GetString(data);
    }
}
