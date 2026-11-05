using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfAccessorSchemaReader
{
    internal GltfAccessor ReadAccessor(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAccessor result = new GltfAccessor();
        if (source.ContainsKey("bufferView"))
        {
            result.HasBufferView = true;
            result.BufferView = values.ReadInt32Value(
                source["bufferView"],
                values.PointerBuilder.AppendProperty(jsonPointer, "bufferView"));
        }

        if (source.ContainsKey("byteOffset"))
        {
            result.HasByteOffset = true;
            result.ByteOffset = values.ReadInt32Value(
                source["byteOffset"],
                values.PointerBuilder.AppendProperty(jsonPointer, "byteOffset"));
        }

        string componentPointer = values.PointerBuilder.AppendProperty(jsonPointer, "componentType");
        result.ComponentType = ReadComponentType(
            values.ReadRequiredNode(source, "componentType", jsonPointer),
            componentPointer,
            values);
        if (source.ContainsKey("normalized"))
        {
            result.HasNormalized = true;
            result.Normalized = values.ReadBooleanValue(
                source["normalized"],
                values.PointerBuilder.AppendProperty(jsonPointer, "normalized"));
        }

        result.Count = values.ReadInt32Value(
            values.ReadRequiredNode(source, "count", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "count"));
        result.Type = ReadAccessorType(
            values.ReadRequiredNode(source, "type", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "type"),
            values);
        if (source.ContainsKey("max"))
        {
            result.Maximum = values.ReadDoubleArrayValue(
                source["max"],
                values.PointerBuilder.AppendProperty(jsonPointer, "max"),
                -1);
        }

        if (source.ContainsKey("min"))
        {
            result.Minimum = values.ReadDoubleArrayValue(
                source["min"],
                values.PointerBuilder.AppendProperty(jsonPointer, "min"),
                -1);
        }

        if (source.ContainsKey("sparse"))
        {
            result.Sparse = ReadSparse(
                source["sparse"],
                values.PointerBuilder.AppendProperty(jsonPointer, "sparse"),
                values);
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
                "bufferView", "byteOffset", "componentType", "normalized", "count", "type",
                "max", "min", "sparse", "name"
            },
            jsonPointer);
        return result;
    }

    internal GltfAccessorSparse ReadSparse(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAccessorSparse result = new GltfAccessorSparse();
        result.Count = values.ReadInt32Value(
            values.ReadRequiredNode(source, "count", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "count"));
        result.Indices = ReadSparseIndices(
            values.ReadRequiredNode(source, "indices", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "indices"),
            values);
        result.Values = ReadSparseValues(
            values.ReadRequiredNode(source, "values", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "values"),
            values);
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "count", "indices", "values" },
            jsonPointer);
        return result;
    }

    internal GltfAccessorSparseIndices ReadSparseIndices(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAccessorSparseIndices result = new GltfAccessorSparseIndices();
        result.BufferView = values.ReadInt32Value(
            values.ReadRequiredNode(source, "bufferView", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "bufferView"));
        if (source.ContainsKey("byteOffset"))
        {
            result.HasByteOffset = true;
            result.ByteOffset = values.ReadInt32Value(
                source["byteOffset"],
                values.PointerBuilder.AppendProperty(jsonPointer, "byteOffset"));
        }

        result.ComponentType = ReadSparseComponentType(
            values.ReadRequiredNode(source, "componentType", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "componentType"),
            values);
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "bufferView", "byteOffset", "componentType" },
            jsonPointer);
        return result;
    }

    internal GltfAccessorSparseValues ReadSparseValues(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAccessorSparseValues result = new GltfAccessorSparseValues();
        result.BufferView = values.ReadInt32Value(
            values.ReadRequiredNode(source, "bufferView", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "bufferView"));
        if (source.ContainsKey("byteOffset"))
        {
            result.HasByteOffset = true;
            result.ByteOffset = values.ReadInt32Value(
                source["byteOffset"],
                values.PointerBuilder.AppendProperty(jsonPointer, "byteOffset"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "bufferView", "byteOffset" },
            jsonPointer);
        return result;
    }

    internal GltfComponentType ReadComponentType(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        switch (value)
        {
            case 5120:
                return GltfComponentType.SignedByte;
            case 5121:
                return GltfComponentType.UnsignedByte;
            case 5122:
                return GltfComponentType.SignedShort;
            case 5123:
                return GltfComponentType.UnsignedShort;
            case 5125:
                return GltfComponentType.UnsignedInt;
            case 5126:
                return GltfComponentType.Float;
            default:
                throw values.CreateSchemaError($"Unknown accessor component type {value}", jsonPointer);
        }
    }

    internal GltfComponentType ReadSparseComponentType(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        GltfComponentType value = ReadComponentType(node, jsonPointer, values);
        if (value != GltfComponentType.UnsignedByte
            && value != GltfComponentType.UnsignedShort
            && value != GltfComponentType.UnsignedInt)
        {
            throw values.CreateSchemaError("Sparse indices require an unsigned component type", jsonPointer);
        }

        return value;
    }

    internal GltfAccessorType ReadAccessorType(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        string value = values.ReadStringValue(node, jsonPointer);
        switch (value)
        {
            case "SCALAR":
                return GltfAccessorType.Scalar;
            case "VEC2":
                return GltfAccessorType.Vector2;
            case "VEC3":
                return GltfAccessorType.Vector3;
            case "VEC4":
                return GltfAccessorType.Vector4;
            case "MAT2":
                return GltfAccessorType.Matrix2;
            case "MAT3":
                return GltfAccessorType.Matrix3;
            case "MAT4":
                return GltfAccessorType.Matrix4;
            default:
                throw values.CreateSchemaError($"Unknown accessor type '{value}'", jsonPointer);
        }
    }
}
