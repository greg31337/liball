namespace barcode.Tests;

public class GridSamplerTests
{
    [Test]
    public void IdentityGridSamplingPreservesModules()
    {
        ModuleMatrix source = CreateSourceMatrix();

        ModuleMatrix sampled = GridSampler.SampleGrid(source, 3, 3, PerspectiveTransform.CreateIdentity());

        Assert.That(sampled.CopyModules(), Is.EqualTo(source.CopyModules()));
    }

    [Test]
    public void TranslatedGridSamplingSelectsRequestedSourceRegion()
    {
        ModuleMatrix source = CreateSourceMatrix();
        PerspectiveTransform translation = new PerspectiveTransform(
            1.0, 0.0, 1.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0);

        ModuleMatrix sampled = GridSampler.SampleGrid(source, 2, 2, translation);

        Assert.That(sampled.CopyModules(), Is.EqualTo(new bool[]
        {
            false, true,
            true, false
        }));
    }

    [Test]
    public void QuadrilateralGridSamplingMapsCompleteSourceBounds()
    {
        ModuleMatrix source = CreateSourceMatrix();
        BarcodeQuadrilateral bounds = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(3.0, 0.0),
            GeometryMath.CreatePoint(3.0, 3.0),
            GeometryMath.CreatePoint(0.0, 3.0));

        ModuleMatrix sampled = GridSampler.SampleQuadrilateralGrid(source, 3, 3, bounds);

        Assert.That(sampled.CopyModules(), Is.EqualTo(source.CopyModules()));
    }

    [Test]
    public void EdgeNudgingClampsOnlyPointsWithinOnePixelOfBounds()
    {
        BarcodePoint[] points = new BarcodePoint[]
        {
            GeometryMath.CreatePoint(-0.25, 1.0),
            GeometryMath.CreatePoint(3.0, 3.0),
            GeometryMath.CreatePoint(1.5, -0.5)
        };

        GridSampler.NudgePointsToBounds(points, 3, 3);

        Assert.Multiple(() =>
        {
            Assert.That(points[0].X, Is.EqualTo(0.0));
            Assert.That(points[0].Y, Is.EqualTo(1.0));
            Assert.That(points[1].X, Is.EqualTo(2.0));
            Assert.That(points[1].Y, Is.EqualTo(2.0));
            Assert.That(points[2].X, Is.EqualTo(1.5));
            Assert.That(points[2].Y, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void GridSamplingRejectsInvalidDimensionsTransformsAndDistantPoints()
    {
        ModuleMatrix source = CreateSourceMatrix();
        PerspectiveTransform outside = new PerspectiveTransform(
            1.0, 0.0, 10.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0);
        BarcodePoint[] distant = new BarcodePoint[] { GeometryMath.CreatePoint(-1.01, 0.0) };

        Assert.Multiple(() =>
        {
            Assert.That(() => GridSampler.SampleGrid(null!, 1, 1, PerspectiveTransform.CreateIdentity()), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GridSampler.SampleGrid(source, 0, 1, PerspectiveTransform.CreateIdentity()), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GridSampler.SampleGrid(source, DecodingContext.DefaultMaximumModules + 1, 1, PerspectiveTransform.CreateIdentity()), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GridSampler.SampleGrid(source, 1, 1, null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GridSampler.SampleGrid(source, 1, 1, outside), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => GridSampler.NudgePointsToBounds(distant, 3, 3), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => GridSampler.NudgePointsToBounds(null!, 3, 3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GridSampler.NudgePointsToBounds(new BarcodePoint[0], 0, 3), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void EdgeNudgingAcceptsExactOnePixelToleranceOnEverySide()
    {
        BarcodePoint[] points = new BarcodePoint[]
        {
            GeometryMath.CreatePoint(-1.0, 1.0),
            GeometryMath.CreatePoint(3.0, 1.0),
            GeometryMath.CreatePoint(1.0, -1.0),
            GeometryMath.CreatePoint(1.0, 3.0)
        };

        GridSampler.NudgePointsToBounds(points, 3, 3);

        Assert.Multiple(() =>
        {
            Assert.That(points[0].X, Is.Zero);
            Assert.That(points[1].X, Is.EqualTo(2.0));
            Assert.That(points[2].Y, Is.Zero);
            Assert.That(points[3].Y, Is.EqualTo(2.0));
        });
    }

    private static ModuleMatrix CreateSourceMatrix()
    {
        ModuleMatrix matrix = new ModuleMatrix(3, 3);
        matrix.LoadModules(new bool[]
        {
            true, false, true,
            false, true, false,
            true, false, true
        });
        return matrix;
    }
}
