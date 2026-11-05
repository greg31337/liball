using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathJsonFunctionTests
{
    [Test]
    public void ParsesRfc8259ValuesIntoMapsArraysAndAtomicValues()
    {
        XdmSequence result = Evaluate("parse-json('\uFEFF{\"name\":\"Ada\",\"values\":[1,true,null],\"unicode\":\"\\uD834\\uDD1E\"}')");
        XdmMapValue map = result.Maps![0];
        XdmSequence values = MapValue(map, "values");
        XdmArrayValue array = values.Arrays![0];
        XdmSequence[] members = array.Members!;

        Assert.Multiple(() =>
        {
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Map }));
            Assert.That(StringValue(MapValue(map, "name")), Is.EqualTo("Ada"));
            Assert.That(StringValue(MapValue(map, "unicode")), Is.EqualTo("𝄞"));
            Assert.That(array.Count, Is.EqualTo(3));
            Assert.That(members[0].AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(members[0].AtomicValues![0].FloatingValue, Is.EqualTo(1d));
            Assert.That(members[1].AtomicValues![0].BooleanValue, Is.True);
            Assert.That(members[2].Count, Is.Zero);
            Assert.That(Evaluate("count(parse-json(())), count(parse-json('null'))").AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "0", "0" }));
        });
    }

    [TestCase("[1,]")]
    [TestCase("{unquoted:1}")]
    [TestCase("01")]
    [TestCase("\"\\x20\"")]
    [TestCase("true false")]
    [TestCase("\"line\nbreak\"")]
    public void RejectsNonRfc8259SyntaxByDefault(string json)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => EvaluateParseJson(json, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0001));
    }

    [Test]
    public void AppliesEveryDuplicatePolicyAfterEscapeExpansion()
    {
        string json = "{\"a\":1,\"\\u0061\":2}";
        XdmSequence first = EvaluateParseJson(json, "map{'duplicates':'use-first'}");
        XdmSequence last = EvaluateParseJson(json, "map{'duplicates':'use-last'}");
        XPathDynamicException rejected = Assert.Throws<XPathDynamicException>(() => EvaluateParseJson(json, "map{'duplicates':'reject'}"))!;

        Assert.Multiple(() =>
        {
            Assert.That(MapValue(first.Maps![0], "a").AtomicValues![0].FloatingValue, Is.EqualTo(1d));
            Assert.That(MapValue(last.Maps![0], "a").AtomicValues![0].FloatingValue, Is.EqualTo(2d));
            Assert.That(rejected.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0003));
        });
    }

    [Test]
    public void HandlesEscapeFallbackAndNumberParserOptions()
    {
        string escaped = "{\"slash\":\"\\\\\",\"bad\":\"\\u0000\"}";
        XdmSequence retained = EvaluateParseJson(escaped, "map{'escape':true()}");
        XdmSequence replaced = EvaluateParseJson(escaped, "map{'fallback':function($s){concat('[',$s,']')}}");
        XdmSequence decimalNumber = EvaluateParseJson("12.50", "map{'number-parser':function($n){xs:decimal($n)}}");

        Assert.Multiple(() =>
        {
            Assert.That(StringValue(MapValue(retained.Maps![0], "slash")), Is.EqualTo("\\\\"));
            Assert.That(StringValue(MapValue(retained.Maps[0], "bad")), Is.EqualTo("\\u0000"));
            Assert.That(StringValue(MapValue(replaced.Maps![0], "slash")), Is.EqualTo("\\"));
            Assert.That(StringValue(MapValue(replaced.Maps[0], "bad")), Is.EqualTo("[\\u0000]"));
            Assert.That(decimalNumber.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(decimalNumber.AtomicValues[0].DecimalValue, Is.EqualTo(12.50m));
        });
    }

    [Test]
    public void LiberalModeAcceptsDocumentedCommonExtensions()
    {
        string json = "{/*comment*/answer:01, list:['x',],}";
        XdmSequence result = EvaluateParseJson(json, "map{'liberal':true()}");
        XdmMapValue map = result.Maps![0];

        Assert.Multiple(() =>
        {
            Assert.That(MapValue(map, "answer").AtomicValues![0].FloatingValue, Is.EqualTo(1d));
            Assert.That(StringValue(MapValue(map, "list").Arrays![0].Members![0]), Is.EqualTo("x"));
        });
    }

    [Test]
    public void ReportsInvalidRecognizedOptionsWithFojs0005()
    {
        XPathDynamicException duplicates = Assert.Throws<XPathDynamicException>(() => EvaluateParseJson("{}", "map{'duplicates':'other'}"))!;
        XPathDynamicException conflict = Assert.Throws<XPathDynamicException>(() => EvaluateParseJson("{}", "map{'escape':true(),'fallback':function($s){$s}}"))!;
        XPathTypeException parserResult = Assert.Throws<XPathTypeException>(() => EvaluateParseJson("1", "map{'number-parser':function($n){'not numeric'}}"))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicates.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0005));
            Assert.That(conflict.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0005));
            Assert.That(parserResult.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void JsonDocResolvesAndCachesExternalJsonText()
    {
        JsonTextResolver resolver = new JsonTextResolver();
        XPathDynamicContext context = new XPathDynamicContext
        {
            ResourceResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed, AllowedSchemes = ["https"] }
        };

        XdmSequence result = Evaluate("(map:get(json-doc('data.json'),'value'), map:get(json-doc('data.json'),'value'))", context, "https://example.test/base/query.xq");

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.FloatingValue), Is.EqualTo(new[] { 7d, 7d }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(resolver.LastUri, Is.EqualTo("https://example.test/base/data.json"));
        });
    }

    private static XdmSequence EvaluateParseJson(string json, string? options)
    {
        string escaped = json.Replace("'", "''");
        string expression = options == null ? "parse-json('" + escaped + "')" : "parse-json('" + escaped + "'," + options + ")";
        return Evaluate(expression, new XPathDynamicContext(), null);
    }

    private static XdmSequence Evaluate(string source)
    {
        return Evaluate(source, new XPathDynamicContext(), null);
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context, string? baseUri)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true, BaseUri = baseUri };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, context).Value!;
    }

    private static XdmSequence MapValue(XdmMapValue map, string key)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(key, XdmAtomicType.String);
        return new XdmMapOperations().GetXdmMapValue(map, atomic);
    }

    private static string StringValue(XdmSequence sequence)
    {
        return sequence.AtomicValues![0].StringValue!;
    }

    private sealed class JsonTextResolver : IXmlResourceResolver
    {
        internal int Calls;
        internal string? LastUri;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            LastUri = systemIdentifier;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Encoding.UTF8.GetBytes("{\"value\":7}") },
                RequestedUri = systemIdentifier,
                AbsoluteUri = systemIdentifier
            };
        }
    }
}
