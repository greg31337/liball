using xml;

namespace xml.Tests;

[TestFixture]
public class XPathNodeFunctionTests
{
    [Test]
    public void ReturnsNodeNamesStringValuesTypedValuesAndNilledState()
    {
        XmlDocument document = Parse("<root xmlns='urn:root' xmlns:p='urn:item'><p:item>one<part/>two</p:item><nil/></root>", null);
        XmlNode nilledElement = document.DocumentElement!.Children![1];
        nilledElement.TypeInfo = new XmlSchemaTypeInfo { IsNilled = true };

        XdmAtomicValue[] names = Evaluate("(name(/*), local-name(/*), namespace-uri(/*), name(/*/*[1]), local-name(/*/*[1]), namespace-uri(/*/*[1]))", Context(document.DocumentNode!)).AtomicValues!;
        XdmAtomicValue qname = Evaluate("node-name(/*/*[1])", Context(document.DocumentNode!)).AtomicValues![0];
        XdmSequence values = Evaluate("(string(/*/*[1]), data(/*/*[1]))", Context(document.DocumentNode!));
        XdmAtomicValue[] atomics = values.AtomicValues!;
        XdmSequence nilled = Evaluate("nilled(/*/*[2])", Context(document.DocumentNode!));
        XdmSequence unnamed = Evaluate("node-name(/)", Context(document.DocumentNode!));
        XdmSequence nonElementNilled = Evaluate("nilled(/)", Context(document.DocumentNode!));

        Assert.Multiple(() =>
        {
            Assert.That(names.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "root", "root", "urn:root", "p:item", "item", "urn:item" }));
            Assert.That(names[2].Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(qname.Type, Is.EqualTo(XdmAtomicType.QName));
            Assert.That(qname.NameValue!.Prefix, Is.EqualTo("p"));
            Assert.That(qname.NameValue.NamespaceUri, Is.EqualTo("urn:item"));
            Assert.That(atomics.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "onetwo", "onetwo" }));
            Assert.That(atomics[0].Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(atomics[1].Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
            Assert.That(nilled.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(unnamed.Count, Is.Zero);
            Assert.That(nonElementNilled.Count, Is.Zero);
        });
    }

    [Test]
    public void ReturnsUrisRootsChildrenAndCanonicalPaths()
    {
        XmlDocument document = Parse("<root xmlns='urn:root' xmlns:p='urn:item' xml:base='section/'><p:item id='x'>first<part/>second</p:item></root>", "https://example.test/doc.xml");
        XPathDynamicContext context = Context(document.DocumentNode!);

        XdmAtomicValue documentUri = Evaluate("document-uri(/)", context).AtomicValues![0];
        XdmAtomicValue baseUri = Evaluate("base-uri(/*/*)", context).AtomicValues![0];
        XdmSequence root = Evaluate("root(/*/*/@*)", context);
        XdmAtomicValue[] children = Evaluate("(has-children(/), has-children(/*/*), has-children(/*/*/@*), has-children(()))", context).AtomicValues!;
        string attributePath = Evaluate("path(/*/*/@*)", context).AtomicValues![0].StringValue!;
        string textPath = Evaluate("path(/*/*/text()[2])", context).AtomicValues![0].StringValue!;
        string documentPath = Evaluate("path(/)", context).AtomicValues![0].StringValue!;

        Assert.Multiple(() =>
        {
            Assert.That(documentUri.Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(documentUri.StringValue, Is.EqualTo("https://example.test/doc.xml"));
            Assert.That(baseUri.StringValue, Is.EqualTo("https://example.test/section/"));
            Assert.That(root.Nodes, Is.EqualTo(new[] { document.DocumentNode }));
            Assert.That(children.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, false, false }));
            Assert.That(attributePath, Is.EqualTo("/Q{urn:root}root[1]/Q{urn:item}item[1]/@id"));
            Assert.That(textPath, Is.EqualTo("/Q{urn:root}root[1]/Q{urn:item}item[1]/text()[2]"));
            Assert.That(documentPath, Is.EqualTo("/"));
        });
    }

    [Test]
    public void SelectsInnermostAndOutermostNodesAndGeneratesStableIdentifiers()
    {
        XmlDocument document = Parse("<root><branch><leaf/></branch><other/></root>", null);
        XPathDynamicContext context = Context(document.DocumentNode!);

        XdmSequence outermost = Evaluate("outermost((/*/*/*, /*/*, /*))", context);
        XdmSequence innermost = Evaluate("innermost((/*, /*/*, /*/*/*))", context);
        XdmAtomicValue[] identifiers = Evaluate("(generate-id(/*), generate-id(/*), generate-id(/*/*[1]), generate-id(()))", context).AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(outermost.Nodes, Is.EqualTo(new[] { document.DocumentElement }));
            Assert.That(innermost.Nodes, Is.EqualTo(new[] { document.DocumentElement!.Children![0].Children![0], document.DocumentElement.Children[1] }));
            Assert.That(identifiers[0].StringValue, Is.EqualTo(identifiers[1].StringValue));
            Assert.That(identifiers[0].StringValue, Is.Not.EqualTo(identifiers[2].StringValue));
            Assert.That(identifiers[0].StringValue, Does.Match("^N[0-9]+$"));
            Assert.That(identifiers[3].StringValue, Is.Empty);
        });
    }

    [Test]
    public void PreservesStableDocumentOrderAcrossDifferentTrees()
    {
        XmlDocument first = Parse("<first><child/></first>", null);
        XmlDocument second = Parse("<second><child/></second>", null);
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmNode(first.DocumentNode!);
        builder.AddXdmNode(first.DocumentElement!);
        builder.AddXdmNode(second.DocumentNode!);
        builder.AddXdmNode(second.DocumentElement!);
        XPathNodeFunctionImplementation implementation = new XPathNodeFunctionImplementation("outermost", new SourceRange(), new XPathStaticContext());

        XdmSequence result = implementation.EvaluateXPathExtremalNodes(builder.BuildImmutableXdmSequence(), false);

        Assert.That(result.Nodes, Is.EqualTo(new[] { first.DocumentNode, second.DocumentNode }));
    }

    [Test]
    public void MatchesInheritedLanguagesIdsElementIdsAndIdReferences()
    {
        XmlDocument document = Parse("<root xml:lang='en-US'><item xml:id='a'/><item xml:id='b'><code>E1</code><ref>a E1</ref></item><empty xml:lang=''/></root>", null);
        XmlNode second = document.DocumentElement!.Children![1];
        XmlNode code = second.Children![0];
        XmlNode reference = second.Children[1];
        code.TypeInfo = new XmlSchemaTypeInfo { IsId = true, NormalizedValue = "E1", AtomicType = XdmAtomicType.Id };
        reference.TypeInfo = new XmlSchemaTypeInfo { IsIdReference = true, NormalizedValue = "a E1", AtomicType = XdmAtomicType.Idref };
        XPathDynamicContext context = Context(document.DocumentNode!);

        XdmAtomicValue[] languages = Evaluate("(lang('en', /*/*[1]), lang('EN-us', /*/*[2]/code), lang('fr', /*), lang('', /*/empty))", context).AtomicValues!;
        XdmSequence attributeIds = Evaluate("id('b a invalid:name')", context);
        XdmSequence elementId = Evaluate("id('E1')", context);
        XdmSequence elementWithId = Evaluate("element-with-id('E1')", context);
        XdmSequence references = Evaluate("idref(('a', 'E1', 'invalid:name'))", context);

        Assert.Multiple(() =>
        {
            Assert.That(languages.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, false, true }));
            Assert.That(attributeIds.Nodes, Is.EqualTo(new[] { document.DocumentElement.Children![0], second }));
            Assert.That(elementId.Nodes, Is.EqualTo(new[] { code }));
            Assert.That(elementWithId.Nodes, Is.EqualTo(new[] { second }));
            Assert.That(references.Nodes, Is.EqualTo(new[] { reference }));
        });
    }

    [Test]
    public void UsesContextFormsAndReportsStandardNodeFunctionErrors()
    {
        XmlDocument document = Parse("<root xml:lang='de'><child/></root>", null);
        XmlNode child = document.DocumentElement!.Children![0];
        XPathDynamicContext childContext = Context(child);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate("name()", childContext).AtomicValues![0].StringValue, Is.EqualTo("child"));
            Assert.That(Evaluate("root()", childContext).Nodes, Is.EqualTo(new[] { document.DocumentNode }));
            Assert.That(Evaluate("has-children()", childContext).AtomicValues![0].BooleanValue, Is.False);
            Assert.That(Evaluate("lang('de')", childContext).AtomicValues![0].BooleanValue, Is.True);
            Assert.That(Evaluate("string()", childContext).AtomicValues![0].StringValue, Is.Empty);
            Assert.That(Evaluate("data()", childContext).AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
        });

        XPathDynamicException missing = Assert.Throws<XPathDynamicException>(() => Evaluate("name()", new XPathDynamicContext()))!;
        XPathDynamicException wrongNode = Assert.Throws<XPathDynamicException>(() => Evaluate("root(1)", new XPathDynamicContext()))!;
        XPathDynamicException stringFunction = Assert.Throws<XPathDynamicException>(() => Evaluate("string(true#0)", new XPathDynamicContext(), true))!;
        XPathDynamicException dataFunction = Assert.Throws<XPathDynamicException>(() => Evaluate("data(true#0)", new XPathDynamicContext(), true))!;
        XmlNode detached = new XmlNode { Kind = XmlNodeKind.Element, Name = new XmlName { LocalName = "detached", QualifiedName = "detached" }, Children = [], Attributes = [] };
        XPathDynamicException detachedId = Assert.Throws<XPathDynamicException>(() => Evaluate("id('x')", Context(detached)))!;
        XPathStaticException arity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("innermost()", new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(wrongNode.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(stringFunction.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0014));
            Assert.That(dataFunction.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0013));
            Assert.That(detachedId.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0001));
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
        });
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext dynamicContext)
    {
        return Evaluate(source, dynamicContext, false);
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext dynamicContext, bool higherOrder)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = higherOrder };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathDynamicContext Context(XmlNode node)
    {
        return new XPathDynamicContext { ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(node) };
    }

    private static XmlDocument Parse(string source, string? baseUri)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreservePrefixes = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = baseUri }, options, null, null, null);
    }
}
