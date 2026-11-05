using geo;

namespace geo.Tests;

public class StructuralValidatorTests
{
    [TestCase("POINT EMPTY")]
    [TestCase("LINESTRING (0 0, 1 1)")]
    [TestCase("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), " +
        "(1 1, 1 2, 2 2, 2 1, 1 1))")]
    [TestCase("TRIANGLE ((0 0, 2 0, 0 2, 0 0))")]
    [TestCase("TIN (((0 0, 2 0, 0 2, 0 0)))")]
    [TestCase("POLYHEDRALSURFACE (((0 0, 2 0, 2 2, 0 0)))")]
    [TestCase("CIRCULARSTRING (0 0, 1 1, 2 0)")]
    [TestCase("COMPOUNDCURVE ((0 0, 1 0), " +
        "CIRCULARSTRING (1 0, 2 1, 3 0))")]
    [TestCase("CURVEPOLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))")]
    [TestCase("MULTICURVE ((0 0, 1 1), " +
        "CIRCULARSTRING (2 0, 3 1, 4 0))")]
    [TestCase("MULTISURFACE (((0 0, 2 0, 2 2, 0 0)), " +
        "TRIANGLE ((3 0, 5 0, 3 2, 3 0)))")]
    public void ValidCoreAndExtendedStructuresPass(string text)
    {
        ValidationResult result = StructuralValidator.ValidateGeometry(
            Read(text));

        AssertValid(result);
    }

    [Test]
    public void NullUnknownAndMismatchedPayloadsAreClassified()
    {
        Geometry wrongPayload = new Geometry
        {
            GeometryType = GeometryType.LineString
        };
        Geometry extraPayload = new Geometry
        {
            GeometryType = GeometryType.Point,
            LineString = new LineString()
        };
        Geometry unknownType = new Geometry
        {
            GeometryType = (GeometryType)999
        };

        ValidationResult nullResult =
            StructuralValidator.ValidateGeometry(null);
        ValidationResult wrongResult =
            StructuralValidator.ValidateGeometry(wrongPayload);
        ValidationResult extraResult =
            StructuralValidator.ValidateGeometry(extraPayload);
        ValidationResult unknownResult =
            StructuralValidator.ValidateGeometry(unknownType);

        Assert.Multiple(() =>
        {
            AssertFailure(nullResult,
                ValidationError.NullGeometry, "Geometry");
            AssertFailure(wrongResult,
                ValidationError.PayloadMismatch, "LineString");
            AssertFailure(extraResult,
                ValidationError.PayloadMismatch, "Point");
            AssertFailure(unknownResult,
                ValidationError.InvalidGeometryType, "Geometry");
        });
    }

    [Test]
    public void CoordinatesValidateFinitenessBeforeLayoutConformance()
    {
        Geometry nonFinite = LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(double.PositiveInfinity, 1.0));
        Geometry missingZ = LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 1.0));
        missingZ.CoordinateLayout = CoordinateLayout.XYZ;
        Geometry unknownLayout = LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 1.0));
        unknownLayout.CoordinateLayout = (CoordinateLayout)99;

        ValidationResult finiteResult =
            StructuralValidator.ValidateGeometry(nonFinite);
        ValidationResult zResult =
            StructuralValidator.ValidateGeometry(missingZ);
        ValidationResult layoutResult =
            StructuralValidator.ValidateGeometry(unknownLayout);

        Assert.Multiple(() =>
        {
            AssertFailure(finiteResult,
                ValidationError.NonFiniteCoordinate,
                "LineString/Coordinates[1]");
            Assert.That(finiteResult.Coordinate, Is.Not.Null);
            Assert.That(double.IsPositiveInfinity(
                finiteResult.Coordinate!.X), Is.True);
            AssertFailure(zResult,
                ValidationError.InvalidCoordinateLayout,
                "LineString/Coordinates[0]");
            AssertFailure(layoutResult,
                ValidationError.InvalidCoordinateLayout, "LineString");
        });
    }

    [Test]
    public void SpatialReferenceAndChildMetadataMustBeConsistent()
    {
        Geometry negative = Read("POINT (1 2)");
        negative.SpatialReferenceId = -1;
        Geometry collection = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION (POINT (1 2), " +
            "LINESTRING (0 0, 1 1))");
        collection.GeometryCollection!.Geometries[1]
            .SpatialReferenceId = 3857;

        ValidationResult negativeResult =
            StructuralValidator.ValidateGeometry(negative);
        ValidationResult collectionResult =
            StructuralValidator.ValidateGeometry(collection);

        Assert.Multiple(() =>
        {
            AssertFailure(negativeResult,
                ValidationError.InvalidCoordinateLayout, "Point");
            AssertFailure(collectionResult,
                ValidationError.InvalidCoordinateLayout,
                "GeometryCollection/Geometries[1]");
        });
    }

    [Test]
    public void LineAndRingCardinalityClosureAndDistinctnessAreReported()
    {
        Geometry shortLine = LineGeometry(
            CoordinateValue(0.0, 0.0));
        Geometry shortRing = RingGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(0.0, 0.0)
        ]);
        Geometry openRing = RingGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(1.0, 1.0),
            CoordinateValue(0.0, 1.0)
        ]);
        Geometry indistinctRing = RingGeometry([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(0.0, 0.0)
        ]);

        Assert.Multiple(() =>
        {
            AssertFailure(StructuralValidator.ValidateGeometry(shortLine),
                ValidationError.TooFewCoordinates,
                "LineString/Coordinates");
            AssertFailure(StructuralValidator.ValidateGeometry(shortRing),
                ValidationError.TooFewCoordinates,
                "LinearRing/Coordinates");
            AssertFailure(StructuralValidator.ValidateGeometry(openRing),
                ValidationError.RingNotClosed,
                "LinearRing/Coordinates");
            AssertFailure(
                StructuralValidator.ValidateGeometry(indistinctRing),
                ValidationError.TooFewDistinctCoordinates,
                "LinearRing/Coordinates");
        });
    }

    [Test]
    public void NullArraysAndNullHomogeneousComponentsAreReported()
    {
        Geometry nullCoordinates = LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 1.0));
        nullCoordinates.LineString!.Coordinates = null!;
        Geometry nullPoint = new Geometry
        {
            GeometryType = GeometryType.MultiPoint,
            Point = null,
            MultiPoint = new MultiPoint { Points = [null!] }
        };
        Geometry nullPolygons = new Geometry
        {
            GeometryType = GeometryType.MultiPolygon,
            Point = null,
            MultiPolygon = new MultiPolygon { Polygons = null! }
        };
        Geometry nullCollection = new Geometry
        {
            GeometryType = GeometryType.GeometryCollection,
            Point = null,
            GeometryCollection = new GeometryCollection
            {
                Geometries = null!
            }
        };

        Assert.Multiple(() =>
        {
            AssertFailure(
                StructuralValidator.ValidateGeometry(nullCoordinates),
                ValidationError.NullGeometry,
                "LineString/Coordinates");
            AssertFailure(
                StructuralValidator.ValidateGeometry(nullPoint),
                ValidationError.NullGeometry,
                "MultiPoint/Points[0]");
            AssertFailure(
                StructuralValidator.ValidateGeometry(nullPolygons),
                ValidationError.NullGeometry,
                "MultiPolygon/Polygons");
            AssertFailure(
                StructuralValidator.ValidateGeometry(nullCollection),
                ValidationError.NullGeometry,
                "GeometryCollection/Geometries");
        });
    }

    [Test]
    public void CyclesAreDetectedBeforeNestingLimits()
    {
        Geometry cyclic = new Geometry
        {
            GeometryType = GeometryType.GeometryCollection,
            Point = null,
            GeometryCollection = new GeometryCollection()
        };
        cyclic.GeometryCollection.Geometries = [cyclic];

        ValidationResult result =
            StructuralValidator.ValidateGeometryWithLimits(
                cyclic, 0, 10, 10);

        AssertFailure(result, ValidationError.CyclicGeometry,
            "GeometryCollection/Geometries[0]");
    }

    [Test]
    public void ConfiguredDepthComponentAndCoordinateLimitsAreEnforced()
    {
        Geometry nested = Read(
            "GEOMETRYCOLLECTION (GEOMETRYCOLLECTION (POINT (1 2)))");
        Geometry components = Read(
            "MULTIPOINT ((0 0), (1 1), (2 2))");
        Geometry coordinates = Read(
            "LINESTRING (0 0, 1 0, 2 0)");

        ValidationResult depthResult =
            StructuralValidator.ValidateGeometryWithLimits(
                nested, 1, 10, 10);
        ValidationResult componentResult =
            StructuralValidator.ValidateGeometryWithLimits(
                components, 2, 3, 10);
        ValidationResult coordinateResult =
            StructuralValidator.ValidateGeometryWithLimits(
                coordinates, 2, 10, 2);

        Assert.Multiple(() =>
        {
            AssertFailure(depthResult,
                ValidationError.NestingLimitExceeded,
                "GeometryCollection/Geometries[0]/Geometries[0]");
            AssertFailure(componentResult,
                ValidationError.NestingLimitExceeded,
                "MultiPoint/Points[2]");
            AssertFailure(coordinateResult,
                ValidationError.CoordinateLimitExceeded,
                "LineString/Coordinates[2]");
        });
    }

    [TestCase(2)]
    [TestCase(4)]
    public void CircularStringsRequireOddArcCardinality(int count)
    {
        Coordinate[] coordinates = new Coordinate[count];
        int index = 0;
        while (index < count)
        {
            coordinates[index] = CoordinateValue(index, index % 2);
            index++;
        }
        Geometry circular = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates = coordinates
            }
        };

        ValidationResult result =
            StructuralValidator.ValidateGeometry(circular);

        AssertFailure(result, ValidationError.InvalidArcCardinality,
            "CircularString/Coordinates");
    }

    [Test]
    public void CurvePolygonRingsMustBeClosedAndHaveEnoughPositions()
    {
        Geometry open = CurvePolygonGeometry(LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0)));
        Geometry shortRing = CurvePolygonGeometry(LineGeometry(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(0.0, 0.0)));

        Assert.Multiple(() =>
        {
            AssertFailure(StructuralValidator.ValidateGeometry(open),
                ValidationError.RingNotClosed,
                "CurvePolygon/Shell");
            AssertFailure(
                StructuralValidator.ValidateGeometry(shortRing),
                ValidationError.TooFewCoordinates,
                "CurvePolygon/Shell");
        });
    }

    [Test]
    public void StructurallyValidDegenerateAndEmptyValuesAreAccepted()
    {
        Geometry repeatedLine = LineGeometry(
            CoordinateValue(3.0, 4.0),
            CoordinateValue(3.0, 4.0));
        Geometry emptyPolygon = Read("POLYGON EMPTY");
        Geometry emptyCollection = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry emptyCircular = Read("CIRCULARSTRING EMPTY");

        Assert.Multiple(() =>
        {
            AssertValid(StructuralValidator.ValidateGeometry(repeatedLine));
            AssertValid(StructuralValidator.ValidateGeometry(emptyPolygon));
            AssertValid(
                StructuralValidator.ValidateGeometry(emptyCollection));
            AssertValid(
                StructuralValidator.ValidateGeometry(emptyCircular));
        });
    }

    private static Geometry CurvePolygonGeometry(Geometry shell)
    {
        return new Geometry
        {
            GeometryType = GeometryType.CurvePolygon,
            Point = null,
            CurvePolygon = new CurvePolygon
            {
                Shell = shell
            }
        };
    }

    private static Geometry LineGeometry(params Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = coordinates }
        };
    }

    private static Geometry RingGeometry(Coordinate[] coordinates)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing { Coordinates = coordinates }
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertValid(ValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(ValidationError.None));
            Assert.That(result.Message, Is.Empty);
            Assert.That(result.Coordinate, Is.Null);
            Assert.That(result.ComponentPath, Is.Empty);
        });
    }

    private static void AssertFailure(
        ValidationResult result, ValidationError error, string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.ComponentPath, Is.EqualTo(path));
        });
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
