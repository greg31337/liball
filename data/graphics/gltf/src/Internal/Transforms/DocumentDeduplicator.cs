using System;
using System.Collections;
using System.Collections.Generic;

namespace gltf;

internal class DeduplicationCandidate
{
    internal int OldIndex;
    internal int NewIndex;
    internal string? ExactKey;
}

internal class DocumentDeduplicator
{
    internal DeduplicationValueKeyWriter Keys = new DeduplicationValueKeyWriter();
    internal CoreReferenceRemapper CoreReferences = new CoreReferenceRemapper();
    internal ExtensionReferenceRemapper ExtensionReferences =
        new ExtensionReferenceRemapper();
    internal ExtensionUsageCollector ExtensionUsage = new ExtensionUsageCollector();
    internal ExtensionRegistry ExtensionRegistry = new ExtensionRegistry();

    internal TransformStepExecution DeduplicateDocument(
        GltfDocument document,
        GltfTransformStep step,
        bool requireSafeExtensions)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        Keys.Resources = document.Resources;
        ExtensionReferences.PrepareRootExtensions(
            document,
            execution.Result.Diagnostics);
        if (requireSafeExtensions)
        {
            EnsureExtensionsCanRemap(document);
        }

        bool changed = false;
        changed = DeduplicateStage(
            document,
            document.Buffers,
            "buffers",
            execution.Result.Remap.Buffers,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.BufferViews,
            "bufferViews",
            execution.Result.Remap.BufferViews,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Accessors,
            "accessors",
            execution.Result.Remap.Accessors,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Images,
            "images",
            execution.Result.Remap.Images,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Samplers,
            "samplers",
            execution.Result.Remap.Samplers,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Textures,
            "textures",
            execution.Result.Remap.Textures,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            GetPunctualLights(document),
            "extensions/KHR_lights_punctual/lights",
            execution.Result.Remap.PunctualLights,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            GetMaterialVariants(document),
            "extensions/KHR_materials_variants/variants",
            execution.Result.Remap.MaterialVariants,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            GetXmpPackets(document),
            "xmpJsonLdPackets",
            execution.Result.Remap.XmpJsonLdPackets,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Materials,
            "materials",
            execution.Result.Remap.Materials,
            execution.Result) || changed;
        changed = DeduplicateStage(
            document,
            document.Meshes,
            "meshes",
            execution.Result.Remap.Meshes,
            execution.Result) || changed;
        PopulateIdentityMap(execution.Result.Remap.Animations, document.Animations.Count);
        PopulateIdentityMap(execution.Result.Remap.Cameras, document.Cameras.Count);
        PopulateIdentityMap(execution.Result.Remap.Nodes, document.Nodes.Count);
        PopulateIdentityMap(
            execution.Result.Remap.Resources,
            document.Resources.Resources.Count);
        PopulateIdentityMap(execution.Result.Remap.Scenes, document.Scenes.Count);
        PopulateIdentityMap(execution.Result.Remap.Skins, document.Skins.Count);
        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }

    internal bool DeduplicateStage(
        GltfDocument document,
        IList? values,
        string collection,
        Dictionary<int, int> remap,
        GltfTransformStepResult result)
    {
        if (values == null)
        {
            return false;
        }

        bool changed = DeduplicateCollection(values, collection, remap, result);
        if (changed)
        {
            GltfIndexRemap stageRemap = new GltfIndexRemap();
            SetCollectionMap(stageRemap, collection, remap);
            CoreReferences.RemapDocumentReferences(document, stageRemap);
            ExtensionReferences.RemapDocumentExtensions(
                document,
                stageRemap,
                result.Diagnostics);
        }

        return changed;
    }

    internal bool DeduplicateCollection(
        IList values,
        string collection,
        Dictionary<int, int> remap,
        GltfTransformStepResult result)
    {
        Dictionary<string, List<DeduplicationCandidate>> candidates =
            new Dictionary<string, List<DeduplicationCandidate>>();
        bool[] retained = new bool[values.Count];
        int oldIndex = 0;
        int newIndex = 0;
        bool changed = false;
        while (oldIndex < values.Count)
        {
            object? value = values[oldIndex];
            string key = Keys.CreateKey(value);
            if (!candidates.ContainsKey(key))
            {
                candidates[key] = new List<DeduplicationCandidate>();
            }

            List<DeduplicationCandidate> bucket = candidates[key];
            string? exactKey = null;
            DeduplicationCandidate? match = FindExactMatch(
                values,
                bucket,
                value,
                exactKey);
            if (match != null)
            {
                remap[oldIndex] = match.NewIndex;
                result.ChangedObjects.Add(collection + "/" + oldIndex);
                changed = true;
            }
            else
            {
                retained[oldIndex] = true;
                remap[oldIndex] = newIndex;
                DeduplicationCandidate candidate = new DeduplicationCandidate();
                candidate.OldIndex = oldIndex;
                candidate.NewIndex = newIndex;
                if (bucket.Count > 0)
                {
                    candidate.ExactKey = Keys.CreateExactKey(value);
                }

                bucket.Add(candidate);
                newIndex++;
            }

            oldIndex++;
        }

        FilterRetainedItems(values, retained);
        return changed;
    }

    internal DeduplicationCandidate? FindExactMatch(
        IList values,
        List<DeduplicationCandidate> candidates,
        object? value,
        string? exactKey)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            DeduplicationCandidate candidate = candidates[index];
            if (exactKey == null)
            {
                exactKey = Keys.CreateExactKey(value);
            }

            if (candidate.ExactKey == null)
            {
                candidate.ExactKey = Keys.CreateExactKey(values[candidate.OldIndex]);
            }

            if (candidate.ExactKey == exactKey)
            {
                return candidate;
            }

            index++;
        }

        return null;
    }

    internal void FilterRetainedItems(IList values, bool[] retained)
    {
        int index = retained.Length - 1;
        while (index >= 0)
        {
            if (!retained[index])
            {
                values.RemoveAt(index);
            }

            index--;
        }
    }

    internal void PopulateIdentityMap(Dictionary<int, int> result, int count)
    {
        int index = 0;
        while (index < count)
        {
            result[index] = index;
            index++;
        }
    }

    internal void SetCollectionMap(
        GltfIndexRemap target,
        string collection,
        Dictionary<int, int> remap)
    {
        if (collection == "buffers") target.Buffers = remap;
        if (collection == "bufferViews") target.BufferViews = remap;
        if (collection == "accessors") target.Accessors = remap;
        if (collection == "images") target.Images = remap;
        if (collection == "samplers") target.Samplers = remap;
        if (collection == "textures") target.Textures = remap;
        if (collection == "materials") target.Materials = remap;
        if (collection == "meshes") target.Meshes = remap;
        if (collection == "extensions/KHR_lights_punctual/lights")
        {
            target.PunctualLights = remap;
        }

        if (collection == "extensions/KHR_materials_variants/variants")
        {
            target.MaterialVariants = remap;
        }

        if (collection == "xmpJsonLdPackets") target.XmpJsonLdPackets = remap;
    }

    internal IList? GetPunctualLights(GltfDocument document)
    {
        string name = GltfExtensionNames.KhrLightsPunctual;
        if (document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            GltfLightsPunctualExtension? extension = document.PropertyJson
                .TypedExtensions[name].LightsPunctual;
            if (extension != null)
            {
                return extension.Lights;
            }
        }

        return null;
    }

    internal IList? GetMaterialVariants(GltfDocument document)
    {
        string name = GltfExtensionNames.KhrMaterialsVariants;
        if (document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            GltfMaterialsVariantsExtension? extension = document.PropertyJson
                .TypedExtensions[name].MaterialsVariants;
            if (extension != null)
            {
                return extension.Variants;
            }
        }

        return null;
    }

    internal IList? GetXmpPackets(GltfDocument document)
    {
        string name = GltfExtensionNames.KhrXmpJsonLd;
        if (document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            GltfXmpJsonLdExtension? extension = document.PropertyJson
                .TypedExtensions[name].XmpJsonLd;
            if (extension != null)
            {
                return extension.Packets;
            }
        }

        return null;
    }

    internal void EnsureExtensionsCanRemap(GltfDocument document)
    {
        List<string> names = ExtensionUsage.CollectEncounteredDocumentExtensions(document);
        int index = 0;
        while (index < names.Count)
        {
            GltfExtensionDescriptor? descriptor = ExtensionRegistry.FindDescriptor(names[index]);
            if (descriptor == null || !descriptor.CanRemapReferences)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Extension '{names[index]}' cannot safely remap references during deduplication.");
            }

            index++;
        }
    }
}
