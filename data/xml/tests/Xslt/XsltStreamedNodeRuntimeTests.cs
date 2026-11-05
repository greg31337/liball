using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStreamedNodeRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void AllowsOneForwardConsumingTraversalFromTheActiveFocus()
    {
        XmlDocument document = Parse("<root><a><leaf/></a><b/></root>");
        XmlNode root = document.DocumentElement!;
        XsltStreamedNodeRuntime streamed = new XsltStreamedNodeRuntime(100);
        streamed.RegisterXsltStreamFocus(root);
        XPathDynamicContext context = Context(root, streamed);

        XdmSequence first = Evaluate("child::*", context);
        XPathDynamicException second = Assert.Throws<XPathDynamicException>(() => Evaluate("child::*", context))!;
        streamed.RegisterXsltStreamFocus(root);
        XPathDynamicException lateral = Assert.Throws<XPathDynamicException>(() => Evaluate("following::*", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Nodes, Is.EqualTo(root.Children));
            Assert.That(second.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(second.Diagnostic.Message, Does.Contain("already been consumed"));
            Assert.That(lateral.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(lateral.Diagnostic.Message, Does.Contain("not available"));
        });
    }

    [Test]
    public void AdvancingFocusExpiresNodesThatAreNoLongerReachable()
    {
        XmlDocument document = Parse("<root><a><leaf/></a><b/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode first = root.Children![0];
        XmlNode second = root.Children[1];
        XsltStreamedNodeRuntime streamed = new XsltStreamedNodeRuntime(100);
        streamed.RegisterXsltStreamFocus(root);
        Evaluate("child::*", Context(root, streamed));
        streamed.RegisterXsltStreamFocus(first);

        XPathDynamicException expired = Assert.Throws<XPathDynamicException>(() => Evaluate(".", Context(second, streamed)))!;
        XdmSequence ancestor = Evaluate("parent::*", Context(first, streamed));
        XmlNode leaf = first.Children![0];
        streamed.RegisterXsltStreamFocus(leaf);
        XdmSequence ancestors = Evaluate("ancestor::*", Context(leaf, streamed));

        Assert.Multiple(() =>
        {
            Assert.That(expired.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(expired.Diagnostic.Message, Does.Contain("no longer available"));
            Assert.That(ancestor.Nodes, Is.EqualTo(new[] { root }));
            Assert.That(ancestors.Nodes, Is.EqualTo(new[] { root, first }));
        });
    }

    [Test]
    public void ElementStringValueConsumesTheStreamedSubtreeOnce()
    {
        XmlDocument document = Parse("<root>value</root>");
        XmlNode root = document.DocumentElement!;
        XsltStreamedNodeRuntime streamed = new XsltStreamedNodeRuntime(100);
        streamed.RegisterXsltStreamFocus(root);
        XPathDynamicContext context = Context(root, streamed);

        XdmSequence value = Evaluate("string(.)", context);
        XPathDynamicException repeated = Assert.Throws<XPathDynamicException>(() => Evaluate("data(.)", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(value.AtomicValues![0].StringValue, Is.EqualTo("value"));
            Assert.That(repeated.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(repeated.Diagnostic.Message, Does.Contain("already been consumed"));
        });
    }

    [TestCase("snapshot")]
    [TestCase("copy-of")]
    public void GroundingFunctionsCreateIndependentTreesBeforeTheFocusExpires(string functionName)
    {
        XmlDocument document = Parse("<root><item/></root>");
        XsltTransformationState state = InitializeGrounding(document, 100, "<xsl:sequence select='" + functionName + "(.)'/>");
        state.StreamedNodeRuntime!.RegisterXsltStreamFocus(document.DocumentNode!);

        state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        state.StreamedNodeRuntime.ExpireXsltStreamedNode(document.DocumentNode!);
        XdmSequence grounded = state.Result.PrincipalValue!;
        XdmSequence name = Evaluate("name(/*)", new XPathDynamicContext { ContextItem = grounded });

        Assert.Multiple(() =>
        {
            Assert.That(grounded.Nodes![0], Is.Not.SameAs(document.DocumentNode));
            Assert.That(name.AtomicValues![0].StringValue, Is.EqualTo("root"));
        });
    }

    [Test]
    public void CopyOfInstructionGroundsAnIndependentTree()
    {
        XmlDocument document = Parse("<root><item/></root>");
        XsltTransformationState state = InitializeGrounding(document, 100, "<xsl:copy-of select='.'/>");
        state.StreamedNodeRuntime!.RegisterXsltStreamFocus(document.DocumentNode!);

        state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        state.StreamedNodeRuntime.ExpireXsltStreamedNode(document.DocumentNode!);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.PrincipalValue!.Nodes![0], Is.Not.SameAs(document.DocumentNode));
            Assert.That(state.Result.PrincipalValue.Nodes[0].Children![0].Name!.LocalName, Is.EqualTo("root"));
        });
    }

    [Test]
    public void GroundingFailsDeterministicallyAtTheConfiguredNodeLimit()
    {
        XmlDocument document = Parse("<root><item/></root>");
        XsltTransformationState state = InitializeGrounding(document, 1, "<xsl:sequence select='snapshot(.)'/>");
        state.StreamedNodeRuntime!.RegisterXsltStreamFocus(document.DocumentNode!);

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(error.Diagnostic.Message, Does.Contain("configured node limit"));
        });
    }

    private static XsltTransformationState InitializeGrounding(XmlDocument document, long maximumNodes, string body)
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry' visibility='public'>" + body + "</xsl:template></xsl:stylesheet>";
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            ConformanceLevel = XsltConformanceLevel.Streaming,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new DeniedResolver());
        XsltTransformOptions options = new XsltTransformOptions
        {
            GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!),
            InitialTemplate = new XmlName { LocalName = "entry" },
            SecurityLimits = new XmlSecurityLimits { MaximumNodeCount = maximumNodes }
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());
        return new XPathRuntime((XPathPlan)expression.Plan!, context).EvaluateXPathPlan();
    }

    private static XPathDynamicContext Context(XmlNode node, XsltStreamedNodeRuntime streamed)
    {
        return new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(node),
            XsltStreamedNodes = streamed
        };
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }

    private sealed class DeniedResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied };
        }
    }
}
