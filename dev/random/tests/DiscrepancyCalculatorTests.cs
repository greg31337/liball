namespace random.Tests;

public class DiscrepancyCalculatorTests
{
    [Test]
    public void ArrayAndSpanFormsMatchKnownOneDimensionalValues()
    {
        IDiscrepancyCalculator calculator = new DiscrepancyCalculator();
        double[] onePoint = [0.5];
        double[] twoPoints = [0.25, 0.75];

        Assert.Multiple(() =>
        {
            Assert.That(calculator.Centered(onePoint, 1, 1), Is.EqualTo(1.0 / 12.0).Within(1.0e-15));
            Assert.That(calculator.CenteredSpan(onePoint, 1, 1), Is.EqualTo(1.0 / 12.0).Within(1.0e-15));
            Assert.That(calculator.L2Star(onePoint, 1, 1), Is.EqualTo(Math.Sqrt(1.0 / 12.0)).Within(1.0e-15));
            Assert.That(calculator.L2StarSpan(onePoint, 1, 1), Is.EqualTo(Math.Sqrt(1.0 / 12.0)).Within(1.0e-15));
            Assert.That(calculator.ExactStar(onePoint, 1, 1, 2L), Is.EqualTo(0.5).Within(1.0e-15));
            Assert.That(calculator.ExactStarSpan(onePoint, 1, 1, 2L), Is.EqualTo(0.5).Within(1.0e-15));
            Assert.That(calculator.Centered(twoPoints, 2, 1), Is.EqualTo(1.0 / 48.0).Within(1.0e-15));
            Assert.That(calculator.L2Star(twoPoints, 2, 1), Is.EqualTo(Math.Sqrt(1.0 / 48.0)).Within(1.0e-15));
            Assert.That(calculator.ExactStar(twoPoints, 2, 1, 3L), Is.EqualTo(0.25).Within(1.0e-15));
        });
    }

    [Test]
    public void NonzeroOffsetSpansUseOnlyTheRequestedRowMajorCoordinates()
    {
        IDiscrepancyCalculator calculator = new DiscrepancyCalculator();
        double[] points = [0.1, 0.2, 0.8, 0.3, 0.4, 0.9];
        double[] storage = [-100.0, 0.1, 0.2, 0.8, 0.3, 0.4, 0.9, 100.0];
        ReadOnlySpan<double> span = storage.AsSpan(1, points.Length);
        double centered = calculator.CenteredSpan(span, 3, 2);
        double l2Star = calculator.L2StarSpan(span, 3, 2);
        double exactStar = calculator.ExactStarSpan(span, 3, 2, 16L);

        Assert.Multiple(() =>
        {
            Assert.That(centered, Is.EqualTo(calculator.Centered(points, 3, 2)));
            Assert.That(l2Star, Is.EqualTo(calculator.L2Star(points, 3, 2)));
            Assert.That(exactStar, Is.EqualTo(calculator.ExactStar(points, 3, 2, 16L)));
        });
    }

    [Test]
    public void ExactStarHonorsTheCandidateBudgetBoundary()
    {
        IDiscrepancyCalculator calculator = new DiscrepancyCalculator();
        double[] points = [0.1, 0.2, 0.3, 0.4, 0.6, 0.7, 0.8, 0.9];

        Assert.Throws<RandomExhaustedException>(() => calculator.ExactStar(points, 4, 2, 24L));
        Assert.Throws<RandomExhaustedException>(() => calculator.ExactStarSpan(points.AsSpan(), 4, 2, 24L));
        double arrayResult = calculator.ExactStar(points, 4, 2, 25L);
        double spanResult = calculator.ExactStarSpan(points.AsSpan(), 4, 2, 25L);

        Assert.Multiple(() =>
        {
            Assert.That(arrayResult, Is.InRange(0.0, 1.0));
            Assert.That(spanResult, Is.EqualTo(arrayResult));
        });
    }

    [Test]
    public void InvalidShapesCoordinatesAndBudgetsRetainArgumentNames()
    {
        IDiscrepancyCalculator calculator = new DiscrepancyCalculator();

        AssertArgument("points", () => calculator.Centered(NullReference.Value<double[]>(), 1, 1));
        AssertArgument("points", () => calculator.L2Star(NullReference.Value<double[]>(), 1, 1));
        AssertArgument("points", () => calculator.ExactStar(NullReference.Value<double[]>(), 1, 1, 1L));
        AssertArgument("pointCount", () => calculator.CenteredSpan(ReadOnlySpan<double>.Empty, 0, 1));
        AssertArgument("dimension", () => calculator.L2StarSpan(ReadOnlySpan<double>.Empty, 1, 0));
        AssertArgument("points", () => calculator.ExactStarSpan(new double[3], 2, 2, 16L));
        AssertArgument("points", () => calculator.Centered([-0.01], 1, 1));
        AssertArgument("points", () => calculator.L2Star([1.01], 1, 1));
        AssertArgument("maximumBoxes", () => calculator.ExactStar([0.5], 1, 1, 0L));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
