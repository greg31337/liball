using xml;

namespace xml.Tests;

[TestFixture]
public class XPathPathEvaluatorTests
{
    [Test]
    public void EvaluatesAbsoluteChildAttributeAndDescendantPaths()
    {
        XmlDocument document = Parse("<root xmlns='urn:root' xmlns:p='urn:items'><p:item id='1'/><p:item id='2'><leaf/></p:item><other/></root>");
        XPathStaticContext staticContext = Namespaces();
        XPathDynamicContext dynamicContext = Context(document.DocumentNode!);

        XdmSequence attributes = Evaluate("/root/p:item/@id", staticContext, dynamicContext);
        XdmSequence descendants = Evaluate("//p:item", staticContext, dynamicContext);

        Assert.Multiple(() =>
        {
            Assert.That(attributes.Count, Is.EqualTo(2));
            Assert.That(attributes.Nodes!.Select(node => node.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(descendants.Count, Is.EqualTo(2));
            Assert.That(descendants.Nodes!.Select(node => node.Name!.LocalName), Is.EqualTo(new[] { "item", "item" }));
            Assert.That(descendants.Nodes!.Select(node => node.Attributes![0].Value), Is.EqualTo(new[] { "1", "2" }));
        });
    }

    [Test]
    public void AppliesForwardAndReverseAxisPredicatePositions()
    {
        XmlDocument document = Parse("<root><a/><b><x/></b><c/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode b = root.Children![1];
        XmlNode x = b.Children![0];
        XmlNode c = root.Children[2];

        XdmSequence secondChild = Evaluate("child::*[2]", new XPathStaticContext(), Context(root));
        XdmSequence precedingSibling = Evaluate("preceding-sibling::*[1]", new XPathStaticContext(), Context(c));
        XdmSequence nearestAncestor = Evaluate("ancestor::*[1]", new XPathStaticContext(), Context(x));
        XdmSequence nextFollowing = Evaluate("following::*[1]", new XPathStaticContext(), Context(x));
        XdmSequence nearestPreceding = Evaluate("preceding::*[1]", new XPathStaticContext(), Context(c));

        Assert.Multiple(() =>
        {
            Assert.That(secondChild.Nodes, Is.EqualTo(new[] { b }));
            Assert.That(precedingSibling.Nodes, Is.EqualTo(new[] { b }));
            Assert.That(nearestAncestor.Nodes, Is.EqualTo(new[] { b }));
            Assert.That(nextFollowing.Nodes, Is.EqualTo(new[] { c }));
            Assert.That(nearestPreceding.Nodes, Is.EqualTo(new[] { x }));
        });
    }

    [Test]
    public void ParenthesesChangeReverseAxisPredicatePositioning()
    {
        XmlDocument document = Parse("<root><a/><b/><c/></root>");
        XmlNode c = document.DocumentElement!.Children![2];

        XdmSequence axisPredicate = Evaluate("preceding-sibling::*[1]", new XPathStaticContext(), Context(c));
        XdmSequence filterPredicate = Evaluate("(preceding-sibling::*)[1]", new XPathStaticContext(), Context(c));

        Assert.Multiple(() =>
        {
            Assert.That(axisPredicate.Nodes, Is.EqualTo(new[] { document.DocumentElement.Children[1] }));
            Assert.That(filterPredicate.Nodes, Is.EqualTo(new[] { document.DocumentElement.Children[0] }));
        });
    }

    [Test]
    public void AppliesNumericPredicatesToNonNodeSequencesInOriginalOrder()
    {
        XdmSequence result = Evaluate("(10, 20, 30)[2]", new XPathStaticContext(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.AtomicValues![0].IntegerValue, Is.EqualTo(20));
        });
    }

    [Test]
    public void AllowsLookupOnAStaticallyNodeTypedEmptySequence()
    {
        XmlDocument document = Parse("<root/>");
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence result = Evaluate("(//missing)?*", staticContext, Context(document.DocumentNode!));

        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemovesDuplicateNodesAndReturnsDocumentOrder()
    {
        XmlDocument document = Parse("<root><a/><b/><c/></root>");
        XmlNode root = document.DocumentElement!;

        XdmSequence parent = Evaluate("child::*/..", new XPathStaticContext(), Context(root));
        XdmSequence ancestors = Evaluate("child::*/ancestor-or-self::*", new XPathStaticContext(), Context(root));

        Assert.Multiple(() =>
        {
            Assert.That(parent.Count, Is.EqualTo(1));
            Assert.That(parent.Nodes, Is.EqualTo(new[] { root }));
            Assert.That(ancestors.Nodes, Is.EqualTo(new[] { root, root.Children![0], root.Children[1], root.Children[2] }));
        });
    }

    [Test]
    public void NormalizesLargeDescendantResultsInDocumentOrder()
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder("<root>");
        int index = 0;
        while (index < 200)
        {
            source.Append("<group><item id='");
            source.Append(index);
            source.Append("'/></group>");
            index = index + 1;
        }

        source.Append("</root>");
        XmlDocument document = Parse(source.ToString());

        XdmSequence result = Evaluate("//item | //item", new XPathStaticContext(), Context(document.DocumentNode!));

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(200));
            Assert.That(result.Nodes![0].Attributes![0].Value, Is.EqualTo("0"));
            Assert.That(result.Nodes[199].Attributes![0].Value, Is.EqualTo("199"));
        });
    }

    [Test]
    public void EvaluatesKindNameWildcardParentAndNamespaceAxes()
    {
        XmlDocument document = Parse("<root xmlns='urn:root' xmlns:p='urn:items' id='r'>text<!--comment--><?go value?><p:item/></root>");
        XmlNode root = document.DocumentElement!;
        XPathStaticContext staticContext = Namespaces();

        XdmSequence text = Evaluate("child::text()", staticContext, Context(root));
        XdmSequence comment = Evaluate("child::comment()", staticContext, Context(root));
        XdmSequence processingInstruction = Evaluate("child::processing-instruction('go')", staticContext, Context(root));
        XdmSequence namespaceNodes = Evaluate("namespace::*", staticContext, Context(root));
        XdmSequence namespaceKindNodes = Evaluate("namespace-node()", staticContext, Context(root));
        XdmSequence parent = Evaluate("..", staticContext, Context(root.Attributes![0]));
        XdmSequence namespaced = Evaluate("child::p:*", staticContext, Context(root));

        Assert.Multiple(() =>
        {
            Assert.That(text.Count, Is.EqualTo(1));
            Assert.That(text.Nodes![0].Value, Is.EqualTo("text"));
            Assert.That(comment.Count, Is.EqualTo(1));
            Assert.That(processingInstruction.Count, Is.EqualTo(1));
            Assert.That(processingInstruction.Nodes![0].Name!.LocalName, Is.EqualTo("go"));
            Assert.That(namespaceNodes.Nodes!.Select(node => node.Name!.LocalName), Does.Contain("p"));
            Assert.That(namespaceKindNodes.Nodes!.Select(node => node.Name!.LocalName), Does.Contain("xml"));
            Assert.That(parent.Nodes, Is.EqualTo(new[] { root }));
            Assert.That(namespaced.Nodes, Is.EqualTo(new[] { root.Children![3] }));
        });
    }

    [Test]
    public void ReportsContextAndMixedPathTypeErrorsAtTheResponsibleStep()
    {
        XPathDynamicContext atomicContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmIntegerAtomicValue("1", XdmAtomicType.Integer))
        };
        XPathDynamicException contextError = Assert.Throws<XPathDynamicException>(() => Evaluate("child::*", new XPathStaticContext(), atomicContext))!;

        XmlDocument document = Parse("<root id='r'/>");
        XPathDynamicException mixedError = Assert.Throws<XPathDynamicException>(() => Evaluate("/root/(1, @id)", new XPathStaticContext(), Context(document.DocumentNode!)))!;

        Assert.Multiple(() =>
        {
            Assert.That(contextError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0020));
            Assert.That(contextError.Diagnostic.Range, Is.Not.Null);
            Assert.That(mixedError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0018));
            Assert.That(mixedError.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void ProvidesStableNodeIdentityAndCurrentDocumentOrder()
    {
        XmlDocument document = Parse("<root><a/><b/></root>");
        XmlNode root = document.DocumentElement!;
        XPathRuntime runtime = RuntimeFor(".", Context(root));
        XdmNode wrappedRoot = runtime.Paths.Nodes.WrapXmlNode(root);
        XdmNode wrappedAgain = runtime.Paths.Nodes.WrapXmlNode(root);
        XdmNode a = runtime.Paths.Nodes.WrapXmlNode(root.Children![0]);
        XdmNode b = runtime.Paths.Nodes.WrapXmlNode(root.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Paths.HasSameXPathNodeIdentity(wrappedRoot, wrappedAgain), Is.True);
            Assert.That(runtime.Paths.HasSameXPathNodeIdentity(a, b), Is.False);
            Assert.That(runtime.Paths.CompareXPathNodeOrder(a, b), Is.LessThan(0));
            Assert.That(runtime.Paths.CompareXPathNodeOrder(b, a), Is.GreaterThan(0));
        });

        new XmlTreeEditor().InsertXmlTreeBefore(root, b.Node, a.Node);
        Assert.That(runtime.Paths.CompareXPathNodeOrder(b, a), Is.LessThan(0));
    }

    [Test]
    public void RejectsNavigationAndReorderingForMarkedStreamedNodes()
    {
        XmlDocument document = Parse("<root><a/></root>");
        XmlNode root = document.DocumentElement!;
        XPathRuntime runtime = RuntimeFor("descendant::*", Context(root));
        runtime.Paths.MarkXPathStreamedNode(root);

        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => runtime.EvaluateXPathPlan())!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathDynamic));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathRuntime RuntimeFor(string source, XPathDynamicContext context)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());
        return new XPathRuntime((XPathPlan)expression.Plan!, context);
    }

    private static XPathStaticContext Namespaces()
    {
        return new XPathStaticContext
        {
            DefaultElementNamespace = "urn:root",
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:items" }]
        };
    }

    private static XPathDynamicContext Context(XmlNode node)
    {
        return new XPathDynamicContext { ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(node) };
    }

    private static XmlDocument Parse(string source)
    {
        XmlInput input = new XmlInput { Text = source };
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreserveComments = true,
            PreserveProcessingInstructions = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
