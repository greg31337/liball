using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStreamingConformanceTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void StreamingAndGroundedExecutionProduceEquivalentOutput()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='shallow-skip'/>" +
            "<xsl:output method='xml' omit-xml-declaration='yes'/>" +
            "<xsl:template match='/' mode='walk'><out><xsl:apply-templates select='root/item' mode='#current'/></out></xsl:template>" +
            "<xsl:template match='item' mode='walk'><value id='{@id}'><xsl:value-of select='string(.)'/></value></xsl:template>");
        XmlDocument groundedSource = Parse("<root><item id='a'>one</item><item id='b'>two</item></root>");
        XmlDocument streamedSource = Parse("<root><item id='a'>one</item><item id='b'>two</item></root>");

        string grounded = Transform(Compile(stylesheet, XsltConformanceLevel.Basic, true), groundedSource);
        string streamed = Transform(Compile(stylesheet, XsltConformanceLevel.Streaming, false), streamedSource);

        Assert.Multiple(() =>
        {
            Assert.That(streamed, Is.EqualTo(grounded));
            Assert.That(streamed, Is.EqualTo("<out><value id=\"a\">one</value><value id=\"b\">two</value></out>"));
        });
    }

    [Test]
    public void StreamFocusEnforcesLifetimeOrderingAndGroundingBudget()
    {
        XmlDocument document = Parse("<root><first><leaf/></first><second/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode first = root.Children![0];
        XmlNode second = root.Children[1];
        XsltStreamedNodeRuntime streamed = new XsltStreamedNodeRuntime(1);
        streamed.RegisterXsltStreamFocus(root);
        Assert.That(streamed.AuthorizeXsltStreamNavigation(root, XPathAxis.Child), Is.EqualTo(XsltStreamNavigationStatus.Allowed));
        streamed.RegisterXsltStreamNavigationResult([first, second]);
        streamed.RegisterXsltStreamFocus(first);

        Assert.Multiple(() =>
        {
            Assert.That(streamed.IsXsltStreamedNodeAvailable(second), Is.False);
            Assert.That(streamed.AuthorizeXsltStreamNavigation(second, XPathAxis.Self), Is.EqualTo(XsltStreamNavigationStatus.Expired));
            Assert.That(streamed.ReserveXsltStreamGrounding(first, false), Is.False);
            Assert.That(streamed.GroundedNodeCount, Is.Zero);
        });
    }

    [Test]
    public void PublicReportPublishesPostureSweepAndSourceLocation()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes'/>" +
            "<xsl:template match='item' mode='walk'><xsl:sequence select='child::value'/></xsl:template>");

        XsltStreamabilityReport report = Compile(stylesheet, XsltConformanceLevel.Basic, true).StreamabilityReport!;
        XsltStreamabilityConstruct child = report.Constructs!.First(value => value.Kind == "expression" && value.Posture == XsltPosture.Striding && value.Sweep == XsltSweep.Consuming);

        Assert.Multiple(() =>
        {
            Assert.That(report.Streamability, Is.EqualTo(XsltStreamability.Streamable));
            Assert.That(child.Streamability, Is.EqualTo(XsltStreamability.Streamable));
            Assert.That(child.ConsumingOperandCount, Is.EqualTo(1));
            Assert.That(child.Location!.Start!.Line, Is.GreaterThan(0));
            Assert.That(child.RequiresFallback, Is.False);
        });
    }

    [Test]
    public void ForkProngsConsumeIndependentlyWhileAccumulatorsAdvanceOnce()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='count' initial-value='0' streamable='yes'><xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:mode name='walk' streamable='yes' visibility='public' use-accumulators='count' on-no-match='shallow-skip'/>" +
            "<xsl:template match='item' mode='walk'><xsl:fork>" +
            "<xsl:sequence select=\"concat(@id, ':', accumulator-before('count'))\"/>" +
            "<xsl:sequence select='string(.)'/>" +
            "</xsl:fork></xsl:template>");
        XsltTransformationState state = Initialize(Compile(stylesheet, XsltConformanceLevel.Streaming, false), Parse("<root><item id='a'>one</item><item id='b'>two</item></root>"));

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue), Is.EqualTo(new[] { "a:1", "one", "b:2", "two" }));
            Assert.That(state.AccumulatorRuntime!.StreamingEvaluations, Has.Count.EqualTo(1));
            Assert.That(state.StreamedNodeRuntime!.IsXsltStreamedNodeConsumed(state.InitialMatchSelection.Nodes![0]), Is.True);
        });
    }

    [Test]
    public void StreamingConformanceRejectsWhatBasicExecutionGrounds()
    {
        string stylesheet = Stylesheet(
            "<xsl:output method='text'/>" +
            "<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='shallow-skip'/>" +
            "<xsl:template match='item' mode='walk'><xsl:value-of select='count(following-sibling::item)'/></xsl:template>");

        XsltStaticException rejected = Assert.Throws<XsltStaticException>(() => Compile(stylesheet, XsltConformanceLevel.Streaming, false))!;
        XsltExecutable fallback = Compile(stylesheet, XsltConformanceLevel.Basic, true);
        string output = Transform(fallback, Parse("<root><item/><item/></root>"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3430));
            Assert.That(fallback.IsStreaming, Is.False);
            Assert.That(fallback.StreamabilityReport!.Streamability, Is.EqualTo(XsltStreamability.NotStreamable));
            Assert.That(fallback.StreamabilityReport.RequiresFallback, Is.True);
            Assert.That(output, Is.EqualTo("10"));
        });
    }

    [Test]
    public void StreamingOutputIsInvariantAcrossSourceChunkSizes()
    {
        string stylesheet = Stylesheet(
            "<xsl:mode name='walk' streamable='yes' visibility='public' on-no-match='shallow-skip'/>" +
            "<xsl:output method='xml' omit-xml-declaration='yes'/>" +
            "<xsl:template match='/' mode='walk'><out><xsl:apply-templates select='root/item' mode='#current'/></out></xsl:template>" +
            "<xsl:template match='item' mode='walk'><value><xsl:value-of select='string(.)'/></value></xsl:template>");
        XsltExecutable executable = Compile(stylesheet, XsltConformanceLevel.Streaming, false);
        string source = "<?xml version='1.0' encoding='UTF-8'?><root><item>café</item><item>😀&amp;tail</item></root>";
        string expected = Transform(executable, Parse(source));
        int chunkSize = 1;
        while (chunkSize <= 11)
        {
            XmlDocument chunked = ParseChunked(source, chunkSize);
            Assert.That(Transform(executable, chunked), Is.EqualTo(expected), "chunk size " + chunkSize);
            chunkSize = chunkSize + 1;
        }
    }

    private static XsltExecutable Compile(string stylesheet, XsltConformanceLevel level, bool analyze)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            ConformanceLevel = level,
            AnalyzeStreamability = analyze,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = stylesheet }, options, new DeniedResolver());
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, XmlDocument source)
    {
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, new XsltTransformOptions
        {
            SourceDocument = source,
            InitialMode = new XmlName { LocalName = "walk" },
            SerializationOptions = new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, OmitXmlDeclaration = true, NormalizationForm = "none" }
        });
    }

    private static string Transform(XsltExecutable executable, XmlDocument source)
    {
        StringWriter writer = new StringWriter();
        XsltTransformOptions options = new XsltTransformOptions
        {
            SourceDocument = source,
            InitialMode = new XmlName { LocalName = "walk" },
            PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer },
            SerializationOptions = new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, OmitXmlDeclaration = true, NormalizationForm = "none" }
        };
        new XsltTransformer().TransformXslt(executable, options);
        return writer.ToString();
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XmlDocument Parse(string source)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, ParserOptions(), null, null, null);
    }

    private static XmlDocument ParseChunked(string source, int chunkSize)
    {
        ChunkedReadStream stream = new ChunkedReadStream(Encoding.UTF8.GetBytes(source), chunkSize);
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream }, ParserOptions(), null, null, null);
    }

    private static XmlParserOptions ParserOptions()
    {
        return new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreserveComments = true,
            PreserveProcessingInstructions = true
        };
    }

    private sealed class ChunkedReadStream : MemoryStream
    {
        internal int ChunkSize;

        internal ChunkedReadStream(byte[] bytes, int chunkSize) : base(bytes)
        {
            ChunkSize = chunkSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, System.Math.Min(count, ChunkSize));
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
