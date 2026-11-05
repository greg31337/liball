using System.Collections.Generic;

namespace geo;

internal static class ComponentWalker
{
    internal static Geometry[] GetDirectChildren(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        List<Geometry> children = new List<Geometry>();
        switch (geometry.GeometryType)
        {
            case GeometryType.MultiPoint:
                MultiPoint multiPoint = (MultiPoint)payload;
                if (multiPoint.Points == null)
                {
                    throw new InvalidGeometryException("MultiPoint component array is null.");
                }
                foreach (Point point in multiPoint.Points)
                {
                    children.Add(CreatePayloadGeometry(geometry, GeometryType.Point, point));
                }
                break;
            case GeometryType.MultiLineString:
                MultiLineString multiLineString = (MultiLineString)payload;
                if (multiLineString.LineStrings == null)
                {
                    throw new InvalidGeometryException("MultiLineString component array is null.");
                }
                foreach (LineString line in multiLineString.LineStrings)
                {
                    children.Add(CreatePayloadGeometry(geometry, GeometryType.LineString, line));
                }
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                if (multiPolygon.Polygons == null)
                {
                    throw new InvalidGeometryException("MultiPolygon component array is null.");
                }
                foreach (Polygon polygon in multiPolygon.Polygons)
                {
                    children.Add(CreatePayloadGeometry(geometry, GeometryType.Polygon, polygon));
                }
                break;
            case GeometryType.GeometryCollection:
                AddGeometryChildren(((GeometryCollection)payload).Geometries, children);
                break;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                if (tin.Triangles == null)
                {
                    throw new InvalidGeometryException("TIN patch array is null.");
                }
                foreach (Triangle triangle in tin.Triangles)
                {
                    children.Add(CreatePayloadGeometry(geometry, GeometryType.Triangle, triangle));
                }
                break;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface polyhedralSurface = (PolyhedralSurface)payload;
                if (polyhedralSurface.Patches == null)
                {
                    throw new InvalidGeometryException("PolyhedralSurface patch array is null.");
                }
                foreach (Polygon patch in polyhedralSurface.Patches)
                {
                    children.Add(CreatePayloadGeometry(geometry, GeometryType.Polygon, patch));
                }
                break;
            case GeometryType.CompoundCurve:
                AddGeometryChildren(((CompoundCurve)payload).Curves, children);
                break;
            case GeometryType.CurvePolygon:
                AddCurvePolygonChildren((CurvePolygon)payload, children);
                break;
            case GeometryType.MultiCurve:
                AddGeometryChildren(((MultiCurve)payload).Curves, children);
                break;
            case GeometryType.MultiSurface:
                AddGeometryChildren(((MultiSurface)payload).Surfaces, children);
                break;
            default:
                break;
        }

        return children.ToArray();
    }

    internal static Geometry[] FlattenLeafGeometries(Geometry geometry, int maximumDepth, int maximumComponents)
    {
        GeometryCycleGuard.EnsureAcyclic(geometry, maximumDepth, maximumComponents);
        List<Geometry> leaves = new List<Geometry>();
        WalkLeaves(geometry, 0, maximumDepth, maximumComponents, leaves);
        return leaves.ToArray();
    }

    internal static void WalkLeaves(Geometry geometry, int depth, int maximumDepth, int maximumComponents, List<Geometry> leaves)
    {
        if (depth > maximumDepth)
        {
            throw new InvalidGeometryException("Maximum geometry nesting depth was exceeded.");
        }

        Geometry[] children = GetDirectChildren(geometry);
        if (children.Length == 0)
        {
            if (leaves.Count >= maximumComponents)
            {
                throw new InvalidGeometryException("Maximum geometry component count was exceeded.");
            }

            leaves.Add(geometry);
            return;
        }

        foreach (Geometry child in children)
        {
            WalkLeaves(child, depth + 1, maximumDepth, maximumComponents, leaves);
        }
    }

    internal static Geometry CreatePayloadGeometry(Geometry parent, GeometryType type, object payload)
    {
        if (payload == null)
        {
            throw new InvalidGeometryException("Geometry payload component is null.");
        }

        Geometry child = new Geometry();
        child.Point = null;
        child.GeometryType = type;
        child.CoordinateLayout = parent.CoordinateLayout;
        child.SpatialReferenceId = parent.SpatialReferenceId;
        switch (type)
        {
            case GeometryType.Point:
                child.Point = (Point)payload;
                break;
            case GeometryType.LineString:
                child.LineString = (LineString)payload;
                break;
            case GeometryType.Polygon:
                child.Polygon = (Polygon)payload;
                break;
            case GeometryType.Triangle:
                child.Triangle = (Triangle)payload;
                break;
            default:
                throw new InvalidGeometryException("Payload cannot be wrapped as a direct component.");
        }

        return child;
    }

    internal static void AddGeometryChildren(Geometry[] source, List<Geometry> destination)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        foreach (Geometry geometry in source)
        {
            if (geometry == null)
            {
                throw new InvalidGeometryException("Geometry component is null.");
            }

            destination.Add(geometry);
        }
    }

    internal static void AddCurvePolygonChildren(CurvePolygon polygon, List<Geometry> destination)
    {
        if (polygon.Shell != null)
        {
            destination.Add(polygon.Shell);
        }

        AddGeometryChildren(polygon.Holes, destination);
    }
}
