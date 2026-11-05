namespace geo;

/// <summary>Provides polygon coverage validation, extraction, union, simplification, and cleaning.</summary>
public class CoverageOperations : ICoverageOperations
{
    /// <summary>Creates a polygon coverage operation provider.</summary>
    public CoverageOperations()
    {
    }

    /// <summary>Validates coverage topology and reports overlaps, non-noded boundaries, and eligible gaps.</summary>
    /// <param name="polygonal">The polygon coverage to validate.</param>
    /// <param name="maximumGapWidth">The finite non-negative gap width to report.</param>
    /// <returns>A detailed coverage validation result and diagnostic geometry.</returns>
    /// <exception cref="InvalidGeometryException">The maximum gap width is invalid.</exception>
    public CoverageValidationResult ValidatePolygonCoverage(
        Geometry polygonal, double maximumGapWidth)
    {
        return CoverageValidator.ValidateCoverage(
            polygonal, maximumGapWidth);
    }

    /// <summary>Dissolves a valid noded polygon coverage by cancelling shared edges.</summary>
    /// <param name="polygonal">The valid polygon coverage to combine.</param>
    /// <returns>An owning polygonal union.</returns>
    /// <exception cref="InvalidGeometryException">The coverage is invalid, non-polygonal, or not fully noded.</exception>
    /// <exception cref="TopologyException">The coverage boundary cannot be reconstructed.</exception>
    public Geometry UnionPolygonCoverage(Geometry polygonal)
    {
        return CoverageUnionBuilder.UnionCoverage(polygonal);
    }

    /// <summary>Simplifies shared coverage edges consistently with the Visvalingam-Whyatt algorithm.</summary>
    /// <param name="polygonal">The valid polygon coverage to simplify.</param>
    /// <param name="parameters">The Visvalingam-Whyatt tolerance and preservation policies.</param>
    /// <returns>An owning simplified polygon coverage.</returns>
    /// <exception cref="InvalidGeometryException">The coverage or simplification parameters are invalid.</exception>
    /// <exception cref="TopologyException">The coverage cannot be reconstructed.</exception>
    public Geometry SimplifyPolygonCoverage(
        Geometry polygonal, SimplifyParameters parameters)
    {
        return CoverageSimplifier.SimplifyCoverage(
            polygonal, parameters);
    }

    /// <summary>Repairs overlaps, non-noded boundaries, and eligible narrow gaps in a polygon coverage.</summary>
    /// <param name="polygonal">The polygon coverage to clean.</param>
    /// <param name="parameters">The snapping, gap, and overlap ownership policies.</param>
    /// <returns>An owning valid polygon coverage.</returns>
    /// <exception cref="InvalidGeometryException">The coverage or repair parameters are invalid.</exception>
    /// <exception cref="TopologyException">A valid coverage cannot be reconstructed.</exception>
    public Geometry CleanPolygonCoverage(
        Geometry polygonal, RepairParameters parameters)
    {
        return GeometryRepairEngine.CleanCoverage(
            polygonal, parameters);
    }

    /// <summary>Extracts every shell and hole edge sequence from a polygon coverage.</summary>
    /// <param name="polygonal">The polygon coverage whose edges are extracted.</param>
    /// <returns>An owning multi-line edge geometry.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or contains a non-polygonal component.</exception>
    public Geometry ExtractPolygonCoverageEdges(Geometry polygonal)
    {
        GeometryAssembler assembler = new GeometryAssembler();
        return assembler.ExtractCoverageEdges(polygonal);
    }
}
