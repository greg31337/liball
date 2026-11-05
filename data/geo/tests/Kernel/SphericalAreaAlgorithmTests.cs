using geo;

namespace geo.Tests;

public class SphericalAreaAlgorithmTests
{
    [Test]
    public void OctantTriangleHasExactSphericalExcessInEveryUnit()
    {
        Geometry triangle = Read(
            "POLYGON ((0 0, 90 0, 0 90, 0 0))");
        double steradians = Math.PI / 2.0;
        double squareMeters = steradians *
            GeographicMath.EarthRadiusMeters *
            GeographicMath.EarthRadiusMeters;

        Assert.Multiple(() =>
        {
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                triangle, SpatialUnit.Radians),
                Is.EqualTo(steradians).Within(1.0e-14));
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                triangle, SpatialUnit.Degrees),
                Is.EqualTo(steradians * 180.0 * 180.0 /
                    (Math.PI * Math.PI)).Within(1.0e-10));
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                triangle, SpatialUnit.Meters),
                Is.EqualTo(squareMeters).Within(1.0));
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                triangle, SpatialUnit.Kilometers),
                Is.EqualTo(squareMeters / 1_000_000.0)
                    .Within(1.0e-6));
        });
    }

    [Test]
    public void RingWindingDoesNotChangePolygonArea()
    {
        Geometry counterClockwise = Read(
            "POLYGON ((0 0, 20 0, 20 10, 0 10, 0 0))");
        Geometry clockwise = Read(
            "POLYGON ((0 0, 0 10, 20 10, 20 0, 0 0))");

        double first = SphericalAreaAlgorithm.GetSphericalArea(
            counterClockwise, SpatialUnit.Radians);
        double second = SphericalAreaAlgorithm.GetSphericalArea(
            clockwise, SpatialUnit.Radians);

        Assert.That(second, Is.EqualTo(first).Within(1.0e-14));
    }

    [Test]
    public void HolesAreSubtractedRegardlessOfTheirWinding()
    {
        Geometry shell = Read(
            "POLYGON ((-30 -20, 30 -20, 30 20, -30 20, -30 -20))");
        Geometry hole = Read(
            "POLYGON ((-10 -5, 10 -5, 10 5, -10 5, -10 -5))");
        Geometry firstDonut = Read(
            "POLYGON ((-30 -20, 30 -20, 30 20, -30 20, -30 -20), " +
            "(-10 -5, -10 5, 10 5, 10 -5, -10 -5))");
        Geometry secondDonut = Read(
            "POLYGON ((-30 -20, 30 -20, 30 20, -30 20, -30 -20), " +
            "(-10 -5, 10 -5, 10 5, -10 5, -10 -5))");
        double expected = SphericalAreaAlgorithm.GetSphericalArea(
            shell, SpatialUnit.Radians) -
            SphericalAreaAlgorithm.GetSphericalArea(
                hole, SpatialUnit.Radians);

        Assert.Multiple(() =>
        {
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                firstDonut, SpatialUnit.Radians),
                Is.EqualTo(expected).Within(1.0e-14));
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                secondDonut, SpatialUnit.Radians),
                Is.EqualTo(expected).Within(1.0e-14));
        });
    }

    [Test]
    public void MultiPolygonAreaIsTheCompensatedSumOfComponents()
    {
        Geometry first = Read(
            "POLYGON ((-10 0, 0 0, 0 10, -10 10, -10 0))");
        Geometry second = Read(
            "POLYGON ((30 0, 45 0, 45 10, 30 10, 30 0))");
        Geometry multiPolygon = Read(
            "MULTIPOLYGON (((-10 0, 0 0, 0 10, -10 10, -10 0)), " +
            "((30 0, 45 0, 45 10, 30 10, 30 0)))");
        double expected = SphericalAreaAlgorithm.GetSphericalArea(
            first, SpatialUnit.Radians) +
            SphericalAreaAlgorithm.GetSphericalArea(
                second, SpatialUnit.Radians);

        Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
            multiPolygon, SpatialUnit.Radians),
            Is.EqualTo(expected).Within(1.0e-14));
    }

    [Test]
    public void AntimeridianCrossingUsesTheShortWrappedEdges()
    {
        Geometry crossing = Read(
            "POLYGON ((170 0, -170 0, -170 10, 170 10, 170 0))");
        Geometry unwrapped = Read(
            "POLYGON ((170 0, 190 0, 190 10, 170 10, 170 0))");
        Geometry rotated = Read(
            "POLYGON ((-10 0, 10 0, 10 10, -10 10, -10 0))");

        double crossingArea = SphericalAreaAlgorithm.GetSphericalArea(
            crossing, SpatialUnit.Radians);

        Assert.Multiple(() =>
        {
            Assert.That(crossingArea,
                Is.EqualTo(SphericalAreaAlgorithm.GetSphericalArea(
                    unwrapped, SpatialUnit.Radians)).Within(1.0e-14));
            Assert.That(crossingArea,
                Is.EqualTo(SphericalAreaAlgorithm.GetSphericalArea(
                    rotated, SpatialUnit.Radians)).Within(1.0e-14));
            Assert.That(crossingArea,
                Is.GreaterThan(0.0).And.LessThan(0.2));
        });
    }

    [Test]
    public void AntimeridianCrossingHoleUsesTheSameWrappedConvention()
    {
        Geometry shell = Read(
            "POLYGON ((160 -20, -160 -20, -160 20, 160 20, 160 -20))");
        Geometry hole = Read(
            "POLYGON ((170 -10, -170 -10, -170 10, 170 10, 170 -10))");
        Geometry donut = Read(
            "POLYGON ((160 -20, -160 -20, -160 20, 160 20, 160 -20), " +
            "(170 -10, 170 10, -170 10, -170 -10, 170 -10))");
        double expected = SphericalAreaAlgorithm.GetSphericalArea(
            shell, SpatialUnit.Radians) -
            SphericalAreaAlgorithm.GetSphericalArea(
                hole, SpatialUnit.Radians);

        Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
            donut, SpatialUnit.Radians),
            Is.EqualTo(expected).Within(1.0e-14));
    }

    [Test]
    public void PolarRingProducesTheSmallPoleCap()
    {
        Geometry polar = Read(
            "POLYGON ((0 80, 90 80, 180 80, -90 80, 0 80))");
        double area = SphericalAreaAlgorithm.GetSphericalArea(
            polar, SpatialUnit.Radians);
        double smallCircleCap = 2.0 * Math.PI *
            (1.0 - Math.Sin(80.0 * Math.PI / 180.0));

        Assert.That(area,
            Is.GreaterThan(0.0).And.LessThan(smallCircleCap));
    }

    [Test]
    public void EmptyPolygonalValuesHaveZeroArea()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                Read("POLYGON EMPTY"), SpatialUnit.Meters), Is.Zero);
            Assert.That(SphericalAreaAlgorithm.GetSphericalArea(
                Read("MULTIPOLYGON EMPTY"), SpatialUnit.Feet), Is.Zero);
        });
    }

    [Test]
    public void InvalidOrNonPolygonalInputsAreRejected()
    {
        Geometry invalidLatitude = Read(
            "POLYGON ((0 0, 10 0, 0 91, 0 0))");
        Geometry antipodalEdge = Read(
            "POLYGON ((0 0, 180 0, 90 10, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                SphericalAreaAlgorithm.GetSphericalArea(
                    Read("LINESTRING (0 0, 1 1)"),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                SphericalAreaAlgorithm.GetSphericalArea(
                    invalidLatitude, SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                SphericalAreaAlgorithm.GetSphericalArea(
                    antipodalEdge, SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                SphericalAreaAlgorithm.GetSphericalArea(
                    Read("POLYGON EMPTY"), (SpatialUnit)99));
            Assert.Throws<InvalidGeometryException>(() =>
                SphericalAreaAlgorithm.GetSphericalArea(
                    null!, SpatialUnit.Meters));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
