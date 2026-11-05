using geo;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsGeometryFixer = NetTopologySuite.Geometries.Utilities.GeometryFixer;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class GeometryRepairExhaustiveTests
{
    [TestCase(
        "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 4 0, 2 2, 4 4, 0 4, 2 2, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(20 20, 20 22, 22 22, 22 20, 20 20))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 8, 8 8, 8 2, 2 2), " +
        "(3 3, 3 4, 4 4, 4 3, 3 3))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(2 2, 2 4, 4 4, 4 2, 2 2), " +
        "(4 4, 2 4, 2 2, 4 2, 4 4))")]
    [TestCase(
        "POLYGON ((0 0, 5 0, 10 0, 10 10, 5 10, 0 10, 0 0), " +
        "(5 0, 9 5, 5 10, 1 5, 5 0))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(5 -1, 7 3, 3 3, 5 -1))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 2, 6 2, 6 6, 2 6, 2 2)))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((4 0, 8 0, 8 4, 4 4, 4 0)))")]
    public void EveryCorePolygonInvalidityIsRepairedByBothStrategies(
        string text)
    {
        Geometry source = Read(text);
        Assert.That(GeometryValidityChecker.ValidateGeometry(
            source).IsValid, Is.False);

        RepairMethod[] methods =
            [RepairMethod.Linework, RepairMethod.Structure];
        int index = 0;
        while (index < methods.Length)
        {
            Geometry result = GeometryRepairEngine.MakeValid(
                source, Parameters(methods[index], false));
            ValidationResult validation =
                GeometryValidityChecker.ValidateGeometry(result);

            Assert.Multiple(() =>
            {
                Assert.That(validation.IsValid, Is.True,
                    $"{methods[index]}: {validation.Error} " +
                    $"{validation.Message} at {validation.ComponentPath}");
                Assert.That(result.CoordinateLayout,
                    Is.EqualTo(source.CoordinateLayout));
                Assert.That(result.SpatialReferenceId,
                    Is.EqualTo(source.SpatialReferenceId));
            });
            index++;
        }
    }

    [Test]
    public void CleanupRepairsNonFiniteRepeatedAndOpenSequences()
    {
        Geometry invalidLine = RawLine([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(0.0, 0.0),
            CoordinateValue(double.NaN, 5.0),
            CoordinateValue(3.0, 0.0)
        ]);
        Geometry openPolygon = RawPolygon([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 4.0)
        ]);

        Geometry line = GeometryRepairEngine.MakeValid(
            invalidLine, Parameters(RepairMethod.Linework, false));
        Geometry polygon = GeometryRepairEngine.MakeValid(
            openPolygon, Parameters(RepairMethod.Structure, false));

        Assert.Multiple(() =>
        {
            Assert.That(line.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(line.LineString!.Coordinates,
                Has.Length.EqualTo(2));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                line).IsValid, Is.True);
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                polygon).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(polygon),
                Is.EqualTo(16.0));
            Assert.That(openPolygon.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void CollapsePoliciesCoverLinesPolygonsAndCollections()
    {
        Geometry line = Read("LINESTRING (1 1, 1 1)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 1 0, 2 0, 0 0))");
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (LINESTRING (1 1, 1 1), " +
            "POLYGON ((0 0, 1 0, 2 0, 0 0)))");

        AssertCollapsePolicy(line, GeometryType.LineString,
            GeometryType.Point);
        AssertCollapsePolicy(polygon, GeometryType.Polygon,
            GeometryType.LineString);

        Geometry dropped = GeometryRepairEngine.MakeValid(
            collection, Parameters(RepairMethod.Linework, false));
        Geometry retained = GeometryRepairEngine.MakeValid(
            collection, Parameters(RepairMethod.Linework, true));
        Assert.Multiple(() =>
        {
            Assert.That(dropped.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(dropped), Is.True);
            Assert.That(retained.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(retained.GeometryCollection!.Geometries,
                Has.Length.EqualTo(2));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                retained).IsValid, Is.True);
        });
    }

    [TestCase("POINT EMPTY", GeometryType.Point)]
    [TestCase("LINESTRING EMPTY", GeometryType.LineString)]
    [TestCase("POLYGON EMPTY", GeometryType.Polygon)]
    [TestCase("MULTIPOINT EMPTY", GeometryType.Point)]
    [TestCase("MULTILINESTRING EMPTY", GeometryType.LineString)]
    [TestCase("MULTIPOLYGON EMPTY", GeometryType.Polygon)]
    [TestCase("GEOMETRYCOLLECTION EMPTY", GeometryType.GeometryCollection)]
    public void EmptyInputsReturnNarrowestNominalEmpty(
        string text, GeometryType expectedType)
    {
        Geometry source = Read(text);

        Geometry result = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(expectedType));
            Assert.That(GeometryInspection.IsEmptyValue(result), Is.True);
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                result).IsValid, Is.True);
        });
    }

    [TestCase("POINT ZM (1 2 3 4)")]
    [TestCase("LINESTRING ZM (0 0 1 2, 3 4 5 6)")]
    [TestCase(
        "POLYGON ZM ((0 0 1 2, 4 0 3 4, 4 4 5 6, " +
        "0 4 7 8, 0 0 1 2))")]
    public void ValidInputsRemainTopologicallyEqualAndOwning(string text)
    {
        Geometry source = ReadWithSrid("SRID=4979;" + text);

        Geometry linework = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, false));
        Geometry structure = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Structure, false));
        Coordinate[] sourceCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(source);
        Coordinate[] lineworkCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(linework);
        double originalX = lineworkCoordinates[0].X;
        sourceCoordinates[0].X += 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                linework, structure), Is.True);
            Assert.That(linework.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(linework.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(lineworkCoordinates[0].X, Is.EqualTo(originalX));
        });
    }

    [TestCase(
        "POLYGON ((0 0, 4 4, 0 4, 4 0, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 4 0, 2 2, 4 4, 0 4, 2 2, 0 0))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(20 20, 20 22, 22 22, 22 20, 20 20))")]
    [TestCase(
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
        "(5 -1, 7 3, 3 3, 5 -1))")]
    [TestCase(
        "MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0)), " +
        "((2 2, 6 2, 6 6, 2 6, 2 2)))")]
    public void StructureRepairMatchesNetTopologySuiteGeometryFixer(
        string text)
    {
        NtsGeometry source = new NtsWktReader().Read(text);
        NtsGeometry expected = NtsGeometryFixer.Fix(source);

        Geometry actual = GeometryRepairEngine.MakeValid(
            Read(text), Parameters(RepairMethod.Structure, false));
        NtsGeometry actualNts = ToNts(actual);

        Assert.Multiple(() =>
        {
            Assert.That(actualNts.IsValid, Is.True);
            Assert.That(actualNts.EqualsTopologically(expected), Is.True,
                $"expected {expected}; actual {actualNts}");
        });
    }

    [TestCase("LINESTRING (1 1, 1 1)")]
    [TestCase("POLYGON ((0 0, 1 0, 2 0, 0 0))")]
    public void CollapsePolicyMatchesNetTopologySuiteGeometryFixer(
        string text)
    {
        int policyIndex = 0;
        while (policyIndex < 2)
        {
            bool keepCollapsed = policyIndex == 1;
            NtsGeometryFixer fixer = new NtsGeometryFixer(
                new NtsWktReader().Read(text));
            fixer.KeepCollapsed = keepCollapsed;
            fixer.KeepMulti = false;
            NtsGeometry expected = fixer.GetResult();

            Geometry actual = GeometryRepairEngine.MakeValid(
                Read(text),
                Parameters(RepairMethod.Structure, keepCollapsed));
            NtsGeometry actualNts = ToNts(actual);

            bool equivalent = expected.IsEmpty ?
                actualNts.IsEmpty &&
                    actualNts.GeometryType == expected.GeometryType :
                actualNts.EqualsTopologically(expected);
            Assert.That(equivalent, Is.True,
                $"keepCollapsed={keepCollapsed}; " +
                $"expected {expected}; actual {actualNts}");
            policyIndex++;
        }
    }

    [Test]
    public void CoverageLargestAreaStrategyOwnsOverlap()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((1 0, 5 0, 5 2, 1 2, 1 0)))");
        RepairParameters firstParameters =
            Parameters(RepairMethod.Structure, false);
        RepairParameters largestParameters =
            Parameters(RepairMethod.Structure, false);
        largestParameters.CoverageMergeStrategy =
            CoverageMergeStrategy.LargestArea;

        Geometry first = GeometryRepairEngine.CleanCoverage(
            source, firstParameters);
        Geometry largest = GeometryRepairEngine.CleanCoverage(
            source, largestParameters);

        Assert.Multiple(() =>
        {
            Assert.That(LargestPolygonMinimumX(first), Is.EqualTo(2.0));
            Assert.That(LargestPolygonMinimumX(largest), Is.EqualTo(1.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(first),
                Is.EqualTo(10.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(largest),
                Is.EqualTo(10.0));
            Assert.That(CoverageValidator.ValidateCoverage(
                first, 0.0).IsValid, Is.True);
            Assert.That(CoverageValidator.ValidateCoverage(
                largest, 0.0).IsValid, Is.True);
        });
    }

    [Test]
    public void CoverageGapIsClosedOnlyWithinConfiguredWidth()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)))");
        RepairParameters unchangedParameters =
            Parameters(RepairMethod.Structure, false);
        RepairParameters closingParameters =
            Parameters(RepairMethod.Structure, false);
        closingParameters.MaximumGapWidth = 0.2;

        Geometry unchanged = GeometryRepairEngine.CleanCoverage(
            source, unchangedParameters);
        Geometry closed = GeometryRepairEngine.CleanCoverage(
            source, closingParameters);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(unchanged),
                Is.EqualTo(8.0).Within(1.0e-12));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(closed),
                Is.EqualTo(8.2).Within(1.0e-12));
            Assert.That(CoverageValidator.ValidateCoverage(
                unchanged, 0.2).Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(CoverageValidator.ValidateCoverage(
                closed, 0.2).IsValid, Is.True);
        });
    }

    [Test]
    public void UnrepairablePayloadAndUnsupportedTypeAreRejected()
    {
        Geometry mismatched = new Geometry
        {
            GeometryType = GeometryType.LineString
        };
        Geometry circular = Read(
            "CIRCULARSTRING (0 0, 1 1, 2 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryRepairEngine.MakeValid(
                    mismatched,
                    Parameters(RepairMethod.Linework, false)));
            Assert.Throws<InvalidGeometryException>(() =>
                GeometryRepairEngine.MakeValid(
                    circular,
                    Parameters(RepairMethod.Linework, false)));
        });
    }

    private static void AssertCollapsePolicy(
        Geometry source, GeometryType droppedType,
        GeometryType retainedType)
    {
        Geometry dropped = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, false));
        Geometry retained = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, true));
        Assert.Multiple(() =>
        {
            Assert.That(dropped.GeometryType, Is.EqualTo(droppedType));
            Assert.That(GeometryInspection.IsEmptyValue(dropped), Is.True);
            Assert.That(retained.GeometryType, Is.EqualTo(retainedType));
            Assert.That(GeometryInspection.IsEmptyValue(retained), Is.False);
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                retained).IsValid, Is.True);
        });
    }

    private static RepairParameters Parameters(
        RepairMethod method, bool keepCollapsed)
    {
        return new RepairParameters
        {
            Method = method,
            KeepCollapsedComponents = keepCollapsed
        };
    }

    private static double LargestPolygonMinimumX(Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        double largestArea = -1.0;
        double minimumX = double.NaN;
        int index = 0;
        while (index < leaves.Length)
        {
            if (leaves[index].GeometryType == GeometryType.Polygon)
            {
                double area = GeometryMeasureAlgorithms.GetProjectedArea(
                    leaves[index]);
                if (area > largestArea)
                {
                    largestArea = area;
                    minimumX = leaves[index].Polygon!.Shell.Coordinates.Min(
                        coordinate => coordinate.X);
                }
            }
            index++;
        }
        return minimumX;
    }

    private static NtsGeometry ToNts(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return new NtsWktReader().Read(codec.WriteWkt(geometry));
    }

    private static Geometry RawLine(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.LineString;
        geometry.LineString = new LineString { Coordinates = coordinates };
        return geometry;
    }

    private static Geometry RawPolygon(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.Polygon;
        geometry.Polygon = new Polygon
        {
            Shell = new LinearRing { Coordinates = coordinates }
        };
        return geometry;
    }

    private static Coordinate CoordinateValue(double x, double y)
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
