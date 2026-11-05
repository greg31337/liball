using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal JsonNode? WriteSceneExtension(
        GltfExtensionContext context,
        GltfTypedExtensionData typed)
    {
        if (context.Name == GltfExtensionNames.KhrAnimationPointer
            && typed.AnimationPointer != null)
        {
            return WriteAnimationPointer(typed.AnimationPointer);
        }

        if (context.Name == GltfExtensionNames.KhrTextureTransform
            && typed.TextureTransform != null)
        {
            return WriteTextureTransform(typed.TextureTransform);
        }

        if (context.Name == GltfExtensionNames.KhrLightsPunctual)
        {
            if (context.ObjectLocation == "node" && typed.PunctualLightNode != null)
            {
                return WritePunctualLightNode(typed.PunctualLightNode);
            }

            if (typed.LightsPunctual != null)
            {
                return WritePunctualLights(typed.LightsPunctual);
            }
        }

        if (context.Name == GltfExtensionNames.KhrNodeVisibility
            && typed.NodeVisibility != null)
        {
            return WriteNodeVisibility(typed.NodeVisibility);
        }

        if (context.Name == GltfExtensionNames.KhrNodeHoverability
            && typed.NodeHoverability != null)
        {
            return WriteNodeHoverability(typed.NodeHoverability);
        }

        if (context.Name == GltfExtensionNames.KhrNodeSelectability
            && typed.NodeSelectability != null)
        {
            return WriteNodeSelectability(typed.NodeSelectability);
        }

        if (context.Name == GltfExtensionNames.ExtMeshGpuInstancing
            && typed.MeshGpuInstancing != null)
        {
            return WriteMeshGpuInstancing(typed.MeshGpuInstancing);
        }

        return null;
    }

    internal JsonObject WriteAnimationPointer(GltfAnimationPointerExtension source)
    {
        JsonObject result = new JsonObject();
        result["pointer"] = JsonValue.Create(source.Pointer);
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteTextureTransform(GltfTextureTransformExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasOffset)
        {
            result["offset"] = WriteDoubleArray(source.Offset);
        }

        if (source.HasRotation)
        {
            result["rotation"] = JsonValue.Create(source.Rotation);
        }

        if (source.HasScale)
        {
            result["scale"] = WriteDoubleArray(source.Scale);
        }

        if (source.HasTextureCoordinate)
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePunctualLights(GltfLightsPunctualExtension source)
    {
        JsonObject result = new JsonObject();
        JsonArray lights = new JsonArray();
        int index = 0;
        while (index < source.Lights.Count)
        {
            lights.Add(WritePunctualLight(source.Lights[index]));
            index++;
        }

        result["lights"] = lights;
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePunctualLight(GltfPunctualLight source)
    {
        JsonObject result = new JsonObject();
        if (source.HasColor)
        {
            result["color"] = WriteDoubleArray(source.Color);
        }

        if (source.HasIntensity)
        {
            result["intensity"] = JsonValue.Create(source.Intensity);
        }

        if (source.Spot != null)
        {
            result["spot"] = WritePunctualLightSpot(source.Spot);
        }

        result["type"] = JsonValue.Create(source.Type);
        if (source.HasRange)
        {
            result["range"] = JsonValue.Create(source.Range);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePunctualLightSpot(GltfPunctualLightSpot source)
    {
        JsonObject result = new JsonObject();
        if (source.HasInnerConeAngle)
        {
            result["innerConeAngle"] = JsonValue.Create(source.InnerConeAngle);
        }

        if (source.HasOuterConeAngle)
        {
            result["outerConeAngle"] = JsonValue.Create(source.OuterConeAngle);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePunctualLightNode(GltfPunctualLightNodeExtension source)
    {
        JsonObject result = new JsonObject();
        result["light"] = JsonValue.Create(source.Light);
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteNodeVisibility(GltfNodeVisibilityExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasVisible)
        {
            result["visible"] = JsonValue.Create(source.Visible);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteNodeHoverability(GltfNodeHoverabilityExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasHoverable)
        {
            result["hoverable"] = JsonValue.Create(source.Hoverable);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteNodeSelectability(GltfNodeSelectabilityExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSelectable)
        {
            result["selectable"] = JsonValue.Create(source.Selectable);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteMeshGpuInstancing(GltfMeshGpuInstancingExtension source)
    {
        JsonObject result = new JsonObject();
        JsonObject attributes = new JsonObject();
        List<string> names = PropertyJson.GetSortedIndexKeys(source.Attributes);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            attributes[name] = JsonValue.Create(source.Attributes[name]);
            index++;
        }

        result["attributes"] = attributes;
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonArray WriteDoubleArray(double[] source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Length)
        {
            result.Add(JsonValue.Create(source[index]));
            index++;
        }

        return result;
    }
}
