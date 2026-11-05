namespace geo;

/// <summary>Constructs robust planar overlay, clipping, splitting, snapping, noding, and shared-path results.</summary>
public interface IGeometryOverlay
{
    /// <summary>Computes the common point set of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning intersection result.</returns>
    Geometry Intersection(Geometry first, Geometry second);

    /// <summary>Computes an intersection after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision intersection result.</returns>
    Geometry IntersectionAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel);

    /// <summary>Computes the combined point set of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning union result.</returns>
    Geometry Union(Geometry first, Geometry second);

    /// <summary>Computes a union after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision union result.</returns>
    Geometry UnionAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel);

    /// <summary>Computes the portion of the first geometry outside the second.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <returns>The narrowest owning difference result.</returns>
    Geometry Difference(Geometry first, Geometry second);

    /// <summary>Computes a difference after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision difference result.</returns>
    Geometry DifferenceAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel);

    /// <summary>Computes points belonging to exactly one of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning symmetric-difference result.</returns>
    Geometry SymmetricDifference(Geometry first, Geometry second);

    /// <summary>Computes a symmetric difference after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision symmetric-difference result.</returns>
    Geometry SymmetricDifferenceAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel);

    /// <summary>Dissolves all compatible components of one geometry.</summary>
    /// <param name="geometry">The geometry to union.</param>
    /// <returns>The narrowest owning unary-union result.</returns>
    Geometry UnaryUnion(Geometry geometry);

    /// <summary>Unions polygonal components using a balanced cascaded strategy.</summary>
    /// <param name="geometry">The polygonal geometry to union.</param>
    /// <returns>The owning cascaded-union result.</returns>
    Geometry CascadedUnion(Geometry geometry);

    /// <summary>Unions two geometries by independently processing envelope-connected subsets.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning disjoint-subset union result.</returns>
    Geometry DisjointSubsetUnion(Geometry first, Geometry second);

    /// <summary>Dissolves duplicate and overlapping components of one geometry.</summary>
    /// <param name="geometry">The geometry to dissolve.</param>
    /// <returns>The narrowest owning dissolved result.</returns>
    Geometry Dissolve(Geometry geometry);

    /// <summary>Clips a geometry to an axis-aligned envelope while preserving dimensional collapses.</summary>
    /// <param name="geometry">The geometry to clip.</param>
    /// <param name="envelope">The clipping envelope.</param>
    /// <returns>The narrowest owning clipped result.</returns>
    Geometry ClipToEnvelope(Geometry geometry, Envelope envelope);

    /// <summary>Splits a lineal geometry at points, line intersections, or polygon boundaries.</summary>
    /// <param name="geometry">The lineal geometry to split.</param>
    /// <param name="splitter">The point, line, or polygonal splitting geometry.</param>
    /// <returns>An owning collection of source-ordered pieces.</returns>
    Geometry Split(Geometry geometry, Geometry splitter);

    /// <summary>Snaps a source geometry to target vertices within a planar tolerance.</summary>
    /// <param name="geometry">The source geometry to snap.</param>
    /// <param name="target">The target geometry supplying snap vertices.</param>
    /// <param name="tolerance">The finite non-negative snap tolerance.</param>
    /// <returns>An owning snapped copy of the source geometry.</returns>
    Geometry Snap(
        Geometry geometry, Geometry target, double tolerance);

    /// <summary>Returns fully noded linework from a geometry.</summary>
    /// <param name="geometry">The geometry whose linework is noded.</param>
    /// <returns>The narrowest owning lineal result.</returns>
    Geometry Node(Geometry geometry);

    /// <summary>Finds shared lineal paths classified by equal or opposite traversal direction.</summary>
    /// <param name="first">The source lineal geometry defining result direction.</param>
    /// <param name="second">The lineal geometry compared with the source.</param>
    /// <returns>A two-member collection containing forward and reverse multi-line results.</returns>
    Geometry SharedPaths(Geometry first, Geometry second);

    /// <summary>Computes an intersection after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning intersection of the linearized inputs.</returns>
    Geometry IntersectionAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Computes a union after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning union of the linearized inputs.</returns>
    Geometry UnionAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Computes a difference after explicitly bounded curve linearization.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning difference of the linearized inputs.</returns>
    Geometry DifferenceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Computes a symmetric difference after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning symmetric difference of the linearized inputs.</returns>
    Geometry SymmetricDifferenceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Dissolves a geometry after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The geometry to union.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning unary union of the linearized input.</returns>
    Geometry UnaryUnionAfterCurveLinearization(
        Geometry geometry, CurveLinearizationParameters parameters);
}
