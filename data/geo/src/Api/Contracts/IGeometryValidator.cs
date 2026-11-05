namespace geo;

/// <summary>Validates geometry structure, topology, simplicity, coverage, and buffer results.</summary>
public interface IGeometryValidator
{
    /// <summary>Validates tags, payloads, metadata, coordinate sequences, and collection limits.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <returns>A detailed structural validation result.</returns>
    ValidationResult ValidateStructure(Geometry geometry);

    /// <summary>Validates structural and topological geometry rules.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <returns>A detailed geometry validation result.</returns>
    ValidationResult ValidateGeometry(Geometry geometry);

    /// <summary>Validates geometry topology after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>A detailed validation result for the linearized geometry.</returns>
    ValidationResult ValidateAfterCurveLinearization(
        Geometry geometry, CurveLinearizationParameters parameters);

    /// <summary>Checks point or lineal simplicity using the selected endpoint rule.</summary>
    /// <param name="geometry">The point or lineal geometry to examine.</param>
    /// <param name="boundaryNodeRule">The endpoint rule used by lineal simplicity.</param>
    /// <returns>A detailed simplicity result.</returns>
    ValidationResult CheckSimplicity(
        Geometry geometry, BoundaryNodeRule boundaryNodeRule);

    /// <summary>Validates polygon components for overlaps and eligible narrow gaps.</summary>
    /// <param name="polygonal">The polygon coverage to validate.</param>
    /// <param name="maximumGapWidth">The finite non-negative gap width to report.</param>
    /// <returns>A detailed coverage validation result and diagnostic geometry.</returns>
    CoverageValidationResult ValidateCoverage(
        Geometry polygonal, double maximumGapWidth);

    /// <summary>Validates polygonality, containment, area, and boundary distance of a buffer result.</summary>
    /// <param name="source">The geometry used to construct the buffer.</param>
    /// <param name="distance">The signed buffer distance.</param>
    /// <param name="result">The proposed buffer result.</param>
    /// <param name="parameters">The buffer construction policies.</param>
    /// <returns>A detailed buffer validation result.</returns>
    ValidationResult ValidateBufferResult(
        Geometry source, double distance, Geometry result,
        BufferParameters parameters);
}
