using geo;

namespace geo.Tests;

public class GeometrySimplifierTests
{
    [Test]
    public void SimplifyDispatchesTheSelectedAlgorithm()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry source = Read(
            "LINESTRING (0 0, 1 0.05, 2 0, 3 2)");
        SimplifyParameters douglasPeucker = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.DouglasPeucker,
            Tolerance = 0.1
        };
        SimplifyParameters topology = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.TopologyPreserving,
            Tolerance = 0.1
        };
        SimplifyParameters visvalingam = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.VisvalingamWhyatt,
            Tolerance = 0.1
        };

        Geometry first = simplifier.Simplify(source, douglasPeucker);
        Geometry second = simplifier.Simplify(source, topology);
        Geometry third = simplifier.Simplify(source, visvalingam);

        Assert.Multiple(() =>
        {
            Assert.That(first.LineString!.Coordinates.Length,
                Is.LessThan(source.LineString!.Coordinates.Length));
            Assert.That(second.LineString!.Coordinates.Length,
                Is.LessThan(source.LineString!.Coordinates.Length));
            Assert.That(third.LineString!.Coordinates.Length,
                Is.LessThan(source.LineString!.Coordinates.Length));
            Assert.That(source.LineString.Coordinates,
                Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void DensifyAppliesTheRequestedPrecision()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry source = ReadWithSrid(
            "SRID=3857;LINESTRING Z (0.2 0.2 1, 2.2 0.2 5)");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0
        };

        Geometry result = simplifier.Densify(
            source, 0.75, precision);

        Assert.Multiple(() =>
        {
            Assert.That(result.LineString!.Coordinates,
                Has.Length.EqualTo(4));
            Assert.That(result.LineString.Coordinates[0].X,
                Is.EqualTo(0.0));
            Assert.That(result.LineString.Coordinates[1].X,
                Is.EqualTo(1.0));
            Assert.That(result.LineString.Coordinates[1].Z,
                Is.EqualTo(2.3333333333333335));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
        });
    }

    [Test]
    public void PolygonHullUsesExplicitInnerOuterSelection()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry source = Read(
            "POLYGON ((0 0, 2 1, 4 0, 3 2, 4 4, " +
            "2 3, 0 4, 1 2, 0 0))");
        SimplifyParameters parameters = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.PolygonHull,
            Tolerance = 0.5
        };

        Geometry inner = simplifier.SimplifyPolygonHull(
            source, parameters, false);
        Geometry outer = simplifier.SimplifyPolygonHull(
            source, parameters, true);
        GeometryPredicates predicates = new GeometryPredicates();

        Assert.Multiple(() =>
        {
            Assert.That(predicates.Covers(source, inner), Is.True);
            Assert.That(predicates.Covers(outer, source), Is.True);
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(source, parameters));
        });
    }

    [Test]
    public void LinearizeCurvesConvertsNestedCurvesToCoreGeometry()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry source = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION (" +
            "CIRCULARSTRING (1 0, 0 1, -1 0), " +
            "CURVEPOLYGON (CIRCULARSTRING " +
            "(2 0, 0 2, -2 0, 0 -2, 2 0)))");
        CurveLinearizationParameters parameters =
            new CurveLinearizationParameters
            {
                Mode = CurveLinearizationMode.MaximumAngle,
                Tolerance = Math.PI / 4.0,
                MaximumSegments = 100
            };

        Geometry result = simplifier.LinearizeCurves(
            source, parameters);
        Geometry[] members = result.GeometryCollection!.Geometries;

        Assert.Multiple(() =>
        {
            Assert.That(members[0].GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(members[0].LineString!.Coordinates.Length,
                Is.GreaterThan(3));
            Assert.That(members[1].GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(members[1].Polygon!.Shell.Coordinates.Length,
                Is.GreaterThan(5));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
        });

        source.GeometryCollection!.Geometries[0]
            .CircularString!.Coordinates[0].X = 99.0;
        Assert.That(members[0].LineString!.Coordinates[0].X,
            Is.EqualTo(1.0));
    }

    [Test]
    public void LinearizeCurvesConvertsMultiCurveAndMultiSurface()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        CurveLinearizationParameters parameters =
            new CurveLinearizationParameters
            {
                Mode = CurveLinearizationMode.MaximumAngle,
                Tolerance = Math.PI / 4.0,
                MaximumSegments = 100
            };
        Geometry curves = Read(
            "MULTICURVE ((0 0, 1 0), " +
            "CIRCULARSTRING (2 0, 3 1, 4 0))");
        Geometry surfaces = Read(
            "MULTISURFACE (((0 0, 1 0, 1 1, 0 1, 0 0)), " +
            "CURVEPOLYGON ((2 0, 3 0, 3 1, 2 1, 2 0)), " +
            "TRIANGLE ((4 0, 5 0, 4 1, 4 0)))");

        Geometry lineResult = simplifier.LinearizeCurves(
            curves, parameters);
        Geometry areaResult = simplifier.LinearizeCurves(
            surfaces, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(lineResult.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lineResult.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(areaResult.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(areaResult.MultiPolygon!.Polygons,
                Has.Length.EqualTo(3));
        });
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
