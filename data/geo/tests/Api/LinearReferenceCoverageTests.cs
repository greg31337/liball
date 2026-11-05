using geo;

namespace geo.Tests;

public class LinearReferenceCoverageTests
{
    [Test]
    public void ComponentIndexesHandleEmptyComponentsAndRepeatedVertices()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = MultiLineWithEmptyAndRepeatedComponents();
        LinearReferenceOptions lower = new LinearReferenceOptions();
        LinearReferenceOptions upper = new LinearReferenceOptions
        {
            ResolveLowerLocation = false
        };
        LinearReferenceOptions clamp = new LinearReferenceOptions
        {
            ClampOutOfRange = true
        };
        LinearLocation lowerBoundary =
            referencer.GetLocationAtAbsoluteIndex(
                source, 5.0, lower);
        LinearLocation upperBoundary =
            referencer.GetLocationAtAbsoluteIndex(
                source, 5.0, upper);
        LinearLocation normalized =
            referencer.GetLocationAtNormalizedIndex(
                source, 5.0 / 9.0, upper);

        Assert.Multiple(() =>
        {
            Assert.That(referencer.GetTotalLength(source),
                Is.EqualTo(9.0));
            AssertLocation(referencer.GetStartLocation(source),
                true, 1, 0, 0.0);
            AssertLocation(referencer.GetEndLocation(source),
                true, 3, 1, 0.0);
            AssertLocation(lowerBoundary, true, 1, 2, 0.0);
            AssertLocation(upperBoundary, true, 3, 0, 0.0);
            AssertLocation(normalized, true, 3, 0, 0.0);
            Assert.That(referencer.GetAbsoluteIndex(
                source, upperBoundary), Is.EqualTo(5.0));
            AssertLocation(referencer.GetLocationAtAbsoluteIndex(
                source, -1.0, clamp), true, 1, 0, 0.0);
            AssertLocation(referencer.GetLocationAtNormalizedIndex(
                source, 2.0, clamp), true, 3, 1, 0.0);
        });
    }

    [Test]
    public void EndpointOffsetsSkipDegenerateSegments()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry repeatedStart = Read(
            "LINESTRING (0 0, 0 0, 10 0)");
        Geometry repeatedEnd = Read(
            "LINESTRING (0 0, 10 0, 10 0)");
        Geometry degenerate = Read(
            "LINESTRING (2 3, 2 3)");
        Coordinate? startOffset = referencer.GetOffsetCoordinate(
            repeatedStart, referencer.GetStartLocation(repeatedStart), 2.0);
        Coordinate? endOffset = referencer.GetOffsetCoordinate(
            repeatedEnd, referencer.GetEndLocation(repeatedEnd), 2.0);
        Coordinate? zeroOffset = referencer.GetOffsetCoordinate(
            degenerate, referencer.GetStartLocation(degenerate), 0.0);

        Assert.Multiple(() =>
        {
            AssertPlanar(startOffset!, 0.0, 2.0);
            AssertPlanar(endOffset!, 10.0, 2.0);
            AssertPlanar(zeroOffset!, 2.0, 3.0);
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.GetOffsetCoordinate(
                    degenerate,
                    referencer.GetStartLocation(degenerate), 1.0));
        });
    }

    [Test]
    public void ProjectionTiesChooseEarliestComponentAndInterpolateZM()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "MULTILINESTRING ZM ((0 0 0 0, 10 0 10 10), " +
            "(0 2 20 20, 10 2 30 30))");
        LinearLocation projected = referencer.ProjectCoordinate(
            source, new Coordinate { X = 5.0, Y = 1.0 });
        Coordinate? coordinate =
            referencer.GetCoordinateAtLocation(source, projected);

        Assert.Multiple(() =>
        {
            AssertLocation(projected, true, 0, 0, 0.5);
            AssertCoordinate(coordinate!, 5.0, 0.0, 5.0, 5.0);
        });
    }

    [Test]
    public void ReverseCrossComponentSubstringIsOwningAndPreservesZM()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry source = Read(
            "MULTILINESTRING ZM ((0 0 0 0, 10 0 10 10, " +
            "10 0 10 10), (20 0 20 20, 20 10 30 30))");
        Geometry result = referencer.ExtractLine(
            source, Location(1, 0, 0.5), Location(0, 0, 0.5));
        source.MultiLineString!.LineStrings[1].Coordinates[0].M = 900.0;
        LineString[] lines = result.MultiLineString!.LineStrings;

        Assert.Multiple(() =>
        {
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(lines, Has.Length.EqualTo(2));
            AssertCoordinate(lines[0].Coordinates[0],
                20.0, 5.0, 25.0, 25.0);
            AssertCoordinate(lines[0].Coordinates[1],
                20.0, 0.0, 20.0, 20.0);
            Assert.That(lines[1].Coordinates, Has.Length.EqualTo(3));
            AssertCoordinate(lines[1].Coordinates[0],
                10.0, 0.0, 10.0, 10.0);
            AssertCoordinate(lines[1].Coordinates[2],
                5.0, 0.0, 5.0, 5.0);
        });
    }

    [Test]
    public void MeasureMonotonicityIncludesComponentTransitions()
    {
        ILinearReferencer referencer = new LinearReferencer();
        Geometry increasingPlateau = Read(
            "MULTILINESTRING M ((0 0 0, 1 0 10), " +
            "(2 0 10, 3 0 20))");
        Geometry decreasing = Read(
            "MULTILINESTRING M ((0 0 30, 1 0 20), " +
            "(2 0 10, 3 0 0))");
        Geometry nonmonotonicTransition = Read(
            "MULTILINESTRING M ((0 0 0, 1 0 10), " +
            "(2 0 5, 3 0 20))");
        LinearReferenceOptions earlier = new LinearReferenceOptions();
        LinearReferenceOptions later = new LinearReferenceOptions
        {
            ResolveLowerLocation = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(referencer.IsMonotonicMeasure(
                increasingPlateau), Is.True);
            Assert.That(referencer.IsMonotonicMeasure(decreasing),
                Is.True);
            Assert.That(referencer.IsMonotonicMeasure(
                nonmonotonicTransition), Is.False);
            AssertLocation(referencer.GetLocationAtMeasure(
                increasingPlateau, 10.0, earlier),
                true, 0, 1, 0.0);
            AssertLocation(referencer.GetLocationAtMeasure(
                increasingPlateau, 10.0, later),
                true, 1, 0, 0.0);
            AssertCoordinate(referencer.GetCoordinateAtMeasure(
                decreasing, 25.0, earlier)!,
                0.5, 0.0, double.NaN, 25.0);
            Assert.Throws<InvalidGeometryException>(() =>
                referencer.GetLocationAtMeasure(
                    nonmonotonicTransition, 7.0, earlier));
        });
    }

    [Test]
    public void EmptyInputsReturnStableEmptyResults()
    {
        ILinearReferencer referencer = new LinearReferencer();
        LinearReferenceOptions options = new LinearReferenceOptions();
        Geometry empty = Read("MULTILINESTRING EMPTY");
        Geometry measuredEmpty = Read("MULTILINESTRING M EMPTY");
        LinearLocation invalid = new LinearLocation { IsValid = false };
        Geometry substring = referencer.ExtractLine(
            empty, invalid, invalid);
        Geometry measureInterval = referencer.ExtractMeasureInterval(
            measuredEmpty, 1.0, 2.0, options);

        Assert.Multiple(() =>
        {
            Assert.That(referencer.GetTotalLength(empty), Is.Zero);
            Assert.That(referencer.GetLocationAtAbsoluteIndex(
                empty, 5.0, options).IsValid, Is.False);
            Assert.That(referencer.GetLocationAtNormalizedIndex(
                empty, 0.5, options).IsValid, Is.False);
            Assert.That(referencer.ProjectCoordinate(
                empty, new Coordinate { X = 1.0, Y = 2.0 }).IsValid,
                Is.False);
            Assert.That(referencer.GetCoordinateAtLocation(
                empty, invalid), Is.Null);
            Assert.That(referencer.GetOffsetCoordinate(
                empty, invalid, 2.0), Is.Null);
            Assert.That(substring.LineString!.Coordinates, Is.Empty);
            Assert.That(referencer.IsMonotonicMeasure(measuredEmpty),
                Is.True);
            Assert.That(referencer.GetLocationAtMeasure(
                measuredEmpty, 4.0, options).IsValid, Is.False);
            Assert.That(measureInterval.LineString!.Coordinates,
                Is.Empty);
        });
    }

    private static Geometry MultiLineWithEmptyAndRepeatedComponents()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiLineString,
            Point = null,
            MultiLineString = new MultiLineString
            {
                LineStrings =
                [
                    new LineString(),
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 0.0, Y = 0.0 },
                            new Coordinate { X = 3.0, Y = 4.0 },
                            new Coordinate { X = 3.0, Y = 4.0 }
                        ]
                    },
                    new LineString(),
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 10.0, Y = 0.0 },
                            new Coordinate { X = 10.0, Y = 4.0 }
                        ]
                    }
                ]
            }
        };
    }

    private static LinearLocation Location(
        int component, int segment, double fraction)
    {
        return new LinearLocation
        {
            ComponentIndex = component,
            SegmentIndex = segment,
            SegmentFraction = fraction
        };
    }

    private static void AssertLocation(
        LinearLocation location, bool valid,
        int component, int segment, double fraction)
    {
        Assert.Multiple(() =>
        {
            Assert.That(location.IsValid, Is.EqualTo(valid));
            Assert.That(location.ComponentIndex, Is.EqualTo(component));
            Assert.That(location.SegmentIndex, Is.EqualTo(segment));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(fraction).Within(1.0e-14));
        });
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z, double measure)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
            if (double.IsNaN(z))
            {
                Assert.That(coordinate.Z, Is.NaN);
            }
            else
            {
                Assert.That(coordinate.Z,
                    Is.EqualTo(z).Within(1.0e-14));
            }
            Assert.That(coordinate.M,
                Is.EqualTo(measure).Within(1.0e-14));
        });
    }

    private static void AssertPlanar(
        Coordinate coordinate, double x, double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(1.0e-14));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-14));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
