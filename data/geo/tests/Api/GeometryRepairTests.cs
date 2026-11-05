using geo;

namespace geo.Tests;

public class GeometryRepairTests
{
    [Test]
    public void MakeValidAndRepairRingsExposeOwningValidResults()
    {
        IGeometryRepair repair = new GeometryRepair();
        Geometry bowTie = Read(
            "POLYGON ((0 0, 2 2, 0 2, 2 0, 0 0))");
        Geometry open = RawPolygon([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(3.0, 0.0),
            CoordinateValue(3.0, 3.0),
            CoordinateValue(0.0, 3.0)
        ]);
        RepairParameters parameters = new RepairParameters();

        Geometry madeValid = repair.MakeValid(bowTie, parameters);
        Geometry rings = repair.RepairRings(open, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                madeValid).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                madeValid), Is.EqualTo(2.0));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                rings).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(rings),
                Is.EqualTo(9.0));
            Assert.That(open.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void CoordinateCleanupAndCollapseRemovalAreExposed()
    {
        IGeometryRepair repair = new GeometryRepair();
        Geometry coordinates = RawLine([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(0.0, 0.0),
            CoordinateValue(double.PositiveInfinity, 1.0),
            CoordinateValue(2.0, 0.0)
        ]);
        Geometry collapsed = Read(
            "POLYGON ((0 0, 1 0, 2 0, 0 0))");

        Geometry cleaned = repair.CleanCoordinates(
            coordinates, new RepairParameters());
        Geometry removed = repair.RemoveCollapsedComponents(collapsed);

        Assert.Multiple(() =>
        {
            Assert.That(cleaned.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(GeometryInspection.IsEmptyValue(removed), Is.True);
            Assert.That(removed.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
        });
    }

    [Test]
    public void PrecisionReductionAndCoverageCleaningAreExposed()
    {
        IGeometryRepair repair = new GeometryRepair();
        Geometry precise = Read(
            "POLYGON ((0.1 0.1, 3.9 0.1, 3.9 3.9, 0.1 3.9, 0.1 0.1))");
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 1.0
        };

        Geometry reduced = repair.ReducePrecision(precise, precision);
        Geometry coverage = repair.CleanCoverage(
            overlap, new RepairParameters());

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                reduced,
                Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))")),
                Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(coverage),
                Is.EqualTo(15.0));
            Assert.That(coverage.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
        });
    }

    private static Geometry RawLine(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.LineString;
        geometry.LineString = new LineString { Coordinates = coordinates };
        return geometry;
    }

    private static Geometry RawPolygon(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.Polygon;
        geometry.Polygon = new Polygon
        {
            Shell = new LinearRing { Coordinates = coordinates }
        };
        return geometry;
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
