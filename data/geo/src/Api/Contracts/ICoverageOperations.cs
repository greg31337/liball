namespace geo;

/// <summary>Validates, extracts, combines, simplifies, and cleans polygon coverages.</summary>
public interface ICoverageOperations
{
    /// <summary>Validates coverage topology and reports overlaps, non-noded boundaries, and eligible gaps.</summary>
    /// <param name="polygonal">The polygon coverage to validate.</param>
    /// <param name="maximumGapWidth">The finite non-negative gap width to report.</param>
    /// <returns>A detailed coverage validation result and diagnostic geometry.</returns>
    CoverageValidationResult ValidatePolygonCoverage(
        Geometry polygonal, double maximumGapWidth);

    /// <summary>Dissolves a valid noded polygon coverage by cancelling shared edges.</summary>
    /// <param name="polygonal">The valid polygon coverage to combine.</param>
    /// <returns>An owning polygonal union.</returns>
    Geometry UnionPolygonCoverage(Geometry polygonal);

    /// <summary>Simplifies shared coverage edges consistently with the Visvalingam-Whyatt algorithm.</summary>
    /// <param name="polygonal">The valid polygon coverage to simplify.</param>
    /// <param name="parameters">The Visvalingam-Whyatt tolerance and preservation policies.</param>
    /// <returns>An owning simplified polygon coverage.</returns>
    Geometry SimplifyPolygonCoverage(
        Geometry polygonal, SimplifyParameters parameters);

    /// <summary>Repairs overlaps, non-noded boundaries, and eligible narrow gaps in a polygon coverage.</summary>
    /// <param name="polygonal">The polygon coverage to clean.</param>
    /// <param name="parameters">The snapping, gap, and overlap ownership policies.</param>
    /// <returns>An owning valid polygon coverage.</returns>
    Geometry CleanPolygonCoverage(
        Geometry polygonal, RepairParameters parameters);

    /// <summary>Extracts every shell and hole edge sequence from a polygon coverage.</summary>
    /// <param name="polygonal">The polygon coverage whose edges are extracted.</param>
    /// <returns>An owning multi-line edge geometry.</returns>
    Geometry ExtractPolygonCoverageEdges(Geometry polygonal);
}
