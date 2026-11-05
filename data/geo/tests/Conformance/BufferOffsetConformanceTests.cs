using System.Globalization;
using geo;
using NtsBufferOp = NetTopologySuite.Operation.Buffer.BufferOp;
using NtsBufferParameters = NetTopologySuite.Operation.Buffer.BufferParameters;
using NtsEndCapStyle = NetTopologySuite.Operation.Buffer.EndCapStyle;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using NtsOffsetCurve = NetTopologySuite.Operation.Buffer.OffsetCurve;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class BufferOffsetConformanceTests
{
    [TestCase(BufferCapStyle.Round, BufferJoinStyle.Round)]
    [TestCase(BufferCapStyle.Round, BufferJoinStyle.Mitre)]
    [TestCase(BufferCapStyle.Round, BufferJoinStyle.Bevel)]
    [TestCase(BufferCapStyle.Flat, BufferJoinStyle.Round)]
    [TestCase(BufferCapStyle.Flat, BufferJoinStyle.Mitre)]
    [TestCase(BufferCapStyle.Flat, BufferJoinStyle.Bevel)]
    [TestCase(BufferCapStyle.Square, BufferJoinStyle.Round)]
    [TestCase(BufferCapStyle.Square, BufferJoinStyle.Mitre)]
    [TestCase(BufferCapStyle.Square, BufferJoinStyle.Bevel)]
    public void EveryCapAndJoinCombinationMatchesNetTopologySuite(
        BufferCapStyle capStyle, BufferJoinStyle joinStyle)
    {
        const string text = "LINESTRING (0 0, 8 0, 8 5)";
        BufferParameters parameters = Parameters(
            capStyle, joinStyle, 8, 5.0, FloatingPrecision());

        Geometry actual = BufferBuilder.GetBuffer(
            Read(text), 1.25, parameters);
        NtsGeometry expected = NtsBufferOp.Buffer(
            new NtsWktReader().Read(text), 1.25,
            NtsParameters(parameters));

        AssertNtsAreaEqual(expected, actual,
            capStyle + " cap and " + joinStyle + " join");
    }

    [TestCase(BufferJoinStyle.Round, 2.0)]
    [TestCase(BufferJoinStyle.Round, -2.0)]
    [TestCase(BufferJoinStyle.Mitre, 2.0)]
    [TestCase(BufferJoinStyle.Mitre, -2.0)]
    [TestCase(BufferJoinStyle.Bevel, 2.0)]
    [TestCase(BufferJoinStyle.Bevel, -2.0)]
    public void EveryOffsetJoinAndSideMatchesNetTopologySuite(
        BufferJoinStyle joinStyle, double distance)
    {
        const string text = "LINESTRING (0 0, 8 0, 8 5)";
        BufferParameters parameters = Parameters(
            BufferCapStyle.Flat, joinStyle, 8, 5.0,
            FloatingPrecision());

        Geometry actual = OffsetCurveBuilder.GetOffsetCurve(
            Read(text), distance, parameters);
        NtsGeometry expected = NtsOffsetCurve.GetCurve(
            new NtsWktReader().Read(text), distance, 8,
            NtsJoin(joinStyle), 5.0);

        AssertNtsEqual(expected, actual,
            joinStyle + " offset at " + distance);
    }

    [Test]
    public void DistanceSignsCoverPointsLinesPolygonsAndSingleSidedModes()
    {
        BufferParameters round = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round,
            8, 5.0, FloatingPrecision());
        Geometry point = Read("POINT (0 0)");
        Geometry line = Read("LINESTRING (0 0, 4 0)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0))");
        Geometry positivePoint = BufferBuilder.GetBuffer(point, 1.0, round);
        Geometry negativePoint = BufferBuilder.GetBuffer(point, -1.0, round);
        Geometry zeroLine = BufferBuilder.GetBuffer(line, 0.0, round);
        Geometry negativeLine = BufferBuilder.GetBuffer(line, -1.0, round);
        Geometry zeroPolygon = BufferBuilder.GetBuffer(polygon, 0.0, round);
        Geometry negativePolygon = BufferBuilder.GetBuffer(
            polygon, -1.0, round);
        round.SingleSided = true;
        Geometry left = BufferBuilder.GetBuffer(line, 1.0, round);
        Geometry right = BufferBuilder.GetBuffer(line, -1.0, round);
        Geometry zeroSingle = BufferBuilder.GetBuffer(line, 0.0, round);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryInspection.IsEmptyValue(positivePoint),
                Is.False);
            Assert.That(ContainsCoordinate(
                positivePoint, -1.0, 0.0), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(negativePoint),
                Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(zeroLine), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(negativeLine), Is.True);
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                zeroPolygon, polygon), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                negativePolygon), Is.LessThan(64.0));
            AssertEnvelope(left, 0.0, 0.0, 4.0, 1.0);
            AssertEnvelope(right, 0.0, -1.0, 4.0, 0.0);
            Assert.That(GeometryInspection.IsEmptyValue(zeroSingle), Is.True);
        });
    }

    [Test]
    public void InvalidBufferAndOffsetOptionsReportTheirSpecificFailures()
    {
        Geometry line = Read("LINESTRING (0 0, 4 0)");
        BufferParameters invalidCap = Parameters(
            (BufferCapStyle)99, BufferJoinStyle.Round,
            8, 5.0, FloatingPrecision());
        BufferParameters invalidJoin = Parameters(
            BufferCapStyle.Round, (BufferJoinStyle)99,
            8, 5.0, FloatingPrecision());
        BufferParameters invalidQuadrants = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round,
            0, 5.0, FloatingPrecision());
        BufferParameters invalidMitre = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre,
            8, double.PositiveInfinity, FloatingPrecision());
        BufferParameters nullPrecision = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round,
            8, 5.0, FloatingPrecision());
        nullPrecision.Precision = null!;
        BufferParameters invalidPrecision = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round,
            8, 5.0, new PrecisionModel
            {
                Type = PrecisionModelType.Fixed,
                Scale = 0.0
            });

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, invalidCap));
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, invalidJoin));
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, invalidQuadrants));
            Assert.Throws<InvalidGeometryException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, invalidMitre));
            Assert.Throws<PrecisionException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, nullPrecision));
            Assert.Throws<PrecisionException>(() =>
                BufferBuilder.GetBuffer(line, 1.0, invalidPrecision));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetCurveBuilder.GetOffsetCurve(line, 1.0, invalidCap));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetCurveBuilder.GetOffsetCurve(line, 1.0, invalidJoin));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetCurveBuilder.GetOffsetCurve(
                    line, double.NegativeInfinity,
                    Parameters(BufferCapStyle.Flat,
                        BufferJoinStyle.Round, 8, 5.0,
                        FloatingPrecision())));
        });
    }

    [Test]
    public void HolesCollectionsCollapseAndContainmentInvariantsArePreserved()
    {
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre,
            8, 5.0, FloatingPrecision());
        Geometry donut = Read(
            "POLYGON ((0 0, 12 0, 12 12, 0 12, 0 0), " +
            "(4 4, 4 8, 8 8, 8 4, 4 4))");
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (0 0), " +
            "LINESTRING (0 0, 5 0), " +
            "POLYGON ((4 -1, 7 -1, 7 1, 4 1, 4 -1)))");
        Geometry expanded = BufferBuilder.GetBuffer(donut, 1.0, parameters);
        Geometry eroded = BufferBuilder.GetBuffer(donut, -1.0, parameters);
        Geometry collapsed = BufferBuilder.GetBuffer(donut, -5.0, parameters);
        BufferParameters collectionParameters = Parameters(
            BufferCapStyle.Square, BufferJoinStyle.Mitre,
            8, 5.0, FloatingPrecision());
        Geometry combined = BufferBuilder.GetBuffer(
            collection, 1.0, collectionParameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryPredicateAlgorithms.Covers(expanded, donut),
                Is.True);
            Assert.That(GeometryPredicateAlgorithms.Covers(donut, eroded),
                Is.True);
            Assert.That(expanded.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(eroded.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(GeometryInspection.IsEmptyValue(collapsed), Is.True);
            Assert.That(combined.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(FullyNodedValidator.ValidateOutput(
                GeometrySplitter.CreateSplitterEdges(
                    new RelationGeometryParts(combined))).IsValid, Is.True);
        });
    }

    [Test]
    public void FixedPrecisionSnapsBufferAndOffsetAndCanCollapseLinework()
    {
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0,
            OffsetX = 0.25,
            OffsetY = 0.25
        };
        BufferParameters parameters = Parameters(
            BufferCapStyle.Flat, BufferJoinStyle.Bevel,
            8, 5.0, precision);
        Geometry source = Read("LINESTRING (0.2 0.2, 4.2 0.2, 4.2 3.2)");
        Geometry buffer = BufferBuilder.GetBuffer(source, 0.6, parameters);
        Geometry offset = OffsetCurveBuilder.GetOffsetCurve(
            source, -0.6, parameters);
        Geometry collapsed = OffsetCurveBuilder.GetOffsetCurve(
            Read("LINESTRING (0.1 0.1, 0.2 0.1)"), 0.1,
            Parameters(BufferCapStyle.Flat, BufferJoinStyle.Round,
                8, 5.0, new PrecisionModel
                {
                    Type = PrecisionModelType.Fixed,
                    Scale = 1.0
                }));

        Assert.Multiple(() =>
        {
            Assert.That(CoordinatesUseGrid(buffer, precision), Is.True);
            Assert.That(CoordinatesUseGrid(offset, precision), Is.True);
            Assert.That(GeometryInspection.IsEmptyValue(collapsed), Is.True);
            Assert.That(source.LineString!.Coordinates[0].X,
                Is.EqualTo(0.2));
        });
    }

    [Test]
    public void SeededAxisAlignedBuffersMatchNetTopologySuite()
    {
        const int seed = 94001;
        const int caseCount = 40;
        Random random = new Random(seed);
        NtsWktReader reader = new NtsWktReader();
        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            int minimumX = random.Next(-50, 40);
            int minimumY = random.Next(-50, 40);
            int maximumX = minimumX + random.Next(2, 12);
            int maximumY = minimumY + random.Next(2, 12);
            int distance = random.Next(1, 4);
            string text = RectangleText(
                minimumX, minimumY, maximumX, maximumY);
            BufferParameters parameters = Parameters(
                BufferCapStyle.Flat, BufferJoinStyle.Mitre,
                8, 5.0, FloatingPrecision());
            Geometry actual = BufferBuilder.GetBuffer(
                Read(text), distance, parameters);
            NtsGeometry expected = NtsBufferOp.Buffer(
                reader.Read(text), distance, NtsParameters(parameters));

            AssertNtsAreaEqual(expected, actual,
                Description(seed, caseIndex, text));
            caseIndex++;
        }
    }

    [Test]
    public void SeededStraightLineCapBuffersMatchNetTopologySuite()
    {
        const int seed = 94002;
        const int caseCount = 36;
        Random random = new Random(seed);
        NtsWktReader reader = new NtsWktReader();
        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            int startX = random.Next(-30, 20);
            int startY = random.Next(-30, 20);
            int endX = startX + random.Next(2, 20);
            int endY = startY;
            int distance = random.Next(1, 5);
            BufferCapStyle cap = (BufferCapStyle)(caseIndex % 3 + 1);
            string text = FormattableString.Invariant(
                $"LINESTRING ({startX} {startY}, {endX} {endY})");
            BufferParameters parameters = Parameters(
                cap, BufferJoinStyle.Round, 8, 5.0,
                FloatingPrecision());
            Geometry actual = BufferBuilder.GetBuffer(
                Read(text), distance, parameters);
            NtsGeometry expected = NtsBufferOp.Buffer(
                reader.Read(text), distance, NtsParameters(parameters));

            AssertNtsAreaEqual(expected, actual,
                Description(seed, caseIndex, text));
            caseIndex++;
        }
    }

    private static NtsBufferParameters NtsParameters(
        BufferParameters parameters)
    {
        NtsBufferParameters result = new NtsBufferParameters();
        result.EndCapStyle = NtsCap(parameters.CapStyle);
        result.JoinStyle = NtsJoin(parameters.JoinStyle);
        result.QuadrantSegments = parameters.QuadrantSegments;
        result.MitreLimit = parameters.MitreLimit;
        result.IsSingleSided = parameters.SingleSided;
        return result;
    }

    private static NtsEndCapStyle NtsCap(BufferCapStyle style)
    {
        if (style == BufferCapStyle.Flat) return NtsEndCapStyle.Flat;
        if (style == BufferCapStyle.Square) return NtsEndCapStyle.Square;
        return NtsEndCapStyle.Round;
    }

    private static NtsJoinStyle NtsJoin(BufferJoinStyle style)
    {
        if (style == BufferJoinStyle.Mitre) return NtsJoinStyle.Mitre;
        if (style == BufferJoinStyle.Bevel) return NtsJoinStyle.Bevel;
        return NtsJoinStyle.Round;
    }

    private static void AssertNtsEqual(
        NtsGeometry expected, Geometry actual, string description)
    {
        NtsGeometry actualNts = new NtsWktReader().Read(Write(actual));
        if (expected.IsEmpty || actualNts.IsEmpty)
        {
            Assert.That(actualNts.IsEmpty,
                Is.EqualTo(expected.IsEmpty), description);
            return;
        }
        Assert.That(actualNts.EqualsTopologically(expected), Is.True,
            description + Environment.NewLine +
            "expected: " + expected + Environment.NewLine +
            "actual: " + actualNts);
    }

    private static void AssertNtsAreaEqual(
        NtsGeometry expected, Geometry actual, string description)
    {
        NtsGeometry actualNts = new NtsWktReader().Read(Write(actual));
        if (expected.IsEmpty || actualNts.IsEmpty)
        {
            Assert.That(actualNts.IsEmpty,
                Is.EqualTo(expected.IsEmpty), description);
            return;
        }
        double differenceArea =
            expected.SymmetricDifference(actualNts).Area;
        Assert.Multiple(() =>
        {
            Assert.That(actualNts.Area,
                Is.EqualTo(expected.Area).Within(0.000000001), description);
            Assert.That(differenceArea, Is.LessThan(0.000000001),
                description + Environment.NewLine +
                "expected: " + expected + Environment.NewLine +
                "actual: " + actualNts);
        });
    }

    private static bool ContainsCoordinate(
        Geometry geometry, double x, double y)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.Any(coordinate =>
            coordinate.X == x && coordinate.Y == y);
    }

    private static bool CoordinatesUseGrid(
        Geometry geometry, PrecisionModel precision)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.All(coordinate =>
            IsOnGrid(coordinate.X, precision.OffsetX, precision.Scale) &&
            IsOnGrid(coordinate.Y, precision.OffsetY, precision.Scale));
    }

    private static bool IsOnGrid(
        double value, double offset, double scale)
    {
        double grid = (value - offset) * scale;
        return grid == Math.Round(grid);
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
        int quadrantSegments, double mitreLimit,
        PrecisionModel precision)
    {
        return new BufferParameters
        {
            CapStyle = capStyle,
            JoinStyle = joinStyle,
            QuadrantSegments = quadrantSegments,
            MitreLimit = mitreLimit,
            Precision = precision
        };
    }

    private static PrecisionModel FloatingPrecision()
    {
        return new PrecisionModel { Type = PrecisionModelType.Floating };
    }

    private static string RectangleText(
        int minimumX, int minimumY, int maximumX, int maximumY)
    {
        return FormattableString.Invariant(
            $"POLYGON (({minimumX} {minimumY}, {maximumX} {minimumY}, {maximumX} {maximumY}, {minimumX} {maximumY}, {minimumX} {minimumY}))");
    }

    private static string Description(
        int seed, int caseIndex, string input)
    {
        return FormattableString.Invariant(
            $"seed {seed}, case {caseIndex}: {input}");
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
    }
}
