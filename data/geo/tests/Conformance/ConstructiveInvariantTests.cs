using System.Globalization;
using System.Text;
using geo;

namespace geo.Tests;

public class ConstructiveInvariantTests
{
    [Test]
    public void MinimumShapesAreDeterministicAcrossInputPermutations()
    {
        Geometry first = Read(
            "MULTIPOINT ((0 0), (4 2), (3 6), (-1 4), (-2 1), (1 3))");
        Geometry second = Read(
            "MULTIPOINT ((1 3), (-2 1), (3 6), (0 0), (-1 4), (4 2))");

        Geometry firstRectangle =
            MinimumShapes.GetMinimumRotatedRectangle(first);
        Geometry secondRectangle =
            MinimumShapes.GetMinimumRotatedRectangle(second);
        CircleResult firstCircle =
            MinimumShapes.GetMinimumBoundingCircle(first);
        CircleResult secondCircle =
            MinimumShapes.GetMinimumBoundingCircle(second);
        DistanceResult firstWidth = MinimumShapes.GetMinimumWidth(first);
        DistanceResult secondWidth = MinimumShapes.GetMinimumWidth(second);
        DistanceResult firstDiameter =
            MinimumShapes.GetMaximumDiameter(first);
        DistanceResult secondDiameter =
            MinimumShapes.GetMaximumDiameter(second);

        Assert.Multiple(() =>
        {
            Assert.That(Write(firstRectangle), Is.EqualTo(Write(secondRectangle)));
            Assert.That(Write(firstCircle.Geometry!),
                Is.EqualTo(Write(secondCircle.Geometry!)));
            Assert.That(firstCircle.Radius, Is.EqualTo(secondCircle.Radius));
            Assert.That(firstWidth.Distance, Is.EqualTo(secondWidth.Distance));
            Assert.That(firstDiameter.Distance,
                Is.EqualTo(secondDiameter.Distance));
            Assert.That(AllCoordinatesWithinCircle(first, firstCircle), Is.True);
            Assert.That(AllCoordinatesCovered(first, firstRectangle), Is.True);
        });
    }

    [Test]
    public void InscribedAndEmptyCirclesAreContainedAndDeterministic()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 12 0, 12 10, 0 10, 0 0), " +
            "(5 3, 7 3, 7 7, 5 7, 5 3))");
        Geometry obstacles = Read(
            "GEOMETRYCOLLECTION (POINT (2 5), " +
            "LINESTRING (9 2, 9 8))");

        CircleResult inscribedFirst =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                polygon, 0.01);
        CircleResult inscribedSecond =
            InteriorCircleAlgorithms.GetMaximumInscribedCircle(
                polygon, 0.01);
        CircleResult emptyFirst =
            InteriorCircleAlgorithms.GetLargestEmptyCircle(
                obstacles, polygon, 0.01);
        CircleResult emptySecond =
            InteriorCircleAlgorithms.GetLargestEmptyCircle(
                obstacles, polygon, 0.01);

        Assert.Multiple(() =>
        {
            Assert.That(Write(inscribedFirst.Geometry!),
                Is.EqualTo(Write(inscribedSecond.Geometry!)));
            Assert.That(Write(emptyFirst.Geometry!),
                Is.EqualTo(Write(emptySecond.Geometry!)));
            Assert.That(GeometryPointLocation.LocatePoint(
                inscribedFirst.Center!, polygon, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(GeometryPointLocation.LocatePoint(
                emptyFirst.Center!, polygon, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(GeometryPointLocation.LocatePoint(
                emptyFirst.Center!, obstacles, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(WitnessDistanceMatches(inscribedFirst), Is.True);
            Assert.That(WitnessDistanceMatches(emptyFirst), Is.True);
        });
    }

    [Test]
    public void PolygonTangentsAreOwningDeterministicSupportLines()
    {
        Geometry concave = Read(
            "POLYGON Z ((0 0 1, 6 0 2, 6 6 3, 3 3 4, " +
            "0 6 5, 0 0 1))");
        Coordinate origin = new Coordinate
        {
            X = -3.0,
            Y = 0.0,
            Z = 9.0
        };

        Geometry first = InteriorCircleAlgorithms.GetPolygonTangents(
            concave, origin);
        Geometry second = InteriorCircleAlgorithms.GetPolygonTangents(
            concave, origin);
        origin.X = -30.0;
        concave.Polygon!.Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(Write(first), Is.EqualTo(Write(second)));
            Assert.That(first.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
            Assert.That(AllCoordinatesOnOneSide(
                second, Read(
                    "POLYGON Z ((0 0 1, 6 0 2, 6 6 3, 3 3 4, " +
                    "0 6 5, 0 0 1))")), Is.True);
            Assert.That(first.MultiLineString.LineStrings[0]
                .Coordinates[0].X, Is.EqualTo(-3.0));
            Assert.That(first.MultiLineString.LineStrings[0]
                .Coordinates[0].Z, Is.EqualTo(9.0));
        });
    }

    [Test]
    public void SeededDelaunayMeshesCoverTheirHullAndHaveEmptyCircumcircles()
    {
        const int seed = 95001;
        const int caseCount = 24;
        Random random = new Random(seed);
        int caseIndex = 0;
        while (caseIndex < caseCount)
        {
            Geometry sites = Read(CreateRandomMultiPoint(random, 9));
            Geometry first = DelaunayBuilder.GetDelaunayTriangulation(sites);
            Geometry second = DelaunayBuilder.GetDelaunayTriangulation(sites);
            Geometry hull = ConvexHullAlgorithm.GetConvexHull(sites);
            string description = Description(seed, caseIndex, Write(sites));

            Assert.Multiple(() =>
            {
                Assert.That(Write(first), Is.EqualTo(Write(second)),
                    description);
                Assert.That(GetTinArea(first),
                    Is.EqualTo(GeometryMeasureAlgorithms.GetProjectedArea(hull))
                        .Within(0.000000001), description);
                Assert.That(AllTrianglesCounterClockwise(first),
                    Is.True, description);
                Assert.That(AllCircumcirclesEmpty(first, sites),
                    Is.True, description);
            });
            caseIndex++;
        }
    }

    [Test]
    public void PolygonTriangulationPreservesTwoHolesAndEveryBoundaryEdge()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 14 0, 14 10, 0 10, 0 0), " +
            "(2 2, 2 4, 4 4, 4 2, 2 2), " +
            "(9 5, 9 8, 12 8, 12 5, 9 5))");

        Geometry ordinary =
            PolygonTriangulator.GetPolygonTriangulation(polygon);
        Geometry constrained =
            ConstrainedDelaunayBuilder.GetConstrainedDelaunayTriangulation(
                polygon);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Tin!.Triangles, Has.Length.EqualTo(14));
            Assert.That(constrained.Tin!.Triangles, Has.Length.EqualTo(14));
            Assert.That(GetTinArea(ordinary), Is.EqualTo(127.0));
            Assert.That(GetTinArea(constrained), Is.EqualTo(127.0));
            Assert.That(AllTriangleCentroidsInside(ordinary, polygon), Is.True);
            Assert.That(AllTriangleCentroidsInside(constrained, polygon),
                Is.True);
            Assert.That(AllPolygonBoundaryEdgesPresent(ordinary, polygon),
                Is.True);
            Assert.That(AllPolygonBoundaryEdgesPresent(constrained, polygon),
                Is.True);
        });
    }

    [Test]
    public void VoronoiHandlesEmptySingleDuplicateAndMultipleSites()
    {
        Envelope clip = new Envelope
        {
            MinX = 0.0,
            MinY = 0.0,
            MaxX = 12.0,
            MaxY = 6.0
        };
        Geometry empty = VoronoiBuilder.GetVoronoiDiagram(
            Read("GEOMETRYCOLLECTION EMPTY"), clip);
        Geometry single = VoronoiBuilder.GetVoronoiDiagram(
            Read("POINT (5 3)"), clip);
        Geometry duplicates = VoronoiBuilder.GetVoronoiDiagram(
            Read("MULTIPOINT ((1 3), (5 3), (11 3), (5 3), (1 3))"),
            clip);
        Geometry repeated = VoronoiBuilder.GetVoronoiDiagram(
            Read("MULTIPOINT ((11 3), (1 3), (5 3))"), clip);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryCollection!.Geometries, Is.Empty);
            Assert.That(single.GeometryCollection!.Geometries,
                Has.Length.EqualTo(1));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(single),
                Is.EqualTo(72.0));
            Assert.That(duplicates.GeometryCollection!.Geometries,
                Has.Length.EqualTo(3));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(duplicates),
                Is.EqualTo(72.0).Within(1e-12));
            Assert.That(Write(duplicates), Is.EqualTo(Write(repeated)));
            Assert.That(EveryVoronoiCellContainsItsSite(
                duplicates,
                [Point(1.0, 3.0), Point(5.0, 3.0), Point(11.0, 3.0)]),
                Is.True);
        });
    }

    [Test]
    public void MinimumSpanningTreeIsAcyclicMinimalAndPermutationStable()
    {
        Geometry firstSites = Read(
            "MULTIPOINT ((0 0), (2 0), (4 0), (1 3), (3 3), (2 5))");
        Geometry secondSites = Read(
            "MULTIPOINT ((2 5), (3 3), (1 3), (4 0), (2 0), (0 0))");

        Geometry first = MinimumSpanningTreeBuilder.GetMinimumSpanningTree(
            firstSites);
        Geometry second = MinimumSpanningTreeBuilder.GetMinimumSpanningTree(
            secondSites);
        LineString[] edges = first.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(edges, Has.Length.EqualTo(5));
            Assert.That(Write(first), Is.EqualTo(Write(second)));
            Assert.That(AllSitesConnected(firstSites, first), Is.True);
            Assert.That(HasNoCycle(firstSites, first), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetLength(first),
                Is.EqualTo(6.0 + Math.Sqrt(5.0) + Math.Sqrt(10.0))
                    .Within(1e-12));
        });
    }

    [Test]
    public void ConstructiveDegeneraciesPreserveMetadataAndNarrowResults()
    {
        Geometry point = ReadWithSrid("SRID=3857;POINT ZM (2 3 4 5)");
        Geometry collinear = ReadWithSrid(
            "SRID=3857;MULTIPOINT ZM ((0 0 1 11), " +
            "(2 0 2 12), (4 0 3 13), (2 0 9 19))");
        Geometry hull = ConvexHullAlgorithm.GetConvexHull(collinear);
        Geometry delaunay = DelaunayBuilder.GetDelaunayTriangulation(collinear);
        Geometry tree = MinimumSpanningTreeBuilder.GetMinimumSpanningTree(point);
        CircleResult circle = MinimumShapes.GetMinimumBoundingCircle(point);

        Assert.Multiple(() =>
        {
            Assert.That(hull.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(hull.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(hull.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(delaunay.Tin!.Triangles, Is.Empty);
            Assert.That(delaunay.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(GeometryInspection.IsEmptyValue(tree), Is.True);
            Assert.That(tree.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(circle.Radius, Is.Zero);
            Assert.That(circle.Center!.Z, Is.EqualTo(4.0));
            Assert.That(circle.Center.M, Is.EqualTo(5.0));
        });
    }

    private static bool AllCoordinatesWithinCircle(
        Geometry geometry, CircleResult circle)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.All(coordinate =>
            SegmentDistanceAlgorithms.GetPlanarDistance(
                circle.Center!, coordinate) <= circle.Radius + 1e-12);
    }

    private static bool AllCoordinatesCovered(
        Geometry geometry, Geometry polygon)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return coordinates.All(coordinate =>
            GeometryPointLocation.LocatePoint(
                coordinate, polygon, CurveParameters()) !=
                    TopologicalLocation.Exterior);
    }

    private static bool WitnessDistanceMatches(CircleResult circle)
    {
        if (circle.Center == null || circle.Witnesses.Length != 1) return false;
        return Math.Abs(SegmentDistanceAlgorithms.GetPlanarDistance(
            circle.Center, circle.Witnesses[0]) - circle.Radius) < 1e-12;
    }

    private static bool AllCoordinatesOnOneSide(
        Geometry tangents, Geometry polygon)
    {
        Coordinate[] polygonCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(polygon);
        foreach (LineString line in tangents.MultiLineString!.LineStrings)
        {
            bool positive = false;
            bool negative = false;
            foreach (Coordinate coordinate in polygonCoordinates)
            {
                int orientation = RobustOrientation.GetOrientationIndex(
                    line.Coordinates[0], line.Coordinates[1], coordinate);
                if (orientation > 0) positive = true;
                if (orientation < 0) negative = true;
            }
            if (positive && negative) return false;
        }
        return true;
    }

    private static bool AllTrianglesCounterClockwise(Geometry tin)
    {
        return tin.Tin!.Triangles.All(triangle =>
            RingMath.GetExactSignedAreaSign(triangle.Shell.Coordinates) > 0);
    }

    private static bool AllCircumcirclesEmpty(
        Geometry tin, Geometry sitesGeometry)
    {
        Coordinate[] sites = ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(
            GeometryInspection.ExtractValidatedCoordinates(sitesGeometry));
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            Coordinate[] ring = triangle.Shell.Coordinates;
            foreach (Coordinate site in sites)
            {
                bool vertex = ring.Take(3).Any(coordinate =>
                    coordinate.X == site.X && coordinate.Y == site.Y);
                if (!vertex && DelaunayBuilder.GetCircumcircleSign(
                    ring[0], ring[1], ring[2], site) > 0) return false;
            }
        }
        return true;
    }

    private static double GetTinArea(Geometry tin)
    {
        return tin.Tin!.Triangles.Sum(triangle =>
            Math.Abs(RingMath.GetSignedArea(triangle.Shell.Coordinates)));
    }

    private static bool AllTriangleCentroidsInside(
        Geometry tin, Geometry polygon)
    {
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            Coordinate[] ring = triangle.Shell.Coordinates;
            Coordinate midpoint =
                RobustLineIntersection.InterpolateLineCoordinate(
                    ring[0], ring[1], 0.5);
            Coordinate centroid =
                RobustLineIntersection.InterpolateLineCoordinate(
                    midpoint, ring[2], 1.0 / 3.0);
            if (GeometryPointLocation.LocatePointInPolygon(
                centroid, polygon.Polygon!) != TopologicalLocation.Interior)
            {
                return false;
            }
        }
        return true;
    }

    private static bool AllPolygonBoundaryEdgesPresent(
        Geometry tin, Geometry polygon)
    {
        List<Coordinate[]> triangleEdges = new List<Coordinate[]>();
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            Coordinate[] ring = triangle.Shell.Coordinates;
            for (int index = 0; index < 3; index++)
            {
                triangleEdges.Add(
                    [ring[index], ring[(index + 1) % 3]]);
            }
        }
        List<LinearRing> rings = new List<LinearRing> { polygon.Polygon!.Shell };
        rings.AddRange(polygon.Polygon.Holes);
        foreach (LinearRing ring in rings)
        {
            for (int index = 0; index + 1 < ring.Coordinates.Length; index++)
            {
                if (!triangleEdges.Any(edge => SameEdge(
                    edge[0], edge[1], ring.Coordinates[index],
                    ring.Coordinates[index + 1]))) return false;
            }
        }
        return true;
    }

    private static bool SameEdge(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        return SamePoint(firstStart, secondStart) &&
                SamePoint(firstEnd, secondEnd) ||
            SamePoint(firstStart, secondEnd) &&
                SamePoint(firstEnd, secondStart);
    }

    private static bool SamePoint(Coordinate first, Coordinate second)
    {
        return first.X == second.X && first.Y == second.Y;
    }

    private static bool EveryVoronoiCellContainsItsSite(
        Geometry diagram, Coordinate[] sites)
    {
        Geometry[] cells = diagram.GeometryCollection!.Geometries;
        if (cells.Length != sites.Length) return false;
        for (int index = 0; index < cells.Length; index++)
        {
            if (GeometryPointLocation.LocatePoint(
                sites[index], cells[index], CurveParameters()) ==
                    TopologicalLocation.Exterior) return false;
        }
        return true;
    }

    private static bool AllSitesConnected(Geometry sites, Geometry tree)
    {
        Coordinate[] unique = ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(
            GeometryInspection.ExtractValidatedCoordinates(sites));
        bool[] reached = new bool[unique.Length];
        reached[0] = true;
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (LineString edge in tree.MultiLineString!.LineStrings)
            {
                int first = FindPoint(unique, edge.Coordinates[0]);
                int second = FindPoint(unique, edge.Coordinates[1]);
                if (reached[first] && !reached[second])
                {
                    reached[second] = true;
                    changed = true;
                }
                if (reached[second] && !reached[first])
                {
                    reached[first] = true;
                    changed = true;
                }
            }
        }
        return reached.All(value => value);
    }

    private static bool HasNoCycle(Geometry sites, Geometry tree)
    {
        Coordinate[] unique = ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(
            GeometryInspection.ExtractValidatedCoordinates(sites));
        int[] parents = Enumerable.Range(0, unique.Length).ToArray();
        foreach (LineString edge in tree.MultiLineString!.LineStrings)
        {
            int first = FindRoot(parents,
                FindPoint(unique, edge.Coordinates[0]));
            int second = FindRoot(parents,
                FindPoint(unique, edge.Coordinates[1]));
            if (first == second) return false;
            parents[second] = first;
        }
        return true;
    }

    private static int FindRoot(int[] parents, int value)
    {
        while (parents[value] != value) value = parents[value];
        return value;
    }

    private static int FindPoint(
        Coordinate[] coordinates, Coordinate point)
    {
        for (int index = 0; index < coordinates.Length; index++)
        {
            if (SamePoint(coordinates[index], point)) return index;
        }
        throw new AssertionException("Tree coordinate is not a source site.");
    }

    private static string CreateRandomMultiPoint(Random random, int count)
    {
        StringBuilder text = new StringBuilder("MULTIPOINT (");
        for (int index = 0; index < count; index++)
        {
            if (index != 0) text.Append(", ");
            double x = random.Next(-100, 101) + index * 0.001;
            double y = random.Next(-100, 101) + index * 0.002;
            text.Append('(');
            text.Append(x.ToString("R", CultureInfo.InvariantCulture));
            text.Append(' ');
            text.Append(y.ToString("R", CultureInfo.InvariantCulture));
            text.Append(')');
        }
        text.Append(')');
        return text.ToString();
    }

    private static string Description(
        int seed, int caseIndex, string input)
    {
        return FormattableString.Invariant(
            $"seed {seed}, case {caseIndex}: {input}");
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
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

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
    }
}
