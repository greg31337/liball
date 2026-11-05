using System;
using System.Collections;
using System.Collections.Generic;

namespace gltf;

internal class DocumentPruner
{
    internal PruneSettingsReader SettingsReader = new PruneSettingsReader();
    internal DependencyGraph Dependencies = new DependencyGraph();
    internal CoreReferenceRemapper CoreReferences = new CoreReferenceRemapper();
    internal ExtensionReferenceRemapper ExtensionReferences =
        new ExtensionReferenceRemapper();
    internal DocumentMerger MappingHelpers = new DocumentMerger();
    internal PropertyJsonRetention PropertyRetention = new PropertyJsonRetention();
    internal ExtensionUsageCollector ExtensionUsage = new ExtensionUsageCollector();
    internal ExtensionRegistry ExtensionRegistry = new ExtensionRegistry();

    internal TransformStepExecution PruneDocument(
        GltfDocument document,
        GltfTransformStep step,
        bool requireSafeExtensions)
    {
        PruneSettings settings = SettingsReader.ReadSettings(step, document.Scenes.Count);
        ExtensionReferences.PrepareRootExtensions(
            document,
            new List<GltfValidationIssue>());
        if (settings.KeepExtensions && requireSafeExtensions)
        {
            EnsureExtensionsCanRemap(document);
        }

        PruneReachability reachable = CreateReachability(document);
        SeedReachability(reachable, settings);
        List<GltfDependency> dependencies;
        if (settings.KeepExtensions)
        {
            dependencies = Dependencies.EnumerateDocumentDependencies(document);
        }
        else
        {
            dependencies = Dependencies.EnumerateCoreDependencies(document);
        }

        ExpandReachability(reachable, dependencies);
        GltfIndexRemap remap = CreatePruneRemap(reachable);
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = remap;
        RecordRemovedObjects(reachable, execution.Result);
        PrepareDefaultScene(document, remap);
        FilterRootCollections(document, reachable);
        FilterExtensionCollections(document, reachable);
        FilterResourceCache(document, execution.Result);
        CoreReferences.RemapDocumentReferences(document, remap);
        if (settings.KeepExtensions)
        {
            ExtensionReferences.RemapDocumentExtensions(
                document,
                remap,
                execution.Result.Diagnostics);
        }

        PropertyRetention.ApplyRetention(
            document,
            settings.KeepExtensions,
            settings.KeepExtras);
        RecordPropertyRemoval(execution.Result);
        if (document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
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
                    $"Extension '{names[index]}' cannot safely remap references during pruning.");
            }

            index++;
        }
    }

    internal PruneReachability CreateReachability(GltfDocument document)
    {
        PruneReachability result = new PruneReachability();
        result.Accessors = new bool[document.Accessors.Count];
        result.Animations = new bool[document.Animations.Count];
        result.Buffers = new bool[document.Buffers.Count];
        result.BufferViews = new bool[document.BufferViews.Count];
        result.Cameras = new bool[document.Cameras.Count];
        result.Images = new bool[document.Images.Count];
        result.Materials = new bool[document.Materials.Count];
        result.Meshes = new bool[document.Meshes.Count];
        result.Nodes = new bool[document.Nodes.Count];
        result.Resources = new bool[document.Resources.Resources.Count];
        result.Samplers = new bool[document.Samplers.Count];
        result.Scenes = new bool[document.Scenes.Count];
        result.Skins = new bool[document.Skins.Count];
        result.Textures = new bool[document.Textures.Count];
        result.PunctualLights = new bool[
            MappingHelpers.GetPunctualLightCount(document.PropertyJson)];
        result.MaterialVariants = new bool[
            MappingHelpers.GetMaterialVariantCount(document.PropertyJson)];
        result.XmpJsonLdPackets = new bool[
            MappingHelpers.GetXmpPacketCount(document.PropertyJson)];
        return result;
    }

    internal void SeedReachability(PruneReachability reachable, PruneSettings settings)
    {
        if (settings.HasScenes)
        {
            int sceneIndex = 0;
            while (sceneIndex < settings.Scenes.Count)
            {
                reachable.Scenes[settings.Scenes[sceneIndex]] = true;
                sceneIndex++;
            }
        }
        else
        {
            MarkAll(reachable.Scenes);
        }

        if (settings.KeepAnimations)
        {
            MarkAll(reachable.Animations);
        }

        if (settings.KeepSkins)
        {
            MarkAll(reachable.Skins);
        }
    }

    internal void MarkAll(bool[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            values[index] = true;
            index++;
        }
    }

    internal void ExpandReachability(
        PruneReachability reachable,
        List<GltfDependency> dependencies)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            int index = 0;
            while (index < dependencies.Count)
            {
                GltfDependency dependency = dependencies[index];
                if (IsSourceReachable(reachable, dependency)
                    && MarkTargetReachable(reachable, dependency))
                {
                    changed = true;
                }

                index++;
            }
        }
    }

    internal bool IsSourceReachable(
        PruneReachability reachable,
        GltfDependency dependency)
    {
        if (dependency.SourceCollection == "document")
        {
            return false;
        }

        bool[]? values = GetReachableCollection(reachable, dependency.SourceCollection);
        if (values == null)
        {
            return dependency.SourceIndex < 0;
        }

        return dependency.SourceIndex >= 0
            && dependency.SourceIndex < values.Length
            && values[dependency.SourceIndex];
    }

    internal bool MarkTargetReachable(
        PruneReachability reachable,
        GltfDependency dependency)
    {
        bool[]? values = GetReachableCollection(reachable, dependency.TargetCollection);
        if (values == null
            || dependency.TargetIndex < 0
            || dependency.TargetIndex >= values.Length
            || values[dependency.TargetIndex])
        {
            return false;
        }

        values[dependency.TargetIndex] = true;
        return true;
    }

    internal bool[]? GetReachableCollection(
        PruneReachability reachable,
        string collection)
    {
        if (collection == "accessors"
            || collection == "accessor"
            || collection == "accessor.sparse") return reachable.Accessors;
        if (collection == "animations"
            || collection == "animation"
            || collection == "animation.channel"
            || collection == "animation.channel.target"
            || collection == "animation.sampler") return reachable.Animations;
        if (collection == "buffers" || collection == "buffer") return reachable.Buffers;
        if (collection == "bufferViews" || collection == "bufferView")
        {
            return reachable.BufferViews;
        }

        if (collection == "cameras" || collection == "camera") return reachable.Cameras;
        if (collection == "images" || collection == "image") return reachable.Images;
        if (collection == "materials"
            || collection == "material"
            || collection == "textureInfo") return reachable.Materials;
        if (collection == "meshes"
            || collection == "mesh"
            || collection == "mesh.primitive") return reachable.Meshes;
        if (collection == "nodes" || collection == "node") return reachable.Nodes;
        if (collection == "resources") return reachable.Resources;
        if (collection == "samplers" || collection == "sampler")
        {
            return reachable.Samplers;
        }

        if (collection == "scenes" || collection == "scene") return reachable.Scenes;
        if (collection == "skins" || collection == "skin") return reachable.Skins;
        if (collection == "textures" || collection == "texture")
        {
            return reachable.Textures;
        }
        if (collection == "extensions/KHR_lights_punctual/lights")
        {
            return reachable.PunctualLights;
        }

        if (collection == "extensions/KHR_materials_variants/variants")
        {
            return reachable.MaterialVariants;
        }

        if (collection == "xmpJsonLdPackets") return reachable.XmpJsonLdPackets;
        return null;
    }

    internal GltfIndexRemap CreatePruneRemap(PruneReachability reachable)
    {
        GltfIndexRemap result = new GltfIndexRemap();
        PopulateRetainedMap(result.Accessors, reachable.Accessors);
        PopulateRetainedMap(result.Animations, reachable.Animations);
        PopulateRetainedMap(result.Buffers, reachable.Buffers);
        PopulateRetainedMap(result.BufferViews, reachable.BufferViews);
        PopulateRetainedMap(result.Cameras, reachable.Cameras);
        PopulateRetainedMap(result.Images, reachable.Images);
        PopulateRetainedMap(result.Materials, reachable.Materials);
        PopulateRetainedMap(result.Meshes, reachable.Meshes);
        PopulateRetainedMap(result.Nodes, reachable.Nodes);
        PopulateRetainedMap(result.Resources, reachable.Resources);
        PopulateRetainedMap(result.Samplers, reachable.Samplers);
        PopulateRetainedMap(result.Scenes, reachable.Scenes);
        PopulateRetainedMap(result.Skins, reachable.Skins);
        PopulateRetainedMap(result.Textures, reachable.Textures);
        PopulateRetainedMap(result.PunctualLights, reachable.PunctualLights);
        PopulateRetainedMap(result.MaterialVariants, reachable.MaterialVariants);
        PopulateRetainedMap(result.XmpJsonLdPackets, reachable.XmpJsonLdPackets);
        return result;
    }

    internal void PopulateRetainedMap(Dictionary<int, int> result, bool[] retained)
    {
        int oldIndex = 0;
        int newIndex = 0;
        while (oldIndex < retained.Length)
        {
            if (retained[oldIndex])
            {
                result[oldIndex] = newIndex;
                newIndex++;
            }

            oldIndex++;
        }
    }

    internal void RecordRemovedObjects(
        PruneReachability reachable,
        GltfTransformStepResult result)
    {
        RecordRemovedCollection(reachable.Accessors, "accessors", result.ChangedObjects);
        RecordRemovedCollection(reachable.Animations, "animations", result.ChangedObjects);
        RecordRemovedCollection(reachable.Buffers, "buffers", result.ChangedObjects);
        RecordRemovedCollection(reachable.BufferViews, "bufferViews", result.ChangedObjects);
        RecordRemovedCollection(reachable.Cameras, "cameras", result.ChangedObjects);
        RecordRemovedCollection(reachable.Images, "images", result.ChangedObjects);
        RecordRemovedCollection(reachable.Materials, "materials", result.ChangedObjects);
        RecordRemovedCollection(reachable.Meshes, "meshes", result.ChangedObjects);
        RecordRemovedCollection(reachable.Nodes, "nodes", result.ChangedObjects);
        RecordRemovedCollection(reachable.Samplers, "samplers", result.ChangedObjects);
        RecordRemovedCollection(reachable.Scenes, "scenes", result.ChangedObjects);
        RecordRemovedCollection(reachable.Skins, "skins", result.ChangedObjects);
        RecordRemovedCollection(reachable.Textures, "textures", result.ChangedObjects);
        RecordRemovedCollection(
            reachable.PunctualLights,
            "extensions/KHR_lights_punctual/lights",
            result.ChangedObjects);
        RecordRemovedCollection(
            reachable.MaterialVariants,
            "extensions/KHR_materials_variants/variants",
            result.ChangedObjects);
        RecordRemovedCollection(
            reachable.XmpJsonLdPackets,
            "xmpJsonLdPackets",
            result.ChangedObjects);
        RecordRemovedCollection(reachable.Resources, "resources", result.ChangedResources);
    }

    internal void RecordRemovedCollection(
        bool[] retained,
        string collection,
        List<string> result)
    {
        int index = 0;
        while (index < retained.Length)
        {
            if (!retained[index])
            {
                result.Add($"{collection}/{index}");
            }

            index++;
        }
    }

    internal void PrepareDefaultScene(GltfDocument document, GltfIndexRemap remap)
    {
        if (document.HasDefaultScene
            && !remap.Scenes.ContainsKey(document.DefaultScene))
        {
            document.HasDefaultScene = false;
            document.DefaultScene = GltfConstants.AbsentIndex;
        }
    }

    internal void FilterRootCollections(
        GltfDocument document,
        PruneReachability reachable)
    {
        FilterRetainedItems(document.Accessors, reachable.Accessors);
        FilterRetainedItems(document.Animations, reachable.Animations);
        FilterRetainedItems(document.Buffers, reachable.Buffers);
        FilterRetainedItems(document.BufferViews, reachable.BufferViews);
        FilterRetainedItems(document.Cameras, reachable.Cameras);
        FilterRetainedItems(document.Images, reachable.Images);
        FilterRetainedItems(document.Materials, reachable.Materials);
        FilterRetainedItems(document.Meshes, reachable.Meshes);
        FilterRetainedItems(document.Nodes, reachable.Nodes);
        FilterRetainedItems(document.Resources.Resources, reachable.Resources);
        FilterRetainedItems(document.Samplers, reachable.Samplers);
        FilterRetainedItems(document.Scenes, reachable.Scenes);
        FilterRetainedItems(document.Skins, reachable.Skins);
        FilterRetainedItems(document.Textures, reachable.Textures);
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

    internal void FilterExtensionCollections(
        GltfDocument document,
        PruneReachability reachable)
    {
        FilterPunctualLights(document, reachable.PunctualLights);
        FilterMaterialVariants(document, reachable.MaterialVariants);
        FilterXmpPackets(document, reachable.XmpJsonLdPackets);
    }

    internal void FilterPunctualLights(GltfDocument document, bool[] retained)
    {
        string name = GltfExtensionNames.KhrLightsPunctual;
        if (!document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            return;
        }

        GltfLightsPunctualExtension? value = document.PropertyJson.TypedExtensions[name]
            .LightsPunctual;
        if (value == null)
        {
            return;
        }

        FilterRetainedItems(value.Lights, retained);
        if (retained.Length > 0 && value.Lights.Count == 0)
        {
            RemoveRootExtension(document, name);
        }
    }

    internal void FilterMaterialVariants(GltfDocument document, bool[] retained)
    {
        string name = GltfExtensionNames.KhrMaterialsVariants;
        if (!document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            return;
        }

        GltfMaterialsVariantsExtension? value = document.PropertyJson.TypedExtensions[name]
            .MaterialsVariants;
        if (value == null)
        {
            return;
        }

        FilterRetainedItems(value.Variants, retained);
        if (retained.Length > 0 && value.Variants.Count == 0)
        {
            RemoveRootExtension(document, name);
        }
    }

    internal void FilterXmpPackets(GltfDocument document, bool[] retained)
    {
        string name = GltfExtensionNames.KhrXmpJsonLd;
        if (!document.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            return;
        }

        GltfXmpJsonLdExtension? value = document.PropertyJson.TypedExtensions[name]
            .XmpJsonLd;
        if (value == null)
        {
            return;
        }

        FilterRetainedItems(value.Packets, retained);
        if (retained.Length > 0 && value.Packets.Count == 0)
        {
            RemoveRootExtension(document, name);
        }
    }

    internal void RemoveRootExtension(GltfDocument document, string name)
    {
        document.PropertyJson.TypedExtensions.Remove(name);
        document.PropertyJson.Extensions.Remove(name);
        RemoveStringValue(document.ExtensionsUsed, name);
        RemoveStringValue(document.ExtensionsRequired, name);
    }

    internal void RemoveStringValue(List<string> values, string value)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (values[index] == value)
            {
                values.RemoveAt(index);
            }

            index--;
        }
    }

    internal void FilterResourceCache(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        HashSet<object> retained = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int resourceIndex = 0;
        while (resourceIndex < document.Resources.Resources.Count)
        {
            retained.Add(document.Resources.Resources[resourceIndex]);
            resourceIndex++;
        }

        List<string> keys = new List<string>();
        foreach (KeyValuePair<string, GltfResource> entry in document.Resources.Cache)
        {
            keys.Add(entry.Key);
        }

        keys.Sort(StringComparer.Ordinal);
        int keyIndex = 0;
        while (keyIndex < keys.Count)
        {
            string key = keys[keyIndex];
            if (!retained.Contains(document.Resources.Cache[key]))
            {
                document.Resources.Cache.Remove(key);
                result.ChangedResources.Add("cache/" + key);
            }

            keyIndex++;
        }
    }

    internal void RecordPropertyRemoval(GltfTransformStepResult result)
    {
        if (PropertyRetention.RemovedExtensions)
        {
            result.ChangedObjects.Add("extensions");
        }

        if (PropertyRetention.RemovedExtras)
        {
            result.ChangedObjects.Add("extras");
        }
    }
}
