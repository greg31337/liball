using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class CompressionExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal bool HandlesCompressionExtension(string name)
    {
        return name == GltfExtensionNames.KhrDracoMeshCompression
            || name == GltfExtensionNames.KhrMeshoptCompression
            || name == GltfExtensionNames.ExtMeshoptCompression;
    }

    internal GltfExtensionResult ReadCompressionExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        JsonObject? source = ReadExtensionObject(context, result);
        if (source == null)
        {
            return result;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (context.Name == GltfExtensionNames.KhrDracoMeshCompression)
        {
            typed.DracoMeshCompression = ReadDraco(source, context, result);
            AddBufferViewReference(
                context,
                result,
                typed.DracoMeshCompression.BufferView,
                "bufferView");
        }
        else if (context.ObjectLocation == "buffer")
        {
            GltfMeshoptCompressionBufferExtension value = ReadMeshoptBuffer(source, context, result);
            if (context.Name == GltfExtensionNames.KhrMeshoptCompression)
            {
                typed.KhrMeshoptCompressionBuffer = value;
            }
            else
            {
                typed.ExtMeshoptCompressionBuffer = value;
            }
        }
        else
        {
            GltfMeshoptCompressionBufferViewExtension value = ReadMeshoptBufferView(
                source,
                context,
                result);
            if (context.Name == GltfExtensionNames.KhrMeshoptCompression)
            {
                typed.KhrMeshoptCompressionBufferView = value;
            }
            else
            {
                typed.ExtMeshoptCompressionBufferView = value;
            }

            AddBufferReference(context, result, value.Buffer);
        }

        result.TypedData = typed;
        return result;
    }

    internal JsonObject? ReadExtensionObject(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        if (context.Json == null || context.Json.GetValueKind() != JsonValueKind.Object)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The compression extension payload must be a JSON object.",
                Fallback.CreateExtensionPointer(context));
            return null;
        }

        return context.Json.AsObject();
    }

    internal GltfDracoMeshCompressionExtension ReadDraco(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfDracoMeshCompressionExtension value = new GltfDracoMeshCompressionExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        value.BufferView = ReadRequiredNonNegativeInteger(
            source,
            "bufferView",
            result,
            pointer);
        JsonNode? attributesNode = source["attributes"];
        if (!source.ContainsKey("attributes")
            || attributesNode == null
            || attributesNode.GetValueKind() != JsonValueKind.Object)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Draco attributes must be a JSON object.",
                pointer + "/attributes");
        }
        else
        {
            JsonObject attributes = attributesNode.AsObject();
            if (attributes.Count == 0)
            {
                AddReadIssue(
                    result,
                    GltfIssueCodes.ArrayLengthNotInList,
                    "Draco attributes must not be empty.",
                    pointer + "/attributes");
            }

            foreach (KeyValuePair<string, JsonNode?> attribute in attributes)
            {
                value.Attributes[attribute.Key] = ReadNonNegativeInteger(
                    attribute.Value,
                    result,
                    pointer + "/attributes/" + Fallback.EscapePointerSegment(attribute.Key));
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["bufferView", "attributes"]);
        return value;
    }

    internal GltfMeshoptCompressionBufferExtension ReadMeshoptBuffer(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfMeshoptCompressionBufferExtension value = new GltfMeshoptCompressionBufferExtension();
        if (source.ContainsKey("fallback"))
        {
            value.HasFallback = true;
            value.Fallback = ReadBoolean(
                source["fallback"],
                result,
                Fallback.CreateExtensionPointer(context) + "/fallback");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["fallback"]);
        return value;
    }

    internal GltfMeshoptCompressionBufferViewExtension ReadMeshoptBufferView(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfMeshoptCompressionBufferViewExtension value = new GltfMeshoptCompressionBufferViewExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        value.Buffer = ReadRequiredNonNegativeInteger(source, "buffer", result, pointer);
        if (source.ContainsKey("byteOffset"))
        {
            value.HasByteOffset = true;
            value.ByteOffset = ReadNonNegativeInteger(
                source["byteOffset"],
                result,
                pointer + "/byteOffset");
        }

        value.ByteLength = ReadRequiredPositiveInteger(
            source,
            "byteLength",
            result,
            pointer);
        value.ByteStride = ReadRequiredPositiveInteger(
            source,
            "byteStride",
            result,
            pointer);
        value.Count = ReadRequiredPositiveInteger(
            source,
            "count",
            result,
            pointer);
        value.Mode = ReadRequiredString(source, "mode", result, pointer);
        if (source.ContainsKey("filter"))
        {
            value.HasFilter = true;
            value.Filter = ReadString(
                source["filter"],
                result,
                pointer + "/filter");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["buffer", "byteOffset", "byteLength", "byteStride", "count", "mode", "filter"]);
        return value;
    }

    internal int ReadRequiredNonNegativeInteger(
        JsonObject source,
        string name,
        GltfExtensionResult result,
        string pointer)
    {
        if (!source.ContainsKey(name))
        {
            AddReadIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                $"The required {name} member is missing.",
                pointer + "/" + name);
            return GltfConstants.AbsentIndex;
        }

        return ReadNonNegativeInteger(
            source[name],
            result,
            pointer + "/" + name);
    }

    internal int ReadRequiredPositiveInteger(
        JsonObject source,
        string name,
        GltfExtensionResult result,
        string pointer)
    {
        int value = ReadRequiredNonNegativeInteger(source, name, result, pointer);
        if (value == 0)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                $"The {name} member must be positive.",
                pointer + "/" + name);
        }

        return value;
    }

    internal int ReadNonNegativeInteger(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddReadIssue(result, GltfIssueCodes.TypeMismatch, "Expected an integer.", pointer);
            return GltfConstants.AbsentIndex;
        }

        double value = double.Parse(
            node.ToJsonString(),
            System.Globalization.CultureInfo.InvariantCulture);
        if (!double.IsFinite(value)
            || value != System.Math.Truncate(value)
            || value < 0.0
            || value > int.MaxValue)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Expected a non-negative 32-bit integer.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        return (int)value;
    }

    internal string ReadRequiredString(
        JsonObject source,
        string name,
        GltfExtensionResult result,
        string pointer)
    {
        if (!source.ContainsKey(name))
        {
            AddReadIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                $"The required {name} member is missing.",
                pointer + "/" + name);
            return "";
        }

        return ReadString(source[name], result, pointer + "/" + name);
    }

    internal string ReadString(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.String)
        {
            AddReadIssue(result, GltfIssueCodes.TypeMismatch, "Expected a string.", pointer);
            return "";
        }

        return node.GetValue<string>();
    }

    internal bool ReadBoolean(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null
            || (node.GetValueKind() != JsonValueKind.True
                && node.GetValueKind() != JsonValueKind.False))
        {
            AddReadIssue(result, GltfIssueCodes.TypeMismatch, "Expected a boolean.", pointer);
            return false;
        }

        return node.GetValue<bool>();
    }

    internal void AddBufferViewReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int bufferView,
        string member)
    {
        if (bufferView < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = context.ObjectLocation;
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/" + context.Name + "/" + member;
        reference.TargetCollection = "bufferViews";
        reference.TargetIndex = bufferView;
        result.References.Add(reference);
    }

    internal void AddBufferReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int buffer)
    {
        if (buffer < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "bufferViews";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/" + context.Name + "/buffer";
        reference.TargetCollection = "buffers";
        reference.TargetIndex = buffer;
        result.References.Add(reference);
    }

    internal void AddReadIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
