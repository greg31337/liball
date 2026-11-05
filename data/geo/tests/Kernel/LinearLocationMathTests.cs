using geo;

namespace geo.Tests;

public class LinearLocationMathTests
{
    [Test]
    public void ComparisonUsesValidityComponentSegmentAndFractionOrder()
    {
        LinearLocation invalid = new LinearLocation { IsValid = false };
        LinearLocation first = Location(0, 1, 0.25);
        LinearLocation second = Location(0, 1, 0.75);
        LinearLocation third = Location(0, 2, 0.0);
        LinearLocation fourth = Location(1, 0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(LinearLocationMath.CompareLocations(
                invalid, invalid), Is.Zero);
            Assert.That(LinearLocationMath.CompareLocations(
                invalid, first), Is.Negative);
            Assert.That(LinearLocationMath.CompareLocations(
                first, invalid), Is.Positive);
            Assert.That(LinearLocationMath.CompareLocations(
                first, second), Is.Negative);
            Assert.That(LinearLocationMath.CompareLocations(
                second, third), Is.Negative);
            Assert.That(LinearLocationMath.CompareLocations(
                third, fourth), Is.Negative);
            Assert.That(LinearLocationMath.CompareLocations(
                first, Location(0, 1, 0.25)), Is.Zero);
        });
    }

    [Test]
    public void NormalizationClampsAndCanonicalizesLocations()
    {
        Geometry line = Read("LINESTRING (0 0, 3 0, 6 0)");
        LinearLocation before = LinearLocationMath.NormalizeLocation(
            line, Location(-2, -4, -0.5));
        LinearLocation vertex = LinearLocationMath.NormalizeLocation(
            line, Location(0, 0, 1.0));
        LinearLocation after = LinearLocationMath.NormalizeLocation(
            line, Location(4, 20, 0.5));

        Assert.Multiple(() =>
        {
            AssertLocation(before, true, 0, 0, 0.0);
            AssertLocation(vertex, true, 0, 1, 0.0);
            AssertLocation(after, true, 0, 2, 0.0);
            Assert.That(LinearLocationMath.IsEndpoint(line, vertex),
                Is.False);
            Assert.That(LinearLocationMath.IsEndpoint(line, after),
                Is.True);
        });
    }

    [Test]
    public void ComponentTraversalSkipsEmptyComponentsAndResolvesBoundaries()
    {
        Geometry multiLine = MultiLineWithEmptyComponents();
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(multiLine);
        LinearLocation start =
            LinearLocationMath.CreateStartLocation(multiLine);
        LinearLocation end =
            LinearLocationMath.CreateEndLocation(multiLine);
        LinearLocation lower =
            LinearLocationMath.ResolveComponentEndpoint(
                multiLine, Location(3, 0, 0.0), true);
        LinearLocation upper =
            LinearLocationMath.ResolveComponentEndpoint(
                multiLine, Location(1, 2, 0.0), false);
        LinearLocation normalizedEmpty =
            LinearLocationMath.NormalizeLocation(
                multiLine, Location(2, 0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(components, Has.Length.EqualTo(4));
            Assert.That(components[0], Is.Empty);
            Assert.That(components[1], Has.Length.EqualTo(3));
            Assert.That(components[2], Is.Empty);
            Assert.That(components[3], Has.Length.EqualTo(2));
            AssertLocation(start, true, 1, 0, 0.0);
            AssertLocation(end, true, 3, 1, 0.0);
            AssertLocation(lower, true, 1, 2, 0.0);
            AssertLocation(upper, true, 3, 0, 0.0);
            AssertLocation(normalizedEmpty, true, 3, 0, 0.0);
            Assert.That(LinearLocationMath.GetComponentCoordinates(
                multiLine, 1), Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void EmptyAndInvalidInputsHaveExplicitOutcomes()
    {
        Geometry empty = Read("LINESTRING EMPTY");
        LinearLocation normalized =
            LinearLocationMath.NormalizeLocation(
                empty, Location(0, 0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(normalized.IsValid, Is.False);
            Assert.That(LinearLocationMath.CreateStartLocation(empty)
                .IsValid, Is.False);
            Assert.That(LinearLocationMath.CreateEndLocation(empty)
                .IsValid, Is.False);
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.NormalizeLocation(empty, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.CompareLocations(
                    Location(0, 0, double.NaN), normalized));
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.GetLineComponents(
                    Read("POINT (0 0)")));
            Assert.Throws<InvalidGeometryException>(() =>
                LinearLocationMath.GetComponentCoordinates(empty, 1));
        });
    }

    private static Geometry MultiLineWithEmptyComponents()
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
            Assert.That(location.SegmentFraction, Is.EqualTo(fraction));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
