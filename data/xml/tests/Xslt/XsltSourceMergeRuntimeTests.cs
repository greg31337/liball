using xml;

namespace xml.Tests;

[TestFixture]
public class XsltSourceMergeRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void SourceDocumentResolvesRelativeUriSetsFocusAndUsesDocumentPool()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:source-document href='data/{1}.xml'><xsl:sequence select='string(/root/@value), position(), last()'/></xsl:source-document>" +
            "<xsl:source-document href='data/1.xml'><xsl:sequence select='string(/root/@value)'/></xsl:source-document>" +
            "</xsl:template>");
        RuntimeResolver resolver = new RuntimeResolver();
        resolver.DocumentText = "<root value='loaded'/>";

        XdmSequence result = Transform(stylesheet, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(AtomicStrings(result), Is.EqualTo(new[] { "loaded", "1", "1", "loaded" }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(resolver.LastUri, Is.EqualTo("https://example.test/data/1.xml"));
        });
    }

    [Test]
    public void MergeSortsInputsAndExposesCurrentGroupAndKey()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:merge>" +
            "<xsl:merge-source name='left' select=\"('b-left', 'a-left')\" sort-before-merge='yes'><xsl:merge-key select=\"substring-before(., '-')\" order='{\"ascending\"}'/></xsl:merge-source>" +
            "<xsl:merge-source name='right' select=\"('a-right', 'c-right')\" sort-before-merge='yes'><xsl:merge-key order='{\"ascending\"}'><xsl:sequence select=\"substring-before(., '-')\"/></xsl:merge-key></xsl:merge-source>" +
            "<xsl:merge-action><xsl:sequence select=\"current-merge-key(), current-merge-group(), count(current-merge-group('left'))\"/></xsl:merge-action>" +
            "</xsl:merge></xsl:template>");

        XdmSequence result = Transform(stylesheet, null);

        Assert.That(AtomicStrings(result), Is.EqualTo(new[] { "a", "a-left", "a-right", "1", "b", "b-left", "1", "c", "c-right", "0" }));
    }

    [Test]
    public void MergeCreatesInputsForEachAnchorItemAndSourceDocument()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:merge>" +
            "<xsl:merge-source for-each-item=\"('b2', 'a1')\" select='substring(., 1, 1)'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-source for-each-source=\"'remote.xml'\" select='/root/item' sort-before-merge='yes'><xsl:merge-key select='@key'/></xsl:merge-source>" +
            "<xsl:merge-action><xsl:sequence select=\"current-merge-key(), string-join(current-merge-group() ! string(.), ',')\"/></xsl:merge-action>" +
            "</xsl:merge></xsl:template>");
        RuntimeResolver resolver = new RuntimeResolver();
        resolver.DocumentText = "<root><item key='a'>remote</item></root>";

        XdmSequence result = Transform(stylesheet, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(AtomicStrings(result), Is.EqualTo(new[] { "a", "a,remote", "b", "b" }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void MergeReportsOrderingCompatibilityAndTypeErrors()
    {
        string unsorted = MergeStylesheet("<xsl:merge-source select='(2, 1)'><xsl:merge-key select='.'/></xsl:merge-source>");
        string attributes = MergeStylesheet(
            "<xsl:merge-source select='1'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-source select='2'><xsl:merge-key select='.' order='ascending'/></xsl:merge-source>");
        string types = MergeStylesheet(
            "<xsl:merge-source select='1'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-source select=\"'text'\"><xsl:merge-key select='.'/></xsl:merge-source>");

        XsltDynamicException orderingError = Assert.Throws<XsltDynamicException>(() => Transform(unsorted, null))!;
        XsltDynamicException attributeError = Assert.Throws<XsltDynamicException>(() => Transform(attributes, null))!;
        XsltTypeException typeError = Assert.Throws<XsltTypeException>(() => Transform(types, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(orderingError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE2220));
            Assert.That(attributeError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE2210));
            Assert.That(typeError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE2230));
        });
    }

    [Test]
    public void MergeContextFunctionsReportAbsentAndUnknownSources()
    {
        string absentGroup = Stylesheet("<xsl:template name='entry'><xsl:sequence select='current-merge-group()'/></xsl:template>");
        string absentKey = Stylesheet("<xsl:template name='entry'><xsl:sequence select='current-merge-key()'/></xsl:template>");
        string unknown = Stylesheet(
            "<xsl:template name='entry'><xsl:merge><xsl:merge-source name='known' select='1'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-action><xsl:sequence select=\"current-merge-group('missing')\"/></xsl:merge-action></xsl:merge></xsl:template>");

        XPathDynamicException groupError = Assert.Throws<XPathDynamicException>(() => Transform(absentGroup, null))!;
        XPathDynamicException keyError = Assert.Throws<XPathDynamicException>(() => Transform(absentKey, null))!;
        XPathDynamicException nameError = Assert.Throws<XPathDynamicException>(() => Transform(unknown, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(groupError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3480));
            Assert.That(keyError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3510));
            Assert.That(nameError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3490));
        });
    }

    [Test]
    public void CompilerRejectsMismatchedKeysAndMergeKeySelectContent()
    {
        string counts = MergeStylesheet(
            "<xsl:merge-source select='1'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-source select='2'><xsl:merge-key select='.'/><xsl:merge-key select='.'/></xsl:merge-source>");
        string content = MergeStylesheet("<xsl:merge-source select='1'><xsl:merge-key select='.'><xsl:sequence select='.'/></xsl:merge-key></xsl:merge-source>");

        XsltStaticException countError = Assert.Throws<XsltStaticException>(() => Compile(counts))!;
        XsltStaticException contentError = Assert.Throws<XsltStaticException>(() => Compile(content))!;

        Assert.Multiple(() =>
        {
            Assert.That(countError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE2200));
            Assert.That(contentError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3200));
        });
    }

    private static string MergeStylesheet(string sources)
    {
        return Stylesheet("<xsl:template name='entry'><xsl:merge>" + sources + "<xsl:merge-action><xsl:sequence select='current-merge-group()'/></xsl:merge-action></xsl:merge></xsl:template>");
    }

    private static string[] AtomicStrings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence Transform(string stylesheet, IXmlResourceResolver? resolver)
    {
        XsltExecutable executable = Compile(stylesheet);
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            ResourceResolver = resolver
        };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private sealed class RuntimeResolver : IXmlResourceResolver
    {
        internal int Calls;
        internal string? LastUri;
        internal string DocumentText = "<root/>";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            LastUri = systemIdentifier;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = systemIdentifier,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = DocumentText, BaseUri = systemIdentifier }
            };
        }
    }
}
