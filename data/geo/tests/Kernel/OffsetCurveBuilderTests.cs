using geo;

namespace geo.Tests;

public class OffsetCurveBuilderTests
{
    [Test]
    public void SignedDistanceSelectsLeftAndRightAndPreservesOrdinates()
    {
        Geometry source = Read("LINESTRING ZM (0 0 3 7, 10 0 5 9)");
        BufferParameters parameters = Parameters(
            BufferJoinStyle.Round, FloatingPrecision());

        Geometry left = OffsetCurveBuilder.GetOffsetCurve(
            source, 2.0, parameters);
        Geometry right = OffsetCurveBuilder.GetOffsetCurve(
            source, -2.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(left.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            AssertCoordinate(left.LineString!.Coordinates[0],
                0.0, 2.0, 3.0, 7.0);
            AssertCoordinate(left.LineString.Coordinates[1],
                10.0, 2.0, 5.0, 9.0);
            AssertCoordinate(right.LineString!.Coordinates[0],
                0.0, -2.0, 3.0, 7.0);
            AssertCoordinate(right.LineString.Coordinates[1],
                10.0, -2.0, 5.0, 9.0);
            AssertCoordinate(source.LineString!.Coordinates[0],
                0.0, 0.0, 3.0, 7.0);
        });
    }

    [Test]
    public void JoinedOffsetsHonorInsideMitreBevelAndRoundPolicies()
    {
        Geometry source = Read("LINESTRING (0 0, 10 0, 10 10)");
        Geometry inside = OffsetCurveBuilder.GetOffsetCurve(
            source, 2.0,
            Parameters(BufferJoinStyle.Round, FloatingPrecision()));
        Geometry mitre = OffsetCurveBuilder.GetOffsetCurve(
            source, -2.0,
            Parameters(BufferJoinStyle.Mitre, FloatingPrecision()));
        Geometry bevel = OffsetCurveBuilder.GetOffsetCurve(
            source, -2.0,
            Parameters(BufferJoinStyle.Bevel, FloatingPrecision()));
        Geometry round = OffsetCurveBuilder.GetOffsetCurve(
            source, -2.0,
            Parameters(BufferJoinStyle.Round, FloatingPrecision()));

        Assert.Multiple(() =>
        {
            AssertLine(inside.LineString!.Coordinates,
                [(0.0, 2.0), (8.0, 2.0), (8.0, 10.0)]);
            AssertLine(mitre.LineString!.Coordinates,
                [(0.0, -2.0), (12.0, -2.0), (12.0, 10.0)]);
            AssertLine(bevel.LineString!.Coordinates,
                [(0.0, -2.0), (10.0, -2.0),
                    (12.0, 0.0), (12.0, 10.0)]);
            Assert.That(round.LineString!.Coordinates, Has.Length.EqualTo(11));
            AssertCoordinate(round.LineString.Coordinates[0], 0.0, -2.0);
            AssertCoordinate(round.LineString.Coordinates[10], 12.0, 10.0);
        });
    }

    [Test]
    public void ClosedOffsetsRemainClosedAndCollapsedInteriorIsEmpty()
    {
        Geometry ring = Read(
            "LINESTRING (0 0, 10 0, 10 10, 0 10, 0 0)");
        BufferParameters parameters = Parameters(
            BufferJoinStyle.Mitre, FloatingPrecision());

        Geometry interior = OffsetCurveBuilder.GetOffsetCurve(
            ring, 1.0, parameters);
        Geometry exterior = OffsetCurveBuilder.GetOffsetCurve(
            ring, -1.0, parameters);
        Geometry collapsed = OffsetCurveBuilder.GetOffsetCurve(
            ring, 5.0, parameters);

        Assert.Multiple(() =>
        {
            AssertLine(interior.LineString!.Coordinates,
                [(1.0, 1.0), (9.0, 1.0), (9.0, 9.0),
                    (1.0, 9.0), (1.0, 1.0)]);
            AssertLine(exterior.LineString!.Coordinates,
                [(-1.0, -1.0), (11.0, -1.0), (11.0, 11.0),
                    (-1.0, 11.0), (-1.0, -1.0)]);
            Assert.That(collapsed.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(GeometryInspection.IsEmptyValue(collapsed), Is.True);
        });
    }

    [Test]
    public void DisjointInputSectionsKeepComponentAndCoordinateOrder()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 4 0), (10 0, 14 0))");

        Geometry result = OffsetCurveBuilder.GetOffsetCurve(
            source, 1.0,
            Parameters(BufferJoinStyle.Round, FloatingPrecision()));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(result.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            AssertLine(result.MultiLineString.LineStrings[0].Coordinates,
                [(0.0, 1.0), (4.0, 1.0)]);
            AssertLine(result.MultiLineString.LineStrings[1].Coordinates,
                [(10.0, 1.0), (14.0, 1.0)]);
        });
    }

    [Test]
    public void RawLoopsAreRemovedIntoDeterministicDisjointSections()
    {
        Geometry source = Read(
            "LINESTRING (2 4, 4 20, 6 11, 18 4, 18 1, 18 12)");
        BufferParameters parameters = Parameters(
            BufferJoinStyle.Round, FloatingPrecision());
        Coordinate[] raw = BufferCurveBuilder.CreateSideCurve(
            source.LineString!.Coordinates,
            OffsetSegmentGenerator.LeftSide, 2.0, parameters);

        Geometry first = OffsetCurveBuilder.GetOffsetCurve(
            source, 2.0, parameters);
        Geometry second = OffsetCurveBuilder.GetOffsetCurve(
            source, 2.0, parameters);
        Coordinate[][] sections = GetSections(first);
        Geometry boundary = GeometryBoundary.CreateBoundaryValue(
            BufferBuilder.GetBuffer(source, 2.0, parameters),
            BoundaryNodeRule.Mod2);
        SegmentString[] boundaryEdges = GeometrySplitter.CreateSplitterEdges(
            new RelationGeometryParts(boundary));

        Assert.Multiple(() =>
        {
            Assert.That(raw.Length, Is.GreaterThan(2));
            Assert.That(FullyNodedValidator.ValidateOutput(
                [SegmentNoding.CreateSegmentString(raw, null, 0)]).IsValid,
                Is.False);
            Assert.That(first.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(first.MultiLineString!.LineStrings,
                Has.Length.EqualTo(3));
            Assert.That(TotalSegmentCount(sections),
                Is.LessThan(raw.Length - 1));
            Assert.That(AllSectionSegmentsAreOnBoundary(
                sections, boundaryEdges), Is.True);
            Assert.That(Write(first), Is.EqualTo(Write(second)));
        });
    }

    [Test]
    public void PrecisionCollapseZeroDistanceAndInvalidKindsAreDefined()
    {
        PrecisionModel fixedPrecision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 1.0
        };
        Geometry collapsed = OffsetCurveBuilder.GetOffsetCurve(
            Read("LINESTRING (0.1 0.1, 0.2 0.1)"), 0.1,
            Parameters(BufferJoinStyle.Round, fixedPrecision));
        Geometry source = ReadWithSrid(
            "SRID=4326;LINESTRING (0 0, 2 1)");
        Geometry zero = OffsetCurveBuilder.GetOffsetCurve(
            source, 0.0,
            Parameters(BufferJoinStyle.Round, FloatingPrecision()));

        source.LineString!.Coordinates[0].X = 99.0;

        Assert.Multiple(() =>
        {
            Assert.That(collapsed.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(GeometryInspection.IsEmptyValue(collapsed), Is.True);
            Assert.That(zero.SpatialReferenceId, Is.EqualTo(4326));
            AssertCoordinate(zero.LineString!.Coordinates[0], 0.0, 0.0);
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetCurveBuilder.GetOffsetCurve(
                    Read("POINT (0 0)"), 1.0,
                    Parameters(BufferJoinStyle.Round, FloatingPrecision())));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetCurveBuilder.GetOffsetCurve(
                    Read("LINESTRING (0 0, 1 0)"), double.NaN,
                    Parameters(BufferJoinStyle.Round, FloatingPrecision())));
        });
    }

    private static Coordinate[][] GetSections(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return [geometry.LineString!.Coordinates];
        }
        LineString[] lines = geometry.MultiLineString!.LineStrings;
        Coordinate[][] coordinates = new Coordinate[lines.Length][];
        int index = 0;
        while (index < lines.Length)
        {
            coordinates[index] = lines[index].Coordinates;
            index++;
        }
        return coordinates;
    }

    private static int TotalSegmentCount(Coordinate[][] sections)
    {
        int count = 0;
        int index = 0;
        while (index < sections.Length)
        {
            count += sections[index].Length - 1;
            index++;
        }
        return count;
    }

    private static bool AllSectionSegmentsAreOnBoundary(
        Coordinate[][] sections, SegmentString[] boundaryEdges)
    {
        int sectionIndex = 0;
        while (sectionIndex < sections.Length)
        {
            Coordinate[] coordinates = sections[sectionIndex];
            int coordinateIndex = 0;
            while (coordinateIndex + 1 < coordinates.Length)
            {
                Coordinate midpoint =
                    RobustLineIntersection.InterpolateLineCoordinate(
                        coordinates[coordinateIndex],
                        coordinates[coordinateIndex + 1], 0.5);
                if (!OffsetCurveBuilder.IsPointOnEdges(
                    midpoint, boundaryEdges, 0.0002)) return false;
                coordinateIndex++;
            }
            sectionIndex++;
        }
        return true;
    }

    private static void AssertLine(
        Coordinate[] actual, (double X, double Y)[] expected)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            AssertCoordinate(actual[index],
                expected[index].X, expected[index].Y);
            index++;
        }
    }

    private static void AssertCoordinate(
        Coordinate actual, double x, double y)
    {
        Assert.That(actual.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(actual.Y, Is.EqualTo(y).Within(1e-12));
    }

    private static void AssertCoordinate(
        Coordinate actual, double x, double y, double z, double m)
    {
        AssertCoordinate(actual, x, y);
        Assert.That(actual.Z, Is.EqualTo(z));
        Assert.That(actual.M, Is.EqualTo(m));
    }

    private static BufferParameters Parameters(
        BufferJoinStyle joinStyle, PrecisionModel precision)
    {
        return new BufferParameters
        {
            CapStyle = BufferCapStyle.Flat,
            JoinStyle = joinStyle,
            QuadrantSegments = 8,
            MitreLimit = 5.0,
            Precision = precision
        };
    }

    private static PrecisionModel FloatingPrecision()
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Floating
        };
    }

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
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
