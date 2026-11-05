using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneExtensionCodec
{
    internal SceneExtensionJsonReader Reader = new SceneExtensionJsonReader();
    internal SceneExtensionJsonWriter Writer = new SceneExtensionJsonWriter();
    internal SceneExtensionValidator Validator = new SceneExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesSceneExtension(string name)
    {
        return name == GltfExtensionNames.KhrTextureTransform
            || name == GltfExtensionNames.KhrAnimationPointer
            || name == GltfExtensionNames.KhrLightsPunctual
            || name == GltfExtensionNames.KhrNodeVisibility
            || name == GltfExtensionNames.KhrNodeHoverability
            || name == GltfExtensionNames.KhrNodeSelectability
            || name == GltfExtensionNames.ExtMeshGpuInstancing;
    }

    internal GltfExtensionResult ReadTypedSceneExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadSceneExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedSceneExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadSceneExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteSceneExtension(context, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
            PopulateReferences(context, result, typed);
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedSceneExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Validator.ValidateSceneExtension(context, registry);
    }

    internal GltfExtensionResult RemapTypedSceneExtension(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        if (context.Name == GltfExtensionNames.KhrAnimationPointer)
        {
            return RemapAnimationPointer(context, remap, registry);
        }

        if (context.Name == GltfExtensionNames.KhrLightsPunctual)
        {
            return RemapPunctualLight(context, remap, registry);
        }

        if (context.Name != GltfExtensionNames.ExtMeshGpuInstancing)
        {
            if (context.TypedData != null)
            {
                return Fallback.CreatePreservedResult(context, registry);
            }

            return Reader.ReadSceneExtension(context, registry);
        }

        GltfExtensionResult parsed;
        if (context.TypedData == null)
        {
            parsed = Reader.ReadSceneExtension(context, registry);
        }
        else
        {
            parsed = Fallback.CreatePreservedResult(context, registry);
        }

        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            typed = parsed.TypedData;
        }

        if (typed == null || typed.MeshGpuInstancing == null)
        {
            return parsed;
        }

        GltfMeshGpuInstancingExtension copied = CopyMeshGpuInstancing(
            typed.MeshGpuInstancing,
            context,
            registry);
        List<string> names = Writer.PropertyJson.GetSortedIndexKeys(copied.Attributes);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            int oldIndex = copied.Attributes[name];
            if (remap.Accessors.ContainsKey(oldIndex))
            {
                copied.Attributes[name] = remap.Accessors[oldIndex];
            }

            index++;
        }

        GltfTypedExtensionData remapped = new GltfTypedExtensionData();
        remapped.MeshGpuInstancing = copied;
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        result.TypedData = remapped;
        result.Json = Writer.WriteMeshGpuInstancing(copied);
        Reader.AddGpuInstancingReferences(context, result, copied);
        return result;
    }

    internal GltfExtensionResult RemapPunctualLight(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfExtensionResult parsed;
        if (context.TypedData == null)
        {
            parsed = Reader.ReadSceneExtension(context, registry);
        }
        else
        {
            parsed = Fallback.CreatePreservedResult(context, registry);
        }

        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            typed = parsed.TypedData;
        }

        if (typed == null || typed.PunctualLightNode == null)
        {
            return parsed;
        }

        GltfPunctualLightNodeExtension copied = new GltfPunctualLightNodeExtension();
        copied.Light = typed.PunctualLightNode.Light;
        copied.PropertyJson = typed.PunctualLightNode.PropertyJson;
        if (remap.PunctualLights.ContainsKey(copied.Light))
        {
            copied.Light = remap.PunctualLights[copied.Light];
        }

        GltfTypedExtensionData remapped = new GltfTypedExtensionData();
        remapped.PunctualLightNode = copied;
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        result.TypedData = remapped;
        result.Json = Writer.WritePunctualLightNode(copied);
        Reader.AddPunctualLightReference(context, result, copied.Light);
        return result;
    }

    internal GltfExtensionResult RemapAnimationPointer(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfExtensionResult parsed;
        if (context.TypedData == null)
        {
            parsed = Reader.ReadSceneExtension(context, registry);
        }
        else
        {
            parsed = Fallback.CreatePreservedResult(context, registry);
        }

        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            typed = parsed.TypedData;
        }

        if (typed == null || typed.AnimationPointer == null)
        {
            return parsed;
        }

        GltfExtensionContext copyContext = new GltfExtensionContext();
        copyContext.Document = context.Document;
        copyContext.Name = context.Name;
        copyContext.ObjectLocation = context.ObjectLocation;
        copyContext.HasObjectIndex = context.HasObjectIndex;
        copyContext.ObjectIndex = context.ObjectIndex;
        copyContext.HasSubObjectIndex = context.HasSubObjectIndex;
        copyContext.SubObjectIndex = context.SubObjectIndex;
        copyContext.Json = Writer.WriteAnimationPointer(typed.AnimationPointer);
        copyContext.Resources = context.Resources;
        GltfExtensionResult copiedResult = Reader.ReadSceneExtension(copyContext, registry);
        if (copiedResult.TypedData == null
            || copiedResult.TypedData.AnimationPointer == null)
        {
            return copiedResult;
        }

        GltfAnimationPointerExtension copied = copiedResult.TypedData.AnimationPointer;
        copied.Pointer = Reader.RemapAnimationPointer(copied.Pointer, remap);
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        result.TypedData = copiedResult.TypedData;
        result.Json = Writer.WriteAnimationPointer(copied);
        Reader.AddAnimationPointerReference(context, result, copied.Pointer);
        return result;
    }

    internal GltfMeshGpuInstancingExtension CopyMeshGpuInstancing(
        GltfMeshGpuInstancingExtension source,
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionContext copyContext = new GltfExtensionContext();
        copyContext.Document = context.Document;
        copyContext.Name = context.Name;
        copyContext.ObjectLocation = context.ObjectLocation;
        copyContext.HasObjectIndex = context.HasObjectIndex;
        copyContext.ObjectIndex = context.ObjectIndex;
        copyContext.HasSubObjectIndex = context.HasSubObjectIndex;
        copyContext.SubObjectIndex = context.SubObjectIndex;
        copyContext.Json = Writer.WriteMeshGpuInstancing(source);
        copyContext.Resources = context.Resources;
        GltfExtensionResult copied = Reader.ReadSceneExtension(copyContext, registry);
        if (copied.TypedData == null || copied.TypedData.MeshGpuInstancing == null)
        {
            return new GltfMeshGpuInstancingExtension();
        }

        return copied.TypedData.MeshGpuInstancing;
    }

    internal void PopulateReferences(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData typed)
    {
        if (typed.PunctualLightNode != null)
        {
            Reader.AddPunctualLightReference(context, result, typed.PunctualLightNode.Light);
        }

        if (typed.MeshGpuInstancing != null)
        {
            Reader.AddGpuInstancingReferences(context, result, typed.MeshGpuInstancing);
        }

        if (typed.AnimationPointer != null)
        {
            Reader.AddAnimationPointerReference(
                context,
                result,
                typed.AnimationPointer.Pointer);
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
