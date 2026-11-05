using geo;

namespace geo.Tests;

public class GeometryValidatorTests
{
    [Test]
    public void StructureAndGeometryValidationReturnDetailedFailures()
    {
        IGeometryValidator validator = new GeometryValidator();
        Geometry malformed = new Geometry
        {
            LineString = new LineString()
        };

        ValidationResult structure = validator.ValidateStructure(malformed);
        ValidationResult topology = validator.ValidateGeometry(Read(
            "POLYGON ((0 0, 2 2, 0 2, 2 0, 0 0))"));
        ValidationResult nullResult = validator.ValidateGeometry(null!);

        Assert.Multiple(() =>
        {
            Assert.That(structure.IsValid, Is.False);
            Assert.That(structure.Error,
                Is.EqualTo(ValidationError.PayloadMismatch));
            Assert.That(structure.ComponentPath, Is.EqualTo("Point"));
            Assert.That(topology.IsValid, Is.False);
            Assert.That(topology.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(topology.Coordinate, Is.Not.Null);
            Assert.That(topology.ComponentPath,
                Does.StartWith("Polygon/Shell/Segments["));
            Assert.That(nullResult.Error,
                Is.EqualTo(ValidationError.NullGeometry));
        });
    }

    [Test]
    public void SimplicityUsesTheRequestedBoundaryRule()
    {
        IGeometryValidator validator = new GeometryValidator();
        Geometry line = Read(
            "LINESTRING (0 0, 2 2, 0 2, 2 0)");

        ValidationResult result = validator.CheckSimplicity(
            line, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(result.Coordinate, Is.Not.Null);
            Assert.That(result.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(result.Coordinate.Y, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CoverageValidationReportsOverlapAndGapWitnesses()
    {
        IGeometryValidator validator = new GeometryValidator();
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        Geometry gap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)))");

        CoverageValidationResult overlapResult =
            validator.ValidateCoverage(overlap, 0.0);
        CoverageValidationResult gapResult =
            validator.ValidateCoverage(gap, 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(overlapResult.IsValid, Is.False);
            Assert.That(overlapResult.Error,
                Is.EqualTo(ValidationError.CoverageOverlap));
            Assert.That(overlapResult.InvalidGeometry, Is.Not.Null);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                overlapResult.InvalidGeometry!), Is.EqualTo(3.0));
            Assert.That(overlapResult.ComponentPath,
                Is.EqualTo("Coverage/Polygons[1]/OverlapWith[0]"));
            Assert.That(gapResult.IsValid, Is.False);
            Assert.That(gapResult.Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(gapResult.InvalidGeometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(gapResult.ComponentPath,
                Is.EqualTo("Coverage/Polygons[1]/GapWith[0]"));
        });
    }

    [Test]
    public void CoverageValidationAcceptsSharedBoundaries()
    {
        IGeometryValidator validator = new GeometryValidator();
        Geometry coverage = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        CoverageValidationResult result =
            validator.ValidateCoverage(coverage, 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(ValidationError.None));
            Assert.That(result.InvalidGeometry, Is.Null);
            Assert.That(result.Coordinate, Is.Null);
            Assert.That(result.ComponentPath, Is.Empty);
        });
    }

    [Test]
    public void BufferValidationIsExposedByTheService()
    {
        IGeometryValidator validator = new GeometryValidator();

        ValidationResult result = validator.ValidateBufferResult(
            Read("POINT (0 0)"), 1.0,
            Read("LINESTRING (-1 0, 1 0)"),
            new BufferParameters());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error,
                Is.EqualTo(ValidationError.BufferResultNotPolygonal));
            Assert.That(result.ComponentPath, Is.EqualTo("BufferResult"));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
