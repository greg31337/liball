using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStreamingExecutionTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void StreamableModeDispatchesTemplatesAndDeliversThePrincipalResult()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='shallow-skip'/>" +
            "<xsl:template match='/' mode='walk'><out><xsl:apply-templates select='root/item' mode='#current'/></out></xsl:template>" +
            "<xsl:template match='item' mode='walk'><value><xsl:value-of select='@id'/></value></xsl:template>");
        StringWriter writer = new StringWriter();
        XsltTransformationState state = Initialize(stylesheet, Parse("<root><item id='a'/><item id='b'/></root>"), new XmlName { LocalName = "walk" }, null, writer);

        state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(writer.ToString(), Is.EqualTo("<out><value>a</value><value>b</value></out>"));
            Assert.That(state.StreamingRuntime!.DeliveredResult, Is.True);
            Assert.That(state.StreamingRuntime.DeliveredItemCount, Is.EqualTo(1));
            Assert.That(state.StreamedNodeRuntime!.Nodes.Count, Is.GreaterThanOrEqualTo(4));
        });
    }

    [Test]
    public void StreamableBuiltInRulesTraverseChildrenInDocumentOrder()
    {
        string stylesheet = Stylesheet("<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='text-only-copy'/>");
        XsltTransformationState state = Initialize(stylesheet, Parse("<root>one<a/>two<b>three</b></root>"), new XmlName { LocalName = "walk" }, null, null);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.That(result.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [TestCase("deep-copy")]
    [TestCase("shallow-copy")]
    public void StreamableCopyBuiltInRulesCreateIndependentResultTrees(string behavior)
    {
        string stylesheet = Stylesheet("<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='" + behavior + "'/>");
        XmlDocument source = Parse("<root code='x'><item>value</item></root>");
        StringWriter writer = new StringWriter();
        XsltTransformationState state = Initialize(stylesheet, source, new XmlName { LocalName = "walk" }, null, writer);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.Nodes![0], Is.Not.SameAs(source.DocumentNode));
            Assert.That(result.Nodes[0].Children![0], Is.Not.SameAs(source.DocumentElement));
            Assert.That(writer.ToString(), Is.EqualTo("<root code=\"x\"><item>value</item></root>"));
        });
    }

    [Test]
    public void StreamedResultDeliveryFlattensArraysAndHonorsItemSeparators()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public'/>" +
            "<xsl:template match='/' mode='walk'><xsl:sequence select='[1, 2]'/></xsl:template>");
        StringWriter writer = new StringWriter();
        XsltTransformationState state = Initialize(stylesheet, Parse("<root/>"), new XmlName { LocalName = "walk" }, null, writer);
        state.PrincipalDestination.SerializationOptions.ItemSeparator = "|";

        state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.That(writer.ToString(), Is.EqualTo("1|2"));
    }

    [Test]
    public void ForkGivesEveryProngAnIndependentViewOfTheInputStream()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public'/>" +
            "<xsl:template match='/' mode='walk'><xsl:fork>" +
            "<xsl:sequence select='string(root/item[1])'/>" +
            "<xsl:sequence select='string(root/item[2])'/>" +
            "</xsl:fork></xsl:template>");
        XsltTransformationState state = Initialize(stylesheet, Parse("<root><item>first</item><item>second</item></root>"), new XmlName { LocalName = "walk" }, null, null);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.That(result.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void StreamableSourceDocumentUsesAndRestoresItsOwnStreamFocus()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public'/>" +
            "<xsl:template match='/' mode='walk'>" +
            "<xsl:source-document href='remote.xml' streamable='yes'><xsl:sequence select='string(/root/item)'/></xsl:source-document>" +
            "<xsl:sequence select='base-uri(.)'/>" +
            "</xsl:template>");
        DocumentResolver resolver = new DocumentResolver();
        XsltTransformationState state = Initialize(stylesheet, Parse("<anchor/>"), new XmlName { LocalName = "walk" }, resolver, null);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "remote", "https://example.test/source.xml" }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(state.DynamicContext.XsltStreamedNodes, Is.SameAs(state.StreamedNodeRuntime));
        });
    }

    [Test]
    public void StreamableAccumulatorsAdvanceWithTemplateEvents()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='count' initial-value='0' streamable='yes'><xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:mode name='walk' streamable='yes' visibility='public' use-accumulators='count' on-no-match='shallow-skip'/>" +
            "<xsl:template match='item' mode='walk'><xsl:sequence select=\"accumulator-before('count')\"/></xsl:template>");
        XsltTransformationState state = Initialize(stylesheet, Parse("<root><item/><item/></root>"), new XmlName { LocalName = "walk" }, null, null);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.CanonicalValue), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(state.AccumulatorRuntime!.Evaluations, Is.Empty);
            Assert.That(state.AccumulatorRuntime.StreamingEvaluations, Has.Count.EqualTo(1));
            Assert.That(state.AccumulatorRuntime.StreamingEvaluations[0].Nodes, Has.Count.EqualTo(4));
        });
    }

    private static XsltTransformationState Initialize(string stylesheet, XmlDocument source, XmlName mode, IXmlResourceResolver? resolver, StringWriter? writer)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = XsltConformanceLevel.Streaming,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = resolver == null ? XmlResourceAccess.Denied : XmlResourceAccess.Allowed,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = stylesheet }, compileOptions, resolver == null ? new DeniedResolver() : resolver);
        XsltTransformOptions transformOptions = new XsltTransformOptions
        {
            SourceDocument = source,
            InitialMode = mode,
            ResourceResolver = resolver,
            PrincipalOutput = writer == null ? null : new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer },
            SerializationOptions = new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, OmitXmlDeclaration = true, NormalizationForm = "none" }
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, transformOptions);
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreserveComments = true,
            PreserveProcessingInstructions = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = "https://example.test/source.xml" }, options, null, null, null);
    }

    private sealed class DocumentResolver : IXmlResourceResolver
    {
        internal int Calls;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = systemIdentifier,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = "<root><item>remote</item></root>", BaseUri = systemIdentifier }
            };
        }
    }

    private sealed class DeniedResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied };
        }
    }
}
