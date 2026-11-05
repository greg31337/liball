using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class CompressionExtensionCodec
{
    internal CompressionExtensionJsonReader Reader = new CompressionExtensionJsonReader();
    internal CompressionExtensionJsonWriter Writer = new CompressionExtensionJsonWriter();
    internal CompressionExtensionValidator Validator = new CompressionExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesCompressionExtension(string name)
    {
        return Reader.HandlesCompressionExtension(name);
    }

    internal GltfExtensionResult ReadTypedCompressionExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadCompressionExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedCompressionExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadCompressionExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteCompressionExtension(context, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
            PopulateReferences(context, result, typed);
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedCompressionExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Validator.ValidateCompressionExtension(context, registry);
    }

    internal GltfExtensionResult RemapTypedCompressionExtension(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            GltfExtensionResult parsed = Reader.ReadCompressionExtension(context, registry);
            typed = parsed.TypedData;
            if (typed == null)
            {
                return parsed;
            }
        }

        JsonNode? copiedJson = Writer.WriteCompressionExtension(context, typed);
        if (copiedJson == null)
        {
            return Fallback.CreatePreservedResult(context, registry);
        }

        GltfExtensionContext copyContext = CopyContext(context, copiedJson);
        GltfExtensionResult copied = Reader.ReadCompressionExtension(copyContext, registry);
        if (copied.TypedData == null)
        {
            return copied;
        }

        ApplyReferenceRemap(copied.TypedData, remap);
        JsonNode? remappedJson = Writer.WriteCompressionExtension(
            copyContext,
            copied.TypedData);
        if (remappedJson == null)
        {
            return copied;
        }

        GltfExtensionContext resultContext = CopyContext(context, remappedJson);
        return Reader.ReadCompressionExtension(resultContext, registry);
    }

    internal void ApplyReferenceRemap(
        GltfTypedExtensionData typed,
        GltfIndexRemap remap)
    {
        if (typed.DracoMeshCompression != null)
        {
            int oldIndex = typed.DracoMeshCompression.BufferView;
            if (remap.BufferViews.ContainsKey(oldIndex))
            {
                typed.DracoMeshCompression.BufferView = remap.BufferViews[oldIndex];
            }
        }

        GltfMeshoptCompressionBufferViewExtension? view = typed.KhrMeshoptCompressionBufferView;
        if (view == null)
        {
            view = typed.ExtMeshoptCompressionBufferView;
        }

        if (view != null && remap.Buffers.ContainsKey(view.Buffer))
        {
            view.Buffer = remap.Buffers[view.Buffer];
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
        if (typed.DracoMeshCompression != null)
        {
            Reader.AddBufferViewReference(
                context,
                result,
                typed.DracoMeshCompression.BufferView,
                "bufferView");
        }

        GltfMeshoptCompressionBufferViewExtension? view = typed.KhrMeshoptCompressionBufferView;
        if (view == null)
        {
            view = typed.ExtMeshoptCompressionBufferView;
        }

        if (view != null)
        {
            Reader.AddBufferReference(context, result, view.Buffer);
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
