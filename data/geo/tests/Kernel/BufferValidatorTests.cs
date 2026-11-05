using geo;

namespace geo.Tests;

public class BufferValidatorTests
{
    [Test]
    public void GeneratedPointLinePolygonAndNodedBuffersValidate()
    {
        Geometry[] sources =
        [
            Read("POINT (2 3)"),
            Read("LINESTRING (0 0, 10 0)"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))"),
            Read("LINESTRING (0 0, 10 0, 10 3, 0 3)")
        ];
        double[] distances = [2.0, 1.0, 1.0, -1.0, 2.0];
        BufferParameters[] parameters =
        [
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Round),
            Parameters(BufferCapStyle.Square, BufferJoinStyle.Round),
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Mitre),
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Mitre),
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Round)
        ];

        int index = 0;
        while (index < sources.Length)
        {
            Geometry result = BufferBuilder.GetBuffer(
                sources[index], distances[index], parameters[index]);
            ValidationResult validation = BufferValidator.ValidateBufferResult(
                sources[index], distances[index], result,
                parameters[index]);

            Assert.That(validation.IsValid, Is.True,
                $"case {index}: {validation.Error} {validation.Message}");
            Assert.That(validation.Error, Is.EqualTo(ValidationError.None));
            Assert.That(validation.Coordinate, Is.Null);
            index++;
        }
    }

    [Test]
    public void GeneratedSingleSidedAndFixedPrecisionBuffersValidate()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        BufferParameters single = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round);
        single.SingleSided = true;
        BufferParameters fixedParameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round);
        fixedParameters.Precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0,
            OffsetX = 0.25,
            OffsetY = 0.25
        };
        Geometry point = Read("POINT (0.25 0.25)");

        Geometry left = BufferBuilder.GetBuffer(line, 1.0, single);
        Geometry right = BufferBuilder.GetBuffer(line, -1.0, single);
        Geometry fixedResult = BufferBuilder.GetBuffer(
            point, 1.0, fixedParameters);

        Assert.Multiple(() =>
        {
            Assert.That(BufferValidator.ValidateBufferResult(
                line, 1.0, left, single).IsValid, Is.True);
            Assert.That(BufferValidator.ValidateBufferResult(
                line, -1.0, right, single).IsValid, Is.True);
            Assert.That(BufferValidator.ValidateBufferResult(
                point, 1.0, fixedResult, fixedParameters).IsValid, Is.True);
        });
    }

    [Test]
    public void NonPolygonalResultsReceiveDedicatedDiagnostic()
    {
        ValidationResult validation = BufferValidator.ValidateBufferResult(
            Read("POINT (0 0)"), 1.0,
            Read("LINESTRING (-1 0, 1 0)"),
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Round));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error,
                Is.EqualTo(ValidationError.BufferResultNotPolygonal));
            Assert.That(validation.Message, Does.Contain("Polygon"));
            Assert.That(validation.Coordinate, Is.Not.Null);
            Assert.That(validation.ComponentPath,
                Is.EqualTo("BufferResult"));
        });
    }

    [Test]
    public void MissingSourceContainmentReportsExteriorWitness()
    {
        Geometry source = Read("POINT (0 0)");
        Geometry shifted = Read(
            "POLYGON ((9 -1, 11 -1, 11 1, 9 1, 9 -1))");

        ValidationResult validation = BufferValidator.ValidateBufferResult(
            source, 1.0, shifted,
            Parameters(BufferCapStyle.Square, BufferJoinStyle.Round));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error,
                Is.EqualTo(ValidationError.BufferContainmentFailure));
            Assert.That(validation.Coordinate, Is.Not.Null);
            Assert.That(validation.Coordinate!.X, Is.EqualTo(0.0));
            Assert.That(validation.ComponentPath,
                Is.EqualTo("BufferResult/Containment"));
        });
    }

    [Test]
    public void UndersizedAndOversizedBoundariesReportDistanceWitnesses()
    {
        Geometry source = Read("POINT (0 0)");
        Geometry undersized = Read(
            "POLYGON ((0.5 0, 0 0.5, -0.5 0, 0 -0.5, 0.5 0))");
        Geometry oversized = Read(
            "POLYGON ((2 0, 0 2, -2 0, 0 -2, 2 0))");
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round);

        ValidationResult small = BufferValidator.ValidateBufferResult(
            source, 1.0, undersized, parameters);
        ValidationResult large = BufferValidator.ValidateBufferResult(
            source, 1.0, oversized, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(small.Error,
                Is.EqualTo(ValidationError.BufferDistanceFailure));
            Assert.That(large.Error,
                Is.EqualTo(ValidationError.BufferDistanceFailure));
            Assert.That(small.Coordinate, Is.Not.Null);
            Assert.That(large.Coordinate, Is.Not.Null);
            Assert.That(small.Message, Does.Contain("permitted range"));
            Assert.That(large.Message, Does.Contain("permitted range"));
        });
    }

    [Test]
    public void NegativeBufferOutsideSourceReportsContainmentFailure()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry outside = Read(
            "POLYGON ((8 8, 12 8, 12 12, 8 12, 8 8))");

        ValidationResult validation = BufferValidator.ValidateBufferResult(
            source, -1.0, outside,
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Mitre));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error,
                Is.EqualTo(ValidationError.BufferContainmentFailure));
            Assert.That(validation.Coordinate, Is.Not.Null);
            Assert.That(validation.ComponentPath,
                Is.EqualTo("BufferResult/Containment"));
        });
    }

    [Test]
    public void AreaDirectionFailureHasDedicatedDiagnostic()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry smaller = Read(
            "POLYGON ((1 1, 9 1, 9 9, 1 9, 1 1))");

        ValidationResult validation = BufferValidator.ValidateBufferResult(
            source, 1.0, smaller,
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Mitre));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error,
                Is.EqualTo(ValidationError.BufferAreaFailure));
            Assert.That(validation.Message, Does.Contain("less area"));
            Assert.That(validation.ComponentPath,
                Is.EqualTo("BufferResult/Area"));
        });
    }

    [Test]
    public void EmptyResultsValidateForEmptyCollapsedAndNonAreaInputs()
    {
        BufferParameters round = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round);
        BufferParameters flat = Parameters(
            BufferCapStyle.Flat, BufferJoinStyle.Round);
        Geometry emptyPolygon = Read("POLYGON EMPTY");

        ValidationResult emptySource = BufferValidator.ValidateBufferResult(
            Read("GEOMETRYCOLLECTION EMPTY"), 1.0,
            emptyPolygon, round);
        ValidationResult flatPoint = BufferValidator.ValidateBufferResult(
            Read("POINT (0 0)"), 1.0,
            emptyPolygon, flat);
        ValidationResult negativeLine = BufferValidator.ValidateBufferResult(
            Read("LINESTRING (0 0, 2 0)"), -1.0,
            emptyPolygon, round);
        ValidationResult collapsedPolygon =
            BufferValidator.ValidateBufferResult(
                Read("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))"),
                -2.0, emptyPolygon, round);

        Assert.Multiple(() =>
        {
            Assert.That(emptySource.IsValid, Is.True);
            Assert.That(flatPoint.IsValid, Is.True);
            Assert.That(negativeLine.IsValid, Is.True);
            Assert.That(collapsedPolygon.IsValid, Is.True);
        });
    }

    [Test]
    public void MetadataMismatchIsDiagnosedBeforeGeometricChecks()
    {
        Geometry source = ReadWithSrid("SRID=4326;POINT (0 0)");
        Geometry result = ReadWithSrid(
            "SRID=3857;POLYGON ((-1 -1, 1 -1, 1 1, -1 1, -1 -1))");

        ValidationResult validation = BufferValidator.ValidateBufferResult(
            source, 1.0, result,
            Parameters(BufferCapStyle.Square, BufferJoinStyle.Round));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error,
                Is.EqualTo(ValidationError.InvalidCoordinateLayout));
            Assert.That(validation.Message, Does.Contain("metadata"));
        });
    }

    private static BufferParameters Parameters(
        BufferCapStyle capStyle, BufferJoinStyle joinStyle)
    {
        return new BufferParameters
        {
            CapStyle = capStyle,
            JoinStyle = joinStyle,
            QuadrantSegments = 8,
            MitreLimit = 5.0,
            Precision = new PrecisionModel
            {
                Type = PrecisionModelType.Floating
            }
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
}
