using xml;

namespace xml.Tests;

[TestFixture]
public class XPathTransformFunctionTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void StylesheetTextSupportsRawDocumentAndSerializedDelivery()
    {
        string stylesheet = InitialTemplate("<out>value</out>");

        XdmSequence raw = Evaluate("transform(map{'stylesheet-text':$sheet,'delivery-format':'raw'})?output", [Variable("sheet", StringSequence(stylesheet))], null);
        XdmSequence document = Evaluate("transform(map{'stylesheet-text':$sheet})?output", [Variable("sheet", StringSequence(stylesheet))], null);
        XdmSequence serialized = Evaluate("transform(map{'stylesheet-text':$sheet,'delivery-format':'serialized'})?output", [Variable("sheet", StringSequence(stylesheet))], null);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Nodes!.Single().Name!.LocalName, Is.EqualTo("out"));
            Assert.That(document.Nodes!.Single().Kind, Is.EqualTo(XmlNodeKind.Document));
            Assert.That(document.Nodes![0].Children!.Single().Name!.LocalName, Is.EqualTo("out"));
            Assert.That(serialized.AtomicValues!.Single().StringValue, Does.Contain("<out>value</out>"));
        });
    }

    [Test]
    public void StylesheetNodeAndLocationInputsAreAccepted()
    {
        string stylesheet = InitialTemplate("<node-result/>");
        XmlDocument sheetDocument = Parse(stylesheet, "https://example.test/node.xsl");
        TransformResolver resolver = new TransformResolver();
        resolver.Stylesheet = InitialTemplate("<location-result/>");
        XdmSequence node = Evaluate("transform(map{'stylesheet-node':$sheet,'delivery-format':'raw'})?output", [Variable("sheet", NodeSequence(sheetDocument.DocumentNode!))], null);
        XdmSequence location = Evaluate("transform(map{'stylesheet-location':'styles/main.xsl','delivery-format':'raw'})?output", [], resolver);
        XdmSequence executable = Evaluate("transform(map{'stylesheet-executable':$sheet,'delivery-format':'raw'})?output", [Variable("sheet", StringSequence(InitialTemplate("<executable-result/>")))], null);

        Assert.Multiple(() =>
        {
            Assert.That(node.Nodes!.Single().Name!.LocalName, Is.EqualTo("node-result"));
            Assert.That(location.Nodes!.Single().Name!.LocalName, Is.EqualTo("location-result"));
            Assert.That(executable.Nodes!.Single().Name!.LocalName, Is.EqualTo("executable-result"));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(resolver.LastUri, Is.EqualTo("https://example.test/base/styles/main.xsl"));
        });
    }

    [Test]
    public void SourceSelectionInitialComponentsAndParametersAreApplied()
    {
        string templateSheet = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:param name='value' required='yes'/>" +
            "<xsl:variable name='global-copy' select='$value'/>" +
            "<xsl:template match='item'><xsl:param name='value'/><xsl:sequence select='$global-copy, string(@id), $value'/></xsl:template>" +
            "</xsl:stylesheet>";
        XmlDocument source = Parse("<root><item id='selected'/></root>", "https://example.test/source.xml");
        string expression = "transform(map{" +
            "'stylesheet-text':$sheet," +
            "'initial-match-selection':$item," +
            "'stylesheet-params':map{QName('', 'value'):'G'}," +
            "'template-params':map{QName('', 'value'):'L'}," +
            "'delivery-format':'raw'})?output";

        XdmSequence result = Evaluate(expression, [Variable("sheet", StringSequence(templateSheet)), Variable("item", NodeSequence(source.DocumentElement!.Children![0]))], null);

        Assert.That(AtomicStrings(result), Is.EqualTo(new[] { "G", "selected", "L" }));
    }

    [Test]
    public void InitialFunctionArgumentsAndSecondaryResultsAreReturned()
    {
        string functionSheet = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema' version='3.0'>" +
            "<xsl:function name='f:next' visibility='public' as='xs:integer'><xsl:param name='value' as='xs:integer'/><xsl:sequence select='$value + 1'/></xsl:function>" +
            "</xsl:stylesheet>";
        string resultSheet = InitialTemplate("<xsl:result-document href='secondary.xml'><secondary/></xsl:result-document><principal/>");

        XdmSequence function = Evaluate("transform(map{'stylesheet-text':$sheet,'initial-function':QName('urn:functions','f:next'),'function-params':[41],'delivery-format':'raw'})?output", [Variable("sheet", StringSequence(functionSheet))], null);
        XdmSequence resultMap = Evaluate("transform(map{'stylesheet-text':$sheet,'base-output-uri':'https://example.test/results/main.xml','delivery-format':'raw'})", [Variable("sheet", StringSequence(resultSheet))], null);
        XdmMapValue map = resultMap.Maps!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(function.AtomicValues!.Single().IntegerValue, Is.EqualTo(42));
            Assert.That(MapKeys(map), Is.EquivalentTo(new[] { "https://example.test/results/main.xml", "https://example.test/results/secondary.xml" }));
        });
    }

    [Test]
    public void InvalidOptionsAndStylesheetErrorsUseRequiredCodes()
    {
        XPathDynamicException missing = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{})", [], null))!;
        XPathDynamicException conflicting = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{'stylesheet-text':'x','stylesheet-location':'x'})", [], null))!;
        XPathDynamicException invocation = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{'stylesheet-text':$sheet,'initial-function':QName('urn:test','f:start'),'initial-template':QName('', 'start')})", [Variable("sheet", StringSequence(InitialTemplate("<out/>")))], null))!;
        XPathDynamicException delivery = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{'stylesheet-text':$sheet,'delivery-format':'saved'})", [Variable("sheet", StringSequence(InitialTemplate("<out/>")))], null))!;
        XPathDynamicException stylesheet = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{'stylesheet-text':'<not-xsl/>'})", [], null))!;
        XPathDynamicException version = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{'stylesheet-text':'not XML','xslt-version':4})", [], null))!;

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0002));
            Assert.That(conflicting.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0002));
            Assert.That(invocation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0002));
            Assert.That(delivery.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0002));
            Assert.That(stylesheet.Diagnostic!.Code, Does.StartWith("XT"));
            Assert.That(version.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0001));
        });
    }

    private static XdmSequence Evaluate(string source, XPathVariableBinding[] variables, IXmlResourceResolver? resolver)
    {
        XPathStaticContext staticContext = new XPathStaticContext
        {
            BaseUri = "https://example.test/base/query.xq",
            Variables = variables,
            AllowHigherOrderFunctions = true
        };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        XPathDynamicContext context = new XPathDynamicContext
        {
            Variables = variables,
            ResourceResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed }
        };
        return new XPathEvaluator().EvaluateXPath(expression, context).Value!;
    }

    private static XPathVariableBinding Variable(string name, XdmSequence value)
    {
        return new XPathVariableBinding { Name = new XmlName { LocalName = name }, Value = value, DeclaredType = XdmAtomicType.AnyAtomicType, DeclaredOccurrence = XdmOccurrence.ZeroOrMore };
    }

    private static XdmSequence StringSequence(string value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    private static XdmSequence NodeSequence(XmlNode value)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(value);
    }

    private static string[] AtomicStrings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static string[] MapKeys(XdmMapValue map)
    {
        return map.Entries!.Select(entry => entry.Key!.StringValue!).ToArray();
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = baseUri }, new XmlParserOptions { NamespaceHandling = XmlNamespaceHandling.Namespaces10 }, new XmlDefaultResolver(), null, null);
    }

    private static string InitialTemplate(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='xsl:initial-template'>" + content + "</xsl:template></xsl:stylesheet>";
    }

    private sealed class TransformResolver : IXmlResourceResolver
    {
        internal int Calls;
        internal string? LastUri;
        internal string Stylesheet = "";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            LastUri = systemIdentifier;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = systemIdentifier,
                Input = new XmlInput { Text = Stylesheet, BaseUri = systemIdentifier }
            };
        }
    }
}
