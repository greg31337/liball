using xml;

namespace xml.Tests;

[TestFixture]
public class XsltRuntimeIntegrationTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Errors = "http://www.w3.org/2005/xqt-errors";

    [Test]
    public void PublicTransformCombinesInitializationParametersModesAndContextFunctions()
    {
        string stylesheet = Stylesheet(
            "<xsl:param name='prefix' as='xs:string' required='yes'/>" +
            "<xsl:variable name='suffix' select=\"'!'\"/>" +
            "<xsl:mode name='m:work'/>" +
            "<xsl:template match='/' mode='m:work'>" +
            "<xsl:apply-templates select='root/item' mode='#current'><xsl:with-param name='label' select='$prefix'/></xsl:apply-templates>" +
            "</xsl:template>" +
            "<xsl:template match='item' mode='m:work'><xsl:param name='label' required='yes'/>" +
            "<xsl:sequence select=\"concat($label, @id, $suffix, ':', position(), '/', last(), ':', name(current()))\"/>" +
            "</xsl:template>",
            "xmlns:m='urn:modes' xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        XsltTransformOptions options = new XsltTransformOptions
        {
            SourceDocument = Parse("<root><item id='a'/><item id='b'/></root>"),
            InitialMode = new XmlName { LocalName = "work", NamespaceUri = "urn:modes" },
            Parameters = [new XsltParameter { Name = new XmlName { LocalName = "prefix" }, Value = AtomicString("item-") }]
        };

        XsltTransformResult result = Transform(stylesheet, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PrincipalValue!.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "item-a!:1/2:item", "item-b!:2/2:item" }));
            Assert.That(result.Options, Is.Not.SameAs(options));
            Assert.That(result.Options!.InitialMode, Is.Not.SameAs(options.InitialMode));
        });
    }

    [Test]
    public void PublicTransformPublishesAnExplicitPrincipalDocumentWithConstructedAndCopiedNodes()
    {
        string stylesheet = Stylesheet(
            "<xsl:template match='/'>" +
            "<xsl:document><out><xsl:namespace name='p' select=\"'urn:p'\"/><xsl:attribute name='id' select=\"'result'\"/>" +
            "<xsl:text>A</xsl:text><xsl:value-of select=\"'B'\"/><xsl:comment select=\"'note'\"/>" +
            "<xsl:processing-instruction name='go' select=\"'now'\"/>" +
            "<xsl:element name='child'><xsl:copy-of select='/root/item/@value'/></xsl:element>" +
            "<xsl:copy select='/root/item'><xsl:attribute name='copied' select=\"'yes'\"/></xsl:copy>" +
            "</out></xsl:document>" +
            "</xsl:template>", "");

        XsltTransformResult result = Transform(stylesheet, new XsltTransformOptions { SourceDocument = Parse("<root><item value='source'/></root>") });
        XmlDocument principal = result.PrincipalDocument!;
        XmlNode root = principal.DocumentElement!;
        XmlNode[] children = root.Children!;

        Assert.Multiple(() =>
        {
            Assert.That(result.PrincipalValue!.Nodes!.Single(), Is.SameAs(principal.DocumentNode));
            Assert.That(root.Name!.LocalName, Is.EqualTo("out"));
            Assert.That(root.Attributes!.Single().Value, Is.EqualTo("result"));
            Assert.That(root.NamespaceBindings!.Any(binding => binding.Prefix == "p" && binding.NamespaceUri == "urn:p"), Is.True);
            Assert.That(children[0].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(children[0].Value, Is.EqualTo("AB"));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Comment));
            Assert.That(children[2].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
            Assert.That(children[3].Name!.LocalName, Is.EqualTo("child"));
            Assert.That(children[3].Attributes!.Single().Value, Is.EqualTo("source"));
            Assert.That(children[4].Name!.LocalName, Is.EqualTo("item"));
            Assert.That(children[4].Attributes!.Single().Name!.LocalName, Is.EqualTo("copied"));
        });
    }

    [Test]
    public void GroundedForkEvaluatesEveryProngInDeclarationOrder()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:fork>" +
            "<xsl:sequence select=\"'first'\"/><xsl:sequence select=\"'second'\"/>" +
            "</xsl:fork></xsl:template>", "");

        XsltTransformResult result = Transform(stylesheet, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });

        Assert.That(result.PrincipalValue!.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void PublicTransformRunsControlFlowRecoveryMessagesAndFallbacksTogether()
    {
        string stylesheet = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:err='" + Errors + "' version='4.0'>" +
            "<xsl:template name='entry'>" +
            "<xsl:if test='true()'><xsl:sequence select=\"'if'\"/></xsl:if>" +
            "<xsl:choose><xsl:when test='false()'/><xsl:otherwise><xsl:sequence select=\"'choose'\"/></xsl:otherwise></xsl:choose>" +
            "<xsl:for-each select='1 to 2'><xsl:sequence select=\"concat('for', .)\"/></xsl:for-each>" +
            "<xsl:iterate select='1 to 3'><xsl:param name='sum' select='0'/>" +
            "<xsl:on-completion select=\"concat('sum', $sum)\"/>" +
            "<xsl:if test='. eq 3'><xsl:break select=\"'break3'\"/></xsl:if>" +
            "<xsl:next-iteration><xsl:with-param name='sum' select='$sum + .'/></xsl:next-iteration>" +
            "</xsl:iterate>" +
            "<xsl:where-populated><empty/><xsl:sequence select=\"'kept'\"/></xsl:where-populated>" +
            "<xsl:try select='1 idiv 0'><xsl:catch errors='err:FOAR0001'>" +
            "<xsl:message select=\"'caught'\"/><xsl:sequence select=\"'recovered'\"/>" +
            "</xsl:catch></xsl:try>" +
            "<xsl:future><xsl:fallback><xsl:sequence select=\"'fallback'\"/></xsl:fallback></xsl:future>" +
            "</xsl:template></xsl:stylesheet>";

        XsltTransformResult result = Transform(stylesheet, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });

        Assert.Multiple(() =>
        {
            Assert.That(result.PrincipalValue!.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "if", "choose", "for1", "for2", "break3", "kept", "recovered", "fallback" }));
            Assert.That(result.PrincipalValue.Nodes, Is.Empty);
            Assert.That(result.Messages!.Single().Text, Is.EqualTo("caught"));
        });
    }

    private static XsltTransformResult Transform(string stylesheet, XsltTransformOptions options)
    {
        return new XsltTransformer().TransformXslt(Compile(stylesheet), options);
    }

    private static XsltExecutable Compile(string stylesheet)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Denied,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = stylesheet, BaseUri = options.BaseUri }, options, new XmlDefaultResolver());
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions { NamespaceHandling = XmlNamespaceHandling.Namespaces10, DtdProcessing = XmlDtdProcessing.Prohibit };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = "https://example.test/source.xml" }, options, null, null, null);
    }

    private static XdmSequence AtomicString(string value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    private static string Stylesheet(string content, string namespaces)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + namespaces + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
