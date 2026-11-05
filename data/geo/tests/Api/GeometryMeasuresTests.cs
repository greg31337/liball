using geo;

namespace geo.Tests;

public class GeometryMeasuresTests
{
    [Test]
    public void PlanarAndThreeDimensionalMeasuresUseTheirDeclaredOrdinates()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry polygon = Read(
            "POLYGON Z ((0 0 0, 3 0 0, 0 4 12, 0 0 0))");

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(polygon), Is.EqualTo(6.0));
            Assert.That(measures.SurfaceArea3D(polygon),
                Is.EqualTo(3.0 * Math.Sqrt(160.0) / 2.0).Within(1.0e-14));
            Assert.That(measures.Length(polygon), Is.EqualTo(12.0));
            Assert.That(measures.Perimeter(polygon), Is.EqualTo(12.0));
        });
    }

    [Test]
    public void SurfaceAreaTreatsUnavailableZAsZeroAndSubtractsHoles()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry polygon = Read(
            "POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1))");

        Assert.That(measures.SurfaceArea3D(polygon), Is.EqualTo(20.0));
    }

    [Test]
    public void DistanceMethodsAndNearestPointsSharePlanarSemantics()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry point = Read("POINT Z (3 4 99)");
        Geometry line = Read("LINESTRING Z (0 0 0, 10 0 20)");

        double distance = measures.Distance(point, line);
        double indexedDistance = measures.IndexedDistance(point, line);
        double envelopeDistance = measures.EnvelopeDistance(point, line);
        DistanceResult nearest = measures.NearestPoints(point, line);
        point.Point!.Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(distance, Is.EqualTo(4.0));
            Assert.That(indexedDistance, Is.EqualTo(4.0));
            Assert.That(envelopeDistance, Is.EqualTo(4.0));
            Assert.That(nearest.Distance, Is.EqualTo(4.0));
            AssertCoordinate(nearest.FirstCoordinate,
                3.0, 4.0, 99.0, double.NaN);
            AssertCoordinate(nearest.SecondCoordinate,
                3.0, 0.0, 6.0, double.NaN);
            Assert.That(nearest.FirstLocation, Is.Null);
            AssertLocation(nearest.SecondLocation, 0, 0, 0.3);
        });
    }

    [Test]
    public void HausdorffMethodsExposeOwningDiscreteWitnessResults()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry line = Read("LINESTRING ZM (0 0 0 10, 10 0 20 30)");
        Geometry endpoints = Read(
            "MULTIPOINT ZM ((0 0 2 4), (10 0 6 8))");

        DistanceResult discrete = measures.HausdorffDistance(line, endpoints);
        DistanceResult densified = measures.DensifiedHausdorffDistance(
            line, endpoints, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(discrete.Distance, Is.Zero);
            Assert.That(densified.Distance, Is.EqualTo(5.0));
            AssertCoordinate(densified.FirstCoordinate,
                5.0, 0.0, 10.0, 20.0);
            AssertCoordinate(densified.SecondCoordinate,
                0.0, 0.0, 2.0, 4.0);
            AssertLocation(densified.FirstLocation, 0, 0, 0.5);
            Assert.That(densified.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void FrechetMethodsExposeOrderedAndDensifiedWitnessResults()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry first = Read("LINESTRING (0 0, 10 0)");
        Geometry reversed = Read("LINESTRING (10 0, 0 0)");
        Geometry subdivided = Read("LINESTRING (0 0, 5 0, 10 0)");

        DistanceResult reversedResult = measures.FrechetDistance(
            first, reversed);
        DistanceResult discrete = measures.FrechetDistance(
            first, subdivided);
        DistanceResult densified = measures.DensifiedFrechetDistance(
            first, subdivided, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(reversedResult.Distance, Is.EqualTo(10.0));
            AssertCoordinate(reversedResult.FirstCoordinate,
                0.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(reversedResult.SecondCoordinate,
                10.0, 0.0, double.NaN, double.NaN);
            Assert.That(discrete.Distance, Is.EqualTo(5.0));
            Assert.That(densified.Distance, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void CentroidPointOnSurfaceAndClearanceReturnOwningWitnesses()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry polygon = Read(
            "POLYGON Z ((0 0 0, 10 0 10, 10 10 20, 0 10 10, 0 0 0))");
        Geometry line = Read("LINESTRING (0 0, 10 0, 5 1)");

        Coordinate? centroid = measures.Centroid(polygon);
        Coordinate? pointOnSurface = measures.PointOnSurface(polygon);
        DistanceResult clearance = measures.MinimumClearance(line);
        polygon.Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            AssertCoordinate(centroid,
                5.0, 5.0, double.NaN, double.NaN);
            AssertCoordinate(pointOnSurface,
                5.0, 5.0, double.NaN, double.NaN);
            Assert.That(clearance.Distance, Is.EqualTo(1.0));
            AssertCoordinate(clearance.FirstCoordinate,
                5.0, 1.0, double.NaN, double.NaN);
            AssertCoordinate(clearance.SecondCoordinate,
                5.0, 0.0, double.NaN, double.NaN);
        });
    }

    [Test]
    public void EmptyMetricResultsUseZeroOrNoResultAsAppropriate()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry point = Read("POINT (1 2)");

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(empty), Is.Zero);
            Assert.That(measures.SurfaceArea3D(empty), Is.Zero);
            Assert.That(measures.Length(empty), Is.Zero);
            Assert.That(measures.Perimeter(empty), Is.Zero);
            Assert.That(measures.Distance(empty, point),
                Is.EqualTo(double.PositiveInfinity));
            Assert.That(measures.IndexedDistance(point, empty),
                Is.EqualTo(double.PositiveInfinity));
            Assert.That(double.IsNaN(
                measures.EnvelopeDistance(empty, point)), Is.True);
            Assert.That(measures.Centroid(empty), Is.Null);
            Assert.That(measures.PointOnSurface(empty), Is.Null);
            Assert.That(measures.NearestPoints(empty, point).HasResult,
                Is.False);
            Assert.That(measures.HausdorffDistance(empty, point).HasResult,
                Is.False);
            Assert.That(measures.FrechetDistance(point, empty).HasResult,
                Is.False);
            Assert.That(measures.MinimumClearance(point).HasResult,
                Is.False);
        });
    }

    [Test]
    public void PublicMeasuresForwardValidationFailures()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry first = Read("POINT (0 0)");
        Geometry second = Read("POINT (1 0)");
        first.SpatialReferenceId = 4326;
        second.SpatialReferenceId = 3857;

        Assert.Multiple(() =>
        {
            Assert.That(() => measures.Distance(first, second),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => measures.NearestPoints(first, second),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => measures.HausdorffDistance(first, second),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => measures.FrechetDistance(first, second),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => measures.DensifiedHausdorffDistance(
                Read("POINT (0 0)"), Read("POINT (1 0)"), 0.0),
                Throws.TypeOf<InvalidGeometryException>());
            Assert.That(() => measures.DensifiedFrechetDistance(
                Read("POINT (0 0)"), Read("POINT (1 0)"), 2.0),
                Throws.TypeOf<InvalidGeometryException>());
        });
    }

    private static void AssertLocation(
        LinearLocation? location, int componentIndex,
        int segmentIndex, double segmentFraction)
    {
        Assert.That(location, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(location!.ComponentIndex, Is.EqualTo(componentIndex));
            Assert.That(location.SegmentIndex, Is.EqualTo(segmentIndex));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(segmentFraction).Within(1.0e-15));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y, double z, double m)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
            AssertOrdinate(coordinate.Z, z);
            AssertOrdinate(coordinate.M, m);
        });
    }

    private static void AssertOrdinate(double actual, double expected)
    {
        if (double.IsNaN(expected))
        {
            Assert.That(double.IsNaN(actual), Is.True);
            return;
        }
        Assert.That(actual, Is.EqualTo(expected).Within(1.0e-14));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
