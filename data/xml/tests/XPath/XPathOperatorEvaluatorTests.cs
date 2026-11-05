using xml;

namespace xml.Tests;

[TestFixture]
public class XPathOperatorEvaluatorTests
{
    [Test]
    public void EvaluatesEveryBinaryNumericOperatorWithPromotion()
    {
        XPathVariableBinding integer = Binding("integer", Sequence(Integer("7")), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XPathVariableBinding decimalValue = Binding("decimalValue", Sequence(Decimal("2.5")), XdmAtomicType.Decimal, XdmOccurrence.ExactlyOne);
        XPathVariableBinding single = Binding("single", Sequence(Float("2")), XdmAtomicType.Float, XdmOccurrence.ExactlyOne);
        XPathVariableBinding doubleValue = Binding("doubleValue", Sequence(Double("0.5")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne);

        Assert.Multiple(() =>
        {
            AssertAtomic(Evaluate("$integer + 3", [integer]), XdmAtomicType.Integer, "10");
            AssertAtomic(Evaluate("$integer - 10", [integer]), XdmAtomicType.Integer, "-3");
            AssertAtomic(Evaluate("$decimalValue * 2", [decimalValue]), XdmAtomicType.Decimal, "5");
            AssertAtomic(Evaluate("$integer div 2", [integer]), XdmAtomicType.Decimal, "3.5");
            AssertAtomic(Evaluate("$integer idiv 2", [integer]), XdmAtomicType.Integer, "3");
            AssertAtomic(Evaluate("$integer mod 4", [integer]), XdmAtomicType.Integer, "3");
            Assert.That(Evaluate("$single + $doubleValue", [single, doubleValue]).AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(Evaluate("$single + $doubleValue", [single, doubleValue]).AtomicValues![0].FloatingValue, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void EvaluatesUnaryArithmeticAndStrictEmptySemantics()
    {
        XPathVariableBinding value = Binding("value", Sequence(Integer("4")), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XPathVariableBinding empty = Binding("empty", Sequence(), XdmAtomicType.Integer, XdmOccurrence.ZeroOrOne);

        Assert.Multiple(() =>
        {
            AssertAtomic(Evaluate("+$value", [value]), XdmAtomicType.Integer, "4");
            AssertAtomic(Evaluate("-$value", [value]), XdmAtomicType.Integer, "-4");
            Assert.That(Evaluate("$empty + 1", [empty]).Count, Is.EqualTo(0));
            Assert.That(Evaluate("-$empty", [empty]).Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void ConvertsUntypedArithmeticAndReportsArithmeticErrors()
    {
        XPathVariableBinding untyped = Binding("untyped", Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("2.5")), XdmAtomicType.UntypedAtomic, XdmOccurrence.ExactlyOne);
        XPathVariableBinding invalid = Binding("invalid", Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("number")), XdmAtomicType.UntypedAtomic, XdmOccurrence.ExactlyOne);
        XPathVariableBinding many = Binding("many", Sequence(Integer("1"), Integer("2")), XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);
        XPathVariableBinding text = Binding("text", Sequence(String("2")), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne);

        Assert.That(Evaluate("$untyped + 1", [untyped]).AtomicValues![0].FloatingValue, Is.EqualTo(3.5));
        Assert.That(double.IsPositiveInfinity(Evaluate("$one div 0e0", [Binding("one", Sequence(Double("1")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne)]).AtomicValues![0].FloatingValue), Is.True);

        XPathDynamicException lexical = Assert.Throws<XPathDynamicException>(() => Evaluate("$invalid + 1", [invalid]))!;
        XPathDynamicException cardinality = Assert.Throws<XPathDynamicException>(() => Evaluate("$many + 1", [many]))!;
        XPathDynamicException type = Assert.Throws<XPathDynamicException>(() => Evaluate("$text + 1", [text]))!;
        XPathDynamicException divideByZero = Assert.Throws<XPathDynamicException>(() => Evaluate("1 div 0", []))!;
        XPathDynamicException invalidIntegerDivision = Assert.Throws<XPathDynamicException>(() => Evaluate("$nan idiv 1", [Binding("nan", Sequence(Double("NaN")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne)]))!;
        XPathDynamicException overflowingIntegerDivision = Assert.Throws<XPathDynamicException>(() => Evaluate("$large idiv $small", [
            Binding("large", Sequence(Double("1e308")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne),
            Binding("small", Sequence(Double("1e-308")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne)
        ]))!;

        Assert.Multiple(() =>
        {
            Assert.That(lexical.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(type.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(divideByZero.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(divideByZero.Diagnostic.Range, Is.Not.Null);
            Assert.That(invalidIntegerDivision.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0002));
            Assert.That(overflowingIntegerDivision.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0002));
            Assert.That(overflowingIntegerDivision.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void AppliesBackwardsCompatibleArithmeticConversion()
    {
        XPathVariableBinding empty = Binding("empty", Sequence(), XdmAtomicType.Double, XdmOccurrence.ZeroOrOne);
        XPathVariableBinding many = Binding("many", Sequence(String("3"), String("ignored")), XdmAtomicType.String, XdmOccurrence.ZeroOrMore);
        XPathStaticContext context = new XPathStaticContext
        {
            CompatibilityMode = XPathCompatibilityMode.BackwardsCompatible,
            Variables = [empty, many]
        };
        XPathCompiledExpression compiled = new XPathCompiler().CompileXPath("$many + 1", context);
        XPathPlan plan = (XPathPlan)compiled.Plan!;

        XdmAtomicValue emptyResult = Evaluate("$empty + 1", context, new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue firstResult = Evaluate("$many + 1", context, new XPathDynamicContext()).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(emptyResult.FloatingValue), Is.True);
            Assert.That(firstResult.FloatingValue, Is.EqualTo(4));
            Assert.That(plan.Root.StaticType.AtomicType, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(plan.Root.StaticType.Cardinality, Is.EqualTo(XPathStaticCardinality.ExactlyOne));
        });
    }

    [Test]
    public void ConstructsIncreasingIntegerRangesAndHandlesEmptyBounds()
    {
        XPathVariableBinding untyped = Binding("untyped", Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("3")), XdmAtomicType.UntypedAtomic, XdmOccurrence.ExactlyOne);
        XPathVariableBinding empty = Binding("empty", Sequence(), XdmAtomicType.Integer, XdmOccurrence.ZeroOrOne);

        Assert.Multiple(() =>
        {
            Assert.That(Integers(Evaluate("2 to 5", [])), Is.EqualTo(new long[] { 2, 3, 4, 5 }));
            Assert.That(Integers(Evaluate("$untyped to 5", [untyped])), Is.EqualTo(new long[] { 3, 4, 5 }));
            Assert.That(Evaluate("5 to 2", []).Count, Is.EqualTo(0));
            Assert.That(Evaluate("$empty to 2", [empty]).Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void ComparesRelationalRangeEndpointsWithoutMaterializingLargeRanges()
    {
        XPathDynamicContext dynamicContext = new XPathDynamicContext { MaximumOperations = 100 };

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("-1000000000000000000000 < -1000000000000000500003 to -1000000000000000000000", [], dynamicContext)), Is.False);
            Assert.That(Boolean(Evaluate("-1000000000000000500003 to -1000000000000000000000 < -1000000000000000000002", [], dynamicContext)), Is.True);
        });
    }

    [Test]
    public void ScansLargeGeneralComparisonOperandsWithoutRepeatedValidation()
    {
        XdmSequence result = Evaluate("(1 to 10000) = 10000", []);

        Assert.That(Boolean(result), Is.True);
    }

    [Test]
    public void EnforcesRangeTypesCardinalityAndMaterializationLimit()
    {
        XPathVariableBinding decimalValue = Binding("decimalValue", Sequence(Decimal("1")), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne);
        XPathVariableBinding many = Binding("many", Sequence(Integer("1"), Integer("2")), XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore);

        XPathDynamicException type = Assert.Throws<XPathDynamicException>(() => Evaluate("$decimalValue to 3", [decimalValue]))!;
        XPathDynamicException cardinality = Assert.Throws<XPathDynamicException>(() => Evaluate("$many to 3", [many]))!;
        XmlLimitException limit = Assert.Throws<XmlLimitException>(() => Evaluate("1 to 3", new XPathStaticContext(), new XPathDynamicContext
        {
            SecurityLimits = new XmlSecurityLimits { MaximumNodeCount = 2 }
        }))!;

        Assert.Multiple(() =>
        {
            Assert.That(type.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(limit.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        });
    }

    [Test]
    public void ConcatenatesAtomicAndAtomizedNodeValues()
    {
        XmlDocument document = Parse("<root><value>middle</value></root>");
        XPathVariableBinding prefix = Binding("prefix", Sequence(String("start-")), XdmAtomicType.String, XdmOccurrence.ExactlyOne);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [prefix] };

        XdmSequence result = Evaluate("$prefix || /root/value || 7", staticContext, Context(document.DocumentNode!));
        XdmSequence empty = Evaluate("() || 'suffix'", [], new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("start-middle7"));
            Assert.That(empty.AtomicValues![0].StringValue, Is.EqualTo("suffix"));
        });
    }

    [Test]
    public void RejectsMultipleStringConcatenationItems()
    {
        XPathVariableBinding many = Binding("many", Sequence(String("a"), String("b")), XdmAtomicType.String, XdmOccurrence.ZeroOrMore);

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("$many || 'c'", [many]))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void CombinesNodeSequencesByIdentityInDocumentOrder()
    {
        XmlDocument document = Parse("<root><a/><b/><c/></root>");
        XPathDynamicContext context = Context(document.DocumentNode!);

        XdmSequence union = Evaluate("/root/b | /root/a | /root/b", [], context);
        XdmSequence intersect = Evaluate("/root/* intersect (/root/b, /root/c)", [], context);
        XdmSequence except = Evaluate("/root/* except /root/b", [], context);

        Assert.Multiple(() =>
        {
            Assert.That(Names(union), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Names(intersect), Is.EqualTo(new[] { "b", "c" }));
            Assert.That(Names(except), Is.EqualTo(new[] { "a", "c" }));
        });
    }

    [Test]
    public void RejectsNonNodeSetOperandsAtRuntime()
    {
        XPathDynamicContext context = new XPathDynamicContext { ContextItem = Sequence(Integer("1")) };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate(". union ()", new XPathStaticContext(), context))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void LogicalOperatorsShortCircuitAndUseEffectiveBooleanValues()
    {
        XmlDocument document = Parse("<root><item/></root>");
        XPathDynamicContext context = Context(document.DocumentNode!);

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("false() and (1 idiv 0 eq 0)", [])), Is.False);
            Assert.That(Boolean(Evaluate("true() or (1 idiv 0 eq 0)", [])), Is.True);
            Assert.That(Boolean(Evaluate("/root/item and 'value'", [], context)), Is.True);
            Assert.That(Boolean(Evaluate("() or 0", [])), Is.False);
        });
    }

    [Test]
    public void ReportsRequiredLogicalErrorsAndInvalidEffectiveBooleanValues()
    {
        XPathVariableBinding many = Binding("many", Sequence(Integer("1"), Integer("2")), XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);

        XPathDynamicException required = Assert.Throws<XPathDynamicException>(() => Evaluate("true() and (1 idiv 0 eq 0)", []))!;
        XPathDynamicException effectiveBoolean = Assert.Throws<XPathDynamicException>(() => Evaluate("$many and true()", [many]))!;

        Assert.Multiple(() =>
        {
            Assert.That(required.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(effectiveBoolean.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(effectiveBoolean.Diagnostic.Range, Is.Not.Null);
        });
    }

    private static XdmSequence Evaluate(string source, XPathVariableBinding[] variables)
    {
        return Evaluate(source, new XPathStaticContext { Variables = variables }, new XPathDynamicContext());
    }

    private static XdmSequence Evaluate(string source, XPathVariableBinding[] variables, XPathDynamicContext dynamicContext)
    {
        return Evaluate(source, new XPathStaticContext { Variables = variables }, dynamicContext);
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathVariableBinding Binding(string name, XdmSequence value, XdmAtomicType type, XdmOccurrence occurrence)
    {
        return new XPathVariableBinding
        {
            Name = new XmlName { LocalName = name, NamespaceUri = "", QualifiedName = name },
            Value = value,
            DeclaredType = type,
            DeclaredOccurrence = occurrence
        };
    }

    private static XdmSequence Sequence(params XdmAtomicValue[] values)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < values.Length)
        {
            builder.AddXdmAtomicValue(values[index]);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static XdmAtomicValue Integer(string value)
    {
        return new XdmFactory().CreateXdmIntegerAtomicValue(value, XdmAtomicType.Integer);
    }

    private static XdmAtomicValue Decimal(string value)
    {
        return new XdmFactory().CreateXdmDecimalAtomicValue(value);
    }

    private static XdmAtomicValue Float(string value)
    {
        return new XdmFactory().CreateXdmFloatingAtomicValue(value, XdmAtomicType.Float);
    }

    private static XdmAtomicValue Double(string value)
    {
        return new XdmFactory().CreateXdmFloatingAtomicValue(value, XdmAtomicType.Double);
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
    }

    private static bool Boolean(XdmSequence value)
    {
        return value.AtomicValues![0].BooleanValue;
    }

    private static long[] Integers(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.IntegerValue).ToArray();
    }

    private static string[] Names(XdmSequence value)
    {
        return value.Nodes!.Select(item => item.Name!.LocalName!).ToArray();
    }

    private static void AssertAtomic(XdmSequence value, XdmAtomicType type, string canonical)
    {
        Assert.That(value.Count, Is.EqualTo(1));
        Assert.That(value.AtomicValues![0].Type, Is.EqualTo(type));
        Assert.That(value.AtomicValues[0].CanonicalValue, Is.EqualTo(canonical));
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
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }
}
