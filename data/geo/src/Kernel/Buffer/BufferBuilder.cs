using System;

namespace geo;

internal static class BufferBuilder
{
    internal static Geometry GetBuffer(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        if (!double.IsFinite(distance))
        {
            throw new InvalidGeometryException(
                "Buffer distance must be finite.");
        }
        OffsetSegmentGenerator.ValidateBufferParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            throw new InvalidGeometryException(
                "Buffer construction supports core linear geometry types.");
        }
        Geometry[] surfaces =
            OffsetCurveSetBuilder.CreateBufferSurfaces(
                geometry, distance, parameters);
        return OffsetCurveSetBuilder.UnionSurfaceArray(
            surfaces, geometry, parameters.Precision);
    }
}
