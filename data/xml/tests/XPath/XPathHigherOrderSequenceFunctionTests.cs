using xml;

namespace xml.Tests;

[TestFixture]
public class XPathHigherOrderSequenceFunctionTests
{
    [Test]
    public void AppliesForEachAndPreservesEveryItemKind()
    {
        XdmSequence expanded = Evaluate("for-each((1, 2, 3), function($value) {($value, $value * 10)})");
        XdmSequence preserved = Evaluate("for-each((map{'a': 1}, [2], true#0), function($value) {$value})");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(expanded), Is.EqualTo(new[] { "1", "10", "2", "20", "3", "30" }));
            Assert.That(preserved.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Map, XdmItemKind.Array, XdmItemKind.Function }));
        });
    }

    [Test]
    public void FiltersOnlyOnAFunctionConvertedBoolean()
    {
        XdmSequence ordinary = Evaluate("filter(1 to 6, function($value) {$value mod 2 eq 0})");
        XdmSequence converted = Evaluate("filter(1 to 3, function($value) as xs:untypedAtomic {xs:untypedAtomic(if ($value ne 2) then 'true' else 'false')})");
        XdmSequence mapped = Evaluate("filter(('Monday', 'Tuesday', 'Wednesday'), map{'Monday': true(), 'Tuesday': false(), 'Wednesday': true()})");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(ordinary), Is.EqualTo(new[] { "2", "4", "6" }));
            Assert.That(StringValues(converted), Is.EqualTo(new[] { "1", "3" }));
            Assert.That(StringValues(mapped), Is.EqualTo(new[] { "Monday", "Wednesday" }));
        });
    }

    [Test]
    public void FoldsSequencesInTheRequiredDirectionWithSequenceAccumulators()
    {
        XdmSequence left = Evaluate("fold-left((1, 2, 3), 0, function($accumulator, $item) {$accumulator * 10 + $item})");
        XdmSequence right = Evaluate("fold-right((1, 2, 3), '', function($item, $accumulator) {concat($item, '[', $accumulator, ']')})");
        XdmSequence accumulated = Evaluate("fold-left((1, 2, 3), (), function($accumulator, $item) {($item, $accumulator)})");
        XdmSequence empty = Evaluate("fold-right((), (4, 5), function($item, $accumulator) {()})");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(left), Is.EqualTo(new[] { "123" }));
            Assert.That(StringValues(right), Is.EqualTo(new[] { "1[2[3[]]]" }));
            Assert.That(StringValues(accumulated), Is.EqualTo(new[] { "3", "2", "1" }));
            Assert.That(StringValues(empty), Is.EqualTo(new[] { "4", "5" }));
        });
    }

    [Test]
    public void AppliesForEachPairUntilEitherInputIsExhausted()
    {
        XdmSequence result = Evaluate("for-each-pair((1, 2, 3), ('a', 'b'), function($left, $right) {($left, $right)})");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "a", "2", "b" }));
    }

    [Test]
    public void CoercesMapsArraysAndTypedFunctionsAtInvocation()
    {
        XdmSequence map = Evaluate("for-each(('a', 'b'), map{'a': 1, 'b': (2, 3)})");
        XdmSequence array = Evaluate("for-each((2, 1), ['a', 'b'])");
        XdmSequence typed = Evaluate("for-each((xs:untypedAtomic('1'), xs:untypedAtomic('2')), function($value as xs:integer) as xs:integer {$value + 1})");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(map), Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(StringValues(array), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(StringValues(typed), Is.EqualTo(new[] { "2", "3" }));
        });
    }

    [Test]
    public void CoercesNestedSameArityFunctionArguments()
    {
        string source = "let $applyTwice := function($function as (function(item()) as item()), $value as xs:string) {$function($function($value))} " +
            "return for-each((upper-case#1, lower-case#1, normalize-space#1, concat(?, '!')), $applyTwice(?, ' Say NO! '))";

        XdmSequence result = Evaluate(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { " SAY NO! ", " say no! ", "Say NO!", " Say NO! !!" }));
    }

    [Test]
    public void ValidatesParameterizedArrayArgumentsAndDynamicArrowTargets()
    {
        XdmSequence arrow = Evaluate("4.54=>((ceiling#1, floor#1, round#1, abs#1)=>head())()");
        XPathTypeException arrayType = Assert.Throws<XPathTypeException>(() => Evaluate("let $f := function($value as array(xs:string)) { array:size($value) } return $f(['a', 0])"))!;

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(arrow), Is.EqualTo(new[] { "5" }));
            Assert.That(arrayType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void ReportsFunctionCoercionErrorsAndPropagatesCallbackErrors()
    {
        XPathTypeException predicateType = Assert.Throws<XPathTypeException>(() => Evaluate("filter((1, 2), function($value) {$value})"))!;
        XPathTypeException missingMapKey = Assert.Throws<XPathTypeException>(() => Evaluate("filter(('present', 'missing'), map{'present': true()})"))!;
        XPathDynamicException wrongArity = Assert.Throws<XPathDynamicException>(() => Evaluate("for-each((1, 2), true#0)"))!;
        XPathDynamicException notCallable = Assert.Throws<XPathDynamicException>(() => Evaluate("fold-left((1, 2), 0, 42)"))!;
        XPathTypeException argumentType = Assert.Throws<XPathTypeException>(() => Evaluate("for-each('invalid', function($value as xs:integer) {$value})"))!;
        XPathDynamicException callback = Assert.Throws<XPathDynamicException>(() => Evaluate("for-each((1, 2), function($value) {exactly-one(())})"))!;

        Assert.Multiple(() =>
        {
            Assert.That(predicateType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(missingMapKey.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(wrongArity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(notCallable.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(argumentType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(callback.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0005));
        });
    }

    [TestCase("for-each((1, 2), function($value) {$value})")]
    [TestCase("filter((1, 2), function($value) {true()})")]
    [TestCase("fold-left((1, 2), 0, function($left, $right) {$left + $right})")]
    [TestCase("fold-right((1, 2), 0, function($left, $right) {$left + $right})")]
    [TestCase("for-each-pair((1, 2), (3, 4), function($left, $right) {$left + $right})")]
    [TestCase("for-each#2")]
    public void RequiresHigherOrderFunctions(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    [TestCase("for-each((1, 2))")]
    [TestCase("filter((1, 2), true#0, false#0)")]
    [TestCase("fold-left((1, 2), 0)")]
    [TestCase("fold-right((1, 2), 0, true#0, false#0)")]
    [TestCase("for-each-pair((1, 2), (3, 4))")]
    public void RejectsUnsupportedArities(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, new XPathStaticContext { AllowHigherOrderFunctions = true }))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source)
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }
}
