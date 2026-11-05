using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaValidatorTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void ValidatesAndAnnotatesTreesThroughThePublicApi()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument document = ParseDocument("<root xmlns='urn:test' a='7' b='8'><value>42</value></root>");
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { AnnotateTree = true };

        XmlSchemaValidationResult result = new XmlSchemaValidator().ValidateXmlSchema(document, schemaSet, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Document, Is.SameAs(document));
            Assert.That(result.SchemaSet, Is.SameAs(schemaSet));
            Assert.That(result.Options, Is.SameAs(options));
            Assert.That(result.TypeAnnotations, Has.Length.EqualTo(4));
            Assert.That(document.DocumentElement!.TypeInfo, Is.Not.Null);
            Assert.That(document.DocumentElement.Children![0].TypeInfo!.ActualValue!.IntegerValue, Is.EqualTo(42));
        });
    }

    [Test]
    public void CollectsAllDiagnosticsOrStopsAfterTheFirstError()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument collectedDocument = ParseDocument("<root xmlns='urn:test' unexpected='x'><value>bad</value></root>");
        XmlDocument failFastDocument = ParseDocument("<root xmlns='urn:test' unexpected='x'><value>bad</value></root>");
        XmlSchemaValidator validator = new XmlSchemaValidator();

        XmlSchemaValidationResult collected = validator.ValidateXmlSchema(collectedDocument, schemaSet, new XmlSchemaValidationOptions());
        XmlSchemaValidationResult failFast = validator.ValidateXmlSchema(failFastDocument, schemaSet, new XmlSchemaValidationOptions { FailFast = true });

        Assert.Multiple(() =>
        {
            Assert.That(collected.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(collected.Diagnostics, Has.Length.GreaterThan(1));
            Assert.That(collected.Diagnostics!.All(value => value.Category == XmlErrorCategory.Schema), Is.True);
            Assert.That(failFast.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(failFast.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(failFast.Succeeded, Is.True);
        });
    }

    [Test]
    public void ValidatesABoundedNormalizedEventBatch()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument source = ParseDocument("<root xmlns='urn:test' a='7' b='8'><value>42</value></root>");
        XmlParserOptions parserOptions = ParserOptions();
        XmlEventBatch emitted = new XmlEventEmitter().EmitXmlNodeEvents(source.DocumentNode!, parserOptions);
        XmlEvent[] padded = new XmlEvent[emitted.Count + 2];
        padded[0] = new XmlEvent { Kind = XmlEventKind.Comment, Value = "before" };
        Array.Copy(emitted.Events!, 0, padded, 1, emitted.Count);
        padded[padded.Length - 1] = new XmlEvent { Kind = XmlEventKind.Comment, Value = "after" };
        XmlEventBatch selected = new XmlEventBatch
        {
            Events = padded,
            Offset = 1,
            Count = emitted.Count,
            Status = XmlParserStatus.Completed,
            IsFinal = true,
            ReaderState = new XmlReaderState { BaseUri = "https://example.test/instance.xml", Options = parserOptions }
        };

        XmlSchemaValidationResult result = new XmlSchemaValidator().ValidateXmlSchemaEvents(
            selected,
            schemaSet,
            new XmlSchemaValidationOptions { AnnotateTree = true });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Document, Is.Not.Null.And.Not.SameAs(source));
            Assert.That(result.Document!.DocumentElement!.Name!.LocalName, Is.EqualTo("root"));
            Assert.That(result.Document.BaseUri, Is.EqualTo("https://example.test/instance.xml"));
            Assert.That(result.TypeAnnotations, Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void RejectsUncompiledSetsAndInvalidEventBounds()
    {
        XmlSchemaValidator validator = new XmlSchemaValidator();
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions();
        XmlDocument document = ParseDocument("<root xmlns='urn:test' a='7' b='8'><value>42</value></root>");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => validator.ValidateXmlSchema(document, new XmlSchemaSet(), options));
            Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateXmlSchemaEvents(
                new XmlEventBatch { Events = [], Offset = 1, Count = 0 },
                CompileSchema(),
                options));
        });
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema =
            "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:test' elementFormDefault='qualified'>" +
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='value' type='xs:int'/></xs:sequence>" +
            "<xs:attribute name='a' type='xs:int' use='required'/><xs:attribute name='b' type='xs:int' use='required'/>" +
            "</xs:complexType></xs:element></xs:schema>";
        XmlInput input = new XmlInput
        {
            Kind = XmlInputKind.String,
            Text = schema,
            BaseUri = "https://example.test/schema.xsd"
        };
        XmlResolverPolicy policy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] };
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { ResolverPolicy = policy };
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas([input], options, new XmlDefaultResolver());
        Assert.That(result.IsImmutable, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        return result;
    }

    private static XmlDocument ParseDocument(string text)
    {
        XmlInput input = new XmlInput
        {
            Kind = XmlInputKind.String,
            Text = text,
            BaseUri = "https://example.test/instance.xml"
        };
        return new XmlParser().ParseXmlDocument(input, ParserOptions(), null, null, null);
    }

    private static XmlParserOptions ParserOptions()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
    }
}
