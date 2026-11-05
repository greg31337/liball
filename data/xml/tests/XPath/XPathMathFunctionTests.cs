using xml;

namespace xml.Tests;

[TestFixture]
public class XPathMathFunctionTests
{
    [Test]
    public void EvaluatesTheMathNamespaceFunctionSet()
    {
        XdmSequence result = Evaluate("(math:pi(), math:exp(1), math:exp10(3), math:log(math:exp(2)), math:log10(1000), math:sqrt(81), math:sin(math:pi() div 2), math:cos(0), math:tan(math:pi() div 4), math:asin(1), math:acos(-1), math:atan(1), math:atan2(1, 0))");
        double[] values = FloatingValues(result);

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo(Math.PI));
            Assert.That(values[1], Is.EqualTo(Math.E).Within(1e-15));
            Assert.That(values[2], Is.EqualTo(1000d));
            Assert.That(values[3], Is.EqualTo(2d).Within(1e-15));
            Assert.That(values[4], Is.EqualTo(3d));
            Assert.That(values[5], Is.EqualTo(9d));
            Assert.That(values[6], Is.EqualTo(1d).Within(1e-15));
            Assert.That(values[7], Is.EqualTo(1d));
            Assert.That(values[8], Is.EqualTo(1d).Within(1e-15));
            Assert.That(values[9], Is.EqualTo(Math.PI / 2d));
            Assert.That(values[10], Is.EqualTo(Math.PI));
            Assert.That(values[11], Is.EqualTo(Math.PI / 4d));
            Assert.That(values[12], Is.EqualTo(Math.PI / 2d));
        });
    }

    [Test]
    public void PropagatesEmptyArgumentsForOptionalMathFunctions()
    {
        XdmSequence result = Evaluate("(math:exp(()), math:log(()), math:sqrt(()), math:sin(()), math:pow((), 3), math:pi())");

        Assert.That(FloatingValues(result), Is.EqualTo(new[] { Math.PI }));
    }

    [Test]
    public void HandlesUnarySpecialValuesWithoutRaisingErrors()
    {
        double[] values = FloatingValues(Evaluate("(math:exp(xs:double('-INF')), math:log(0), math:log(-1), math:sqrt(-1), math:sqrt(-0.0e0), math:sin(xs:double('INF')), math:asin(2), math:atan(xs:double('-INF')))"));

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo(0d));
            Assert.That(values[1], Is.EqualTo(double.NegativeInfinity));
            Assert.That(double.IsNaN(values[2]), Is.True);
            Assert.That(double.IsNaN(values[3]), Is.True);
            Assert.That(BitConverter.DoubleToInt64Bits(values[4]), Is.LessThan(0));
            Assert.That(double.IsNaN(values[5]), Is.True);
            Assert.That(double.IsNaN(values[6]), Is.True);
            Assert.That(values[7], Is.EqualTo(-Math.PI / 2d));
        });
    }

    [Test]
    public void AppliesIntegerPowerRulesIncludingUnboundedExponents()
    {
        double[] values = FloatingValues(Evaluate("(math:pow(-2, 3), math:pow(-2, -3), math:pow(xs:double('NaN'), 0), math:pow(-0.0e0, 3), math:pow(-0.0e0, -3), math:pow(-1, 999999999999999999999999999999999999999), math:pow(2, 999999999999999999999999999999999999999), math:pow(2, -999999999999999999999999999999999999999))"));

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo(-8d));
            Assert.That(values[1], Is.EqualTo(-0.125d));
            Assert.That(values[2], Is.EqualTo(1d));
            Assert.That(values[3], Is.EqualTo(0d));
            Assert.That(BitConverter.DoubleToInt64Bits(values[3]), Is.LessThan(0));
            Assert.That(values[4], Is.EqualTo(double.NegativeInfinity));
            Assert.That(values[5], Is.EqualTo(-1d));
            Assert.That(values[6], Is.EqualTo(double.PositiveInfinity));
            Assert.That(values[7], Is.EqualTo(0d));
        });
    }

    [Test]
    public void AppliesFloatingPowerAndAtan2SignedZeroRules()
    {
        double[] powers = FloatingValues(Evaluate("(math:pow(1, xs:double('NaN')), math:pow(-1, xs:double('INF')), math:pow(-2.5e0, 2.0e0), math:pow(-2.5e0, 2.00000001e0))"));
        double[] angles = FloatingValues(Evaluate("(math:atan2(0.0e0, -0.0e0), math:atan2(-0.0e0, -0.0e0), math:atan2(-0.0e0, 1), math:atan2(0.0e0, 1))"));

        Assert.Multiple(() =>
        {
            Assert.That(powers[0], Is.EqualTo(1d));
            Assert.That(powers[1], Is.EqualTo(1d));
            Assert.That(powers[2], Is.EqualTo(6.25d));
            Assert.That(double.IsNaN(powers[3]), Is.True);
            Assert.That(angles[0], Is.EqualTo(Math.PI));
            Assert.That(angles[1], Is.EqualTo(-Math.PI));
            Assert.That(BitConverter.DoubleToInt64Bits(angles[2]), Is.LessThan(0));
            Assert.That(BitConverter.DoubleToInt64Bits(angles[3]), Is.EqualTo(0));
        });
    }

    [Test]
    public void ConvertsNumericArgumentsToDoubleAndReportsTypeErrors()
    {
        XdmSequence converted = Evaluate("(math:sqrt(xs:decimal('2.25')), math:atan2(xs:float('1'), 1))");
        XPathTypeException type = Assert.Throws<XPathTypeException>(() => Evaluate("math:sin('not numeric')"))!;
        XPathDynamicException missing = Assert.Throws<XPathDynamicException>(() => Evaluate("math:pow(2, ())"))!;

        Assert.Multiple(() =>
        {
            Assert.That(FloatingValues(converted), Is.EqualTo(new[] { 1.5d, Math.PI / 4d }));
            Assert.That(type.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [TestCase("math:pi(1)")]
    [TestCase("math:pow(2)")]
    [TestCase("math:atan2(1)")]
    [TestCase("math:unknown(1)")]
    public void RejectsUnknownMathFunctionsAndUnsupportedArities(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());
        return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
    }

    private static double[] FloatingValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.FloatingValue).ToArray();
    }
}
