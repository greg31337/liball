using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class ImageExtensionCodec
{
    internal ImageExtensionJsonReader Reader = new ImageExtensionJsonReader();
    internal ImageExtensionJsonWriter Writer = new ImageExtensionJsonWriter();
    internal ImageExtensionValidator Validator = new ImageExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesImageExtension(string name)
    {
        return Reader.HandlesImageExtension(name);
    }

    internal GltfExtensionResult ReadTypedImageExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadImageExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedImageExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadImageExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteImageExtension(context, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
            PopulateReferences(context, result, typed);
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedImageExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Validator.ValidateImageExtension(context, registry);
    }

    internal GltfExtensionResult RemapTypedImageExtension(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            GltfExtensionResult parsed = Reader.ReadImageExtension(context, registry);
            typed = parsed.TypedData;
            if (typed == null)
            {
                return parsed;
            }
        }

        JsonNode? copiedJson = Writer.WriteImageExtension(context, typed);
        if (copiedJson == null)
        {
            return Fallback.CreatePreservedResult(context, registry);
        }

        GltfExtensionContext copyContext = CopyContext(context, copiedJson);
        GltfExtensionResult copied = Reader.ReadImageExtension(copyContext, registry);
        if (copied.TypedData == null)
        {
            return copied;
        }

        ApplyReferenceRemap(copied.TypedData, remap);
        JsonNode? remappedJson = Writer.WriteImageExtension(copyContext, copied.TypedData);
        if (remappedJson == null)
        {
            return copied;
        }

        return Reader.ReadImageExtension(CopyContext(context, remappedJson), registry);
    }

    internal void ApplyReferenceRemap(
        GltfTypedExtensionData typed,
        GltfIndexRemap remap)
    {
        if (typed.TextureBasisu != null
            && remap.Images.ContainsKey(typed.TextureBasisu.Source))
        {
            typed.TextureBasisu.Source = remap.Images[typed.TextureBasisu.Source];
        }

        if (typed.TextureWebp != null
            && remap.Images.ContainsKey(typed.TextureWebp.Source))
        {
            typed.TextureWebp.Source = remap.Images[typed.TextureWebp.Source];
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
        int image = Validator.GetImageIndex(context.Name, typed);
        Reader.AddImageReference(context, result, image);
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
