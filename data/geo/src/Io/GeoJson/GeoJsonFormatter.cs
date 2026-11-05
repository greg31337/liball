using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace geo;

internal enum GeoJsonContainerKind
{
    Geometry = 0,
    Feature = 1,
    FeatureCollection = 2
}

internal class GeoJsonFormatter
{
    internal GeoJsonWriteOptions Options;
    internal byte[] Utf8 = [];
    internal string Text = "";

    internal GeoJsonFormatter(GeoJsonDocument document, GeoJsonWriteOptions options)
    {
        Options = CopyAndValidateOptions(options);
        ValidateDocument(document);
        JsonWriterOptions writerOptions = new JsonWriterOptions();
        writerOptions.Indented = Options.Indented;
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, writerOptions))
            {
                WriteDocument(writer, document);
                writer.Flush();
            }
            Utf8 = stream.ToArray();
        }
        Text = Encoding.UTF8.GetString(Utf8);
    }

    internal string FormatString()
    {
        return Text;
    }

    internal byte[] FormatUtf8Bytes()
    {
        byte[] copy = new byte[Utf8.Length];
        Utf8.CopyTo(copy, 0);
        return copy;
    }

    internal WriteResult TryWriteUtf8Bytes(Span<byte> destination)
    {
        WriteResult result = new WriteResult();
        result.RequiredSize = Utf8.Length;
        if (destination.Length < Utf8.Length)
        {
            ParseError error = new ParseError();
            error.Kind = ParseErrorKind.LimitExceeded;
            error.Offset = destination.Length;
            error.Message =
                $"GeoJSON destination requires {Utf8.Length} bytes but only {destination.Length} are available.";
            result.Error = error;
            return result;
        }
        Utf8.AsSpan().CopyTo(destination);
        result.Success = true;
        result.WrittenSize = Utf8.Length;
        return result;
    }

    internal void WriteToStream(Stream stream)
    {
        if (stream == null) throw new GeoJsonException("GeoJSON output stream is null.");
        if (!stream.CanWrite) throw new GeoJsonException("GeoJSON output stream is not writable.");
        stream.Write(Utf8, 0, Utf8.Length);
    }

    internal void WriteDocument(Utf8JsonWriter writer, GeoJsonDocument document)
    {
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                GeoJsonGeometry geometry = document.Geometry == null ?
                    throw new GeoJsonException("GeoJSON geometry document has no geometry value.") : document.Geometry;
                double[] geometryBoundingBox = SelectRootBoundingBox(document.BoundingBox, geometry.BoundingBox);
                Dictionary<string, JsonElement> geometryForeign =
                    SelectRootForeignMembers(document.ForeignMembers, geometry.ForeignMembers);
                WriteGeometryObject(
                    writer, geometry.Value, geometry, geometryBoundingBox, geometryForeign);
                break;
            case GeoJsonObjectType.Feature:
                GeoJsonFeature feature = document.Feature == null ?
                    throw new GeoJsonException("GeoJSON Feature document has no Feature value.") : document.Feature;
                double[] featureBoundingBox = SelectRootBoundingBox(document.BoundingBox, feature.BoundingBox);
                Dictionary<string, JsonElement> featureForeign =
                    SelectRootForeignMembers(document.ForeignMembers, feature.ForeignMembers);
                WriteFeatureObject(writer, feature, featureBoundingBox, featureForeign);
                break;
            case GeoJsonObjectType.FeatureCollection:
                GeoJsonFeatureCollection collection = document.FeatureCollection == null ?
                    throw new GeoJsonException(
                        "GeoJSON FeatureCollection document has no FeatureCollection value.") :
                    document.FeatureCollection;
                double[] collectionBoundingBox =
                    SelectRootBoundingBox(document.BoundingBox, collection.BoundingBox);
                Dictionary<string, JsonElement> collectionForeign =
                    SelectRootForeignMembers(document.ForeignMembers, collection.ForeignMembers);
                WriteFeatureCollectionObject(writer, collection, collectionBoundingBox, collectionForeign);
                break;
            default:
                throw new GeoJsonException("GeoJSON document object type is invalid.");
        }
    }

    internal void WriteGeometryObject(
        Utf8JsonWriter writer, Geometry? geometry, GeoJsonGeometry? metadata,
        double[] boundingBox, Dictionary<string, JsonElement> foreignMembers)
    {
        if (geometry == null) throw new GeoJsonException("GeoJSON geometry value is null.");
        ValidateGeometryForOutput(geometry);
        if (metadata != null)
        {
            if (metadata.GeometryType != geometry.GeometryType)
            {
                throw new GeoJsonException("GeoJSON geometry metadata type does not match its geometry value.");
            }
            if (metadata.Geometries == null)
            {
                throw new GeoJsonException("GeoJSON child geometry metadata array is null.");
            }
        }

        writer.WriteStartObject();
        writer.WriteString(GeoJsonNames.TypeMember, GetGeometryTypeName(geometry.GeometryType));
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            writer.WritePropertyName(GeoJsonNames.GeometriesMember);
            WriteGeometryCollectionArray(
                writer, geometry.GeometryCollection, metadata == null ? null : metadata.Geometries);
        }
        else
        {
            writer.WritePropertyName(GeoJsonNames.CoordinatesMember);
            WriteCoordinatePayload(writer, geometry);
        }
        WriteBoundingBoxMember(writer, boundingBox, geometry.CoordinateLayout, true);
        WriteForeignMembers(writer, foreignMembers, GeoJsonContainerKind.Geometry);
        writer.WriteEndObject();
    }

    internal void WriteFeatureObject(
        Utf8JsonWriter writer, GeoJsonFeature feature,
        double[] boundingBox, Dictionary<string, JsonElement> foreignMembers)
    {
        if (feature == null) throw new GeoJsonException("GeoJSON Feature value is null.");
        writer.WriteStartObject();
        writer.WriteString(GeoJsonNames.TypeMember, GeoJsonNames.FeatureType);
        if (feature.Id.ValueKind != JsonValueKind.Undefined)
        {
            if (feature.Id.ValueKind != JsonValueKind.String &&
                feature.Id.ValueKind != JsonValueKind.Number)
            {
                throw new GeoJsonException("GeoJSON Feature id must be a string or number.");
            }
            writer.WritePropertyName(GeoJsonNames.IdMember);
            feature.Id.WriteTo(writer);
        }

        writer.WritePropertyName(GeoJsonNames.GeometryMember);
        if (feature.Geometry == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteGeometryObject(
                writer, feature.Geometry.Value, feature.Geometry,
                feature.Geometry.BoundingBox, feature.Geometry.ForeignMembers);
        }

        writer.WritePropertyName(GeoJsonNames.PropertiesMember);
        if (feature.Properties.ValueKind == JsonValueKind.Undefined)
        {
            if (Options.Strict)
            {
                throw new GeoJsonException("Strict GeoJSON Feature output requires a properties value.");
            }
            writer.WriteNullValue();
        }
        else
        {
            feature.Properties.WriteTo(writer);
        }

        CoordinateLayout layout = feature.Geometry == null || feature.Geometry.Value == null ?
            CoordinateLayout.XY : feature.Geometry.Value.CoordinateLayout;
        bool requireLayout = feature.Geometry != null && feature.Geometry.Value != null;
        WriteBoundingBoxMember(writer, boundingBox, layout, requireLayout);
        WriteForeignMembers(writer, foreignMembers, GeoJsonContainerKind.Feature);
        writer.WriteEndObject();
    }

    internal void WriteFeatureCollectionObject(
        Utf8JsonWriter writer, GeoJsonFeatureCollection collection,
        double[] boundingBox, Dictionary<string, JsonElement> foreignMembers)
    {
        if (collection == null) throw new GeoJsonException("GeoJSON FeatureCollection value is null.");
        if (collection.Features == null)
        {
            throw new GeoJsonException("GeoJSON FeatureCollection feature array is null.");
        }
        writer.WriteStartObject();
        writer.WriteString(GeoJsonNames.TypeMember, GeoJsonNames.FeatureCollectionType);
        writer.WritePropertyName(GeoJsonNames.FeaturesMember);
        writer.WriteStartArray();
        int index = 0;
        while (index < collection.Features.Length)
        {
            GeoJsonFeature feature = collection.Features[index];
            if (feature == null) throw new GeoJsonException("GeoJSON FeatureCollection contains a null Feature.");
            WriteFeatureObject(writer, feature, feature.BoundingBox, feature.ForeignMembers);
            index++;
        }
        writer.WriteEndArray();
        WriteBoundingBoxMember(writer, boundingBox, CoordinateLayout.XY, false);
        WriteForeignMembers(writer, foreignMembers, GeoJsonContainerKind.FeatureCollection);
        writer.WriteEndObject();
    }

    internal void WriteCoordinatePayload(Utf8JsonWriter writer, Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                WritePointCoordinates(writer, geometry.Point, geometry.CoordinateLayout);
                break;
            case GeometryType.LineString:
                WriteCoordinateSequence(writer, geometry.LineString == null ? null :
                    geometry.LineString.Coordinates, geometry.CoordinateLayout);
                break;
            case GeometryType.Polygon:
                WritePolygonCoordinates(writer, geometry.Polygon, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiPoint:
                WriteMultiPointCoordinates(writer, geometry.MultiPoint, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiLineString:
                WriteMultiLineCoordinates(writer, geometry.MultiLineString, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiPolygon:
                WriteMultiPolygonCoordinates(writer, geometry.MultiPolygon, geometry.CoordinateLayout);
                break;
            default:
                throw new GeoJsonException("Geometry type cannot be represented by GeoJSON coordinates.");
        }
    }

    internal void WritePointCoordinates(
        Utf8JsonWriter writer, Point? point, CoordinateLayout layout)
    {
        if (point == null) throw new InvalidGeometryException("Point payload is null.");
        if (point.Coordinate == null)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }
        WritePosition(writer, point.Coordinate, layout);
    }

    internal void WriteCoordinateSequence(
        Utf8JsonWriter writer, Coordinate[]? coordinates, CoordinateLayout layout)
    {
        if (coordinates == null) throw new InvalidGeometryException("Coordinate sequence is null.");
        writer.WriteStartArray();
        int index = 0;
        while (index < coordinates.Length)
        {
            WritePosition(writer, coordinates[index], layout);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void WritePolygonCoordinates(
        Utf8JsonWriter writer, Polygon? polygon, CoordinateLayout layout)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon payload, shell, or holes are null.");
        }
        writer.WriteStartArray();
        if (polygon.Shell.Coordinates.Length != 0)
        {
            WriteRingCoordinates(writer, polygon.Shell.Coordinates, layout, true);
            int index = 0;
            while (index < polygon.Holes.Length)
            {
                LinearRing hole = polygon.Holes[index];
                if (hole == null) throw new InvalidGeometryException("Polygon hole is null.");
                WriteRingCoordinates(writer, hole.Coordinates, layout, false);
                index++;
            }
        }
        writer.WriteEndArray();
    }

    internal void WriteMultiPointCoordinates(
        Utf8JsonWriter writer, MultiPoint? multiPoint, CoordinateLayout layout)
    {
        if (multiPoint == null || multiPoint.Points == null)
        {
            throw new InvalidGeometryException("MultiPoint payload or point array is null.");
        }
        writer.WriteStartArray();
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            Point point = multiPoint.Points[index];
            if (point == null || point.Coordinate == null)
            {
                throw new GeoJsonException("GeoJSON MultiPoint cannot contain an empty Point component.");
            }
            WritePosition(writer, point.Coordinate, layout);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void WriteMultiLineCoordinates(
        Utf8JsonWriter writer, MultiLineString? multiLine, CoordinateLayout layout)
    {
        if (multiLine == null || multiLine.LineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString payload or line array is null.");
        }
        writer.WriteStartArray();
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            LineString line = multiLine.LineStrings[index];
            if (line == null) throw new InvalidGeometryException("MultiLineString component is null.");
            WriteCoordinateSequence(writer, line.Coordinates, layout);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void WriteMultiPolygonCoordinates(
        Utf8JsonWriter writer, MultiPolygon? multiPolygon, CoordinateLayout layout)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon payload or polygon array is null.");
        }
        writer.WriteStartArray();
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            WritePolygonCoordinates(writer, multiPolygon.Polygons[index], layout);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void WriteGeometryCollectionArray(
        Utf8JsonWriter writer, GeometryCollection? collection, GeoJsonGeometry[]? metadata)
    {
        if (collection == null || collection.Geometries == null)
        {
            throw new InvalidGeometryException("GeometryCollection payload or geometry array is null.");
        }
        int metadataCount = metadata == null ? 0 : metadata.Length;
        if (metadataCount != 0 && metadataCount != collection.Geometries.Length)
        {
            throw new GeoJsonException(
                "GeoJSON GeometryCollection child metadata count does not match its geometry count.");
        }
        writer.WriteStartArray();
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            Geometry child = collection.Geometries[index];
            GeoJsonGeometry? childMetadata = metadata == null || metadataCount == 0 ? null : metadata[index];
            if (childMetadata != null && childMetadata.Value != null && childMetadata.Value != child)
            {
                throw new GeoJsonException(
                    "GeoJSON GeometryCollection child metadata does not reference the corresponding geometry.");
            }
            double[] boundingBox = childMetadata == null ? [] : childMetadata.BoundingBox;
            Dictionary<string, JsonElement> foreign = childMetadata == null ?
                new Dictionary<string, JsonElement>() : childMetadata.ForeignMembers;
            WriteGeometryObject(writer, child, childMetadata, boundingBox, foreign);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void WriteRingCoordinates(
        Utf8JsonWriter writer, Coordinate[] coordinates, CoordinateLayout layout, bool shell)
    {
        Coordinate[] rounded = CreateRoundedRing(coordinates, layout);
        double area = GeometryNormalization.GetSignedRingArea(rounded);
        bool shouldBePositive = shell;
        bool reverse = shouldBePositive ? area < 0.0 : area > 0.0;
        if (Options.Strict && !Options.NormalizeRingOrientation && reverse)
        {
            string orientation = shell ? "counter-clockwise" : "clockwise";
            throw new GeoJsonException($"Strict GeoJSON requires a {orientation} polygon ring.");
        }

        writer.WriteStartArray();
        if (Options.NormalizeRingOrientation && reverse)
        {
            int index = coordinates.Length - 1;
            while (index >= 0)
            {
                WritePosition(writer, coordinates[index], layout);
                index--;
            }
        }
        else
        {
            int index = 0;
            while (index < coordinates.Length)
            {
                WritePosition(writer, coordinates[index], layout);
                index++;
            }
        }
        writer.WriteEndArray();
    }

    internal Coordinate[] CreateRoundedRing(Coordinate[] coordinates, CoordinateLayout layout)
    {
        if (coordinates == null || coordinates.Length < 4)
        {
            throw new InvalidGeometryException("GeoJSON polygon ring requires at least four coordinates.");
        }
        Coordinate[] rounded = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate source = coordinates[index];
            if (source == null) throw new InvalidGeometryException("GeoJSON polygon ring coordinate is null.");
            Coordinate coordinate = new Coordinate();
            coordinate.X = RoundCoordinate(source.X);
            coordinate.Y = RoundCoordinate(source.Y);
            if (layout == CoordinateLayout.XYZ) coordinate.Z = RoundCoordinate(source.Z);
            rounded[index] = coordinate;
            index++;
        }
        if (!GeoJsonParser.PositionsAreEqual(rounded[0], rounded[rounded.Length - 1], layout))
        {
            throw new GeoJsonException("Rounded GeoJSON polygon ring is not closed.");
        }
        if (!GeometryInspection.HasThreeDistinctPlanarCoordinates(rounded))
        {
            throw new GeoJsonException(
                "Coordinate precision collapses a GeoJSON polygon ring below three distinct positions.");
        }
        if (GeometryNormalization.GetSignedRingArea(rounded) == 0.0)
        {
            throw new GeoJsonException("Coordinate precision collapses a GeoJSON polygon ring to zero area.");
        }
        return rounded;
    }

    internal void WritePosition(
        Utf8JsonWriter writer, Coordinate coordinate, CoordinateLayout layout)
    {
        if (coordinate == null) throw new InvalidGeometryException("GeoJSON coordinate is null.");
        writer.WriteStartArray();
        WriteCoordinateNumber(writer, coordinate.X);
        WriteCoordinateNumber(writer, coordinate.Y);
        if (layout == CoordinateLayout.XYZ) WriteCoordinateNumber(writer, coordinate.Z);
        writer.WriteEndArray();
    }

    internal void WriteCoordinateNumber(Utf8JsonWriter writer, double value)
    {
        writer.WriteRawValue(FormatCoordinateNumber(value), true);
    }

    internal string FormatCoordinateNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new GeoJsonException("GeoJSON coordinate and bbox values must be finite.");
        }
        value = RoundCoordinate(value);
        if (value == 0.0) return "0";
        if (Options.CoordinatePrecision == 0)
        {
            return value.ToString("0", CultureInfo.InvariantCulture);
        }
        string format = "0." +
            new string('#', Options.CoordinatePrecision);
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    internal double RoundCoordinate(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new GeoJsonException("GeoJSON coordinate and bbox values must be finite.");
        }
        return Math.Round(value, Options.CoordinatePrecision, MidpointRounding.ToEven);
    }

    internal void WriteBoundingBoxMember(
        Utf8JsonWriter writer, double[] boundingBox, CoordinateLayout layout, bool requireLayout)
    {
        if (!Options.IncludeBoundingBox || boundingBox.Length == 0) return;
        ValidateBoundingBox(boundingBox, layout, requireLayout);
        writer.WritePropertyName(GeoJsonNames.BoundingBoxMember);
        writer.WriteStartArray();
        int index = 0;
        while (index < boundingBox.Length)
        {
            WriteCoordinateNumber(writer, boundingBox[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal void ValidateBoundingBox(
        double[] boundingBox, CoordinateLayout layout, bool requireLayout)
    {
        if (boundingBox == null) throw new GeoJsonException("GeoJSON bbox array is null.");
        if (boundingBox.Length != 4 && boundingBox.Length != 6)
        {
            throw new GeoJsonException("GeoJSON bbox must contain four or six values.");
        }
        if (Options.Strict && requireLayout)
        {
            int required = layout == CoordinateLayout.XYZ ? 6 : 4;
            if (boundingBox.Length != required)
            {
                throw new GeoJsonException("GeoJSON bbox dimensionality does not match its geometry.");
            }
        }
        int dimensions = boundingBox.Length / 2;
        int index = 0;
        while (index < boundingBox.Length)
        {
            if (!double.IsFinite(boundingBox[index]))
            {
                throw new GeoJsonException("GeoJSON bbox values must be finite.");
            }
            index++;
        }
        if (boundingBox[1] > boundingBox[dimensions + 1])
        {
            throw new GeoJsonException("GeoJSON bbox south value cannot exceed its north value.");
        }
        if (dimensions == 3 && boundingBox[2] > boundingBox[5])
        {
            throw new GeoJsonException("GeoJSON bbox minimum Z cannot exceed maximum Z.");
        }
    }

    internal void WriteForeignMembers(
        Utf8JsonWriter writer, Dictionary<string, JsonElement> foreignMembers,
        GeoJsonContainerKind kind)
    {
        if (!Options.IncludeForeignMembers) return;
        if (foreignMembers == null) throw new GeoJsonException("GeoJSON foreign-member map is null.");
        string[] names = new string[foreignMembers.Count];
        int index = 0;
        foreach (KeyValuePair<string, JsonElement> member in foreignMembers)
        {
            names[index] = member.Key;
            index++;
        }
        Array.Sort(names, StringComparer.Ordinal);
        index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            if (IsReservedMember(name, kind))
            {
                throw new GeoJsonException($"GeoJSON foreign member '{name}' conflicts with a standard member.");
            }
            if (Options.Strict && name == "crs")
            {
                throw new GeoJsonException("Strict RFC 7946 output cannot contain a crs member.");
            }
            JsonElement value = foreignMembers[name];
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                throw new GeoJsonException($"GeoJSON foreign member '{name}' has an undefined value.");
            }
            writer.WritePropertyName(name);
            value.WriteTo(writer);
            index++;
        }
    }

    internal void ValidateDocument(GeoJsonDocument document)
    {
        if (document == null) throw new GeoJsonException("GeoJSON document is null.");
        if (document.BoundingBox == null) throw new GeoJsonException("GeoJSON root bbox array is null.");
        if (document.ForeignMembers == null) throw new GeoJsonException("GeoJSON root foreign-member map is null.");
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                if (document.Geometry == null) throw new GeoJsonException("GeoJSON geometry document has no geometry.");
                if (Options.Strict && (document.Feature != null || document.FeatureCollection != null))
                {
                    throw new GeoJsonException("GeoJSON document contains values outside its selected root type.");
                }
                ValidateGeometryWrapper(document.Geometry);
                break;
            case GeoJsonObjectType.Feature:
                if (document.Feature == null) throw new GeoJsonException("GeoJSON Feature document has no Feature.");
                if (Options.Strict && (document.Geometry != null || document.FeatureCollection != null))
                {
                    throw new GeoJsonException("GeoJSON document contains values outside its selected root type.");
                }
                ValidateFeatureValue(document.Feature);
                break;
            case GeoJsonObjectType.FeatureCollection:
                if (document.FeatureCollection == null)
                {
                    throw new GeoJsonException("GeoJSON FeatureCollection document has no FeatureCollection.");
                }
                if (Options.Strict && (document.Geometry != null || document.Feature != null))
                {
                    throw new GeoJsonException("GeoJSON document contains values outside its selected root type.");
                }
                ValidateFeatureCollectionValue(document.FeatureCollection);
                break;
            default:
                throw new GeoJsonException("GeoJSON document object type is invalid.");
        }
    }

    internal void ValidateGeometryWrapper(GeoJsonGeometry geometry)
    {
        if (geometry == null || geometry.Value == null)
        {
            throw new GeoJsonException("GeoJSON geometry wrapper or value is null.");
        }
        if (geometry.BoundingBox == null || geometry.ForeignMembers == null || geometry.Geometries == null)
        {
            throw new GeoJsonException("GeoJSON geometry metadata collection is null.");
        }
        if (geometry.GeometryType != geometry.Value.GeometryType)
        {
            throw new GeoJsonException("GeoJSON geometry wrapper type does not match its geometry value.");
        }
        GeometryTransforms.ValidateEditedGeometryStructure(geometry.Value);
        GeometryInspection.ExtractValidatedCoordinates(geometry.Value);
        ValidateGeometryForOutput(geometry.Value);
        if (geometry.Value.GeometryType == GeometryType.GeometryCollection)
        {
            Geometry[] values = geometry.Value.GeometryCollection == null ? [] :
                geometry.Value.GeometryCollection.Geometries;
            if (geometry.Geometries.Length != 0 && geometry.Geometries.Length != values.Length)
            {
                throw new GeoJsonException(
                    "GeoJSON GeometryCollection child metadata count does not match its geometry count.");
            }
            int index = 0;
            while (index < geometry.Geometries.Length)
            {
                GeoJsonGeometry child = geometry.Geometries[index];
                if (child == null || child.Value != values[index])
                {
                    throw new GeoJsonException(
                        "GeoJSON GeometryCollection child metadata does not match its geometry value.");
                }
                ValidateGeometryWrapper(child);
                index++;
            }
        }
        else if (geometry.Geometries.Length != 0)
        {
            throw new GeoJsonException("Only a GeoJSON GeometryCollection can have child geometry metadata.");
        }
    }

    internal void ValidateFeatureValue(GeoJsonFeature feature)
    {
        if (feature == null) throw new GeoJsonException("GeoJSON Feature is null.");
        if (feature.BoundingBox == null || feature.ForeignMembers == null)
        {
            throw new GeoJsonException("GeoJSON Feature metadata collection is null.");
        }
        if (feature.Id.ValueKind != JsonValueKind.Undefined &&
            feature.Id.ValueKind != JsonValueKind.String && feature.Id.ValueKind != JsonValueKind.Number)
        {
            throw new GeoJsonException("GeoJSON Feature id must be a string or number.");
        }
        if (Options.Strict && feature.Properties.ValueKind == JsonValueKind.Undefined)
        {
            throw new GeoJsonException("Strict GeoJSON Feature output requires a properties value.");
        }
        if (feature.Geometry != null) ValidateGeometryWrapper(feature.Geometry);
    }

    internal void ValidateFeatureCollectionValue(GeoJsonFeatureCollection collection)
    {
        if (collection == null || collection.Features == null)
        {
            throw new GeoJsonException("GeoJSON FeatureCollection or feature array is null.");
        }
        if (collection.BoundingBox == null || collection.ForeignMembers == null)
        {
            throw new GeoJsonException("GeoJSON FeatureCollection metadata collection is null.");
        }
        int index = 0;
        while (index < collection.Features.Length)
        {
            ValidateFeatureValue(collection.Features[index]);
            index++;
        }
    }

    internal void ValidateGeometryForOutput(Geometry geometry)
    {
        if (geometry.GeometryType != GeometryType.Point && geometry.GeometryType != GeometryType.LineString &&
            geometry.GeometryType != GeometryType.Polygon && geometry.GeometryType != GeometryType.MultiPoint &&
            geometry.GeometryType != GeometryType.MultiLineString &&
            geometry.GeometryType != GeometryType.MultiPolygon &&
            geometry.GeometryType != GeometryType.GeometryCollection)
        {
            throw new GeoJsonException("Extended geometry type cannot be represented by RFC 7946 GeoJSON.");
        }
        if (geometry.CoordinateLayout != CoordinateLayout.XY &&
            geometry.CoordinateLayout != CoordinateLayout.XYZ)
        {
            throw new GeoJsonException("GeoJSON supports only XY and XYZ coordinate layouts.");
        }
        if (Options.Strict && geometry.SpatialReferenceId != GeometryConstants.UnknownSpatialReferenceId &&
            geometry.SpatialReferenceId != GeometryConstants.Crs84SpatialReferenceId)
        {
            throw new GeoJsonException("Strict GeoJSON requires unknown or CRS84 spatial reference metadata.");
        }
    }

    internal static string GetGeometryTypeName(GeometryType type)
    {
        if (type == GeometryType.Point) return GeoJsonNames.PointType;
        if (type == GeometryType.LineString) return GeoJsonNames.LineStringType;
        if (type == GeometryType.Polygon) return GeoJsonNames.PolygonType;
        if (type == GeometryType.MultiPoint) return GeoJsonNames.MultiPointType;
        if (type == GeometryType.MultiLineString) return GeoJsonNames.MultiLineStringType;
        if (type == GeometryType.MultiPolygon) return GeoJsonNames.MultiPolygonType;
        if (type == GeometryType.GeometryCollection) return GeoJsonNames.GeometryCollectionType;
        throw new GeoJsonException("Geometry type cannot be represented by RFC 7946 GeoJSON.");
    }

    internal static double[] SelectRootBoundingBox(double[] root, double[] nested)
    {
        if (root == null || nested == null) throw new GeoJsonException("GeoJSON bbox array is null.");
        return root.Length == 0 ? nested : root;
    }

    internal static Dictionary<string, JsonElement> SelectRootForeignMembers(
        Dictionary<string, JsonElement> root, Dictionary<string, JsonElement> nested)
    {
        if (root == null || nested == null)
        {
            throw new GeoJsonException("GeoJSON foreign-member map is null.");
        }
        return root.Count == 0 ? nested : root;
    }

    internal static bool IsReservedMember(string name, GeoJsonContainerKind kind)
    {
        if (name == GeoJsonNames.TypeMember || name == GeoJsonNames.BoundingBoxMember) return true;
        if (kind == GeoJsonContainerKind.Geometry)
        {
            return name == GeoJsonNames.CoordinatesMember || name == GeoJsonNames.GeometriesMember;
        }
        if (kind == GeoJsonContainerKind.Feature)
        {
            return name == GeoJsonNames.GeometryMember || name == GeoJsonNames.PropertiesMember ||
                name == GeoJsonNames.IdMember;
        }
        return name == GeoJsonNames.FeaturesMember;
    }

    internal static GeoJsonWriteOptions CopyAndValidateOptions(GeoJsonWriteOptions source)
    {
        if (source == null) throw new GeoJsonException("GeoJSON write options are null.");
        if (source.CoordinatePrecision < 0 || source.CoordinatePrecision > 15)
        {
            throw new GeoJsonException("GeoJSON coordinate precision must be between zero and fifteen.");
        }
        GeoJsonWriteOptions copy = new GeoJsonWriteOptions();
        copy.Strict = source.Strict;
        copy.CoordinatePrecision = source.CoordinatePrecision;
        copy.IncludeBoundingBox = source.IncludeBoundingBox;
        copy.IncludeForeignMembers = source.IncludeForeignMembers;
        copy.NormalizeRingOrientation = source.NormalizeRingOrientation;
        copy.Indented = source.Indented;
        return copy;
    }
}
