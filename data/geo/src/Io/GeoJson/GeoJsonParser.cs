using System.Collections.Generic;
using System.Text.Json;

namespace geo;

internal class GeoJsonMemberSet
{
    internal JsonElement Type;
    internal JsonElement Coordinates;
    internal JsonElement Geometries;
    internal JsonElement BoundingBox;
    internal Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();
}

internal class GeoJsonLayoutState
{
    internal CoordinateLayout Layout = CoordinateLayout.XY;
    internal bool HasLayout;
}

internal class GeoJsonContainerMemberSet
{
    internal JsonElement Type;
    internal JsonElement Geometry;
    internal JsonElement Properties;
    internal JsonElement Features;
    internal JsonElement Id;
    internal JsonElement BoundingBox;
    internal Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();
}

internal class GeoJsonParser
{
    internal byte[] Source = [];
    internal GeoJsonReadOptions Options = new GeoJsonReadOptions();
    internal int CoordinateCount;
    internal int ComponentCount;
    internal ParseError? Failure;
    internal Dictionary<string, long> Offsets = new Dictionary<string, long>();

    internal GeoJsonParser(byte[] source, GeoJsonReadOptions options)
    {
        if (source == null)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, 0, "$", "GeoJSON input is null at byte offset 0.");
        }
        else
        {
            Source = source;
        }
        if (options == null)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, "$", "GeoJSON read options are null at byte offset 0.");
            return;
        }
        Options = CopyReadOptions(options);
        ValidateOptions();
    }

    internal GeoJsonReadResult ParseDocument()
    {
        GeoJsonReadResult result = new GeoJsonReadResult();
        if (Failure != null)
        {
            SetFailureResult(result);
            return result;
        }

        int jsonMaximumDepth = Options.MaximumNestingDepth > 508 ?
            1024 : Options.MaximumNestingDepth * 2 + 8;
        GeoJsonSyntaxValidator syntax = new GeoJsonSyntaxValidator(Source, jsonMaximumDepth);
        if (!syntax.ValidateDocument())
        {
            Failure = syntax.Failure;
            SetFailureResult(result);
            return result;
        }
        Offsets = GeoJsonOffsetLocator.CreateOffsetMap(Source, jsonMaximumDepth);

        JsonDocumentOptions documentOptions = new JsonDocumentOptions();
        documentOptions.MaxDepth = jsonMaximumDepth;
        using (JsonDocument json = JsonDocument.Parse(Source, documentOptions))
        {
            JsonElement root = json.RootElement;
            string? rootType = ReadRootTypeName(root, "$.");
            if (Failure != null)
            {
                SetFailureResult(result);
                return result;
            }

            GeoJsonDocument? document = ParseRootObject(root, rootType);
            if (Failure != null || document == null)
            {
                SetFailureResult(result);
                return result;
            }
            result.Success = true;
            result.Document = document;
            result.Consumed = Source.Length;
            return result;
        }
    }

    internal GeoJsonDocument? ParseRootObject(JsonElement root, string? rootType)
    {
        GeoJsonDocument document = new GeoJsonDocument();
        if (rootType == GeoJsonNames.FeatureType)
        {
            GeoJsonFeature? feature = ParseFeatureObject(root, "$", 0);
            if (feature == null) return null;
            document.ObjectType = GeoJsonObjectType.Feature;
            document.Feature = feature;
            document.BoundingBox = (double[])feature.BoundingBox.Clone();
            document.ForeignMembers = CloneForeignMemberMap(feature.ForeignMembers);
            return document;
        }
        if (rootType == GeoJsonNames.FeatureCollectionType)
        {
            GeoJsonFeatureCollection? collection = ParseFeatureCollectionObject(root, "$", 0);
            if (collection == null) return null;
            document.ObjectType = GeoJsonObjectType.FeatureCollection;
            document.FeatureCollection = collection;
            document.BoundingBox = (double[])collection.BoundingBox.Clone();
            document.ForeignMembers = CloneForeignMemberMap(collection.ForeignMembers);
            return document;
        }

        GeoJsonLayoutState layout = new GeoJsonLayoutState();
        GeoJsonGeometry? geometry = ParseGeometryObject(root, "$", layout, 0);
        if (geometry == null || geometry.Value == null) return null;
        ApplyLayoutToGeometryMetadata(geometry, layout.Layout);
        if (!ValidateBoundingBoxLayouts(geometry, layout.Layout, "$")) return null;
        document.ObjectType = GeoJsonObjectType.Geometry;
        document.Geometry = geometry;
        document.BoundingBox = (double[])geometry.BoundingBox.Clone();
        document.ForeignMembers = CloneForeignMemberMap(geometry.ForeignMembers);
        return document;
    }

    internal string? ReadRootTypeName(JsonElement element, string pathPrefix)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, "$",
                "GeoJSON root value must be an object at $.");
            return null;
        }
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name == GeoJsonNames.TypeMember)
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    SetFailure(ParseErrorKind.InvalidGeometryType, 0, pathPrefix + "type",
                        $"GeoJSON root requires a string type member at {pathPrefix}type.");
                    return null;
                }
                return property.Value.GetString();
            }
        }
        SetFailure(ParseErrorKind.InvalidGeometryType, 0, pathPrefix + "type",
            $"GeoJSON root requires a string type member at {pathPrefix}type.");
        return null;
    }

    internal GeoJsonFeature? ParseFeatureObject(JsonElement element, string path, int depth)
    {
        if (!CheckObjectDepth(depth, path)) return null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON Feature must be an object at {path}.");
            return null;
        }
        if (!ReserveComponents(1, path)) return null;
        GeoJsonContainerMemberSet? members = ReadContainerMembers(element, path, false);
        if (members == null) return null;
        if (!ValidateContainerType(members.Type, GeoJsonNames.FeatureType, path)) return null;

        GeoJsonFeature feature = new GeoJsonFeature();
        if (members.Geometry.ValueKind == JsonValueKind.Undefined)
        {
            if (Options.Strict)
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".geometry",
                    $"RFC 7946 Feature requires a geometry member at {path}.geometry.");
                return null;
            }
        }
        else if (members.Geometry.ValueKind != JsonValueKind.Null)
        {
            GeoJsonLayoutState layout = new GeoJsonLayoutState();
            feature.Geometry = ParseGeometryObject(members.Geometry, path + ".geometry", layout, depth + 1);
            if (feature.Geometry == null || feature.Geometry.Value == null) return null;
            ApplyLayoutToGeometryMetadata(feature.Geometry, layout.Layout);
            if (!ValidateBoundingBoxLayouts(feature.Geometry, layout.Layout, path + ".geometry")) return null;
        }

        if (members.Properties.ValueKind == JsonValueKind.Undefined)
        {
            if (Options.Strict)
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".properties",
                    $"RFC 7946 Feature requires a properties member at {path}.properties.");
                return null;
            }
        }
        else
        {
            feature.Properties = members.Properties.Clone();
        }

        if (members.Id.ValueKind != JsonValueKind.Undefined)
        {
            if (members.Id.ValueKind != JsonValueKind.String && members.Id.ValueKind != JsonValueKind.Number)
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".id",
                    $"GeoJSON Feature id must be a string or number at {path}.id.");
                return null;
            }
            feature.Id = members.Id.Clone();
        }

        feature.BoundingBox = ParseBoundingBox(members.BoundingBox, path + ".bbox");
        if (Failure != null) return null;
        if (Options.Strict && feature.Geometry != null && feature.Geometry.Value != null &&
            !ValidateBoundingBoxDimension(
                feature.BoundingBox, feature.Geometry.Value.CoordinateLayout, path + ".bbox")) return null;
        feature.ForeignMembers = members.ForeignMembers;
        return feature;
    }

    internal GeoJsonFeatureCollection? ParseFeatureCollectionObject(
        JsonElement element, string path, int depth)
    {
        if (!CheckObjectDepth(depth, path)) return null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON FeatureCollection must be an object at {path}.");
            return null;
        }
        if (!ReserveComponents(1, path)) return null;
        GeoJsonContainerMemberSet? members = ReadContainerMembers(element, path, true);
        if (members == null) return null;
        if (!ValidateContainerType(members.Type, GeoJsonNames.FeatureCollectionType, path)) return null;
        if (members.Features.ValueKind == JsonValueKind.Undefined)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".features",
                $"GeoJSON FeatureCollection requires a features member at {path}.features.");
            return null;
        }
        if (members.Features.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".features",
                $"GeoJSON FeatureCollection features must be an array at {path}.features.");
            return null;
        }

        int count = members.Features.GetArrayLength();
        if (!CheckComponentCapacity(count, path + ".features")) return null;
        GeoJsonFeature[] features = new GeoJsonFeature[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            string featurePath = path + ".features[" + index + "]";
            GeoJsonFeature? feature = ParseFeatureObject(members.Features[index], featurePath, depth + 1);
            if (feature == null) return null;
            features[index] = feature;
            index++;
        }

        GeoJsonFeatureCollection collection = new GeoJsonFeatureCollection();
        collection.Features = features;
        collection.BoundingBox = ParseBoundingBox(members.BoundingBox, path + ".bbox");
        if (Failure != null) return null;
        collection.ForeignMembers = members.ForeignMembers;
        return collection;
    }

    internal GeoJsonContainerMemberSet? ReadContainerMembers(
        JsonElement element, string path, bool featureCollection)
    {
        GeoJsonContainerMemberSet members = new GeoJsonContainerMemberSet();
        Dictionary<string, bool> seen = new Dictionary<string, bool>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string name = property.Name;
            if (seen.ContainsKey(name))
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + "." + name,
                    $"GeoJSON member '{name}' is duplicated at {path}.{name}.");
                return null;
            }
            seen.Add(name, true);

            if (name == GeoJsonNames.TypeMember) members.Type = property.Value;
            else if (name == GeoJsonNames.BoundingBoxMember) members.BoundingBox = property.Value;
            else if (!featureCollection && name == GeoJsonNames.GeometryMember) members.Geometry = property.Value;
            else if (!featureCollection && name == GeoJsonNames.PropertiesMember) members.Properties = property.Value;
            else if (!featureCollection && name == GeoJsonNames.IdMember) members.Id = property.Value;
            else if (featureCollection && name == GeoJsonNames.FeaturesMember) members.Features = property.Value;
            else if (Options.Strict && name == "crs")
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".crs",
                    $"RFC 7946 GeoJSON cannot contain a crs member at {path}.crs.");
                return null;
            }
            else if (Options.PreserveForeignMembers)
            {
                members.ForeignMembers.Add(name, property.Value.Clone());
            }
        }
        return members;
    }

    internal bool ValidateContainerType(JsonElement type, string required, string path)
    {
        if (type.ValueKind != JsonValueKind.String || type.GetString() != required)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".type",
                $"GeoJSON object type must be '{required}' at {path}.type.");
            return false;
        }
        return true;
    }

    internal bool CheckObjectDepth(int depth, string path)
    {
        if (depth <= Options.MaximumNestingDepth) return true;
        SetFailure(ParseErrorKind.LimitExceeded, 0, path,
            $"Maximum GeoJSON object nesting depth of {Options.MaximumNestingDepth} was exceeded at {path}.");
        return false;
    }

    internal GeoJsonGeometry? ParseGeometryObject(
        JsonElement element, string path, GeoJsonLayoutState layout, int depth)
    {
        if (Failure != null) return null;
        if (depth > Options.MaximumNestingDepth)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, path,
                $"Maximum GeoJSON geometry nesting depth of {Options.MaximumNestingDepth} was exceeded at {path}.");
            return null;
        }
        if (element.ValueKind != JsonValueKind.Object)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON geometry must be an object at {path}.");
            return null;
        }
        if (!ReserveComponents(1, path)) return null;

        GeoJsonMemberSet? members = ReadGeometryMembers(element, path);
        if (members == null) return null;
        if (members.Type.ValueKind != JsonValueKind.String)
        {
            SetFailure(ParseErrorKind.InvalidGeometryType, 0, path + ".type",
                $"GeoJSON geometry requires a string type member at {path}.type.");
            return null;
        }

        string? typeName = members.Type.GetString();
        GeoJsonGeometry? geometry;
        if (typeName == GeoJsonNames.PointType)
        {
            geometry = ParsePointGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.LineStringType)
        {
            geometry = ParseLineStringGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.PolygonType)
        {
            geometry = ParsePolygonGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.MultiPointType)
        {
            geometry = ParseMultiPointGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.MultiLineStringType)
        {
            geometry = ParseMultiLineStringGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.MultiPolygonType)
        {
            geometry = ParseMultiPolygonGeometry(members, path, layout);
        }
        else if (typeName == GeoJsonNames.GeometryCollectionType)
        {
            geometry = ParseGeometryCollectionGeometry(members, path, layout, depth);
        }
        else
        {
            SetFailure(ParseErrorKind.InvalidGeometryType, 0, path + ".type",
                $"GeoJSON geometry type '{typeName}' is invalid at {path}.type.");
            return null;
        }
        if (geometry == null) return null;

        geometry.BoundingBox = ParseBoundingBox(members.BoundingBox, path + ".bbox");
        if (Failure != null) return null;
        geometry.ForeignMembers = members.ForeignMembers;
        return geometry;
    }

    internal GeoJsonMemberSet? ReadGeometryMembers(JsonElement element, string path)
    {
        GeoJsonMemberSet members = new GeoJsonMemberSet();
        Dictionary<string, bool> seen = new Dictionary<string, bool>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string name = property.Name;
            if (seen.ContainsKey(name))
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + "." + name,
                    $"GeoJSON member '{name}' is duplicated at {path}.{name}.");
                return null;
            }
            seen.Add(name, true);

            if (name == GeoJsonNames.TypeMember) members.Type = property.Value;
            else if (name == GeoJsonNames.CoordinatesMember) members.Coordinates = property.Value;
            else if (name == GeoJsonNames.GeometriesMember) members.Geometries = property.Value;
            else if (name == GeoJsonNames.BoundingBoxMember) members.BoundingBox = property.Value;
            else if (Options.Strict && name == "crs")
            {
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".crs",
                    $"RFC 7946 GeoJSON cannot contain a crs member at {path}.crs.");
                return null;
            }
            else if (Options.PreserveForeignMembers)
            {
                members.ForeignMembers.Add(name, property.Value.Clone());
            }
        }
        return members;
    }

    internal GeoJsonGeometry? ParsePointGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        if (members.Coordinates.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path + ".coordinates",
                $"GeoJSON Point coordinates must be an array at {path}.coordinates.");
            return null;
        }

        Point point = new Point();
        if (members.Coordinates.GetArrayLength() != 0)
        {
            if (!ReserveCoordinates(1, path + ".coordinates")) return null;
            point.Coordinate = ParsePosition(members.Coordinates, path + ".coordinates", layout);
            if (Failure != null || point.Coordinate == null) return null;
        }
        Geometry value = CreateGeometryShell(GeometryType.Point, layout.Layout);
        value.Point = point;
        return CreateGeometryWrapper(GeometryType.Point, value);
    }

    internal GeoJsonGeometry? ParseLineStringGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        Coordinate[]? coordinates = ParseLineCoordinates(
            members.Coordinates, path + ".coordinates", layout, true);
        if (coordinates == null) return null;
        LineString line = new LineString();
        line.Coordinates = coordinates;
        Geometry value = CreateGeometryShell(GeometryType.LineString, layout.Layout);
        value.LineString = line;
        return CreateGeometryWrapper(GeometryType.LineString, value);
    }

    internal GeoJsonGeometry? ParsePolygonGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        Polygon? polygon = ParsePolygonCoordinates(members.Coordinates, path + ".coordinates", layout);
        if (polygon == null) return null;
        Geometry value = CreateGeometryShell(GeometryType.Polygon, layout.Layout);
        value.Polygon = polygon;
        return CreateGeometryWrapper(GeometryType.Polygon, value);
    }

    internal GeoJsonGeometry? ParseMultiPointGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        JsonElement coordinates = members.Coordinates;
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path + ".coordinates",
                $"GeoJSON MultiPoint coordinates must be an array at {path}.coordinates.");
            return null;
        }
        int count = coordinates.GetArrayLength();
        if (!ReserveComponents(count, path + ".coordinates")) return null;
        if (!ReserveCoordinates(count, path + ".coordinates")) return null;
        Point[] points = new Point[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            string positionPath = path + ".coordinates[" + index + "]";
            Coordinate? coordinate = ParsePosition(coordinates[index], positionPath, layout);
            if (coordinate == null) return null;
            Point point = new Point();
            point.Coordinate = coordinate;
            points[index] = point;
            index++;
        }
        MultiPoint multiPoint = new MultiPoint();
        multiPoint.Points = points;
        Geometry value = CreateGeometryShell(GeometryType.MultiPoint, layout.Layout);
        value.MultiPoint = multiPoint;
        return CreateGeometryWrapper(GeometryType.MultiPoint, value);
    }

    internal GeoJsonGeometry? ParseMultiLineStringGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        JsonElement coordinates = members.Coordinates;
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path + ".coordinates",
                $"GeoJSON MultiLineString coordinates must be an array at {path}.coordinates.");
            return null;
        }
        int count = coordinates.GetArrayLength();
        if (!ReserveComponents(count, path + ".coordinates")) return null;
        LineString[] lines = new LineString[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            string linePath = path + ".coordinates[" + index + "]";
            Coordinate[]? lineCoordinates = ParseLineCoordinates(coordinates[index], linePath, layout, true);
            if (lineCoordinates == null) return null;
            LineString line = new LineString();
            line.Coordinates = lineCoordinates;
            lines[index] = line;
            index++;
        }
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = lines;
        Geometry value = CreateGeometryShell(GeometryType.MultiLineString, layout.Layout);
        value.MultiLineString = multiLine;
        return CreateGeometryWrapper(GeometryType.MultiLineString, value);
    }

    internal GeoJsonGeometry? ParseMultiPolygonGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout)
    {
        if (!RequireCoordinateMembers(members, path)) return null;
        JsonElement coordinates = members.Coordinates;
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path + ".coordinates",
                $"GeoJSON MultiPolygon coordinates must be an array at {path}.coordinates.");
            return null;
        }
        int count = coordinates.GetArrayLength();
        if (!ReserveComponents(count, path + ".coordinates")) return null;
        Polygon[] polygons = new Polygon[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            string polygonPath = path + ".coordinates[" + index + "]";
            Polygon? polygon = ParsePolygonCoordinates(coordinates[index], polygonPath, layout);
            if (polygon == null) return null;
            polygons[index] = polygon;
            index++;
        }
        MultiPolygon multiPolygon = new MultiPolygon();
        multiPolygon.Polygons = polygons;
        Geometry value = CreateGeometryShell(GeometryType.MultiPolygon, layout.Layout);
        value.MultiPolygon = multiPolygon;
        return CreateGeometryWrapper(GeometryType.MultiPolygon, value);
    }

    internal GeoJsonGeometry? ParseGeometryCollectionGeometry(
        GeoJsonMemberSet members, string path, GeoJsonLayoutState layout, int depth)
    {
        if (members.Geometries.ValueKind == JsonValueKind.Undefined)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".geometries",
                $"GeoJSON GeometryCollection requires a geometries member at {path}.geometries.");
            return null;
        }
        if (members.Coordinates.ValueKind != JsonValueKind.Undefined)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".coordinates",
                $"GeoJSON GeometryCollection cannot contain a coordinates member at {path}.coordinates.");
            return null;
        }
        if (members.Geometries.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".geometries",
                $"GeoJSON GeometryCollection geometries must be an array at {path}.geometries.");
            return null;
        }
        int count = members.Geometries.GetArrayLength();
        if (!CheckComponentCapacity(count, path + ".geometries")) return null;
        GeoJsonGeometry[] wrappers = new GeoJsonGeometry[count];
        Geometry[] values = new Geometry[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            string geometryPath = path + ".geometries[" + index + "]";
            GeoJsonGeometry? child = ParseGeometryObject(members.Geometries[index], geometryPath, layout, depth + 1);
            if (child == null || child.Value == null) return null;
            wrappers[index] = child;
            values[index] = child.Value;
            index++;
        }
        GeometryCollection collection = new GeometryCollection();
        collection.Geometries = values;
        Geometry value = CreateGeometryShell(GeometryType.GeometryCollection, layout.Layout);
        value.GeometryCollection = collection;
        GeoJsonGeometry wrapper = CreateGeometryWrapper(GeometryType.GeometryCollection, value);
        wrapper.Geometries = wrappers;
        return wrapper;
    }

    internal Polygon? ParsePolygonCoordinates(
        JsonElement coordinates, string path, GeoJsonLayoutState layout)
    {
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON Polygon coordinates must be an array at {path}.");
            return null;
        }
        int ringCount = coordinates.GetArrayLength();
        Polygon polygon = new Polygon();
        if (ringCount == 0) return polygon;
        if (!ReserveComponents(ringCount, path)) return null;
        LinearRing[] rings = new LinearRing[ringCount];
        int index = 0;
        while (index < ringCount && Failure == null)
        {
            string ringPath = path + "[" + index + "]";
            Coordinate[]? ringCoordinates = ParseRingCoordinates(coordinates[index], ringPath, layout, index == 0);
            if (ringCoordinates == null) return null;
            LinearRing ring = new LinearRing();
            ring.Coordinates = ringCoordinates;
            rings[index] = ring;
            index++;
        }
        polygon.Shell = rings[0];
        polygon.Holes = new LinearRing[ringCount - 1];
        index = 1;
        while (index < ringCount)
        {
            polygon.Holes[index - 1] = rings[index];
            index++;
        }
        return polygon;
    }

    internal Coordinate[]? ParseLineCoordinates(
        JsonElement sequence, string path, GeoJsonLayoutState layout, bool allowEmpty)
    {
        if (sequence.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON coordinate sequence must be an array at {path}.");
            return null;
        }
        int count = sequence.GetArrayLength();
        if ((!allowEmpty && count == 0) || count == 1)
        {
            SetFailure(ParseErrorKind.InvalidCount, 0, path,
                $"GeoJSON LineString coordinate sequence must contain at least two positions at {path}.");
            return null;
        }
        if (!ReserveCoordinates(count, path)) return null;
        Coordinate[] coordinates = new Coordinate[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Coordinate? coordinate = ParsePosition(sequence[index], path + "[" + index + "]", layout);
            if (coordinate == null) return null;
            coordinates[index] = coordinate;
            index++;
        }
        return coordinates;
    }

    internal Coordinate[]? ParseRingCoordinates(
        JsonElement sequence, string path, GeoJsonLayoutState layout, bool shell)
    {
        if (sequence.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON linear ring must be an array at {path}.");
            return null;
        }
        int count = sequence.GetArrayLength();
        if (count < 4)
        {
            SetFailure(ParseErrorKind.InvalidCount, 0, path,
                $"GeoJSON linear ring must contain at least four positions at {path}.");
            return null;
        }
        if (!ReserveCoordinates(count, path)) return null;
        Coordinate[] coordinates = new Coordinate[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Coordinate? coordinate = ParsePosition(sequence[index], path + "[" + index + "]", layout);
            if (coordinate == null) return null;
            coordinates[index] = coordinate;
            index++;
        }
        if (!PositionsAreEqual(coordinates[0], coordinates[count - 1], layout.Layout))
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON linear ring must be closed with equivalent first and last positions at {path}.");
            return null;
        }
        if (!GeometryInspection.HasThreeDistinctPlanarCoordinates(coordinates))
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON linear ring requires three distinct planar positions at {path}.");
            return null;
        }
        if (Options.Strict)
        {
            double area = GeometryNormalization.GetSignedRingArea(coordinates);
            bool correct = shell ? area > 0.0 : area < 0.0;
            if (!correct)
            {
                string orientation = shell ? "counter-clockwise" : "clockwise";
                SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                    $"RFC 7946 requires this linear ring to be {orientation} at {path}.");
                return null;
            }
        }
        return coordinates;
    }

    internal Coordinate? ParsePosition(JsonElement position, string path, GeoJsonLayoutState layout)
    {
        if (position.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidCoordinate, 0, path,
                $"GeoJSON position must be an array at {path}.");
            return null;
        }
        int count = position.GetArrayLength();
        if (count != 2 && count != 3)
        {
            SetFailure(ParseErrorKind.InvalidCoordinateLayout, 0, path,
                $"GeoJSON position must contain exactly two or three ordinates at {path}.");
            return null;
        }
        CoordinateLayout currentLayout = count == 2 ? CoordinateLayout.XY : CoordinateLayout.XYZ;
        if (!layout.HasLayout)
        {
            layout.Layout = currentLayout;
            layout.HasLayout = true;
        }
        else if (layout.Layout != currentLayout)
        {
            SetFailure(ParseErrorKind.InvalidCoordinateLayout, 0, path,
                $"GeoJSON position layout {currentLayout} does not match required layout {layout.Layout} at {path}.");
            return null;
        }

        double x = ReadFiniteOrdinate(position[0], path + "[0]");
        double y = ReadFiniteOrdinate(position[1], path + "[1]");
        double z = GeometryConstants.NoDataOrdinate;
        if (count == 3) z = ReadFiniteOrdinate(position[2], path + "[2]");
        if (Failure != null) return null;
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        coordinate.Z = z;
        return coordinate;
    }

    internal double ReadFiniteOrdinate(JsonElement ordinate, string path)
    {
        if (ordinate.ValueKind != JsonValueKind.Number)
        {
            SetFailure(ParseErrorKind.InvalidNumber, 0, path,
                $"GeoJSON ordinate must be a number at {path}.");
            return 0.0;
        }
        double value = ordinate.GetDouble();
        if (!double.IsFinite(value))
        {
            SetFailure(ParseErrorKind.InvalidNumber, 0, path,
                $"GeoJSON ordinate must be a finite double at {path}.");
            return 0.0;
        }
        return value;
    }

    internal double[] ParseBoundingBox(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return [];
        if (element.ValueKind != JsonValueKind.Array)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON bbox must be an array at {path}.");
            return [];
        }
        int count = element.GetArrayLength();
        if (count != 4 && count != 6)
        {
            SetFailure(ParseErrorKind.InvalidCount, 0, path,
                $"GeoJSON bbox must contain four or six numbers at {path}.");
            return [];
        }
        double[] values = new double[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            values[index] = ReadFiniteOrdinate(element[index], path + "[" + index + "]");
            index++;
        }
        if (Failure != null) return [];
        int dimensions = count / 2;
        if (values[1] > values[dimensions + 1])
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON bbox south ordinate cannot exceed its north ordinate at {path}.");
            return [];
        }
        if (dimensions == 3 && values[2] > values[5])
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path,
                $"GeoJSON bbox minimum Z cannot exceed maximum Z at {path}.");
            return [];
        }
        return values;
    }

    internal bool RequireCoordinateMembers(GeoJsonMemberSet members, string path)
    {
        if (members.Coordinates.ValueKind == JsonValueKind.Undefined)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".coordinates",
                $"GeoJSON geometry requires a coordinates member at {path}.coordinates.");
            return false;
        }
        if (members.Geometries.ValueKind != JsonValueKind.Undefined)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, path + ".geometries",
                $"GeoJSON coordinate geometry cannot contain a geometries member at {path}.geometries.");
            return false;
        }
        return true;
    }

    internal bool ReserveCoordinates(int count, string path)
    {
        if (count < 0 || count > Options.MaximumCoordinates - CoordinateCount)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, path,
                $"Maximum GeoJSON coordinate count of {Options.MaximumCoordinates} would be exceeded at {path}.");
            return false;
        }
        CoordinateCount += count;
        return true;
    }

    internal bool ReserveComponents(int count, string path)
    {
        if (!CheckComponentCapacity(count, path)) return false;
        ComponentCount += count;
        return true;
    }

    internal bool CheckComponentCapacity(int count, string path)
    {
        if (count < 0 || count > Options.MaximumComponents - ComponentCount)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, path,
                $"Maximum GeoJSON component count of {Options.MaximumComponents} would be exceeded at {path}.");
            return false;
        }
        return true;
    }

    internal void ApplyLayoutToGeometryMetadata(GeoJsonGeometry geometry, CoordinateLayout layout)
    {
        if (geometry.Value != null) geometry.Value.CoordinateLayout = layout;
        int index = 0;
        while (index < geometry.Geometries.Length)
        {
            ApplyLayoutToGeometryMetadata(geometry.Geometries[index], layout);
            index++;
        }
    }

    internal bool ValidateBoundingBoxLayouts(
        GeoJsonGeometry geometry, CoordinateLayout layout, string path)
    {
        if (Options.Strict && !ValidateBoundingBoxDimension(
            geometry.BoundingBox, layout, path + ".bbox")) return false;
        int index = 0;
        while (index < geometry.Geometries.Length)
        {
            string childPath = path + ".geometries[" + index + "]";
            if (!ValidateBoundingBoxLayouts(geometry.Geometries[index], layout, childPath)) return false;
            index++;
        }
        return true;
    }

    internal bool ValidateBoundingBoxDimension(
        double[] boundingBox, CoordinateLayout layout, string path)
    {
        int expected = layout == CoordinateLayout.XYZ ? 6 : 4;
        if (boundingBox.Length == 0 || boundingBox.Length == expected) return true;
        SetFailure(ParseErrorKind.InvalidCoordinateLayout, 0, path,
            $"GeoJSON bbox dimensionality does not match geometry layout {layout} at {path}.");
        return false;
    }

    internal void ValidateOptions()
    {
        if (Options.MaximumCoordinates < 1)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, "$",
                "Maximum GeoJSON coordinate count must be positive at byte offset 0.");
            return;
        }
        if (Options.MaximumComponents < 1)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, "$",
                "Maximum GeoJSON component count must be positive at byte offset 0.");
            return;
        }
        if (Options.MaximumNestingDepth < 0)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, "$",
                "Maximum GeoJSON nesting depth cannot be negative at byte offset 0.");
            return;
        }
        if (Options.MaximumInputBytes < 1)
        {
            SetFailure(ParseErrorKind.LimitExceeded, 0, "$",
                "Maximum GeoJSON input size must be positive at byte offset 0.");
            return;
        }
        if (Source.Length > Options.MaximumInputBytes)
        {
            SetFailure(ParseErrorKind.LimitExceeded, Options.MaximumInputBytes, "$",
                $"Maximum GeoJSON input size of {Options.MaximumInputBytes} bytes was exceeded at byte offset {Options.MaximumInputBytes}.");
        }
    }

    internal void SetFailure(ParseErrorKind kind, int offset, string path, string message)
    {
        if (Failure != null) return;
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Offset = offset == 0 && Offsets.Count != 0 ?
            GeoJsonOffsetLocator.ResolveOffset(Offsets, path) : offset;
        error.JsonPath = path;
        string detailedMessage = message.Contains(path) ? message :
            message + " JSON path " + path + ".";
        error.Message = detailedMessage.Contains("byte offset") ? detailedMessage :
            detailedMessage + " The failure is at byte offset " + error.Offset + ".";
        Failure = error;
    }

    internal void SetFailureResult(GeoJsonReadResult result)
    {
        if (Failure == null)
        {
            SetFailure(ParseErrorKind.InvalidGeoJson, 0, "$", "GeoJSON parsing failed at byte offset 0.");
        }
        result.Error = Failure;
        result.Consumed = Failure == null ? 0 : (int)Failure.Offset;
    }

    internal static Geometry CreateGeometryShell(GeometryType type, CoordinateLayout layout)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = type;
        geometry.CoordinateLayout = layout;
        geometry.SpatialReferenceId = GeometryConstants.Crs84SpatialReferenceId;
        return geometry;
    }

    internal static GeoJsonGeometry CreateGeometryWrapper(GeometryType type, Geometry value)
    {
        GeoJsonGeometry geometry = new GeoJsonGeometry();
        geometry.GeometryType = type;
        geometry.Value = value;
        return geometry;
    }

    internal static bool PositionsAreEqual(
        Coordinate first, Coordinate second, CoordinateLayout layout)
    {
        if (first.X != second.X || first.Y != second.Y) return false;
        if (layout == CoordinateLayout.XYZ && first.Z != second.Z) return false;
        return true;
    }

    internal static Dictionary<string, JsonElement> CloneForeignMemberMap(
        Dictionary<string, JsonElement> source)
    {
        Dictionary<string, JsonElement> copy = new Dictionary<string, JsonElement>(source.Count);
        foreach (KeyValuePair<string, JsonElement> member in source)
        {
            copy.Add(member.Key, member.Value.Clone());
        }
        return copy;
    }

    internal static GeoJsonReadOptions CopyReadOptions(GeoJsonReadOptions source)
    {
        GeoJsonReadOptions copy = new GeoJsonReadOptions();
        copy.Strict = source.Strict;
        copy.PreserveForeignMembers = source.PreserveForeignMembers;
        copy.MaximumCoordinates = source.MaximumCoordinates;
        copy.MaximumComponents = source.MaximumComponents;
        copy.MaximumNestingDepth = source.MaximumNestingDepth;
        copy.MaximumInputBytes = source.MaximumInputBytes;
        return copy;
    }
}
