using System;
using System.Collections.Generic;

namespace geo;

internal static class OffsetCurveSetBuilder
{
    internal static Geometry[] CreateBufferSurfaces(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> surfaces = new List<Geometry>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (!GeometryInspection.IsEmptyValue(leaf))
            {
                Geometry surface = CreateComponentBufferSurface(
                    leaf, distance, parameters);
                if (!GeometryInspection.IsEmptyValue(surface))
                {
                    surfaces.Add(surface);
                }
            }
            index++;
        }
        return surfaces.ToArray();
    }

    internal static Geometry CreateComponentBufferSurface(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                if (distance <= 0.0)
                {
                    return CreateEmptySurface(options);
                }
                return BufferCurveBuilder.CreatePointBufferSurface(
                    (Point)payload, distance, parameters, options);
            case GeometryType.LineString:
                if (distance <= 0.0 && !parameters.SingleSided)
                {
                    return CreateEmptySurface(options);
                }
                if (distance == 0.0)
                {
                    return CreateEmptySurface(options);
                }
                return NormalizeRawSurface(
                    BufferCurveBuilder.CreateLineBufferSurface(
                        (LineString)payload, distance,
                        parameters, options));
            case GeometryType.LinearRing:
                if (distance <= 0.0 && !parameters.SingleSided)
                {
                    return CreateEmptySurface(options);
                }
                if (distance == 0.0)
                {
                    return CreateEmptySurface(options);
                }
                LinearRing ring = (LinearRing)payload;
                LineString ringLine = new LineString();
                ringLine.Coordinates = ring.Coordinates;
                return NormalizeRawSurface(
                    BufferCurveBuilder.CreateLineBufferSurface(
                        ringLine, distance, parameters, options));
            case GeometryType.Polygon:
                return CreatePolygonBufferSurface(
                    (Polygon)payload,
                    distance, parameters, options);
            default:
                throw new InvalidGeometryException(
                    "Buffer construction supports core linear geometry types.");
        }
    }

    internal static Geometry CreatePolygonBufferSurface(
        Polygon polygon, double distance,
        BufferParameters parameters, GeometryFactoryOptions options)
    {
        if (distance == 0.0)
        {
            Geometry empty = CreateEmptySurface(options);
            Geometry source = GeometryBuilder.CreatePolygonValue(
                polygon.Shell, polygon.Holes, options);
            return UnionSurfaces(source, empty, parameters.Precision);
        }
        BufferParameters ringParameters =
            BufferCurveBuilder.CopyParameters(parameters, false);
        Coordinate[] shell = BufferCurveBuilder.CreateAreaOffsetRing(
            polygon.Shell.Coordinates, false,
            distance, ringParameters);
        if (shell.Length < 4)
        {
            return CreateEmptySurface(options);
        }
        List<LinearRing> holes = new List<LinearRing>();
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            Coordinate[] hole = BufferCurveBuilder.CreateAreaOffsetRing(
                polygon.Holes[index].Coordinates, true,
                distance, ringParameters);
            if (hole.Length >= 4)
            {
                if (RingContainsAllCoordinates(hole, shell))
                {
                    return CreateEmptySurface(options);
                }
                holes.Add(BufferCurveBuilder.CreateRingPayload(hole));
            }
            index++;
        }
        Geometry result = BufferCurveBuilder.CreatePolygonSurface(
            shell, holes.ToArray(), options);
        return GeometryNormalization.NormalizeGeometryValue(result);
    }

    internal static bool RingContainsAllCoordinates(
        Coordinate[] container, Coordinate[] candidate)
    {
        int index = 0;
        while (index < candidate.Length)
        {
            if (RingPointLocation.LocatePointByWinding(
                candidate[index], container) ==
                    TopologicalLocation.Exterior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static Geometry NormalizeRawSurface(Geometry raw)
    {
        SegmentString[] edges = GeometrySplitter.CreateSplitterEdges(
            new RelationGeometryParts(raw));
        if (edges.Length != 0 &&
            !FullyNodedValidator.ValidateOutput(edges).IsValid)
        {
            return BufferPolygonizer.NodeAndPolygonizeSurface(raw);
        }
        return GeometryNormalization.NormalizeGeometryValue(raw);
    }

    internal static Geometry UnionSurfaceArray(
        Geometry[] surfaces, Geometry metadataSource,
        PrecisionModel precision)
    {
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(
                metadataSource);
        Geometry result = CreateEmptySurface(options);
        int index = 0;
        while (index < surfaces.Length)
        {
            result = UnionSurfaces(
                result, surfaces[index], precision);
            index++;
        }
        return result;
    }

    internal static Geometry UnionSurfaces(
        Geometry first, Geometry second, PrecisionModel precision)
    {
        if (GeometryInspection.IsEmptyValue(first))
        {
            return GeometryNormalization.NormalizeGeometryValue(second);
        }
        if (GeometryInspection.IsEmptyValue(second))
        {
            return GeometryNormalization.NormalizeGeometryValue(first);
        }
        if (precision.Type == PrecisionModelType.Fixed)
        {
            return OverlayEngine.GetUnionWithPrecision(
                first, second, precision);
        }
        return OverlayEngine.GetUnion(first, second);
    }

    internal static Geometry DifferenceSurfaces(
        Geometry first, Geometry second, PrecisionModel precision)
    {
        if (precision.Type == PrecisionModelType.Fixed)
        {
            return OverlayEngine.GetDifferenceWithPrecision(
                first, second, precision);
        }
        return OverlayEngine.GetDifference(first, second);
    }

    internal static Geometry CreateEmptySurface(
        GeometryFactoryOptions options)
    {
        return GeometryBuilder.CreateEmptyValue(
            GeometryType.Polygon, options);
    }
}
