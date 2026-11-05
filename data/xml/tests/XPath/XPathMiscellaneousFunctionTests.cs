using xml;

namespace xml.Tests;

[TestFixture]
public class XPathMiscellaneousFunctionTests
{
    [Test]
    public void ParseXmlCreatesANewDocumentWithTheStaticBaseUri()
    {
        XdmSequence result = Evaluate("parse-xml('<root xmlns=\"urn:test\"><child>value</child></root>')", new XPathDynamicContext(), "https://example.test/base.xq");
        XmlNode document = result.Nodes![0];
        XmlNode root = document.Children![0];

        Assert.Multiple(() =>
        {
            Assert.That(new XdmNodeAccessor().GetXdmNodeKind(new XdmNodeAccessor().WrapXmlNode(document)), Is.EqualTo(XdmNodeKind.Document));
            Assert.That(document.OwnerDocument!.BaseUri, Is.EqualTo("https://example.test/base.xq"));
            Assert.That(document.OwnerDocument.DocumentUri, Is.Null);
            Assert.That(root.Name!.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(root.Children![0].Children![0].Value, Is.EqualTo("value"));
            Assert.That(Evaluate("count(parse-xml(()))", new XPathDynamicContext(), null).AtomicValues![0].IntegerValue, Is.Zero);
        });
    }

    [Test]
    public void ParseXmlAllowsAnInternalDocumentType()
    {
        XdmSequence result = Evaluate("parse-xml(\"<!DOCTYPE a [<!ELEMENT a (#PCDATA)>]><a>text</a>\")", new XPathDynamicContext(), null);

        Assert.That(result.Nodes![0].Children!.Any(node => node.Name?.LocalName == "a"), Is.True);
    }

    [Test]
    public void ParseXmlFragmentPreservesTopLevelTextAndElements()
    {
        XdmSequence result = Evaluate("parse-xml-fragment('before<a/>after<b/>')", new XPathDynamicContext(), null);
        XmlNode[] children = result.Nodes![0].Children!;

        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(4));
            Assert.That(children[0].Value, Is.EqualTo("before"));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("a"));
            Assert.That(children[2].Value, Is.EqualTo("after"));
            Assert.That(children[3].Name!.LocalName, Is.EqualTo("b"));
        });
    }

    [Test]
    public void NamedFunctionReferencesRetainTheirCreationFocus()
    {
        XdmSequence result = Evaluate("let $f := parse-xml('<a xml:lang=\"de\"><b/></a>')/*/b ! lang#1 return $f('de')", new XPathDynamicContext(), null);

        Assert.That(result.AtomicValues![0].BooleanValue, Is.True);
    }

    [TestCase("parse-xml('<a>')")]
    [TestCase("parse-xml('<a/><b/>')")]
    [TestCase("parse-xml-fragment('&external;')")]
    public void ParseXmlFunctionsReportMalformedInputAsFodc0006(string source)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate(source, new XPathDynamicContext(), null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0006));
    }

    [Test]
    public void SerializeSupportsXmlTextJsonAndMapParameters()
    {
        string xml = StringValue(Evaluate("serialize(parse-xml('<root><child>value</child></root>')/*, map{'omit-xml-declaration':true()})", new XPathDynamicContext(), null));
        string text = StringValue(Evaluate("serialize((1,2,'x'), map{'method':'text','item-separator':'|'})", new XPathDynamicContext(), null));
        string json = StringValue(Evaluate("serialize(map{'a':1}, map{'method':'json'})", new XPathDynamicContext(), null));
        string characterMap = StringValue(Evaluate("serialize('a-b', map{'method':'text','use-character-maps':map{'-':' plus '}})", new XPathDynamicContext(), null));
        string declaration = StringValue(Evaluate("serialize(parse-xml('<root/>')/*, map{'encoding':'us-ascii','omit-xml-declaration':false()})", new XPathDynamicContext(), null));

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo("<root><child>value</child></root>"));
            Assert.That(text, Is.EqualTo("1|2|x"));
            Assert.That(json, Is.EqualTo("{\"a\":1}"));
            Assert.That(characterMap, Is.EqualTo("a plus b"));
            Assert.That(declaration, Does.StartWith("<?xml version=\"1.0\" encoding=\"us-ascii\"?>"));
        });
    }

    [Test]
    public void SerializeAcceptsTheStandardParameterElement()
    {
        XmlDocument parameters = Parse("<output:serialization-parameters xmlns:output='http://www.w3.org/2010/xslt-xquery-serialization'><output:method value='text'/><output:item-separator value='/'/></output:serialization-parameters>");
        XPathDynamicContext context = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parameters.DocumentNode!)
        };

        XdmSequence result = Evaluate("serialize((1,2,3), /*)", context, null);

        Assert.That(StringValue(result), Is.EqualTo("1/2/3"));
    }

    [Test]
    public void SerializeAppliesStructuredCharacterMapsAndValidatesParameterElements()
    {
        XmlDocument parameters = Parse("<output:serialization-parameters xmlns:output='http://www.w3.org/2010/xslt-xquery-serialization'><output:use-character-maps><output:character-map character='$' map-string='£'/></output:use-character-maps></output:serialization-parameters>");
        XPathDynamicContext context = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parameters.DocumentNode!)
        };
        string mapped = StringValue(Evaluate("serialize('$', /*)", context, null));
        XmlDocument invalidParameters = Parse("<output:serialization-parameters xmlns:output='http://www.w3.org/2010/xslt-xquery-serialization' invalid='yes'><output:indent value='yes'/></output:serialization-parameters>");
        XPathDynamicContext invalidContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(invalidParameters.DocumentNode!)
        };
        XPathDynamicException invalid = Assert.Throws<XPathDynamicException>(() => Evaluate("serialize('x', /*)", invalidContext, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(mapped, Is.EqualTo("£"));
            Assert.That(invalid.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0017));
        });
    }

    [Test]
    public void SerializeHandlesJsonNumbersEncodingsAndEmptyHtmlHeads()
    {
        string json = StringValue(Evaluate("serialize(parse-json('[1,2]'), map{'method':'json'})", new XPathDynamicContext(), null));
        string escaped = StringValue(Evaluate("serialize(codepoints-to-string(119070), map{'method':'json','encoding':'ISO-8859-1'})", new XPathDynamicContext(), null));
        string html = StringValue(Evaluate("serialize(parse-xml('<html><head/><body/></html>'), map{'method':'html'})", new XPathDynamicContext(), null));

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("[1,2]"));
            Assert.That(escaped, Is.EqualTo("\"\\uD834\\uDD1E\""));
            Assert.That(html, Does.Contain("<meta http-equiv"));
        });
    }

    [TestCase("Wed, 06 Jun 1994 07:29:35 GMT", "1994-06-06T07:29:35Z")]
    [TestCase("Sunday, 06-Nov-94 08:49:37 GMT", "1994-11-06T08:49:37Z")]
    [TestCase("Wed Jun 06 11:54:45 EST 2013", "2013-06-06T11:54:45-05:00")]
    [TestCase("Wed, 6 Jun 94 07:29:35 +0500", "1994-06-06T07:29:35+05:00")]
    [TestCase("1 jan 0070 2:03", "0070-01-01T02:03:00Z")]
    public void ParseIetfDateAcceptsEveryStandardShape(string source, string expected)
    {
        string expression = "parse-ietf-date('" + source + "')";
        XdmAtomicValue value = Evaluate(expression, new XPathDynamicContext(), null).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.DateTime));
            Assert.That(value.StringValue, Is.EqualTo(expected));
        });
    }

    [TestCase("31 Feb 2020 10:00 GMT")]
    [TestCase("6 June 1994 07:29 GMT")]
    [TestCase("6 Jun 1994 07:29 +1500")]
    [TestCase("6 Jun 1994 07:29 +5:3")]
    [TestCase("Aug 20 19:36:01 -05:60 2014")]
    public void ParseIetfDateRejectsInvalidValues(string source)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("parse-ietf-date('" + source + "')", new XPathDynamicContext(), null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0010));
    }

    [Test]
    public void RandomNumberGeneratorIsDeterministicAndProvidesNextAndPermute()
    {
        XdmSequence numbers = Evaluate("let $a := random-number-generator('seed'), $b := random-number-generator('seed') return ($a?number, $b?number, $a?next()?number)", new XPathDynamicContext(), null);
        double[] values = numbers.AtomicValues!.Select(value => value.FloatingValue).ToArray();
        XdmSequence defaultNumbers = Evaluate("(random-number-generator()?number, random-number-generator(())?number)", new XPathDynamicContext(), null);
        double[] defaults = defaultNumbers.AtomicValues!.Select(value => value.FloatingValue).ToArray();
        XdmSequence firstPermutation = Evaluate("random-number-generator('seed')?permute((1,2,3,4,5))", new XPathDynamicContext(), null);
        XdmSequence secondPermutation = Evaluate("random-number-generator('seed')?permute((1,2,3,4,5))", new XPathDynamicContext(), null);
        long[] first = firstPermutation.AtomicValues!.Select(value => value.IntegerValue).ToArray();
        long[] second = secondPermutation.AtomicValues!.Select(value => value.IntegerValue).ToArray();
        XdmSequence functionTypes = Evaluate("(random-number-generator('seed')?next instance of function() as map(xs:string, item()), random-number-generator('seed')?permute instance of function(item()*) as item()*)", new XPathDynamicContext(), null);

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.GreaterThanOrEqualTo(0d).And.LessThan(1d));
            Assert.That(values[1], Is.EqualTo(values[0]));
            Assert.That(values[2], Is.Not.EqualTo(values[0]));
            Assert.That(defaults[1], Is.EqualTo(defaults[0]));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.OrderBy(value => value), Is.EqualTo(new long[] { 1, 2, 3, 4, 5 }));
            Assert.That(functionTypes.AtomicValues!.Select(value => value.BooleanValue), Is.EqualTo(new[] { true, true }));
        });
    }

    [Test]
    public void ErrorUsesTheDefaultOrSuppliedCodeAndDescription()
    {
        XPathDynamicException defaultError = Assert.Throws<XPathDynamicException>(() => Evaluate("error()", new XPathDynamicContext(), null))!;
        XPathDynamicException customError = Assert.Throws<XPathDynamicException>(() => Evaluate("error(QName('urn:test','CUSTOM'), 'boom', (1,2))", new XPathDynamicContext(), null))!;

        Assert.Multiple(() =>
        {
            Assert.That(defaultError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOER0000));
            Assert.That(customError.Diagnostic!.Code, Is.EqualTo("CUSTOM"));
            Assert.That(customError.Message, Is.EqualTo("boom"));
        });
    }

    [Test]
    public void DynamicLoadingFunctionsUseTheirStandardUnavailableProcessorErrors()
    {
        XPathDynamicException query = Assert.Throws<XPathDynamicException>(() => Evaluate("load-xquery-module('urn:module')", new XPathDynamicContext(), null))!;
        XPathDynamicException transform = Assert.Throws<XPathDynamicException>(() => Evaluate("transform(map{})", new XPathDynamicContext(), null))!;
        XPathDynamicException emptyModule = Assert.Throws<XPathDynamicException>(() => Evaluate("load-xquery-module('')", new XPathDynamicContext(), null))!;

        Assert.Multiple(() =>
        {
            Assert.That(query.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOQM0006));
            Assert.That(transform.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0002));
            Assert.That(emptyModule.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOQM0001));
        });
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context, string? baseUri)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true, BaseUri = baseUri };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, context).Value!;
    }

    private static string StringValue(XdmSequence sequence)
    {
        return sequence.AtomicValues![0].StringValue!;
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
}
