using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfSceneSchemaReader
{
    internal GltfCamera ReadCamera(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfCamera result = new GltfCamera();
        result.Type = ReadCameraType(
            values.ReadRequiredNode(source, "type", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "type"),
            values);
        if (source.ContainsKey("perspective"))
        {
            result.Perspective = ReadPerspectiveCamera(
                source["perspective"],
                values.PointerBuilder.AppendProperty(jsonPointer, "perspective"),
                values);
        }

        if (source.ContainsKey("orthographic"))
        {
            result.Orthographic = ReadOrthographicCamera(
                source["orthographic"],
                values.PointerBuilder.AppendProperty(jsonPointer, "orthographic"),
                values);
        }

        if (result.Type == GltfCameraType.Perspective && result.Perspective == null)
        {
            throw values.CreateSchemaError("A perspective camera requires 'perspective' parameters", jsonPointer);
        }

        if (result.Type == GltfCameraType.Orthographic && result.Orthographic == null)
        {
            throw values.CreateSchemaError("An orthographic camera requires 'orthographic' parameters", jsonPointer);
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "type", "perspective", "orthographic", "name" },
            jsonPointer);
        return result;
    }

    internal GltfPerspectiveCamera ReadPerspectiveCamera(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfPerspectiveCamera result = new GltfPerspectiveCamera();
        if (source.ContainsKey("aspectRatio"))
        {
            result.HasAspectRatio = true;
            result.AspectRatio = values.ReadDoubleValue(
                source["aspectRatio"],
                values.PointerBuilder.AppendProperty(jsonPointer, "aspectRatio"));
        }

        result.VerticalFieldOfView = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "yfov", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "yfov"));
        if (source.ContainsKey("zfar"))
        {
            result.HasFarPlane = true;
            result.FarPlane = values.ReadDoubleValue(
                source["zfar"],
                values.PointerBuilder.AppendProperty(jsonPointer, "zfar"));
        }

        result.NearPlane = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "znear", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "znear"));
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "aspectRatio", "yfov", "zfar", "znear" },
            jsonPointer);
        return result;
    }

    internal GltfOrthographicCamera ReadOrthographicCamera(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfOrthographicCamera result = new GltfOrthographicCamera();
        result.HorizontalMagnification = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "xmag", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "xmag"));
        result.VerticalMagnification = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "ymag", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "ymag"));
        result.FarPlane = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "zfar", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "zfar"));
        result.NearPlane = values.ReadDoubleValue(
            values.ReadRequiredNode(source, "znear", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "znear"));
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "xmag", "ymag", "zfar", "znear" },
            jsonPointer);
        return result;
    }

    internal GltfMesh ReadMesh(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfMesh result = new GltfMesh();
        string primitivesPointer = values.PointerBuilder.AppendProperty(jsonPointer, "primitives");
        JsonArray primitives = values.ReadArrayValue(
            values.ReadRequiredNode(source, "primitives", jsonPointer),
            primitivesPointer);
        values.RequireNonEmptyCollection(primitives.Count, primitivesPointer);
        int index = 0;
        while (index < primitives.Count)
        {
            result.Primitives.Add(ReadMeshPrimitive(
                primitives[index],
                values.PointerBuilder.AppendIndex(primitivesPointer, index),
                values));
            index++;
        }

        if (source.ContainsKey("weights"))
        {
            result.Weights = values.ReadDoubleArrayValue(
                source["weights"],
                values.PointerBuilder.AppendProperty(jsonPointer, "weights"),
                -1);
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "primitives", "weights", "name" },
            jsonPointer);
        return result;
    }

    internal GltfMeshPrimitive ReadMeshPrimitive(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfMeshPrimitive result = new GltfMeshPrimitive();
        string attributesPointer = values.PointerBuilder.AppendProperty(jsonPointer, "attributes");
        JsonObject attributes = values.ReadObjectValue(
            values.ReadRequiredNode(source, "attributes", jsonPointer),
            attributesPointer);
        values.RequireNonEmptyCollection(attributes.Count, attributesPointer);
        foreach (KeyValuePair<string, JsonNode?> attribute in attributes)
        {
            string attributePointer = values.PointerBuilder.AppendProperty(attributesPointer, attribute.Key);
            result.Attributes.Add(ReadAttribute(attribute.Key, attribute.Value, attributePointer, values));
        }

        if (source.ContainsKey("indices"))
        {
            result.HasIndices = true;
            result.Indices = values.ReadInt32Value(
                source["indices"],
                values.PointerBuilder.AppendProperty(jsonPointer, "indices"));
        }

        if (source.ContainsKey("material"))
        {
            result.HasMaterial = true;
            result.Material = values.ReadInt32Value(
                source["material"],
                values.PointerBuilder.AppendProperty(jsonPointer, "material"));
        }

        if (source.ContainsKey("mode"))
        {
            result.HasMode = true;
            result.Mode = ReadPrimitiveMode(
                source["mode"],
                values.PointerBuilder.AppendProperty(jsonPointer, "mode"),
                values);
        }

        if (source.ContainsKey("targets"))
        {
            string targetsPointer = values.PointerBuilder.AppendProperty(jsonPointer, "targets");
            JsonArray targets = values.ReadArrayValue(source["targets"], targetsPointer);
            int index = 0;
            while (index < targets.Count)
            {
                result.Targets.Add(ReadMorphTarget(
                    targets[index],
                    values.PointerBuilder.AppendIndex(targetsPointer, index),
                    values));
                index++;
            }
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "attributes", "indices", "material", "mode", "targets" },
            jsonPointer);
        return result;
    }

    internal GltfAttribute ReadAttribute(
        string semantic,
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        GltfAttribute result = new GltfAttribute();
        result.Semantic = semantic;
        result.Accessor = values.ReadInt32Value(node, jsonPointer);
        return result;
    }

    internal GltfMorphTarget ReadMorphTarget(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        values.RequireNonEmptyCollection(source.Count, jsonPointer);
        GltfMorphTarget result = new GltfMorphTarget();
        foreach (KeyValuePair<string, JsonNode?> attribute in source)
        {
            string attributePointer = values.PointerBuilder.AppendProperty(jsonPointer, attribute.Key);
            result.Attributes.Add(ReadAttribute(attribute.Key, attribute.Value, attributePointer, values));
        }

        return result;
    }

    internal GltfNode ReadNode(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfNode result = new GltfNode();
        if (source.ContainsKey("children"))
        {
            result.Children = values.ReadInt32ListValue(
                source["children"],
                values.PointerBuilder.AppendProperty(jsonPointer, "children"));
        }

        if (source.ContainsKey("camera"))
        {
            result.HasCamera = true;
            result.Camera = values.ReadInt32Value(
                source["camera"],
                values.PointerBuilder.AppendProperty(jsonPointer, "camera"));
        }

        if (source.ContainsKey("skin"))
        {
            result.HasSkin = true;
            result.Skin = values.ReadInt32Value(
                source["skin"],
                values.PointerBuilder.AppendProperty(jsonPointer, "skin"));
        }

        if (source.ContainsKey("mesh"))
        {
            result.HasMesh = true;
            result.Mesh = values.ReadInt32Value(
                source["mesh"],
                values.PointerBuilder.AppendProperty(jsonPointer, "mesh"));
        }

        if (source.ContainsKey("matrix"))
        {
            result.HasMatrix = true;
            result.Matrix = values.ReadDoubleArrayValue(
                source["matrix"],
                values.PointerBuilder.AppendProperty(jsonPointer, "matrix"),
                16);
        }

        if (source.ContainsKey("rotation"))
        {
            result.HasRotation = true;
            result.Rotation = values.ReadDoubleArrayValue(
                source["rotation"],
                values.PointerBuilder.AppendProperty(jsonPointer, "rotation"),
                4);
        }

        if (source.ContainsKey("scale"))
        {
            result.HasScale = true;
            result.Scale = values.ReadDoubleArrayValue(
                source["scale"],
                values.PointerBuilder.AppendProperty(jsonPointer, "scale"),
                3);
        }

        if (source.ContainsKey("translation"))
        {
            result.HasTranslation = true;
            result.Translation = values.ReadDoubleArrayValue(
                source["translation"],
                values.PointerBuilder.AppendProperty(jsonPointer, "translation"),
                3);
        }

        if (source.ContainsKey("weights"))
        {
            result.Weights = values.ReadDoubleArrayValue(
                source["weights"],
                values.PointerBuilder.AppendProperty(jsonPointer, "weights"),
                -1);
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string>
            {
                "children", "camera", "skin", "mesh", "matrix", "rotation", "scale",
                "translation", "weights", "name"
            },
            jsonPointer);
        return result;
    }

    internal GltfScene ReadScene(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfScene result = new GltfScene();
        if (source.ContainsKey("nodes"))
        {
            result.Nodes = values.ReadInt32ListValue(
                source["nodes"],
                values.PointerBuilder.AppendProperty(jsonPointer, "nodes"));
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "nodes", "name" },
            jsonPointer);
        return result;
    }

    internal GltfSkin ReadSkin(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfSkin result = new GltfSkin();
        if (source.ContainsKey("inverseBindMatrices"))
        {
            result.HasInverseBindMatrices = true;
            result.InverseBindMatrices = values.ReadInt32Value(
                source["inverseBindMatrices"],
                values.PointerBuilder.AppendProperty(jsonPointer, "inverseBindMatrices"));
        }

        if (source.ContainsKey("skeleton"))
        {
            result.HasSkeleton = true;
            result.Skeleton = values.ReadInt32Value(
                source["skeleton"],
                values.PointerBuilder.AppendProperty(jsonPointer, "skeleton"));
        }

        string jointsPointer = values.PointerBuilder.AppendProperty(jsonPointer, "joints");
        result.Joints = values.ReadInt32ListValue(
            values.ReadRequiredNode(source, "joints", jsonPointer),
            jointsPointer);
        values.RequireNonEmptyCollection(result.Joints.Count, jointsPointer);
        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "inverseBindMatrices", "skeleton", "joints", "name" },
            jsonPointer);
        return result;
    }

    internal GltfCameraType ReadCameraType(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        string value = values.ReadStringValue(node, jsonPointer);
        if (value == "perspective")
        {
            return GltfCameraType.Perspective;
        }

        if (value == "orthographic")
        {
            return GltfCameraType.Orthographic;
        }

        throw values.CreateSchemaError($"Unknown camera type '{value}'", jsonPointer);
    }

    internal GltfPrimitiveMode ReadPrimitiveMode(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        switch (value)
        {
            case 0:
                return GltfPrimitiveMode.Points;
            case 1:
                return GltfPrimitiveMode.Lines;
            case 2:
                return GltfPrimitiveMode.LineLoop;
            case 3:
                return GltfPrimitiveMode.LineStrip;
            case 4:
                return GltfPrimitiveMode.Triangles;
            case 5:
                return GltfPrimitiveMode.TriangleStrip;
            case 6:
                return GltfPrimitiveMode.TriangleFan;
            default:
                throw values.CreateSchemaError($"Unknown primitive mode {value}", jsonPointer);
        }
    }
}
