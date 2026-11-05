using xml;

namespace xml.Tests;

[TestFixture]
public class XPathMapFunctionTests
{
    [Test]
    public void ReadsMapSizeKeysContainmentAndValues()
    {
        XdmSequence result = Evaluate("(map:size(map{'a':1, 'b':2}), map:keys(map{'a':1, 'b':2}), map:contains(map{1:'numeric'}, xs:double(1)), map:contains(map{xs:double('NaN'):'nan'}, xs:float('NaN')), map:get(map{'a':(1,2)}, 'a'), map:get(map{}, 'missing'))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2", "a", "b", "true", "true", "1", "2" }));
    }

    [Test]
    public void CreatesPutsAndRemovesEntriesWithoutChangingInputs()
    {
        XdmSequence result = Evaluate("(map:get(map:entry('x', (1,2)), 'x'), map:get(map:put(map{'a':1}, 'a', 2), 'a'), map:get(map{'a':1}, 'a'), map:size(map:put(map{'a':1}, 'b', 2)), map:contains(map:remove(map{'a':1,'b':2,'c':3}, ('a','c','missing')), 'b'), map:size(map:remove(map{'a':1}, ())))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "2", "2", "1", "2", "true", "1" }));
    }

    [Test]
    public void MergesMapsUsingEveryDuplicatePolicy()
    {
        XdmSequence result = Evaluate("(map:get(map:merge((map{'a':1}, map{'a':2})), 'a'), map:get(map:merge((map{'a':1}, map{'a':2}), map{'duplicates':'use-first'}), 'a'), map:get(map:merge((map{'a':1}, map{'a':2}), map{'duplicates':'use-last'}), 'a'), map:get(map:merge((map{'a':1}, map{'a':2}), map{'duplicates':'use-any'}), 'a'), map:get(map:merge((map{'a':(1,2)}, map{'a':(3,4)}), map{'duplicates':'combine'}), 'a'), map:size(map:merge(())), map:size(map:merge((map{'a':1}, map{'b':2}), map{'vendor-option':true()})))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "1", "2", "1", "1", "2", "3", "4", "0", "2" }));
    }

    [Test]
    public void MergesLargeSetsOfDistinctNumericKeys()
    {
        XdmSequence result = Evaluate("let $map := map:merge(for $key in 1 to 10000 return map:entry($key, $key + 1)) return (map:size($map), map:get($map, 1), map:get($map, 10000))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "10000", "2", "10001" }));
    }

    [Test]
    public void UsesForAndUntypedFunctionVariablesAsMapConstructorKeys()
    {
        XdmSequence nested = Evaluate("map:merge(for $key in 1 to 3 return map:entry($key, map{$key : string($key)}))?3?3");
        XdmSequence inverted = Evaluate("map:merge(map:for-each(map{'a':1}, function($key,$value){map{$value : $key}}))?1");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(nested), Is.EqualTo(new[] { "3" }));
            Assert.That(StringValues(inverted), Is.EqualTo(new[] { "a" }));
        });
    }

    [Test]
    public void AppliesCallbacksToEachMapEntry()
    {
        XdmSequence result = Evaluate("map:for-each(map{'a':1, 'b':2}, function($key as xs:string, $value as xs:integer) {($key, $value * 10)})");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "a", "10", "b", "20" }));
    }

    [Test]
    public void FindsMatchingEntriesRecursivelyThroughMapsAndArrays()
    {
        XdmSequence result = Evaluate("map:find([map{0:'no'}, map{'nested':map{0:('yes','indeed')}}, [map{0:'again'}]], 0)");
        XdmArrayValue array = result.Arrays![0];

        Assert.Multiple(() =>
        {
            Assert.That(array.Count, Is.EqualTo(3));
            Assert.That(StringValues(array.Members![0]), Is.EqualTo(new[] { "no" }));
            Assert.That(StringValues(array.Members[1]), Is.EqualTo(new[] { "yes", "indeed" }));
            Assert.That(StringValues(array.Members[2]), Is.EqualTo(new[] { "again" }));
        });
    }

    [Test]
    public void ReportsMergePolicyAndCallbackErrors()
    {
        XPathDynamicException duplicate = Assert.Throws<XPathDynamicException>(() => Evaluate("map:merge((map{'a':1}, map{'a':2}), map{'duplicates':'reject'})"))!;
        XPathDynamicException policy = Assert.Throws<XPathDynamicException>(() => Evaluate("map:merge((map{'a':1}, map{'a':2}), map{'duplicates':'invalid'})"))!;
        XPathTypeException optionType = Assert.Throws<XPathTypeException>(() => Evaluate("map:merge((map{'a':1}, map{'a':2}), map{'duplicates':(1,2)})"))!;
        XPathTypeException callbackArity = Assert.Throws<XPathTypeException>(() => Evaluate("map:for-each(map{}, function($value) {$value})"))!;
        XPathTypeException callbackType = Assert.Throws<XPathTypeException>(() => Evaluate("map:for-each(map{'a':1}, 42)"))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0003));
            Assert.That(policy.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOJS0005));
            Assert.That(optionType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(callbackArity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(callbackType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void ReportsMapArgumentAndKeyTypeErrors()
    {
        XPathTypeException map = Assert.Throws<XPathTypeException>(() => Evaluate("map:size(1)"))!;
        XPathTypeException key = Assert.Throws<XPathTypeException>(() => Evaluate("map:get(map{}, ())"))!;
        XPathTypeException merge = Assert.Throws<XPathTypeException>(() => Evaluate("map:merge((map{}, 1))"))!;

        Assert.Multiple(() =>
        {
            Assert.That(map.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(key.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(merge.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [TestCase("map:size()")]
    [TestCase("map:get(map{})")]
    [TestCase("map:put(map{}, 'x')")]
    [TestCase("map:unknown(map{})")]
    public void RejectsUnknownMapFunctionsAndUnsupportedArities(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, Context()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, Context());
        return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
    }

    private static XPathStaticContext Context()
    {
        return new XPathStaticContext { AllowHigherOrderFunctions = true };
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }
}
