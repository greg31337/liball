namespace geo;

/// <summary>Provides owning simplification, densification, polygon-hull, and curve-linearization operations.</summary>
public class GeometrySimplifier : IGeometrySimplifier
{
    /// <summary>Creates a geometry simplification provider.</summary>
    public GeometrySimplifier()
    {
    }

    /// <summary>Simplifies geometry with the selected distance or effective-area algorithm.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="parameters">The algorithm, tolerance, endpoint, and collapse policies.</param>
    /// <returns>An owning simplified geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry, parameters, or algorithm selection is invalid.</exception>
    public Geometry Simplify(
        Geometry geometry, SimplifyParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Simplification parameters are null.");
        }
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        switch (parameters.Algorithm)
        {
            case SimplificationAlgorithm.DouglasPeucker:
                return DouglasPeucker.SimplifyGeometry(
                    projected, parameters);
            case SimplificationAlgorithm.TopologyPreserving:
                return TopologyPreservingSimplifier.SimplifyGeometry(
                    projected, parameters);
            case SimplificationAlgorithm.VisvalingamWhyatt:
                return VisvalingamWhyatt.SimplifyGeometry(
                    projected, parameters);
            case SimplificationAlgorithm.PolygonHull:
                throw new InvalidGeometryException(
                    "Polygon-hull simplification requires SimplifyPolygonHull.");
            default:
                throw new InvalidGeometryException(
                    "Simplification algorithm is not supported.");
        }
    }

    /// <summary>Inserts vertices so no source segment exceeds the requested planar length.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="maximumSegmentLength">The finite positive maximum segment length.</param>
    /// <param name="precisionModel">The precision applied to generated coordinates.</param>
    /// <returns>An owning densified geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or maximum segment length is invalid.</exception>
    /// <exception cref="PrecisionException">The precision model is invalid or collapses a required ring.</exception>
    public Geometry Densify(
        Geometry geometry, double maximumSegmentLength,
        PrecisionModel precisionModel)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return Densifier.DensifyGeometry(
            projected, maximumSegmentLength, precisionModel);
    }

    /// <summary>Constructs a simplified inner or outer polygon hull.</summary>
    /// <param name="polygonal">The polygonal source geometry.</param>
    /// <param name="parameters">Polygon-hull parameters whose tolerance is a vertex-removal ratio.</param>
    /// <param name="outer">Whether to construct an outer rather than inner hull.</param>
    /// <returns>An owning polygonal hull.</returns>
    /// <exception cref="InvalidGeometryException">The polygonal geometry, parameters, or ratio is invalid.</exception>
    public Geometry SimplifyPolygonHull(
        Geometry polygonal, SimplifyParameters parameters,
        bool outer)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(polygonal);
        return PolygonHullSimplifier.SimplifyGeometry(
            projected, parameters, outer);
    }

    /// <summary>Converts every curved component to a linear geometry.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="parameters">The arc subdivision policy.</param>
    /// <returns>An owning geometry containing no curved components.</returns>
    /// <exception cref="InvalidGeometryException">The geometry, curve structure, or subdivision policy is invalid.</exception>
    public Geometry LinearizeCurves(
        Geometry geometry,
        CurveLinearizationParameters parameters)
    {
        return CurveLinearizer.LinearizeGeometry(
            geometry, parameters);
    }

    /// <summary>Simplifies geometry after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="simplifyParameters">The simplification policy.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning simplified linear geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or a parameter value is invalid.</exception>
    public Geometry SimplifyAfterCurveLinearization(
        Geometry geometry,
        SimplifyParameters simplifyParameters,
        CurveLinearizationParameters curveParameters)
    {
        Geometry linearized = CurveOperationLinearizer.LinearizeOne(
            geometry, curveParameters);
        return Simplify(linearized, simplifyParameters);
    }
}
