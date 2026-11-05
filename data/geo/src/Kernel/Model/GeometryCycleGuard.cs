using System.Collections.Generic;

namespace geo;

internal class GeometryCycleState
{
    internal HashSet<Geometry> Active = new HashSet<Geometry>();
    internal int Count;
    internal int MaximumDepth;
    internal int MaximumComponents;
}

internal static class GeometryCycleGuard
{
    internal static void EnsureAcyclic(Geometry geometry, int maximumDepth, int maximumComponents)
    {
        if (maximumDepth < 0)
        {
            throw new InvalidGeometryException("Maximum geometry depth cannot be negative.");
        }

        if (maximumComponents < 1)
        {
            throw new InvalidGeometryException("Maximum geometry component count must be positive.");
        }

        GeometryCycleState state = new GeometryCycleState();
        state.MaximumDepth = maximumDepth;
        state.MaximumComponents = maximumComponents;
        VisitGeometry(geometry, 0, state);
    }

    internal static void VisitGeometry(Geometry geometry, int depth, GeometryCycleState state)
    {
        if (geometry == null)
        {
            throw new InvalidGeometryException("Geometry component is null.");
        }

        if (depth > state.MaximumDepth)
        {
            throw new InvalidGeometryException("Maximum geometry nesting depth was exceeded.");
        }

        state.Count++;
        if (state.Count > state.MaximumComponents)
        {
            throw new InvalidGeometryException("Maximum geometry component count was exceeded.");
        }

        if (!state.Active.Add(geometry))
        {
            throw new InvalidGeometryException("Geometry contains a cyclic component reference.");
        }

        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.GeometryCollection:
                VisitGeometryArray(((GeometryCollection)payload).Geometries, depth + 1, state);
                break;
            case GeometryType.CompoundCurve:
                VisitGeometryArray(((CompoundCurve)payload).Curves, depth + 1, state);
                break;
            case GeometryType.CurvePolygon:
                VisitCurvePolygon((CurvePolygon)payload, depth + 1, state);
                break;
            case GeometryType.MultiCurve:
                VisitGeometryArray(((MultiCurve)payload).Curves, depth + 1, state);
                break;
            case GeometryType.MultiSurface:
                VisitGeometryArray(((MultiSurface)payload).Surfaces, depth + 1, state);
                break;
            default:
                break;
        }

        state.Active.Remove(geometry);
    }

    internal static void VisitGeometryArray(Geometry[] geometries, int depth, GeometryCycleState state)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        foreach (Geometry geometry in geometries)
        {
            VisitGeometry(geometry, depth, state);
        }
    }

    internal static void VisitCurvePolygon(CurvePolygon polygon, int depth, GeometryCycleState state)
    {
        if (polygon.Shell != null)
        {
            VisitGeometry(polygon.Shell, depth, state);
        }

        VisitGeometryArray(polygon.Holes, depth, state);
    }
}
