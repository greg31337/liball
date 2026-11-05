using geo;

namespace geo.Tests;

public class ValidationDiagnosticMatrixTests
{
    [Test]
    public void NullCoordinatesAndPolygonMembersReportExactNestedPaths()
    {
        Geometry nullCoordinate = LineGeometry([
            CoordinateValue(0.0, 0.0),
            null!
        ]);
        Geometry nullShell = PolygonGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 0.0)
        ]);
        nullShell.Polygon!.Shell = null!;
        Geometry nullHole = PolygonGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 0.0)
        ]);
        nullHole.Polygon!.Holes = [null!];

        AssertFailure(
            StructuralValidator.ValidateGeometry(nullCoordinate),
            ValidationError.NullGeometry,
            "LineString/Coordinates[1]");
        AssertFailure(
            StructuralValidator.ValidateGeometry(nullShell),
            ValidationError.NullGeometry,
            "Polygon/Shell");
        AssertFailure(
            StructuralValidator.ValidateGeometry(nullHole),
            ValidationError.NullGeometry,
            "Polygon/Holes[0]");
    }

    [Test]
    public void HigherDimensionOrdinateFailuresRetainCoordinateWitnesses()
    {
        Geometry xyz = LineGeometry([
            CoordinateValue(0.0, 0.0, 1.0, double.NaN),
            CoordinateValue(1.0, 1.0, double.PositiveInfinity, double.NaN)
        ]);
        xyz.CoordinateLayout = CoordinateLayout.XYZ;
        Geometry xym = LineGeometry([
            CoordinateValue(0.0, 0.0, double.NaN, 1.0),
            CoordinateValue(1.0, 1.0, double.NaN, double.NaN)
        ]);
        xym.CoordinateLayout = CoordinateLayout.XYM;
        Geometry xyzm = LineGeometry([
            CoordinateValue(0.0, 0.0, 1.0, 1.0),
            CoordinateValue(1.0, 1.0, 2.0, double.NegativeInfinity)
        ]);
        xyzm.CoordinateLayout = CoordinateLayout.XYZM;

        ValidationResult xyzResult =
            StructuralValidator.ValidateGeometry(xyz);
        ValidationResult xymResult =
            StructuralValidator.ValidateGeometry(xym);
        ValidationResult xyzmResult =
            StructuralValidator.ValidateGeometry(xyzm);

        Assert.Multiple(() =>
        {
            AssertFailure(xyzResult,
                ValidationError.InvalidCoordinateLayout,
                "LineString/Coordinates[1]");
            Assert.That(double.IsPositiveInfinity(
                xyzResult.Coordinate!.Z), Is.True);
            AssertFailure(xymResult,
                ValidationError.InvalidCoordinateLayout,
                "LineString/Coordinates[1]");
            Assert.That(double.IsNaN(xymResult.Coordinate!.M), Is.True);
            AssertFailure(xyzmResult,
                ValidationError.InvalidCoordinateLayout,
                "LineString/Coordinates[1]");
            Assert.That(double.IsNegativeInfinity(
                xyzmResult.Coordinate!.M), Is.True);
        });
    }

    [Test]
    public void SharedChildIsAllowedButBackReferenceIsACycle()
    {
        Geometry child = Read("POINT (1 2)");
        Geometry shared = CollectionGeometry([child, child]);
        Geometry parent = CollectionGeometry([]);
        Geometry nested = CollectionGeometry([parent]);
        parent.GeometryCollection!.Geometries = [nested];

        ValidationResult sharedResult =
            StructuralValidator.ValidateGeometry(shared);
        ValidationResult cycleResult =
            StructuralValidator.ValidateGeometry(parent);

        Assert.Multiple(() =>
        {
            Assert.That(sharedResult.IsValid, Is.True);
            AssertFailure(cycleResult,
                ValidationError.CyclicGeometry,
                "GeometryCollection/Geometries[0]/Geometries[0]");
        });
    }

    [Test]
    public void FirstCoordinateFailurePrecedesSequenceCardinality()
    {
        Geometry line = LineGeometry([
            CoordinateValue(double.NaN, 1.0)
        ]);

        ValidationResult result =
            StructuralValidator.ValidateGeometry(line);

        Assert.Multiple(() =>
        {
            AssertFailure(result,
                ValidationError.NonFiniteCoordinate,
                "LineString/Coordinates[0]");
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(double.IsNaN(result.Coordinate!.X), Is.True);
        });
    }

    [Test]
    public void UnsupportedSimplicityTypeReturnsDiagnosticNotException()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");

        ValidationResult result = SimplicityChecker.CheckSimplicity(
            polygon, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            AssertFailure(result,
                ValidationError.InvalidGeometryType, "Polygon");
            Assert.That(result.Coordinate, Is.Null);
            Assert.That(result.Message, Does.Contain("point and core lineal"));
        });
    }

    [Test]
    public void NestedTopologyFailureIncludesEveryCollectionIndex()
    {
        Geometry geometry = Read(
            "GEOMETRYCOLLECTION (POINT (0 0), " +
            "GEOMETRYCOLLECTION (LINESTRING (0 0, 1 1), " +
            "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))))");

        ValidationResult result =
            GeometryValidityChecker.ValidateGeometry(geometry);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.Coordinate!.X, Is.EqualTo(2.0));
            Assert.That(result.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(result.ComponentPath,
                Does.StartWith(
                    "GeometryCollection/Geometries[1]/Geometries[1]/" +
                    "Shell/Segments["));
        });
    }

    private static Geometry CollectionGeometry(Geometry[] geometries)
    {
        return new Geometry
        {
            GeometryType = GeometryType.GeometryCollection,
            Point = null,
            GeometryCollection = new GeometryCollection
            {
                Geometries = geometries
            }
        };
    }

    private static Geometry LineGeometry(Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = coordinates }
        };
    }

    private static Geometry PolygonGeometry(Coordinate[] shell)
    {
        return new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = new Polygon
            {
                Shell = new LinearRing { Coordinates = shell }
            }
        };
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN,
        double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static void AssertFailure(
        ValidationResult result, ValidationError error, string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.ComponentPath, Is.EqualTo(path));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
