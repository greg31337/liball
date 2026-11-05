using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class XmpJsonLdExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal bool HandlesXmpJsonLdExtension(string name)
    {
        return name == GltfExtensionNames.KhrXmpJsonLd;
    }

    internal GltfExtensionResult ReadXmpJsonLdExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        if (context.Json == null || context.Json.GetValueKind() != JsonValueKind.Object)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The XMP JSON-LD extension payload must be a JSON object.",
                Fallback.CreateExtensionPointer(context));
            return result;
        }

        JsonObject source = context.Json.AsObject();
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (context.ObjectLocation == "glTF")
        {
            typed.XmpJsonLd = ReadRoot(source, context, result);
        }
        else
        {
            typed.XmpJsonLdAssignment = ReadAssignment(source, context, result);
            AddPacketReference(context, result, typed.XmpJsonLdAssignment.Packet);
        }

        result.TypedData = typed;
        return result;
    }

    internal GltfXmpJsonLdExtension ReadRoot(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfXmpJsonLdExtension value = new GltfXmpJsonLdExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        value.HasPackets = source.ContainsKey("packets");
        JsonNode? packetsNode = source["packets"];
        if (!value.HasPackets)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The root XMP JSON-LD extension must define packets.",
                pointer + "/packets");
        }
        else if (packetsNode == null || packetsNode.GetValueKind() != JsonValueKind.Array)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                "The XMP JSON-LD packets member must be an array.",
                pointer + "/packets");
        }
        else
        {
            ReadPackets(packetsNode.AsArray(), value, result, pointer + "/packets");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["packets"]);
        return value;
    }

    internal void ReadPackets(
        JsonArray source,
        GltfXmpJsonLdExtension target,
        GltfExtensionResult result,
        string pointer)
    {
        int index = 0;
        while (index < source.Count)
        {
            JsonNode? packetNode = source[index];
            GltfXmpJsonLdPacket packet = new GltfXmpJsonLdPacket();
            target.Packets.Add(packet);
            if (packetNode == null || packetNode.GetValueKind() != JsonValueKind.Object)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    "Each XMP JSON-LD packet must be a JSON object.",
                    pointer + "/" + index);
            }
            else
            {
                ReadPacket(packetNode.AsObject(), packet, result, pointer + "/" + index);
            }

            index++;
        }
    }

    internal void ReadPacket(
        JsonObject source,
        GltfXmpJsonLdPacket target,
        GltfExtensionResult result,
        string pointer)
    {
        target.HasContext = source.ContainsKey("@context");
        JsonNode? contextNode = source["@context"];
        if (!target.HasContext)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The XMP JSON-LD packet must define @context.",
                pointer + "/@context");
        }
        else if (contextNode == null || contextNode.GetValueKind() != JsonValueKind.Object)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The XMP JSON-LD context must be a JSON object.",
                pointer + "/@context");
        }
        else
        {
            ReadContext(contextNode.AsObject(), target, result, pointer + "/@context");
        }

        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            if (member.Key != "@context")
            {
                target.Properties[member.Key] = JsonValueCloner.CloneNode(member.Value);
            }
        }
    }

    internal void ReadContext(
        JsonObject source,
        GltfXmpJsonLdPacket target,
        GltfExtensionResult result,
        string pointer)
    {
        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            if (member.Value == null
                || member.Value.GetValueKind() != JsonValueKind.String)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    "XMP JSON-LD context definitions must be strings.",
                    pointer + "/" + Fallback.EscapePointerSegment(member.Key));
            }
            else
            {
                target.Context[member.Key] = member.Value.GetValue<string>();
            }
        }
    }

    internal GltfXmpJsonLdAssignmentExtension ReadAssignment(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfXmpJsonLdAssignmentExtension value = new GltfXmpJsonLdAssignmentExtension();
        string pointer = Fallback.CreateExtensionPointer(context) + "/packet";
        value.HasPacket = source.ContainsKey("packet");
        if (!value.HasPacket)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The XMP JSON-LD assignment must define packet.",
                pointer);
        }
        else
        {
            value.Packet = ReadPacketIndex(source["packet"], result, pointer);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["packet"]);
        return value;
    }

    internal int ReadPacketIndex(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Expected a non-negative XMP packet index.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        double value = double.Parse(
            node.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(value)
            || value != System.Math.Truncate(value)
            || value < 0.0
            || value > int.MaxValue)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Expected a non-negative 32-bit XMP packet index.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        return (int)value;
    }

    internal void AddPacketReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int packet)
    {
        if (packet < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = GetSourceCollection(context.ObjectLocation);
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/" + context.Name + "/packet";
        reference.TargetCollection = "xmpJsonLdPackets";
        reference.TargetIndex = packet;
        result.References.Add(reference);
    }

    internal string GetSourceCollection(string location)
    {
        if (location == "scene")
        {
            return "scenes";
        }

        if (location == "node")
        {
            return "nodes";
        }

        if (location == "mesh")
        {
            return "meshes";
        }

        if (location == "material")
        {
            return "materials";
        }

        if (location == "image")
        {
            return "images";
        }

        if (location == "animation")
        {
            return "animations";
        }

        return location;
    }

    internal void AddIssue(
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
