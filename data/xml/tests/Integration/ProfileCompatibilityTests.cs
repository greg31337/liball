using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class ProfileCompatibilityTests
{
    private const string Namespace = "urn:profile";
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void OptionalProfilesPreserveSharedXmlXPathAndXsltSemantics()
    {
        XmlSchemaSet schemas = CompileSchema();
        ProfileSnapshot basic = Snapshot(ParseBasic(Source()), XsltConformanceLevel.Basic, null, false);
        ProfileSnapshot dtd = Snapshot(ParseDtd(DtdSource(true)), XsltConformanceLevel.Basic, null, false);
        XmlDocument schemaDocument = ParseBasic(Source());
        XmlSchemaValidationResult schemaValidation = ValidateSchema(schemaDocument, schemas);
        ProfileSnapshot schemaAware = Snapshot(schemaDocument, XsltConformanceLevel.SchemaAware, schemas, true);
        ProfileSnapshot streaming = Snapshot(ParseChunked(Source(), 1), XsltConformanceLevel.Streaming, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(basic.CanonicalXml, Is.EqualTo("<root xmlns=\"urn:profile\" xmlns:p=\"urn:profile\"><item p:id=\"a\">2</item><item p:id=\"b\">3</item></root>"));
            AssertEquivalent(dtd, basic, "DTD-validating");
            AssertEquivalent(schemaAware, basic, "schema-aware");
            AssertEquivalent(streaming, basic, "streaming");
            Assert.That(schemaValidation.Validity, Is.EqualTo(XmlSchemaValidity.Valid), FormatDiagnostics(schemaValidation.Diagnostics));
            Assert.That(schemaDocument.DocumentElement!.TypeInfo!.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(schemaAware.IsSchemaAware, Is.True);
            Assert.That(streaming.IsStreaming, Is.True);
            Assert.That(basic.IsSchemaAware, Is.False);
            Assert.That(basic.IsStreaming, Is.False);
            Assert.That(dtd.Diagnostics, Is.Zero);
        });
    }

    [Test]
    public void ValidationProfilesAddOnlyTheirDeclaredValidityChecks()
    {
        XmlDocument basicDtdEquivalent = ParseBasic("<root xmlns='" + Namespace + "' xmlns:p='" + Namespace + "'><item p:id='a'>2</item></root>");
        XmlValidationException dtdFailure = Assert.Throws<XmlValidationException>(() => ParseDtd(DtdSource(false)))!;
        XmlSchemaSet schemas = CompileSchema();
        XmlDocument basicSchemaEquivalent = ParseBasic("<root xmlns='" + Namespace + "' xmlns:p='" + Namespace + "'><item p:id='a'>not-an-integer</item><item p:id='b'>3</item></root>");
        XmlSchemaValidationResult schemaFailure = ValidateSchema(basicSchemaEquivalent, schemas);

        Assert.Multiple(() =>
        {
            Assert.That(basicDtdEquivalent.DocumentElement!.Children, Has.Length.EqualTo(1));
            Assert.That(dtdFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
            Assert.That(dtdFailure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Validation));
            Assert.That(schemaFailure.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(schemaFailure.Diagnostics, Is.Not.Empty);
            Assert.That(schemaFailure.Diagnostics![0].Category, Is.EqualTo(XmlErrorCategory.Schema));
            Assert.That(new XmlTreeNavigator().GetXmlNodeStringValue(basicSchemaEquivalent.DocumentElement!), Is.EqualTo("not-an-integer3"));
        });
    }

    [Test]
    public void OptionalProfilesKeepCommonXPathAndXsltFailuresStable()
    {
        XmlSchemaSet schemas = CompileSchema();
        XmlDocument document = ParseBasic(Source());
        XPathDynamicException basicXPath = EvaluateXPathFailure(document, null, false);
        XPathDynamicException schemaXPath = EvaluateXPathFailure(document, schemas, true);
        XPathDynamicException basicXslt = EvaluateXsltFailure(XsltConformanceLevel.Basic, null);
        XPathDynamicException schemaXslt = EvaluateXsltFailure(XsltConformanceLevel.SchemaAware, schemas);
        XPathDynamicException streamingXslt = EvaluateXsltFailure(XsltConformanceLevel.Streaming, null);
        XmlWellFormednessException basicXml = Assert.Throws<XmlWellFormednessException>(() => ParseBasic("<root><item></root>"))!;
        XmlWellFormednessException dtdXml = Assert.Throws<XmlWellFormednessException>(() => ParseDtd("<!DOCTYPE root [<!ELEMENT root (item)><!ELEMENT item (#PCDATA)>]><root><item></root>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(schemaXPath.Diagnostic!.Code, Is.EqualTo(basicXPath.Diagnostic!.Code));
            Assert.That(basicXPath.Diagnostic.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(schemaXslt.Diagnostic!.Code, Is.EqualTo(basicXslt.Diagnostic!.Code));
            Assert.That(streamingXslt.Diagnostic!.Code, Is.EqualTo(basicXslt.Diagnostic.Code));
            Assert.That(basicXslt.Diagnostic.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(dtdXml.Diagnostic!.Code, Is.EqualTo(basicXml.Diagnostic!.Code));
            Assert.That(basicXml.Diagnostic.Code, Is.EqualTo(XmlErrorCodes.MalformedMarkup));
        });
    }

    private static ProfileSnapshot Snapshot(XmlDocument document, XsltConformanceLevel level, XmlSchemaSet? schemas, bool schemaAwareXPath)
    {
        XsltExecutable executable = CompileXslt(CompatibleStylesheet(), level, schemas);
        ProfileSnapshot snapshot = new ProfileSnapshot();
        snapshot.CanonicalXml = Canonicalize(document);
        snapshot.XPathValue = EvaluateXPath(document, schemas, schemaAwareXPath);
        snapshot.XsltValue = Transform(executable, document, Name("walk", "", null));
        snapshot.StringValue = new XmlTreeNavigator().GetXmlNodeStringValue(document.DocumentElement!);
        snapshot.DescendantCount = new XmlTreeNavigator().SelectXmlDescendants(document.DocumentElement!).Nodes!.Length;
        snapshot.Diagnostics = document.Diagnostics == null ? 0 : document.Diagnostics.Length;
        snapshot.IsSchemaAware = executable.IsSchemaAware;
        snapshot.IsStreaming = executable.IsStreaming;
        return snapshot;
    }

    private static void AssertEquivalent(ProfileSnapshot actual, ProfileSnapshot expected, string profile)
    {
        Assert.That(actual.CanonicalXml, Is.EqualTo(expected.CanonicalXml), profile + " canonical XML");
        Assert.That(actual.XPathValue, Is.EqualTo(expected.XPathValue), profile + " XPath value");
        Assert.That(actual.XsltValue, Is.EqualTo(expected.XsltValue), profile + " XSLT value");
        Assert.That(actual.StringValue, Is.EqualTo(expected.StringValue), profile + " string value");
        Assert.That(actual.DescendantCount, Is.EqualTo(expected.DescendantCount), profile + " descendants");
    }

    private static string FormatDiagnostics(XmlDiagnostic[]? diagnostics)
    {
        return diagnostics == null ? "no diagnostics" : string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message));
    }

    private static XmlDocument ParseBasic(string source)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput
        {
            Kind = XmlInputKind.String,
            Text = source,
            BaseUri = "https://example.test/profile.xml"
        }, ParserOptions(XmlDtdProcessing.Prohibit, XmlValidationMode.None), null, null, null);
    }

    private static XmlDocument ParseDtd(string source)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput
        {
            Kind = XmlInputKind.String,
            Text = source,
            BaseUri = "https://example.test/profile.xml"
        }, ParserOptions(XmlDtdProcessing.Parse, XmlValidationMode.Dtd), null, null, null);
    }

    private static XmlDocument ParseChunked(string source, int chunkSize)
    {
        ChunkedStream stream = new ChunkedStream(Encoding.UTF8.GetBytes(source), chunkSize);
        return new XmlParser().ParseXmlDocument(new XmlInput
        {
            Kind = XmlInputKind.Stream,
            ByteStream = stream,
            OwnsResource = true,
            BaseUri = "https://example.test/profile.xml"
        }, ParserOptions(XmlDtdProcessing.Prohibit, XmlValidationMode.None), null, null, null);
    }

    private static XmlParserOptions ParserOptions(XmlDtdProcessing dtd, XmlValidationMode validation)
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = dtd,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = validation,
            PreserveCData = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = false,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema =
            "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='" + Namespace + "' xmlns:p='" + Namespace + "' elementFormDefault='qualified' attributeFormDefault='qualified'>" +
            "<xs:attribute name='id' type='xs:ID'/>" +
            "<xs:element name='root'><xs:complexType><xs:sequence>" +
            "<xs:element name='item' minOccurs='2' maxOccurs='2'><xs:complexType><xs:simpleContent><xs:extension base='xs:int'>" +
            "<xs:attribute ref='p:id' use='required'/>" +
            "</xs:extension></xs:simpleContent></xs:complexType></xs:element>" +
            "</xs:sequence></xs:complexType></xs:element></xs:schema>";
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/profile.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied } },
            new XmlDefaultResolver());
    }

    private static XmlSchemaValidationResult ValidateSchema(XmlDocument document, XmlSchemaSet schemas)
    {
        return new XmlSchemaValidator().ValidateXmlSchema(document, schemas, new XmlSchemaValidationOptions
        {
            ProcessContents = XmlSchemaProcessContents.Strict,
            FailFast = false,
            AnnotateTree = true,
            ApplyDefaultValues = false,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        });
    }

    private static string EvaluateXPath(XmlDocument document, XmlSchemaSet? schemas, bool schemaAware)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(
            "string-join(for $item in /p:root/p:item return concat(string($item/@p:id), '=', string($item)), ';')",
            new XPathStaticContext
            {
                Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = Namespace }],
                SchemaAware = schemaAware,
                SchemaSet = schemas
            });
        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!),
            Position = 1,
            Size = 1
        });
        return result.Value!.AtomicValues!.Single().StringValue!;
    }

    private static XPathDynamicException EvaluateXPathFailure(XmlDocument document, XmlSchemaSet? schemas, bool schemaAware)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("1 idiv 0", new XPathStaticContext
        {
            SchemaAware = schemaAware,
            SchemaSet = schemas
        });
        return Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!),
            Position = 1,
            Size = 1
        }))!;
    }

    private static XsltExecutable CompileXslt(string stylesheet, XsltConformanceLevel level, XmlSchemaSet? schemas)
    {
        return new XsltCompiler().CompileXslt(
            new XmlInput { Kind = XmlInputKind.String, Text = stylesheet, BaseUri = "https://example.test/profile.xsl" },
            new XsltCompileOptions
            {
                ConformanceLevel = level,
                SchemaSet = schemas,
                BaseUri = "https://example.test/profile.xsl",
                ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
            },
            new XmlDefaultResolver());
    }

    private static XPathDynamicException EvaluateXsltFailure(XsltConformanceLevel level, XmlSchemaSet? schemas)
    {
        string stylesheet = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'><xsl:sequence select='1 idiv 0'/></xsl:template></xsl:stylesheet>";
        XsltExecutable executable = CompileXslt(stylesheet, level, schemas);
        return Assert.Throws<XPathDynamicException>(() => new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            InitialTemplate = Name("entry", "", null)
        }))!;
    }

    private static string Transform(XsltExecutable executable, XmlDocument document, XmlName mode)
    {
        StringWriter writer = new StringWriter();
        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            SourceDocument = document,
            InitialMode = mode,
            PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer },
            SerializationOptions = new XmlSerializationOptions { Method = XmlSerializationMethod.Text, Encoding = "utf-8" }
        });
        Assert.That(result.Succeeded, Is.True);
        return writer.ToString();
    }

    private static string Canonicalize(XmlDocument document)
    {
        MemoryStream stream = new MemoryStream();
        new XmlCanonicalizer().CanonicalizeXml(document, new XmlOutput
        {
            Kind = XmlOutputKind.Stream,
            ByteStream = stream
        }, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = false
        });
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CompatibleStylesheet()
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:p='" + Namespace + "' exclude-result-prefixes='p' version='3.0'>" +
            "<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='shallow-skip'/>" +
            "<xsl:output method='text'/>" +
            "<xsl:template match='/' mode='walk'><xsl:apply-templates select='p:root/p:item' mode='#current'/></xsl:template>" +
            "<xsl:template match='p:item' mode='walk'><xsl:value-of select='@p:id'/><xsl:text>=</xsl:text><xsl:value-of select='string(.)'/><xsl:text>;</xsl:text></xsl:template>" +
            "</xsl:stylesheet>";
    }

    private static string Source()
    {
        return "<root xmlns='" + Namespace + "' xmlns:p='" + Namespace + "'><item p:id='a'>2</item><item p:id='b'>3</item></root>";
    }

    private static string DtdSource(bool valid)
    {
        string content = valid ? "<item p:id='a'>2</item><item p:id='b'>3</item>" : "<item p:id='a'>2</item>";
        return "<!DOCTYPE root [" +
            "<!ELEMENT root (item,item)>" +
            "<!ATTLIST root xmlns CDATA #IMPLIED xmlns:p CDATA #IMPLIED>" +
            "<!ELEMENT item (#PCDATA)>" +
            "<!ATTLIST item p:id CDATA #REQUIRED>" +
            "]><root xmlns='" + Namespace + "' xmlns:p='" + Namespace + "'>" + content + "</root>";
    }

    private static XmlName Name(string localName, string namespaceUri, string? prefix)
    {
        return new XmlName
        {
            LocalName = localName,
            NamespaceUri = namespaceUri,
            Prefix = prefix,
            QualifiedName = prefix == null || prefix.Length == 0 ? localName : prefix + ":" + localName
        };
    }

    private sealed class ChunkedStream : MemoryStream
    {
        internal int ChunkSize;

        internal ChunkedStream(byte[] source, int chunkSize) : base(source)
        {
            ChunkSize = chunkSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int limited = count < ChunkSize ? count : ChunkSize;
            return base.Read(buffer, offset, limited);
        }
    }

    private sealed class ProfileSnapshot
    {
        internal string CanonicalXml = "";
        internal string XPathValue = "";
        internal string XsltValue = "";
        internal string StringValue = "";
        internal int DescendantCount;
        internal int Diagnostics;
        internal bool IsSchemaAware;
        internal bool IsStreaming;
    }
}
