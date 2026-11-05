using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfAnimationSchemaReader
{
    internal GltfAnimation ReadAnimation(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAnimation result = new GltfAnimation();
        string channelsPointer = values.PointerBuilder.AppendProperty(jsonPointer, "channels");
        JsonArray channels = values.ReadArrayValue(
            values.ReadRequiredNode(source, "channels", jsonPointer),
            channelsPointer);
        values.RequireNonEmptyCollection(channels.Count, channelsPointer);
        int index = 0;
        while (index < channels.Count)
        {
            result.Channels.Add(ReadAnimationChannel(
                channels[index],
                values.PointerBuilder.AppendIndex(channelsPointer, index),
                values));
            index++;
        }

        string samplersPointer = values.PointerBuilder.AppendProperty(jsonPointer, "samplers");
        JsonArray samplers = values.ReadArrayValue(
            values.ReadRequiredNode(source, "samplers", jsonPointer),
            samplersPointer);
        values.RequireNonEmptyCollection(samplers.Count, samplersPointer);
        index = 0;
        while (index < samplers.Count)
        {
            result.Samplers.Add(ReadAnimationSampler(
                samplers[index],
                values.PointerBuilder.AppendIndex(samplersPointer, index),
                values));
            index++;
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "channels", "samplers", "name" },
            jsonPointer);
        return result;
    }

    internal GltfAnimationSampler ReadAnimationSampler(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAnimationSampler result = new GltfAnimationSampler();
        result.Input = values.ReadInt32Value(
            values.ReadRequiredNode(source, "input", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "input"));
        if (source.ContainsKey("interpolation"))
        {
            result.HasInterpolation = true;
            result.Interpolation = ReadAnimationInterpolation(
                source["interpolation"],
                values.PointerBuilder.AppendProperty(jsonPointer, "interpolation"),
                values);
        }

        result.Output = values.ReadInt32Value(
            values.ReadRequiredNode(source, "output", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "output"));
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "input", "interpolation", "output" },
            jsonPointer);
        return result;
    }

    internal GltfAnimationChannel ReadAnimationChannel(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAnimationChannel result = new GltfAnimationChannel();
        result.Sampler = values.ReadInt32Value(
            values.ReadRequiredNode(source, "sampler", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "sampler"));
        result.Target = ReadAnimationTarget(
            values.ReadRequiredNode(source, "target", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "target"),
            values);
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "sampler", "target" },
            jsonPointer);
        return result;
    }

    internal GltfAnimationTarget ReadAnimationTarget(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfAnimationTarget result = new GltfAnimationTarget();
        if (source.ContainsKey("node"))
        {
            result.HasNode = true;
            result.Node = values.ReadInt32Value(
                source["node"],
                values.PointerBuilder.AppendProperty(jsonPointer, "node"));
        }

        result.Path = ReadAnimationPath(
            values.ReadRequiredNode(source, "path", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "path"),
            values);
        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "node", "path" },
            jsonPointer);
        return result;
    }

    internal GltfAnimationInterpolation ReadAnimationInterpolation(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        string value = values.ReadStringValue(node, jsonPointer);
        switch (value)
        {
            case "LINEAR":
                return GltfAnimationInterpolation.Linear;
            case "STEP":
                return GltfAnimationInterpolation.Step;
            case "CUBICSPLINE":
                return GltfAnimationInterpolation.CubicSpline;
            default:
                throw values.CreateSchemaError($"Unknown animation interpolation '{value}'", jsonPointer);
        }
    }

    internal GltfAnimationPath ReadAnimationPath(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        string value = values.ReadStringValue(node, jsonPointer);
        switch (value)
        {
            case "translation":
                return GltfAnimationPath.Translation;
            case "rotation":
                return GltfAnimationPath.Rotation;
            case "scale":
                return GltfAnimationPath.Scale;
            case "weights":
                return GltfAnimationPath.Weights;
            case "pointer":
                return GltfAnimationPath.Pointer;
            default:
                throw values.CreateSchemaError($"Unknown animation target path '{value}'", jsonPointer);
        }
    }
}
