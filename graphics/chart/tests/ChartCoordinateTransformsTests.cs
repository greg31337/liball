namespace chart.Tests;

[TestFixture]
public class ChartCoordinateTransformsTests
{
    [Test]
    public void TernaryTransform_NormalizesMapsAndInvertsComponents()
    {
        ChartTernaryTransform transform = new ChartTernaryTransform(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(2.0, 0.0),
            new ChartPoint(0.0, 2.0));

        ChartPoint point = transform.Map(2.0, 1.0, 1.0);
        double[] components = transform.Invert(point);

        Assert.That(point.GetX(), Is.EqualTo(0.5).Within(0.000000001));
        Assert.That(point.GetY(), Is.EqualTo(0.5).Within(0.000000001));
        Assert.That(components, Is.EqualTo(new double[] { 0.5, 0.25, 0.25 }).Within(0.000000001));
        Assert.That(components[0] + components[1] + components[2], Is.EqualTo(1.0).Within(0.000000001));
    }

    [Test]
    public void TernaryTransform_CopiesVerticesAndMapsEachVertex()
    {
        ChartPoint first = new ChartPoint(1.0, 1.0);
        ChartTernaryTransform transform = new ChartTernaryTransform(
            first,
            new ChartPoint(5.0, 1.0),
            new ChartPoint(1.0, 5.0));
        first.SetX(100.0);
        ChartPoint returned = transform.GetFirstVertex();
        returned.SetY(100.0);

        AssertPoint(transform.Map(1.0, 0.0, 0.0), 1.0, 1.0);
        AssertPoint(transform.Map(0.0, 1.0, 0.0), 5.0, 1.0);
        AssertPoint(transform.Map(0.0, 0.0, 1.0), 1.0, 5.0);
        AssertPoint(transform.GetFirstVertex(), 1.0, 1.0);
    }

    [Test]
    public void EquirectangularProjection_UsesOriginAndStandardParallel()
    {
        ChartGeographicProjection projection = new ChartGeographicProjection(
            ChartGeographicProjectionKind.Equirectangular,
            10.0,
            20.0,
            30.0,
            0.0);

        ChartPoint point = projection.Project(40.0, 25.0);
        ChartPoint coordinate = projection.Invert(point);

        Assert.That(point.GetX(), Is.EqualTo(System.Math.PI / 6.0 * System.Math.Cos(System.Math.PI / 6.0)).Within(0.000000001));
        Assert.That(point.GetY(), Is.EqualTo(5.0 * System.Math.PI / 180.0).Within(0.000000001));
        AssertPoint(coordinate, 40.0, 25.0);
        Assert.That(projection.GetKind(), Is.EqualTo(ChartGeographicProjectionKind.Equirectangular));
        Assert.That(projection.GetCentralLongitude(), Is.EqualTo(10.0));
        Assert.That(projection.GetCentralLatitude(), Is.EqualTo(20.0));
        Assert.That(projection.GetFirstStandardParallel(), Is.EqualTo(30.0));
        Assert.That(projection.GetSecondStandardParallel(), Is.EqualTo(0.0));
    }

    [Test]
    public void MercatorProjection_RoundTripsCoordinates()
    {
        ChartGeographicProjection projection = new ChartGeographicProjection(
            ChartGeographicProjectionKind.Mercator,
            -15.0,
            10.0,
            0.0,
            0.0);

        ChartPoint point = projection.Project(45.0, 40.0);
        ChartPoint coordinate = projection.Invert(point);

        Assert.That(point.GetX(), Is.EqualTo(System.Math.PI / 3.0).Within(0.000000001));
        AssertPoint(coordinate, 45.0, 40.0);
    }

    [Test]
    public void OrthographicProjection_MapsVisibleHemisphereAndInvertsDisk()
    {
        ChartGeographicProjection projection = new ChartGeographicProjection(
            ChartGeographicProjectionKind.Orthographic,
            0.0,
            0.0,
            0.0,
            0.0);

        ChartPoint point = projection.Project(30.0, 20.0);
        ChartPoint coordinate = projection.Invert(point);

        Assert.That(point.GetX(), Is.EqualTo(System.Math.Cos(20.0 * System.Math.PI / 180.0) * 0.5).Within(0.000000001));
        Assert.That(point.GetY(), Is.EqualTo(System.Math.Sin(20.0 * System.Math.PI / 180.0)).Within(0.000000001));
        AssertPoint(coordinate, 30.0, 20.0);
        AssertPoint(projection.Invert(new ChartPoint(0.0, 0.0)), 0.0, 0.0);
    }

    [Test]
    public void AlbersLikeProjection_RoundTripsNorthernAndSouthernCones()
    {
        ChartGeographicProjection northern = new ChartGeographicProjection(
            ChartGeographicProjectionKind.AlbersLike,
            -96.0,
            23.0,
            20.0,
            50.0);
        ChartGeographicProjection southern = new ChartGeographicProjection(
            ChartGeographicProjectionKind.AlbersLike,
            135.0,
            -25.0,
            -18.0,
            -36.0);

        ChartPoint northernCoordinate = northern.Invert(northern.Project(-75.0, 35.0));
        ChartPoint southernCoordinate = southern.Invert(southern.Project(150.0, -33.0));

        AssertPoint(northernCoordinate, -75.0, 35.0);
        AssertPoint(southernCoordinate, 150.0, -33.0);
    }

    [Test]
    public void CoordinateTransforms_RejectInvalidInputsAndUndefinedInverses()
    {
        Assert.Throws<System.ArgumentNullException>(() => new ChartTernaryTransform(null!, new ChartPoint(1.0, 0.0), new ChartPoint(0.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => new ChartTernaryTransform(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0), new ChartPoint(2.0, 2.0)));
        ChartTernaryTransform ternary = new ChartTernaryTransform(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 0.0), new ChartPoint(0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ternary.Map(-1.0, 1.0, 1.0));
        Assert.Throws<System.ArgumentException>(() => ternary.Map(0.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => ternary.Invert(null!));

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartGeographicProjection((ChartGeographicProjectionKind)99, 0.0, 0.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => new ChartGeographicProjection(ChartGeographicProjectionKind.Equirectangular, 0.0, 0.0, 90.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => new ChartGeographicProjection(ChartGeographicProjectionKind.AlbersLike, 0.0, 0.0, -20.0, 20.0));
        ChartGeographicProjection mercator = new ChartGeographicProjection(ChartGeographicProjectionKind.Mercator, 0.0, 0.0, 0.0, 0.0);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => mercator.Project(0.0, 90.0));
        ChartGeographicProjection orthographic = new ChartGeographicProjection(ChartGeographicProjectionKind.Orthographic, 0.0, 0.0, 0.0, 0.0);
        Assert.Throws<System.ArgumentException>(() => orthographic.Project(180.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => orthographic.Invert(new ChartPoint(1.01, 0.0)));
        Assert.Throws<System.ArgumentNullException>(() => orthographic.Invert(null!));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }
}
