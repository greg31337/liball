using geo;

namespace geo.Tests;

public class BufferBuilderTests
{
    [Test]
    public void PointBuffersHonorRoundSquareFlatAndDistanceSigns()
    {
        Geometry point = Read("POINT (2 3)");
        BufferParameters roundParameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0);
        BufferParameters squareParameters = Parameters(
            BufferCapStyle.Square, BufferJoinStyle.Round, 8, 5.0);
        BufferParameters flatParameters = Parameters(
            BufferCapStyle.Flat, BufferJoinStyle.Round, 8, 5.0);

        Geometry round = BufferBuilder.GetBuffer(
            point, 2.0, roundParameters);
        Geometry square = BufferBuilder.GetBuffer(
            point, 2.0, squareParameters);
        Geometry flat = BufferBuilder.GetBuffer(
            point, 2.0, flatParameters);
        Geometry zero = BufferBuilder.GetBuffer(
            point, 0.0, roundParameters);
        Geometry negative = BufferBuilder.GetBuffer(
            point, -2.0, roundParameters);
        double expectedRoundArea = RegularCircleArea(2.0, 8);

        Assert.Multiple(() =>
        {
            Assert.That(round.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(round),
                Is.EqualTo(expectedRoundArea).Within(1e-12));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(square),
                Is.EqualTo(16.0));
            Assert.That(GeometryInspection.IsEmptyValue(flat), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(zero), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(negative), Is.True);
        });
    }

    [Test]
    public void OpenLineBuffersHonorEveryEndCapStyle()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        Geometry round = BufferBuilder.GetBuffer(
            line, 1.0,
            Parameters(BufferCapStyle.Round,
                BufferJoinStyle.Round, 4, 5.0));
        Geometry flat = BufferBuilder.GetBuffer(
            line, 1.0,
            Parameters(BufferCapStyle.Flat,
                BufferJoinStyle.Round, 4, 5.0));
        Geometry square = BufferBuilder.GetBuffer(
            line, 1.0,
            Parameters(BufferCapStyle.Square,
                BufferJoinStyle.Round, 4, 5.0));

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(round),
                Is.EqualTo(20.0 + RegularCircleArea(1.0, 4))
                    .Within(1e-12));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(flat),
                Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(square),
                Is.EqualTo(24.0));
            AssertEnvelope(round, -1.0, -1.0, 11.0, 1.0);
            AssertEnvelope(flat, 0.0, -1.0, 10.0, 1.0);
            AssertEnvelope(square, -1.0, -1.0, 11.0, 1.0);
        });
    }

    [Test]
    public void LineCornersHonorRoundMitreBevelAndMitreLimit()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0, 10 10)");
        Geometry round = BufferBuilder.GetBuffer(
            line, 2.0,
            Parameters(BufferCapStyle.Flat,
                BufferJoinStyle.Round, 8, 5.0));
        Geometry mitre = BufferBuilder.GetBuffer(
            line, 2.0,
            Parameters(BufferCapStyle.Flat,
                BufferJoinStyle.Mitre, 8, 5.0));
        Geometry bevel = BufferBuilder.GetBuffer(
            line, 2.0,
            Parameters(BufferCapStyle.Flat,
                BufferJoinStyle.Bevel, 8, 5.0));
        Geometry limited = BufferBuilder.GetBuffer(
            line, 2.0,
            Parameters(BufferCapStyle.Flat,
                BufferJoinStyle.Mitre, 8, 1.0));

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(round),
                Is.GreaterThan(
                    GeometryMeasureAlgorithms.GetProjectedArea(bevel)));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(mitre),
                Is.GreaterThan(
                    GeometryMeasureAlgorithms.GetProjectedArea(round)));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                bevel, limited), Is.True);
            Assert.That(GeometryPointLocation.LocatePoint(
                Point(11.9, -1.9), mitre, CurveParameters()),
                Is.Not.EqualTo(TopologicalLocation.Exterior));
            Assert.That(GeometryPointLocation.LocatePoint(
                Point(11.9, -1.9), bevel, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void PolygonBuffersExpandErodeAndPreserveZeroArea()
    {
        Geometry square = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre, 8, 5.0);

        Geometry expanded = BufferBuilder.GetBuffer(
            square, 2.0, parameters);
        Geometry eroded = BufferBuilder.GetBuffer(
            square, -2.0, parameters);
        Geometry zero = BufferBuilder.GetBuffer(
            square, 0.0, parameters);
        Geometry collapsed = BufferBuilder.GetBuffer(
            square, -6.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(expanded),
                Is.EqualTo(196.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(eroded),
                Is.EqualTo(36.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(zero),
                Is.EqualTo(100.0));
            Assert.That(GeometryInspection.IsEmptyValue(collapsed), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                zero, square), Is.True);
        });
    }

    [Test]
    public void PolygonHoleBuffersShrinkAndGrowWithDistanceSign()
    {
        Geometry donut = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (3 3, 3 7, 7 7, 7 3, 3 3))");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre, 8, 5.0);

        Geometry expanded = BufferBuilder.GetBuffer(
            donut, 1.0, parameters);
        Geometry eroded = BufferBuilder.GetBuffer(
            donut, -1.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(expanded),
                Is.EqualTo(140.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(eroded),
                Is.EqualTo(28.0));
            Assert.That(expanded.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(eroded.Polygon!.Holes, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SingleSidedLineBuffersSelectLeftOrRightWithoutEndCaps()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0);
        parameters.SingleSided = true;

        Geometry left = BufferBuilder.GetBuffer(line, 1.0, parameters);
        Geometry right = BufferBuilder.GetBuffer(line, -1.0, parameters);
        Geometry zero = BufferBuilder.GetBuffer(line, 0.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(left),
                Is.EqualTo(10.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(right),
                Is.EqualTo(10.0));
            AssertEnvelope(left, 0.0, 0.0, 10.0, 1.0);
            AssertEnvelope(right, 0.0, -1.0, 10.0, 0.0);
            Assert.That(GeometryInspection.IsEmptyValue(zero), Is.True);
        });
    }

    [Test]
    public void CollectionBuffersNodeAndUnionOverlappingComponentCurves()
    {
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (0 0), LINESTRING (0 0, 6 0), POLYGON ((4 -1, 8 -1, 8 1, 4 1, 4 -1)))");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Square, BufferJoinStyle.Mitre, 8, 5.0);

        Geometry result = BufferBuilder.GetBuffer(
            collection, 1.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(32.0));
            AssertEnvelope(result, -1.0, -2.0, 9.0, 2.0);
            Assert.That(FullyNodedValidator.ValidateOutput(
                GeometrySplitter.CreateSplitterEdges(
                    new RelationGeometryParts(result))).IsValid, Is.True);
        });
    }

    [Test]
    public void SelfOverlappingRawLineCurveIsNodedIntoPolygonalOutput()
    {
        Geometry line = Read(
            "LINESTRING (0 0, 10 0, 10 3, 0 3)");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0);

        Geometry result = BufferBuilder.GetBuffer(
            line, 2.0, parameters);
        NodingResult validation = FullyNodedValidator.ValidateOutput(
            GeometrySplitter.CreateSplitterEdges(
                new RelationGeometryParts(result)));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.GreaterThan(40.0));
        });
    }

    [Test]
    public void FixedPrecisionBuffersSnapCurvesAndPreserveMetadataOrdinates()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;LINESTRING Z (0 0 5, 4 0 7)");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0,
            OffsetX = 0.25,
            OffsetY = 0.25
        };
        BufferParameters parameters = Parameters(
            BufferCapStyle.Flat, BufferJoinStyle.Round, 8, 5.0);
        parameters.Precision = precision;

        Geometry result = BufferBuilder.GetBuffer(
            source, 0.6, parameters);
        Coordinate[] coordinates = result.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(coordinates.All(coordinate =>
                IsOnGrid(coordinate.X, precision.OffsetX, precision.Scale) &&
                IsOnGrid(coordinate.Y, precision.OffsetY, precision.Scale)),
                Is.True);
            Assert.That(coordinates.All(coordinate =>
                double.IsFinite(coordinate.Z)), Is.True);
            Assert.That(source.LineString!.Coordinates[0].X,
                Is.EqualTo(0.0));
        });
    }

    [Test]
    public void EmptyNegativeAndInvalidInputsHaveDefinedBehavior()
    {
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0);

        Geometry empty = BufferBuilder.GetBuffer(
            Read("GEOMETRYCOLLECTION EMPTY"), 2.0, parameters);
        Geometry negativeLine = BufferBuilder.GetBuffer(
            Read("LINESTRING (0 0, 2 0)"), -1.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(empty), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(negativeLine), Is.True);
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(
                    Read("POINT (0 0)"), double.NaN, parameters));
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(
                    Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))"),
                    1.0, parameters));
        });
    }

    private static double RegularCircleArea(
        double radius, int quadrantSegments)
    {
        int segmentCount = quadrantSegments * 4;
        return segmentCount * radius * radius *
            Math.Sin(2.0 * Math.PI / segmentCount) / 2.0;
    }

    private static bool IsOnGrid(
        double value, double offset, double scale)
    {
        double gridValue = (value - offset) * scale;
        return gridValue == Math.Round(gridValue);
    }

    private static void AssertEnvelope(
        Geometry geometry, double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        Envelope envelope = EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
        Assert.That(envelope.MinX, Is.EqualTo(minimumX).Within(1e-12));
        Assert.That(envelope.MinY, Is.EqualTo(minimumY).Within(1e-12));
        Assert.That(envelope.MaxX, Is.EqualTo(maximumX).Within(1e-12));
        Assert.That(envelope.MaxY, Is.EqualTo(maximumY).Within(1e-12));
    }

    private static BufferParameters Parameters(
        BufferCapStyle capStyle, BufferJoinStyle joinStyle,
        int quadrantSegments, double mitreLimit)
    {
        return new BufferParameters
        {
            CapStyle = capStyle,
            JoinStyle = joinStyle,
            QuadrantSegments = quadrantSegments,
            MitreLimit = mitreLimit,
            Precision = new PrecisionModel
            {
                Type = PrecisionModelType.Floating
            }
        };
    }

    private static CurveLinearizationParameters CurveParameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 16
        };
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
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
