using System.Collections.Generic;

namespace geo;

internal static class GeometryInspection
{
    internal static GeometryType GetGeometryTypeValue(Geometry geometry)
    {
        ValidateRootMetadata(geometry);
        return geometry.GeometryType;
    }

    internal static TopologicalDimension GetTopologicalDimensionValue(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        if (ExtractValidatedCoordinates(geometry).Length == 0)
        {
            return TopologicalDimension.Empty;
        }

        if (geometry.GeometryType != GeometryType.GeometryCollection)
        {
            return GeometryDispatch.GetDeclaredDimension(geometry);
        }

        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        TopologicalDimension dimension = TopologicalDimension.Empty;
        int index = 0;
        while (index < children.Length)
        {
            TopologicalDimension childDimension = GetTopologicalDimensionValue(children[index]);
            if ((int)childDimension > (int)dimension)
            {
                dimension = childDimension;
            }

            index++;
        }

        return dimension;
    }

    internal static CoordinateLayout GetCoordinateLayoutValue(Geometry geometry)
    {
        ValidateRootMetadata(geometry);
        return geometry.CoordinateLayout;
    }

    internal static int GetSpatialReferenceIdValue(Geometry geometry)
    {
        ValidateRootMetadata(geometry);
        return geometry.SpatialReferenceId;
    }

    internal static bool IsEmptyValue(Geometry geometry)
    {
        return ExtractValidatedCoordinates(geometry).Length == 0;
    }

    internal static bool IsClosedValue(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        if (coordinates.Length == 0)
        {
            return false;
        }

        switch (geometry.GeometryType)
        {
            case GeometryType.LineString:
            case GeometryType.LinearRing:
                return CoordinatesAreClosed(coordinates);
            case GeometryType.CircularString:
                CircularStringAlgorithms.ValidateValue(
                    (CircularString)
                        GeometryDispatch.GetSelectedPayload(geometry));
                return CoordinatesAreClosed(coordinates);
            case GeometryType.CompoundCurve:
                return ExtendedGeometryValidityChecker.
                    IsCompoundCurveClosed(
                        (CompoundCurve)
                            GeometryDispatch.GetSelectedPayload(geometry));
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
            case GeometryType.Triangle:
            case GeometryType.CurvePolygon:
                return true;
            case GeometryType.Tin:
                return SurfaceTopologyValidator.IsTinClosed(
                    (Tin)GeometryDispatch.GetSelectedPayload(geometry));
            case GeometryType.PolyhedralSurface:
                return SurfaceTopologyValidator.IsPolyhedralClosed(
                    (PolyhedralSurface)
                        GeometryDispatch.GetSelectedPayload(geometry));
            case GeometryType.MultiLineString:
            case GeometryType.GeometryCollection:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
                return AllDirectChildrenAreClosed(geometry);
            default:
                return false;
        }
    }

    internal static bool IsRingValue(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                break;
            default:
                return false;
        }

        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        if (coordinates.Length < 4 || !CoordinatesAreClosed(coordinates))
        {
            return false;
        }

        if (geometry.GeometryType == GeometryType.CompoundCurve &&
            !ExtendedGeometryValidityChecker.IsCompoundCurveClosed(
                (CompoundCurve)
                    GeometryDispatch.GetSelectedPayload(geometry)))
        {
            return false;
        }

        return HasThreeDistinctPlanarCoordinates(coordinates);
    }

    internal static bool IsRectangleValue(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        if (geometry.GeometryType != GeometryType.Polygon)
        {
            return false;
        }

        Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(geometry);
        if (polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon shell or hole array is null.");
        }

        if (polygon.Holes.Length != 0 || polygon.Shell.Coordinates == null || polygon.Shell.Coordinates.Length != 5)
        {
            return false;
        }

        Coordinate[] coordinates = polygon.Shell.Coordinates;
        int index = 0;
        while (index < coordinates.Length)
        {
            GeometryBuilder.ValidateCoordinateForLayout(coordinates[index], geometry.CoordinateLayout);
            index++;
        }

        if (!CoordinatesAreClosed(coordinates))
        {
            return false;
        }

        double minimumX = coordinates[0].X;
        double minimumY = coordinates[0].Y;
        double maximumX = coordinates[0].X;
        double maximumY = coordinates[0].Y;
        index = 1;
        while (index < 4)
        {
            Coordinate coordinate = coordinates[index];
            if (coordinate.X < minimumX) minimumX = coordinate.X;
            if (coordinate.Y < minimumY) minimumY = coordinate.Y;
            if (coordinate.X > maximumX) maximumX = coordinate.X;
            if (coordinate.Y > maximumY) maximumY = coordinate.Y;
            index++;
        }

        if (minimumX == maximumX || minimumY == maximumY)
        {
            return false;
        }

        int cornerMask = 0;
        index = 0;
        while (index < 4)
        {
            Coordinate current = coordinates[index];
            Coordinate next = coordinates[index + 1];
            bool horizontal = current.Y == next.Y && current.X != next.X;
            bool vertical = current.X == next.X && current.Y != next.Y;
            if (!horizontal && !vertical)
            {
                return false;
            }

            int corner = GetRectangleCornerBit(current, minimumX, minimumY, maximumX, maximumY);
            if (corner == 0 || (cornerMask & corner) != 0)
            {
                return false;
            }

            cornerMask = cornerMask | corner;
            index++;
        }

        return cornerMask == 15;
    }

    internal static int CountCoordinateValues(Geometry geometry)
    {
        return ExtractValidatedCoordinates(geometry).Length;
    }

    internal static int CountComponentValues(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        if (!IsContainerGeometryType(geometry.GeometryType))
        {
            return 1;
        }

        return ComponentWalker.GetDirectChildren(geometry).Length;
    }

    internal static Coordinate GetPointValueAt(Geometry geometry, int pointIndex)
    {
        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        if (pointIndex < 0 || pointIndex >= coordinates.Length)
        {
            throw new InvalidGeometryException("Coordinate index is outside the geometry coordinate sequence.");
        }

        return CoordinateSequences.CopyCoordinateValue(coordinates[pointIndex]);
    }

    internal static Coordinate? GetStartPointValue(Geometry geometry)
    {
        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        if (coordinates.Length == 0)
        {
            return null;
        }

        return CoordinateSequences.CopyCoordinateValue(coordinates[0]);
    }

    internal static Coordinate? GetEndPointValue(Geometry geometry)
    {
        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        if (coordinates.Length == 0)
        {
            return null;
        }

        return CoordinateSequences.CopyCoordinateValue(coordinates[coordinates.Length - 1]);
    }

    internal static Geometry GetComponentValueAt(Geometry geometry, int componentIndex)
    {
        EnsureGeometryGraph(geometry);
        if (!IsContainerGeometryType(geometry.GeometryType))
        {
            if (componentIndex != 0)
            {
                throw new InvalidGeometryException("Component index is outside the geometry component sequence.");
            }

            return GeometryCopy.CopyGeometry(geometry);
        }

        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        if (componentIndex < 0 || componentIndex >= children.Length)
        {
            throw new InvalidGeometryException("Component index is outside the geometry component sequence.");
        }

        return GeometryCopy.CopyGeometry(children[componentIndex]);
    }

    internal static Coordinate[] ExtractCoordinateCopies(Geometry geometry)
    {
        return CoordinateSequences.CopyCoordinateSequence(ExtractValidatedCoordinates(geometry));
    }

    internal static Coordinate[] ExtractUniqueCoordinateCopies(Geometry geometry)
    {
        Coordinate[] coordinates = ExtractValidatedCoordinates(geometry);
        List<Coordinate> unique = new List<Coordinate>();
        HashSet<string> identities = new HashSet<string>();
        int index = 0;
        while (index < coordinates.Length)
        {
            string identity = GetCoordinateIdentity(coordinates[index]);
            if (identities.Add(identity))
            {
                unique.Add(CoordinateSequences.CopyCoordinateValue(coordinates[index]));
            }

            index++;
        }

        return unique.ToArray();
    }

    internal static Coordinate[] ExtractValidatedCoordinates(Geometry geometry)
    {
        EnsureGeometryGraph(geometry);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < coordinates.Length)
        {
            GeometryBuilder.ValidateCoordinateForLayout(coordinates[index], geometry.CoordinateLayout);
            index++;
        }

        return coordinates;
    }

    internal static void EnsureGeometryGraph(Geometry geometry)
    {
        ValidateRootMetadata(geometry);
        GeometryCycleGuard.EnsureAcyclic(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        ValidateMetadataTree(geometry, geometry.CoordinateLayout, geometry.SpatialReferenceId);
    }

    internal static void ValidateRootMetadata(Geometry geometry)
    {
        GeometryDispatch.GetSelectedPayload(geometry);
        GeometryBuilder.ValidateCoordinateLayout(geometry.CoordinateLayout);
        if (geometry.SpatialReferenceId < GeometryConstants.UnknownSpatialReferenceId)
        {
            throw new InvalidGeometryException("Spatial reference identifier cannot be negative.");
        }
    }

    internal static void ValidateMetadataTree(Geometry geometry, CoordinateLayout coordinateLayout, int spatialReferenceId)
    {
        GeometryDispatch.GetSelectedPayload(geometry);
        if (geometry.CoordinateLayout != coordinateLayout || geometry.SpatialReferenceId != spatialReferenceId)
        {
            throw new InvalidGeometryException("Geometry components must share coordinate layout and spatial reference metadata.");
        }

        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            ValidateMetadataTree(children[index], coordinateLayout, spatialReferenceId);
            index++;
        }
    }

    internal static bool CoordinatesAreClosed(Coordinate[] coordinates)
    {
        if (coordinates.Length == 0)
        {
            return false;
        }

        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Length - 1];
        return first.X == last.X && first.Y == last.Y;
    }

    internal static bool HasThreeDistinctPlanarCoordinates(Coordinate[] coordinates)
    {
        Coordinate first = coordinates[0];
        Coordinate? second = null;
        int index = 1;
        while (index < coordinates.Length - 1)
        {
            Coordinate current = coordinates[index];
            if (current.X != first.X || current.Y != first.Y)
            {
                if (second == null)
                {
                    second = current;
                }
                else if (current.X != second.X || current.Y != second.Y)
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    internal static bool AllDirectChildrenAreClosed(Geometry geometry)
    {
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        if (children.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < children.Length)
        {
            if (!IsClosedValue(children[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsContainerGeometryType(GeometryType geometryType)
    {
        switch (geometryType)
        {
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
            case GeometryType.Tin:
            case GeometryType.PolyhedralSurface:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
                return true;
            default:
                return false;
        }
    }

    internal static int GetRectangleCornerBit(Coordinate coordinate, double minimumX, double minimumY, double maximumX, double maximumY)
    {
        if (coordinate.X == minimumX && coordinate.Y == minimumY) return 1;
        if (coordinate.X == maximumX && coordinate.Y == minimumY) return 2;
        if (coordinate.X == maximumX && coordinate.Y == maximumY) return 4;
        if (coordinate.X == minimumX && coordinate.Y == maximumY) return 8;
        return 0;
    }

    internal static string GetCoordinateIdentity(Coordinate coordinate)
    {
        return $"{GetOrdinateIdentity(coordinate.X)}:{GetOrdinateIdentity(coordinate.Y)}:{GetOrdinateIdentity(coordinate.Z)}:{GetOrdinateIdentity(coordinate.M)}";
    }

    internal static string GetOrdinateIdentity(double ordinate)
    {
        if (double.IsNaN(ordinate))
        {
            return "N";
        }

        if (ordinate == 0.0)
        {
            return "0";
        }

        return System.BitConverter.DoubleToInt64Bits(ordinate).ToString();
    }
}
