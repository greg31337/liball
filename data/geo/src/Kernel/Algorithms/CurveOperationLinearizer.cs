namespace geo;

internal static class CurveOperationLinearizer
{
    internal static Geometry LinearizeOne(
        Geometry geometry,
        CurveLinearizationParameters parameters)
    {
        return CurveLinearizer.LinearizeGeometry(geometry, parameters);
    }

    internal static Geometry[] LinearizePair(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        Geometry[] result = new Geometry[2];
        result[0] = CurveLinearizer.LinearizeGeometry(first, parameters);
        result[1] = CurveLinearizer.LinearizeGeometry(second, parameters);
        return result;
    }
}
