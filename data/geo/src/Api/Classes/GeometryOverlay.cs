namespace geo;

/// <summary>Constructs owning robust planar overlay and linework-operation results without mutating inputs.</summary>
public class GeometryOverlay : IGeometryOverlay
{
    /// <summary>Creates a geometry overlay provider.</summary>
    public GeometryOverlay()
    {
    }

    /// <summary>Computes the common point set of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning intersection result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Intersection(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetIntersection(values[0], values[1]);
    }

    /// <summary>Computes an intersection after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision intersection result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="PrecisionException">The precision grid is invalid or unusable.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry IntersectionAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetIntersectionWithPrecision(
            values[0], values[1], precisionModel);
    }

    /// <summary>Computes the combined point set of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning union result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Union(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetUnion(values[0], values[1]);
    }

    /// <summary>Computes a union after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision union result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="PrecisionException">The precision grid is invalid or unusable.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry UnionAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetUnionWithPrecision(
            values[0], values[1], precisionModel);
    }

    /// <summary>Computes the portion of the first geometry outside the second.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <returns>The narrowest owning difference result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Difference(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetDifference(values[0], values[1]);
    }

    /// <summary>Computes a difference after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision difference result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="PrecisionException">The precision grid is invalid or unusable.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry DifferenceAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetDifferenceWithPrecision(
            values[0], values[1], precisionModel);
    }

    /// <summary>Computes points belonging to exactly one of two geometries.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning symmetric-difference result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry SymmetricDifference(
        Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetSymmetricDifference(
            values[0], values[1]);
    }

    /// <summary>Computes a symmetric difference after reducing both geometries to a fixed precision grid.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>The narrowest owning fixed-precision symmetric-difference result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="PrecisionException">The precision grid is invalid or unusable.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry SymmetricDifferenceAtPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return OverlayEngine.GetSymmetricDifferenceWithPrecision(
            values[0], values[1], precisionModel);
    }

    /// <summary>Dissolves all compatible components of one geometry.</summary>
    /// <param name="geometry">The geometry to union.</param>
    /// <returns>The narrowest owning unary-union result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry UnaryUnion(Geometry geometry)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return UnaryUnionEngine.GetUnaryUnion(projected);
    }

    /// <summary>Unions polygonal components using a balanced cascaded strategy.</summary>
    /// <param name="geometry">The polygonal geometry to union.</param>
    /// <returns>The owning cascaded-union result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid, unsupported, or not polygonal.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry CascadedUnion(Geometry geometry)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return CascadedUnionEngine.GetCascadedUnion(projected);
    }

    /// <summary>Unions two geometries by independently processing envelope-connected subsets.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The narrowest owning disjoint-subset union result.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry DisjointSubsetUnion(
        Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return DisjointSubsetUnionEngine.GetDisjointSubsetUnion(
            values[0], values[1]);
    }

    /// <summary>Dissolves duplicate and overlapping components of one geometry.</summary>
    /// <param name="geometry">The geometry to dissolve.</param>
    /// <returns>The narrowest owning dissolved result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Dissolve(Geometry geometry)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return UnaryUnionEngine.GetDissolvedGeometry(projected);
    }

    /// <summary>Clips a geometry to an axis-aligned envelope while preserving dimensional collapses.</summary>
    /// <param name="geometry">The geometry to clip.</param>
    /// <param name="envelope">The clipping envelope.</param>
    /// <returns>The narrowest owning clipped result.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or envelope is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry ClipToEnvelope(
        Geometry geometry, Envelope envelope)
    {
        return RectangleClipper.ClipToEnvelope(geometry, envelope);
    }

    /// <summary>Splits a lineal geometry at points, line intersections, or polygon boundaries.</summary>
    /// <param name="geometry">The lineal geometry to split.</param>
    /// <param name="splitter">The point, line, or polygonal splitting geometry.</param>
    /// <returns>An owning collection of source-ordered pieces.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Split(Geometry geometry, Geometry splitter)
    {
        return GeometrySplitter.SplitGeometry(geometry, splitter);
    }

    /// <summary>Snaps a source geometry to target vertices within a planar tolerance.</summary>
    /// <param name="geometry">The source geometry to snap.</param>
    /// <param name="target">The target geometry supplying snap vertices.</param>
    /// <param name="tolerance">The finite non-negative snap tolerance.</param>
    /// <returns>An owning snapped copy of the source geometry.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has conflicting metadata.</exception>
    /// <exception cref="PrecisionException">The tolerance is non-finite or negative.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry Snap(
        Geometry geometry, Geometry target, double tolerance)
    {
        return GeometrySnapper.SnapGeometry(
            geometry, target, tolerance);
    }

    /// <summary>Returns fully noded linework from a geometry.</summary>
    /// <param name="geometry">The geometry whose linework is noded.</param>
    /// <returns>The narrowest owning lineal result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">The linework cannot be fully noded.</exception>
    public Geometry Node(Geometry geometry)
    {
        return GeometryNoder.NodeGeometry(geometry);
    }

    /// <summary>Finds shared lineal paths classified by equal or opposite traversal direction.</summary>
    /// <param name="first">The source lineal geometry defining result direction.</param>
    /// <param name="second">The lineal geometry compared with the source.</param>
    /// <returns>A two-member collection containing forward and reverse multi-line results.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, non-lineal, or has conflicting metadata.</exception>
    /// <exception cref="TopologyException">The linework cannot be mutually noded.</exception>
    public Geometry SharedPaths(Geometry first, Geometry second)
    {
        return SharedPathFinder.GetSharedPaths(first, second);
    }

    /// <summary>Computes an intersection after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning intersection of the linearized inputs.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry IntersectionAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return OverlayEngine.GetIntersection(values[0], values[1]);
    }

    /// <summary>Computes a union after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning union of the linearized inputs.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry UnionAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return OverlayEngine.GetUnion(values[0], values[1]);
    }

    /// <summary>Computes a difference after explicitly bounded curve linearization.</summary>
    /// <param name="first">The geometry being subtracted from.</param>
    /// <param name="second">The geometry to subtract.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning difference of the linearized inputs.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry DifferenceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return OverlayEngine.GetDifference(values[0], values[1]);
    }

    /// <summary>Computes a symmetric difference after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning symmetric difference of the linearized inputs.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry SymmetricDifferenceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return OverlayEngine.GetSymmetricDifference(
            values[0], values[1]);
    }

    /// <summary>Dissolves a geometry after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The geometry to union.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The narrowest owning unary union of the linearized input.</returns>
    /// <exception cref="InvalidGeometryException">The input or linearization policy is invalid.</exception>
    /// <exception cref="TopologyException">A robust topology result cannot be constructed.</exception>
    public Geometry UnaryUnionAfterCurveLinearization(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        Geometry value = CurveOperationLinearizer.LinearizeOne(
            geometry, parameters);
        return UnaryUnionEngine.GetUnaryUnion(value);
    }
}
