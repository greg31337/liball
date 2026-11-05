using icc;

namespace icc.Tests;

public class CurveAnalysisTests
{
    [Test]
    public void SamplingIncludesBothDomainEndpoints()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve gamma = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 };

        double[] general = evaluator.SampleCurve(gamma, 5);
        double[] specialized = evaluator.SampleGammaCurve(2.0, 5);
        IccCurve resampled = evaluator.ResampleCurve(gamma, 5);

        Assert.Multiple(() =>
        {
            Assert.That(general, Is.EqualTo(new double[] { 0.0, 0.0625, 0.25, 0.5625, 1.0 }).Within(1e-15));
            Assert.That(specialized, Is.EqualTo(general));
            Assert.That(resampled.Kind, Is.EqualTo(IccCurveKind.Sampled));
            Assert.That(resampled.Samples, Is.EqualTo(general));
            Assert.That(resampled.Analysis, Is.Not.Null);
        });
    }

    [Test]
    public void ResamplingCreatesAnIndependentCurveAndPreservesExplicitDomain()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve source = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            Samples = new double[] { 10.0, 20.0, 40.0 },
            InputMinimum = 2.0,
            InputMaximum = 4.0,
            Extension = 1,
            Encoding = 2
        };

        IccCurve result = evaluator.ResampleCurve(source, 5);
        source.Samples[1] = 99.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(IccCurveKind.SingleSampled));
            Assert.That(result.Samples, Is.EqualTo(new double[] { 10.0, 15.0, 20.0, 30.0, 40.0 }).Within(1e-15));
            Assert.That(result.Samples, Is.Not.SameAs(source.Samples));
            Assert.That(result.InputMinimum, Is.EqualTo(2.0));
            Assert.That(result.InputMaximum, Is.EqualTo(4.0));
            Assert.That(result.Extension, Is.EqualTo(1));
            Assert.That(result.Encoding, Is.EqualTo(2));
            Assert.That(result.Analysis, Is.Not.Null);
            Assert.That(result.Analysis!.SampleCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void SlopeEvaluationUsesExactLinearAndGammaDerivatives()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve identity = new IccCurve();
        IccCurve gamma = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 };
        IccCurve sampled = new IccCurve { Kind = IccCurveKind.Sampled, Samples = new double[] { 0.0, 0.25, 1.0 } };
        IccCurve parametric = new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve { FunctionType = 0, Parameters = new double[] { 2.0 } }
        };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurveSlope(identity, 0.4), Is.EqualTo(1.0));
            Assert.That(evaluator.EvaluateCurveSlope(identity, -0.1), Is.Zero);
            Assert.That(evaluator.EvaluateCurveSlope(gamma, 0.25), Is.EqualTo(0.5).Within(1e-15));
            Assert.That(evaluator.EvaluateCurveSlope(gamma, 0.0), Is.Zero);
            Assert.That(evaluator.EvaluateCurveSlope(new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 }, 0.0), Is.EqualTo(1.0));
            Assert.That(evaluator.EvaluateCurveSlope(new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 0.5 }, 0.0), Is.EqualTo(double.PositiveInfinity));
            Assert.That(evaluator.EvaluateCurveSlope(sampled, 0.25), Is.EqualTo(0.5).Within(1e-15));
            Assert.That(evaluator.EvaluateCurveSlope(sampled, 0.5), Is.EqualTo(1.0).Within(1e-15));
            Assert.That(evaluator.EvaluateCurveSlope(sampled, 0.75), Is.EqualTo(1.5).Within(1e-15));
            Assert.That(evaluator.EvaluateCurveSlope(parametric, -0.1), Is.Zero);
            Assert.That(evaluator.EvaluateCurveSlope(parametric, 1.1), Is.Zero);
        });
    }

    [Test]
    public void SingleSampledSlopeHonorsClippedAndLinearExtension()
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

        Assert.That(evaluator.EvaluateCurveSlope(curve, 1.0), Is.Zero);
        curve.Extension = 1;

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurveSlope(curve, 1.0), Is.EqualTo(10.0));
            Assert.That(evaluator.EvaluateCurveSlope(curve, 5.0), Is.EqualTo(20.0));
        });
    }

    [Test]
    public void AnalysisReportsDomainRangeDirectionSlopeAndRoundTripError()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = new IccCurve { Kind = IccCurveKind.Sampled, Samples = new double[] { 0.0, 0.25, 1.0 } };

        IccCurveAnalysis analysis = evaluator.AnalyzeCurve(curve, 5);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(analysis.IsMonotonic, Is.True);
            Assert.That(analysis.IsIncreasing, Is.True);
            Assert.That(analysis.IsDecreasing, Is.False);
            Assert.That(analysis.IsContinuous, Is.True);
            Assert.That(analysis.IsInvertible, Is.True);
            Assert.That(analysis.DomainMinimum, Is.Zero);
            Assert.That(analysis.DomainMaximum, Is.EqualTo(1.0));
            Assert.That(analysis.Minimum, Is.Zero);
            Assert.That(analysis.Maximum, Is.EqualTo(1.0));
            Assert.That(analysis.MinimumSlope, Is.EqualTo(0.5).Within(1e-15));
            Assert.That(analysis.MaximumSlope, Is.EqualTo(1.5).Within(1e-15));
            Assert.That(analysis.MaximumRoundTripError, Is.LessThan(1e-12));
            Assert.That(analysis.SampleCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void AnalysisDistinguishesDescendingConstantAndNonMonotonicCurves()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve descending = SampledCurve(1.0, 0.5, 0.0);
        IccCurve constant = SampledCurve(0.5, 0.5, 0.5);
        IccCurve nonMonotonic = SampledCurve(0.0, 1.0, 0.5);

        IccCurveAnalysis descendingAnalysis = evaluator.AnalyzeCurve(descending, 5);
        IccCurveAnalysis constantAnalysis = evaluator.AnalyzeCurve(constant, 5);
        IccCurveAnalysis nonMonotonicAnalysis = evaluator.AnalyzeCurve(nonMonotonic, 5);

        Assert.Multiple(() =>
        {
            Assert.That(descendingAnalysis.IsMonotonic, Is.True);
            Assert.That(descendingAnalysis.IsIncreasing, Is.False);
            Assert.That(descendingAnalysis.IsDecreasing, Is.True);
            Assert.That(descendingAnalysis.IsInvertible, Is.True);
            Assert.That(constantAnalysis.IsMonotonic, Is.True);
            Assert.That(constantAnalysis.IsIncreasing, Is.True);
            Assert.That(constantAnalysis.IsDecreasing, Is.True);
            Assert.That(constantAnalysis.IsInvertible, Is.False);
            Assert.That(nonMonotonicAnalysis.IsMonotonic, Is.False);
            Assert.That(nonMonotonicAnalysis.IsInvertible, Is.False);
            Assert.That(evaluator.AnalyzeSampledCurve(descending.Samples).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(evaluator.AnalyzeSampledCurve(constant.Samples).Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(evaluator.AnalyzeSampledCurve(nonMonotonic.Samples).Status, Is.EqualTo(IccOperationStatus.Conflict));
        });
    }

    [Test]
    public void AnalysisDetectsParametricAndSegmentedDiscontinuities()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve parametric = new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve
            {
                FunctionType = 3,
                Parameters = new double[] { 1.0, 1.0, 0.0, 0.5, 0.5 }
            }
        };
        IccCurve segmented = new IccCurve
        {
            Kind = IccCurveKind.Segmented,
            Segments = new IccCurveSegment[]
            {
                Formula(double.NegativeInfinity, 1.0),
                Formula(0.5, 2.0)
            }
        };

        IccCurveAnalysis parametricAnalysis = evaluator.AnalyzeCurve(parametric, 9);
        IccCurveAnalysis segmentedAnalysis = evaluator.AnalyzeCurve(segmented, 9);

        Assert.Multiple(() =>
        {
            Assert.That(parametricAnalysis.IsContinuous, Is.False);
            Assert.That(parametricAnalysis.IsInvertible, Is.False);
            Assert.That(segmentedAnalysis.IsContinuous, Is.False);
            Assert.That(segmentedAnalysis.IsInvertible, Is.False);
        });
    }

    [Test]
    public void SegmentedSlopeContinuityAndRoundTripMatchALinearReference()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve curve = new IccCurve
        {
            Kind = IccCurveKind.Segmented,
            Segments = new IccCurveSegment[]
            {
                LinearFormula(double.NegativeInfinity, 2.0, 0.0),
                LinearFormula(0.5, 2.0, 0.0)
            }
        };

        IccCurveAnalysis analysis = evaluator.AnalyzeCurve(curve, 9);

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateCurveSlope(curve, 0.25), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(analysis.IsMonotonic, Is.True);
            Assert.That(analysis.IsContinuous, Is.True);
            Assert.That(analysis.IsInvertible, Is.True);
            Assert.That(analysis.Minimum, Is.Zero.Within(1e-15));
            Assert.That(analysis.Maximum, Is.EqualTo(2.0).Within(1e-15));
            Assert.That(analysis.MinimumSlope, Is.EqualTo(2.0).Within(1e-9));
            Assert.That(analysis.MaximumSlope, Is.EqualTo(2.0).Within(1e-9));
            Assert.That(analysis.MaximumRoundTripError, Is.LessThan(1e-12));
        });
    }

    [Test]
    public void AnalysisFieldsArePreservedWhenCurveDataIsCloned()
    {
        IccCurve source = new IccCurve
        {
            Analysis = new IccCurveAnalysis
            {
                IsMonotonic = true,
                IsIncreasing = true,
                IsContinuous = true,
                IsInvertible = true,
                DomainMinimum = 2.0,
                DomainMaximum = 4.0,
                Minimum = 10.0,
                Maximum = 40.0,
                MinimumSlope = 10.0,
                MaximumSlope = 20.0,
                MaximumRoundTripError = 0.001,
                SampleCount = 17
            }
        };

        IccCurve clone = ExtendedTagDataClone.CloneCurve(source, new ProfileCloneContext());

        Assert.Multiple(() =>
        {
            Assert.That(clone.Analysis, Is.Not.SameAs(source.Analysis));
            Assert.That(clone.Analysis!.IsIncreasing, Is.True);
            Assert.That(clone.Analysis.IsContinuous, Is.True);
            Assert.That(clone.Analysis.DomainMinimum, Is.EqualTo(2.0));
            Assert.That(clone.Analysis.DomainMaximum, Is.EqualTo(4.0));
            Assert.That(clone.Analysis.MinimumSlope, Is.EqualTo(10.0));
            Assert.That(clone.Analysis.MaximumSlope, Is.EqualTo(20.0));
            Assert.That(clone.Analysis.MaximumRoundTripError, Is.EqualTo(0.001));
            Assert.That(clone.Analysis.SampleCount, Is.EqualTo(17));
        });
    }

    [Test]
    public void SamplingAndAnalysisRejectInvalidCountsAndCurves()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccOperationResult nullSamples = evaluator.AnalyzeSampledCurve(null!);
        IccOperationResult emptySamples = evaluator.AnalyzeSampledCurve(
            new double[0]);
        IccOperationResult oneSample = evaluator.AnalyzeSampledCurve(
            new double[] { 0.5 });
        IccOperationResult nonFinite = evaluator.AnalyzeSampledCurve(
            new double[] { 0.0, double.NaN });

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => evaluator.SampleCurve(new IccCurve(), 1));
            Assert.Throws<IccProfileValidationException>(() => evaluator.SampleCurve(new IccCurve(), 1048577));
            Assert.Throws<IccProfileValidationException>(() => evaluator.SampleCurve(null!, 2));
            Assert.Throws<IccProfileValidationException>(() => evaluator.SampleGammaCurve(0.0, 3));
            Assert.Throws<IccProfileValidationException>(() => evaluator.EvaluateCurveSlope(new IccCurve(), double.NaN));
            Assert.That(nullSamples.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nullSamples.Message, Does.Contain("at least two finite samples"));
            Assert.That(emptySamples.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(oneSample.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonFinite.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonFinite.Message, Does.Contain("sample 1 must be finite"));
        });
    }

    private static IccCurve SampledCurve(params double[] samples)
    {
        return new IccCurve { Kind = IccCurveKind.Sampled, Samples = samples };
    }

    private static IccCurveSegment Formula(double breakpoint, double value)
    {
        return new IccCurveSegment
        {
            Kind = IccCurveSegmentKind.Formula,
            BreakPoint = breakpoint,
            FormulaType = 0,
            Parameters = new double[] { 1.0, 0.0, 0.0, value }
        };
    }

    private static IccCurveSegment LinearFormula(double breakpoint, double scale, double offset)
    {
        return new IccCurveSegment
        {
            Kind = IccCurveSegmentKind.Formula,
            BreakPoint = breakpoint,
            FormulaType = 0,
            Parameters = new double[] { 1.0, scale, 0.0, offset }
        };
    }
}
