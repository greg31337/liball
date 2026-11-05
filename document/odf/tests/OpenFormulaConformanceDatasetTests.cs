namespace odf.Tests;

public class OpenFormulaConformanceDatasetTests
{
    [TestCase("=ACOS(1)", 0.0)]
    [TestCase("=ACOSH(1)", 0.0)]
    [TestCase("=ACOT(1)", 0.7853981633974483)]
    [TestCase("=ACOTH(2)", 0.5493061443340549)]
    [TestCase("=ASIN(1)", 1.5707963267948966)]
    [TestCase("=ASINH(0)", 0.0)]
    [TestCase("=ATAN(1)", 0.7853981633974483)]
    [TestCase("=ATAN2(1;1)", 0.7853981633974483)]
    [TestCase("=ATANH(0.5)", 0.5493061443340549)]
    [TestCase("=BESSELI(0;0)", 1.0)]
    [TestCase("=BESSELK(1;0)", 0.4210244382407083)]
    [TestCase("=BESSELY(1;0)", 0.08825696421567696)]
    [TestCase("=CHISQINV(0.5;2)", 1.3862943611198906)]
    [TestCase("=COS(0)", 1.0)]
    [TestCase("=COSH(0)", 1.0)]
    [TestCase("=COT(0.7853981633974483)", 1.0)]
    [TestCase("=COTH(0.5493061443340549)", 2.0)]
    [TestCase("=CSC(1.5707963267948966)", 1.0)]
    [TestCase("=CSCH(0.881373587019543)", 1.0)]
    [TestCase("=DELTA(5;5)", 1.0)]
    [TestCase("=ERFC(0)", 1.0)]
    [TestCase("=EVEN(3)", 4.0)]
    [TestCase("=EXP(1)", 2.718281828459045)]
    [TestCase("=GAMMALN(5)", 3.1780538303479458)]
    [TestCase("=GESTEP(5;4)", 1.0)]
    [TestCase("=HEX2DEC(\"A\")", 10.0)]
    [TestCase("=IMARGUMENT(\"1+i\")", 0.7853981633974483)]
    [TestCase("=LN(2.718281828459045)", 1.0)]
    [TestCase("=LOG(8;2)", 3.0)]
    [TestCase("=LOG10(100)", 2.0)]
    [TestCase("=MAXA(1;TRUE();3)", 3.0)]
    [TestCase("=MINA(1;FALSE();3)", 0.0)]
    [TestCase("=MOD(-3;2)", 1.0)]
    [TestCase("=MULTINOMIAL(2;3;4)", 1260.0)]
    [TestCase("=OCT2DEC(\"12\")", 10.0)]
    [TestCase("=PI()", 3.141592653589793)]
    [TestCase("=POWER(2;10)", 1024.0)]
    [TestCase("=PRODUCT(2;3;4)", 24.0)]
    [TestCase("=QUOTIENT(-7;2)", -3.0)]
    [TestCase("=RADIANS(180)", 3.141592653589793)]
    [TestCase("=SEARCHB(\"world\";\"hello world\")", 7.0)]
    [TestCase("=SEC(0)", 1.0)]
    [TestCase("=SECH(0)", 1.0)]
    [TestCase("=SERIESSUM(2;0;1;{1;2;3})", 17.0)]
    [TestCase("=SIGN(-5)", -1.0)]
    [TestCase("=SINH(0)", 0.0)]
    [TestCase("=SQRTPI(4)", 3.544907701811032)]
    [TestCase("=STDEVA(1;TRUE();3)", 1.1547005383792515)]
    [TestCase("=STDEVP(1;2;3)", 0.816496580927726)]
    [TestCase("=STDEVPA(1;TRUE();3)", 0.9428090415820634)]
    [TestCase("=SUMSQ(2;3)", 13.0)]
    [TestCase("=SUMX2MY2({3;4};{1;2})", 20.0)]
    [TestCase("=SUMX2PY2({3;4};{1;2})", 30.0)]
    [TestCase("=TAN(0.7853981633974483)", 1.0)]
    [TestCase("=TANH(0)", 0.0)]
    [TestCase("=VARA(1;TRUE();3)", 1.3333333333333333)]
    [TestCase("=VARPA(1;TRUE();3)", 0.8888888888888888)]
    public void EvaluatesIndependentNumericExamples(
        string source,
        double expected)
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormula formula = parser.ParseFormula(source, 64);

        OdfFormulaResult result = evaluator.EvaluateFormula(
            formula,
            new OdfFormulaEvaluationData());

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True, source);
            Assert.That(formula.Root!.Function, Is.Not.Null, source);
            Assert.That(result.Error, Is.EqualTo(OdfFormulaError.None), source);
            Assert.That(result.NumericValue,
                Is.EqualTo(expected).Within(0.000001), source);
        });
    }

    [TestCase("=BIN2HEX(\"1010\")", "A")]
    [TestCase("=BIN2OCT(\"1010\")", "12")]
    [TestCase("=DEC2BIN(10)", "1010")]
    [TestCase("=DEC2OCT(10)", "12")]
    [TestCase("=HEX2BIN(\"A\")", "1010")]
    [TestCase("=HEX2OCT(\"A\")", "12")]
    [TestCase("=IMSUB(\"3+4i\";\"1+2i\")", "2+2i")]
    [TestCase("=OCT2BIN(\"12\")", "1010")]
    [TestCase("=OCT2HEX(\"12\")", "A")]
    public void EvaluatesIndependentTextAndComplexExamples(
        string source,
        string expected)
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormula formula = parser.ParseFormula(source, 64);

        OdfFormulaResult result = evaluator.EvaluateFormula(
            formula,
            new OdfFormulaEvaluationData());

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True, source);
            Assert.That(formula.Root!.Function, Is.Not.Null, source);
            Assert.That(result.Error, Is.EqualTo(OdfFormulaError.None), source);
            Assert.That(result.StringValue, Is.EqualTo(expected), source);
        });
    }

    [TestCase("=ABS(-1)", OdfFormulaConformance.Small)]
    [TestCase("=ACOSH(1)", OdfFormulaConformance.Medium)]
    [TestCase("=DELTA(1;1)", OdfFormulaConformance.Large)]
    [TestCase("=DOLLAR(1)", OdfFormulaConformance.Unknown)]
    public void PublishesEveryConformanceLevel(
        string source,
        OdfFormulaConformance expected)
    {
        IOdfFormulaParser parser = new OdfFormulaParser();

        OdfFormula formula = parser.ParseFormula(source, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formula.Root!.Function, Is.Not.Null);
            Assert.That(formula.Root.Function!.Conformance,
                Is.EqualTo(expected));
        });
    }
}
