using xml;

namespace xml.Tests;

[TestFixture]
public class XsltPublicRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void TransformerExecutesAndPublishesMessagesResultsAndTraceEvents()
    {
        string stylesheet = Stylesheet(
            "<xsl:function name='f:value'><xsl:sequence select=\"'done'\"/></xsl:function>" +
            "<xsl:template name='entry'>" +
            "<xsl:message select=\"'hello'\"/>" +
            "<xsl:result-document href='secondary.xml'><secondary uri='{current-output-uri()}'/></xsl:result-document>" +
            "<principal><xsl:value-of select='f:value()'/></principal>" +
            "</xsl:template>", "xmlns:f='urn:functions'");
        XsltMessageCollector messages = new XsltMessageCollector();
        XsltTraceCollector traces = new XsltTraceCollector();
        XsltResultDocumentCollector results = new XsltResultDocumentCollector();
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            BaseOutputUri = "https://example.test/results/main.xml",
            MessageReceiver = messages,
            TraceReceiver = traces,
            ResultDocumentResolver = results,
            EnableTracing = true
        };

        XsltTransformResult transformed = new XsltTransformer().TransformXslt(Compile(stylesheet), options);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.Succeeded, Is.True);
            Assert.That(transformed.PrincipalValue!.Nodes!.Single().Name!.LocalName, Is.EqualTo("principal"));
            Assert.That(transformed.Messages!.Single().Text, Is.EqualTo("hello"));
            Assert.That(messages.SnapshotXsltMessages().Single().Text, Is.EqualTo("hello"));
            Assert.That(transformed.SecondaryResults!.Single().Uri, Is.EqualTo("https://example.test/results/secondary.xml"));
            Assert.That(transformed.SecondaryResults!.Single().Value!.Nodes!.Single().Attributes!.Single().Value, Is.EqualTo("https://example.test/results/secondary.xml"));
            Assert.That(results.SnapshotXsltResultDocuments().Single().Disposition, Is.EqualTo(XsltResultDisposition.Commit));
            Assert.That(transformed.TraceEvents, Is.Not.Empty);
            Assert.That(transformed.TraceEvents!.Select(value => value.Kind), Does.Contain(XsltTraceEventKind.EnterTemplate));
            Assert.That(transformed.TraceEvents!.Select(value => value.Kind), Does.Contain(XsltTraceEventKind.EnterFunction));
            Assert.That(transformed.TraceEvents!.Select(value => value.Kind), Does.Contain(XsltTraceEventKind.EnterInstruction));
            Assert.That(traces.SnapshotXsltTraceEvents(), Has.Length.EqualTo(transformed.TraceEvents!.Length));
        });
    }

    [Test]
    public void TransformerExecutesAPublicInitialFunction()
    {
        string stylesheet = "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:package' package-version='1' version='3.0'>" +
            "<xsl:function name='f:start' visibility='public'><xsl:sequence select=\"'started'\"/></xsl:function>" +
            "</xsl:package>";
        XsltTransformOptions options = new XsltTransformOptions { InitialFunction = new XmlName { LocalName = "start", NamespaceUri = "urn:functions" } };

        XsltTransformResult result = new XsltTransformer().TransformXslt(Compile(stylesheet), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("started"));
        });
    }

    [Test]
    public void XsltSpecificFunctionsUseTheCurrentItemOutputAndStaticEnvironment()
    {
        string body = "<xsl:template match='/'>" +
            "<xsl:for-each select='/root/item'>" +
            "<xsl:sequence select=\"concat(string(child/@id[current()/@id = 'outer']), '|', string(current-output-uri()), '|', count(available-system-properties()), '|', function-available('fn:substring', 2), '|', type-available('xs:normalizedString'), '|', system-property('xsl:supports-serialization'), '|', stream-available('source.xml'), '|', name(document('')/*))\"/>" +
            "</xsl:for-each>" +
            "</xsl:template>";
        XmlDocument source = Parse("<root><item id='outer'><child id='inner'/></item></root>", "https://example.test/source.xml");
        XsltTransformOptions options = new XsltTransformOptions
        {
            SourceDocument = source,
            BaseOutputUri = "https://example.test/results/main.xml"
        };

        XsltTransformResult result = new XsltTransformer().TransformXslt(Compile(Stylesheet(body, "")), options);

        Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("inner|https://example.test/results/main.xml|14|true|true|yes|false|xsl:stylesheet"));
    }

    [Test]
    public void CopyOfAndSnapshotCreateIndependentTreesWithTheRequiredAncestors()
    {
        string body = "<xsl:template match='/'>" +
            "<xsl:sequence select=\"concat(exists(copy-of(/root/item)/..), '|', name(snapshot(/root/item)/..), '|', count(snapshot(/root/item)/../*))\"/>" +
            "</xsl:template>";
        XmlDocument source = Parse("<root><before/><item><child/></item><after/></root>", "https://example.test/source.xml");

        XsltTransformResult result = new XsltTransformer().TransformXslt(Compile(Stylesheet(body, "")), new XsltTransformOptions { SourceDocument = source });

        Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("false|root|1"));
    }

    [Test]
    public void FunctionFailuresCarryTemplateFunctionAndComponentStacks()
    {
        string declarations =
            "<xsl:function name='f:inner'><xsl:sequence select=\"error(QName('urn:error', 'e:boom'), 'failure')\"/></xsl:function>" +
            "<xsl:function name='f:outer'><xsl:sequence select='f:inner()'/></xsl:function>" +
            "<xsl:template name='entry'><xsl:sequence select='f:outer()'/></xsl:template>";
        XsltExecutable executable = Compile(Stylesheet(declarations, "xmlns:f='urn:functions' xmlns:e='urn:error'"));

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } }))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.TemplateStack, Is.EqualTo(new[] { "Q{}entry" }));
            Assert.That(error.Diagnostic.FunctionStack, Is.EqualTo(new[] { "Q{urn:functions}outer#0", "Q{urn:functions}inner#0" }));
            Assert.That(error.Diagnostic.ComponentStack, Has.Length.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void DocumentFunctionUsesTheTransformationResourceResolver()
    {
        string stylesheet = Stylesheet("<xsl:template match='/'><xsl:sequence select=\"string(document(/root/@href)/root/@id)\"/></xsl:template>", "");
        DocumentResolver resolver = new DocumentResolver();
        XsltTransformOptions options = new XsltTransformOptions
        {
            SourceDocument = Parse("<root href='data.xml'/>", "https://source.test/folder/source.xml"),
            ResourceResolver = resolver,
            SecurityLimits = new XmlSecurityLimits { MaximumExternalResources = 4 }
        };

        XsltTransformResult result = new XsltTransformer().TransformXslt(Compile(stylesheet), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("loaded"));
            Assert.That(resolver.Uri, Is.EqualTo("https://source.test/folder/data.xml"));
        });
    }

    [Test]
    public void UnparsedEntityFunctionsReadRetainedDocumentDeclarations()
    {
        string stylesheet = Stylesheet("<xsl:template match='/'><xsl:sequence select=\"concat(unparsed-entity-public-id('logo', /), '|', unparsed-entity-uri('logo', /))\"/></xsl:template>", "");
        string xml = "<!DOCTYPE root [<!NOTATION image SYSTEM 'image/png'><!ENTITY logo PUBLIC 'public-logo' 'logo.bin' NDATA image>]><root/>";
        XmlParserOptions parserOptions = new XmlParserOptions { NamespaceHandling = XmlNamespaceHandling.Namespaces10, DtdProcessing = XmlDtdProcessing.Parse };
        XmlDocument source = new XmlParser().ParseXmlDocument(new XmlInput { Text = xml, BaseUri = "https://example.test/source.xml" }, parserOptions, null, null, null);

        XsltTransformResult result = new XsltTransformer().TransformXslt(Compile(stylesheet), new XsltTransformOptions { SourceDocument = source });

        Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("public-logo|https://example.test/logo.bin"));
    }

    private static string Stylesheet(string content, string namespaces)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + namespaces + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 67108864,
                MaximumWorkUnits = 67108864
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source, BaseUri = options.BaseUri }, options, new XmlDefaultResolver());
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = baseUri }, options, null, null, null);
    }

    private sealed class DocumentResolver : IXmlResourceResolver
    {
        internal string? Uri;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Uri = systemIdentifier;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = systemIdentifier,
                Input = new XmlInput { Text = "<root id='loaded'/>", BaseUri = systemIdentifier }
            };
        }
    }
}
