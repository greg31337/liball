using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class DocumentMerger
{
    internal DocumentCloner Cloner = new DocumentCloner();
    internal CoreReferenceRemapper CoreReferences = new CoreReferenceRemapper();
    internal ExtensionReferenceRemapper ExtensionReferences =
        new ExtensionReferenceRemapper();
    internal IGltfValidator Validator = new GltfValidator();

    internal GltfMergeResult MergeDocumentData(
        GltfDocument[] documents,
        GltfEditOptions options)
    {
        if (documents == null)
        {
            throw new ArgumentNullException("documents");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfMergeResult result = new GltfMergeResult();
        GltfDocument merged = new GltfDocument();
        result.Document = merged;
        int sourceIndex = 0;
        while (sourceIndex < documents.Length)
        {
            GltfDocument source = documents[sourceIndex];
            if (source == null)
            {
                throw new ArgumentException(
                    $"Source document {sourceIndex} must not be null.",
                    "documents");
            }

            GltfDocument copied = Cloner.CloneDocumentData(source, options);
            ExtensionReferences.PrepareRootExtensions(copied, result.Diagnostics);
            GltfIndexRemap remap = CreateSourceRemap(copied, merged);
            CoreReferences.RemapDocumentReferences(copied, remap);
            ExtensionReferences.RemapDocumentExtensions(
                copied,
                remap,
                result.Diagnostics);
            AppendSourceDocument(merged, copied, sourceIndex);
            result.SourceRemaps.Add(remap);
            sourceIndex++;
        }

        merged.Container = null;
        if (options.ValidateResult)
        {
            GltfValidationReport validation = Validator.ValidateDocument(
                merged,
                new GltfValidationOptions());
            if (!validation.IsValid)
            {
                string message = "The merged document is invalid.";
                if (validation.Issues.Count > 0)
                {
                    message = validation.Issues[0].Message;
                }

                throw new GltfValidationException(message);
            }
        }

        return result;
    }

    internal GltfIndexRemap CreateSourceRemap(
        GltfDocument source,
        GltfDocument target)
    {
        GltfIndexRemap result = new GltfIndexRemap();
        PopulateOffsetMap(result.Accessors, source.Accessors.Count, target.Accessors.Count);
        PopulateOffsetMap(result.Animations, source.Animations.Count, target.Animations.Count);
        PopulateOffsetMap(result.Buffers, source.Buffers.Count, target.Buffers.Count);
        PopulateOffsetMap(
            result.BufferViews,
            source.BufferViews.Count,
            target.BufferViews.Count);
        PopulateOffsetMap(result.Cameras, source.Cameras.Count, target.Cameras.Count);
        PopulateOffsetMap(result.Images, source.Images.Count, target.Images.Count);
        PopulateOffsetMap(result.Materials, source.Materials.Count, target.Materials.Count);
        PopulateOffsetMap(result.Meshes, source.Meshes.Count, target.Meshes.Count);
        PopulateOffsetMap(result.Nodes, source.Nodes.Count, target.Nodes.Count);
        PopulateOffsetMap(result.Samplers, source.Samplers.Count, target.Samplers.Count);
        PopulateOffsetMap(result.Scenes, source.Scenes.Count, target.Scenes.Count);
        PopulateOffsetMap(result.Skins, source.Skins.Count, target.Skins.Count);
        PopulateOffsetMap(result.Textures, source.Textures.Count, target.Textures.Count);
        PopulateOffsetMap(
            result.Resources,
            source.Resources.Resources.Count,
            target.Resources.Resources.Count);
        PopulateOffsetMap(
            result.PunctualLights,
            GetPunctualLightCount(source.PropertyJson),
            GetPunctualLightCount(target.PropertyJson));
        PopulateOffsetMap(
            result.MaterialVariants,
            GetMaterialVariantCount(source.PropertyJson),
            GetMaterialVariantCount(target.PropertyJson));
        PopulateOffsetMap(
            result.XmpJsonLdPackets,
            GetXmpPacketCount(source.PropertyJson),
            GetXmpPacketCount(target.PropertyJson));
        return result;
    }

    internal void PopulateOffsetMap(
        Dictionary<int, int> result,
        int count,
        int offset)
    {
        int index = 0;
        while (index < count)
        {
            result[index] = offset + index;
            index++;
        }
    }

    internal void AppendSourceDocument(
        GltfDocument target,
        GltfDocument source,
        int sourceIndex)
    {
        if (sourceIndex == 0)
        {
            target.Asset = source.Asset;
        }

        if (!target.HasDefaultScene && source.HasDefaultScene)
        {
            target.HasDefaultScene = true;
            target.DefaultScene = source.DefaultScene;
        }

        AppendObjectList(target.Accessors, source.Accessors);
        AppendObjectList(target.Animations, source.Animations);
        AppendObjectList(target.Buffers, source.Buffers);
        AppendObjectList(target.BufferViews, source.BufferViews);
        AppendObjectList(target.Cameras, source.Cameras);
        AppendObjectList(target.Images, source.Images);
        AppendObjectList(target.Materials, source.Materials);
        AppendObjectList(target.Meshes, source.Meshes);
        AppendObjectList(target.Nodes, source.Nodes);
        AppendObjectList(target.Samplers, source.Samplers);
        AppendObjectList(target.Scenes, source.Scenes);
        AppendObjectList(target.Skins, source.Skins);
        AppendObjectList(target.Textures, source.Textures);
        AppendUniqueStrings(target.ExtensionsUsed, source.ExtensionsUsed);
        AppendUniqueStrings(target.ExtensionsRequired, source.ExtensionsRequired);
        MergeRootPropertyJson(target.PropertyJson, source.PropertyJson);
        AppendResources(target.Resources, source.Resources, sourceIndex);
    }

    internal void AppendObjectList(IList target, IList source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal void AppendUniqueStrings(List<string> target, List<string> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!target.Contains(source[index]))
            {
                target.Add(source[index]);
            }

            index++;
        }
    }

    internal void AppendResources(
        GltfResourceSet target,
        GltfResourceSet source,
        int sourceIndex)
    {
        AppendObjectList(target.Resources, source.Resources);
        List<string> keys = new List<string>();
        foreach (KeyValuePair<string, GltfResource> entry in source.Cache)
        {
            keys.Add(entry.Key);
        }

        keys.Sort(StringComparer.Ordinal);
        int index = 0;
        while (index < keys.Count)
        {
            string sourceKey = keys[index];
            string targetKey = CreateAvailableCacheKey(target.Cache, sourceKey, sourceIndex);
            GltfResource resource = source.Cache[sourceKey];
            target.Cache[targetKey] = resource;
            if (resource.CacheKey == sourceKey)
            {
                resource.CacheKey = targetKey;
            }

            index++;
        }
    }

    internal string CreateAvailableCacheKey(
        Dictionary<string, GltfResource> cache,
        string sourceKey,
        int sourceIndex)
    {
        if (!cache.ContainsKey(sourceKey))
        {
            return sourceKey;
        }

        string candidate = $"{sourceKey}#{sourceIndex}";
        int suffix = 1;
        while (cache.ContainsKey(candidate))
        {
            candidate = $"{sourceKey}#{sourceIndex}.{suffix}";
            suffix++;
        }

        return candidate;
    }

    internal void MergeRootPropertyJson(GltfPropertyJson target, GltfPropertyJson source)
    {
        if (!target.HasExtras && target.Extras == null
            && (source.HasExtras || source.Extras != null))
        {
            target.HasExtras = source.HasExtras;
            target.Extras = source.Extras;
        }

        List<string> unknownNames = GetSortedJsonKeys(source.UnknownMembers);
        int index = 0;
        while (index < unknownNames.Count)
        {
            string name = unknownNames[index];
            if (!target.UnknownMembers.ContainsKey(name))
            {
                target.UnknownMembers[name] = source.UnknownMembers[name];
            }

            index++;
        }

        List<string> extensionNames = GetSortedJsonKeys(source.Extensions);
        index = 0;
        while (index < extensionNames.Count)
        {
            string name = extensionNames[index];
            if (!target.Extensions.ContainsKey(name))
            {
                target.Extensions[name] = source.Extensions[name];
            }

            index++;
        }

        List<string> typedNames = new List<string>();
        foreach (KeyValuePair<string, GltfTypedExtensionData> entry
            in source.TypedExtensions)
        {
            typedNames.Add(entry.Key);
        }

        typedNames.Sort(StringComparer.Ordinal);
        index = 0;
        while (index < typedNames.Count)
        {
            string name = typedNames[index];
            if (!target.TypedExtensions.ContainsKey(name))
            {
                target.TypedExtensions[name] = source.TypedExtensions[name];
            }
            else
            {
                MergeRootTypedExtension(
                    name,
                    target.TypedExtensions[name],
                    source.TypedExtensions[name]);
            }

            index++;
        }
    }

    internal List<string> GetSortedJsonKeys(Dictionary<string, JsonNode?> values)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> entry in values)
        {
            result.Add(entry.Key);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal void MergeRootTypedExtension(
        string name,
        GltfTypedExtensionData target,
        GltfTypedExtensionData source)
    {
        if (name == GltfExtensionNames.KhrLightsPunctual
            && target.LightsPunctual != null
            && source.LightsPunctual != null)
        {
            AppendObjectList(target.LightsPunctual.Lights, source.LightsPunctual.Lights);
        }
        else if (name == GltfExtensionNames.KhrMaterialsVariants
            && target.MaterialsVariants != null
            && source.MaterialsVariants != null)
        {
            AppendObjectList(
                target.MaterialsVariants.Variants,
                source.MaterialsVariants.Variants);
        }
        else if (name == GltfExtensionNames.KhrXmpJsonLd
            && target.XmpJsonLd != null
            && source.XmpJsonLd != null)
        {
            AppendObjectList(target.XmpJsonLd.Packets, source.XmpJsonLd.Packets);
        }
        else if (name == GltfExtensionNames.KhrInteractivity
            && target.Interactivity != null
            && source.Interactivity != null)
        {
            int graphOffset = target.Interactivity.Graphs.Count;
            AppendObjectList(target.Interactivity.Graphs, source.Interactivity.Graphs);
            if (!target.Interactivity.HasDefaultGraph
                && source.Interactivity.HasDefaultGraph)
            {
                target.Interactivity.HasDefaultGraph = true;
                target.Interactivity.DefaultGraph = graphOffset
                    + source.Interactivity.DefaultGraph;
            }
        }
    }

    internal int GetPunctualLightCount(GltfPropertyJson propertyJson)
    {
        if (propertyJson.TypedExtensions.ContainsKey(GltfExtensionNames.KhrLightsPunctual))
        {
            GltfTypedExtensionData typed = propertyJson.TypedExtensions[
                GltfExtensionNames.KhrLightsPunctual];
            if (typed.LightsPunctual != null)
            {
                return typed.LightsPunctual.Lights.Count;
            }
        }

        return 0;
    }

    internal int GetMaterialVariantCount(GltfPropertyJson propertyJson)
    {
        if (propertyJson.TypedExtensions.ContainsKey(GltfExtensionNames.KhrMaterialsVariants))
        {
            GltfTypedExtensionData typed = propertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsVariants];
            if (typed.MaterialsVariants != null)
            {
                return typed.MaterialsVariants.Variants.Count;
            }
        }

        return 0;
    }

    internal int GetXmpPacketCount(GltfPropertyJson propertyJson)
    {
        if (propertyJson.TypedExtensions.ContainsKey(GltfExtensionNames.KhrXmpJsonLd))
        {
            GltfTypedExtensionData typed = propertyJson.TypedExtensions[
                GltfExtensionNames.KhrXmpJsonLd];
            if (typed.XmpJsonLd != null)
            {
                return typed.XmpJsonLd.Packets.Count;
            }
        }

        return 0;
    }
}
