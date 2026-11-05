using System.Collections;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathFunctionSignatureTests
{
    [TestCaseSource(nameof(StandardFunctionSignatures))]
    public void ExposesExactlyTheStandardFunctionArities(string namespaceUri, string localName, int[] arities, bool unbounded)
    {
        int maximum = unbounded ? 8 : arities.Max() + 1;
        int arity = 0;
        while (arity <= maximum)
        {
            bool expected = arities.Contains(arity) || unbounded && arity >= arities.Min();
            XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
            bool actual = EvaluateBoolean("exists(function-lookup(QName('" + namespaceUri + "','" + localName + "')," + arity + "))", context, new XPathDynamicContext());
            Assert.That(actual, Is.EqualTo(expected), namespaceUri + "#" + localName + "#" + arity);
            string reference = "Q{" + namespaceUri + "}" + localName + "#" + arity;
            if (expected)
            {
                Assert.That(EvaluateBoolean("exists(" + reference + ")", context, new XPathDynamicContext()), Is.True, reference);
            }
            else
            {
                XPathStaticException error = Assert.Throws<XPathStaticException>(() => Evaluate(reference, context, new XPathDynamicContext()))!;
                Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017), reference);
            }

            arity = arity + 1;
        }
    }

    [TestCaseSource(nameof(StandardConstructorSignatures))]
    public void ExposesEachConcreteAtomicConstructorAtArityOne(string localName)
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string name = "QName('" + XPathNamespaces.XmlSchema + "','" + localName + "')";

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateBoolean("empty(function-lookup(" + name + ",0))", context, new XPathDynamicContext()), Is.True);
            Assert.That(EvaluateBoolean("exists(function-lookup(" + name + ",1))", context, new XPathDynamicContext()), Is.True);
            Assert.That(EvaluateBoolean("empty(function-lookup(" + name + ",2))", context, new XPathDynamicContext()), Is.True);
            Assert.That(EvaluateBoolean("exists(Q{" + XPathNamespaces.XmlSchema + "}" + localName + "#1)", context, new XPathDynamicContext()), Is.True);
        });
    }

    [Test]
    public void ConstructsBuiltInListTypesAndConvertsTheirUntypedItems()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence tokens = Evaluate("xs:NMTOKENS('first second')", context, new XPathDynamicContext());
        XdmSequence converted = Evaluate("function($values as xs:decimal*) as xs:decimal {sum($values, 0.0)}(xs:NMTOKENS('1 1.5')!xs:untypedAtomic(.))", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(tokens.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(converted.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(converted.AtomicValues[0].DecimalValue, Is.EqualTo(2.5m));
        });
    }

    [TestCase(XPathNamespaces.Functions, "true", 0, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "node-name", 1, XdmAtomicType.QName, XdmOccurrence.ZeroOrOne)]
    [TestCase(XPathNamespaces.Functions, "data", 1, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore)]
    [TestCase(XPathNamespaces.Functions, "position", 0, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "current-dateTime", 0, XdmAtomicType.DateTimeStamp, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "default-language", 0, XdmAtomicType.Language, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "static-base-uri", 0, XdmAtomicType.AnyUri, XdmOccurrence.ZeroOrOne)]
    [TestCase(XPathNamespaces.Functions, "serialize", 2, XdmAtomicType.String, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "parse-ietf-date", 1, XdmAtomicType.DateTime, XdmOccurrence.ZeroOrOne)]
    [TestCase(XPathNamespaces.Functions, "collation-key", 2, XdmAtomicType.Base64Binary, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Functions, "unparsed-text-lines", 2, XdmAtomicType.String, XdmOccurrence.ZeroOrMore)]
    [TestCase(XPathNamespaces.Math, "pi", 0, XdmAtomicType.Double, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Math, "sqrt", 1, XdmAtomicType.Double, XdmOccurrence.ZeroOrOne)]
    [TestCase(XPathNamespaces.Map, "contains", 2, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Map, "keys", 1, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore)]
    [TestCase(XPathNamespaces.Array, "size", 1, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne)]
    [TestCase(XPathNamespaces.Array, "get", 2, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore)]
    [TestCase(XPathNamespaces.XmlSchema, "int", 1, XdmAtomicType.Int, XdmOccurrence.ZeroOrOne)]
    public void PublishesStandardReturnTypeAndCardinality(string namespaceUri, string localName, int arity, XdmAtomicType resultType, XdmOccurrence occurrence)
    {
        XdmFunctionItem function = LookupFunction(namespaceUri, localName, arity);

        Assert.Multiple(() =>
        {
            Assert.That(function.ResultType, Is.EqualTo(resultType));
            Assert.That(function.ResultOccurrence, Is.EqualTo(occurrence));
        });
    }

    [TestCase("number() eq number(.)")]
    [TestCase("string() eq string(.)")]
    [TestCase("deep-equal(data(), data(.))")]
    [TestCase("deep-equal(node-name(), node-name(.))")]
    [TestCase("deep-equal(nilled(), nilled(.))")]
    [TestCase("deep-equal(base-uri(), base-uri(.))")]
    [TestCase("deep-equal(document-uri(), document-uri(.))")]
    [TestCase("root() is root(.)")]
    [TestCase("path() eq path(.)")]
    [TestCase("has-children() eq has-children(.)")]
    [TestCase("name() eq name(.)")]
    [TestCase("local-name() eq local-name(.)")]
    [TestCase("namespace-uri() eq namespace-uri(.)")]
    [TestCase("generate-id() eq generate-id(.)")]
    [TestCase("lang('en') eq lang('en', .)")]
    [TestCase("deep-equal(id('missing'), id('missing', .))")]
    [TestCase("deep-equal(element-with-id('missing'), element-with-id('missing', .))")]
    [TestCase("deep-equal(idref('missing'), idref('missing', .))")]
    [TestCase("string-length() eq string-length(.)")]
    [TestCase("normalize-space() eq normalize-space(.)")]
    [TestCase("random-number-generator()?number eq random-number-generator(())?number")]
    [TestCase("serialize(/*) eq serialize(/*, ())")]
    public void OptionalArgumentFormsMatchTheirExplicitEquivalent(string source)
    {
        XmlDocument document = Parse("<root xml:lang='en'>12</root>", "urn:context");
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!)
        };

        Assert.That(EvaluateBoolean(source, new XPathStaticContext { AllowHigherOrderFunctions = true }, dynamicContext), Is.True);
    }

    [Test]
    public void ContextDependentFunctionsUseTheCapturedExecutionContext()
    {
        XPathStaticContext staticContext = new XPathStaticContext
        {
            AllowHigherOrderFunctions = true,
            BaseUri = "urn:static-base",
            DefaultCollation = XPathNamespaces.CodepointCollation
        };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue("focus", XdmAtomicType.String)),
            Position = 2,
            Size = 3,
            DefaultLanguage = "fr-CA",
            ImplicitTimezoneMinutes = 330,
            CurrentDateTime = new DateTimeOffset(2026, 4, 5, 6, 7, 8, TimeSpan.Zero)
        };
        XdmSequence result = Evaluate("(position(), last(), default-language(), (function(){default-language()})(), format-date(xs:date('2026-04-05'), '[MNn]'), implicit-timezone(), current-dateTime(), default-collation(), static-base-uri())", staticContext, dynamicContext);
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(value => value.Type).ToArray(), Is.EqualTo(new[]
            {
                XdmAtomicType.Integer,
                XdmAtomicType.Integer,
                XdmAtomicType.Language,
                XdmAtomicType.Language,
                XdmAtomicType.String,
                XdmAtomicType.DayTimeDuration,
                XdmAtomicType.DateTimeStamp,
                XdmAtomicType.String,
                XdmAtomicType.AnyUri
            }));
            Assert.That(values.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[]
            {
                "2",
                "3",
                "fr-CA",
                "fr-CA",
                "Avril",
                "PT5H30M",
                "2026-04-05T11:37:08+05:30",
                XPathNamespaces.CodepointCollation,
                "urn:static-base"
            }));
            Assert.That(StringValue(Evaluate("default-language()", new XPathStaticContext(), new XPathDynamicContext())), Is.EqualTo("en"));
        });
    }

    [TestCase("substring('12345', 0, 3)", "12")]
    [TestCase("round-half-to-even(2.5)", "2")]
    [TestCase("string-join(subsequence((1,2,3), 0, 2), ',')", "1")]
    [TestCase("string(array:get([10], 1))", "10")]
    [TestCase("string(map:get(map{'x':20}, 'x'))", "20")]
    [TestCase("parse-ietf-date('1 Jan 70 0:00 +0')", "1970-01-01T00:00:00Z")]
    public void StandardFunctionsHonorBoundaryValues(string source, string expected)
    {
        Assert.That(StringValue(Evaluate(source, new XPathStaticContext { AllowHigherOrderFunctions = true }, new XPathDynamicContext())), Is.EqualTo(expected));
    }

    [Test]
    public void CollationAndTimezoneArgumentsControlTheResult()
    {
        const string collationUri = "urn:ascii-case-insensitive";
        XPathStaticContext context = new XPathStaticContext
        {
            CollationBindings =
            [
                new XPathCollationBinding
                {
                    Uri = collationUri,
                    Implementation = new XPathAsciiCaseInsensitiveCollation()
                }
            ]
        };
        XdmSequence result = Evaluate("(compare('A','a','" + collationUri + "'), adjust-dateTime-to-timezone(xs:dateTime('2026-01-01T00:00:00Z'), xs:dayTimeDuration('PT5H30M')))", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].IntegerValue, Is.Zero);
            Assert.That(result.AtomicValues[1].StringValue, Is.EqualTo("2026-01-01T05:30:00+05:30"));
        });
    }

    [TestCase("zero-or-one((1,2))", "FORG0003")]
    [TestCase("one-or-more(())", "FORG0004")]
    [TestCase("exactly-one(())", "FORG0005")]
    [TestCase("codepoints-to-string(0)", "FOCH0001")]
    [TestCase("compare('a','b','urn:missing')", "FOCH0002")]
    [TestCase("normalize-unicode('x','INVALID')", "FOCH0003")]
    [TestCase("matches('x','x','z')", "FORX0001")]
    [TestCase("matches('x','[')", "FORX0002")]
    [TestCase("replace('x','x*','y')", "FORX0003")]
    [TestCase("replace('x','x','$x')", "FORX0004")]
    [TestCase("dateTime(xs:date('2020-01-01Z'), xs:time('00:00:00+01:00'))", "FORG0008")]
    [TestCase("adjust-date-to-timezone(xs:date('2020-01-01'), xs:dayTimeDuration('PT1S'))", "FODT0003")]
    [TestCase("array:get([],1)", "FOAY0001")]
    [TestCase("array:subarray([1],1,-1)", "FOAY0002")]
    [TestCase("parse-json('{')", "FOJS0001")]
    [TestCase("parse-xml('<')", "FODC0006")]
    [TestCase("resolve-uri('relative')", "FONS0005")]
    [TestCase("QName('', 'p:name')", "FOCA0002")]
    [TestCase("error()", "FOER0000")]
    [TestCase("load-xquery-module('urn:module')", "FOQM0006")]
    [TestCase("transform(map{})", "FOXT0002")]
    public void StandardFunctionsReportTheirMandatedErrors(string source, string expectedCode)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate(source, new XPathStaticContext { AllowHigherOrderFunctions = true }, new XPathDynamicContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    private static IEnumerable StandardFunctionSignatures()
    {
        string fn = XPathNamespaces.Functions;
        yield return Signature(fn, "node-name", false, 0, 1);
        yield return Signature(fn, "nilled", false, 0, 1);
        yield return Signature(fn, "string", false, 0, 1);
        yield return Signature(fn, "data", false, 0, 1);
        yield return Signature(fn, "base-uri", false, 0, 1);
        yield return Signature(fn, "document-uri", false, 0, 1);
        yield return Signature(fn, "error", false, 0, 1, 2, 3);
        yield return Signature(fn, "trace", false, 1, 2);
        yield return Signature(fn, "abs", false, 1);
        yield return Signature(fn, "ceiling", false, 1);
        yield return Signature(fn, "floor", false, 1);
        yield return Signature(fn, "round", false, 1, 2);
        yield return Signature(fn, "round-half-to-even", false, 1, 2);
        yield return Signature(fn, "number", false, 0, 1);
        yield return Signature(fn, "codepoints-to-string", false, 1);
        yield return Signature(fn, "string-to-codepoints", false, 1);
        yield return Signature(fn, "compare", false, 2, 3);
        yield return Signature(fn, "codepoint-equal", false, 2);
        yield return Signature(fn, "concat", true, 2);
        yield return Signature(fn, "string-join", false, 1, 2);
        yield return Signature(fn, "substring", false, 2, 3);
        yield return Signature(fn, "string-length", false, 0, 1);
        yield return Signature(fn, "normalize-space", false, 0, 1);
        yield return Signature(fn, "normalize-unicode", false, 1, 2);
        yield return Signature(fn, "upper-case", false, 1);
        yield return Signature(fn, "lower-case", false, 1);
        yield return Signature(fn, "translate", false, 3);
        yield return Signature(fn, "contains", false, 2, 3);
        yield return Signature(fn, "starts-with", false, 2, 3);
        yield return Signature(fn, "ends-with", false, 2, 3);
        yield return Signature(fn, "substring-before", false, 2, 3);
        yield return Signature(fn, "substring-after", false, 2, 3);
        yield return Signature(fn, "contains-token", false, 2, 3);
        yield return Signature(fn, "matches", false, 2, 3);
        yield return Signature(fn, "replace", false, 3, 4);
        yield return Signature(fn, "tokenize", false, 1, 2, 3);
        yield return Signature(fn, "analyze-string", false, 2, 3);
        yield return Signature(fn, "resolve-uri", false, 1, 2);
        yield return Signature(fn, "encode-for-uri", false, 1);
        yield return Signature(fn, "iri-to-uri", false, 1);
        yield return Signature(fn, "escape-html-uri", false, 1);
        yield return Signature(fn, "true", false, 0);
        yield return Signature(fn, "false", false, 0);
        yield return Signature(fn, "boolean", false, 1);
        yield return Signature(fn, "not", false, 1);
        yield return Signature(fn, "years-from-duration", false, 1);
        yield return Signature(fn, "months-from-duration", false, 1);
        yield return Signature(fn, "days-from-duration", false, 1);
        yield return Signature(fn, "hours-from-duration", false, 1);
        yield return Signature(fn, "minutes-from-duration", false, 1);
        yield return Signature(fn, "seconds-from-duration", false, 1);
        yield return Signature(fn, "year-from-dateTime", false, 1);
        yield return Signature(fn, "month-from-dateTime", false, 1);
        yield return Signature(fn, "day-from-dateTime", false, 1);
        yield return Signature(fn, "hours-from-dateTime", false, 1);
        yield return Signature(fn, "minutes-from-dateTime", false, 1);
        yield return Signature(fn, "seconds-from-dateTime", false, 1);
        yield return Signature(fn, "timezone-from-dateTime", false, 1);
        yield return Signature(fn, "year-from-date", false, 1);
        yield return Signature(fn, "month-from-date", false, 1);
        yield return Signature(fn, "day-from-date", false, 1);
        yield return Signature(fn, "timezone-from-date", false, 1);
        yield return Signature(fn, "hours-from-time", false, 1);
        yield return Signature(fn, "minutes-from-time", false, 1);
        yield return Signature(fn, "seconds-from-time", false, 1);
        yield return Signature(fn, "timezone-from-time", false, 1);
        yield return Signature(fn, "dateTime", false, 2);
        yield return Signature(fn, "adjust-dateTime-to-timezone", false, 1, 2);
        yield return Signature(fn, "adjust-date-to-timezone", false, 1, 2);
        yield return Signature(fn, "adjust-time-to-timezone", false, 1, 2);
        yield return Signature(fn, "format-dateTime", false, 2, 5);
        yield return Signature(fn, "format-date", false, 2, 5);
        yield return Signature(fn, "format-time", false, 2, 5);
        yield return Signature(fn, "parse-ietf-date", false, 1);
        yield return Signature(fn, "QName", false, 2);
        yield return Signature(fn, "resolve-QName", false, 2);
        yield return Signature(fn, "prefix-from-QName", false, 1);
        yield return Signature(fn, "local-name-from-QName", false, 1);
        yield return Signature(fn, "namespace-uri-from-QName", false, 1);
        yield return Signature(fn, "namespace-uri-for-prefix", false, 2);
        yield return Signature(fn, "in-scope-prefixes", false, 1);
        yield return Signature(fn, "name", false, 0, 1);
        yield return Signature(fn, "local-name", false, 0, 1);
        yield return Signature(fn, "namespace-uri", false, 0, 1);
        yield return Signature(fn, "lang", false, 1, 2);
        yield return Signature(fn, "root", false, 0, 1);
        yield return Signature(fn, "path", false, 0, 1);
        yield return Signature(fn, "has-children", false, 0, 1);
        yield return Signature(fn, "innermost", false, 1);
        yield return Signature(fn, "outermost", false, 1);
        yield return Signature(fn, "empty", false, 1);
        yield return Signature(fn, "exists", false, 1);
        yield return Signature(fn, "head", false, 1);
        yield return Signature(fn, "tail", false, 1);
        yield return Signature(fn, "insert-before", false, 3);
        yield return Signature(fn, "remove", false, 2);
        yield return Signature(fn, "reverse", false, 1);
        yield return Signature(fn, "subsequence", false, 2, 3);
        yield return Signature(fn, "unordered", false, 1);
        yield return Signature(fn, "distinct-values", false, 1, 2);
        yield return Signature(fn, "index-of", false, 2, 3);
        yield return Signature(fn, "deep-equal", false, 2, 3);
        yield return Signature(fn, "zero-or-one", false, 1);
        yield return Signature(fn, "one-or-more", false, 1);
        yield return Signature(fn, "exactly-one", false, 1);
        yield return Signature(fn, "count", false, 1);
        yield return Signature(fn, "avg", false, 1);
        yield return Signature(fn, "max", false, 1, 2);
        yield return Signature(fn, "min", false, 1, 2);
        yield return Signature(fn, "sum", false, 1, 2);
        yield return Signature(fn, "id", false, 1, 2);
        yield return Signature(fn, "element-with-id", false, 1, 2);
        yield return Signature(fn, "idref", false, 1, 2);
        yield return Signature(fn, "generate-id", false, 0, 1);
        yield return Signature(fn, "doc", false, 1);
        yield return Signature(fn, "doc-available", false, 1);
        yield return Signature(fn, "collection", false, 0, 1);
        yield return Signature(fn, "uri-collection", false, 0, 1);
        yield return Signature(fn, "unparsed-text", false, 1, 2);
        yield return Signature(fn, "unparsed-text-lines", false, 1, 2);
        yield return Signature(fn, "unparsed-text-available", false, 1, 2);
        yield return Signature(fn, "environment-variable", false, 1);
        yield return Signature(fn, "available-environment-variables", false, 0);
        yield return Signature(fn, "parse-xml", false, 1);
        yield return Signature(fn, "parse-xml-fragment", false, 1);
        yield return Signature(fn, "serialize", false, 1, 2);
        yield return Signature(fn, "position", false, 0);
        yield return Signature(fn, "last", false, 0);
        yield return Signature(fn, "current-dateTime", false, 0);
        yield return Signature(fn, "current-date", false, 0);
        yield return Signature(fn, "current-time", false, 0);
        yield return Signature(fn, "implicit-timezone", false, 0);
        yield return Signature(fn, "default-collation", false, 0);
        yield return Signature(fn, "default-language", false, 0);
        yield return Signature(fn, "static-base-uri", false, 0);
        yield return Signature(fn, "function-lookup", false, 2);
        yield return Signature(fn, "function-name", false, 1);
        yield return Signature(fn, "function-arity", false, 1);
        yield return Signature(fn, "for-each", false, 2);
        yield return Signature(fn, "filter", false, 2);
        yield return Signature(fn, "fold-left", false, 3);
        yield return Signature(fn, "fold-right", false, 3);
        yield return Signature(fn, "for-each-pair", false, 3);
        yield return Signature(fn, "sort", false, 1, 2, 3);
        yield return Signature(fn, "apply", false, 2);
        yield return Signature(fn, "load-xquery-module", false, 1, 2);
        yield return Signature(fn, "transform", false, 1);
        yield return Signature(fn, "parse-json", false, 1, 2);
        yield return Signature(fn, "json-doc", false, 1, 2);
        yield return Signature(fn, "json-to-xml", false, 1, 2);
        yield return Signature(fn, "xml-to-json", false, 1, 2);
        yield return Signature(fn, "random-number-generator", false, 0, 1);
        yield return Signature(fn, "format-integer", false, 2, 3);
        yield return Signature(fn, "format-number", false, 2, 3);
        yield return Signature(fn, "collation-key", false, 1, 2);

        string map = XPathNamespaces.Map;
        yield return Signature(map, "merge", false, 1, 2);
        yield return Signature(map, "size", false, 1);
        yield return Signature(map, "keys", false, 1);
        yield return Signature(map, "contains", false, 2);
        yield return Signature(map, "get", false, 2);
        yield return Signature(map, "find", false, 2);
        yield return Signature(map, "put", false, 3);
        yield return Signature(map, "entry", false, 2);
        yield return Signature(map, "remove", false, 2);
        yield return Signature(map, "for-each", false, 2);

        string array = XPathNamespaces.Array;
        yield return Signature(array, "size", false, 1);
        yield return Signature(array, "get", false, 2);
        yield return Signature(array, "put", false, 3);
        yield return Signature(array, "append", false, 2);
        yield return Signature(array, "subarray", false, 2, 3);
        yield return Signature(array, "remove", false, 2);
        yield return Signature(array, "insert-before", false, 3);
        yield return Signature(array, "head", false, 1);
        yield return Signature(array, "tail", false, 1);
        yield return Signature(array, "reverse", false, 1);
        yield return Signature(array, "join", false, 1);
        yield return Signature(array, "flatten", false, 1);
        yield return Signature(array, "for-each", false, 2);
        yield return Signature(array, "filter", false, 2);
        yield return Signature(array, "fold-left", false, 3);
        yield return Signature(array, "fold-right", false, 3);
        yield return Signature(array, "for-each-pair", false, 3);
        yield return Signature(array, "sort", false, 1, 2, 3);

        string math = XPathNamespaces.Math;
        yield return Signature(math, "pi", false, 0);
        yield return Signature(math, "exp", false, 1);
        yield return Signature(math, "exp10", false, 1);
        yield return Signature(math, "log", false, 1);
        yield return Signature(math, "log10", false, 1);
        yield return Signature(math, "pow", false, 2);
        yield return Signature(math, "sqrt", false, 1);
        yield return Signature(math, "sin", false, 1);
        yield return Signature(math, "cos", false, 1);
        yield return Signature(math, "tan", false, 1);
        yield return Signature(math, "asin", false, 1);
        yield return Signature(math, "acos", false, 1);
        yield return Signature(math, "atan", false, 1);
        yield return Signature(math, "atan2", false, 2);
    }

    private static IEnumerable StandardConstructorSignatures()
    {
        string[] names =
        [
            "untypedAtomic", "string", "normalizedString", "token", "language", "Name", "NCName", "NMTOKEN", "ID", "IDREF", "ENTITY",
            "boolean", "decimal", "integer", "nonPositiveInteger", "negativeInteger", "long", "int", "short", "byte", "nonNegativeInteger",
            "unsignedLong", "unsignedInt", "unsignedShort", "unsignedByte", "positiveInteger", "float", "double", "duration",
            "yearMonthDuration", "dayTimeDuration", "dateTime", "dateTimeStamp", "date", "time", "gYearMonth", "gYear", "gMonthDay",
            "gMonth", "gDay", "hexBinary", "base64Binary", "anyURI", "QName"
        ];
        int index = 0;
        while (index < names.Length)
        {
            yield return new TestCaseData(names[index]).SetName("constructor_" + names[index]);
            index = index + 1;
        }
    }

    private static TestCaseData Signature(string namespaceUri, string localName, bool unbounded, params int[] arities)
    {
        return new TestCaseData(namespaceUri, localName, arities, unbounded).SetName("signature_" + Prefix(namespaceUri) + "_" + localName);
    }

    private static string Prefix(string namespaceUri)
    {
        if (namespaceUri == XPathNamespaces.Map)
        {
            return "map";
        }

        if (namespaceUri == XPathNamespaces.Array)
        {
            return "array";
        }

        return namespaceUri == XPathNamespaces.Math ? "math" : "fn";
    }

    private static XdmFunctionItem LookupFunction(string namespaceUri, string localName, int arity)
    {
        string source = "function-lookup(QName('" + namespaceUri + "','" + localName + "')," + arity + ")";
        return Evaluate(source, new XPathStaticContext { AllowHigherOrderFunctions = true }, new XPathDynamicContext()).Functions![0];
    }

    private static bool EvaluateBoolean(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        return Evaluate(source, staticContext, dynamicContext).AtomicValues![0].BooleanValue;
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static string StringValue(XdmSequence sequence)
    {
        return sequence.AtomicValues![0].StringValue!;
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlInput input = new XmlInput { Text = source, BaseUri = baseUri };
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
