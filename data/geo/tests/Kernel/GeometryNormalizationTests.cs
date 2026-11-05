using geo;

namespace geo.Tests;

public class GeometryNormalizationTests
{
    [Test]
    public void CloneCreatesADeepOwningCopyWithoutChangingTypeLayoutOrSrid()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            SpatialReferenceId = 4979
        });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0, 3.0, 4.0));
        Geometry line = factory.CreateLineString([
            CoordinateValue(5.0, 6.0, 7.0, 8.0),
            CoordinateValue(9.0, 10.0, 11.0, 12.0)
        ]);
        Geometry source = factory.CreateGeometryCollection([point, line]);

        Geometry clone = GeometryNormalization.CloneGeometryValue(source);
        source.GeometryCollection!.Geometries[0].Point!.Coordinate!.X = 100.0;
        source.GeometryCollection.Geometries[1].LineString!.Coordinates[0].Z = 200.0;

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(clone.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(clone.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(clone.GeometryCollection, Is.Not.SameAs(source.GeometryCollection));
            Assert.That(clone.GeometryCollection!.Geometries[0].Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(clone.GeometryCollection.Geometries[1].LineString!.Coordinates[0].Z, Is.EqualTo(7.0));
        });
    }

    [Test]
    public void OpenCoordinateSequencesChooseLexicographicallySmallerDirection()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYM, SpatialReferenceId = 4326 });
        Geometry source = factory.CreateLineString([
            CoordinateValue(5.0, 5.0, double.NaN, 9.0),
            CoordinateValue(3.0, 3.0, double.NaN, 8.0),
            CoordinateValue(1.0, 1.0, double.NaN, 7.0)
        ]);

        Geometry normalized = GeometryNormalization.NormalizeGeometryValue(source);
        source.LineString!.Coordinates[2].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(normalized.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYM));
            Assert.That(normalized.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(normalized.LineString!.Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(normalized.LineString.Coordinates[0].M, Is.EqualTo(7.0));
            Assert.That(normalized.LineString.Coordinates[2].X, Is.EqualTo(5.0));
        });
    }

    [Test]
    public void PolygonNormalizationRotatesOrientsAndSortsRings()
    {
        LinearRing shell = new LinearRing
        {
            Coordinates =
            [
                CoordinateValue(10.0, 10.0, double.NaN, double.NaN),
                CoordinateValue(10.0, 0.0, double.NaN, double.NaN),
                CoordinateValue(0.0, 0.0, double.NaN, double.NaN),
                CoordinateValue(0.0, 10.0, double.NaN, double.NaN),
                CoordinateValue(10.0, 10.0, double.NaN, double.NaN)
            ]
        };
        LinearRing higherHole = RingCounterClockwise(6.0, 6.0, 8.0, 8.0);
        LinearRing lowerHole = RingCounterClockwise(2.0, 2.0, 4.0, 4.0);
        Geometry source = new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = new Polygon { Shell = shell, Holes = [higherHole, lowerHole] }
        };

        Geometry normalized = GeometryNormalization.NormalizeGeometryValue(source);
        Polygon polygon = normalized.Polygon!;
        shell.Coordinates[0].X = 100.0;
        higherHole.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(polygon.Shell.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(polygon.Shell.Coordinates[0].Y, Is.EqualTo(0.0));
            Assert.That(SignedArea(polygon.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(polygon.Holes[0].Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(polygon.Holes[0].Coordinates[0].Y, Is.EqualTo(2.0));
            Assert.That(polygon.Holes[1].Coordinates[0].X, Is.EqualTo(6.0));
            Assert.That(SignedArea(polygon.Holes[0].Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(polygon.Holes[1].Coordinates), Is.LessThan(0.0));
            Assert.That(polygon.Shell.Coordinates[^1].X, Is.EqualTo(polygon.Shell.Coordinates[0].X));
            Assert.That(polygon.Shell.Coordinates[^1].Y, Is.EqualTo(polygon.Shell.Coordinates[0].Y));
        });
    }

    [Test]
    public void HomogeneousComponentsNormalizeAndSortByCanonicalCoordinates()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry multiPoint = factory.CreateMultiPoint([
            new Point { Coordinate = CoordinateValue(5.0, 0.0, double.NaN, double.NaN) },
            new Point(),
            new Point { Coordinate = CoordinateValue(1.0, 0.0, double.NaN, double.NaN) }
        ]);
        Geometry multiLine = factory.CreateMultiLineString([
            new LineString { Coordinates = [CoordinateValue(9.0, 0.0, double.NaN, double.NaN), CoordinateValue(8.0, 0.0, double.NaN, double.NaN)] },
            new LineString { Coordinates = [CoordinateValue(4.0, 0.0, double.NaN, double.NaN), CoordinateValue(2.0, 0.0, double.NaN, double.NaN)] }
        ]);
        Geometry multiPolygon = factory.CreateMultiPolygon([
            new Polygon { Shell = RingCounterClockwise(10.0, 10.0, 12.0, 12.0) },
            new Polygon { Shell = RingCounterClockwise(2.0, 2.0, 4.0, 4.0) }
        ]);

        Geometry normalizedPoints = GeometryNormalization.NormalizeGeometryValue(multiPoint);
        Geometry normalizedLines = GeometryNormalization.NormalizeGeometryValue(multiLine);
        Geometry normalizedPolygons = GeometryNormalization.NormalizeGeometryValue(multiPolygon);

        Assert.Multiple(() =>
        {
            Assert.That(normalizedPoints.MultiPoint!.Points[0].Coordinate, Is.Null);
            Assert.That(normalizedPoints.MultiPoint.Points[1].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(normalizedPoints.MultiPoint.Points[2].Coordinate!.X, Is.EqualTo(5.0));
            Assert.That(normalizedLines.MultiLineString!.LineStrings[0].Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(normalizedLines.MultiLineString.LineStrings[0].Coordinates[1].X, Is.EqualTo(4.0));
            Assert.That(normalizedLines.MultiLineString.LineStrings[1].Coordinates[0].X, Is.EqualTo(8.0));
            Assert.That(normalizedPolygons.MultiPolygon!.Polygons[0].Shell.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(normalizedPolygons.MultiPolygon.Polygons[1].Shell.Coordinates[0].X, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void GeometryCollectionsSortAndNarrowOnlyWhenCoreContentsPermitIt()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry lowPoint = factory.CreatePoint(CoordinateValue(1.0, 0.0, double.NaN, double.NaN));
        Geometry highPoint = factory.CreatePoint(CoordinateValue(5.0, 0.0, double.NaN, double.NaN));
        Geometry line = factory.CreateLineString([CoordinateValue(4.0, 0.0, double.NaN, double.NaN), CoordinateValue(2.0, 0.0, double.NaN, double.NaN)]);
        Geometry polygon = factory.CreateEnvelopePolygon(new Envelope { MinX = 10.0, MinY = 10.0, MaxX = 12.0, MaxY = 12.0 });
        Geometry homogeneous = factory.CreateGeometryCollection([highPoint, lowPoint]);
        Geometry singleton = factory.CreateGeometryCollection([highPoint]);
        Geometry heterogeneous = factory.CreateGeometryCollection([polygon, line, lowPoint]);

        Geometry narrowedMulti = GeometryNormalization.NormalizeGeometryValue(homogeneous);
        Geometry narrowedSingleton = GeometryNormalization.NormalizeGeometryValue(singleton);
        Geometry normalizedCollection = GeometryNormalization.NormalizeGeometryValue(heterogeneous);

        Assert.Multiple(() =>
        {
            Assert.That(narrowedMulti.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(narrowedMulti.MultiPoint!.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(narrowedMulti.MultiPoint.Points[1].Coordinate!.X, Is.EqualTo(5.0));
            Assert.That(narrowedSingleton.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(narrowedSingleton.Point!.Coordinate!.X, Is.EqualTo(5.0));
            Assert.That(normalizedCollection.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(normalizedCollection.GeometryCollection!.Geometries[0].GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(normalizedCollection.GeometryCollection.Geometries[1].GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(normalizedCollection.GeometryCollection.Geometries[1].LineString!.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(normalizedCollection.GeometryCollection.Geometries[2].GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(normalizedCollection.SpatialReferenceId, Is.EqualTo(4326));
        });
    }

    [Test]
    public void NormalizationIsIdempotentAndSupportsEveryCanonicalEmptyType()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        GeometryType[] types = AllGeometryTypes();
        foreach (GeometryType type in types)
        {
            Geometry source = factory.CreateEmptyGeometry(type);
            Geometry first = GeometryNormalization.NormalizeGeometryValue(source);
            Geometry second = GeometryNormalization.NormalizeGeometryValue(first);
            Assert.Multiple(() =>
            {
                Assert.That(first.GeometryType, Is.EqualTo(type), type.ToString());
                Assert.That(second.GeometryType, Is.EqualTo(type), type.ToString());
                Assert.That(GeometryNormalization.CompareGeometryValues(first, second), Is.EqualTo(0), type.ToString());
                Assert.That(first, Is.Not.SameAs(source), type.ToString());
                Assert.That(second, Is.Not.SameAs(first), type.ToString());
            });
        }

        Geometry polygon = factory.CreatePolygon(
            RingCounterClockwise(0.0, 0.0, 10.0, 10.0),
            [RingCounterClockwise(6.0, 6.0, 8.0, 8.0), RingCounterClockwise(2.0, 2.0, 4.0, 4.0)]);
        Geometry normalized = GeometryNormalization.NormalizeGeometryValue(polygon);
        Geometry normalizedAgain = GeometryNormalization.NormalizeGeometryValue(normalized);
        Assert.That(GeometryNormalization.CompareGeometryValues(normalized, normalizedAgain), Is.EqualTo(0));
    }

    [Test]
    public void ExtendedSurfaceAndCurveContainersReceiveDeterministicOrdering()
    {
        Geometry firstCircular = CircularGeometry(5.0, 4.0, 3.0);
        Geometry secondCircular = CircularGeometry(2.0, 1.0, 0.0);
        Geometry multiCurve = new Geometry
        {
            GeometryType = GeometryType.MultiCurve,
            Point = null,
            MultiCurve = new MultiCurve { Curves = [firstCircular, secondCircular] }
        };
        Geometry normalizedCurve = GeometryNormalization.NormalizeGeometryValue(multiCurve);

        Triangle highTriangle = new Triangle { Shell = TriangleRing(10.0) };
        Triangle lowTriangle = new Triangle { Shell = TriangleRing(1.0) };
        Geometry tin = new Geometry
        {
            GeometryType = GeometryType.Tin,
            Point = null,
            Tin = new Tin { Triangles = [highTriangle, lowTriangle] }
        };
        Geometry normalizedTin = GeometryNormalization.NormalizeGeometryValue(tin);

        Assert.Multiple(() =>
        {
            Assert.That(normalizedCurve.MultiCurve!.Curves[0].CircularString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(normalizedCurve.MultiCurve.Curves[1].CircularString!.Coordinates[0].X, Is.EqualTo(3.0));
            Assert.That(normalizedTin.Tin!.Triangles[0].Shell.Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(normalizedTin.Tin.Triangles[1].Shell.Coordinates[0].X, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void CloneAndNormalizationRejectCyclesMismatchesAndMalformedSequences()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry cyclic = factory.CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = [cyclic];
        Geometry mismatch = new Geometry { GeometryType = GeometryType.LineString };
        Geometry malformedLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [CoordinateValue(0.0, 0.0, double.NaN, double.NaN)] }
        };
        Geometry malformedRing = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing
            {
                Coordinates =
                [
                    CoordinateValue(0.0, 0.0, double.NaN, double.NaN),
                    CoordinateValue(1.0, 0.0, double.NaN, double.NaN),
                    CoordinateValue(1.0, 1.0, double.NaN, double.NaN),
                    CoordinateValue(0.0, 1.0, double.NaN, double.NaN)
                ]
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.CloneGeometryValue(cyclic));
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.NormalizeGeometryValue(cyclic));
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.CloneGeometryValue(mismatch));
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.NormalizeGeometryValue(mismatch));
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.NormalizeGeometryValue(malformedLine));
            Assert.Throws<InvalidGeometryException>(() => GeometryNormalization.NormalizeGeometryValue(malformedRing));
        });
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static LinearRing RingCounterClockwise(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        Coordinate first = CoordinateValue(minimumX, minimumY, double.NaN, double.NaN);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(maximumX, minimumY, double.NaN, double.NaN),
                CoordinateValue(maximumX, maximumY, double.NaN, double.NaN),
                CoordinateValue(minimumX, maximumY, double.NaN, double.NaN),
                first
            ]
        };
    }

    private static LinearRing TriangleRing(double minimum)
    {
        Coordinate first = CoordinateValue(minimum, minimum, double.NaN, double.NaN);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(minimum + 2.0, minimum, double.NaN, double.NaN),
                CoordinateValue(minimum + 1.0, minimum + 2.0, double.NaN, double.NaN),
                first
            ]
        };
    }

    private static Geometry CircularGeometry(double firstX, double middleX, double lastX)
    {
        return new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates =
                [
                    CoordinateValue(firstX, 0.0, double.NaN, double.NaN),
                    CoordinateValue(middleX, 1.0, double.NaN, double.NaN),
                    CoordinateValue(lastX, 0.0, double.NaN, double.NaN)
                ]
            }
        };
    }

    private static double SignedArea(Coordinate[] coordinates)
    {
        double area = 0.0;
        int index = 0;
        while (index < coordinates.Length - 1)
        {
            area += coordinates[index].X * coordinates[index + 1].Y - coordinates[index + 1].X * coordinates[index].Y;
            index++;
        }
        return area / 2.0;
    }

    private static GeometryType[] AllGeometryTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.LinearRing,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection,
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface,
            GeometryType.PolyhedralSurface,
            GeometryType.Tin,
            GeometryType.Triangle
        ];
    }
}
