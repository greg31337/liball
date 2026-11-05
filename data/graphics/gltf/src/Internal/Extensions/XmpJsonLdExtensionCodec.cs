using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class XmpJsonLdExtensionCodec
{
    internal XmpJsonLdExtensionJsonReader Reader = new XmpJsonLdExtensionJsonReader();
    internal XmpJsonLdExtensionJsonWriter Writer = new XmpJsonLdExtensionJsonWriter();
    internal XmpJsonLdExtensionValidator Validator = new XmpJsonLdExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesXmpJsonLdExtension(string name)
    {
        return Reader.HandlesXmpJsonLdExtension(name);
    }

    internal GltfExtensionResult ReadTypedXmpJsonLdExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadXmpJsonLdExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedXmpJsonLdExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadXmpJsonLdExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteXmpJsonLdExtension(context, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
            PopulateReferences(context, result, typed);
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedXmpJsonLdExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Validator.ValidateXmpJsonLdExtension(context, registry);
    }

    internal GltfExtensionResult RemapTypedXmpJsonLdExtension(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            GltfExtensionResult parsed = Reader.ReadXmpJsonLdExtension(context, registry);
            typed = parsed.TypedData;
            if (typed == null)
            {
                return parsed;
            }
        }

        JsonNode? copiedJson = Writer.WriteXmpJsonLdExtension(context, typed);
        if (copiedJson == null)
        {
            return Fallback.CreatePreservedResult(context, registry);
        }

        GltfExtensionContext copyContext = CopyContext(context, copiedJson);
        GltfExtensionResult copied = Reader.ReadXmpJsonLdExtension(copyContext, registry);
        if (copied.TypedData == null)
        {
            return copied;
        }

        ApplyReferenceRemap(copied.TypedData, remap);
        JsonNode? remappedJson = Writer.WriteXmpJsonLdExtension(
            copyContext,
            copied.TypedData);
        if (remappedJson == null)
        {
            return copied;
        }

        return Reader.ReadXmpJsonLdExtension(
            CopyContext(context, remappedJson),
            registry);
    }

    internal void ApplyReferenceRemap(
        GltfTypedExtensionData typed,
        GltfIndexRemap remap)
    {
        if (typed.XmpJsonLdAssignment != null
            && remap.XmpJsonLdPackets.ContainsKey(
                typed.XmpJsonLdAssignment.Packet))
        {
            typed.XmpJsonLdAssignment.Packet = remap.XmpJsonLdPackets[
                typed.XmpJsonLdAssignment.Packet];
        }
    }

    internal GltfExtensionContext CopyContext(
        GltfExtensionContext source,
        JsonNode json)
    {
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = source.Document;
        result.Name = source.Name;
        result.ObjectLocation = source.ObjectLocation;
        result.HasObjectIndex = source.HasObjectIndex;
        result.ObjectIndex = source.ObjectIndex;
        result.HasSubObjectIndex = source.HasSubObjectIndex;
        result.SubObjectIndex = source.SubObjectIndex;
        result.Json = json;
        result.Resources = source.Resources;
        return result;
    }

    internal void PopulateReferences(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData typed)
    {
        if (typed.XmpJsonLdAssignment != null
            && typed.XmpJsonLdAssignment.HasPacket)
        {
            Reader.AddPacketReference(
                context,
                result,
                typed.XmpJsonLdAssignment.Packet);
        }
    }

    internal void CopyDiagnostics(
        List<GltfValidationIssue> source,
        List<GltfValidationIssue> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
