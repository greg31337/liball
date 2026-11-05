using geo;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsWkbReader = NetTopologySuite.IO.WKBReader;
using NtsWkbWriter = NetTopologySuite.IO.WKBWriter;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

internal static class NtsGeometryAdapter
{
    private static readonly IWktCodec GeoWkt = new WktCodec(
        new WktReadOptions(), new WktWriteOptions());
    private static readonly IWkbCodec GeoWkb = new WkbCodec(
        new WkbReadOptions(),
        new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian });

    internal static Geometry ReadGeo(string text)
    {
        return GeoWkt.ReadWkt(text);
    }

    internal static NtsGeometry ReadNts(string text)
    {
        return new NtsWktReader().Read(text);
    }

    internal static NtsGeometry ToNts(Geometry geometry)
    {
        byte[] bytes = GeoWkb.WriteWkb(geometry);
        return new NtsWkbReader().Read(bytes);
    }

    internal static Geometry ToGeo(NtsGeometry geometry)
    {
        byte[] bytes = new NtsWkbWriter().Write(geometry);
        return GeoWkb.ReadWkb(bytes);
    }

    internal static string WriteGeoWkt(Geometry geometry)
    {
        return GeoWkt.WriteWkt(geometry);
    }

    internal static string WriteNtsWkt(NtsGeometry geometry)
    {
        return geometry.AsText();
    }

    internal static byte[] WriteGeoWkb(Geometry geometry)
    {
        return GeoWkb.WriteWkb(geometry);
    }

    internal static byte[] WriteNtsWkb(NtsGeometry geometry)
    {
        return new NtsWkbWriter().Write(geometry);
    }

    internal static NtsGeometry ReadNtsWkb(byte[] bytes)
    {
        return new NtsWkbReader().Read(bytes);
    }

    internal static Geometry ReadGeoWkb(byte[] bytes)
    {
        return GeoWkb.ReadWkb(bytes);
    }
}

internal static class NtsGeometryComparison
{
    internal static void AssertEquivalent(
        NtsGeometry expected, Geometry actual, string description)
    {
        NtsGeometry converted = NtsGeometryAdapter.ToNts(actual);
        if (expected.IsEmpty || converted.IsEmpty)
        {
            Assert.Multiple(() =>
            {
                Assert.That(converted.IsEmpty,
                    Is.EqualTo(expected.IsEmpty), description);
                Assert.That(converted.GeometryType,
                    Is.EqualTo(expected.GeometryType), description);
            });
            return;
        }

        Assert.That(converted.EqualsTopologically(expected), Is.True,
            description + Environment.NewLine +
            "expected: " + expected + Environment.NewLine +
            "actual: " + converted);
    }

    internal static void AssertAreaEquivalent(
        NtsGeometry expected, Geometry actual, double tolerance,
        string description)
    {
        NtsGeometry converted = NtsGeometryAdapter.ToNts(actual);
        if (expected.IsEmpty || converted.IsEmpty)
        {
            Assert.That(converted.IsEmpty,
                Is.EqualTo(expected.IsEmpty), description);
            return;
        }

        double differenceArea =
            expected.SymmetricDifference(converted).Area;
        Assert.Multiple(() =>
        {
            Assert.That(converted.Area,
                Is.EqualTo(expected.Area).Within(tolerance), description);
            Assert.That(differenceArea, Is.LessThanOrEqualTo(tolerance),
                description + Environment.NewLine +
                "expected: " + expected + Environment.NewLine +
                "actual: " + converted);
        });
    }
}
