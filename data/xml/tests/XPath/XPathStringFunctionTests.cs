using xml;

namespace xml.Tests;

[TestFixture]
public class XPathStringFunctionTests
{
    private const string HtmlAsciiCaseInsensitiveCollation = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    [Test]
    public void ConvertsBetweenUnicodeCodepointsAndStrings()
    {
        XdmSequence text = Evaluate("codepoints-to-string((65, 128512, 233))", new XPathStaticContext(), new XPathDynamicContext());
        XdmSequence codepoints = Evaluate("string-to-codepoints('A😀é')", new XPathStaticContext(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(text.AtomicValues![0].StringValue, Is.EqualTo("A😀é"));
            Assert.That(codepoints.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 65, 128512, 233 }));
            Assert.That(Evaluate("string-to-codepoints(())", new XPathStaticContext(), new XPathDynamicContext()).Count, Is.Zero);
            Assert.That(Evaluate("codepoints-to-string(())", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0].StringValue, Is.Empty);
        });
    }

    [Test]
    public void ComparesStringsAndCreatesCollationKeys()
    {
        XPathStaticContext context = new XPathStaticContext { DefaultCollation = HtmlAsciiCaseInsensitiveCollation };
        XdmSequence result = Evaluate("(compare('alpha', 'BETA'), compare('A', 'a'), codepoint-equal('A', 'a'))", context, new XPathDynamicContext());
        XdmAtomicValue firstKey = Evaluate("collation-key('Hello')", context, new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue secondKey = Evaluate("collation-key('hELLo')", context, new XPathDynamicContext()).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].IntegerValue, Is.EqualTo(-1));
            Assert.That(result.AtomicValues[1].IntegerValue, Is.Zero);
            Assert.That(result.AtomicValues[2].BooleanValue, Is.False);
            Assert.That(firstKey.Type, Is.EqualTo(XdmAtomicType.Base64Binary));
            Assert.That(firstKey.BinaryValue, Is.EqualTo(secondKey.BinaryValue));
            Assert.That(Evaluate("codepoint-equal((), 'x')", context, new XPathDynamicContext()).Count, Is.Zero);
        });
    }

    [Test]
    public void RegisteredCollationsAndRelativeCollationUrisControlStringFunctions()
    {
        const string uri = "https://example.test/collations/fold";
        FoldingCollation collation = new FoldingCollation();
        XPathStaticContext context = new XPathStaticContext
        {
            BaseUri = "https://example.test/main.xpath",
            CollationBindings = [new XPathCollationBinding { Uri = uri, Implementation = collation }]
        };

        XdmSequence result = Evaluate("(compare('A', 'a', 'collations/fold'), contains('Alpha', 'LPH', 'collations/fold'), collation-key('A', 'collations/fold') eq collation-key('a', 'collations/fold'))", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].IntegerValue, Is.Zero);
            Assert.That(result.AtomicValues[1].BooleanValue, Is.True);
            Assert.That(result.AtomicValues[2].BooleanValue, Is.True);
            Assert.That(collation.CompareCalls, Is.GreaterThanOrEqualTo(2));
            Assert.That(collation.KeyCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void ConcatenatesAndJoinsAtomizedAtomicValues()
    {
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = NodeSequence(Parse("<root><v>one</v><v>two</v></root>").DocumentNode!)
        };

        XdmSequence result = Evaluate("(concat(1, (), true(), 'x'), string-join((1, 2, 3), '-'), string-join(('a', 'b')), string-join(/root/v, ','))", new XPathStaticContext(), dynamicContext);

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "1truex", "1-2-3", "ab", "one,two" }));
    }

    [Test]
    public void SubstringUsesXPathRoundingAndUnicodeCharacterPositions()
    {
        XdmSequence result = Evaluate("(substring('A😀BC', 2, 2), substring('12345', 1.5, 2.6), substring('12345', 0, 3), substring('12345', -3, 5), substring('12345', xs:double('NaN'), 3), substring('12345', -42, xs:double('INF')), substring('12345', -3.1e0, 5.2e0))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "😀B", "234", "12", "1", "", "12345", "1" }));
    }

    [Test]
    public void MeasuresAndNormalizesExplicitAndContextStrings()
    {
        XPathDynamicContext nodeContext = new XPathDynamicContext
        {
            ContextItem = NodeSequence(Parse("<root>  A😀\n B  </root>").DocumentNode!)
        };
        XPathDynamicContext atomicContext = new XPathDynamicContext
        {
            ContextItem = AtomicSequence(new XdmFactory().CreateXdmIntegerAtomicValue("12345", XdmAtomicType.Integer))
        };

        XdmAtomicValue[] values = Evaluate("(string-length('A😀B'), string-length(), normalize-space())", new XPathStaticContext(), nodeContext).AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values[0].IntegerValue, Is.EqualTo(3));
            Assert.That(values[1].IntegerValue, Is.EqualTo(9));
            Assert.That(values[2].StringValue, Is.EqualTo("A😀 B"));
            Assert.That(Evaluate("string-length()", new XPathStaticContext(), atomicContext).AtomicValues![0].IntegerValue, Is.EqualTo(5));
            Assert.That(Evaluate("normalize-space(())", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0].StringValue, Is.Empty);
        });
    }

    [Test]
    public void NormalizesUnicodeAndMapsCaseInvariantly()
    {
        XdmSequence result = Evaluate("(normalize-unicode('é'), normalize-unicode('é', ' NFD '), normalize-unicode('①', 'NFKC'), normalize-unicode('é', ''), upper-case('Abçωß'), lower-case('AbÇΩİ'), lower-case('ΟΣ ΟΣΑ'))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "é", "é", "1", "é", "ABÇΩSS", "abçωi̇", "ος οσα" }));
    }

    [Test]
    public void TranslateUsesCodepointsFirstMapOccurrenceAndDeletion()
    {
        XdmSequence result = Evaluate("(translate('bar', 'abc', 'ABC'), translate('abcd', 'aba', 'XYZ'), translate('😀a😀b', '😀ab', 'X'))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "BAr", "XYcd", "XX" }));
    }

    [Test]
    public void PerformsContainmentWithCodepointAndAsciiCaseInsensitiveCollations()
    {
        string collation = HtmlAsciiCaseInsensitiveCollation;
        string source = "(contains('tattoo', 'att'), starts-with('😀abc', '😀a'), ends-with('abc😀', 'c😀'), " +
            "substring-before('abcabc', 'bc'), substring-after('abcabc', 'bc'), contains('AlphaBeta', 'HAb', '" + collation + "'), " +
            "starts-with('Alpha', 'ALP', '" + collation + "'), ends-with('Alpha', 'PHA', '" + collation + "'))";
        XdmAtomicValue[] values = Evaluate(source, new XPathStaticContext(), new XPathDynamicContext()).AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Take(3).Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, true }));
            Assert.That(values[3].StringValue, Is.EqualTo("a"));
            Assert.That(values[4].StringValue, Is.EqualTo("abc"));
            Assert.That(values.Skip(5).Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, true }));
        });
    }

    [Test]
    public void ContainsTokenUsesXmlWhitespaceAndSelectedCollation()
    {
        string collation = HtmlAsciiCaseInsensitiveCollation;
        XdmSequence result = Evaluate("(contains-token(('red', 'green blue'), ' green '), contains-token('red, green, blue', 'red'), contains-token('red green blue', 'RED', '" + collation + "'), contains-token('red green', '   '))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, false, true, false }));
    }

    [Test]
    public void MinAndMaxOrderStringsWithTheSelectedCollation()
    {
        XPathStaticContext context = new XPathStaticContext { DefaultCollation = HtmlAsciiCaseInsensitiveCollation };
        XdmSequence result = Evaluate("(min(('b', 'A', 'a')), max(('b', 'A', 'a')), min((xs:anyURI('z'), 'c')), max((xs:anyURI('a'), xs:anyURI('z'))))", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "A", "b", "c", "z" }));
            Assert.That(result.AtomicValues![2].Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(result.AtomicValues![3].Type, Is.EqualTo(XdmAtomicType.AnyUri));
        });
    }

    [Test]
    public void ReportsStringFunctionErrorsWithStandardCodesAndSourceRanges()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:string-functions" };
        XPathDynamicException invalidCodepoint = Assert.Throws<XPathDynamicException>(() => Evaluate("codepoints-to-string(0)", context, new XPathDynamicContext()))!;
        XPathDynamicException invalidNormalization = Assert.Throws<XPathDynamicException>(() => Evaluate("normalize-unicode('x', 'FULLY-NORMALIZED')", context, new XPathDynamicContext()))!;
        XPathDynamicException missingCollation = Assert.Throws<XPathDynamicException>(() => Evaluate("compare('a', 'b', 'urn:missing')", context, new XPathDynamicContext()))!;
        XPathDynamicException wrongStringType = Assert.Throws<XPathDynamicException>(() => Evaluate("upper-case(1)", context, new XPathDynamicContext()))!;
        XPathDynamicException missingContext = Assert.Throws<XPathDynamicException>(() => Evaluate("string-length()", context, new XPathDynamicContext()))!;
        XPathStaticException wrongArity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("translate('x', 'x')", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalidCodepoint.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCH0001));
            Assert.That(invalidNormalization.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCH0003));
            Assert.That(missingCollation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCH0002));
            Assert.That(wrongStringType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(missingContext.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(missingContext.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:string-functions"));
            Assert.That(wrongArity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XdmSequence AtomicSequence(XdmAtomicValue value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(value);
    }

    private static XdmSequence NodeSequence(XmlNode value)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(value);
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

    private sealed class FoldingCollation : IXPathCollation
    {
        internal int CompareCalls;
        internal int KeyCalls;

        public int CompareXPathCollation(string left, string right)
        {
            CompareCalls = CompareCalls + 1;
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public byte[] CreateXPathCollationKey(string value)
        {
            KeyCalls = KeyCalls + 1;
            return System.Text.Encoding.UTF8.GetBytes(value.ToUpperInvariant());
        }
    }
}
