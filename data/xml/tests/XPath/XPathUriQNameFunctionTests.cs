using xml;

namespace xml.Tests;

[TestFixture]
public class XPathUriQNameFunctionTests
{
    [Test]
    public void ResolvesRelativeUrisAgainstSuppliedAndStaticBases()
    {
        XdmAtomicValue supplied = Evaluate("resolve-uri('../images/pic.png', 'https://example.test/a/b/doc.xml')", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue implicitBase = Evaluate("resolve-uri('?view=full')", new XPathStaticContext { BaseUri = "https://example.test/a/doc.xml" }, new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue iri = Evaluate("resolve-uri('bébé', 'https://example.test/a/')", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue absolute = Evaluate("resolve-uri('urn:example:item')", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmSequence empty = Evaluate("resolve-uri(())", new XPathStaticContext(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(supplied.Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(supplied.StringValue, Is.EqualTo("https://example.test/a/images/pic.png"));
            Assert.That(implicitBase.StringValue, Is.EqualTo("https://example.test/a/doc.xml?view=full"));
            Assert.That(iri.StringValue, Is.EqualTo("https://example.test/a/bébé"));
            Assert.That(absolute.StringValue, Is.EqualTo("urn:example:item"));
            Assert.That(empty.Count, Is.Zero);
        });
    }

    [Test]
    public void EncodesUriIriAndHtmlUriCharactersUsingUppercaseUtf8Escapes()
    {
        XdmAtomicValue[] values = Evaluate("(encode-for-uri('~bébé /%😀'), iri-to-uri('http://example.test/~bébé My|x%20'), escape-html-uri('http://example.test/~bébé My|x%20'), encode-for-uri(()), iri-to-uri(()), escape-html-uri(()))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues!;

        Assert.That(values.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[]
        {
            "~b%C3%A9b%C3%A9%20%2F%25%F0%9F%98%80",
            "http://example.test/~b%C3%A9b%C3%A9%20My%7Cx%20",
            "http://example.test/~b%C3%A9b%C3%A9 My|x%20",
            "",
            "",
            ""
        }));
    }

    [Test]
    public void ConstructsQNamesAndReturnsTheirComponents()
    {
        XdmAtomicValue qname = Evaluate("QName('urn:example', 'p:item')", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue[] components = Evaluate("(prefix-from-QName(QName('urn:example', 'p:item')), local-name-from-QName(QName('urn:example', 'p:item')), namespace-uri-from-QName(QName('urn:example', 'p:item')))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues!;
        XdmSequence absentPrefix = Evaluate("prefix-from-QName(QName('', 'item'))", new XPathStaticContext(), new XPathDynamicContext());
        XdmAtomicValue absentNamespace = Evaluate("namespace-uri-from-QName(QName((), 'item'))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(qname.Type, Is.EqualTo(XdmAtomicType.QName));
            Assert.That(qname.NameValue!.Prefix, Is.EqualTo("p"));
            Assert.That(qname.NameValue.LocalName, Is.EqualTo("item"));
            Assert.That(qname.NameValue.NamespaceUri, Is.EqualTo("urn:example"));
            Assert.That(components.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "p", "item", "urn:example" }));
            Assert.That(components.Select(value => value.Type).ToArray(), Is.EqualTo(new[] { XdmAtomicType.NcName, XdmAtomicType.NcName, XdmAtomicType.AnyUri }));
            Assert.That(absentPrefix.Count, Is.Zero);
            Assert.That(absentNamespace.Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(absentNamespace.StringValue, Is.Empty);
        });
    }

    [Test]
    public void ResolvesQNamesAndQueriesInScopeNamespacesFromElements()
    {
        XmlDocument document = Parse("<root xmlns='urn:default' xmlns:p='urn:p'><child xmlns:q='urn:q'/></root>");
        XPathDynamicContext dynamicContext = new XPathDynamicContext { ContextItem = NodeSequence(document.DocumentNode!) };
        XdmAtomicValue inherited = Evaluate("resolve-QName('p:item', /*/*)", new XPathStaticContext(), dynamicContext).AtomicValues![0];
        XdmAtomicValue defaulted = Evaluate("resolve-QName('item', /*/*)", new XPathStaticContext(), dynamicContext).AtomicValues![0];
        XdmAtomicValue defaultNamespace = Evaluate("namespace-uri-for-prefix((), /*/*)", new XPathStaticContext(), dynamicContext).AtomicValues![0];
        XdmAtomicValue xmlNamespace = Evaluate("namespace-uri-for-prefix('xml', /*/*)", new XPathStaticContext(), dynamicContext).AtomicValues![0];
        XdmSequence missing = Evaluate("namespace-uri-for-prefix('missing', /*/*)", new XPathStaticContext(), dynamicContext);
        XdmSequence prefixes = Evaluate("in-scope-prefixes(/*/*)", new XPathStaticContext(), dynamicContext);

        Assert.Multiple(() =>
        {
            Assert.That(inherited.NameValue!.Prefix, Is.EqualTo("p"));
            Assert.That(inherited.NameValue.NamespaceUri, Is.EqualTo("urn:p"));
            Assert.That(defaulted.NameValue!.Prefix, Is.Null);
            Assert.That(defaulted.NameValue.NamespaceUri, Is.EqualTo("urn:default"));
            Assert.That(defaultNamespace.StringValue, Is.EqualTo("urn:default"));
            Assert.That(xmlNamespace.StringValue, Is.EqualTo(XmlNamespaces.Xml));
            Assert.That(missing.Count, Is.Zero);
            Assert.That(prefixes.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EquivalentTo(new[] { "q", "p", "", "xml" }));
        });
    }

    [Test]
    public void ResolveQNameValidatesTheElementEvenForAnEmptyQName()
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-QName((), /)", new XPathStaticContext(), Context(Parse("<root/>"))))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void ReportsUriAndNamespaceErrorsWithStandardCodes()
    {
        XPathDynamicException missingBase = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-uri('child')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException invalidRelative = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-uri('bad uri', 'https://example.test/base')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException fragmentedBase = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-uri('child', 'https://example.test/base#part')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException opaqueBase = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-uri('child', 'urn:example:base')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException invalidQName = Assert.Throws<XPathDynamicException>(() => Evaluate("QName('urn:test', 'a:b:c')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException missingQNameNamespace = Assert.Throws<XPathDynamicException>(() => Evaluate("QName('', 'p:item')", new XPathStaticContext(), new XPathDynamicContext()))!;

        XmlDocument document = Parse("<root/>");
        XPathDynamicContext context = Context(document);
        XPathDynamicException invalidResolvedQName = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-QName('a:b:c', /*)", new XPathStaticContext(), context))!;
        XPathDynamicException unboundPrefix = Assert.Throws<XPathDynamicException>(() => Evaluate("resolve-QName('p:item', /*)", new XPathStaticContext(), context))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingBase.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FONS0005));
            Assert.That(invalidRelative.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0002));
            Assert.That(fragmentedBase.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0002));
            Assert.That(opaqueBase.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0002));
            Assert.That(invalidQName.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0002));
            Assert.That(missingQNameNamespace.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0002));
            Assert.That(invalidResolvedQName.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0002));
            Assert.That(unboundPrefix.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FONS0004));
        });
    }

    [Test]
    public void RejectsWrongUriQNameAritiesAndArgumentTypes()
    {
        XPathStaticException arity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("QName('urn:test', 'p:item', 'extra')", new XPathStaticContext()))!;
        XPathStaticException uriArity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("resolve-uri('a', 'b', 'c')", new XPathStaticContext()))!;
        XPathDynamicException qnameType = Assert.Throws<XPathDynamicException>(() => Evaluate("prefix-from-QName('p:item')", new XPathStaticContext(), new XPathDynamicContext()))!;
        XPathDynamicException stringType = Assert.Throws<XPathDynamicException>(() => Evaluate("encode-for-uri(1)", new XPathStaticContext(), new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
            Assert.That(uriArity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
            Assert.That(qnameType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(stringType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathDynamicContext Context(XmlDocument document)
    {
        return new XPathDynamicContext { ContextItem = NodeSequence(document.DocumentNode!) };
    }

    private static XdmSequence NodeSequence(XmlNode node)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(node);
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreservePrefixes = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }
}
