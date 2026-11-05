using xml;

namespace xml.Tests;

[TestFixture]
public class XPathSequenceFunctionTests
{
    [Test]
    public void InsertsRemovesReversesAndPreservesAllItemKinds()
    {
        XdmSequence inserted = Evaluate("insert-before((1, 2, 3), 2, ('a', 'b'))", false, null);
        XdmSequence insertedAtStart = Evaluate("insert-before((1, 2), -10, 0)", false, null);
        XdmSequence insertedAtEnd = Evaluate("insert-before((1, 2), 100, 3)", false, null);
        XdmSequence removed = Evaluate("remove((1, 2, 3), 2)", false, null);
        XdmSequence unchanged = Evaluate("remove((1, 2, 3), 0)", false, null);
        XdmSequence reversed = Evaluate("reverse((1, map{'k': 2}, [3]))", true, null);
        XdmSequence unordered = Evaluate("unordered((1, 2, 3))", false, null);

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(inserted), Is.EqualTo(new[] { "1", "a", "b", "2", "3" }));
            Assert.That(StringValues(insertedAtStart), Is.EqualTo(new[] { "0", "1", "2" }));
            Assert.That(StringValues(insertedAtEnd), Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(StringValues(removed), Is.EqualTo(new[] { "1", "3" }));
            Assert.That(StringValues(unchanged), Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(reversed.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Array, XdmItemKind.Map, XdmItemKind.Atomic }));
            Assert.That(StringValues(unordered), Is.EquivalentTo(new[] { "1", "2", "3" }));
        });
    }

    [Test]
    public void SelectsSubsequencesUsingXPathRoundingAndSpecialValues()
    {
        XdmSequence rounded = Evaluate("subsequence((1, 2, 3, 4, 5), 1.5, 2.6)", false, null);
        XdmSequence negative = Evaluate("subsequence((1, 2, 3), -2, 4)", false, null);
        XdmSequence infinite = Evaluate("subsequence((1, 2, 3), xs:double('-INF'), xs:double('INF'))", false, null);
        XdmSequence nan = Evaluate("subsequence((1, 2, 3), xs:double('NaN'))", false, null);
        XdmSequence nonMidpoint = Evaluate("subsequence((1, 2, 3), 1.1, 1.4)", false, null);

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(rounded), Is.EqualTo(new[] { "2", "3", "4" }));
            Assert.That(StringValues(negative), Is.EqualTo(new[] { "1" }));
            Assert.That(infinite.Count, Is.Zero);
            Assert.That(nan.Count, Is.Zero);
            Assert.That(StringValues(nonMidpoint), Is.EqualTo(new[] { "1" }));
        });
    }

    [Test]
    public void FindsIndexesAndDistinctValuesWithAtomicAndCollationSemantics()
    {
        string insensitive = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";
        XdmSequence indexes = Evaluate("index-of(('A', 'b', 'a', 'A'), 'a', '" + insensitive + "')", false, null);
        XdmSequence distinct = Evaluate("distinct-values(('A', 'a', 'B', 'b'), '" + insensitive + "')", false, null);
        XdmSequence numeric = Evaluate("distinct-values((1, 1.0, xs:double('NaN'), xs:float('NaN'), -0.0, 0.0))", false, null);
        XdmSequence incomparable = Evaluate("index-of((1, xs:date('2026-01-01'), '1'), true())", false, null);
        XdmSequence arrayIndexes = Evaluate("index-of([1, [5, 6], [6, 7]], 6)", true, null);

        Assert.Multiple(() =>
        {
            Assert.That(indexes.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 1, 3, 4 }));
            Assert.That(StringValues(distinct), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(numeric.Count, Is.EqualTo(3));
            Assert.That(numeric.AtomicValues!.Count(value => double.IsNaN(value.FloatingValue)), Is.EqualTo(1));
            Assert.That(incomparable.Count, Is.Zero);
            Assert.That(arrayIndexes.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 3, 4 }));
        });
    }

    [Test]
    public void DeepEqualComparesNodesMapsArraysAndNanRecursively()
    {
        XmlDocument document = Parse("<root><item a='1' b='2'>x<!--first--><child/></item><item b='2' a='1'>x<!--second--><child/></item><item a='9' b='2'>x<child/></item></root>");
        XPathDynamicContext context = Context(document.DocumentNode!);
        XdmAtomicValue[] nodes = Evaluate("(deep-equal(/*/item[1], /*/item[2]), deep-equal(/*/item[1], /*/item[3]), deep-equal(/*/item[1]/comment(), /*/item[2]/comment()))", false, context).AtomicValues!;
        XdmAtomicValue[] structures = Evaluate("(deep-equal(map{1:'a', 2:[xs:double('NaN'), (3, 4)]}, map{2:[xs:float('NaN'), (3, 4)], 1:'a'}), deep-equal([1, (2, 3)], [1, (2, 3)]), deep-equal((1, 2, 3), [1, 2, 3]))", true, null).AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(nodes.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, false, false }));
            Assert.That(structures.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, false }));
        });
    }

    [Test]
    public void DeepEqualUsesSameKeyForMapsAndTheSelectedCollationForValues()
    {
        string insensitive = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";
        XdmAtomicValue[] values = Evaluate("(deep-equal(map{1:'A'}, map{1.0:'a'}, '" + insensitive + "'), deep-equal(map{'A':1}, map{'a':1}, '" + insensitive + "'))", true, null).AtomicValues!;

        Assert.That(values.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void SortsLexicographicallyAndStablyWithDefaultAndSuppliedKeys()
    {
        XdmSequence numeric = Evaluate("sort((4, 1, 3, 2))", false, null);
        XdmSequence stable = Evaluate("sort((1, -2, 5, 10, -10, 10, 8), (), abs#1)", true, null);
        XdmSequence lexical = Evaluate("sort(('b2', 'a2', 'a1'), (), function($value) {(substring($value, 2), substring($value, 1, 1))})", true, null);
        string insensitive = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";
        XdmSequence strings = Evaluate("sort(('b', 'A', 'a', 'B'), '" + insensitive + "')", false, null);

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(numeric), Is.EqualTo(new[] { "1", "2", "3", "4" }));
            Assert.That(StringValues(stable), Is.EqualTo(new[] { "1", "-2", "5", "8", "10", "-10", "10" }));
            Assert.That(StringValues(lexical), Is.EqualTo(new[] { "a1", "a2", "b2" }));
            Assert.That(StringValues(strings), Is.EqualTo(new[] { "A", "a", "b", "B" }));
        });
    }

    [Test]
    public void ReportsSequenceFunctionTypeCollationFunctionAndArityErrors()
    {
        XPathTypeException position = Assert.Throws<XPathTypeException>(() => Evaluate("remove((1, 2), 1.5)", false, null))!;
        XPathDynamicException sortTypes = Assert.Throws<XPathDynamicException>(() => Evaluate("sort((1, 'a'))", false, null))!;
        XPathDynamicException binaryOrder = Assert.Throws<XPathDynamicException>(() => Evaluate("sort((xs:hexBinary('01'), xs:hexBinary('02')))", false, null))!;
        XPathDynamicException collation = Assert.Throws<XPathDynamicException>(() => Evaluate("distinct-values(('a', 'b'), 'urn:missing')", false, null))!;
        XPathDynamicException function = Assert.Throws<XPathDynamicException>(() => Evaluate("deep-equal(true#0, true#0)", true, null))!;
        XPathStaticException arity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("insert-before((1, 2), 1)", new XPathStaticContext()))!;
        XPathStaticException higherOrder = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("sort((1, 2), (), abs#1)", new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(position.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(sortTypes.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(binaryOrder.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(collation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCH0002));
            Assert.That(function.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0015));
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
            Assert.That(higherOrder.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
        });
    }

    private static XdmSequence Evaluate(string source, bool higherOrder, XPathDynamicContext? dynamicContext)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = higherOrder };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext == null ? new XPathDynamicContext() : dynamicContext).Value!;
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }

    private static XPathDynamicContext Context(XmlNode node)
    {
        return new XPathDynamicContext { ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(node) };
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            PreservePrefixes = true,
            PreserveComments = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }
}
