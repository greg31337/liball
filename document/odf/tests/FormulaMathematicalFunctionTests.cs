namespace odf.Tests;

public class FormulaMathematicalFunctionTests
{
    [Test]
    public void PublishesCompleteT112FunctionMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions, Has.Length.EqualTo(393));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Bit"), Is.EqualTo(5));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Complex"), Is.EqualTo(26));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Mathematical"), Is.EqualTo(69));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Conversion"), Is.EqualTo(16));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.ABS)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.CONVERT)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.BITAND)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.RAND)!
                .IsVolatile, Is.True);
        });
    }

    [Test]
    public void EvaluatesAllBitOperationsAndTheirBounds()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.BITAND,
                [Number(13), Number(11)], context).NumericValue, Is.EqualTo(9));
            Assert.That(Evaluate(OdfFormulaNames.BITOR,
                [Number(12), Number(3)], context).NumericValue, Is.EqualTo(15));
            Assert.That(Evaluate(OdfFormulaNames.BITXOR,
                [Number(12), Number(10)], context).NumericValue, Is.EqualTo(6));
            Assert.That(Evaluate(OdfFormulaNames.BITLSHIFT,
                [Number(3), Number(4)], context).NumericValue, Is.EqualTo(48));
            Assert.That(Evaluate(OdfFormulaNames.BITRSHIFT,
                [Number(48), Number(4)], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.BITAND,
                [Number(-1), Number(1)], context).Error,
                Is.EqualTo(OdfFormulaError.Number));
            Assert.That(Evaluate(OdfFormulaNames.BITLSHIFT,
                [Number(281474976710655), Number(1)], context).Error,
                Is.EqualTo(OdfFormulaError.Number));
        });
    }

    [Test]
    public void EvaluatesComplexConstructionArithmeticAndComponents()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult complex = Evaluate(OdfFormulaNames.COMPLEX,
            [Number(3), Number(4)], context);
        OdfFormulaResult conjugate = Evaluate(OdfFormulaNames.IMCONJUGATE,
            [complex], context);
        OdfFormulaResult sum = Evaluate(OdfFormulaNames.IMSUM,
            [complex, Text("1-2i")], context);
        OdfFormulaResult product = Evaluate(OdfFormulaNames.IMPRODUCT,
            [Text("1+i"), Text("1-i")], context);
        OdfFormulaResult quotient = Evaluate(OdfFormulaNames.IMDIV,
            [Text("3+4i"), Text("1+i")], context);
        OdfFormulaResult power = Evaluate(OdfFormulaNames.IMPOWER,
            [Text("i"), Number(2)], context);

        Assert.Multiple(() =>
        {
            Assert.That(complex.Kind, Is.EqualTo(OdfValueKind.Complex));
            Assert.That(complex.StringValue, Is.EqualTo("3+4i"));
            Assert.That(Evaluate(OdfFormulaNames.IMABS,
                [complex], context).NumericValue, Is.EqualTo(5));
            Assert.That(Evaluate(OdfFormulaNames.IMREAL,
                [complex], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.IMAGINARY,
                [complex], context).NumericValue, Is.EqualTo(4));
            Assert.That(conjugate.StringValue, Is.EqualTo("3-4i"));
            Assert.That(sum.StringValue, Is.EqualTo("4+2i"));
            Assert.That(product.StringValue, Is.EqualTo("2"));
            Assert.That(quotient.StringValue, Is.EqualTo("3.5+0.5i"));
            Assert.That(power.StringValue, Is.EqualTo("-1"));
        });
    }

    [Test]
    public void EvaluatesEveryComplexUnaryFamily()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        string[] names =
        [
            OdfFormulaNames.IMCOS,
            OdfFormulaNames.IMCOSH,
            OdfFormulaNames.IMCOT,
            OdfFormulaNames.IMCSC,
            OdfFormulaNames.IMCSCH,
            OdfFormulaNames.IMEXP,
            OdfFormulaNames.IMLN,
            OdfFormulaNames.IMLOG10,
            OdfFormulaNames.IMLOG2,
            OdfFormulaNames.IMSIN,
            OdfFormulaNames.IMSINH,
            OdfFormulaNames.IMSEC,
            OdfFormulaNames.IMSECH,
            OdfFormulaNames.IMSQRT,
            OdfFormulaNames.IMTAN
        ];

        foreach (string name in names)
        {
            OdfFormulaResult result = Evaluate(name, [Text("2+i")], context);
            Assert.That(result.Error, Is.EqualTo(OdfFormulaError.None), name);
            Assert.That(result.Kind, Is.EqualTo(OdfValueKind.Complex), name);
        }
    }

    [Test]
    public void EvaluatesElementaryTrigonometricAndSpecialFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.ABS,
                [Number(-4)], context).NumericValue, Is.EqualTo(4));
            Assert.That(Evaluate(OdfFormulaNames.SIN,
                [Number(global::System.Math.PI / 2)], context).NumericValue,
                Is.EqualTo(1).Within(0.000000001));
            Assert.That(Evaluate(OdfFormulaNames.DEGREES,
                [Number(global::System.Math.PI)], context).NumericValue,
                Is.EqualTo(180).Within(0.000000001));
            Assert.That(Evaluate(OdfFormulaNames.ERF,
                [Number(1)], context).NumericValue,
                Is.EqualTo(0.84270079).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.GAMMA,
                [Number(5)], context).NumericValue,
                Is.EqualTo(24).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.BESSELJ,
                [Number(0), Number(0)], context).NumericValue,
                Is.EqualTo(1).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.SQRT,
                [Number(-1)], context).Error,
                Is.EqualTo(OdfFormulaError.Number));
            Assert.That(Evaluate(OdfFormulaNames.ODD,
                [Number(1)], context).NumericValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void EvaluatesCombinatoricsAggregatesAndArraySums()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult first = Array(2, 2, 1, 2, 3, 4);
        OdfFormulaResult second = Array(2, 2, 5, 6, 7, 8);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.COMBIN,
                [Number(5), Number(2)], context).NumericValue, Is.EqualTo(10));
            Assert.That(Evaluate(OdfFormulaNames.COMBINA,
                [Number(3), Number(2)], context).NumericValue, Is.EqualTo(6));
            Assert.That(Evaluate(OdfFormulaNames.FACT,
                [Number(6)], context).NumericValue, Is.EqualTo(720));
            Assert.That(Evaluate(OdfFormulaNames.FACTDOUBLE,
                [Number(7)], context).NumericValue, Is.EqualTo(105));
            Assert.That(Evaluate(OdfFormulaNames.GCD,
                [Number(24), Number(18)], context).NumericValue, Is.EqualTo(6));
            Assert.That(Evaluate(OdfFormulaNames.LCM,
                [Number(6), Number(8)], context).NumericValue, Is.EqualTo(24));
            Assert.That(Evaluate(OdfFormulaNames.SUM,
                [first, Number(5)], context).NumericValue, Is.EqualTo(15));
            Assert.That(Evaluate(OdfFormulaNames.SUMPRODUCT,
                [first, second], context).NumericValue, Is.EqualTo(70));
            Assert.That(Evaluate(OdfFormulaNames.SUMXMY2,
                [first, second], context).NumericValue, Is.EqualTo(64));
        });
    }

    [Test]
    public void EvaluatesConditionalAndSubtotalAggregates()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult values = Reference(1, 4, 10, 20, 30, 40);
        OdfFormulaResult categories = ReferenceText(
            "west", "east", "west", "west");

        OdfFormulaResult sumIf = Evaluate(OdfFormulaNames.SUMIF,
            [categories, Text("west"), values], context);
        OdfFormulaResult sumIfs = Evaluate(OdfFormulaNames.SUMIFS,
            [values, categories, Text("west"), values, Text(">20")], context);
        OdfFormulaResult subtotal = Evaluate(OdfFormulaNames.SUBTOTAL,
            [Number(9), values], context);

        Assert.Multiple(() =>
        {
            Assert.That(sumIf.NumericValue, Is.EqualTo(80));
            Assert.That(sumIfs.NumericValue, Is.EqualTo(70));
            Assert.That(subtotal.NumericValue, Is.EqualTo(100));
        });
    }

    [Test]
    public void EvaluatesUnitCurrencyAndRepresentationConversions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.CONVERT,
                [Number(1), Text("km"), Text("m")], context).NumericValue,
                Is.EqualTo(1000));
            Assert.That(Evaluate(OdfFormulaNames.CONVERT,
                [Number(32), Text("F"), Text("C")], context).NumericValue,
                Is.EqualTo(0).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.EUROCONVERT,
                [Number(1), Text("EUR"), Text("DEM")], context).NumericValue,
                Is.EqualTo(1.96));
            Assert.That(Evaluate(OdfFormulaNames.EUROCONVERT,
                [Number(1), Text("EUR"), Text("DEM"),
                    FormulaValueModel.CreateBoolean(true)], context).NumericValue,
                Is.EqualTo(1.95583));
            Assert.That(Evaluate(OdfFormulaNames.BASE,
                [Number(31), Number(16), Number(4)], context).StringValue,
                Is.EqualTo("001F"));
            Assert.That(Evaluate(OdfFormulaNames.DECIMAL,
                [Text("FF"), Number(16)], context).NumericValue,
                Is.EqualTo(255));
            Assert.That(Evaluate(OdfFormulaNames.BIN2DEC,
                [Text("1111111111")], context).NumericValue,
                Is.EqualTo(-1));
            Assert.That(Evaluate(OdfFormulaNames.DEC2HEX,
                [Number(-1)], context).StringValue,
                Is.EqualTo("FFFFFFFFFF"));
            Assert.That(Evaluate(OdfFormulaNames.ROMAN,
                [Number(1999)], context).StringValue,
                Is.EqualTo("MCMXCIX"));
            Assert.That(Evaluate(OdfFormulaNames.ROMAN,
                [Number(499), Number(4)], context).StringValue,
                Is.EqualTo("ID"));
            Assert.That(Evaluate(OdfFormulaNames.ARABIC,
                [Text("MCMXCIX")], context).NumericValue,
                Is.EqualTo(1999));
        });
    }

    [Test]
    public void UsesCallerSuppliedRandomValueDeterministically()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.RandomValue = 0.5;

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.RAND, [], context).NumericValue,
                Is.EqualTo(0.5));
            Assert.That(Evaluate(OdfFormulaNames.RANDBETWEEN,
                [Number(10), Number(20)], context).NumericValue,
                Is.EqualTo(15));
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

    static OdfFormulaResult Text(string value)
    {
        return FormulaValueModel.CreateText(value);
    }

    static OdfFormulaResult Array(
        int rows,
        int columns,
        params double[] values)
    {
        OdfFormulaResult[] items = values.Select(Number).ToArray();
        return FormulaValueModel.CreateArray(items, rows, columns);
    }

    static OdfFormulaResult Reference(
        int rows,
        int columns,
        params double[] values)
    {
        return FormulaValueModel.CreateReference(
            new OdfFormulaReference(),
            values.Select(Number).ToArray(),
            rows,
            columns);
    }

    static OdfFormulaResult ReferenceText(params string[] values)
    {
        return FormulaValueModel.CreateReference(
            new OdfFormulaReference(),
            values.Select(Text).ToArray(),
            1,
            values.Length);
    }
}
