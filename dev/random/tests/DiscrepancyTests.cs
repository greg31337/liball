namespace random.Tests;

public class DiscrepancyTests
{
    [Test]
    public void CenteredMatchesPublishedScipyReferenceSample()
    {
        // scipy.stats.qmc.discrepancy documentation example, method="CD".
        double[] points =
        [
            1.0 / 12.0, 5.0 / 12.0,
            3.0 / 12.0, 11.0 / 12.0,
            5.0 / 12.0, 3.0 / 12.0,
            7.0 / 12.0, 9.0 / 12.0,
            9.0 / 12.0, 1.0 / 12.0,
            11.0 / 12.0, 7.0 / 12.0
        ];

        double actual = Discrepancy.Centered(points, 6, 2);

        Assert.That(actual, Is.EqualTo(0.008142039609053464).Within(2.0e-15));
    }

    [Test]
    public void OneDimensionalCellCentersHaveKnownDiscrepancies()
    {
        double[] onePoint = [0.5];
        double[] twoPoints = [0.25, 0.75];

        Assert.Multiple(() =>
        {
            Assert.That(Discrepancy.Centered(onePoint, 1, 1), Is.EqualTo(1.0 / 12.0).Within(1.0e-15));
            Assert.That(Discrepancy.L2Star(onePoint, 1, 1), Is.EqualTo(Math.Sqrt(1.0 / 12.0)).Within(1.0e-15));
            Assert.That(Discrepancy.Star(onePoint, 1, 1), Is.EqualTo(0.5).Within(1.0e-15));
            Assert.That(Discrepancy.Centered(twoPoints, 2, 1), Is.EqualTo(1.0 / 48.0).Within(1.0e-15));
            Assert.That(Discrepancy.L2Star(twoPoints, 2, 1), Is.EqualTo(Math.Sqrt(1.0 / 48.0)).Within(1.0e-15));
            Assert.That(Discrepancy.Star(twoPoints, 2, 1), Is.EqualTo(0.25).Within(1.0e-15));
        });
    }

    [Test]
    public void ExactStarMatchesOneDimensionalEmpiricalCdfFormula()
    {
        double[] points = [0.8, 0.1, 0.4];

        double actual = Discrepancy.Star(points, 3, 1);

        Assert.That(actual, Is.EqualTo(2.0 / 3.0 - 0.4).Within(1.0e-15));
    }

    [Test]
    public void ExactStarHandlesClosedUnitHypercubeBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Discrepancy.Star(new double[] { 0.0, 1.0 }, 2, 1), Is.EqualTo(0.5));
            Assert.That(Discrepancy.Star(new double[] { 0.5, 0.5 }, 1, 2), Is.EqualTo(0.75));
            Assert.That(Discrepancy.Star(new double[] { 1.0, 0.0 }, 1, 2), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void DiagnosticsAreInvariantUnderPointAndCoordinatePermutations()
    {
        double[] original =
        [
            0.1, 0.4, 0.8,
            0.7, 0.2, 0.3,
            0.5, 0.9, 0.6,
            0.3, 0.6, 0.1
        ];
        double[] permuted =
        [
            0.1, 0.3, 0.6,
            0.8, 0.1, 0.4,
            0.3, 0.7, 0.2,
            0.6, 0.5, 0.9
        ];

        Assert.Multiple(() =>
        {
            Assert.That(Discrepancy.Centered(permuted, 4, 3), Is.EqualTo(Discrepancy.Centered(original, 4, 3)).Within(2.0e-15));
            Assert.That(Discrepancy.L2Star(permuted, 4, 3), Is.EqualTo(Discrepancy.L2Star(original, 4, 3)).Within(2.0e-15));
            Assert.That(Discrepancy.Star(permuted, 4, 3), Is.EqualTo(Discrepancy.Star(original, 4, 3)).Within(2.0e-15));
        });
    }

    [Test]
    public void RegularCoverageScoresBetterThanCoincidentCenterPoints()
    {
        double[] regular =
        [
            0.25, 0.25,
            0.25, 0.75,
            0.75, 0.25,
            0.75, 0.75
        ];
        double[] coincident = Enumerable.Repeat(0.5, 8).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(Discrepancy.Centered(regular, 4, 2), Is.LessThan(Discrepancy.Centered(coincident, 4, 2)));
            Assert.That(Discrepancy.L2Star(regular, 4, 2), Is.LessThan(Discrepancy.L2Star(coincident, 4, 2)));
            Assert.That(Discrepancy.Star(regular, 4, 2), Is.LessThan(Discrepancy.Star(coincident, 4, 2)));
        });
    }

    [Test]
    public void ArrayAndReadOnlySpanOverloadsMatchExactly()
    {
        double[] points = [0.2, 0.7, 0.8, 0.3];
        ReadOnlySpan<double> span = points;
        double centered = Discrepancy.Centered(span, 2, 2);
        double l2Star = Discrepancy.L2Star(span, 2, 2);
        double star = Discrepancy.Star(span, 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(centered, Is.EqualTo(Discrepancy.Centered(points, 2, 2)));
            Assert.That(l2Star, Is.EqualTo(Discrepancy.L2Star(points, 2, 2)));
            Assert.That(star, Is.EqualTo(Discrepancy.Star(points, 2, 2)));
        });
    }

    [Test]
    public void ExactStarCandidateBudgetIsEnforcedBeforeEnumeration()
    {
        double[] points =
        [
            0.1, 0.2,
            0.3, 0.4,
            0.5, 0.6,
            0.7, 0.8
        ];

        Assert.Throws<RandomExhaustedException>(() => Discrepancy.Star(points, 4, 2, 24L));
        Assert.That(Discrepancy.Star(points, 4, 2, 25L), Is.InRange(0.0, 1.0));
        AssertArgument("maximumBoxes", () => Discrepancy.Star(points, 4, 2, 0L));
    }

    [Test]
    public void InvalidSamplesAreRejectedConsistently()
    {
        AssertArgument("points", () => Discrepancy.Centered((double[])NullReference.Value<double[]>(), 1, 1));
        AssertArgument("points", () => Discrepancy.L2Star((double[])NullReference.Value<double[]>(), 1, 1));
        AssertArgument("points", () => Discrepancy.Star((double[])NullReference.Value<double[]>(), 1, 1));
        AssertArgument("pointCount", () => Discrepancy.Centered(Array.Empty<double>(), 0, 1));
        AssertArgument("dimension", () => Discrepancy.Centered(Array.Empty<double>(), 1, 0));
        AssertArgument("points", () => Discrepancy.Centered(new double[3], 2, 2));

        double[] invalid = [double.NaN, double.NegativeInfinity, -double.Epsilon, Math.BitIncrement(1.0), double.PositiveInfinity];
        foreach (double value in invalid)
        {
            AssertArgument("points", () => Discrepancy.Centered(new double[] { value }, 1, 1));
            AssertArgument("points", () => Discrepancy.L2Star(new double[] { value }, 1, 1));
            AssertArgument("points", () => Discrepancy.Star(new double[] { value }, 1, 1));
        }
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
