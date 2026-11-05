using xml;

namespace xml.Tests;

[TestFixture]
public class XPathFunctionEvaluatorTests
{
    [Test]
    public void EvaluatesStaticAndNamedIntrinsicFunctions()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence result = Evaluate("(fn:boolean(1), fn:not(()), (fn:boolean#1)(0))", context, new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, true, false }));
    }

    [Test]
    public void ConvertsInlineFunctionArgumentsAndResults()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence result = Evaluate("(function($value as xs:double) as xs:double {$value})(2)", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(result.AtomicValues[0].FloatingValue, Is.EqualTo(2));
        });
    }

    [Test]
    public void CapturesLexicalValuesBeforeLaterShadowing()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "let $increment := 2, " +
            "$add := function($value as xs:integer) as xs:integer {$value + $increment}, " +
            "$increment := 100 return $add(3)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 5 }));
    }

    [Test]
    public void ReturnedClosuresRemainCallableAfterTheirCreatingEvaluation()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XdmSequence closure = Evaluate("let $captured := 7 return function() as xs:integer {$captured}", context, new XPathDynamicContext());
        XdmFunctionItem function = closure.Functions![0];

        XdmSequence result = new XdmFunctionOperations().InvokeXdmFunctionItem(function, [], new XPathDynamicContext(), context);

        Assert.That(Integers(result), Is.EqualTo(new long[] { 7 }));
    }

    [Test]
    public void InlineFunctionFocusIsAbsent()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XmlDocument document = Parse("<root/>");
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!)
        };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("(function(){.})()", context, dynamicContext))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
    }

    [Test]
    public void PartiallyAppliesInlineFunctionsInPlaceholderOrder()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "let $combine := function($a as xs:integer, $b as xs:integer, $c as xs:integer) as xs:integer {$a * 100 + $b * 10 + $c}, " +
            "$partial := $combine(?, 2, ?) return $partial(1, 3)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 123 }));
    }

    [Test]
    public void ConvertsFixedArgumentsWhenPartialFunctionIsCreated()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "let $function := function($fixed as xs:integer, $later) {$fixed} return $function('invalid', ?)";

        XPathTypeException error = Assert.Throws<XPathTypeException>(() => Evaluate(source, context, new XPathDynamicContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void InvokesConstructedMapsAndBothArrayFormsAsFunctions()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(map {'key': (1, 2)}('key'), [10, (20, 21)](2), array {(30, 31)}(2))";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 1, 2, 20, 21, 31 }));
    }

    [Test]
    public void PartialApplicationOfMapProducesAnOrdinaryFunction()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence result = Evaluate("let $lookup := map {'answer': 42}(?) return $lookup('answer')", context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 42 }));
    }

    [Test]
    public void EvaluatesEveryPostfixLookupKeyForm()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string mapSource = "(map {'name': 'Ada', 2: 'two'}?name, " +
            "map {'name': 'Ada', 2: 'two'}?2, " +
            "map {'name': 'Ada', 2: 'two'}?('name', 2), " +
            "map {'first': 'A', 'second': 'B'}?*)";
        string arraySource = "([10, (20, 21), 30]?2, [10, (20, 21), 30]?(3, 1), [10, (20, 21), 30]?*)";

        XdmSequence maps = Evaluate(mapSource, context, new XPathDynamicContext());
        XdmSequence arrays = Evaluate(arraySource, context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(Strings(maps), Is.EqualTo(new[] { "Ada", "two", "Ada", "two", "A", "B" }));
            Assert.That(Integers(arrays), Is.EqualTo(new long[] { 20, 21, 30, 10, 10, 20, 21, 30 }));
        });
    }

    [Test]
    public void UnaryLookupUsesSimpleMapAndPredicateFocus()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XdmSequence maps = Evaluate("(map {'value': 1}, map {'value': 2}) ! ?value", context, new XPathDynamicContext());
        XdmSequence arrays = Evaluate("([1, 2], [3, 4])[?2 eq 4]?1", context, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(Integers(maps), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(Integers(arrays), Is.EqualTo(new long[] { 3 }));
        });
    }

    [Test]
    public void ParenthesizedLookupKeysRemainLazyAndRunOncePerTarget()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathTraceCollector collector = new XPathTraceCollector();

        XdmSequence empty = Evaluate("()?(1 idiv 0)", context, new XPathDynamicContext());
        XPathEvaluationResult repeated = EvaluateResult("(map {'x': 1}, map {'x': 2})?(fn:trace('x', 'key'))", context, new XPathDynamicContext
        {
            TraceReceiver = collector
        });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Count, Is.EqualTo(0));
            Assert.That(Integers(repeated.Value!), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(repeated.TraceEvents!.Select(item => item.Label).ToArray(), Is.EqualTo(new[] { "key", "key" }));
        });
    }

    [Test]
    public void LookupReportsContextTargetKeyAndBoundsErrors()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathDynamicException absent = Assert.Throws<XPathDynamicException>(() => Evaluate("?name", context, new XPathDynamicContext()))!;
        XPathDynamicException target = Assert.Throws<XPathDynamicException>(() => Evaluate("(function(){1})()?name", context, new XPathDynamicContext()))!;
        XPathDynamicException nameKey = Assert.Throws<XPathDynamicException>(() => Evaluate("[1]?name", context, new XPathDynamicContext()))!;
        XPathDynamicException decimalKey = Assert.Throws<XPathDynamicException>(() => Evaluate("[1]?(1.0)", context, new XPathDynamicContext()))!;
        XPathDynamicException bounds = Assert.Throws<XPathDynamicException>(() => Evaluate("[1]?2", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(absent.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(target.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(nameKey.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(decimalKey.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(bounds.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(bounds.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void LookupExpansionCountsAgainstTheOperationLimit()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathDynamicContext dynamicContext = new XPathDynamicContext { MaximumOperations = 8 };

        XmlLimitException exception = Assert.Throws<XmlLimitException>(() => Evaluate("[1, 2, 3]?*", context, dynamicContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void RejectsDuplicateMapKeysAndWrongCallableArities()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathDynamicException duplicate = Assert.Throws<XPathDynamicException>(() => Evaluate("map {'key': 1, 'key': 2}", context, new XPathDynamicContext()))!;
        XPathDynamicException arity = Assert.Throws<XPathDynamicException>(() => Evaluate("map {'key': 1}()", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo("XQDY0137"));
            Assert.That(duplicate.Diagnostic.Range, Is.Not.Null);
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(arity.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void ResolvesPrefixedInlineParametersByExpandedName()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            AllowHigherOrderFunctions = true,
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:parameters" }]
        };

        XdmSequence result = Evaluate("(function($p:value as xs:integer) {$p:value})(9)", context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 9 }));
    }

    [Test]
    public void ExecutesDeepSelfTailRecursionWithoutGrowingTheCallStack()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "let $loop := function($self, $remaining as xs:integer) as xs:integer {" +
            "if ($remaining eq 0) then 0 else $self($self, $remaining - 1)} " +
            "return $loop($loop, 2000)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 0 }));
    }

    [Test]
    public void CountsTailIterationsAgainstTheOperationLimit()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathDynamicContext dynamicContext = new XPathDynamicContext { MaximumOperations = 20 };
        string source = "let $loop := function($self, $remaining as xs:integer) as xs:integer {" +
            "if ($remaining eq 0) then 0 else $self($self, $remaining - 1)} " +
            "return $loop($loop, 100)";

        XmlLimitException error = Assert.Throws<XmlLimitException>(() => Evaluate(source, context, dynamicContext))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
    }

    [Test]
    public void EvaluatesPositionAndLastAgainstTheCurrentFocus()
    {
        XPathStaticContext context = new XPathStaticContext();
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue("item", XdmAtomicType.String)),
            Position = 2,
            Size = 4
        };

        XdmSequence rootFocus = Evaluate("(position(), last())", context, dynamicContext);
        XdmSequence predicateFocus = Evaluate("(10, 20, 30)[position() = 2 and last() = 3]", context, dynamicContext);

        Assert.Multiple(() =>
        {
            Assert.That(Integers(rootFocus), Is.EqualTo(new long[] { 2, 4 }));
            Assert.That(Integers(predicateFocus), Is.EqualTo(new long[] { 20 }));
        });
    }

    [Test]
    public void PositionAndLastRequireAnAvailableFocus()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:focus-functions" };

        XPathDynamicException position = Assert.Throws<XPathDynamicException>(() => Evaluate("position()", context, new XPathDynamicContext()))!;
        XPathDynamicException last = Assert.Throws<XPathDynamicException>(() => Evaluate("last()", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(position.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(position.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:focus-functions"));
            Assert.That(last.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(last.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void ReturnsStableCurrentValuesAndDefaultContextMetadata()
    {
        const string collation = "urn:collation:configured";
        XPathStaticContext staticContext = new XPathStaticContext
        {
            BaseUri = "https://example.test/query/../query/main.xpath",
            DefaultCollation = collation,
            Collations = [collation]
        };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            CurrentDateTime = new DateTimeOffset(2026, 3, 1, 1, 2, 3, 450, TimeSpan.Zero),
            ImplicitTimezoneMinutes = -330
        };

        XdmSequence result = Evaluate("(current-dateTime(), current-dateTime(), current-date(), current-time(), implicit-timezone(), default-collation(), static-base-uri())", staticContext, dynamicContext);
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(item => item.Type).ToArray(), Is.EqualTo(new[]
            {
                XdmAtomicType.DateTimeStamp,
                XdmAtomicType.DateTimeStamp,
                XdmAtomicType.Date,
                XdmAtomicType.Time,
                XdmAtomicType.DayTimeDuration,
                XdmAtomicType.String,
                XdmAtomicType.AnyUri
            }));
            Assert.That(values[0].CanonicalValue, Is.EqualTo("2026-02-28T19:32:03.45-05:30"));
            Assert.That(values[1].CanonicalValue, Is.EqualTo(values[0].CanonicalValue));
            Assert.That(values[2].CanonicalValue, Is.EqualTo("2026-02-28-05:30"));
            Assert.That(values[3].CanonicalValue, Is.EqualTo("19:32:03.45-05:30"));
            Assert.That(values[4].CanonicalValue, Is.EqualTo("-PT5H30M"));
            Assert.That(values[5].StringValue, Is.EqualTo(collation));
            Assert.That(values[6].StringValue, Is.EqualTo("https://example.test/query/main.xpath"));
        });
    }

    [Test]
    public void StaticBaseUriIsEmptyWhenTheStaticContextHasNone()
    {
        XdmSequence result = Evaluate("static-base-uri()", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void EvaluatesSequenceInspectionAndPartitionFunctionsForMixedItems()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(count((1, map {'a': 2}, [3])), empty(()), exists(map {}), " +
            "head((map {'x': 1}, [2])), tail((map {'x': 1}, [2], function(){3})), " +
            "count(head(())), count(tail(())), (fn:count#1)((4, 5)))";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(result.ItemKinds, Is.EqualTo(new[]
            {
                XdmItemKind.Atomic,
                XdmItemKind.Atomic,
                XdmItemKind.Atomic,
                XdmItemKind.Map,
                XdmItemKind.Array,
                XdmItemKind.Function,
                XdmItemKind.Atomic,
                XdmItemKind.Atomic,
                XdmItemKind.Atomic
            }));
            Assert.That(values.Select(item => item.Type).ToArray(), Is.EqualTo(new[]
            {
                XdmAtomicType.Integer,
                XdmAtomicType.Boolean,
                XdmAtomicType.Boolean,
                XdmAtomicType.Integer,
                XdmAtomicType.Integer,
                XdmAtomicType.Integer
            }));
            Assert.That(values.Select(item => item.IntegerValue).ToArray(), Is.EqualTo(new long[] { 3, 0, 0, 0, 0, 2 }));
            Assert.That(values.Select(item => item.BooleanValue).ToArray(), Is.EqualTo(new[] { false, true, true, false, false, false }));
            Assert.That(result.Maps, Has.Length.EqualTo(1));
            Assert.That(result.Arrays, Has.Length.EqualTo(1));
            Assert.That(result.Functions, Has.Length.EqualTo(1));
            Assert.That(result.IsImmutable, Is.True);
        });
    }

    [Test]
    public void CardinalityFunctionsReturnTheirInputsWhenValid()
    {
        XPathStaticContext context = new XPathStaticContext();

        XdmSequence result = Evaluate("(zero-or-one(()), zero-or-one(7), one-or-more((8, 9)), exactly-one(10))", context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 7, 8, 9, 10 }));
    }

    [Test]
    public void CardinalityFunctionsReportTheirRequiredForgErrors()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:cardinality-functions" };

        XPathDynamicException zeroOrOne = Assert.Throws<XPathDynamicException>(() => Evaluate("zero-or-one((1, 2))", context, new XPathDynamicContext()))!;
        XPathDynamicException oneOrMore = Assert.Throws<XPathDynamicException>(() => Evaluate("one-or-more(())", context, new XPathDynamicContext()))!;
        XPathDynamicException exactlyOneEmpty = Assert.Throws<XPathDynamicException>(() => Evaluate("exactly-one(())", context, new XPathDynamicContext()))!;
        XPathDynamicException exactlyOneMany = Assert.Throws<XPathDynamicException>(() => Evaluate("exactly-one((1, 2))", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(zeroOrOne.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0003));
            Assert.That(oneOrMore.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0004));
            Assert.That(exactlyOneEmpty.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0005));
            Assert.That(exactlyOneMany.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0005));
            Assert.That(zeroOrOne.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:cardinality-functions"));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        return EvaluateResult(source, staticContext, dynamicContext).Value!;
    }

    private static XPathEvaluationResult EvaluateResult(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext);
    }

    private static long[] Integers(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.IntegerValue).ToArray();
    }

    private static bool[] Booleans(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.BooleanValue).ToArray();
    }

    private static string[] Strings(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.StringValue!).ToArray();
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
