using System.Globalization;
using System.Text;
using geo;

namespace geo.Tests;

internal static class RandomizedRegressionCapture
{
    internal static void Check(
        int seed, int caseIndex, Geometry geometry,
        Action<Geometry> assertion)
    {
        try
        {
            assertion(geometry);
        }
        catch (Exception)
        {
            Geometry minimized = Minimize(geometry, assertion);
            string path = WriteFixture(seed, caseIndex, minimized);
            TestContext.Progress.WriteLine(
                "Minimized randomized regression fixture: " + path);
            TestContext.AddTestAttachment(
                path, "Minimized randomized regression fixture");
            throw;
        }
    }

    private static Geometry Minimize(
        Geometry original, Action<Geometry> assertion)
    {
        Geometry current = original;
        bool changed = true;
        while (changed)
        {
            changed = false;
            List<Geometry> candidates = Candidates(current);
            int currentSize = Size(current);
            int index = 0;
            while (index < candidates.Count)
            {
                Geometry candidate = candidates[index];
                if (Size(candidate) < currentSize && Fails(candidate, assertion))
                {
                    current = candidate;
                    changed = true;
                    break;
                }
                index++;
            }
        }
        return current;
    }

    private static bool Fails(
        Geometry geometry, Action<Geometry> assertion)
    {
        try
        {
            assertion(geometry);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static List<Geometry> Candidates(Geometry geometry)
    {
        List<Geometry> candidates = [];
        if (geometry.GeometryType == GeometryType.LineString &&
            geometry.LineString != null)
        {
            AddLineCandidates(geometry, geometry.LineString.Coordinates,
                candidates);
        }
        else if (geometry.GeometryType == GeometryType.Polygon &&
            geometry.Polygon != null)
        {
            AddPolygonCandidates(geometry, geometry.Polygon, candidates);
        }
        else if (geometry.GeometryType == GeometryType.MultiPoint &&
            geometry.MultiPoint != null)
        {
            AddPointCollectionCandidates(geometry, candidates);
        }
        else if (geometry.GeometryType == GeometryType.MultiLineString &&
            geometry.MultiLineString != null)
        {
            AddLineCollectionCandidates(geometry, candidates);
        }
        else if (geometry.GeometryType == GeometryType.MultiPolygon &&
            geometry.MultiPolygon != null)
        {
            AddPolygonCollectionCandidates(geometry, candidates);
        }
        else if (geometry.GeometryType == GeometryType.GeometryCollection &&
            geometry.GeometryCollection != null)
        {
            AddGeometryCollectionCandidates(geometry, candidates);
        }
        return candidates;
    }

    private static void AddLineCandidates(
        Geometry source, Coordinate[] coordinates, List<Geometry> candidates)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.LineString,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                LineString = new LineString
                {
                    Coordinates = RemoveAt(coordinates, index)
                }
            });
            index++;
        }
    }

    private static void AddPolygonCandidates(
        Geometry source, Polygon polygon, List<Geometry> candidates)
    {
        if (polygon.Holes.Length > 0)
        {
            candidates.Add(PolygonGeometry(source,
                new Polygon { Shell = polygon.Shell, Holes = [] }));
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                candidates.Add(PolygonGeometry(source, new Polygon
                {
                    Shell = polygon.Shell,
                    Holes = RemoveAt(polygon.Holes, holeIndex)
                }));
                holeIndex++;
            }
        }
        Coordinate[] shell = polygon.Shell.Coordinates;
        int index = 0;
        while (index < shell.Length)
        {
            candidates.Add(PolygonGeometry(source, new Polygon
            {
                Shell = new LinearRing
                {
                    Coordinates = RemoveRingCoordinate(shell, index)
                },
                Holes = polygon.Holes
            }));
            index++;
        }
    }

    private static void AddPointCollectionCandidates(
        Geometry source, List<Geometry> candidates)
    {
        Point[] points = source.MultiPoint!.Points;
        int index = 0;
        while (index < points.Length)
        {
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.MultiPoint,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                MultiPoint = new MultiPoint { Points = [points[index]] }
            });
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.Point,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = points[index]
            });
            index++;
        }
    }

    private static void AddLineCollectionCandidates(
        Geometry source, List<Geometry> candidates)
    {
        LineString[] lines = source.MultiLineString!.LineStrings;
        int index = 0;
        while (index < lines.Length)
        {
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.MultiLineString,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                MultiLineString = new MultiLineString
                {
                    LineStrings = [lines[index]]
                }
            });
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.LineString,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                LineString = lines[index]
            });
            index++;
        }
    }

    private static void AddPolygonCollectionCandidates(
        Geometry source, List<Geometry> candidates)
    {
        Polygon[] polygons = source.MultiPolygon!.Polygons;
        int index = 0;
        while (index < polygons.Length)
        {
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.MultiPolygon,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                MultiPolygon = new MultiPolygon
                {
                    Polygons = [polygons[index]]
                }
            });
            candidates.Add(PolygonGeometry(source, polygons[index]));
            index++;
        }
    }

    private static void AddGeometryCollectionCandidates(
        Geometry source, List<Geometry> candidates)
    {
        Geometry[] geometries = source.GeometryCollection!.Geometries;
        int index = 0;
        while (index < geometries.Length)
        {
            candidates.Add(new Geometry
            {
                GeometryType = GeometryType.GeometryCollection,
                CoordinateLayout = source.CoordinateLayout,
                SpatialReferenceId = source.SpatialReferenceId,
                Point = null,
                GeometryCollection = new GeometryCollection
                {
                    Geometries = [geometries[index]]
                }
            });
            candidates.Add(geometries[index]);
            index++;
        }
    }

    private static Geometry PolygonGeometry(Geometry source, Polygon polygon)
    {
        return new Geometry
        {
            GeometryType = GeometryType.Polygon,
            CoordinateLayout = source.CoordinateLayout,
            SpatialReferenceId = source.SpatialReferenceId,
            Point = null,
            Polygon = polygon
        };
    }

    private static Coordinate[] RemoveAt(
        Coordinate[] values, int removedIndex)
    {
        Coordinate[] result = new Coordinate[values.Length - 1];
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < values.Length)
        {
            if (sourceIndex != removedIndex)
            {
                result[resultIndex] = values[sourceIndex];
                resultIndex++;
            }
            sourceIndex++;
        }
        return result;
    }

    private static LinearRing[] RemoveAt(
        LinearRing[] values, int removedIndex)
    {
        LinearRing[] result = new LinearRing[values.Length - 1];
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < values.Length)
        {
            if (sourceIndex != removedIndex)
            {
                result[resultIndex] = values[sourceIndex];
                resultIndex++;
            }
            sourceIndex++;
        }
        return result;
    }

    private static Coordinate[] RemoveRingCoordinate(
        Coordinate[] values, int removedIndex)
    {
        if (values.Length > 1 && SamePosition(
            values[0], values[values.Length - 1]))
        {
            int uniqueCount = values.Length - 1;
            if (uniqueCount <= 1) return [];
            int uniqueIndex = removedIndex;
            if (removedIndex == values.Length - 1) uniqueIndex = 0;
            Coordinate[] result = new Coordinate[uniqueCount];
            int sourceIndex = 0;
            int resultIndex = 0;
            while (sourceIndex < uniqueCount)
            {
                if (sourceIndex != uniqueIndex)
                {
                    result[resultIndex] = values[sourceIndex];
                    resultIndex++;
                }
                sourceIndex++;
            }
            result[result.Length - 1] = result[0];
            return result;
        }
        return RemoveAt(values, removedIndex);
    }

    private static bool SamePosition(Coordinate first, Coordinate second)
    {
        return first.X == second.X && first.Y == second.Y;
    }

    private static int Size(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.Point) return 2;
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return 1 + (geometry.LineString == null ? 0 :
                geometry.LineString.Coordinates.Length);
        }
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            if (geometry.Polygon == null) return 1;
            int size = 1 + geometry.Polygon.Shell.Coordinates.Length;
            int index = 0;
            while (index < geometry.Polygon.Holes.Length)
            {
                size += 1 + geometry.Polygon.Holes[index].Coordinates.Length;
                index++;
            }
            return size;
        }
        if (geometry.GeometryType == GeometryType.MultiPoint)
        {
            return 1 + (geometry.MultiPoint == null ? 0 :
                geometry.MultiPoint.Points.Length * 2);
        }
        if (geometry.GeometryType == GeometryType.MultiLineString)
        {
            if (geometry.MultiLineString == null) return 1;
            int size = 1;
            int index = 0;
            while (index < geometry.MultiLineString.LineStrings.Length)
            {
                size += 1 + geometry.MultiLineString.LineStrings[index]
                    .Coordinates.Length;
                index++;
            }
            return size;
        }
        if (geometry.GeometryType == GeometryType.MultiPolygon)
        {
            if (geometry.MultiPolygon == null) return 1;
            int size = 1;
            int index = 0;
            while (index < geometry.MultiPolygon.Polygons.Length)
            {
                size += PolygonSize(geometry.MultiPolygon.Polygons[index]);
                index++;
            }
            return size;
        }
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            if (geometry.GeometryCollection == null) return 1;
            int size = 1;
            int index = 0;
            while (index < geometry.GeometryCollection.Geometries.Length)
            {
                size += Size(geometry.GeometryCollection.Geometries[index]);
                index++;
            }
            return size;
        }
        return 1;
    }

    private static int PolygonSize(Polygon polygon)
    {
        int size = 1 + polygon.Shell.Coordinates.Length;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            size += 1 + polygon.Holes[index].Coordinates.Length;
            index++;
        }
        return size;
    }

    private static string WriteFixture(
        int seed, int caseIndex, Geometry geometry)
    {
        string directory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory, "RegressionFixtures");
        Directory.CreateDirectory(directory);
        string testName = SafeName(TestContext.CurrentContext.Test.Name);
        string path = Path.Combine(directory,
            testName + "-seed-" + seed.ToString(CultureInfo.InvariantCulture) +
            "-case-" + caseIndex.ToString(CultureInfo.InvariantCulture) +
            ".txt");
        StringBuilder fixture = new StringBuilder();
        fixture.AppendLine("# Re-run with this fixed seed and case index.");
        fixture.Append("seed=").AppendLine(
            seed.ToString(CultureInfo.InvariantCulture));
        fixture.Append("caseIndex=").AppendLine(
            caseIndex.ToString(CultureInfo.InvariantCulture));
        fixture.Append("layout=").AppendLine(geometry.CoordinateLayout.ToString());
        fixture.Append("srid=").AppendLine(
            geometry.SpatialReferenceId.ToString(CultureInfo.InvariantCulture));
        fixture.Append("geometry=").AppendLine(Describe(geometry));
        File.WriteAllText(path, fixture.ToString());
        return path;
    }

    private static string SafeName(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsLetterOrDigit(character) || character == '-' ||
                character == '_') result.Append(character);
            else result.Append('_');
            index++;
        }
        return result.ToString();
    }

    private static string Describe(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.Point)
        {
            if (geometry.Point == null || geometry.Point.Coordinate == null)
                return "POINT EMPTY";
            return "POINT (" + DescribeCoordinate(geometry.Point.Coordinate,
                geometry.CoordinateLayout) + ")";
        }
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return "LINESTRING " + DescribeCoordinates(
                geometry.LineString == null ? [] :
                    geometry.LineString.Coordinates,
                geometry.CoordinateLayout);
        }
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            return DescribePolygon(geometry.Polygon, geometry.CoordinateLayout);
        }
        if (geometry.GeometryType == GeometryType.MultiPoint)
        {
            return "MULTIPOINT " + DescribePoints(
                geometry.MultiPoint == null ? [] : geometry.MultiPoint.Points,
                geometry.CoordinateLayout);
        }
        if (geometry.GeometryType == GeometryType.MultiLineString)
        {
            return "MULTILINESTRING " + DescribeLines(
                geometry.MultiLineString == null ? [] :
                    geometry.MultiLineString.LineStrings,
                geometry.CoordinateLayout);
        }
        if (geometry.GeometryType == GeometryType.MultiPolygon)
        {
            return "MULTIPOLYGON " + DescribePolygons(
                geometry.MultiPolygon == null ? [] :
                    geometry.MultiPolygon.Polygons,
                geometry.CoordinateLayout);
        }
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            return "GEOMETRYCOLLECTION " + DescribeGeometries(
                geometry.GeometryCollection == null ? [] :
                    geometry.GeometryCollection.Geometries);
        }
        return geometry.GeometryType.ToString().ToUpperInvariant();
    }

    private static string DescribePolygon(
        Polygon? polygon, CoordinateLayout layout)
    {
        if (polygon == null) return "POLYGON NULL";
        StringBuilder result = new StringBuilder("POLYGON (");
        result.Append(DescribeCoordinates(polygon.Shell.Coordinates, layout));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            result.Append(", ").Append(DescribeCoordinates(
                polygon.Holes[index].Coordinates, layout));
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string DescribeCoordinates(
        Coordinate[] coordinates, CoordinateLayout layout)
    {
        StringBuilder result = new StringBuilder("(");
        int index = 0;
        while (index < coordinates.Length)
        {
            if (index > 0) result.Append(", ");
            result.Append(DescribeCoordinate(coordinates[index], layout));
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string DescribeCoordinate(
        Coordinate coordinate, CoordinateLayout layout)
    {
        StringBuilder result = new StringBuilder();
        result.Append(Number(coordinate.X)).Append(' ')
            .Append(Number(coordinate.Y));
        if (layout == CoordinateLayout.XYZ ||
            layout == CoordinateLayout.XYZM)
        {
            result.Append(' ').Append(Number(coordinate.Z));
        }
        if (layout == CoordinateLayout.XYM ||
            layout == CoordinateLayout.XYZM)
        {
            result.Append(' ').Append(Number(coordinate.M));
        }
        return result.ToString();
    }

    private static string DescribePoints(
        Point[] points, CoordinateLayout layout)
    {
        StringBuilder result = new StringBuilder("(");
        int index = 0;
        while (index < points.Length)
        {
            if (index > 0) result.Append(", ");
            if (points[index] == null || points[index].Coordinate == null)
                result.Append("EMPTY");
            else result.Append('(').Append(DescribeCoordinate(
                points[index].Coordinate!, layout)).Append(')');
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string DescribeLines(
        LineString[] lines, CoordinateLayout layout)
    {
        StringBuilder result = new StringBuilder("(");
        int index = 0;
        while (index < lines.Length)
        {
            if (index > 0) result.Append(", ");
            result.Append(DescribeCoordinates(lines[index].Coordinates, layout));
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string DescribePolygons(
        Polygon[] polygons, CoordinateLayout layout)
    {
        StringBuilder result = new StringBuilder("(");
        int index = 0;
        while (index < polygons.Length)
        {
            if (index > 0) result.Append(", ");
            string polygon = DescribePolygon(polygons[index], layout);
            result.Append(polygon.Substring("POLYGON ".Length));
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string DescribeGeometries(Geometry[] geometries)
    {
        StringBuilder result = new StringBuilder("(");
        int index = 0;
        while (index < geometries.Length)
        {
            if (index > 0) result.Append(", ");
            result.Append(Describe(geometries[index]));
            index++;
        }
        return result.Append(')').ToString();
    }

    private static string Number(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "Infinity";
        if (double.IsNegativeInfinity(value)) return "-Infinity";
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
