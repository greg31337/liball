using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class XmpJsonLdExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal XmpJsonLdExtensionJsonReader Reader = new XmpJsonLdExtensionJsonReader();

    internal GltfExtensionResult ValidateXmpJsonLdExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadXmpJsonLdExtension(context, registry);
            typed = result.TypedData;
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, registry);
            typed = context.TypedData;
            result.TypedData = typed;
        }

        if (context.ObjectLocation == "glTF")
        {
            ValidateRoot(context, result, typed);
        }
        else
        {
            ValidateAssignment(context, result, typed);
        }

        return result;
    }

    internal void ValidateRoot(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData? typed)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (typed == null || typed.XmpJsonLd == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The root extension must contain XMP JSON-LD packet data.",
                pointer);
            return;
        }

        GltfXmpJsonLdExtension root = typed.XmpJsonLd;
        if (!root.HasPackets)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The root XMP JSON-LD extension must define packets.",
                pointer + "/packets");
            return;
        }

        if (root.Packets.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "The XMP JSON-LD packets array must not be empty.",
                pointer + "/packets");
        }

        int index = 0;
        while (index < root.Packets.Count)
        {
            ValidatePacket(
                root.Packets[index],
                result,
                pointer + "/packets/" + index);
            index++;
        }
    }

    internal void ValidatePacket(
        GltfXmpJsonLdPacket packet,
        GltfExtensionResult result,
        string pointer)
    {
        if (!packet.HasContext)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The XMP JSON-LD packet must define @context.",
                pointer + "/@context");
        }

        ValidateContextAliases(packet, result, pointer + "/@context");
        foreach (KeyValuePair<string, JsonNode?> property in packet.Properties)
        {
            string propertyPointer = pointer + "/"
                + Fallback.EscapePointerSegment(property.Key);
            ValidatePropertyName(packet, property.Key, result, propertyPointer);
            ValidatePayloadValue(
                packet,
                property.Value,
                property.Key,
                result,
                propertyPointer);
        }
    }

    internal void ValidateContextAliases(
        GltfXmpJsonLdPacket packet,
        GltfExtensionResult result,
        string pointer)
    {
        foreach (KeyValuePair<string, string> first in packet.Context)
        {
            foreach (KeyValuePair<string, string> second in packet.Context)
            {
                if (string.CompareOrdinal(first.Key, second.Key) < 0
                    && string.Equals(
                        first.Value,
                        second.Value,
                        System.StringComparison.Ordinal))
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.DuplicateElements,
                        "An XMP namespace must not have multiple context prefixes.",
                        pointer + "/" + Fallback.EscapePointerSegment(second.Key));
                }
            }
        }
    }

    internal void ValidatePropertyName(
        GltfXmpJsonLdPacket packet,
        string name,
        GltfExtensionResult result,
        string pointer)
    {
        if (name.Length > 0 && name[0] == '@')
        {
            return;
        }

        int separator = name.IndexOf(':');
        if (separator <= 0 || separator == name.Length - 1)
        {
            AddIssue(
                result,
                GltfIssueCodes.PatternMismatch,
                "XMP property names must be compact IRIs in prefix:suffix form.",
                pointer);
            return;
        }

        string prefix = name.Substring(0, separator);
        if (!packet.Context.ContainsKey(prefix))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                $"XMP property prefix '{prefix}' is not defined by the packet context.",
                pointer);
        }
    }

    internal void ValidatePayloadValue(
        GltfXmpJsonLdPacket packet,
        JsonNode? node,
        string memberName,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null)
        {
            return;
        }

        JsonValueKind kind = node.GetValueKind();
        if (kind == JsonValueKind.Array)
        {
            if (memberName != "@list" && memberName != "@set")
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnexpectedProperty,
                    "XMP arrays must be children of @list or @set.",
                    pointer);
            }

            JsonArray array = node.AsArray();
            int index = 0;
            while (index < array.Count)
            {
                ValidatePayloadValue(
                    packet,
                    array[index],
                    "",
                    result,
                    pointer + "/" + index);
                index++;
            }

            return;
        }

        if (kind != JsonValueKind.Object)
        {
            return;
        }

        JsonObject objectValue = node.AsObject();
        foreach (KeyValuePair<string, JsonNode?> member in objectValue)
        {
            string memberPointer = pointer + "/"
                + Fallback.EscapePointerSegment(member.Key);
            ValidatePropertyName(packet, member.Key, result, memberPointer);
            if (member.Key == "@context"
                && !IsContentLanguageContext(member.Value))
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnexpectedProperty,
                    "Local JSON-LD contexts are not permitted in XMP packets.",
                    memberPointer);
            }

            if ((member.Key == "@list" || member.Key == "@set")
                && (member.Value == null
                    || member.Value.GetValueKind() != JsonValueKind.Array))
            {
                AddIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The @list and @set members must contain arrays.",
                    memberPointer);
            }

            ValidatePayloadValue(
                packet,
                member.Value,
                member.Key,
                result,
                memberPointer);
        }
    }

    internal bool IsContentLanguageContext(JsonNode? node)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Object)
        {
            return false;
        }

        JsonObject context = node.AsObject();
        JsonNode? language = context["@language"];
        return context.Count == 1
            && context.ContainsKey("@language")
            && language != null
            && language.GetValueKind() == JsonValueKind.String;
    }

    internal void ValidateAssignment(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData? typed)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (!IsAssignmentLocation(context.ObjectLocation))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                $"KHR_xmp_json_ld is not valid on {context.ObjectLocation}.",
                pointer);
        }

        if (typed == null || typed.XmpJsonLdAssignment == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The extension must contain an XMP packet assignment.",
                pointer);
            return;
        }

        GltfXmpJsonLdAssignmentExtension assignment = typed.XmpJsonLdAssignment;
        if (!assignment.HasPacket || assignment.Packet < 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "The XMP assignment must reference a packet.",
                pointer + "/packet");
            return;
        }

        if (context.TypedData != null)
        {
            Reader.AddPacketReference(context, result, assignment.Packet);
        }

        int packetCount = GetRootPacketCount(context.Document);
        if (packetCount < 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "The XMP assignment requires root KHR_xmp_json_ld packets.",
                pointer + "/packet");
        }
        else if (assignment.Packet >= packetCount)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The XMP packet assignment does not resolve.",
                pointer + "/packet");
        }
    }

    internal bool IsAssignmentLocation(string location)
    {
        return location == "asset"
            || location == "scene"
            || location == "node"
            || location == "mesh"
            || location == "material"
            || location == "image"
            || location == "animation";
    }

    internal int GetRootPacketCount(GltfDocument? document)
    {
        if (document == null)
        {
            return GltfConstants.AbsentIndex;
        }

        if (document.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrXmpJsonLd))
        {
            GltfTypedExtensionData typed = document.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrXmpJsonLd];
            if (typed.XmpJsonLd != null && typed.XmpJsonLd.HasPackets)
            {
                return typed.XmpJsonLd.Packets.Count;
            }
        }

        if (document.PropertyJson.Extensions.ContainsKey(
            GltfExtensionNames.KhrXmpJsonLd))
        {
            JsonNode? rootNode = document.PropertyJson.Extensions[
                GltfExtensionNames.KhrXmpJsonLd];
            if (rootNode != null && rootNode.GetValueKind() == JsonValueKind.Object)
            {
                JsonNode? packetsNode = rootNode.AsObject()["packets"];
                if (packetsNode != null
                    && packetsNode.GetValueKind() == JsonValueKind.Array)
                {
                    return packetsNode.AsArray().Count;
                }
            }
        }

        return GltfConstants.AbsentIndex;
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
