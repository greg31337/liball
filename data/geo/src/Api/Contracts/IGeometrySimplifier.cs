namespace geo;

/// <summary>Simplifies, densifies, and linearizes geometry into independent owning results.</summary>
public interface IGeometrySimplifier
{
    /// <summary>Simplifies geometry with the selected distance or effective-area algorithm.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="parameters">The algorithm, tolerance, endpoint, and collapse policies.</param>
    /// <returns>An owning simplified geometry.</returns>
    Geometry Simplify(
        Geometry geometry, SimplifyParameters parameters);

    /// <summary>Inserts vertices so no source segment exceeds the requested planar length.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="maximumSegmentLength">The finite positive maximum segment length.</param>
    /// <param name="precisionModel">The precision applied to generated coordinates.</param>
    /// <returns>An owning densified geometry.</returns>
    Geometry Densify(
        Geometry geometry, double maximumSegmentLength,
        PrecisionModel precisionModel);

    /// <summary>Constructs a simplified inner or outer polygon hull.</summary>
    /// <param name="polygonal">The polygonal source geometry.</param>
    /// <param name="parameters">Polygon-hull parameters whose tolerance is a vertex-removal ratio.</param>
    /// <param name="outer">Whether to construct an outer rather than inner hull.</param>
    /// <returns>An owning polygonal hull.</returns>
    Geometry SimplifyPolygonHull(
        Geometry polygonal, SimplifyParameters parameters,
        bool outer);

    /// <summary>Converts every curved component to a linear geometry.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="parameters">The arc subdivision policy.</param>
    /// <returns>An owning geometry containing no curved components.</returns>
    Geometry LinearizeCurves(
        Geometry geometry,
        CurveLinearizationParameters parameters);

    /// <summary>Simplifies geometry after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="simplifyParameters">The simplification policy.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning simplified linear geometry.</returns>
    Geometry SimplifyAfterCurveLinearization(
        Geometry geometry,
        SimplifyParameters simplifyParameters,
        CurveLinearizationParameters curveParameters);
}
