using xml;

namespace xml.Tests;

[TestFixture]
public class XPathArrayFunctionTests
{
    [Test]
    public void ReadsAndImmutablyUpdatesArrayMembers()
    {
        XdmSequence result = Evaluate("(array:size([1,(2,3)]), array:get([1,(2,3)],2), array:get(array:put([1,2],2,(8,9)),2), array:get([1,2],2), array:get(array:append([1],(4,5)),2))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2", "2", "3", "8", "9", "2", "4", "5" }));
    }

    [Test]
    public void ExtractsInsertsRemovesAndReordersMembers()
    {
        XdmSequence result = Evaluate("(array:flatten(array:subarray([1,2,3,4],2)), array:flatten(array:subarray([1,2,3,4],2,2)), array:flatten(array:remove([1,2,3,4],(1,3,3))), array:flatten(array:insert-before([1,3],2,(7,8))), array:head([(1,2),3]), array:flatten(array:tail([1,2,3])), array:flatten(array:reverse([1,2,3])))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2", "3", "4", "2", "3", "2", "4", "1", "7", "8", "3", "1", "2", "2", "3", "3", "2", "1" }));
    }

    [Test]
    public void JoinsEmptyAndPopulatedArraySequences()
    {
        XdmSequence result = Evaluate("(array:size(array:join(())), array:flatten(array:join(([1,2], [(3,4)], [], [5]))))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "0", "1", "2", "3", "4", "5" }));
    }

    [Test]
    public void AppliesForEachAndFilterCallbacksToWholeMembers()
    {
        XdmSequence result = Evaluate("(array:flatten(array:for-each([(1,2),(3,4)], function($member) {sum($member)})), array:flatten(array:filter([1,2,3,4], function($member) {$member mod 2 eq 0})))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "3", "7", "2", "4" }));
    }

    [Test]
    public void FoldsArraysInBothDirectionsAndPairsUntilExhaustion()
    {
        XdmSequence result = Evaluate("(array:fold-left([1,2,3],0,function($acc,$member){$acc * 10 + $member}), array:fold-right([1,2,3],'',function($member,$acc){concat($member,'[',$acc,']')}), array:fold-left([],('zero','value'),function($acc,$member){()}), array:flatten(array:for-each-pair([1,2,3], [10,20], function($left,$right){($left,$right)})))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "123", "1[2[3[]]]", "zero", "value", "1", "10", "2", "20" }));
    }

    [Test]
    public void SortsMembersLexicographicallyStablyAndByKeys()
    {
        XdmSequence result = Evaluate("(array:flatten(array:sort([3,1,2,1])), array:flatten(array:sort([-10,10,-2,1],(),function($member){abs($member)})), array:flatten(array:sort([(1,1),(1,0),(0,1)])), array:flatten(array:sort(['b','A','a'],'http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')))");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "1", "2", "3", "1", "-2", "-10", "10", "0", "1", "1", "0", "1", "1", "A", "a", "b" }));
    }

    [Test]
    public void RecursivelyFlattensArraysWhilePreservingOtherItemKinds()
    {
        XdmSequence result = Evaluate("array:flatten((0,[1,[(2,3),[4]],map{'x':5}],6))");

        Assert.Multiple(() =>
        {
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Map, XdmItemKind.Atomic }));
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "0", "1", "2", "3", "4", "6" }));
        });
    }

    [Test]
    public void ReportsArrayBoundsAndSubarrayLengthErrors()
    {
        XPathDynamicException get = Assert.Throws<XPathDynamicException>(() => Evaluate("array:get([1],0)"))!;
        XPathDynamicException head = Assert.Throws<XPathDynamicException>(() => Evaluate("array:head([])"))!;
        XPathDynamicException insert = Assert.Throws<XPathDynamicException>(() => Evaluate("array:insert-before([1],3,2)"))!;
        XPathDynamicException extent = Assert.Throws<XPathDynamicException>(() => Evaluate("array:subarray([1,2],2,2)"))!;
        XPathDynamicException length = Assert.Throws<XPathDynamicException>(() => Evaluate("array:subarray([1,2],1,-1)"))!;

        Assert.Multiple(() =>
        {
            Assert.That(get.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(head.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(insert.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(extent.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(length.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0002));
        });
    }

    [Test]
    public void ReportsArrayArgumentCallbackAndSortErrors()
    {
        XPathTypeException array = Assert.Throws<XPathTypeException>(() => Evaluate("array:size(1)"))!;
        XPathTypeException position = Assert.Throws<XPathTypeException>(() => Evaluate("array:get([1],1.5)"))!;
        XPathTypeException join = Assert.Throws<XPathTypeException>(() => Evaluate("array:join(([1],2))"))!;
        XPathTypeException callback = Assert.Throws<XPathTypeException>(() => Evaluate("array:for-each([],function($left,$right){$left})"))!;
        XPathTypeException predicate = Assert.Throws<XPathTypeException>(() => Evaluate("array:filter([1],function($member){$member})"))!;
        XPathDynamicException sort = Assert.Throws<XPathDynamicException>(() => Evaluate("array:sort([1,'a'])"))!;

        Assert.Multiple(() =>
        {
            Assert.That(array.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(position.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(join.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(callback.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(predicate.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(sort.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [TestCase("array:size()")]
    [TestCase("array:join([], [])")]
    [TestCase("array:subarray([1])")]
    [TestCase("array:fold-left([1],0)")]
    [TestCase("array:unknown([])")]
    public void RejectsUnknownArrayFunctionsAndUnsupportedArities(string source)
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
