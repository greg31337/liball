using geo;

namespace geo.Tests;

public class FixedSeedRandomGeometryTests
{
    private static readonly GeometryType[] CoreTypes =
    [
        GeometryType.Point,
        GeometryType.LineString,
        GeometryType.Polygon,
        GeometryType.MultiPoint,
        GeometryType.MultiLineString,
        GeometryType.MultiPolygon,
        GeometryType.GeometryCollection
    ];

    [Test]
    public void FixedSeedValidGeometriesValidateAndRoundTrip()
    {
        const int seed = 152001;
        const int casesPerType = 24;
        RandomGeometryFixtureGenerator generator =
            new RandomGeometryFixtureGenerator(seed);
        IGeometryValidator validator = new GeometryValidator();
        IGeometryPredicates predicates = new GeometryPredicates();
        IWktCodec textCodec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        IWkbCodec littleEndianCodec = BinaryCodec(ByteOrder.LittleEndian);
        IWkbCodec bigEndianCodec = BinaryCodec(ByteOrder.BigEndian);

        int typeIndex = 0;
        int caseIndex = 0;
        while (typeIndex < CoreTypes.Length)
        {
            int typeCase = 0;
            while (typeCase < casesPerType)
            {
                GeometryType geometryType = CoreTypes[typeIndex];
                Geometry geometry = generator.NextValid(geometryType);
                int capturedIndex = caseIndex;
                RandomizedRegressionCapture.Check(
                    seed, capturedIndex, geometry, candidate =>
                    {
                        ValidationResult validation =
                            validator.ValidateGeometry(candidate);
                        Geometry textRoundTrip = textCodec.ReadWkt(
                            textCodec.WriteWkt(candidate));
                        IWkbCodec binaryCodec = capturedIndex % 2 == 0 ?
                            littleEndianCodec : bigEndianCodec;
                        Geometry binaryRoundTrip = binaryCodec.ReadWkb(
                            binaryCodec.WriteWkb(candidate));
                        string description = Description(
                            seed, capturedIndex, geometryType,
                            candidate.CoordinateLayout);

                        Assert.Multiple(() =>
                        {
                            Assert.That(validation.IsValid, Is.True,
                                description + ": " + validation.Message);
                            Assert.That(validation.Error,
                                Is.EqualTo(ValidationError.None), description);
                            Assert.That(predicates.AreExactlyEqual(
                                candidate, textRoundTrip), Is.True,
                                description + " WKT");
                            Assert.That(predicates.AreExactlyEqual(
                                candidate, binaryRoundTrip), Is.True,
                                description + " WKB");
                        });
                    });
                typeCase++;
                caseIndex++;
            }
            typeIndex++;
        }
    }

    [Test]
    public void FixedSeedInvalidGeometriesReturnValidationDiagnostics()
    {
        const int seed = 152002;
        const int casesPerType = 24;
        RandomGeometryFixtureGenerator generator =
            new RandomGeometryFixtureGenerator(seed);
        IGeometryValidator validator = new GeometryValidator();

        int typeIndex = 0;
        int caseIndex = 0;
        while (typeIndex < CoreTypes.Length)
        {
            int typeCase = 0;
            while (typeCase < casesPerType)
            {
                GeometryType geometryType = CoreTypes[typeIndex];
                Geometry geometry = generator.NextInvalid(geometryType);
                int capturedIndex = caseIndex;
                RandomizedRegressionCapture.Check(
                    seed, capturedIndex, geometry, candidate =>
                    {
                        ValidationResult validation =
                            validator.ValidateGeometry(candidate);
                        string description = Description(
                            seed, capturedIndex, geometryType,
                            candidate.CoordinateLayout);

                        Assert.Multiple(() =>
                        {
                            Assert.That(validation.IsValid, Is.False,
                                description);
                            Assert.That(validation.Error,
                                Is.Not.EqualTo(ValidationError.None),
                                description);
                            Assert.That(validation.Message, Is.Not.Empty,
                                description);
                        });
                    });
                typeCase++;
                caseIndex++;
            }
            typeIndex++;
        }
    }

    private static IWkbCodec BinaryCodec(ByteOrder byteOrder)
    {
        return new WkbCodec(
            new WkbReadOptions(),
            new WkbWriteOptions { ByteOrder = byteOrder });
    }

    private static string Description(
        int seed, int caseIndex, GeometryType geometryType,
        CoordinateLayout layout)
    {
        return "seed=" + seed + ", case=" + caseIndex +
            ", type=" + geometryType + ", layout=" + layout;
    }
}
