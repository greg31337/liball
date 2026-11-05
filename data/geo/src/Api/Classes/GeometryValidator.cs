namespace geo;

/// <summary>Provides non-throwing geometry diagnostics for invalid geometry values.</summary>
public class GeometryValidator : IGeometryValidator
{
    /// <summary>Creates a geometry validation provider.</summary>
    public GeometryValidator()
    {
    }

    /// <summary>Validates tags, payloads, metadata, coordinate sequences, and collection limits.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <returns>A detailed structural validation result.</returns>
    public ValidationResult ValidateStructure(Geometry geometry)
    {
        return StructuralValidator.ValidateGeometry(geometry);
    }

    /// <summary>Validates structural and topological geometry rules.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <returns>A detailed geometry validation result.</returns>
    public ValidationResult ValidateGeometry(Geometry geometry)
    {
        return GeometryValidityChecker.ValidateGeometry(geometry);
    }

    /// <summary>Validates geometry topology after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The geometry to validate.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>A detailed validation result for the linearized geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or linearization parameters are invalid.</exception>
    public ValidationResult ValidateAfterCurveLinearization(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        ValidationResult source =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!source.IsValid) return source;
        Geometry linearized = CurveOperationLinearizer.LinearizeOne(
            geometry, parameters);
        return GeometryValidityChecker.ValidateGeometry(linearized);
    }

    /// <summary>Checks point or lineal simplicity using the selected endpoint rule.</summary>
    /// <param name="geometry">The point or lineal geometry to examine.</param>
    /// <param name="boundaryNodeRule">The endpoint rule used by lineal simplicity.</param>
    /// <returns>A detailed simplicity result.</returns>
    public ValidationResult CheckSimplicity(
        Geometry geometry, BoundaryNodeRule boundaryNodeRule)
    {
        return SimplicityChecker.CheckSimplicity(
            geometry, boundaryNodeRule);
    }

    /// <summary>Validates polygon components for overlaps and eligible narrow gaps.</summary>
    /// <param name="polygonal">The polygon coverage to validate.</param>
    /// <param name="maximumGapWidth">The finite non-negative gap width to report.</param>
    /// <returns>A detailed coverage validation result and diagnostic geometry.</returns>
    /// <exception cref="InvalidGeometryException">The gap width is invalid.</exception>
    public CoverageValidationResult ValidateCoverage(
        Geometry polygonal, double maximumGapWidth)
    {
        return CoverageValidator.ValidateCoverage(
            polygonal, maximumGapWidth);
    }

    /// <summary>Validates polygonality, containment, area, and boundary distance of a buffer result.</summary>
    /// <param name="source">The geometry used to construct the buffer.</param>
    /// <param name="distance">The signed buffer distance.</param>
    /// <param name="result">The proposed buffer result.</param>
    /// <param name="parameters">The buffer construction policies.</param>
    /// <returns>A detailed buffer validation result.</returns>
    /// <exception cref="InvalidGeometryException">The inputs, distance, or parameters are invalid.</exception>
    public ValidationResult ValidateBufferResult(
        Geometry source, double distance, Geometry result,
        BufferParameters parameters)
    {
        return BufferValidator.ValidateBufferResult(
            source, distance, result, parameters);
    }
}
