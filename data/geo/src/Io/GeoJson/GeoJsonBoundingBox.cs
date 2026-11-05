using System;
using System.Collections.Generic;

namespace geo;

internal class GeoJsonBoundingBoxState
{
    internal List<double> Longitudes = new List<double>();
    internal double MinimumLatitude = double.PositiveInfinity;
    internal double MaximumLatitude = double.NegativeInfinity;
    internal double MinimumElevation = double.PositiveInfinity;
    internal double MaximumElevation = double.NegativeInfinity;
    internal bool HasCoordinate;
    internal bool AllCoordinatesHaveElevation = true;
    internal bool HasFullLongitudeSpan;
}

internal static class GeoJsonBoundingBox
{
    internal static double[] ComputeBoundingBox(
        GeoJsonDocument document)
    {
        GeoJsonBoundingBoxState state =
            new GeoJsonBoundingBoxState();
        AccumulateDocumentCoordinates(document, state);
        if (!state.HasCoordinate) return [];

        double west = -180.0;
        double east = 180.0;
        if (!state.HasFullLongitudeSpan)
        {
            state.Longitudes.Sort();
            west = state.Longitudes[0];
            east = state.Longitudes[state.Longitudes.Count - 1];
            double largestGap = west + 360.0 - east;
            int index = 0;
            while (index + 1 < state.Longitudes.Count)
            {
                double gap = state.Longitudes[index + 1] -
                    state.Longitudes[index];
                if (gap > largestGap)
                {
                    largestGap = gap;
                    west = state.Longitudes[index + 1];
                    east = state.Longitudes[index];
                }
                index++;
            }
        }

        if (state.AllCoordinatesHaveElevation)
        {
            return
            [
                west,
                state.MinimumLatitude,
                state.MinimumElevation,
                east,
                state.MaximumLatitude,
                state.MaximumElevation
            ];
        }
        return
        [
            west,
            state.MinimumLatitude,
            east,
            state.MaximumLatitude
        ];
    }

    internal static void AccumulateDocumentCoordinates(
        GeoJsonDocument document, GeoJsonBoundingBoxState state)
    {
        ValidateDocumentSelection(document);
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                AccumulateGeometryCoordinates(document.Geometry, state);
                return;
            case GeoJsonObjectType.Feature:
                AccumulateFeatureCoordinates(document.Feature, state);
                return;
            case GeoJsonObjectType.FeatureCollection:
                AccumulateCollectionCoordinates(
                    document.FeatureCollection, state);
                return;
            default:
                throw new GeoJsonException(
                    "GeoJSON document object type is invalid.");
        }
    }

    internal static void ValidateDocumentSelection(
        GeoJsonDocument document)
    {
        if (document == null)
        {
            throw new GeoJsonException("GeoJSON document is null.");
        }
        int count = 0;
        if (document.Geometry != null) count++;
        if (document.Feature != null) count++;
        if (document.FeatureCollection != null) count++;
        if (count != 1)
        {
            throw new GeoJsonException(
                "GeoJSON document must contain exactly one root value.");
        }
        if ((document.ObjectType == GeoJsonObjectType.Geometry &&
                document.Geometry == null) ||
            (document.ObjectType == GeoJsonObjectType.Feature &&
                document.Feature == null) ||
            (document.ObjectType == GeoJsonObjectType.FeatureCollection &&
                document.FeatureCollection == null))
        {
            throw new GeoJsonException(
                "GeoJSON document type does not match its root value.");
        }
    }

    internal static void AccumulateFeatureCoordinates(
        GeoJsonFeature? feature, GeoJsonBoundingBoxState state)
    {
        if (feature == null)
        {
            throw new GeoJsonException("GeoJSON Feature is null.");
        }
        if (feature.Geometry != null)
        {
            AccumulateGeometryCoordinates(feature.Geometry, state);
        }
    }

    internal static void AccumulateCollectionCoordinates(
        GeoJsonFeatureCollection? collection,
        GeoJsonBoundingBoxState state)
    {
        if (collection == null || collection.Features == null)
        {
            throw new GeoJsonException(
                "GeoJSON FeatureCollection or feature array is null.");
        }
        int index = 0;
        while (index < collection.Features.Length)
        {
            AccumulateFeatureCoordinates(
                collection.Features[index], state);
            index++;
        }
    }

    internal static void AccumulateGeometryCoordinates(
        GeoJsonGeometry? geometry, GeoJsonBoundingBoxState state)
    {
        if (geometry == null || geometry.Value == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata or value is null.");
        }
        if (geometry.GeometryType != geometry.Value.GeometryType)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata type does not match its value.");
        }
        ValidateGeoJsonGeometryType(geometry.Value.GeometryType);
        UpdatePoleCoverage(geometry.Value, state);
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(
                geometry.Value);
        bool hasElevation =
            geometry.Value.CoordinateLayout == CoordinateLayout.XYZ;
        if (!hasElevation &&
            geometry.Value.CoordinateLayout != CoordinateLayout.XY)
        {
            throw new GeoJsonException(
                "GeoJSON geometry must use XY or XYZ coordinates.");
        }

        int index = 0;
        while (index < coordinates.Length)
        {
            AddCoordinate(coordinates[index], hasElevation, state);
            index++;
        }
    }

    internal static void UpdatePoleCoverage(
        Geometry geometry, GeoJsonBoundingBoxState state)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            UpdatePolygonPoleCoverage((Polygon)payload, state);
            return;
        }
        if (geometry.GeometryType == GeometryType.MultiPolygon)
        {
            MultiPolygon multiPolygon = (MultiPolygon)payload;
            int polygonIndex = 0;
            while (polygonIndex < multiPolygon.Polygons.Length)
            {
                UpdatePolygonPoleCoverage(
                    multiPolygon.Polygons[polygonIndex], state);
                polygonIndex++;
            }
            return;
        }
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            GeometryCollection collection = (GeometryCollection)payload;
            int geometryIndex = 0;
            while (geometryIndex < collection.Geometries.Length)
            {
                UpdatePoleCoverage(
                    collection.Geometries[geometryIndex], state);
                geometryIndex++;
            }
        }
    }

    internal static void UpdatePolygonPoleCoverage(
        Polygon polygon, GeoJsonBoundingBoxState state)
    {
        int pole = GetEnclosedPole(polygon.Shell.Coordinates);
        if (pole == 0) return;
        state.HasFullLongitudeSpan = true;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            if (GetEnclosedPole(polygon.Holes[index].Coordinates) == pole)
            {
                return;
            }
            index++;
        }
        if (pole > 0)
        {
            state.MaximumLatitude = 90.0;
        }
        else
        {
            state.MinimumLatitude = -90.0;
        }
    }

    internal static int GetEnclosedPole(Coordinate[] coordinates)
    {
        if (coordinates.Length < 4) return 0;
        CompensatedMeasureSum longitudeSum =
            new CompensatedMeasureSum();
        CompensatedMeasureSum latitudeSum =
            new CompensatedMeasureSum();
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            GeographicMath.ValidateGeographicCoordinate(
                coordinates[index]);
            GeographicMath.ValidateGeographicCoordinate(
                coordinates[index + 1]);
            longitudeSum.AddValue(
                GeographicMath.GetLongitudeDeltaRadians(
                    coordinates[index], coordinates[index + 1]));
            latitudeSum.AddValue(coordinates[index].Y);
            index++;
        }
        if (Math.Abs(longitudeSum.GetValue()) <= Math.PI) return 0;
        return latitudeSum.GetValue() < 0.0 ? -1 : 1;
    }

    internal static void ValidateGeoJsonGeometryType(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return;
            default:
                throw new GeoJsonException(
                    "Geometry type cannot be represented by GeoJSON.");
        }
    }

    internal static void AddCoordinate(
        Coordinate coordinate, bool hasElevation,
        GeoJsonBoundingBoxState state)
    {
        GeographicMath.ValidateGeographicCoordinate(coordinate);
        double longitude =
            GeographicMath.NormalizeLongitude(coordinate.X);
        state.Longitudes.Add(longitude);
        if (coordinate.Y < state.MinimumLatitude)
        {
            state.MinimumLatitude = coordinate.Y;
        }
        if (coordinate.Y > state.MaximumLatitude)
        {
            state.MaximumLatitude = coordinate.Y;
        }
        if (state.HasCoordinate && !hasElevation)
        {
            state.AllCoordinatesHaveElevation = false;
        }
        if (!state.HasCoordinate)
        {
            state.AllCoordinatesHaveElevation = hasElevation;
        }
        if (hasElevation)
        {
            if (coordinate.Z < state.MinimumElevation)
            {
                state.MinimumElevation = coordinate.Z;
            }
            if (coordinate.Z > state.MaximumElevation)
            {
                state.MaximumElevation = coordinate.Z;
            }
        }
        else
        {
            state.AllCoordinatesHaveElevation = false;
        }
        state.HasCoordinate = true;
    }
}
