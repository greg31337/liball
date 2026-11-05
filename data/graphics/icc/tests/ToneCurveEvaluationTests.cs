using icc;

namespace icc.Tests;

public class ToneCurveEvaluationTests
{
    [Test]
    public void IdentityAndGammaCurvesClipToTheNormalizedDomain()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve identity = new IccCurve();
        IccCurve gamma = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(identity, -1.0), Is.Zero);
            Assert.That(evaluator.EvaluateCurve(identity, 0.4), Is.EqualTo(0.4));
            Assert.That(evaluator.EvaluateCurve(identity, 2.0), Is.EqualTo(1.0));
            Assert.That(evaluator.EvaluateCurve(gamma, -1.0), Is.Zero);
            Assert.That(evaluator.EvaluateCurve(gamma, 0.5), Is.EqualTo(0.25));
            Assert.That(evaluator.EvaluateCurve(gamma, 2.0), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void SampledCurvesInterpolateAndUseNearestEndpointOutsideTheDomain()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        double[] samples = new double[] { 0.0, 0.2, 1.0 };
        IccCurve curve = new IccCurve { Kind = IccCurveKind.Sampled, Samples = samples };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateSampledCurve(samples, -0.5), Is.Zero);
            Assert.That(evaluator.EvaluateSampledCurve(samples, 0.25), Is.EqualTo(0.1).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(curve, 0.75), Is.EqualTo(0.6).Within(1e-15));
            Assert.That(evaluator.EvaluateSampledCurve(samples, 1.5), Is.EqualTo(1.0));
            Assert.That(samples, Is.EqualTo(new double[] { 0.0, 0.2, 1.0 }));
        });
    }

    [Test]
    public void ParametricCurvesDispatchThroughThePublicEvaluator()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccParametricCurve parametric = new IccParametricCurve
        {
            FunctionType = 4,
            Parameters = new double[] { 2.0, 1.0, 0.0, 0.5, 0.25, 0.1, 0.05 }
        };
        IccCurve curve = new IccCurve { Kind = IccCurveKind.Parametric, Parametric = parametric };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(curve, 0.2), Is.EqualTo(0.15).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 0.5), Is.EqualTo(0.35).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 2.0), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void EveryParametricCurveTypeEvaluatesItsDefinedBranches()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve type0 = ParametricCurve(0, 2.0);
        IccCurve type1 = ParametricCurve(1, 2.0, 2.0, -0.5);
        IccCurve type2 = ParametricCurve(2, 2.0, 2.0, -0.5, 0.1);
        IccCurve type3 = ParametricCurve(3, 2.0, 1.0, 0.0, 0.5, 0.25);
        IccCurve type4 = ParametricCurve(4, 2.0, 1.0, 0.0, 0.5, 0.25, 0.1, 0.05);

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(type0, 0.5), Is.EqualTo(0.25).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type1, 0.2), Is.Zero);
            Assert.That(evaluator.EvaluateCurve(type1, 0.5), Is.EqualTo(0.25).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type2, 0.2), Is.EqualTo(0.1).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type2, 0.5), Is.EqualTo(0.35).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type3, 0.2), Is.EqualTo(0.1).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type3, 0.5), Is.EqualTo(0.25).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type4, 0.2), Is.EqualTo(0.15).Within(1e-15));
            Assert.That(evaluator.EvaluateCurve(type4, 0.5), Is.EqualTo(0.35).Within(1e-15));
        });
    }

    [Test]
    public void SegmentedFormulaCurvesEvaluateEveryIccFormulaSelector()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateFormula(evaluator, 0, 0.5, 2.0, 2.0, 1.0, 3.0), Is.EqualTo(7.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 1, 0.5, 2.0, 1.0, 2.0, 1.0, 3.0), Is.EqualTo(System.Math.Log10(1.5) + 3.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 2, 0.5, 2.0, 3.0, 1.0, 0.0, 4.0), Is.EqualTo((2.0 * System.Math.Sqrt(3.0)) + 4.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 3, 0.5, 2.0, 2.0, 1.0, -1.0, 3.0), Is.EqualTo(3.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 4, 0.5, 1.0, 2.0, 1.0, 3.0, 4.0), Is.EqualTo(3.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 5, 0.5, 1.0, 2.0, 3.0, 1.0, 4.0, 5.0), Is.EqualTo((5.0 * System.Math.Exp(0.5)) + 3.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 6, 0.5, 2.0, 1.0, 1.0, 4.0, 1.0, 2.0, 3.0), Is.EqualTo(2.0 / 49.0).Within(1e-12));
            Assert.That(EvaluateFormula(evaluator, 7, 0.5, 2.0, 1.0, 1.0, 2.0, 1.0, 3.0), Is.EqualTo(16.0 / 3.0).Within(1e-12));
        });
    }

    [Test]
    public void SegmentedCurveBreakpointsBelongToThePrecedingSegment()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = SegmentedCurve(
            FormulaSegment(double.NegativeInfinity, 0, 1.0, 0.0, 0.0, 1.0),
            FormulaSegment(0.5, 0, 1.0, 0.0, 0.0, 2.0));

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(curve, -10.0), Is.EqualTo(1.0));
            Assert.That(evaluator.EvaluateCurve(curve, 0.5), Is.EqualTo(1.0));
            Assert.That(evaluator.EvaluateCurve(curve, 0.500001), Is.EqualTo(2.0));
            Assert.That(evaluator.EvaluateCurve(curve, 10.0), Is.EqualTo(2.0));
        });
    }

    [Test]
    public void SampledSegmentsInterpolateFromThePreviousSegmentEndpoint()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurveSegment sampled = new IccCurveSegment
        {
            Kind = IccCurveSegmentKind.Sampled,
            BreakPoint = 0.25,
            Samples = new double[] { 0.5, 0.8 }
        };
        IccCurve curve = SegmentedCurve(
            FormulaSegment(double.NegativeInfinity, 0, 1.0, 1.0, 0.0, 0.0),
            sampled,
            FormulaSegment(0.75, 0, 1.0, 1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(curve, 0.25), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 0.375), Is.EqualTo(0.375).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 0.5), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 0.75), Is.EqualTo(0.8).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 0.8), Is.EqualTo(0.8).Within(1e-12));
        });
    }

    [Test]
    public void SingleSampledCurvesSupportClippedAndLinearEndpointExtension()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            Samples = new double[] { 10.0, 20.0, 40.0 },
            InputMinimum = 2.0,
            InputMaximum = 4.0,
            Extension = 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(curve, 1.0), Is.EqualTo(10.0));
            Assert.That(evaluator.EvaluateCurve(curve, 3.0), Is.EqualTo(20.0));
            Assert.That(evaluator.EvaluateCurve(curve, 5.0), Is.EqualTo(40.0));
        });

        curve.Extension = 1;

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurve(curve, 1.0), Is.Zero.Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(curve, 5.0), Is.EqualTo(60.0).Within(1e-12));
        });
    }

    [Test]
    public void AdaptiveGainCurvesUseCubicHermiteInterpolationAndConstantEndpoints()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccAdaptiveGainCurve curve = new IccAdaptiveGainCurve
        {
            RedNodes = new IccAdaptiveGainCurveNode[] { Node(0.0, 0.0, 0.0), Node(1.0, 1.0, 0.0) },
            GreenNodes = new IccAdaptiveGainCurveNode[] { Node(0.0, 1.0, 2.0), Node(1.0, 3.0, 2.0) },
            BlueNodes = new IccAdaptiveGainCurveNode[] { Node(0.2, 4.0, 1.0), Node(0.8, 6.0, 1.0) }
        };

        double[] result = evaluator.EvaluateAdaptiveGainCurve(curve, new double[] { 0.5, 0.25, -1.0 });
        double[] endpoints = evaluator.EvaluateAdaptiveGainCurve(curve, new double[] { -1.0, 2.0, 2.0 });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new double[] { 0.5, 1.5, 4.0 }).Within(1e-12));
            Assert.That(endpoints, Is.EqualTo(new double[] { 0.0, 3.0, 6.0 }).Within(1e-12));
        });
    }

    [Test]
    public void CurveEvaluationRejectsMalformedModelsAndNonFiniteValues()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve badFormula = SegmentedCurve(FormulaSegment(double.NegativeInfinity, 4, 1.0, 1.0, 2.0, 0.0, 1.0));
        IccCurve badSingleSampled = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            Samples = new double[] { 0.0, 1.0 },
            InputMinimum = 0.0,
            InputMaximum = 1.0,
            Extension = 2
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(null!, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(new IccCurve(), double.NaN));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 0.0 }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateSampledCurve(new double[] { 0.0 }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateSampledCurve(new double[] { 0.0, double.PositiveInfinity }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(new IccCurve { Kind = IccCurveKind.Parametric }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(new IccCurve { Kind = IccCurveKind.Segmented }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(badFormula, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurve(badSingleSampled, 0.5));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateAdaptiveGainCurve(new IccAdaptiveGainCurve(), new double[3]));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateAdaptiveGainCurve(new IccAdaptiveGainCurve(), new double[2]));
        });
    }

    private static double EvaluateFormula(IIccToneCurveEvaluator evaluator, int type, double input, params double[] parameters)
    {
        return evaluator.EvaluateCurve(SegmentedCurve(FormulaSegment(double.NegativeInfinity, type, parameters)), input);
    }

    private static IccCurve ParametricCurve(int type, params double[] parameters)
    {
        return new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve { FunctionType = type, Parameters = parameters }
        };
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

    private static IccAdaptiveGainCurveNode Node(double input, double gain, double slope)
    {
        return new IccAdaptiveGainCurveNode { Input = input, Gain = gain, Slope = slope };
    }
}
