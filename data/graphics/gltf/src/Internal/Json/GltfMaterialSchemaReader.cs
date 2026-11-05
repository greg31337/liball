using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfMaterialSchemaReader
{
    internal GltfMaterial ReadMaterial(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfMaterial result = new GltfMaterial();
        if (source.ContainsKey("pbrMetallicRoughness"))
        {
            result.PbrMetallicRoughness = ReadPbrMetallicRoughness(
                source["pbrMetallicRoughness"],
                values.PointerBuilder.AppendProperty(jsonPointer, "pbrMetallicRoughness"),
                values);
        }

        if (source.ContainsKey("normalTexture"))
        {
            result.NormalTexture = ReadNormalTextureInfo(
                source["normalTexture"],
                values.PointerBuilder.AppendProperty(jsonPointer, "normalTexture"),
                values);
        }

        if (source.ContainsKey("occlusionTexture"))
        {
            result.OcclusionTexture = ReadOcclusionTextureInfo(
                source["occlusionTexture"],
                values.PointerBuilder.AppendProperty(jsonPointer, "occlusionTexture"),
                values);
        }

        if (source.ContainsKey("emissiveTexture"))
        {
            result.EmissiveTexture = ReadTextureInfo(
                source["emissiveTexture"],
                values.PointerBuilder.AppendProperty(jsonPointer, "emissiveTexture"),
                values);
        }

        if (source.ContainsKey("emissiveFactor"))
        {
            result.HasEmissiveFactor = true;
            result.EmissiveFactor = values.ReadDoubleArrayValue(
                source["emissiveFactor"],
                values.PointerBuilder.AppendProperty(jsonPointer, "emissiveFactor"),
                3);
        }

        if (source.ContainsKey("alphaMode"))
        {
            result.HasAlphaMode = true;
            result.AlphaMode = ReadAlphaMode(
                source["alphaMode"],
                values.PointerBuilder.AppendProperty(jsonPointer, "alphaMode"),
                values);
        }

        if (source.ContainsKey("alphaCutoff"))
        {
            result.HasAlphaCutoff = true;
            result.AlphaCutoff = values.ReadDoubleValue(
                source["alphaCutoff"],
                values.PointerBuilder.AppendProperty(jsonPointer, "alphaCutoff"));
        }

        if (source.ContainsKey("doubleSided"))
        {
            result.HasDoubleSided = true;
            result.DoubleSided = values.ReadBooleanValue(
                source["doubleSided"],
                values.PointerBuilder.AppendProperty(jsonPointer, "doubleSided"));
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
                "pbrMetallicRoughness", "normalTexture", "occlusionTexture", "emissiveTexture",
                "emissiveFactor", "alphaMode", "alphaCutoff", "doubleSided", "name"
            },
            jsonPointer);
        return result;
    }

    internal GltfPbrMetallicRoughness ReadPbrMetallicRoughness(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfPbrMetallicRoughness result = new GltfPbrMetallicRoughness();
        if (source.ContainsKey("baseColorFactor"))
        {
            result.HasBaseColorFactor = true;
            result.BaseColorFactor = values.ReadDoubleArrayValue(
                source["baseColorFactor"],
                values.PointerBuilder.AppendProperty(jsonPointer, "baseColorFactor"),
                4);
        }

        if (source.ContainsKey("baseColorTexture"))
        {
            result.BaseColorTexture = ReadTextureInfo(
                source["baseColorTexture"],
                values.PointerBuilder.AppendProperty(jsonPointer, "baseColorTexture"),
                values);
        }

        if (source.ContainsKey("metallicFactor"))
        {
            result.HasMetallicFactor = true;
            result.MetallicFactor = values.ReadDoubleValue(
                source["metallicFactor"],
                values.PointerBuilder.AppendProperty(jsonPointer, "metallicFactor"));
        }

        if (source.ContainsKey("roughnessFactor"))
        {
            result.HasRoughnessFactor = true;
            result.RoughnessFactor = values.ReadDoubleValue(
                source["roughnessFactor"],
                values.PointerBuilder.AppendProperty(jsonPointer, "roughnessFactor"));
        }

        if (source.ContainsKey("metallicRoughnessTexture"))
        {
            result.MetallicRoughnessTexture = ReadTextureInfo(
                source["metallicRoughnessTexture"],
                values.PointerBuilder.AppendProperty(jsonPointer, "metallicRoughnessTexture"),
                values);
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string>
            {
                "baseColorFactor", "baseColorTexture", "metallicFactor", "roughnessFactor",
                "metallicRoughnessTexture"
            },
            jsonPointer);
        return result;
    }

    internal GltfTextureInfo ReadTextureInfo(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfTextureInfo result = new GltfTextureInfo();
        result.Index = values.ReadInt32Value(
            values.ReadRequiredNode(source, "index", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "index"));
        if (source.ContainsKey("texCoord"))
        {
            result.HasTextureCoordinate = true;
            result.TextureCoordinate = values.ReadInt32Value(
                source["texCoord"],
                values.PointerBuilder.AppendProperty(jsonPointer, "texCoord"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "index", "texCoord" },
            jsonPointer);
        return result;
    }

    internal GltfNormalTextureInfo ReadNormalTextureInfo(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfNormalTextureInfo result = new GltfNormalTextureInfo();
        result.Index = values.ReadInt32Value(
            values.ReadRequiredNode(source, "index", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "index"));
        if (source.ContainsKey("texCoord"))
        {
            result.HasTextureCoordinate = true;
            result.TextureCoordinate = values.ReadInt32Value(
                source["texCoord"],
                values.PointerBuilder.AppendProperty(jsonPointer, "texCoord"));
        }

        if (source.ContainsKey("scale"))
        {
            result.HasScale = true;
            result.Scale = values.ReadDoubleValue(
                source["scale"],
                values.PointerBuilder.AppendProperty(jsonPointer, "scale"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "index", "texCoord", "scale" },
            jsonPointer);
        return result;
    }

    internal GltfOcclusionTextureInfo ReadOcclusionTextureInfo(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfOcclusionTextureInfo result = new GltfOcclusionTextureInfo();
        result.Index = values.ReadInt32Value(
            values.ReadRequiredNode(source, "index", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "index"));
        if (source.ContainsKey("texCoord"))
        {
            result.HasTextureCoordinate = true;
            result.TextureCoordinate = values.ReadInt32Value(
                source["texCoord"],
                values.PointerBuilder.AppendProperty(jsonPointer, "texCoord"));
        }

        if (source.ContainsKey("strength"))
        {
            result.HasStrength = true;
            result.Strength = values.ReadDoubleValue(
                source["strength"],
                values.PointerBuilder.AppendProperty(jsonPointer, "strength"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "index", "texCoord", "strength" },
            jsonPointer);
        return result;
    }

    internal GltfAlphaMode ReadAlphaMode(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        string value = values.ReadStringValue(node, jsonPointer);
        switch (value)
        {
            case "OPAQUE":
                return GltfAlphaMode.Opaque;
            case "MASK":
                return GltfAlphaMode.Mask;
            case "BLEND":
                return GltfAlphaMode.Blend;
            default:
                throw values.CreateSchemaError($"Unknown material alpha mode '{value}'", jsonPointer);
        }
    }
}
