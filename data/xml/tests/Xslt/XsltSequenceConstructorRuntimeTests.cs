using xml;

namespace xml.Tests;

[TestFixture]
public class XsltSequenceConstructorRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ConstructsLiteralAndComputedNodesWithValueTemplates()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:param name='who' select=\"'world'\"/>" +
            "<result greeting='Hello {$who}'>" +
            "<xsl:attribute name='p:code' namespace='urn:p' select=\"'42'\"/>" +
            "<xsl:namespace name='q' select=\"'urn:q'\"/>" +
            "<xsl:text>start</xsl:text><xsl:value-of select='1 to 2' separator=','/>" +
            "<xsl:comment select=\"'ok'\"/><xsl:processing-instruction name='go' select=\"'now'\"/>" +
            "<xsl:element name='q:child' namespace='urn:q'><xsl:text>body</xsl:text></xsl:element>" +
            "</result></xsl:template>",
            "xmlns:p='urn:p'");
        XsltTemplateRuntime runtime = Initialize(source).TemplateRuntime!;

        XdmSequence value = runtime.ExecuteXsltInitialTemplateInvocation();
        XmlNode result = value.Nodes!.Single();
        XmlNode[] children = result.Children!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Name!.LocalName, Is.EqualTo("result"));
            Assert.That(result.Attributes!.Single(item => item.Name!.LocalName == "greeting").Value, Is.EqualTo("Hello world"));
            Assert.That(result.Attributes!.Single(item => item.Name!.LocalName == "code").Name!.NamespaceUri, Is.EqualTo("urn:p"));
            Assert.That(result.NamespaceBindings!.Any(item => item.Prefix == "q" && item.NamespaceUri == "urn:q"), Is.True);
            Assert.That(children[0].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(children[0].Value, Is.EqualTo("start1,2"));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Comment));
            Assert.That(children[1].Value, Is.EqualTo("ok"));
            Assert.That(children[2].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
            Assert.That(children[2].Name!.LocalName, Is.EqualTo("go"));
            Assert.That(children[3].Name!.LocalName, Is.EqualTo("child"));
            Assert.That(children[3].Name!.NamespaceUri, Is.EqualTo("urn:q"));
            Assert.That(children[3].Children!.Single().Value, Is.EqualTo("body"));
        });
    }

    [Test]
    public void PreservesRawSequenceValuesAndNormalizesConstructedContent()
    {
        string rawSource = Stylesheet("<xsl:template name='entry'><xsl:sequence select='1 to 2'/></xsl:template>", "");
        XdmSequence raw = Initialize(rawSource).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        string documentSource = Stylesheet(
            "<xsl:template name='entry'><xsl:document><root><xsl:sequence select='1 to 2'/>tail</root></xsl:document></xsl:template>",
            "");
        XdmSequence constructed = Initialize(documentSource).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XmlNode document = constructed.Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(raw.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic }));
            Assert.That(raw.AtomicValues!.Select(item => item.IntegerValue), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(document.Kind, Is.EqualTo(XmlNodeKind.Document));
            Assert.That(document.Children!.Single().Name!.LocalName, Is.EqualTo("root"));
            Assert.That(document.Children!.Single().Children!.Single().Value, Is.EqualTo("1 2tail"));
        });
    }

    [Test]
    public void EvaluatesGlobalAndLocalVariableSequenceConstructors()
    {
        string source = Stylesheet(
            "<xsl:variable name='global'><xsl:sequence select='1 to 2'/></xsl:variable>" +
            "<xsl:template name='entry'><xsl:variable name='local'><xsl:text>local</xsl:text></xsl:variable>" +
            "<xsl:sequence select='$global'/><xsl:sequence select='$local'/></xsl:template>",
            "");
        XsltTransformationState state = Initialize(source);

        XdmSequence value = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(value.Count, Is.EqualTo(3));
            Assert.That(value.AtomicValues!.Select(item => item.IntegerValue), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(value.Nodes!.Single().Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(value.Nodes!.Single().Value, Is.EqualTo("local"));
            Assert.That(state.VariableRuntime!.Globals.Single().Status, Is.EqualTo(XsltVariableEvaluationStatus.Succeeded));
        });
    }

    [Test]
    public void AppliesNamespaceAliasesAndExcludesUnusedNamespaces()
    {
        string source = Stylesheet(
            "<xsl:namespace-alias stylesheet-prefix='s' result-prefix='r'/>" +
            "<xsl:template name='entry'><s:item xmlns:unused='urn:unused' xsl:exclude-result-prefixes='unused'/></xsl:template>",
            "xmlns:s='urn:source' xmlns:r='urn:result'");
        XdmSequence value = Initialize(source).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XmlNode item = value.Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.Name!.Prefix, Is.EqualTo("r"));
            Assert.That(item.Name.NamespaceUri, Is.EqualTo("urn:result"));
            Assert.That(item.NamespaceBindings!.Any(binding => binding.Prefix == "r" && binding.NamespaceUri == "urn:result"), Is.True);
            Assert.That(item.NamespaceBindings!.Any(binding => binding.NamespaceUri == "urn:unused"), Is.False);
            Assert.That(item.NamespaceBindings!.Any(binding => binding.NamespaceUri == Xsl), Is.False);
        });
    }

    [Test]
    public void EnforcesAttributeDocumentAndComputedNameConstraints()
    {
        string lateAttribute = Stylesheet(
            "<xsl:template name='entry'><xsl:element name='item'><xsl:text>child</xsl:text><xsl:attribute name='late'/></xsl:element></xsl:template>",
            "");
        string documentAttribute = Stylesheet(
            "<xsl:template name='entry'><xsl:document><xsl:attribute name='bad'/></xsl:document></xsl:template>",
            "");
        string invalidName = Stylesheet(
            "<xsl:template name='entry'><xsl:element name='{\"two:colons:bad\"}'/></xsl:template>",
            "");

        XsltDynamicException late = Assert.Throws<XsltDynamicException>(() => Initialize(lateAttribute).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException document = Assert.Throws<XsltDynamicException>(() => Initialize(documentAttribute).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException name = Assert.Throws<XsltDynamicException>(() => Initialize(invalidName).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(late.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0410));
            Assert.That(document.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0420));
            Assert.That(name.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0820));
        });
    }

    [Test]
    public void RejectsInvalidCommentProcessingInstructionAndNamespaceValues()
    {
        string commentSource = Stylesheet("<xsl:template name='entry'><xsl:comment select=\"'bad--comment'\"/></xsl:template>", "");
        string processingSource = Stylesheet("<xsl:template name='entry'><xsl:processing-instruction name='go' select=\"'bad?>data'\"/></xsl:template>", "");
        string namespaceSource = Stylesheet("<xsl:template name='entry'><xsl:namespace name='xml' select=\"'urn:not-xml'\"/></xsl:template>", "");

        XsltDynamicException comment = Assert.Throws<XsltDynamicException>(() => Initialize(commentSource).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException processing = Assert.Throws<XsltDynamicException>(() => Initialize(processingSource).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException ns = Assert.Throws<XsltDynamicException>(() => Initialize(namespaceSource).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(comment.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0980));
            Assert.That(processing.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0905));
            Assert.That(ns.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0930));
        });
    }

    private static XsltTransformationState Initialize(string source)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions
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
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions transformOptions = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, transformOptions);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
