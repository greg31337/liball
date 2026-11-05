using geo;

namespace geo.Tests;

public class PolygonHullSimplifierTests
{
    [Test]
    public void OuterHullFillsConcavitiesAndCoversTheSource()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 4 0, 4 4, 3 4, 3 1, 1 1, 1 4, 0 4, 0 0))");
        double sourceArea =
            GeometryMeasureAlgorithms.GetProjectedArea(source);

        Geometry result = PolygonHullSimplifier.SimplifyGeometry(
            source, Parameters(1.0), true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryPredicateAlgorithms.Covers(result, source),
                Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.GreaterThan(sourceArea));
            Assert.That(result.Polygon!.Shell.Coordinates.Length,
                Is.LessThan(source.Polygon!.Shell.Coordinates.Length));
            Assert.That(VisvalingamWhyatt.PolygonRingsAreValid(
                result.Polygon.Shell.Coordinates,
                result.Polygon.Holes), Is.True);
        });
    }

    [Test]
    public void InnerHullContractsThePolygonAndRemainsCoveredBySource()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 6 0, 7 2, 6 5, 3 6, 0 5, -1 2, 0 0))");
        double sourceArea =
            GeometryMeasureAlgorithms.GetProjectedArea(source);

        Geometry result = PolygonHullSimplifier.SimplifyPolygonHull(
            source, 1.0, false);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Covers(source, result),
                Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.LessThan(sourceArea));
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(4));
            Assert.That(VisvalingamWhyatt.PolygonRingsAreValid(
                result.Polygon.Shell.Coordinates,
                result.Polygon.Holes), Is.True);
        });
    }

    [Test]
    public void OuterHullShrinksHolesWithoutRemovingThem()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(3 3, 3 7, 7 7, 7 3, 3 3))");
        double sourceArea =
            GeometryMeasureAlgorithms.GetProjectedArea(source);

        Geometry result = PolygonHullSimplifier.SimplifyPolygonHull(
            source, 1.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(result.Polygon.Holes[0].Coordinates,
                Has.Length.EqualTo(4));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.GreaterThan(sourceArea));
            Assert.That(GeometryPredicateAlgorithms.Covers(result, source),
                Is.True);
        });
    }

    [Test]
    public void ZeroRatioReturnsAnOwningUnchangedPolygon()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;POLYGON Z ((0 0 1, 4 0 2, 4 4 3, 2 2 4, 0 4 5, 0 0 1))");

        Geometry result = PolygonHullSimplifier.SimplifyPolygonHull(
            source, 0.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(result, source),
                Is.True);
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
        });

        source.Polygon!.Shell.Coordinates[0].X = 99.0;
        Assert.That(result.Polygon!.Shell.Coordinates[0].X,
            Is.EqualTo(0.0));
    }

    [Test]
    public void MultiPolygonHullsPreserveDisjointComponents()
    {
        Geometry source = Read(
            "MULTIPOLYGON (((0 0, 4 0, 4 4, 3 4, 3 1, 1 1, 1 4, 0 4, 0 0)), " +
            "((20 0, 24 0, 24 4, 23 4, 23 1, 21 1, 21 4, 20 4, 20 0)))");

        Geometry result = PolygonHullSimplifier.SimplifyPolygonHull(
            source, 1.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(result.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(GeometryPredicateAlgorithms.Covers(result, source),
                Is.True);
        });
    }

    [Test]
    public void InvalidRatiosAndNonPolygonalInputsAreRejected()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        SimplifyParameters wrong = Parameters(0.5);
        wrong.Algorithm = SimplificationAlgorithm.DouglasPeucker;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyGeometry(
                    polygon, null!, true));
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyGeometry(
                    polygon, wrong, true));
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyPolygonHull(
                    polygon, -0.1, true));
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyPolygonHull(
                    polygon, 1.1, true));
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyPolygonHull(
                    polygon, double.NaN, true));
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonHullSimplifier.SimplifyPolygonHull(
                    Read("LINESTRING (0 0, 1 1)"), 0.5, true));
        });
    }

    private static SimplifyParameters Parameters(double ratio)
    {
        return new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.PolygonHull,
            Tolerance = ratio
        };
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
