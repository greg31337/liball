using System.Collections.Generic;

namespace geo;

internal static class GeometryCollections
{
    internal static Geometry FlattenGeometryValue(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> nonEmpty = new List<Geometry>();
        int index = 0;
        while (index < leaves.Length)
        {
            if (!GeometryInspection.IsEmptyValue(leaves[index]))
            {
                nonEmpty.Add(GeometryCopy.CopyGeometry(leaves[index]));
            }
            index++;
        }

        return CreateRawCollection(nonEmpty.ToArray(), geometry.CoordinateLayout, geometry.SpatialReferenceId);
    }

    internal static Geometry CombineGeometryValues(Geometry[] geometries, GeometryFactoryOptions emptyOptions)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry array is null.");
        }

        GeometryBuilder.ValidateComponentCount(geometries.Length);

        if (geometries.Length == 0)
        {
            GeometryFactoryOptions options = GeometryBuilder.CopyAndValidateOptions(emptyOptions);
            return GeometryBuilder.CreateEmptyValue(GeometryType.GeometryCollection, options);
        }

        GeometryInspection.EnsureGeometryGraph(geometries[0]);
        CoordinateLayout coordinateLayout = geometries[0].CoordinateLayout;
        int spatialReferenceId = geometries[0].SpatialReferenceId;
        Geometry[] copies = new Geometry[geometries.Length];
        bool allCore = true;
        bool allCurves = true;
        bool allSurfaces = true;
        int index = 0;
        while (index < geometries.Length)
        {
            GeometryInspection.EnsureGeometryGraph(geometries[index]);
            if (geometries[index].CoordinateLayout != coordinateLayout || geometries[index].SpatialReferenceId != spatialReferenceId)
            {
                throw new InvalidGeometryException("Combined geometries must share coordinate layout and spatial reference metadata.");
            }
            if (!GeometryNormalization.IsCoreNormalizationType(geometries[index].GeometryType)) allCore = false;
            if (!StructuralValidator.IsCurveType(
                geometries[index].GeometryType)) allCurves = false;
            if (!StructuralValidator.IsSurfaceType(
                geometries[index].GeometryType)) allSurfaces = false;
            copies[index] = GeometryCopy.CopyGeometry(geometries[index]);
            index++;
        }

        if (copies.Length == 1)
        {
            return copies[0];
        }

        if (allCore)
        {
            Geometry optionsSource = geometries[0];
            return FactoryConstructions.CreateNarrowestCollectionValue(copies, GeometryNormalization.CreateNormalizationOptions(optionsSource));
        }

        GeometryFactoryOptions resultOptions =
            GeometryNormalization.CreateNormalizationOptions(
                geometries[0]);
        if (allCurves)
        {
            return ExtendedGeometryBuilder.CreateMultiCurveValue(
                copies, resultOptions);
        }
        if (allSurfaces)
        {
            return ExtendedGeometryBuilder.CreateMultiSurfaceValue(
                copies, resultOptions);
        }

        return CreateRawCollection(copies, coordinateLayout, spatialReferenceId);
    }

    internal static Geometry ExtractGeometryTypeValue(Geometry geometry, GeometryType geometryType)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        GeometryBuilder.CreateEmptyValue(geometryType, GeometryNormalization.CreateNormalizationOptions(geometry));
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> matches = new List<Geometry>();
        int index = 0;
        while (index < leaves.Length)
        {
            if (leaves[index].GeometryType == geometryType && !GeometryInspection.IsEmptyValue(leaves[index]))
            {
                matches.Add(GeometryCopy.CopyGeometry(leaves[index]));
            }
            index++;
        }

        GeometryFactoryOptions options = GeometryNormalization.CreateNormalizationOptions(geometry);
        if (matches.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(geometryType, options);
        }

        return CombineGeometryValues(matches.ToArray(), options);
    }

    internal static Geometry ExtractTopologicalDimensionValue(Geometry geometry, TopologicalDimension dimension)
    {
        ValidateTopologicalDimension(dimension);
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> matches = new List<Geometry>();
        int index = 0;
        while (index < leaves.Length)
        {
            TopologicalDimension leafDimension = GeometryInspection.GetTopologicalDimensionValue(leaves[index]);
            if (leafDimension == dimension)
            {
                matches.Add(GeometryCopy.CopyGeometry(leaves[index]));
            }
            index++;
        }

        GeometryFactoryOptions options = GeometryNormalization.CreateNormalizationOptions(geometry);
        if (matches.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(GeometryType.GeometryCollection, options);
        }

        return CombineGeometryValues(matches.ToArray(), options);
    }

    internal static Geometry CreateRawCollection(Geometry[] geometries, CoordinateLayout coordinateLayout, int spatialReferenceId)
    {
        GeometryBuilder.ValidateComponentCount(geometries.Length);
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.GeometryCollection;
        geometry.CoordinateLayout = coordinateLayout;
        geometry.SpatialReferenceId = spatialReferenceId;
        GeometryCollection payload = new GeometryCollection();
        payload.Geometries = geometries;
        geometry.GeometryCollection = payload;
        return geometry;
    }

    internal static void ValidateTopologicalDimension(TopologicalDimension dimension)
    {
        switch (dimension)
        {
            case TopologicalDimension.Empty:
            case TopologicalDimension.Point:
            case TopologicalDimension.Curve:
            case TopologicalDimension.Surface:
                return;
            default:
                throw new InvalidGeometryException("Topological dimension is not supported.");
        }
    }
}
