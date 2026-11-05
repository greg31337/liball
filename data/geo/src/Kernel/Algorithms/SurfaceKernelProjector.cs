namespace geo;

internal static class SurfaceKernelProjector
{
    internal static Geometry ProjectGeometry(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        return ProjectGeometryValue(geometry);
    }

    internal static Geometry[] ProjectPair(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        Geometry[] values = new Geometry[2];
        values[0] = ProjectGeometry(first);
        values[1] = ProjectGeometry(second);
        return values;
    }

    internal static Geometry ProjectGeometryValue(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Triangle:
                ValidateSurfaceGeometry(geometry);
                return ProjectTriangle(
                    (Triangle)payload, options);
            case GeometryType.Tin:
                ValidateSurfaceGeometry(geometry);
                return ProjectTin((Tin)payload, options);
            case GeometryType.PolyhedralSurface:
                ValidateSurfaceGeometry(geometry);
                return ProjectPolyhedralSurface(
                    (PolyhedralSurface)payload, options);
            case GeometryType.GeometryCollection:
                return ProjectCollection(
                    (GeometryCollection)payload, geometry);
            default:
                return geometry;
        }
    }

    internal static void ValidateSurfaceGeometry(Geometry geometry)
    {
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Surface projection requires a valid geometry: " +
                validation.Message);
        }
    }

    internal static Geometry ProjectTriangle(
        Triangle triangle, GeometryFactoryOptions options)
    {
        if (triangle == null || triangle.Shell == null)
        {
            throw new InvalidGeometryException(
                "Triangle payload or shell is null.");
        }
        return GeometryBuilder.CreatePolygonValue(
            triangle.Shell, [], options);
    }

    internal static Geometry ProjectTin(
        Tin tin, GeometryFactoryOptions options)
    {
        if (tin == null || tin.Triangles == null)
        {
            throw new InvalidGeometryException(
                "TIN payload or patch array is null.");
        }
        Polygon[] polygons = new Polygon[tin.Triangles.Length];
        int index = 0;
        while (index < polygons.Length)
        {
            Triangle triangle = tin.Triangles[index];
            if (triangle == null || triangle.Shell == null)
            {
                throw new InvalidGeometryException(
                    "TIN triangle or shell is null.");
            }
            Polygon polygon = new Polygon();
            polygon.Shell = triangle.Shell;
            polygons[index] = polygon;
            index++;
        }
        return GeometryBuilder.CreateMultiPolygonValue(
            polygons, options);
    }

    internal static Geometry ProjectPolyhedralSurface(
        PolyhedralSurface surface, GeometryFactoryOptions options)
    {
        if (surface == null || surface.Patches == null)
        {
            throw new InvalidGeometryException(
                "PolyhedralSurface payload or patch array is null.");
        }
        return GeometryBuilder.CreateMultiPolygonValue(
            surface.Patches, options);
    }

    internal static Geometry ProjectCollection(
        GeometryCollection collection, Geometry metadataSource)
    {
        if (collection == null || collection.Geometries == null)
        {
            throw new InvalidGeometryException(
                "GeometryCollection payload or component array is null.");
        }
        Geometry[] geometries = new Geometry[collection.Geometries.Length];
        int index = 0;
        while (index < geometries.Length)
        {
            geometries[index] = ProjectGeometryValue(
                collection.Geometries[index]);
            index++;
        }
        return GeometryCollections.CreateRawCollection(
            geometries, metadataSource.CoordinateLayout,
            metadataSource.SpatialReferenceId);
    }
}
