using System.Globalization;
using System.Text;
using geo;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsUnaryUnionOp = NetTopologySuite.Operation.Union.UnaryUnionOp;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class OverlayOperationRandomizedTests
{
    [Test]
    public void SeededRectangleClippingMatchesNetTopologySuite()
    {
        const int seed = 83001;
        const int caseCount = 48;
        Random random = new Random(seed);
        NtsWktReader ntsReader = new NtsWktReader();

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            string sourceText = CreateRectangleText(random);
            int minimumX = random.Next(-20, 16);
            int minimumY = random.Next(-20, 16);
            int maximumX = minimumX + random.Next(1, 10);
            int maximumY = minimumY + random.Next(1, 10);
            Geometry actual = RectangleClipper.ClipToEnvelope(
                Read(sourceText),
                EnvelopeAlgorithms.CreateBoundsEnvelope(
                    minimumX, minimumY, maximumX, maximumY));
            NtsGeometry expected = ntsReader.Read(sourceText).Intersection(
                ntsReader.Read(CreateRectangleText(
                    minimumX, minimumY, maximumX, maximumY)));

            AssertNtsEqual(
                expected, actual,
                CreateDescription(seed, caseIndex, sourceText));
            caseIndex++;
        }
    }

    [Test]
    public void SeededPointSplittingPreservesOrderTopologyAndLength()
    {
        const int seed = 83002;
        const int caseCount = 32;
        Random random = new Random(seed);
        NtsWktReader ntsReader = new NtsWktReader();

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            int[] splitPositions = CreateDistinctPositions(
                random, random.Next(1, 8), 1, 100);
            string splitterText = CreateMultiPointText(splitPositions);
            Geometry source = Read("LINESTRING (0 0, 100 0)");
            Geometry result = GeometrySplitter.SplitGeometry(
                source, Read(splitterText));
            Geometry[] pieces = result.GeometryCollection!.Geometries;
            double previousEnd = 0.0;

            Assert.That(
                pieces, Has.Length.EqualTo(splitPositions.Length + 1),
                CreateDescription(seed, caseIndex, splitterText));
            int pieceIndex = 0;
            while (pieceIndex < pieces.Length)
            {
                Coordinate[] coordinates =
                    pieces[pieceIndex].LineString!.Coordinates;
                Assert.That(coordinates[0].X, Is.EqualTo(previousEnd),
                    CreateDescription(seed, caseIndex, splitterText));
                previousEnd = coordinates[coordinates.Length - 1].X;
                pieceIndex++;
            }
            Assert.Multiple(() =>
            {
                Assert.That(previousEnd, Is.EqualTo(100.0));
                Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                    Is.EqualTo(100.0));
                Assert.That(ToNts(result).EqualsTopologically(
                    ntsReader.Read("LINESTRING (0 0, 100 0)")), Is.True);
            });
            caseIndex++;
        }
    }

    [Test]
    public void SeededSnappingInsertsEveryNearbyTargetVertex()
    {
        const int seed = 83003;
        const int caseCount = 40;
        Random random = new Random(seed);

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            int targetX = random.Next(1, 100);
            double targetY = random.Next(0, 2) == 0 ? -0.125 : 0.125;
            Geometry result = GeometrySnapper.SnapGeometry(
                Read("LINESTRING (0 0, 100 0)"),
                Read(CreatePointText(targetX, targetY)), 0.25);
            Coordinate[] coordinates = result.LineString!.Coordinates;

            Assert.Multiple(() =>
            {
                Assert.That(coordinates, Has.Length.EqualTo(3),
                    CreateDescription(seed, caseIndex,
                        CreatePointText(targetX, targetY)));
                Assert.That(coordinates[1].X, Is.EqualTo(targetX));
                Assert.That(coordinates[1].Y, Is.EqualTo(targetY));
                Assert.That(coordinates[0].X, Is.EqualTo(0.0));
                Assert.That(coordinates[2].X, Is.EqualTo(100.0));
            });
            caseIndex++;
        }
    }

    [Test]
    public void SeededGridNodingProducesCompleteLengthPreservingFragments()
    {
        const int seed = 83004;
        const int caseCount = 24;
        Random random = new Random(seed);

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            int[] horizontalPositions =
                CreateDistinctPositions(random, 3, 1, 100);
            int[] verticalPositions =
                CreateDistinctPositions(random, 3, 1, 100);
            string sourceText = CreateGridText(
                horizontalPositions, verticalPositions);
            Geometry source = Read(sourceText);
            Geometry result = GeometryNoder.NodeGeometry(source);
            LineString[] lines = GetLines(result);
            NodingResult validation = FullyNodedValidator.ValidateOutput(
                GeometrySplitter.CreateSplitterEdges(
                    new RelationGeometryParts(result)));

            Assert.Multiple(() =>
            {
                Assert.That(lines, Has.Length.EqualTo(24),
                    CreateDescription(seed, caseIndex, sourceText));
                Assert.That(validation.IsValid, Is.True,
                    CreateDescription(seed, caseIndex, sourceText));
                Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                    Is.EqualTo(GeometryMeasureAlgorithms.GetLength(source)));
            });
            caseIndex++;
        }
    }

    [Test]
    public void SeededSharedPathsMatchDirectionalRuns()
    {
        const int seed = 83005;
        const int caseCount = 28;
        Random random = new Random(seed);

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            bool[] directions = new bool[8];
            int index = 0;
            while (index < directions.Length)
            {
                directions[index] = random.Next(0, 2) == 0;
                index++;
            }
            string secondText = CreateDirectionalSegmentsText(directions);
            Geometry result = SharedPathFinder.GetSharedPaths(
                Read("LINESTRING (0 0, 80 0)"), Read(secondText));
            Geometry[] groups = result.GeometryCollection!.Geometries;
            LineString[] forward = groups[0].MultiLineString!.LineStrings;
            LineString[] reverse = groups[1].MultiLineString!.LineStrings;

            Assert.Multiple(() =>
            {
                Assert.That(forward, Has.Length.EqualTo(
                    CountDirectionalRuns(directions, true)),
                    CreateDescription(seed, caseIndex, secondText));
                Assert.That(reverse, Has.Length.EqualTo(
                    CountDirectionalRuns(directions, false)),
                    CreateDescription(seed, caseIndex, secondText));
                Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                    Is.EqualTo(80.0));
            });
            caseIndex++;
        }
    }

    [Test]
    public void SeededUnionVariantsMatchNetTopologySuiteUnaryUnion()
    {
        const int seed = 83006;
        const int caseCount = 32;
        Random random = new Random(seed);
        NtsWktReader ntsReader = new NtsWktReader();

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            string[] rectangles = new string[6];
            int index = 0;
            while (index < rectangles.Length)
            {
                rectangles[index] = CreateRectangleText(random);
                index++;
            }
            string allText = CreateCollectionText(rectangles, 0, 6);
            string firstText = CreateCollectionText(rectangles, 0, 3);
            string secondText = CreateCollectionText(rectangles, 3, 3);
            Geometry all = Read(allText);
            NtsGeometry expected = NtsUnaryUnionOp.Union(
                ntsReader.Read(allText));

            AssertNtsEqual(expected,
                UnaryUnionEngine.GetUnaryUnion(all),
                CreateDescription(seed, caseIndex, "unary " + allText));
            AssertNtsEqual(expected,
                CascadedUnionEngine.GetCascadedUnion(all),
                CreateDescription(seed, caseIndex, "cascaded " + allText));
            AssertNtsEqual(expected,
                DisjointSubsetUnionEngine.GetDisjointSubsetUnion(
                    Read(firstText), Read(secondText)),
                CreateDescription(seed, caseIndex, "disjoint " + allText));
            caseIndex++;
        }
    }

    [Test]
    public void SeededPolygonOverlaysMatchNetTopologySuite()
    {
        const int seed = 83007;
        const int caseCount = 64;
        Random random = new Random(seed);
        NtsWktReader ntsReader = new NtsWktReader();

        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            string firstText = CreateRectangleText(random);
            string secondText = CreateRectangleText(random);
            Geometry first = Read(firstText);
            Geometry second = Read(secondText);
            NtsGeometry ntsFirst = ntsReader.Read(firstText);
            NtsGeometry ntsSecond = ntsReader.Read(secondText);
            string description = CreateDescription(
                seed, caseIndex, firstText + " versus " + secondText);

            AssertNtsEqual(ntsFirst.Intersection(ntsSecond),
                OverlayEngine.GetIntersection(first, second),
                description + " intersection");
            AssertNtsEqual(ntsFirst.Union(ntsSecond),
                OverlayEngine.GetUnion(first, second),
                description + " union");
            AssertNtsEqual(ntsFirst.Difference(ntsSecond),
                OverlayEngine.GetDifference(first, second),
                description + " difference");
            AssertNtsEqual(ntsFirst.SymmetricDifference(ntsSecond),
                OverlayEngine.GetSymmetricDifference(first, second),
                description + " symmetric difference");
            caseIndex++;
        }
    }

    private static int[] CreateDistinctPositions(
        Random random, int count, int minimum, int maximum)
    {
        HashSet<int> positions = new HashSet<int>();
        while (positions.Count < count)
        {
            positions.Add(random.Next(minimum, maximum));
        }
        int[] result = positions.ToArray();
        Array.Sort(result);
        return result;
    }

    private static string CreateRectangleText(Random random)
    {
        int minimumX = random.Next(-20, 16);
        int minimumY = random.Next(-20, 16);
        return CreateRectangleText(
            minimumX, minimumY,
            minimumX + random.Next(1, 10),
            minimumY + random.Next(1, 10));
    }

    private static string CreateRectangleText(
        int minimumX, int minimumY, int maximumX, int maximumY)
    {
        return FormattableString.Invariant(
            $"POLYGON (({minimumX} {minimumY}, {maximumX} {minimumY}, {maximumX} {maximumY}, {minimumX} {maximumY}, {minimumX} {minimumY}))");
    }

    private static string CreatePointText(int x, double y)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "POINT ({0} {1})", x, y);
    }

    private static string CreateMultiPointText(int[] positions)
    {
        StringBuilder text = new StringBuilder("MULTIPOINT (");
        int index = 0;
        while (index < positions.Length)
        {
            if (index != 0) text.Append(", ");
            text.Append('(');
            text.Append(positions[index]);
            text.Append(" 0)");
            index++;
        }
        text.Append(')');
        return text.ToString();
    }

    private static string CreateGridText(
        int[] horizontalPositions, int[] verticalPositions)
    {
        StringBuilder text = new StringBuilder("MULTILINESTRING (");
        int index = 0;
        while (index < horizontalPositions.Length)
        {
            if (index != 0) text.Append(", ");
            int y = horizontalPositions[index];
            text.Append(FormattableString.Invariant(
                $"(0 {y}, 100 {y})"));
            index++;
        }
        index = 0;
        while (index < verticalPositions.Length)
        {
            text.Append(", ");
            int x = verticalPositions[index];
            text.Append(FormattableString.Invariant(
                $"({x} 0, {x} 100)"));
            index++;
        }
        text.Append(')');
        return text.ToString();
    }

    private static string CreateDirectionalSegmentsText(bool[] directions)
    {
        StringBuilder text = new StringBuilder("MULTILINESTRING (");
        int index = 0;
        while (index < directions.Length)
        {
            if (index != 0) text.Append(", ");
            int start = index * 10;
            int end = start + 10;
            if (directions[index])
            {
                text.Append(FormattableString.Invariant(
                    $"({start} 0, {end} 0)"));
            }
            else
            {
                text.Append(FormattableString.Invariant(
                    $"({end} 0, {start} 0)"));
            }
            index++;
        }
        text.Append(')');
        return text.ToString();
    }

    private static int CountDirectionalRuns(
        bool[] directions, bool requestedDirection)
    {
        int count = 0;
        int index = 0;
        while (index < directions.Length)
        {
            if (directions[index] == requestedDirection &&
                (index == 0 || directions[index - 1] != requestedDirection))
            {
                count++;
            }
            index++;
        }
        return count;
    }

    private static string CreateCollectionText(
        string[] geometries, int start, int count)
    {
        StringBuilder text = new StringBuilder("GEOMETRYCOLLECTION (");
        int index = 0;
        while (index < count)
        {
            if (index != 0) text.Append(", ");
            text.Append(geometries[start + index]);
            index++;
        }
        text.Append(')');
        return text.ToString();
    }

    private static LineString[] GetLines(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return [geometry.LineString!];
        }
        Assert.That(geometry.GeometryType,
            Is.EqualTo(GeometryType.MultiLineString));
        return geometry.MultiLineString!.LineStrings;
    }

    private static void AssertNtsEqual(
        NtsGeometry expected, Geometry actual, string description)
    {
        NtsGeometry actualNts = ToNts(actual);
        if (expected.IsEmpty || actualNts.IsEmpty)
        {
            Assert.That(actualNts.IsEmpty,
                Is.EqualTo(expected.IsEmpty), description);
            return;
        }
        Assert.That(actualNts.EqualsTopologically(expected),
            Is.True, description + Environment.NewLine +
            "expected: " + expected + Environment.NewLine +
            "actual: " + actualNts);
    }

    private static string CreateDescription(
        int seed, int caseIndex, string input)
    {
        return FormattableString.Invariant(
            $"seed {seed}, case {caseIndex}: {input}");
    }

    private static NtsGeometry ToNts(Geometry geometry)
    {
        NtsWktReader reader = new NtsWktReader();
        return reader.Read(Write(geometry));
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
