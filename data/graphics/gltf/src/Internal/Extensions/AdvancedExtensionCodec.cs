using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class AdvancedExtensionCodec
{
    internal AdvancedExtensionJsonReader Reader = new AdvancedExtensionJsonReader();
    internal AdvancedExtensionJsonWriter Writer = new AdvancedExtensionJsonWriter();
    internal AdvancedExtensionValidator Validator = new AdvancedExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesAdvancedExtension(string name)
    {
        return Reader.HandlesAdvancedExtension(name);
    }

    internal GltfExtensionResult ReadTypedAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadAdvancedExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadAdvancedExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteAdvancedExtension(context.Name, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Validator.ValidateAdvancedExtension(context, registry);
    }

    internal GltfExtensionResult RemapTypedAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            GltfExtensionResult parsed = Reader.ReadAdvancedExtension(context, registry);
            typed = parsed.TypedData;
            if (typed == null)
            {
                return parsed;
            }
        }

        JsonNode? json = Writer.WriteAdvancedExtension(context.Name, typed);
        if (json == null)
        {
            return Fallback.CreatePreservedResult(context, registry);
        }

        return Reader.ReadAdvancedExtension(CopyContext(context, json), registry);
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
