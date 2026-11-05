using xml;

namespace xml.Tests;

[TestFixture]
public class XPathNumericFunctionTests
{
    [Test]
    public void MinAndMaxOrderDateValuesUsingTheImplicitTimezone()
    {
        XdmSequence result = Evaluate("(min((xs:date('2005-01-01'), xs:date('2001-01-01'))), max((xs:date('2005-01-01'), xs:date('2001-01-01'))))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "2001-01-01", "2005-01-01" }));
    }

    [Test]
    public void NumberConvertsSupportedValuesAndReturnsNanForOtherInputs()
    {
        XdmSequence result = Evaluate("(number(true()), number('12.5'), number('invalid'), number(()), number(xs:date('2026-01-02')), number(xs:float('3.4028235E38')))", new XPathStaticContext(), new XPathDynamicContext());
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(item => item.Type).ToArray(), Is.EqualTo(Enumerable.Repeat(XdmAtomicType.Double, 6)));
            Assert.That(values[0].FloatingValue, Is.EqualTo(1));
            Assert.That(values[1].FloatingValue, Is.EqualTo(12.5));
            Assert.That(double.IsNaN(values[2].FloatingValue), Is.True);
            Assert.That(double.IsNaN(values[3].FloatingValue), Is.True);
            Assert.That(double.IsNaN(values[4].FloatingValue), Is.True);
            Assert.That(values[5].FloatingValue, Is.EqualTo((double)float.MaxValue));
        });
    }

    [Test]
    public void NumberUsesTheContextItemAndReportsAnAbsentContext()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:number-function" };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = Sequence(new XdmFactory().CreateXdmStringAtomicValue("7.25", XdmAtomicType.String))
        };

        XdmAtomicValue value = Evaluate("number()", context, dynamicContext).AtomicValues![0];
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("number()", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(value.FloatingValue, Is.EqualTo(7.25));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(error.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:number-function"));
        });
    }

    [Test]
    public void AppliesNumericFunctionsWithXPathRoundingAndSpecialValueSemantics()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(abs(xs:double('-0')), ceiling(xs:float('-0.1')), floor(2.9), round(-2.5), " +
            "round-half-to-even(2.5), round(12.345, 2), round-half-to-even(12.345, 2), " +
            "(fn:round#2)(125, -1), round(xs:double('-1'), -99999999999999), " +
            "round(xs:double('INF'), 2), round-half-to-even(xs:double('NaN'), 2))";

        XdmAtomicValue[] values = Evaluate(source, context, new XPathDynamicContext()).AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(item => item.CanonicalValue).ToArray(), Is.EqualTo(new[]
            {
                "0.0E0",
                "-0.0E0",
                "2",
                "-2",
                "2",
                "12.35",
                "12.34",
                "130",
                "-0.0E0",
                "INF",
                "NaN"
            }));
            Assert.That(values[0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(values[1].Type, Is.EqualTo(XdmAtomicType.Float));
            Assert.That(values[2].Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(BitConverter.DoubleToInt64Bits(values[8].FloatingValue), Is.EqualTo(long.MinValue));
            Assert.That(double.IsPositiveInfinity(values[9].FloatingValue), Is.True);
            Assert.That(double.IsNaN(values[10].FloatingValue), Is.True);
        });
    }

    [Test]
    public void NumericFunctionsAtomizeNodesAndConvertUntypedValuesToDouble()
    {
        XPathDynamicContext context = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(Parse("<root><value>-2.5</value></root>").DocumentNode!)
        };

        XdmSequence empty = Evaluate("abs(())", new XPathStaticContext(), context);
        XdmAtomicValue value = Evaluate("abs(/root/value)", new XPathStaticContext(), context).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(empty.Count, Is.Zero);
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(value.FloatingValue, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void ComputesNumericAndDurationAggregatesWithPromotion()
    {
        XdmAtomicValue sumEmpty = Evaluate("sum(())", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue sumPromoted = Evaluate("sum((1, 2.5, xs:float('3')))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue arraySum = Evaluate("sum([1, 2, 3])", new XPathStaticContext { AllowHigherOrderFunctions = true }, new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue average = Evaluate("avg((3, 4, 5))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue minimum = Evaluate("min((xs:float('3'), xs:double('-INF'), 2), 'ignored')", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue maximum = Evaluate("max((xs:float('3'), xs:double('INF'), 2))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue integerSubtype = Evaluate("max((xs:byte(5), xs:int(4)))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue booleanMinimum = Evaluate("min((true(), false(), true()))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue booleanMaximum = Evaluate("max((false(), true(), false()))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue nan = Evaluate("min((xs:float('NaN'), xs:double(1)))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue monthSum = Evaluate("sum((xs:yearMonthDuration('P1Y'), xs:yearMonthDuration('P6M')))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];
        XdmAtomicValue dayAverage = Evaluate("avg((xs:dayTimeDuration('P1D'), xs:dayTimeDuration('P3D')))", new XPathStaticContext(), new XPathDynamicContext()).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(sumEmpty.Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(sumEmpty.CanonicalValue, Is.EqualTo("0"));
            Assert.That(sumPromoted.Type, Is.EqualTo(XdmAtomicType.Float));
            Assert.That(sumPromoted.FloatingValue, Is.EqualTo(6.5f));
            Assert.That(arraySum.IntegerValue, Is.EqualTo(6));
            Assert.That(average.Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(average.CanonicalValue, Is.EqualTo("4"));
            Assert.That(minimum.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(double.IsNegativeInfinity(minimum.FloatingValue), Is.True);
            Assert.That(maximum.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(double.IsPositiveInfinity(maximum.FloatingValue), Is.True);
            Assert.That(integerSubtype.Type, Is.EqualTo(XdmAtomicType.Byte));
            Assert.That(integerSubtype.IntegerValue, Is.EqualTo(5));
            Assert.That(booleanMinimum.BooleanValue, Is.False);
            Assert.That(booleanMaximum.BooleanValue, Is.True);
            Assert.That(nan.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(double.IsNaN(nan.FloatingValue), Is.True);
            Assert.That(monthSum.Type, Is.EqualTo(XdmAtomicType.YearMonthDuration));
            Assert.That(monthSum.CanonicalValue, Is.EqualTo("P1Y6M"));
            Assert.That(dayAverage.Type, Is.EqualTo(XdmAtomicType.DayTimeDuration));
            Assert.That(dayAverage.CanonicalValue, Is.EqualTo("P2D"));
        });
    }

    [Test]
    public void MinAndMaxOrderBinaryValuesOfTheSameType()
    {
        XdmSequence result = Evaluate("(min((xs:base64Binary('qg=='), xs:base64Binary('uw=='), xs:base64Binary('iA=='))), max((xs:base64Binary('qg=='), xs:base64Binary('uw=='), xs:base64Binary('iA=='))), min((xs:hexBinary('AA'), xs:hexBinary('BB'), xs:hexBinary('88'))), max((xs:hexBinary('AA'), xs:hexBinary('BB'), xs:hexBinary('88'))))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.Type).ToArray(), Is.EqualTo(new[] { XdmAtomicType.Base64Binary, XdmAtomicType.Base64Binary, XdmAtomicType.HexBinary, XdmAtomicType.HexBinary }));
            Assert.That(result.AtomicValues!.Select(value => value.CanonicalValue).ToArray(), Is.EqualTo(new[] { "iA==", "uw==", "88", "BB" }));
        });
    }

    [Test]
    public void SumUsesTheSuppliedFallbackAndAverageOfEmptyIsEmpty()
    {
        XdmSequence negativeZero = Evaluate("sum((), xs:double('-0'))", new XPathStaticContext(), new XPathDynamicContext());
        XdmSequence emptyFallback = Evaluate("sum((), ())", new XPathStaticContext(), new XPathDynamicContext());
        XdmSequence emptyAverage = Evaluate("avg(())", new XPathStaticContext(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(negativeZero.AtomicValues![0].CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(BitConverter.DoubleToInt64Bits(negativeZero.AtomicValues[0].FloatingValue), Is.EqualTo(long.MinValue));
            Assert.That(emptyFallback.Count, Is.Zero);
            Assert.That(emptyAverage.Count, Is.Zero);
        });
    }

    [Test]
    public void ReportsNumericFunctionTypeConversionAndArityErrors()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:numeric-functions" };
        XPathDynamicContext invalidUntyped = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(Parse("<root><value>invalid</value></root>").DocumentNode!)
        };

        XPathDynamicException wrongNumericType = Assert.Throws<XPathDynamicException>(() => Evaluate("abs('2')", context, new XPathDynamicContext()))!;
        XPathDynamicException wrongPrecision = Assert.Throws<XPathDynamicException>(() => Evaluate("round(2, 1.5)", context, new XPathDynamicContext()))!;
        XPathDynamicException emptyWrongPrecision = Assert.Throws<XPathDynamicException>(() => Evaluate("round((), 1.5)", context, new XPathDynamicContext()))!;
        XPathDynamicException incompatibleAggregate = Assert.Throws<XPathDynamicException>(() => Evaluate("sum((1, xs:dayTimeDuration('P1D')))", context, new XPathDynamicContext()))!;
        XPathDynamicException ignoredWrongZero = Assert.Throws<XPathDynamicException>(() => Evaluate("sum(1, (2, 3))", context, new XPathDynamicContext()))!;
        XPathDynamicException wrongAggregateType = Assert.Throws<XPathDynamicException>(() => Evaluate("max((xs:QName('name')))", context, new XPathDynamicContext()))!;
        XPathDynamicException emptyWrongCollation = Assert.Throws<XPathDynamicException>(() => Evaluate("min((), (1, 2))", context, new XPathDynamicContext()))!;
        XPathDynamicException invalidLexical = Assert.Throws<XPathDynamicException>(() => Evaluate("sum(/root/value)", context, invalidUntyped))!;
        XPathStaticException wrongArity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("sum(1, 2, 3)", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(wrongNumericType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(wrongPrecision.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(emptyWrongPrecision.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(incompatibleAggregate.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(ignoredWrongZero.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(wrongAggregateType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(emptyWrongCollation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(invalidLexical.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(invalidLexical.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:numeric-functions"));
            Assert.That(wrongArity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XdmSequence Sequence(XdmAtomicValue value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(value);
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
