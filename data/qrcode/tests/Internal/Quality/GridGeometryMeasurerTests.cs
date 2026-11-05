using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GridGeometryMeasurerTests
{
    [TestCase(21, 21, 0)]
    [TestCase(27, 7, 0)]
    [TestCase(27, 7, 37)]
    [TestCase(11, 11, 90)]
    [TestCase(2, 2, 180)]
    public void RegularGridsRemainUniformRegardlessOfShapeRotationOrTranslation(int columns, int rows, double angle)
    {
        double radians = angle * Math.PI / 180;
        QrPoint[] centers = Grid(columns, rows, 4 * Math.Cos(radians), 4 * Math.Sin(radians),
            -4 * Math.Sin(radians), 4 * Math.Cos(radians), 1234, -5678);
        GridGeometryMeasurement result = Measure(centers, columns, rows);
        Assert.That(result.SampleCount, Is.EqualTo(columns * rows));
        Assert.That(result.HorizontalPitch, Is.EqualTo(4).Within(1e-10));
        Assert.That(result.VerticalPitch, Is.EqualTo(4).Within(1e-10));
        Assert.That(result.AxialNonUniformity.Value, Is.Zero.Within(1e-10));
        Assert.That(result.GridNonUniformity.Value, Is.Zero.Within(1e-10));
        Assert.That(result.GeometricDistortion.Value, Is.Zero.Within(1e-10));
        Assert.That(result.RootMeanSquareDisplacement, Is.Zero.Within(1e-10));
        Assert.That(result.ModuleDisplacements, Has.Length.EqualTo(columns * rows));
        AssertMetrics(result, true);
    }

    [TestCase(4, 6)]
    [TestCase(6, 4)]
    public void MeasuresAxisStretchSeparatelyFromLocalDisplacement(double horizontal, double vertical)
    {
        GridGeometryMeasurement result = Measure(Grid(9, 5, horizontal, 0, 0, vertical, 0, 0), 9, 5);
        Assert.That(result.HorizontalPitch, Is.EqualTo(horizontal).Within(1e-12));
        Assert.That(result.VerticalPitch, Is.EqualTo(vertical).Within(1e-12));
        Assert.That(result.AxialNonUniformity.Value, Is.EqualTo(0.4).Within(1e-12));
        Assert.That(result.GridNonUniformity.Value, Is.Zero.Within(1e-12));
        Assert.That(result.GeometricDistortion.Value, Is.Zero.Within(1e-12));
    }

    [TestCase(2)]
    [TestCase(-2)]
    public void MeasuresShearWithoutConfusingItWithLocalWarp(double shear)
    {
        GridGeometryMeasurement result = Measure(Grid(9, 5, 4, 0, shear, 4, 0, 0), 9, 5);
        Assert.That(result.GeometricDistortion.Value, Is.EqualTo(Math.Atan(0.5) * 180 / Math.PI).Within(1e-10));
        Assert.That(result.HorizontalPitch, Is.EqualTo(4).Within(1e-12));
        Assert.That(result.VerticalPitch, Is.EqualTo(Math.Sqrt(20)).Within(1e-12));
        Assert.That(result.GridNonUniformity.Value, Is.Zero.Within(1e-12));
    }

    [Test]
    public void LocatesAnInteriorDisplacementAndReportsItsFittedResidual()
    {
        QrPoint[] centers = Grid(3, 3, 4, 0, 0, 4, 10, 20);
        centers[4].X += 1;
        GridGeometryMeasurement result = Measure(centers, 3, 3);
        Assert.That(result.GridNonUniformity.Value, Is.EqualTo(2.0 / 9).Within(1e-12));
        Assert.That(result.ModuleDisplacements[4], Is.EqualTo(2.0 / 9).Within(1e-12));
        for (int index = 0; index < centers.Length; index++)
            if (index != 4) Assert.That(result.ModuleDisplacements[index], Is.EqualTo(1.0 / 36).Within(1e-12));
        Assert.That(result.RootMeanSquareDisplacement, Is.EqualTo(Math.Sqrt(8) / 36).Within(1e-12));
        Assert.That(result.AxialNonUniformity.Value, Is.Zero.Within(1e-12));
        Assert.That(result.GeometricDistortion.Value, Is.Zero.Within(1e-12));
    }

    [Test]
    public void MeasuresNonlinearBowingAcrossTheGrid()
    {
        QrPoint[] centers = Grid(5, 5, 1, 0, 0, 1, 0, 0);
        for (int row = 0; row < 5; row++)
            for (int column = 0; column < 5; column++) centers[row * 5 + column].Y += 0.2 * (column - 2) * (column - 2);
        GridGeometryMeasurement result = Measure(centers, 5, 5);
        Assert.That(result.GridNonUniformity.Value, Is.EqualTo(0.4).Within(1e-12));
        Assert.That(result.RootMeanSquareDisplacement, Is.EqualTo(Math.Sqrt(0.112)).Within(1e-12));
        Assert.That(result.GeometricDistortion.Value, Is.Zero.Within(1e-12));
    }

    [Test]
    public void PerspectiveDistortionCannotDisappearIntoTheAffineFit()
    {
        PerspectiveTransform transform = new PerspectiveTransform { M00 = 4, M11 = 4, M20 = 0.02, M21 = 0.01, M22 = 1 };
        QrPoint[] centers = Grid(9, 7, 1, 0, 0, 1, 0.5, 0.5).Select(transform.MapPoint).ToArray();
        GridGeometryMeasurement result = Measure(centers, 9, 7);
        Assert.That(result.GridNonUniformity.Value, Is.GreaterThan(0.05));
        Assert.That(result.RootMeanSquareDisplacement, Is.GreaterThan(0));
        Assert.That(result.RootMeanSquareDisplacement, Is.LessThanOrEqualTo(result.GridNonUniformity.Value));
        AssertMetrics(result, true);
    }

    [Test]
    public void NormalizedMeasurementsAreInvariantUnderUniformScaleRotationAndReflection()
    {
        QrPoint[] centers = Grid(5, 3, 4, 0, 1, 6, 0, 0);
        centers[7].Y += 0.25;
        GridGeometryMeasurement original = Measure(centers, 5, 3);
        QrPoint[] transformed = centers.Select(p => new QrPoint { X = 1000 - 3 * p.Y, Y = -500 - 3 * p.X }).ToArray();
        GridGeometryMeasurement moved = Measure(transformed, 5, 3);
        Assert.That(moved.AxialNonUniformity.Value, Is.EqualTo(original.AxialNonUniformity.Value).Within(1e-12));
        Assert.That(moved.GeometricDistortion.Value, Is.EqualTo(original.GeometricDistortion.Value).Within(1e-10));
        Assert.That(moved.ModuleDisplacements, Is.EqualTo(original.ModuleDisplacements).Within(1e-12));
        Assert.That(moved.HorizontalPitch, Is.EqualTo(3 * original.HorizontalPitch).Within(1e-12));
        Assert.That(moved.VerticalPitch, Is.EqualTo(3 * original.VerticalPitch).Within(1e-12));
    }

    [TestCase(1e-150)]
    [TestCase(1e150)]
    public void NormalizesCoordinateScaleBeforeSquaredDistanceCalculations(double scale)
    {
        GridGeometryMeasurement result = Measure(Grid(3, 3, 4 * scale, 0, 0, 4 * scale, 0, 0), 3, 3);
        Assert.That(result.HorizontalPitch / scale, Is.EqualTo(4).Within(1e-12));
        Assert.That(result.VerticalPitch / scale, Is.EqualTo(4).Within(1e-12));
        Assert.That(result.GridNonUniformity.Value, Is.Zero.Within(1e-12));
        AssertMetrics(result, true);
    }

    [Test]
    public void PreservesSmallDisplacementsFarFromTheOrigin()
    {
        QrPoint[] centers = Grid(3, 3, 4, 0, 0, 4, 1e12, -1e12);
        centers[4].X += 1;
        Assert.That(Measure(centers, 3, 3).GridNonUniformity.Value, Is.EqualTo(2.0 / 9).Within(1e-12));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DegenerateAxesReturnUncalculatedMetrics(bool collapsed)
    {
        QrPoint[] centers = Grid(3, 3, collapsed ? 0 : 1, 0, collapsed ? 0 : 1, 0, 10, 20);
        GridGeometryMeasurement result = Measure(centers, 3, 3);
        AssertMetrics(result, false);
        Assert.That(result.ModuleDisplacements, Is.Empty);
        Assert.That(result.AxialNonUniformity.Limitation, Does.Contain("independent grid axes"));
    }

    [Test]
    public void HonorsDisableFlagAndDoesNotMutateInputsOrRetainMutableResults()
    {
        QrPoint[] centers = Grid(5, 3, 4, 0, 0, 4, 10, 20);
        double[] before = centers.SelectMany(p => new[] { p.X, p.Y }).ToArray();
        QrQualityOptions options = new QrQualityOptions { AnalyzeGridUniformity = false, RequireCalibratedInputs = true };
        GridGeometryMeasurement disabled = GridGeometryMeasurer.Measure(centers, 5, 3, options);
        AssertMetrics(disabled, false);
        Assert.That(disabled.ModuleDisplacements, Is.Empty);
        Assert.That(options.AnalyzeGridUniformity, Is.False);
        GridGeometryMeasurement first = Measure(centers, 5, 3);
        first.ModuleDisplacements[0] = 100;
        first.AxialNonUniformity.Value = 100;
        Assert.That(Measure(centers, 5, 3).ModuleDisplacements[0], Is.Zero.Within(1e-12));
        Assert.That(centers.SelectMany(p => new[] { p.X, p.Y }), Is.EqualTo(before));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void RejectsNonfiniteCoordinatesEvenWhenDisabled(double value)
    {
        QrPoint[] centers = Grid(2, 2, 1, 0, 0, 1, 0, 0);
        centers[3].Y = value;
        Assert.Throws<QrInputException>(() => GridGeometryMeasurer.Measure(centers, 2, 2, new QrQualityOptions { AnalyzeGridUniformity = false }));
    }

    [Test]
    public void ValidatesDimensionsMissingPointsAndLimitsBeforeFitting()
    {
        QrPoint[] centers = Grid(2, 2, 1, 0, 0, 1, 0, 0);
        Assert.Throws<QrInputException>(() => Measure(null!, 2, 2));
        Assert.Throws<QrInputException>(() => GridGeometryMeasurer.Measure(centers, 2, 2, null!));
        Assert.Throws<QrInputException>(() => Measure(centers, 1, 4));
        Assert.Throws<QrInputException>(() => Measure(centers, 2, 3));
        Assert.Throws<QrInputException>(() => GridGeometryMeasurer.Measure(centers, 2, 2, new QrQualityOptions { MaximumModuleSamples = 0 }));
        Assert.Throws<QrResourceLimitException>(() => GridGeometryMeasurer.Measure(centers, 2, 2, new QrQualityOptions { MaximumModuleSamples = 3 }));
        Assert.That(GridGeometryMeasurer.Measure(centers, 2, 2, new QrQualityOptions { MaximumModuleSamples = 4 }).SampleCount, Is.EqualTo(4));
        centers[3] = null!;
        Assert.Throws<QrInputException>(() => Measure(centers, 2, 2));
        QrPoint[] excessive = new QrPoint[2048 * 1024];
        Assert.Throws<QrResourceLimitException>(() => GridGeometryMeasurer.Measure(excessive, 2048, 1024,
            new QrQualityOptions { MaximumModuleSamples = excessive.Length }));
        centers = Grid(2, 2, 1, 0, 0, 1, 0, 0);
        centers[0].X = -double.MaxValue;
        centers[1].X = double.MaxValue;
        Assert.Throws<QrInputException>(() => Measure(centers, 2, 2));
    }

    static GridGeometryMeasurement Measure(QrPoint[] centers, int columns, int rows) =>
        GridGeometryMeasurer.Measure(centers, columns, rows, new QrQualityOptions());

    static QrPoint[] Grid(int columns, int rows, double ux, double uy, double vx, double vy, double x, double y) =>
        Enumerable.Range(0, columns * rows).Select(i => new QrPoint
        {
            X = x + (i % columns) * ux + (i / columns) * vx,
            Y = y + (i % columns) * uy + (i / columns) * vy
        }).ToArray();

    static void AssertMetrics(GridGeometryMeasurement result, bool calculated)
    {
        foreach (QrQualityMetric metric in new[] { result.AxialNonUniformity, result.GridNonUniformity, result.GeometricDistortion })
        {
            Assert.That(metric.IsCalculated, Is.EqualTo(calculated));
            Assert.That(metric.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
            Assert.That(metric.UsesCalibratedInputs, Is.False);
            Assert.That(metric.Limitation, Is.Not.Null.And.Not.Empty);
            Assert.That(double.IsFinite(metric.Value), Is.True);
        }
    }
}
