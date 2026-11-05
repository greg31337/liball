namespace odf.Tests;

public class FormulaStatisticalFunctionTests
{
    [Test]
    public void PublishesCompleteStatisticalFunctionMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions,
                Has.Length.EqualTo(393));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Statistical"), Is.EqualTo(78));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.AVERAGE)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.BETADIST)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.GROWTH)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.AVERAGEIFS)!
                .MaximumArguments, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void EvaluatesDescriptiveStatisticsAndMoments()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult values = Array(1, 2, 2, 4, 5);
        OdfFormulaResult mixed = FormulaValueModel.CreateArray(
            [Number(2), Boolean(true), Text("ignored")], 1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.AVERAGE,
                [values], context).NumericValue, Is.EqualTo(2.8));
            Assert.That(Evaluate(OdfFormulaNames.AVEDEV,
                [values], context).NumericValue, Is.EqualTo(1.36).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.DEVSQ,
                [values], context).NumericValue, Is.EqualTo(10.8).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.AVERAGEA,
                [mixed], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.GEOMEAN,
                [Array(1, 4)], context).NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.HARMEAN,
                [Array(1, 2, 4)], context).NumericValue,
                Is.EqualTo(12.0 / 7).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.MAX,
                [values], context).NumericValue, Is.EqualTo(5));
            Assert.That(Evaluate(OdfFormulaNames.MIN,
                [values], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.MEDIAN,
                [values], context).NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.MODE,
                [values], context).NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.TRIMMEAN,
                [Array(1, 2, 3, 100), Number(0.5)], context).NumericValue,
                Is.EqualTo(2.5));
            Assert.That(Evaluate(OdfFormulaNames.VAR,
                [values], context).NumericValue, Is.EqualTo(2.7).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.VARP,
                [values], context).NumericValue, Is.EqualTo(2.16).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.STDEV,
                [values], context).NumericValue,
                Is.EqualTo(global::System.Math.Sqrt(2.7)).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.SKEW,
                [values], context).Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(Evaluate(OdfFormulaNames.SKEWP,
                [values], context).Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(Evaluate(OdfFormulaNames.KURT,
                [values], context).Error, Is.EqualTo(OdfFormulaError.None));
        });
    }

    [Test]
    public void EvaluatesConditionalRankingAndFrequencyFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult criteria = Reference(1, 4, 1, 2, 3, 4);
        OdfFormulaResult averages = Reference(1, 4, 10, 20, 30, 40);
        OdfFormulaResult values = Array(1, 2, 2, 3, 4);
        OdfFormulaResult frequency = Evaluate(OdfFormulaNames.FREQUENCY,
            [Array(1, 2, 3, 4, 5), Array(2, 4)], context);
        OdfFormulaResult unsortedFrequency = Evaluate(
            OdfFormulaNames.FREQUENCY,
            [Array(1, 2, 3, 4, 5, 6), Array(5, 2)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.AVERAGEIF,
                [criteria, Text(">2"), averages], context).NumericValue,
                Is.EqualTo(35));
            Assert.That(Evaluate(OdfFormulaNames.AVERAGEIFS,
                [averages, criteria, Text(">1"), criteria, Text("<4")],
                context).NumericValue, Is.EqualTo(25));
            Assert.That(Numbers(frequency), Is.EqualTo(new[] { 2.0, 2.0, 1.0 }));
            Assert.That(Numbers(unsortedFrequency),
                Is.EqualTo(new[] { 3.0, 2.0, 1.0 }));
            Assert.That(Evaluate(OdfFormulaNames.LARGE,
                [values, Number(2)], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.SMALL,
                [values, Number(3)], context).NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.PERCENTILE,
                [Array(1, 2, 3, 4), Number(0.25)], context).NumericValue,
                Is.EqualTo(1.75));
            Assert.That(Evaluate(OdfFormulaNames.QUARTILE,
                [Array(1, 2, 3, 4), Number(3)], context).NumericValue,
                Is.EqualTo(3.25));
            Assert.That(Evaluate(OdfFormulaNames.PERCENTRANK,
                [Array(1, 2, 3, 4), Number(2.5)], context).NumericValue,
                Is.EqualTo(0.5));
            Assert.That(Evaluate(OdfFormulaNames.PERCENTRANK,
                [Array(7), Number(7)], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.RANK,
                [Number(2), values], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.RANK,
                [Number(2), values, Number(1)], context).NumericValue,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void EvaluatesContinuousDistributionsAndTheirInverses()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        double gammaProbability = 1 - global::System.Math.Exp(-1);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.NORMDIST,
                [Number(0), Number(0), Number(1), Boolean(true)], context)
                .NumericValue, Is.EqualTo(0.5).Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.NORMINV,
                [Number(0.5), Number(0), Number(1)], context).NumericValue,
                Is.Zero.Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.PHI,
                [Number(0)], context).NumericValue,
                Is.EqualTo(0.3989422804).Within(1E-9));
            Assert.That(Evaluate(OdfFormulaNames.GAUSS,
                [Number(0)], context).NumericValue, Is.Zero.Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.LOGNORMDIST,
                [Number(1)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.LOGINV,
                [Number(0.5)], context).NumericValue, Is.EqualTo(1).Within(1E-7));
            Assert.That(Evaluate(OdfFormulaNames.BETADIST,
                [Number(0.5), Number(2), Number(2)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.BETAINV,
                [Number(0.5), Number(2), Number(2)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.BETADIST,
                [Number(2), Number(2), Number(2)], context).NumericValue,
                Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.BETADIST,
                [Number(2), Number(2), Number(2), Number(0), Number(1),
                    Boolean(false)], context).NumericValue, Is.Zero);
            Assert.That(Evaluate(OdfFormulaNames.GAMMADIST,
                [Number(2), Number(1), Number(2)], context).NumericValue,
                Is.EqualTo(gammaProbability).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.GAMMAINV,
                [Number(gammaProbability), Number(1), Number(2)], context)
                .NumericValue, Is.EqualTo(2).Within(1E-9));
            Assert.That(Evaluate(OdfFormulaNames.GAMMADIST,
                [Number(0), Number(1), Number(2), Boolean(false)], context)
                .NumericValue, Is.EqualTo(0.5));
            Assert.That(Evaluate(OdfFormulaNames.CHISQDIST,
                [Number(2), Number(2)], context).NumericValue,
                Is.EqualTo(gammaProbability).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.FDIST,
                [Number(1), Number(5), Number(5)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.FINV,
                [Number(0.5), Number(5), Number(5)], context).NumericValue,
                Is.EqualTo(1).Within(1E-9));
            Assert.That(Evaluate(OdfFormulaNames.EXPONDIST,
                [Number(1), Number(2)], context).NumericValue,
                Is.EqualTo(1 - global::System.Math.Exp(-2)).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.EXPONDIST,
                [Number(-1), Number(2)], context).NumericValue, Is.Zero);
            Assert.That(Evaluate(OdfFormulaNames.WEIBULL,
                [Number(1), Number(2), Number(1), Boolean(true)], context)
                .NumericValue, Is.EqualTo(gammaProbability).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.TINV,
                [Number(0.05), Number(10)], context).NumericValue,
                Is.EqualTo(2.228139).Within(0.00001));
        });
    }

    [Test]
    public void EvaluatesDiscreteProbabilityAndPermutationFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.BINOMDIST,
                [Number(2), Number(4), Number(0.5), Boolean(false)], context)
                .NumericValue, Is.EqualTo(0.375));
            Assert.That(Evaluate(OdfFormulaNames.BINOM_DIST_RANGE,
                [Number(4), Number(0.5), Number(1), Number(2)], context)
                .NumericValue, Is.EqualTo(0.625));
            Assert.That(Evaluate(OdfFormulaNames.CRITBINOM,
                [Number(4), Number(0.5), Number(0.7)], context).NumericValue,
                Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.HYPGEOMDIST,
                [Number(1), Number(3), Number(2), Number(6)], context)
                .NumericValue, Is.EqualTo(0.6));
            Assert.That(Evaluate(OdfFormulaNames.POISSON,
                [Number(2), Number(2), Boolean(false)], context).NumericValue,
                Is.EqualTo(2 * global::System.Math.Exp(-2)).Within(1E-12));
            Assert.That(Evaluate(OdfFormulaNames.NEGBINOMDIST,
                [Number(2), Number(3), Number(0.5)], context).NumericValue,
                Is.EqualTo(0.1875));
            Assert.That(Evaluate(OdfFormulaNames.PROB,
                [Array(1, 2, 3), Array(0.2, 0.3, 0.5), Number(2)], context)
                .NumericValue, Is.EqualTo(0.3));
            Assert.That(Evaluate(OdfFormulaNames.PERMUT,
                [Number(5), Number(2)], context).NumericValue, Is.EqualTo(20));
            Assert.That(Evaluate(OdfFormulaNames.PERMUTATIONA,
                [Number(5), Number(2)], context).NumericValue, Is.EqualTo(25));
            Assert.That(Evaluate(OdfFormulaNames.PERMUTATIONA,
                [Number(2), Number(3)], context).NumericValue, Is.EqualTo(8));
        });
    }

    [Test]
    public void EvaluatesLinearAndExponentialRegressionFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult x = Array(1, 2, 3, 4);
        OdfFormulaResult y = Array(3, 5, 7, 9);
        OdfFormulaResult line = Evaluate(OdfFormulaNames.LINEST,
            [y, x], context);
        OdfFormulaResult trend = Evaluate(OdfFormulaNames.TREND,
            [y, x, Array(5, 6)], context);
        OdfFormulaResult log = Evaluate(OdfFormulaNames.LOGEST,
            [Array(2, 4, 8), Array(1, 2, 3)], context);
        OdfFormulaResult growth = Evaluate(OdfFormulaNames.GROWTH,
            [Array(2, 4, 8), Array(1, 2, 3), Array(4)], context);
        OdfFormulaResult multipleX = Matrix(
            5,
            2,
            1, 2,
            2, 0,
            3, 1,
            4, 3,
            5, 4);
        OdfFormulaResult multipleY = Matrix(5, 1, 9, 5, 10, 18, 23);
        OdfFormulaResult multipleLine = Evaluate(OdfFormulaNames.LINEST,
            [multipleY, multipleX], context);
        OdfFormulaResult multipleTrend = Evaluate(OdfFormulaNames.TREND,
            [multipleY, multipleX, Matrix(2, 2, 6, 2, 7, 5)], context);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.SLOPE,
                [y, x], context).NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.INTERCEPT,
                [y, x], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.CORREL,
                [y, x], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.PEARSON,
                [y, x], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.COVAR,
                [y, x], context).NumericValue, Is.EqualTo(2.5));
            Assert.That(Evaluate(OdfFormulaNames.RSQ,
                [y, x], context).NumericValue, Is.EqualTo(1));
            Assert.That(Evaluate(OdfFormulaNames.STEYX,
                [y, x], context).NumericValue, Is.Zero);
            Assert.That(Evaluate(OdfFormulaNames.FORECAST,
                [Number(5), y, x], context).NumericValue, Is.EqualTo(11));
            Assert.That(Evaluate(OdfFormulaNames.SLOPE,
                [Array(4, 4, 4, 4), x], context).NumericValue, Is.Zero);
            Assert.That(Evaluate(OdfFormulaNames.INTERCEPT,
                [Array(4, 4, 4, 4), x], context).NumericValue, Is.EqualTo(4));
            Assert.That(Numbers(line), Is.EqualTo(new[] { 2.0, 1.0 })
                .Within(1E-12));
            Assert.That(Numbers(trend), Is.EqualTo(new[] { 11.0, 13.0 })
                .Within(1E-12));
            Assert.That(Numbers(log), Is.EqualTo(new[] { 2.0, 1.0 })
                .Within(1E-12));
            Assert.That(Numbers(growth), Is.EqualTo(new[] { 16.0 })
                .Within(1E-10));
            Assert.That(Numbers(multipleLine),
                Is.EqualTo(new[] { 3.0, 2.0, 1.0 }).Within(1E-10));
            Assert.That(Numbers(multipleTrend),
                Is.EqualTo(new[] { 19.0, 30.0 }).Within(1E-10));
        });
    }

    [Test]
    public void EvaluatesStatisticalTestsAndTransformations()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult first = Array(1, 2, 4, 7);
        OdfFormulaResult second = Array(2, 4, 5, 8);
        OdfFormulaResult fTest = Evaluate(OdfFormulaNames.FTEST,
            [first, first], context);
        OdfFormulaResult tTest = Evaluate(OdfFormulaNames.TTEST,
            [first, second, Number(2), Number(1)], context);
        OdfFormulaResult fisher = Evaluate(OdfFormulaNames.FISHER,
            [Number(0.5)], context);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.CONFIDENCE,
                [Number(0.05), Number(2), Number(100)], context).NumericValue,
                Is.EqualTo(0.3919928).Within(0.000001));
            Assert.That(Evaluate(OdfFormulaNames.STANDARDIZE,
                [Number(10), Number(6), Number(2)], context).NumericValue,
                Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.ZTEST,
                [Array(1, 2, 3), Number(2), Number(1)], context).NumericValue,
                Is.EqualTo(0.5).Within(1E-7));
            Assert.That(fTest.NumericValue, Is.EqualTo(1).Within(1E-10));
            Assert.That(tTest.NumericValue, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(fisher.NumericValue,
                Is.EqualTo(0.5493061443).Within(1E-10));
            Assert.That(Evaluate(OdfFormulaNames.FISHERINV,
                [fisher], context).NumericValue, Is.EqualTo(0.5).Within(1E-12));
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

    static OdfFormulaResult Boolean(bool value)
    {
        return FormulaValueModel.CreateBoolean(value);
    }

    static OdfFormulaResult Text(string value)
    {
        return FormulaValueModel.CreateText(value);
    }

    static OdfFormulaResult Array(params double[] values)
    {
        return Matrix(1, values.Length, values);
    }

    static OdfFormulaResult Matrix(
        int rows,
        int columns,
        params double[] values)
    {
        OdfFormulaResult[] results = new OdfFormulaResult[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            results[index] = Number(values[index]);
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(results, rows, columns);
    }

    static OdfFormulaResult Reference(
        int rows,
        int columns,
        params double[] values)
    {
        OdfFormulaReference reference = new OdfFormulaReference
        {
            Kind = OdfFormulaNodeKind.Range,
            HasEnd = true,
            Start = new OdfFormulaReferenceEndpoint
            {
                RowNumber = 1,
                ColumnNumber = 1,
                HasRow = true,
                HasColumn = true
            },
            End = new OdfFormulaReferenceEndpoint
            {
                RowNumber = rows,
                ColumnNumber = columns,
                HasRow = true,
                HasColumn = true
            }
        };
        return FormulaValueModel.CreateReference(
            reference,
            Array(values).Values,
            rows,
            columns);
    }

    static double[] Numbers(OdfFormulaResult result)
    {
        double[] values = new double[result.Values.Length];
        int index = 0;
        while (index < result.Values.Length)
        {
            values[index] = result.Values[index].NumericValue;
            index = index + 1;
        }
        return values;
    }
}
