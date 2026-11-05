using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfSchemaReader
{
    internal GltfSchemaValueReader Values = new GltfSchemaValueReader();
    internal GltfAccessorSchemaReader Accessors = new GltfAccessorSchemaReader();
    internal GltfAnimationSchemaReader Animations = new GltfAnimationSchemaReader();
    internal GltfMaterialSchemaReader Materials = new GltfMaterialSchemaReader();
    internal GltfResourceSchemaReader Resources = new GltfResourceSchemaReader();
    internal GltfSceneSchemaReader Scenes = new GltfSceneSchemaReader();

    internal GltfDocument ReadDocument(
        GltfJsonReadResult json,
        GltfReadLimits limits,
        bool preserveUnknownMembers,
        List<IGltfExtensionHandler> extensionHandlers)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        Values.Configure(json, limits, preserveUnknownMembers, extensionHandlers);
        if (!json.HasRoot)
        {
            throw Values.CreateSchemaError("JSON input does not contain a root value", "");
        }

        JsonObject source = Values.ReadObjectValue(json.Root, "");
        GltfDocument result = new GltfDocument();
        result.Asset = ReadAsset(
            Values.ReadRequiredNode(source, "asset", ""),
            "/asset");
        if (source.ContainsKey("accessors"))
        {
            result.Accessors = ReadAccessorCollection(source["accessors"], "/accessors");
        }

        if (source.ContainsKey("animations"))
        {
            result.Animations = ReadAnimationCollection(source["animations"], "/animations");
        }

        if (source.ContainsKey("buffers"))
        {
            result.Buffers = ReadBufferCollection(source["buffers"], "/buffers");
        }

        if (source.ContainsKey("bufferViews"))
        {
            result.BufferViews = ReadBufferViewCollection(source["bufferViews"], "/bufferViews");
        }

        if (source.ContainsKey("cameras"))
        {
            result.Cameras = ReadCameraCollection(source["cameras"], "/cameras");
        }

        if (source.ContainsKey("images"))
        {
            result.Images = ReadImageCollection(source["images"], "/images");
        }

        if (source.ContainsKey("materials"))
        {
            result.Materials = ReadMaterialCollection(source["materials"], "/materials");
        }

        if (source.ContainsKey("meshes"))
        {
            result.Meshes = ReadMeshCollection(source["meshes"], "/meshes");
        }

        if (source.ContainsKey("nodes"))
        {
            result.Nodes = ReadNodeCollection(source["nodes"], "/nodes");
        }

        if (source.ContainsKey("samplers"))
        {
            result.Samplers = ReadSamplerCollection(source["samplers"], "/samplers");
        }

        if (source.ContainsKey("scenes"))
        {
            result.Scenes = ReadSceneCollection(source["scenes"], "/scenes");
        }

        if (source.ContainsKey("skins"))
        {
            result.Skins = ReadSkinCollection(source["skins"], "/skins");
        }

        if (source.ContainsKey("textures"))
        {
            result.Textures = ReadTextureCollection(source["textures"], "/textures");
        }

        if (source.ContainsKey("scene"))
        {
            result.HasDefaultScene = true;
            result.DefaultScene = Values.ReadInt32Value(source["scene"], "/scene");
        }

        if (source.ContainsKey("extensionsUsed"))
        {
            result.ExtensionsUsed = Values.ReadStringListValue(source["extensionsUsed"], "/extensionsUsed");
            Values.RequireNonEmptyCollection(result.ExtensionsUsed.Count, "/extensionsUsed");
        }

        if (source.ContainsKey("extensionsRequired"))
        {
            result.ExtensionsRequired = Values.ReadStringListValue(
                source["extensionsRequired"],
                "/extensionsRequired");
            Values.RequireNonEmptyCollection(result.ExtensionsRequired.Count, "/extensionsRequired");
        }

        result.PropertyJson = Values.ReadPropertyJson(
            source,
            new HashSet<string>
            {
                "asset", "accessors", "animations", "buffers", "bufferViews", "cameras",
                "images", "materials", "meshes", "nodes", "samplers", "scenes", "skins",
                "textures", "scene", "extensionsUsed", "extensionsRequired"
            },
            "");
        return result;
    }

    internal GltfAsset ReadAsset(JsonNode? node, string jsonPointer)
    {
        JsonObject source = Values.ReadObjectValue(node, jsonPointer);
        GltfAsset result = new GltfAsset();
        result.Version = Values.ReadStringValue(
            Values.ReadRequiredNode(source, "version", jsonPointer),
            Values.PointerBuilder.AppendProperty(jsonPointer, "version"));
        if (source.ContainsKey("minVersion"))
        {
            result.MinimumVersion = Values.ReadStringValue(
                source["minVersion"],
                Values.PointerBuilder.AppendProperty(jsonPointer, "minVersion"));
        }

        if (source.ContainsKey("generator"))
        {
            result.Generator = Values.ReadStringValue(
                source["generator"],
                Values.PointerBuilder.AppendProperty(jsonPointer, "generator"));
        }

        if (source.ContainsKey("copyright"))
        {
            result.Copyright = Values.ReadStringValue(
                source["copyright"],
                Values.PointerBuilder.AppendProperty(jsonPointer, "copyright"));
        }

        result.PropertyJson = Values.ReadPropertyJson(
            source,
            new HashSet<string> { "version", "minVersion", "generator", "copyright" },
            jsonPointer);
        return result;
    }

    internal List<GltfAccessor> ReadAccessorCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfAccessor> result = new List<GltfAccessor>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Accessors.ReadAccessor(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfAnimation> ReadAnimationCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfAnimation> result = new List<GltfAnimation>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Animations.ReadAnimation(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfBuffer> ReadBufferCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfBuffer> result = new List<GltfBuffer>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Resources.ReadBuffer(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfBufferView> ReadBufferViewCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfBufferView> result = new List<GltfBufferView>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Resources.ReadBufferView(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfCamera> ReadCameraCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfCamera> result = new List<GltfCamera>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Scenes.ReadCamera(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfImage> ReadImageCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfImage> result = new List<GltfImage>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Resources.ReadImage(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfMaterial> ReadMaterialCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfMaterial> result = new List<GltfMaterial>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Materials.ReadMaterial(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfMesh> ReadMeshCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfMesh> result = new List<GltfMesh>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Scenes.ReadMesh(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfNode> ReadNodeCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfNode> result = new List<GltfNode>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Scenes.ReadNode(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfSampler> ReadSamplerCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfSampler> result = new List<GltfSampler>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Resources.ReadSampler(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfScene> ReadSceneCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfScene> result = new List<GltfScene>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Scenes.ReadScene(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfSkin> ReadSkinCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfSkin> result = new List<GltfSkin>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Scenes.ReadSkin(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }

    internal List<GltfTexture> ReadTextureCollection(JsonNode? node, string jsonPointer)
    {
        JsonArray source = Values.ReadArrayValue(node, jsonPointer);
        Values.RequireNonEmptyCollection(source.Count, jsonPointer);
        List<GltfTexture> result = new List<GltfTexture>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(Resources.ReadTexture(
                source[index],
                Values.PointerBuilder.AppendIndex(jsonPointer, index),
                Values));
            index++;
        }

        return result;
    }
}
