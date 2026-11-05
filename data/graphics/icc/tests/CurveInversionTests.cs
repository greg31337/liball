using icc;

namespace icc.Tests;

public class CurveInversionTests
{
    [Test]
    public void IdentityAndGammaCurvesInvertAnalytically()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve identity = new IccCurve();
        IccCurve gamma = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.2 };

        IccCurveInversionResult identityResult = evaluator.InvertCurve(identity, 0.35);
        IccCurveInversionResult gammaResult = evaluator.InvertCurve(gamma, evaluator.EvaluateCurve(gamma, 0.42));

        Assert.Multiple(() =>
        {
            AssertSuccess(identityResult, 0.35, 1e-15);
            AssertSuccess(gammaResult, 0.42, 1e-12);
            AssertSuccess(evaluator.InvertCurve(gamma, -1.0), 0.0, 1e-15);
            AssertSuccess(evaluator.InvertCurve(gamma, 2.0), 1.0, 1e-15);
        });
    }

    [Test]
    public void EveryIccParametricCurveTypeUsesAnAnalyticalInverse()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve[] curves = new IccCurve[]
        {
            ParametricCurve(0, 2.0),
            ParametricCurve(1, 2.0, 1.0, 0.0),
            ParametricCurve(2, 2.0, 1.0, 0.0, 0.1),
            ParametricCurve(3, 2.0, 1.0, 0.0, 0.25, 0.25),
            ParametricCurve(4, 2.0, 1.0, 0.0, 0.45, 0.25, 0.1, 0.05)
        };

        int index = 0;
        while (index < curves.Length)
        {
            double output = evaluator.EvaluateCurve(curves[index], 0.6);
            IccCurveInversionResult result = evaluator.InvertCurve(curves[index], output);

            AssertSuccess(result, 0.6, 1e-10);
            Assert.That(result.Result.Message, Does.Contain("analytically"));
            index++;
        }
    }

    [Test]
    public void ParametricLowerPlateauMapsToItsInputMidpoint()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = ParametricCurve(1, 2.0, 2.0, -0.5);

        IccCurveInversionResult result = evaluator.InvertCurve(curve, 0.0);

        AssertSuccess(result, 0.125, 1e-15);
        Assert.That(result.Result.Message, Does.Contain("plateau"));
    }

    [Test]
    public void ParametricClippingPlateausMapToTheirInputMidpoints()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = ParametricCurve(2, 1.0, 2.0, 0.0, -0.4);

        IccCurveInversionResult lower = evaluator.InvertCurve(curve, 0.0);
        IccCurveInversionResult upper = evaluator.InvertCurve(curve, 1.0);

        Assert.Multiple(() =>
        {
            AssertSuccess(lower, 0.1, 1e-12);
            Assert.That(lower.Result.Message, Does.Contain("plateau"));
            AssertSuccess(upper, 0.85, 1e-12);
            Assert.That(upper.Result.Message, Does.Contain("plateau"));
        });
    }

    [Test]
    public void SampledCurvesInvertInBothDirectionsAndClipOutsideTheirRange()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        double[] ascending = new double[] { 0.1, 0.3, 0.7, 0.9 };
        double[] descending = new double[] { 1.0, 0.8, 0.2, 0.0 };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.InvertSampledCurve(ascending, 0.5), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(evaluator.InvertSampledCurve(descending, 0.5), Is.EqualTo(0.5).Within(1e-12));
            AssertSuccess(evaluator.InvertCurve(SampledCurve(ascending), -1.0), 0.0, 1e-15);
            AssertSuccess(evaluator.InvertCurve(SampledCurve(ascending), 2.0), 1.0, 1e-15);
            AssertSuccess(evaluator.InvertCurve(SampledCurve(descending), -1.0), 1.0, 1e-15);
            AssertSuccess(evaluator.InvertCurve(SampledCurve(descending), 2.0), 0.0, 1e-15);
        });
    }

    [Test]
    public void DuplicateSamplePlateauMapsToThePlateauMidpoint()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        double[] samples = new double[] { 0.0, 0.5, 0.5, 0.5, 1.0 };

        IccCurveInversionResult result = evaluator.InvertCurve(SampledCurve(samples), 0.5);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 0.5, 1e-15);
            Assert.That(evaluator.InvertSampledCurve(samples, 0.5), Is.EqualTo(0.5));
            Assert.That(result.Result.Message, Does.Contain("plateau"));
        });
    }

    [Test]
    public void SingleSampledInverseHonorsLinearExtension()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            Samples = new double[] { 10.0, 20.0, 40.0 },
            InputMinimum = 2.0,
            InputMaximum = 4.0,
            Extension = 1
        };

        Assert.Multiple(() =>
        {
            AssertSuccess(evaluator.InvertCurve(curve, 0.0), 1.0, 1e-12);
            AssertSuccess(evaluator.InvertCurve(curve, 30.0), 3.5, 1e-12);
            AssertSuccess(evaluator.InvertCurve(curve, 60.0), 5.0, 1e-12);
        });
    }

    [Test]
    public void SegmentedCurvesInvertFormulaAndSampledSegmentsNumerically()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurveSegment sampled = new IccCurveSegment
        {
            Kind = IccCurveSegmentKind.Sampled,
            BreakPoint = 0.25,
            Samples = new double[] { 0.5, 0.75 }
        };
        IccCurve curve = SegmentedCurve(
            FormulaSegment(double.NegativeInfinity, 0, 2.0, 1.0, 0.0, 0.0),
            sampled,
            FormulaSegment(0.75, 0, 1.0, 1.0, 0.0, 0.0));

        double formulaOutput = evaluator.EvaluateCurve(curve, 0.2);
        double sampledOutput = evaluator.EvaluateCurve(curve, 0.6);
        IccCurveInversionResult formulaResult = evaluator.InvertCurve(curve, formulaOutput);
        IccCurveInversionResult sampledResult = evaluator.InvertCurve(curve, sampledOutput);

        Assert.Multiple(() =>
        {
            AssertSuccess(formulaResult, 0.2, 1e-8);
            AssertSuccess(sampledResult, 0.6, 1e-8);
            Assert.That(formulaResult.Result.Message, Does.Contain("numerically"));
            Assert.That(sampledResult.Result.Message, Does.Contain("numerically"));
        });
    }

    [Test]
    public void ConstantAndNonMonotonicCurvesReturnExplicitConflictResults()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve constant = SampledCurve(new double[] { 0.5, 0.5, 0.5 });
        IccCurve nonMonotonic = SampledCurve(new double[] { 0.0, 1.0, 0.5 });
        IccCurve nonMonotonicParametric = ParametricCurve(3, 1.0, 1.0, 0.0, 2.0, 0.5);

        IccCurveInversionResult constantResult = evaluator.InvertCurve(constant, 0.5);
        IccCurveInversionResult nonMonotonicResult = evaluator.InvertCurve(nonMonotonic, 0.75);
        IccCurveInversionResult parametricResult = evaluator.InvertCurve(nonMonotonicParametric, 0.75);

        Assert.Multiple(() =>
        {
            AssertConflict(constantResult, "constant");
            AssertConflict(nonMonotonicResult, "non-monotonic");
            AssertConflict(parametricResult, "non-monotonic");
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertSampledCurve(constant.Samples, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertSampledCurve(nonMonotonic.Samples, 0.75));
        });
    }

    [Test]
    public void InversionRejectsMalformedModelsAndNonFiniteOutputs()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertCurve(null!, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertCurve(new IccCurve(), double.NaN));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertSampledCurve(new double[] { 0.0 }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertSampledCurve(new double[] { 0.0, double.PositiveInfinity }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertCurve(new IccCurve { Kind = IccCurveKind.Parametric }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.InvertCurve(new IccCurve { Kind = IccCurveKind.Segmented }, 0.5));
        });
    }

    private static IccCurve ParametricCurve(int type, params double[] parameters)
    {
        return new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve { FunctionType = type, Parameters = parameters }
        };
    }

    private static IccCurve SampledCurve(double[] samples)
    {
        return new IccCurve { Kind = IccCurveKind.Sampled, Samples = samples };
    }

    private static IccCurve SegmentedCurve(params IccCurveSegment[] segments)
    {
        return new IccCurve { Kind = IccCurveKind.Segmented, Segments = segments };
    }

    private static IccCurveSegment FormulaSegment(double breakPoint, int type, params double[] parameters)
    {
        return new IccCurveSegment
        {
            Kind = IccCurveSegmentKind.Formula,
            BreakPoint = breakPoint,
            FormulaType = type,
            Parameters = parameters
        };
    }

    private static void AssertSuccess(IccCurveInversionResult result, double expected, double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.HasInput, Is.True);
            Assert.That(result.Input, Is.EqualTo(expected).Within(tolerance));
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Result.Message, Is.Not.Empty);
        });
    }

    private static void AssertConflict(IccCurveInversionResult result, string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.HasInput, Is.False);
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(result.Result.Message, Does.Contain(message));
        });
    }
}
