using xml;

namespace xml.Tests;

[TestFixture]
public class XPathJsonXmlFunctionTests
{
    private const string FunctionsNamespace = "http://www.w3.org/2005/xpath-functions";

    [Test]
    public void JsonToXmlCreatesTheStandardVocabularyAndPreservesNumberLexicalForms()
    {
        XdmSequence result = Evaluate("json-to-xml('{\"name\":\"Ada\",\"values\":[1.2300,true,null]}')", new XPathDynamicContext(), "https://example.test/query.xq");
        XmlNode document = result.Nodes![0];
        XmlNode map = document.Children![0];
        XmlNode name = map.Children![0];
        XmlNode array = map.Children[1];
        XmlNode[] members = array.Children!;

        Assert.Multiple(() =>
        {
            Assert.That(document.Kind, Is.EqualTo(XmlNodeKind.Document));
            Assert.That(document.OwnerDocument!.BaseUri, Is.EqualTo("https://example.test/query.xq"));
            Assert.That(map.Name!.LocalName, Is.EqualTo("map"));
            Assert.That(map.Name.NamespaceUri, Is.EqualTo(FunctionsNamespace));
            Assert.That(map.NamespaceBindings, Has.Length.EqualTo(1));
            Assert.That(map.NamespaceBindings![0].NamespaceUri, Is.EqualTo(FunctionsNamespace));
            Assert.That(name.Name!.LocalName, Is.EqualTo("string"));
            Assert.That(AttributeValue(name, "key"), Is.EqualTo("name"));
            Assert.That(NodeStringValue(name), Is.EqualTo("Ada"));
            Assert.That(array.Name!.LocalName, Is.EqualTo("array"));
            Assert.That(AttributeValue(array, "key"), Is.EqualTo("values"));
            Assert.That(members.Select(child => child.Name!.LocalName), Is.EqualTo(new[] { "number", "boolean", "null" }));
            Assert.That(NodeStringValue(members[0]), Is.EqualTo("1.2300"));
            Assert.That(NodeStringValue(members[1]), Is.EqualTo("true"));
            Assert.That(members[2].Children, Is.Empty);
        });
    }

    [Test]
    public void JsonToXmlResultsCanBeNavigatedWithTheFunctionsNamespace()
    {
        XPathStaticContext staticContext = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "j", NamespaceUri = FunctionsNamespace }]
        };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("let $result := json-to-xml('{\"glossary\":{\"title\":\"example glossary\"}}') return $result/j:map/j:map[@key='glossary']/j:string[@key='title']", staticContext);
        XdmSequence result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;

        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public void JsonToXmlAppliesEscapingAndFallbackRules()
    {
        string input = "{\"\\\\\":\"\\u0000\"}";
        XmlNode escaped = Root(EvaluateJsonToXml(input, "map{'escape':true()}"));
        XmlNode escapedValue = escaped.Children![0];
        XmlNode replaced = Root(EvaluateJsonToXml(input, "map{'fallback':function($s){concat('[',$s,']')}}"));
        XmlNode replacedValue = replaced.Children![0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(escapedValue, "key"), Is.EqualTo("\\\\"));
            Assert.That(AttributeValue(escapedValue, "escaped-key"), Is.EqualTo("true"));
            Assert.That(AttributeValue(escapedValue, "escaped"), Is.EqualTo("true"));
            Assert.That(NodeStringValue(escapedValue), Is.EqualTo("\\u0000"));
            Assert.That(AttributeValue(replacedValue, "key"), Is.EqualTo("\\"));
            Assert.That(NodeStringValue(replacedValue), Is.EqualTo("[\\u0000]"));
        });
    }

    [Test]
    public void JsonToXmlSupportsItsDuplicatePoliciesAndValidationConstraint()
    {
        string input = "{\"a\":1,\"\\u0061\":2}";
        XmlNode retained = Root(EvaluateJsonToXml(input, null));
        XmlNode first = Root(EvaluateJsonToXml(input, "map{'duplicates':'use-first'}"));
        XPathDynamicException rejected = Assert.Throws<XPathDynamicException>(() => EvaluateJsonToXml(input, "map{'duplicates':'reject'}"))!;
        XPathDynamicException invalidValidation = Assert.Throws<XPathDynamicException>(() => EvaluateJsonToXml(input, "map{'validate':true(),'duplicates':'retain'}"))!;

        Assert.Multiple(() =>
        {
            Assert.That(retained.Children, Has.Length.EqualTo(2));
            Assert.That(NodeStringValue(retained.Children![0]), Is.EqualTo("1"));
            Assert.That(NodeStringValue(retained.Children[1]), Is.EqualTo("2"));
            Assert.That(first.Children, Has.Length.EqualTo(1));
            Assert.That(NodeStringValue(first.Children![0]), Is.EqualTo("1"));
            Assert.That(rejected.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0003));
            Assert.That(invalidValidation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0005));
        });
    }

    [Test]
    public void JsonToXmlValidationAddsVocabularyTypeAnnotations()
    {
        XmlNode map = Root(EvaluateJsonToXml("{\"enabled\":true}", "map{'validate':true()}"));
        XmlNode member = map.Children![0];
        XmlNode key = member.Attributes!.Single(attribute => attribute.Name!.LocalName == "key");

        Assert.Multiple(() =>
        {
            Assert.That(map.TypeInfo!.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(map.TypeInfo.TypeName!.NamespaceUri, Is.EqualTo(FunctionsNamespace));
            Assert.That(map.TypeInfo.TypeName.LocalName, Is.EqualTo("mapType"));
            Assert.That(member.TypeInfo!.TypeName!.LocalName, Is.EqualTo("booleanWithinMapType"));
            Assert.That(member.TypeInfo.AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(member.TypeInfo.ActualValue!.BooleanValue, Is.True);
            Assert.That(key.TypeInfo!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(key.TypeInfo.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });
    }

    [Test]
    public void XmlToJsonMapsVocabularyTypesAndSupportsIndentation()
    {
        string xml = "<map xmlns='" + FunctionsNamespace + "'><number key='n'>1</number><array key='a'><boolean>1</boolean><null/><string>x\\y</string></array></map>";
        string compact = EvaluateXmlToJson(xml, null).AtomicValues![0].StringValue!;
        string indented = EvaluateXmlToJson(xml, "map{'indent':true()}").AtomicValues![0].StringValue!;

        Assert.Multiple(() =>
        {
            Assert.That(compact, Is.EqualTo("{\"n\":1,\"a\":[true,null,\"x\\\\y\"]}"));
            Assert.That(indented, Does.Contain("\n  \"n\": 1"));
            Assert.That(indented, Does.Contain("\n    true"));
            Assert.That(indented, Does.EndWith("\n}"));
        });
    }

    [Test]
    public void JsonXmlConversionsRoundTripNestedValues()
    {
        XdmSequence result = Evaluate("xml-to-json(json-to-xml('{\"a\":[1,true,null,\"x\"]}'))", new XPathDynamicContext(), null);

        Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("{\"a\":[1,true,null,\"x\"]}"));
    }

    [Test]
    public void XmlToJsonEscapesSolidusCharactersAfterDecodedBackslashes()
    {
        XdmSequence result = Evaluate("xml-to-json(json-to-xml('\"\\\\\\/\"'))", new XPathDynamicContext(), null);

        Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("\"\\\\\\/\""));
    }

    [TestCase("<map xmlns='http://www.w3.org/2005/xpath-functions'><string/></map>")]
    [TestCase("<array xmlns='http://www.w3.org/2005/xpath-functions'><string key='a'/></array>")]
    [TestCase("<number xmlns='http://www.w3.org/2005/xpath-functions'>INF</number>")]
    [TestCase("<boolean xmlns='http://www.w3.org/2005/xpath-functions'>yes</boolean>")]
    [TestCase("<null xmlns='http://www.w3.org/2005/xpath-functions'>x</null>")]
    [TestCase("<string xmlns='urn:wrong'>x</string>")]
    [TestCase("<string xmlns='http://www.w3.org/2005/xpath-functions' unsupported='x'/>")]
    public void XmlToJsonRejectsInvalidVocabularyTrees(string source)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => EvaluateXmlToJson(source, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0006));
    }

    [Test]
    public void XmlToJsonDetectsNormalizedDuplicateKeysAndInvalidEscapes()
    {
        string duplicates = "<map xmlns='" + FunctionsNamespace + "'><string key='a'/><string key='\\u0061' escaped-key='true'/></map>";
        string invalidEscape = "<string xmlns='" + FunctionsNamespace + "' escaped='true'>\\x</string>";
        XPathDynamicException duplicateError = Assert.Throws<XPathDynamicException>(() => EvaluateXmlToJson(duplicates, null))!;
        XPathDynamicException escapeError = Assert.Throws<XPathDynamicException>(() => EvaluateXmlToJson(invalidEscape, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0006));
            Assert.That(escapeError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0007));
        });
    }

    [Test]
    public void BothConversionsReturnEmptyForAnEmptyInputSequence()
    {
        XdmSequence result = Evaluate("(json-to-xml(()), xml-to-json(()))", new XPathDynamicContext(), null);

        Assert.That(result.Count, Is.Zero);
    }

    private static XdmSequence EvaluateJsonToXml(string json, string? options)
    {
        string input = json.Replace("'", "''");
        string expression = options == null ? "json-to-xml('" + input + "')" : "json-to-xml('" + input + "'," + options + ")";
        return Evaluate(expression, new XPathDynamicContext(), null);
    }

    private static XdmSequence EvaluateXmlToJson(string source, string? options)
    {
        XmlDocument document = Parse(source);
        XPathDynamicContext context = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!)
        };
        string expression = options == null ? "xml-to-json(.)" : "xml-to-json(.," + options + ")";
        return Evaluate(expression, context, null);
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context, string? baseUri)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true, BaseUri = baseUri };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, context).Value!;
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

    private static XmlNode Root(XdmSequence sequence)
    {
        return sequence.Nodes![0].Children![0];
    }

    private static string? AttributeValue(XmlNode node, string localName)
    {
        return node.Attributes!.SingleOrDefault(attribute => attribute.Name!.LocalName == localName)?.Value;
    }

    private static string NodeStringValue(XmlNode node)
    {
        return new TreeNavigation().GetXmlNodeStringValueOperation(node);
    }
}
