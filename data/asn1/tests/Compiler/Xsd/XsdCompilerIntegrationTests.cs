using asn1;

namespace asn1.Tests;

public class XsdCompilerIntegrationTests
{
    [Test]
    public void CompilesMappedModulesThroughReflectionAndCodeGeneration()
    {
        AsnCompilationResult result = Compile(
            "<xs:element name=\"greeting\" type=\"xs:string\"/>",
            true);
        AsnAssignmentDescriptor greeting = result.ReflectionCatalog
            ?.Assignments.Single(value => value.Name == "Greeting")
            ?? throw new AssertionException(
                "The mapped assignment is missing. "
                    + Format(result.Diagnostics));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(result.ValidationResult?.Success, Is.True);
            Assert.That(result.Modules.Select(value => value.Name),
                Does.Contain("Mapped"));
            Assert.That(result.Modules.Select(value => value.Name),
                Does.Contain("XSDv2"));
            Assert.That(greeting.Type?.Kind,
                Is.EqualTo(AsnTypeKind.Utf8String));
            Assert.That(greeting.Derivations.Any(value =>
                value.Kind == AsnDerivationKind.XsdMapping
                    && value.Span?.Start.SourceName == "mapped.xsd"),
                Is.True);
            Assert.That(result.GeneratedFiles, Is.Not.Empty);
            Assert.That(result.GeneratedFiles.SelectMany(value =>
                value.SourceSpans).Any(value =>
                    value.Start.SourceName == "mapped.xsd"),
                Is.True);
        });
    }

    [Test]
    public void ExtendedXerRoundTripPreservesMappedElementInformation()
    {
        AsnCompilationResult compilation = Compile(
            "<xs:element name=\"greeting\" type=\"xs:string\"/>",
            false);
        AsnReflectionCatalog catalog = compilation.ReflectionCatalog
            ?? throw new AssertionException(
                "The reflection catalog is missing. "
                    + Format(compilation.Diagnostics));
        string xml = "<greeting xmlns=\"urn:test\">hello</greeting>";
        XerSchemaCodec codec = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer);
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Mapped.Greeting",
            Data = System.Text.Encoding.UTF8.GetBytes(xml),
            Options = Options()
        });
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Mapped.Greeting",
            Value = decoded.Value,
            Options = Options()
        });
        XerXmlReadResult reproduced = new XerXmlTokenReader(
            new AsnResourceLimits()).ConsumeXml(new AsnIncrementalInput
            {
                Data = encoded.Data,
                IsFinalBlock = true
            });
        XerXmlToken wrapper = reproduced.Tokens.First(value =>
            value.Kind == XerXmlTokenKind.StartElement);
        XerXmlToken text = reproduced.Tokens.First(value =>
            value.Kind == XerXmlTokenKind.Text);

        Assert.Multiple(() =>
        {
            Assert.That(compilation.Success, Is.True,
                Format(compilation.Diagnostics));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(decoded.Diagnostics));
            Assert.That(encoded.Success, Is.True,
                Format(encoded.Diagnostics));
            Assert.That(reproduced.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(wrapper.LocalName, Is.EqualTo("greeting"));
            Assert.That(wrapper.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(text.Value, Is.EqualTo("hello"));
        });
    }

    [Test]
    public void ExtendedXerRoundTripPreservesElementsAttributesAndValues()
    {
        AsnCompilationResult compilation = Compile(
            "<xs:complexType name=\"PersonType\">"
                + "<xs:sequence>"
                + "<xs:element name=\"name\" type=\"xs:string\"/>"
                + "<xs:element name=\"age\" type=\"xs:int\"/>"
                + "</xs:sequence>"
                + "<xs:attribute name=\"id\" type=\"xs:string\" "
                + "use=\"required\"/>"
                + "</xs:complexType>"
                + "<xs:element name=\"person\" type=\"tns:PersonType\"/>",
            false);
        AsnReflectionCatalog catalog = compilation.ReflectionCatalog
            ?? throw new AssertionException(
                "The reflection catalog is missing. "
                    + Format(compilation.Diagnostics));
        string xml = "<person xmlns=\"urn:test\" id=\"p-1\">"
            + "<name>Ada</name><age>42</age></person>";
        XerSchemaCodec codec = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer);
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Mapped.Person",
            Data = System.Text.Encoding.UTF8.GetBytes(xml),
            Options = Options()
        });
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Mapped.Person",
            Value = decoded.Value,
            Options = Options()
        });
        XerXmlReadResult reproduced = new XerXmlTokenReader(
            new AsnResourceLimits()).ConsumeXml(new AsnIncrementalInput
            {
                Data = encoded.Data,
                IsFinalBlock = true
            });
        XerXmlToken[] elements = reproduced.Tokens.Where(value =>
            value.Kind == XerXmlTokenKind.StartElement).ToArray();
        XerXmlAttribute id = elements[0].Attributes.Single(value =>
            !value.NamespaceDeclaration && value.LocalName == "id");

        Assert.Multiple(() =>
        {
            Assert.That(compilation.Success, Is.True,
                Format(compilation.Diagnostics));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(decoded.Diagnostics));
            Assert.That(encoded.Success, Is.True,
                Format(encoded.Diagnostics));
            Assert.That(reproduced.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(elements.Select(value => value.LocalName),
                Is.EqualTo(new[] { "person", "name", "age" }));
            Assert.That(elements.All(value =>
                value.NamespaceUri == "urn:test"), Is.True);
            Assert.That(id.NamespaceUri, Is.Empty);
            Assert.That(id.Value, Is.EqualTo("p-1"));
            Assert.That(reproduced.Tokens.Where(value =>
                value.Kind == XerXmlTokenKind.Text).Select(value =>
                    value.Value), Is.EqualTo(new[] { "Ada", "42" }));
        });
    }

    [Test]
    public void CompilesSpecializedPredeclaredXsdSupportShapes()
    {
        AsnCompilationResult result = Compile(
            "<xs:element name=\"dateValue\" type=\"xs:date\"/>"
                + "<xs:element name=\"qualifiedName\" type=\"xs:QName\"/>"
                + "<xs:element name=\"amount\" type=\"xs:decimal\"/>"
                + "<xs:element name=\"references\" type=\"xs:IDREFS\"/>",
            false);
        AsnAssignmentDescriptor[] assignments = result.ReflectionCatalog
            ?.Assignments ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(assignments.Single(value =>
                value.Name == "DateValue").Type?.Kind,
                Is.EqualTo(AsnTypeKind.Choice));
            Assert.That(assignments.Single(value =>
                value.Name == "QualifiedName").Type?.Kind,
                Is.EqualTo(AsnTypeKind.Sequence));
            Assert.That(assignments.Single(value =>
                value.Name == "Amount").Type?.Kind,
                Is.EqualTo(AsnTypeKind.Real));
            Assert.That(assignments.Single(value =>
                value.Name == "References").Type?.Kind,
                Is.EqualTo(AsnTypeKind.SequenceOf));
        });
    }

    private static AsnCompilationResult Compile(
        string content,
        bool generate)
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            XsdInputs =
            [
                new AsnXsdInput
                {
                    Name = "mapped.xsd",
                    HasBaseUri = true,
                    BaseUri = "urn:mapped.xsd",
                    Text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" "
                        + "xmlns:tns=\"urn:test\" targetNamespace=\"urn:test\" "
                        + "elementFormDefault=\"qualified\" "
                        + "attributeFormDefault=\"unqualified\">"
                        + content + "</xs:schema>"
                }
            ],
            XsdImportOptions = new AsnXsdImportOptions
            {
                HasModuleName = true,
                ModuleName = "Mapped"
            },
            GenerationOptions = generate
                ? new AsnGenerationOptions
                {
                    NamespaceName = "Generated.Xsd",
                    Target = AsnCodeGenerationTarget.Combined,
                    IncludeSourceMappings = true
                }
                : null
        });
    }

    private static AsnCodecOptions Options()
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.ExtendedXer
        };
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(value =>
            value.Code + ":" + value.Message));
    }
}
