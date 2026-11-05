using geo;

namespace geo.Tests;

public class WkbSizerTests
{
    [TestCase("POINT (1 2)", 21)]
    [TestCase("POINT ZM (1 2 3 4)", 37)]
    [TestCase("LINESTRING (0 0, 1 1)", 41)]
    [TestCase("LINESTRING M (0 0 4, 1 1 5)", 57)]
    [TestCase("POLYGON ((0 0, 2 0, 1 2, 0 0))", 77)]
    [TestCase("POLYGON Z ((0 0 3, 2 0 3, 1 2 3, 0 0 3))", 109)]
    [TestCase("MULTIPOINT ((1 2), (3 4))", 51)]
    [TestCase("MULTILINESTRING ((0 0, 1 1))", 50)]
    [TestCase("MULTIPOLYGON (((0 0, 2 0, 1 2, 0 0)))", 86)]
    [TestCase("GEOMETRYCOLLECTION (POINT (1 2), LINESTRING (0 0, 1 1))", 71)]
    public void ComputesExactCoreWkbSizes(string text, int expected)
    {
        Geometry geometry = ParseWkt(text);

        Assert.That(WkbSizer.GetSize(geometry), Is.EqualTo(expected));
    }

    [Test]
    public void RejectsInvalidExtendedSerializationTrees()
    {
        Geometry circular = ParseWkt("CIRCULARSTRING (0 0, 1 1, 2 0)");
        circular.CircularString!.Coordinates =
        [
            circular.CircularString.Coordinates[0],
            circular.CircularString.Coordinates[1],
            circular.CircularString.Coordinates[2],
            new Coordinate { X = 3.0, Y = 1.0 }
        ];
        Geometry triangle = ParseWkt("TRIANGLE ((0 0, 2 0, 1 2, 0 0))");
        triangle.Triangle!.Shell.Coordinates =
        [
            new Coordinate { X = 0.0, Y = 0.0 },
            new Coordinate { X = 2.0, Y = 0.0 },
            new Coordinate { X = 2.0, Y = 2.0 },
            new Coordinate { X = 0.0, Y = 2.0 },
            new Coordinate { X = 0.0, Y = 0.0 }
        ];
        Geometry compound = ParseWkt(
            "COMPOUNDCURVE ((0 0, 1 0), CIRCULARSTRING (1 0, 2 2, 3 0))");
        compound.CompoundCurve!.Curves[1].CircularString!.Coordinates[0].X = 2.0;
        Geometry curvePolygon = ParseWkt("CURVEPOLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        curvePolygon.CurvePolygon!.Shell!.LineString!.Coordinates[4].X = 1.0;
        Geometry multiCurve = ParseWkt("MULTICURVE ((0 0, 1 1))");
        multiCurve.MultiCurve!.Curves[0] = ParseWkt("POINT (1 2)");
        Geometry multiSurface = ParseWkt("MULTISURFACE (POLYGON EMPTY)");
        multiSurface.MultiSurface!.Surfaces[0] = ParseWkt("POINT EMPTY");
        Geometry linearRing = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing()
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(null!));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(circular));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(triangle));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(compound));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(curvePolygon));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(multiCurve));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(multiSurface));
            Assert.Throws<InvalidGeometryException>(() => WkbSizer.GetSize(linearRing));
        });
    }

    private static Geometry ParseWkt(string text)
    {
        GeometryReadResult result = new WktParser(text, new WktReadOptions()).ParseDocument();
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Geometry!;
    }
}
