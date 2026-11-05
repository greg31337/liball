using geo;

namespace geo.Tests;

public class OverlayComprehensiveTests
{
    [Test]
    public void EveryCoreTypePairSupportsAllOverlayOperations()
    {
        Geometry[] geometries = CoreGeometryValues();

        for (int firstIndex = 0;
            firstIndex < geometries.Length; firstIndex++)
        {
            for (int secondIndex = 0;
                secondIndex < geometries.Length; secondIndex++)
            {
                Geometry first = geometries[firstIndex];
                Geometry second = geometries[secondIndex];
                string pair = first.GeometryType + " / " +
                    second.GeometryType;

                Geometry intersection = OverlayEngine.GetIntersection(
                    first, second);
                Geometry union = OverlayEngine.GetUnion(first, second);
                Geometry difference = OverlayEngine.GetDifference(
                    first, second);
                Geometry symmetricDifference =
                    OverlayEngine.GetSymmetricDifference(first, second);

                Assert.Multiple(() =>
                {
                    AssertValidCoreResult(intersection, pair);
                    AssertValidCoreResult(union, pair);
                    AssertValidCoreResult(difference, pair);
                    AssertValidCoreResult(symmetricDifference, pair);
                    Assert.That(intersection.CoordinateLayout,
                        Is.EqualTo(CoordinateLayout.XY), pair);
                    Assert.That(union.SpatialReferenceId,
                        Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId),
                        pair);
                });
            }
        }
    }

    [Test]
    public void CommutativeOperationsAreExactlyStableForEveryCoreTypePair()
    {
        Geometry[] geometries = CoreGeometryValues();

        for (int firstIndex = 0;
            firstIndex < geometries.Length; firstIndex++)
        {
            for (int secondIndex = firstIndex;
                secondIndex < geometries.Length; secondIndex++)
            {
                Geometry first = geometries[firstIndex];
                Geometry second = geometries[secondIndex];
                string pair = first.GeometryType + " / " +
                    second.GeometryType;

                Assert.Multiple(() =>
                {
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        OverlayEngine.GetIntersection(first, second),
                        OverlayEngine.GetIntersection(second, first)),
                        Is.True, "Intersection " + pair);
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        OverlayEngine.GetUnion(first, second),
                        OverlayEngine.GetUnion(second, first)),
                        Is.True, "Union " + pair);
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        OverlayEngine.GetSymmetricDifference(first, second),
                        OverlayEngine.GetSymmetricDifference(second, first)),
                        Is.True, "SymmetricDifference " + pair);
                });
            }
        }
    }

    [Test]
    public void EveryCoreTypeSatisfiesSelfAndEmptySetIdentities()
    {
        Geometry[] geometries = CoreGeometryValues();
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");

        foreach (Geometry geometry in geometries)
        {
            string name = geometry.GeometryType.ToString();
            Geometry selfIntersection =
                OverlayEngine.GetIntersection(geometry, geometry);
            Geometry selfUnion = OverlayEngine.GetUnion(geometry, geometry);
            Geometry selfDifference =
                OverlayEngine.GetDifference(geometry, geometry);
            Geometry selfSymmetricDifference =
                OverlayEngine.GetSymmetricDifference(geometry, geometry);
            Geometry emptyIntersection =
                OverlayEngine.GetIntersection(geometry, empty);
            Geometry emptyUnion = OverlayEngine.GetUnion(geometry, empty);
            Geometry emptyDifference =
                OverlayEngine.GetDifference(geometry, empty);
            Geometry emptySymmetricDifference =
                OverlayEngine.GetSymmetricDifference(geometry, empty);

            Assert.Multiple(() =>
            {
                Assert.That(GeometryEquality.AreTopologicallyEqual(
                    selfIntersection, geometry), Is.True,
                    "Self intersection " + name);
                Assert.That(GeometryEquality.AreTopologicallyEqual(
                    selfUnion, geometry), Is.True,
                    "Self union " + name);
                Assert.That(GeometryInspection.IsEmptyValue(
                    selfDifference), Is.True,
                    "Self difference " + name);
                Assert.That(GeometryInspection.IsEmptyValue(
                    selfSymmetricDifference), Is.True,
                    "Self symmetric difference " + name);
                Assert.That(GeometryInspection.IsEmptyValue(
                    emptyIntersection), Is.True,
                    "Empty intersection " + name);
                Assert.That(GeometryEquality.AreTopologicallyEqual(
                    emptyUnion, geometry), Is.True,
                    "Empty union " + name);
                Assert.That(GeometryEquality.AreTopologicallyEqual(
                    emptyDifference, geometry), Is.True,
                    "Empty difference " + name);
                Assert.That(GeometryEquality.AreTopologicallyEqual(
                    emptySymmetricDifference, geometry), Is.True,
                    "Empty symmetric difference " + name);
            });
        }
    }

    [Test]
    public void EveryCoreEmptyTypePairProducesStableEmptyResults()
    {
        Geometry[] geometries = EmptyCoreGeometryValues();

        for (int firstIndex = 0;
            firstIndex < geometries.Length; firstIndex++)
        {
            for (int secondIndex = 0;
                secondIndex < geometries.Length; secondIndex++)
            {
                Geometry first = geometries[firstIndex];
                Geometry second = geometries[secondIndex];
                string pair = first.GeometryType + " / " +
                    second.GeometryType;
                Geometry intersection = OverlayEngine.GetIntersection(
                    first, second);
                Geometry union = OverlayEngine.GetUnion(first, second);
                Geometry difference = OverlayEngine.GetDifference(
                    first, second);
                Geometry symmetricDifference =
                    OverlayEngine.GetSymmetricDifference(first, second);

                Assert.Multiple(() =>
                {
                    Assert.That(GeometryInspection.IsEmptyValue(
                        intersection), Is.True,
                        "Intersection " + pair);
                    Assert.That(GeometryInspection.IsEmptyValue(union),
                        Is.True, "Union " + pair);
                    Assert.That(GeometryInspection.IsEmptyValue(difference),
                        Is.True, "Difference " + pair);
                    Assert.That(GeometryInspection.IsEmptyValue(
                        symmetricDifference), Is.True,
                        "SymmetricDifference " + pair);
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        intersection,
                        OverlayEngine.GetIntersection(second, first)),
                        Is.True, "Commutative intersection " + pair);
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        union, OverlayEngine.GetUnion(second, first)),
                        Is.True, "Commutative union " + pair);
                    Assert.That(GeometryEquality.AreExactlyEqual(
                        symmetricDifference,
                        OverlayEngine.GetSymmetricDifference(second, first)),
                        Is.True, "Commutative symmetric difference " + pair);
                });
            }
        }
    }

    [Test]
    public void HoleOverlayPreservesDisconnectedRegionsAndAreaIdentities()
    {
        Geometry donut = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (3 3, 7 3, 7 7, 3 7, 3 3))");
        Geometry strip = Read(
            "POLYGON ((4 -2, 6 -2, 6 12, 4 12, 4 -2))");

        Geometry intersection = OverlayEngine.GetIntersection(
            donut, strip);
        Geometry union = OverlayEngine.GetUnion(donut, strip);
        Geometry difference = OverlayEngine.GetDifference(donut, strip);
        Geometry reverseDifference = OverlayEngine.GetDifference(
            strip, donut);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifference(donut, strip);

        Assert.Multiple(() =>
        {
            Assert.That(intersection.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(intersection.MultiPolygon!.Polygons,
                Has.Length.EqualTo(2));
            Assert.That(Area(intersection), Is.EqualTo(12.0));
            Assert.That(Area(union), Is.EqualTo(100.0));
            Assert.That(Area(difference), Is.EqualTo(72.0));
            Assert.That(Area(reverseDifference), Is.EqualTo(16.0));
            Assert.That(Area(symmetricDifference), Is.EqualTo(88.0));
            Assert.That(Area(union), Is.EqualTo(
                Area(donut) + Area(strip) - Area(intersection)));
        });
    }

    [Test]
    public void TouchingAndCrossingIntersectionsUseNarrowestCollapseTypes()
    {
        Geometry square = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry edgeTouch = Read(
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0))");
        Geometry cornerTouch = Read(
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2))");
        Geometry crossing = OverlayEngine.GetIntersection(
            Read("LINESTRING (0 0, 4 4)"),
            Read("LINESTRING (0 4, 4 0)"));

        Geometry edge = OverlayEngine.GetIntersection(
            square, edgeTouch);
        Geometry corner = OverlayEngine.GetIntersection(
            square, cornerTouch);

        Assert.Multiple(() =>
        {
            Assert.That(edge.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(corner.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(crossing.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                edge, Read("LINESTRING (2 0, 2 2)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                corner, Read("POINT (2 2)")), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                crossing, Read("POINT (2 2)")), Is.True);
        });
    }

    [Test]
    public void FixedPrecisionOperationsHonorOffsetGridAndSetAreas()
    {
        Geometry first = Read(
            "POLYGON ((-1.1 -0.1, 1.9 -0.1, 1.9 2.1, -1.1 2.1, -1.1 -0.1))");
        Geometry second = Read(
            "POLYGON ((0.9 -0.1, 3.9 -0.1, 3.9 2.1, 0.9 2.1, 0.9 -0.1))");
        PrecisionModel precision = FixedPrecision(2.0, 0.25, 0.25);

        Geometry intersection =
            OverlayEngine.GetIntersectionWithPrecision(
                first, second, precision);
        Geometry union = OverlayEngine.GetUnionWithPrecision(
            first, second, precision);
        Geometry difference = OverlayEngine.GetDifferenceWithPrecision(
            first, second, precision);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifferenceWithPrecision(
                first, second, precision);

        Assert.Multiple(() =>
        {
            Assert.That(Area(intersection), Is.EqualTo(2.5));
            Assert.That(Area(union), Is.EqualTo(12.5));
            Assert.That(Area(difference), Is.EqualTo(5.0));
            Assert.That(Area(symmetricDifference), Is.EqualTo(10.0));
            Assert.That(UsesGrid(intersection, precision), Is.True);
            Assert.That(UsesGrid(union, precision), Is.True);
            Assert.That(UsesGrid(difference, precision), Is.True);
            Assert.That(UsesGrid(symmetricDifference, precision), Is.True);
        });
    }

    [Test]
    public void OverlayCarriesMatchingSridLayoutZAndMThroughAllOperations()
    {
        Geometry first = ReadWithSrid(
            "SRID=4979;LINESTRING ZM (0 0 0 10, 4 4 4 18)");
        Geometry second = ReadWithSrid(
            "SRID=4979;LINESTRING ZM (0 4 0 10, 4 0 4 18)");

        Geometry intersection = OverlayEngine.GetIntersection(
            first, second);
        Geometry union = OverlayEngine.GetUnion(first, second);
        Geometry difference = OverlayEngine.GetDifference(first, second);
        Geometry symmetricDifference =
            OverlayEngine.GetSymmetricDifference(first, second);
        Coordinate witness = intersection.Point!.Coordinate!;

        Assert.Multiple(() =>
        {
            AssertCoordinateMetadata(intersection, 4979);
            AssertCoordinateMetadata(union, 4979);
            AssertCoordinateMetadata(difference, 4979);
            AssertCoordinateMetadata(symmetricDifference, 4979);
            Assert.That(witness.X, Is.EqualTo(2.0));
            Assert.That(witness.Y, Is.EqualTo(2.0));
            Assert.That(witness.Z, Is.EqualTo(2.0));
            Assert.That(witness.M, Is.EqualTo(14.0));
            Assert.That(HasCoordinate(union, 2.0, 2.0), Is.True);
            Assert.That(HasCoordinate(difference, 2.0, 2.0), Is.True);
        });
    }

    [Test]
    public void OverlayAllowsOneUnknownSridAndRejectsKnownConflicts()
    {
        Geometry unknown = Read("POINT (1 1)");
        Geometry known = ReadWithSrid(
            "SRID=4326;POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        Geometry conflict = ReadWithSrid(
            "SRID=3857;POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");

        Geometry firstResult = OverlayEngine.GetIntersection(
            unknown, known);
        Geometry secondResult = OverlayEngine.GetIntersection(
            known, unknown);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.SpatialReferenceId,
                Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
            Assert.That(secondResult.SpatialReferenceId,
                Is.EqualTo(4326));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetIntersection(known, conflict));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetUnion(known, conflict));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetDifference(known, conflict));
            Assert.Throws<InvalidGeometryException>(() =>
                OverlayEngine.GetSymmetricDifference(known, conflict));
        });
    }

    [Test]
    public void RobustOverlayHandlesLargeNearCoincidentAndRedundantVertices()
    {
        Geometry largeFirst = Read(
            "POLYGON ((1000000000000000 1000000000000000, 1000000000000004 1000000000000000, 1000000000000004 1000000000000004, 1000000000000000 1000000000000004, 1000000000000000 1000000000000000))");
        Geometry largeSecond = Read(
            "POLYGON ((1000000000000002 999999999999999.9, 1000000000000006 999999999999999.9, 1000000000000006 1000000000000003, 1000000000000002 1000000000000003, 1000000000000002 999999999999999.9))");
        Geometry redundantFirst = Read(
            "POLYGON ((0 0, 2 0, 4 0, 4 4, 2 4, 0 4, 0 0))");
        Geometry redundantSecond = Read(
            "POLYGON ((2 -1, 4 -1, 4 0, 4 2, 4 5, 2 5, 2 -1))");

        Geometry largeIntersection = OverlayEngine.GetIntersection(
            largeFirst, largeSecond);
        Geometry redundantIntersection = OverlayEngine.GetIntersection(
            redundantFirst, redundantSecond);
        Geometry redundantUnion = OverlayEngine.GetUnion(
            redundantFirst, redundantSecond);

        Assert.Multiple(() =>
        {
            Assert.That(Area(largeIntersection), Is.EqualTo(6.0));
            Assert.That(Area(redundantIntersection), Is.EqualTo(8.0));
            Assert.That(Area(redundantUnion), Is.EqualTo(20.0));
            Assert.That(GeometryEquality.AreExactlyEqual(
                redundantIntersection,
                OverlayEngine.GetIntersection(
                    redundantSecond, redundantFirst)), Is.True);
        });
    }

    private static Geometry[] CoreGeometryValues()
    {
        return
        [
            Read("POINT (2 2)"),
            Read("LINESTRING (-1 2, 5 2)"),
            Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))"),
            Read("MULTIPOINT ((2 2), (6 6))"),
            Read("MULTILINESTRING ((-1 1, 5 1), (-1 3, 5 3))"),
            Read("MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), ((6 0, 8 0, 8 2, 6 2, 6 0)))"),
            Read("GEOMETRYCOLLECTION (POINT (10 10), LINESTRING (-1 5, 5 5), POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)))")
        ];
    }

    private static Geometry[] EmptyCoreGeometryValues()
    {
        return
        [
            Read("POINT EMPTY"),
            Read("LINESTRING EMPTY"),
            Read("POLYGON EMPTY"),
            Read("MULTIPOINT EMPTY"),
            Read("MULTILINESTRING EMPTY"),
            Read("MULTIPOLYGON EMPTY"),
            Read("GEOMETRYCOLLECTION EMPTY")
        ];
    }

    private static void AssertValidCoreResult(
        Geometry geometry, string message)
    {
        Assert.That(CollectionRelationGraphBuilder.SupportsCoreGeometry(
            geometry), Is.True, message);
        GeometryInspection.ExtractValidatedCoordinates(geometry);
    }

    private static void AssertCoordinateMetadata(
        Geometry geometry, int spatialReferenceId)
    {
        Assert.That(geometry.CoordinateLayout,
            Is.EqualTo(CoordinateLayout.XYZM));
        Assert.That(geometry.SpatialReferenceId,
            Is.EqualTo(spatialReferenceId));
    }

    private static bool HasCoordinate(
        Geometry geometry, double x, double y)
    {
        return GeometryInspection.ExtractValidatedCoordinates(geometry)
            .Any(coordinate => coordinate.X == x && coordinate.Y == y);
    }

    private static double Area(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetProjectedArea(geometry);
    }

    private static bool UsesGrid(
        Geometry geometry, PrecisionModel precision)
    {
        return GeometryInspection.ExtractValidatedCoordinates(geometry)
            .All(coordinate =>
                (coordinate.X - precision.OffsetX) * precision.Scale ==
                    Math.Round(
                        (coordinate.X - precision.OffsetX) *
                        precision.Scale) &&
                (coordinate.Y - precision.OffsetY) * precision.Scale ==
                    Math.Round(
                        (coordinate.Y - precision.OffsetY) *
                        precision.Scale));
    }

    private static PrecisionModel FixedPrecision(
        double scale, double offsetX, double offsetY)
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY
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
