namespace odf.Tests;

public class FormulaCompatibilityFunctionTests
{
    [Test]
    public void PublishesRoundingLegacyVolatileAndVersionMetadata()
    {
        string[] volatileNames =
        [
            OdfFormulaNames.DDE,
            OdfFormulaNames.EASTERSUNDAY,
            OdfFormulaNames.NOW,
            OdfFormulaNames.RAND,
            OdfFormulaNames.RANDBETWEEN,
            OdfFormulaNames.TODAY
        ];

        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions,
                Has.Length.EqualTo(393));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Rounding"), Is.EqualTo(8));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Legacy"), Is.EqualTo(8));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.INT)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.CEILING)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(FormulaFunctionCatalog.Find(
                OdfFormulaNames.LEGACY_TDIST)!.Conformance,
                Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(FormulaFunctionCatalog.Find(
                OdfFormulaNames.EASTERSUNDAY)!.IntroducedVersion,
                Is.EqualTo(OdfVersion.Version14));
            Assert.That(volatileNames.All(name =>
                FormulaFunctionCatalog.Find(name)!.IsVolatile), Is.True);
        });
    }

    [Test]
    public void EvaluatesCeilingFloorAndIntegerRoundingModes()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.CEILING,
                [Number(-4.3)], context).NumericValue, Is.EqualTo(-4));
            Assert.That(Evaluate(OdfFormulaNames.CEILING,
                [Number(-4.3), Number(-2), Number(1)], context).NumericValue,
                Is.EqualTo(-6));
            Assert.That(Evaluate(OdfFormulaNames.FLOOR,
                [Number(-4.3)], context).NumericValue, Is.EqualTo(-5));
            Assert.That(Evaluate(OdfFormulaNames.FLOOR,
                [Number(-4.3), Number(-2), Number(1)], context).NumericValue,
                Is.EqualTo(-4));
            Assert.That(Evaluate(OdfFormulaNames.CEILING,
                [Number(4.3), FormulaValueModel.CreateEmpty()], context)
                .NumericValue, Is.EqualTo(5));
            Assert.That(Evaluate(OdfFormulaNames.CEILING,
                [Number(-4), Number(2)], context).Error,
                Is.EqualTo(OdfFormulaError.Number));
            Assert.That(Evaluate(OdfFormulaNames.FLOOR,
                [Number(4), Number(0)], context).NumericValue, Is.Zero);
            Assert.That(Evaluate(OdfFormulaNames.INT,
                [Number(-3.2)], context).NumericValue, Is.EqualTo(-4));
        });
    }

    [Test]
    public void EvaluatesDecimalAndMultipleRoundingTieRules()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.MROUND,
                [Number(5), Number(2)], context).NumericValue, Is.EqualTo(6));
            Assert.That(Evaluate(OdfFormulaNames.MROUND,
                [Number(-5), Number(2)], context).NumericValue, Is.EqualTo(-4));
            Assert.That(Evaluate(OdfFormulaNames.ROUND,
                [Number(2.5)], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.ROUND,
                [Number(-2.5)], context).NumericValue, Is.EqualTo(-3));
            Assert.That(Evaluate(OdfFormulaNames.ROUND,
                [Number(1234.5), Number(-2)], context).NumericValue,
                Is.EqualTo(1200));
            Assert.That(Evaluate(OdfFormulaNames.ROUNDDOWN,
                [Number(-12.39), Number(1)], context).NumericValue,
                Is.EqualTo(-12.3).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.ROUNDUP,
                [Number(-12.31), Number(1)], context).NumericValue,
                Is.EqualTo(-12.4).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.TRUNC,
                [Number(1299), Number(-2)], context).NumericValue,
                Is.EqualTo(1200));
        });
    }

    [Test]
    public void EvaluatesLegacyRightTailDistributionsAndInverses()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        double chiTail = global::System.Math.Exp(-1);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHIDIST,
                [Number(2), Number(2)], context).NumericValue,
                Is.EqualTo(chiTail).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHIDIST,
                [Number(0), Number(2)], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHIINV,
                [Number(chiTail), Number(2)], context).NumericValue,
                Is.EqualTo(2).Within(1E-9));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_FDIST,
                [Number(1), Number(5), Number(5)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_FINV,
                [Number(0.5), Number(5), Number(5)], context).NumericValue,
                Is.EqualTo(1).Within(1E-9));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_NORMSDIST,
                [Number(0)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_NORMSINV,
                [Number(0.5)], context).NumericValue, Is.Zero.Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_TDIST,
                [Number(0), Number(10), Number(1)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_TDIST,
                [Number(0), Number(10), Number(2)], context).NumericValue,
                Is.EqualTo(1).Within(1E-12));
        });
    }

    [Test]
    public void EvaluatesLegacyChiTestShapesAndIgnoredValues()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult actual = Matrix(1, 2, Number(10), Number(20));
        OdfFormulaResult expected = Matrix(1, 2, Number(15), Number(15));
        OdfFormulaResult mixedActual = Matrix(
            1,
            3,
            Number(10),
            FormulaValueModel.CreateText("ignored"),
            Number(20));
        OdfFormulaResult mixedExpected = Matrix(
            1,
            3,
            Number(15),
            Number(999),
            Number(15));

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHITEST,
                [actual, expected], context).NumericValue,
                Is.EqualTo(0.06788915).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHITEST,
                [mixedActual, mixedExpected], context).NumericValue,
                Is.EqualTo(global::System.Math.Exp(-5.0 / 3)).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHITEST,
                [actual, Matrix(1, 2, Number(0), Number(15))], context).Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(Evaluate(OdfFormulaNames.LEGACY_CHITEST,
                [actual, Matrix(2, 1, Number(15), Number(15))], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void ResolvesDottedCompatibilityNamesAndRejectsUnknownFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormula parsed = FormulaParser.Parse("=LEGACY.NORMSDIST(0)", 32);
        OdfFormulaResult dotted = FormulaFunctionEvaluator.Evaluate(
            "LEGACY.NORMSDIST",
            [Number(0)],
            context);
        OdfFormulaResult unknown = FormulaFunctionEvaluator.Evaluate(
            "VENDOR.MISSING",
            [],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Root!.Function, Is.Not.Null);
            Assert.That(parsed.Root.Function!.Name,
                Is.EqualTo(OdfFormulaNames.LEGACY_NORMSDIST));
            Assert.That(dotted.NumericValue, Is.EqualTo(0.5).Within(1E-7));
            Assert.That(unknown.Error,
                Is.EqualTo(OdfFormulaError.UnsupportedFunction));
        });
    }

    static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, arguments, context);
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult Matrix(
        int rows,
        int columns,
        params OdfFormulaResult[] values)
    {
        return FormulaValueModel.CreateArray(values, rows, columns);
    }
}
