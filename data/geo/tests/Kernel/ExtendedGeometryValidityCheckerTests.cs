using geo;
using NtsIsValidOp = NetTopologySuite.Operation.Valid.IsValidOp;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class ExtendedGeometryValidityCheckerTests
{
    [TestCase(
        "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "((4 0, 6 0, 6 2, 4 2, 4 0)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "((2 2, 4 2, 4 4, 2 4, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 0, 3 -1, 2 -2, 1 -1, 2 0)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 8, 8 8, 8 2, 2 2)), " +
        "((3 3, 4 3, 4 4, 3 4, 3 3)))")]
    public void DisjointPointTouchingAndHoleNestedMultiPolygonsAreValid(
        string text)
    {
        ValidationResult result = Validate(text);

        AssertValid(result);
    }

    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 2, 6 2, 6 6, 2 6, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0)), " +
        "((2 2, 4 2, 4 4, 2 4, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((4 0, 8 0, 8 4, 4 4, 4 0)))")]
    public void OverlappingNestedAndSharedEdgeMultiPolygonsAreInvalid(
        string text)
    {
        ValidationResult result = Validate(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.OverlappingComponents));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.ComponentPath,
                Does.StartWith("MultiPolygon/Polygons["));
        });
    }

    [TestCase(
        "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "((4 0, 6 0, 6 2, 4 2, 4 0)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "((2 2, 4 2, 4 4, 2 4, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 0, 3 -1, 2 -2, 1 -1, 2 0)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 2, 6 2, 6 6, 2 6, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0)), " +
        "((2 2, 4 2, 4 4, 2 4, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 8, 8 8, 8 2, 2 2)), " +
        "((3 3, 4 3, 4 4, 3 4, 3 3)))")]
    public void MultiPolygonValidityMatchesNetTopologySuite(string text)
    {
        bool reference = new NtsIsValidOp(
            new NtsWktReader().Read(text)).IsValid;

        ValidationResult result = Validate(text);

        Assert.That(result.IsValid, Is.EqualTo(reference));
    }

    [Test]
    public void GeometryCollectionRecursivelyReportsChildPathOnly()
    {
        Geometry invalid = Read(
            "GEOMETRYCOLLECTION (POINT (1 1), " +
            "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0)))");
        Geometry independent = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "POLYGON ((2 2, 6 2, 6 6, 2 6, 2 2)))");

        ValidationResult invalidResult =
            GeometryValidityChecker.ValidateGeometry(invalid);
        ValidationResult independentResult =
            GeometryValidityChecker.ValidateGeometry(independent);

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.IsValid, Is.False);
            Assert.That(invalidResult.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(invalidResult.ComponentPath,
                Does.StartWith(
                    "GeometryCollection/Geometries[1]/Shell/Segments["));
            AssertValid(independentResult);
        });
    }

    [Test]
    public void TriangleRequiresExactlyThreeNonCollinearVertices()
    {
        Geometry valid = Read(
            "TRIANGLE ((0 0, 4 0, 1 3, 0 0))");
        Geometry quadrilateral = TriangleGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 4.0),
            CoordinateValue(0.0, 0.0));
        Geometry collinear = TriangleGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(0.0, 0.0));

        ValidationResult validResult =
            GeometryValidityChecker.ValidateGeometry(valid);
        ValidationResult quadrilateralResult =
            GeometryValidityChecker.ValidateGeometry(quadrilateral);
        ValidationResult collinearResult =
            GeometryValidityChecker.ValidateGeometry(collinear);

        Assert.Multiple(() =>
        {
            AssertValid(validResult);
            AssertFailure(quadrilateralResult,
                ValidationError.InvalidSurface, "Triangle/Shell");
            AssertFailure(collinearResult,
                ValidationError.InvalidSurface, "Triangle/Shell");
        });
    }

    [Test]
    public void TinAllowsSharedEdgesAndRejectsInteriorOverlap()
    {
        Geometry adjacent = Read(
            "TIN (((0 0, 2 0, 0 2, 0 0)), " +
            "((2 0, 2 2, 0 2, 2 0)))");
        Geometry nested = Read(
            "TIN (((0 0, 6 0, 0 6, 0 0)), " +
            "((1 1, 2 1, 1 2, 1 1)))");
        Geometry sameSide = Read(
            "TIN (((0 0, 4 0, 1 3, 0 0)), " +
            "((0 0, 4 0, 3 3, 0 0)))");

        ValidationResult adjacentResult =
            GeometryValidityChecker.ValidateGeometry(adjacent);
        ValidationResult nestedResult =
            GeometryValidityChecker.ValidateGeometry(nested);
        ValidationResult sameSideResult =
            GeometryValidityChecker.ValidateGeometry(sameSide);

        Assert.Multiple(() =>
        {
            AssertValid(adjacentResult);
            Assert.That(nestedResult.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(nestedResult.ComponentPath,
                Is.EqualTo("Tin/Triangles[1]"));
            Assert.That(sameSideResult.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(sameSideResult.ComponentPath,
                Is.EqualTo("Tin/Triangles[1]"));
        });
    }

    [Test]
    public void TinRejectsPartialAndNonManifoldSharedEdges()
    {
        Geometry partial = Read(
            "TIN (((0 0, 4 0, 0 4, 0 0)), " +
            "((0 0, 2 0, 1 -2, 0 0)))");
        Geometry nonManifold = Read(
            "TIN (((0 0, 4 0, 1 2, 0 0)), " +
            "((4 0, 0 0, 1 -2, 4 0)), " +
            "((0 0, 4 0, 3 2, 0 0)))");
        Geometry elevationMismatch = Read(
            "TIN Z (((0 0 0, 2 0 0, 0 2 0, 0 0 0)), " +
            "((2 0 1, 2 2 1, 0 2 1, 2 0 1)))");

        ValidationResult partialResult =
            GeometryValidityChecker.ValidateGeometry(partial);
        ValidationResult nonManifoldResult =
            GeometryValidityChecker.ValidateGeometry(nonManifold);
        ValidationResult elevationResult =
            GeometryValidityChecker.ValidateGeometry(elevationMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(partialResult.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(partialResult.ComponentPath,
                Is.EqualTo("Tin/Triangles[1]"));
            Assert.That(nonManifoldResult.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(nonManifoldResult.ComponentPath,
                Is.EqualTo("Tin/Triangles[2]/Shell"));
            Assert.That(elevationResult.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(elevationResult.ComponentPath,
                Is.EqualTo("Tin/Triangles[1]"));
        });
    }

    [Test]
    public void PolyhedralSurfaceValidatesPatchesAndEdgeManifold()
    {
        Geometry invalidPatch = Read(
            "POLYHEDRALSURFACE (((0 0, 4 4, 0 4, 4 0, 0 0)))");
        Geometry nonManifold = Read(
            "POLYHEDRALSURFACE (" +
            "((0 0, 4 0, 1 2, 0 0)), " +
            "((4 0, 0 0, 1 -2, 4 0)), " +
            "((0 0, 4 0, 3 2, 0 0)))");

        ValidationResult patchResult =
            GeometryValidityChecker.ValidateGeometry(invalidPatch);
        ValidationResult manifoldResult =
            GeometryValidityChecker.ValidateGeometry(nonManifold);

        Assert.Multiple(() =>
        {
            Assert.That(patchResult.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(patchResult.ComponentPath,
                Does.StartWith(
                    "PolyhedralSurface/Patches[0]/Shell/Segments["));
            AssertFailure(manifoldResult,
                ValidationError.InvalidSurface,
                "PolyhedralSurface/Patches[2]/Shell");
        });
    }

    [Test]
    public void SurfaceClosureRequiresEveryExactThreeDimensionalEdgeTwice()
    {
        IGeometryInspector inspector = new GeometryInspector();
        Geometry openTin = Read(
            "TIN Z (((0 0 0, 4 0 0, 1 3 0, 0 0 0)))");
        Geometry closedTin = Read(TetrahedronText("TIN Z"));
        Geometry openPolyhedral = Read(
            "POLYHEDRALSURFACE Z (" +
            "((0 0 0, 4 0 0, 1 3 0, 0 0 0)))");
        Geometry closedPolyhedral = Read(
            TetrahedronText("POLYHEDRALSURFACE Z"));

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(openTin), Is.False);
            Assert.That(inspector.IsClosed(closedTin), Is.True);
            Assert.That(inspector.IsClosed(openPolyhedral), Is.False);
            Assert.That(inspector.IsClosed(closedPolyhedral), Is.True);
        });
    }

    [Test]
    public void CompoundCurveValidityAndClosureRequireContinuity()
    {
        IGeometryInspector inspector = new GeometryInspector();
        Geometry continuous = Read(
            "COMPOUNDCURVE ((0 0, 2 0), " +
            "CIRCULARSTRING (2 0, 3 1, 4 0))");
        Geometry closed = Read(
            "COMPOUNDCURVE ((0 0, 2 0), (2 0, 0 0))");
        Geometry discontinuous = CompoundCurveGeometry(
            LineGeometry(0.0, 0.0, 2.0, 0.0),
            LineGeometry(3.0, 0.0, 0.0, 0.0));

        ValidationResult continuousResult =
            GeometryValidityChecker.ValidateGeometry(continuous);
        ValidationResult closedResult =
            GeometryValidityChecker.ValidateGeometry(closed);
        ValidationResult discontinuousResult =
            GeometryValidityChecker.ValidateGeometry(discontinuous);

        Assert.Multiple(() =>
        {
            AssertValid(continuousResult);
            AssertValid(closedResult);
            Assert.That(inspector.IsClosed(continuous), Is.False);
            Assert.That(inspector.IsClosed(closed), Is.True);
            AssertFailure(discontinuousResult,
                ValidationError.CurveDiscontinuity,
                "CompoundCurve/Curves[1]");
            Assert.That(inspector.IsClosed(discontinuous), Is.False);
        });
    }

    [TestCase("CIRCULARSTRING (0 0, 2 2, 4 0)")]
    [TestCase("CURVEPOLYGON (CIRCULARSTRING " +
        "(2 0, 0 2, -2 0, 0 -2, 2 0))")]
    [TestCase("MULTICURVE ((0 0, 2 0), " +
        "CIRCULARSTRING (4 0, 5 1, 6 0))")]
    [TestCase("MULTISURFACE (" +
        "((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "TRIANGLE ((4 0, 6 0, 4 2, 4 0)))")]
    public void ValidCurvedAndAggregateValuesPass(string text)
    {
        AssertValid(Validate(text));
    }

    [Test]
    public void CurvePolygonAndMultiSurfaceReusePolygonTopologyRules()
    {
        Geometry curvePolygon = Read(
            "CURVEPOLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))");
        Geometry multiSurface = Read(
            "MULTISURFACE (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "TRIANGLE ((2 2, 6 2, 2 6, 2 2)))");

        ValidationResult curveResult =
            GeometryValidityChecker.ValidateGeometry(curvePolygon);
        ValidationResult surfaceResult =
            GeometryValidityChecker.ValidateGeometry(multiSurface);

        Assert.Multiple(() =>
        {
            Assert.That(curveResult.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(curveResult.ComponentPath,
                Does.StartWith("CurvePolygon/Shell/Segments["));
            Assert.That(surfaceResult.Error,
                Is.EqualTo(ValidationError.OverlappingComponents));
            Assert.That(surfaceResult.ComponentPath,
                Does.StartWith("MultiSurface/Surfaces["));
        });
    }

    private static string TetrahedronText(string type)
    {
        return type + " (" +
            "((0 0 0, 4 0 0, 1 3 0, 0 0 0)), " +
            "((0 0 0, 1 1 4, 4 0 0, 0 0 0)), " +
            "((4 0 0, 1 1 4, 1 3 0, 4 0 0)), " +
            "((1 3 0, 1 1 4, 0 0 0, 1 3 0)))";
    }

    private static Geometry TriangleGeometry(params Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.Triangle,
            Point = null,
            Triangle = new Triangle
            {
                Shell = new LinearRing { Coordinates = coordinates }
            }
        };
    }

    private static Geometry CompoundCurveGeometry(params Geometry[] curves)
    {
        return new Geometry
        {
            GeometryType = GeometryType.CompoundCurve,
            Point = null,
            CompoundCurve = new CompoundCurve { Curves = curves }
        };
    }

    private static Geometry LineGeometry(
        double startX, double startY, double endX, double endY)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates =
                [
                    CoordinateValue(startX, startY),
                    CoordinateValue(endX, endY)
                ]
            }
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static ValidationResult Validate(string text)
    {
        return GeometryValidityChecker.ValidateGeometry(Read(text));
    }

    private static void AssertValid(ValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(ValidationError.None));
            Assert.That(result.Message, Is.Empty);
            Assert.That(result.Coordinate, Is.Null);
            Assert.That(result.ComponentPath, Is.Empty);
        });
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
