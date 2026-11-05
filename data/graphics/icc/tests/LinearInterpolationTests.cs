using icc;

namespace icc.Tests;

public class LinearInterpolationTests
{
    [Test]
    public void OneDimensionalInterpolationReturnsExactSamplesAtEndpointsAndKnots()
    {
        double[] samples = new double[] { -0.0, 10.0, double.MaxValue };

        double minimum = ClutInterpolator.InterpolateOneDimensional(samples, 2.0, 2.0, 6.0, true);
        double knot = ClutInterpolator.InterpolateOneDimensional(samples, 4.0, 2.0, 6.0, true);
        double maximum = ClutInterpolator.InterpolateOneDimensional(samples, 6.0, 2.0, 6.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(minimum), Is.EqualTo(BitConverter.DoubleToInt64Bits(samples[0])));
            Assert.That(knot, Is.EqualTo(samples[1]));
            Assert.That(maximum, Is.EqualTo(samples[2]));
        });
    }

    [Test]
    public void OneDimensionalInterpolationUsesEveryPiecewiseLinearInterval()
    {
        double[] samples = new double[] { 0.0, 0.2, 0.8, 1.0 };

        Assert.Multiple(() =>
        {
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 1.0 / 6.0, 0.0, 1.0, false), Is.EqualTo(0.1).Within(1e-15));
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 0.5, 0.0, 1.0, false), Is.EqualTo(0.5).Within(1e-15));
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 5.0 / 6.0, 0.0, 1.0, false), Is.EqualTo(0.9).Within(1e-15));
        });
    }

    [Test]
    public void EndpointPolicySelectsClippingOrLinearExtension()
    {
        double[] samples = new double[] { 10.0, 20.0, 40.0 };

        Assert.Multiple(() =>
        {
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 1.0, 2.0, 4.0, false), Is.EqualTo(10.0));
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 5.0, 2.0, 4.0, false), Is.EqualTo(40.0));
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 1.0, 2.0, 4.0, true), Is.EqualTo(0.0).Within(1e-12));
            Assert.That(ClutInterpolator.InterpolateOneDimensional(samples, 5.0, 2.0, 4.0, true), Is.EqualTo(60.0).Within(1e-12));
        });
    }

    [Test]
    public void InterpolationHandlesVeryLargeFiniteDomainsAndFlatExtensions()
    {
        double middle = ClutInterpolator.InterpolateOneDimensional(
            new double[] { -1.0e308, 1.0e308 },
            0.0,
            -double.MaxValue,
            double.MaxValue,
            false);
        double flat = ClutInterpolator.InterpolateOneDimensional(
            new double[] { 0.5, 0.5 },
            double.MaxValue,
            0.0,
            1.0,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(middle, Is.Zero);
            Assert.That(flat, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void LutShapersApplyIndependentChannelTablesAndPolicies()
    {
        double[][] tables = new double[][]
        {
            new double[] { 0.0, 0.25, 1.0 },
            new double[] { 1.0, 0.5, 0.0 },
            new double[] { 0.2, 0.4 }
        };

        double[] clipped = ClutInterpolator.ApplyLutShapers(tables, new double[] { 0.25, 0.75, 2.0 }, false);
        double[] extended = ClutInterpolator.ApplyLutShapers(tables, new double[] { -0.5, 1.5, 2.0 }, true);

        Assert.Multiple(() =>
        {
            Assert.That(clipped, Is.EqualTo(new double[] { 0.125, 0.25, 0.4 }).Within(1e-15));
            Assert.That(extended, Is.EqualTo(new double[] { -0.25, -0.5, 0.6 }).Within(1e-15));
            Assert.That(clipped, Is.Not.SameAs(extended));
            Assert.That(tables[0], Is.EqualTo(new double[] { 0.0, 0.25, 1.0 }));
        });
    }

    [Test]
    public void PublicSampledCurvePathsUseTheSharedBoundaryPolicies()
    {
        IIccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();
        IccCurve extended = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            Samples = new double[] { 10.0, 20.0, 40.0 },
            InputMinimum = 2.0,
            InputMaximum = 4.0,
            Extension = 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateSampledCurve(new double[] { 0.1, 0.9 }, -1.0), Is.EqualTo(0.1));
            Assert.That(evaluator.EvaluateSampledCurve(new double[] { 0.1, 0.9 }, 2.0), Is.EqualTo(0.9));
            Assert.That(evaluator.EvaluateCurve(extended, 1.0), Is.EqualTo(0.0).Within(1e-12));
            Assert.That(evaluator.EvaluateCurve(extended, 5.0), Is.EqualTo(60.0).Within(1e-12));
        });
    }

    [Test]
    public void InvalidTablesDomainsAndNonFiniteResultsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(null!, 0.5, 0.0, 1.0, false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(new double[] { 0.0 }, 0.5, 0.0, 1.0, false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(new double[] { 0.0, double.NaN }, 0.5, 0.0, 1.0, false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(new double[] { 0.0, 1.0 }, double.NaN, 0.0, 1.0, false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(new double[] { 0.0, 1.0 }, 0.5, 1.0, 1.0, false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateOneDimensional(new double[] { 0.0, double.MaxValue }, 2.0, 0.0, 1.0, true));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.ApplyLutShapers(new double[0][], new double[0], false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.ApplyLutShapers(new double[][] { new double[] { 0.0, 1.0 } }, new double[2], false));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.ApplyLutShapers(new double[][] { null! }, new double[1], false));
        });
    }
}
