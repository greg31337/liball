using geo;

namespace geo.Tests;

public class LengthIndexTests
{
    [Test]
    public void AbsoluteIndexesRoundTripAcrossSegments()
    {
        Geometry line = Read("LINESTRING (0 0, 3 4, 6 4)");
        LinearLocation middle = LengthIndex.GetLocationAtAbsoluteIndex(
            line, 2.5, false, false);
        LinearLocation vertex = LengthIndex.GetLocationAtAbsoluteIndex(
            line, 5.0, false, false);
        LinearLocation end = LengthIndex.GetLocationAtAbsoluteIndex(
            line, 8.0, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(LengthIndex.GetTotalLength(line),
                Is.EqualTo(8.0));
            AssertLocation(middle, true, 0, 0, 0.5);
            AssertLocation(vertex, true, 0, 1, 0.0);
            AssertLocation(end, true, 0, 2, 0.0);
            Assert.That(LengthIndex.GetAbsoluteIndex(line, middle),
                Is.EqualTo(2.5));
            Assert.That(LengthIndex.GetAbsoluteIndex(
                line, Location(0, 1, 0.5)), Is.EqualTo(6.5));
            Assert.That(LengthIndex.GetAbsoluteIndex(
                line, Location(0, 0, 1.0)), Is.EqualTo(5.0));
        });
    }

    [Test]
    public void NormalizedIndexesRoundTripAndHonorClampPolicy()
    {
        Geometry line = Read("LINESTRING (0 0, 3 4, 6 4)");
        LinearLocation vertex = LengthIndex.GetLocationAtNormalizedIndex(
            line, 0.625, false, false);
        LinearLocation before = LengthIndex.GetLocationAtAbsoluteIndex(
            line, -2.0, true, false);
        LinearLocation after = LengthIndex.GetLocationAtNormalizedIndex(
            line, 1.5, true, false);

        Assert.Multiple(() =>
        {
            AssertLocation(vertex, true, 0, 1, 0.0);
            AssertLocation(before, true, 0, 0, 0.0);
            AssertLocation(after, true, 0, 2, 0.0);
            Assert.That(LengthIndex.GetNormalizedIndex(
                line, Location(0, 1, 0.5)), Is.EqualTo(0.8125));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetLocationAtAbsoluteIndex(
                    line, -0.1, false, false));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetLocationAtAbsoluteIndex(
                    line, 8.1, false, false));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetLocationAtNormalizedIndex(
                    line, 1.1, false, false));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetLocationAtNormalizedIndex(
                    line, double.NaN, true, false));
        });
    }

    [Test]
    public void MultiLineBoundariesResolveToRequestedComponentEndpoint()
    {
        Geometry multiLine = MultiLineWithRepeatedEndpoint();
        LinearLocation lower = LengthIndex.GetLocationAtAbsoluteIndex(
            multiLine, 3.0, false, true);
        LinearLocation upper = LengthIndex.GetLocationAtAbsoluteIndex(
            multiLine, 3.0, false, false);
        LinearLocation secondMiddle =
            LengthIndex.GetLocationAtAbsoluteIndex(
                multiLine, 5.0, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(LengthIndex.GetTotalLength(multiLine),
                Is.EqualTo(7.0));
            AssertLocation(lower, true, 0, 2, 0.0);
            AssertLocation(upper, true, 2, 0, 0.0);
            AssertLocation(secondMiddle, true, 2, 0, 0.5);
            Assert.That(LengthIndex.GetAbsoluteIndex(
                multiLine, lower), Is.EqualTo(3.0));
            Assert.That(LengthIndex.GetAbsoluteIndex(
                multiLine, upper), Is.EqualTo(3.0));
            Assert.That(LengthIndex.GetNormalizedIndex(
                multiLine, secondMiddle), Is.EqualTo(5.0 / 7.0));
        });
    }

    [Test]
    public void EmptyAndZeroLengthLineworkProducesStableLocations()
    {
        Geometry empty = Read("MULTILINESTRING EMPTY");
        Geometry repeated = Read("LINESTRING (2 3, 2 3)");
        LinearLocation emptyLocation =
            LengthIndex.GetLocationAtAbsoluteIndex(
                empty, 0.0, false, false);
        LinearLocation repeatedStart =
            LengthIndex.GetLocationAtNormalizedIndex(
                repeated, 0.0, false, false);
        LinearLocation repeatedEnd =
            LengthIndex.GetLocationAtNormalizedIndex(
                repeated, 1.0, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(LengthIndex.GetTotalLength(empty), Is.Zero);
            Assert.That(emptyLocation.IsValid, Is.False);
            Assert.That(LengthIndex.GetAbsoluteIndex(
                empty, new LinearLocation { IsValid = false }), Is.Zero);
            Assert.That(LengthIndex.GetTotalLength(repeated), Is.Zero);
            AssertLocation(repeatedStart, true, 0, 0, 0.0);
            AssertLocation(repeatedEnd, true, 0, 1, 0.0);
            Assert.That(LengthIndex.GetNormalizedIndex(
                repeated, repeatedEnd), Is.Zero);
        });
    }

    [Test]
    public void LengthIndexRejectsUnsupportedAndInvalidLinework()
    {
        Geometry invalidLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }]
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetTotalLength(Read("POINT (0 0)")));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetTotalLength(invalidLine));
            Assert.Throws<InvalidGeometryException>(() =>
                LengthIndex.GetAbsoluteIndex(
                    Read("LINESTRING (0 0, 1 0)"), null!));
        });
    }

    private static Geometry MultiLineWithRepeatedEndpoint()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiLineString,
            Point = null,
            MultiLineString = new MultiLineString
            {
                LineStrings =
                [
                    new LineString
                    {
                        Coordinates =
                        [
                            new Coordinate { X = 0.0, Y = 0.0 },
                            new Coordinate { X = 3.0, Y = 0.0 },
                            new Coordinate { X = 3.0, Y = 0.0 }
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

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
