using System;
using System.Collections;
using System.Collections.Generic;

namespace gltf;

internal class CanonicalOrderEntry
{
    internal int OldIndex;
    internal string Key = "";
    internal object? Value;
}

internal class DocumentCanonicalizer
{
    internal DeduplicationValueKeyWriter Keys = new DeduplicationValueKeyWriter();
    internal CanonicalNodeKeyWriter NodeKeys = new CanonicalNodeKeyWriter();
    internal CoreReferenceRemapper CoreReferences = new CoreReferenceRemapper();
    internal ExtensionReferenceRemapper ExtensionReferences =
        new ExtensionReferenceRemapper();
    internal ExtensionUsageCollector ExtensionUsage = new ExtensionUsageCollector();
    internal ExtensionRegistry ExtensionRegistry = new ExtensionRegistry();

    internal TransformStepExecution CanonicalizeDocument(
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
        changed = CanonicalizeStage(
            document,
            document.Resources.Resources,
            "resources",
            execution.Result.Remap.Resources,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Buffers,
            "buffers",
            execution.Result.Remap.Buffers,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.BufferViews,
            "bufferViews",
            execution.Result.Remap.BufferViews,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Accessors,
            "accessors",
            execution.Result.Remap.Accessors,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Images,
            "images",
            execution.Result.Remap.Images,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Samplers,
            "samplers",
            execution.Result.Remap.Samplers,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Textures,
            "textures",
            execution.Result.Remap.Textures,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            GetPunctualLights(document),
            "extensions/KHR_lights_punctual/lights",
            execution.Result.Remap.PunctualLights,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            GetMaterialVariants(document),
            "extensions/KHR_materials_variants/variants",
            execution.Result.Remap.MaterialVariants,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            GetXmpPackets(document),
            "xmpJsonLdPackets",
            execution.Result.Remap.XmpJsonLdPackets,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Materials,
            "materials",
            execution.Result.Remap.Materials,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Meshes,
            "meshes",
            execution.Result.Remap.Meshes,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Cameras,
            "cameras",
            execution.Result.Remap.Cameras,
            execution.Result) || changed;
        changed = CanonicalizeNodes(
            document,
            execution.Result.Remap.Nodes,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Skins,
            "skins",
            execution.Result.Remap.Skins,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Animations,
            "animations",
            execution.Result.Remap.Animations,
            execution.Result) || changed;
        changed = CanonicalizeStage(
            document,
            document.Scenes,
            "scenes",
            execution.Result.Remap.Scenes,
            execution.Result) || changed;
        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }

    internal bool CanonicalizeStage(
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

        List<string> keys = new List<string>();
        int index = 0;
        while (index < values.Count)
        {
            keys.Add(Keys.CreateKey(values[index]));
            index++;
        }

        bool changed = ReorderCollection(values, keys, collection, remap, result);
        if (changed)
        {
            ApplyRemap(document, collection, remap, result);
        }

        return changed;
    }

    internal bool CanonicalizeNodes(
        GltfDocument document,
        Dictionary<int, int> remap,
        GltfTransformStepResult result)
    {
        List<string> keys = NodeKeys.CreateNodeKeys(document);
        bool changed = ReorderCollection(
            document.Nodes,
            keys,
            "nodes",
            remap,
            result);
        if (changed)
        {
            ApplyRemap(document, "nodes", remap, result);
        }

        return changed;
    }

    internal bool ReorderCollection(
        IList values,
        List<string> keys,
        string collection,
        Dictionary<int, int> remap,
        GltfTransformStepResult result)
    {
        List<CanonicalOrderEntry> order = new List<CanonicalOrderEntry>();
        int index = 0;
        while (index < values.Count)
        {
            CanonicalOrderEntry entry = new CanonicalOrderEntry();
            entry.OldIndex = index;
            entry.Key = keys[index];
            entry.Value = values[index];
            InsertEntry(order, entry);
            index++;
        }

        bool changed = false;
        index = 0;
        while (index < order.Count)
        {
            remap[order[index].OldIndex] = index;
            if (order[index].OldIndex != index)
            {
                changed = true;
                RecordChangedIdentifier(result, collection, order[index].OldIndex);
            }

            index++;
        }

        if (changed)
        {
            values.Clear();
            index = 0;
            while (index < order.Count)
            {
                values.Add(order[index].Value);
                index++;
            }
        }

        return changed;
    }

    internal void InsertEntry(List<CanonicalOrderEntry> values, CanonicalOrderEntry entry)
    {
        int index = values.Count;
        values.Add(entry);
        while (index > 0 && CompareEntries(values[index - 1], entry) > 0)
        {
            values[index] = values[index - 1];
            index--;
        }

        values[index] = entry;
    }

    internal int CompareEntries(CanonicalOrderEntry left, CanonicalOrderEntry right)
    {
        int keyOrder = string.CompareOrdinal(left.Key, right.Key);
        if (keyOrder != 0)
        {
            return keyOrder;
        }

        return left.OldIndex.CompareTo(right.OldIndex);
    }

    internal void RecordChangedIdentifier(
        GltfTransformStepResult result,
        string collection,
        int oldIndex)
    {
        string identifier = collection + "/" + oldIndex;
        if (collection == "resources")
        {
            result.ChangedResources.Add(identifier);
        }
        else
        {
            result.ChangedObjects.Add(identifier);
        }
    }

    internal void ApplyRemap(
        GltfDocument document,
        string collection,
        Dictionary<int, int> remap,
        GltfTransformStepResult result)
    {
        GltfIndexRemap stageRemap = new GltfIndexRemap();
        SetCollectionMap(stageRemap, collection, remap);
        CoreReferences.RemapDocumentReferences(document, stageRemap);
        ExtensionReferences.RemapDocumentExtensions(
            document,
            stageRemap,
            result.Diagnostics);
    }

    internal void SetCollectionMap(
        GltfIndexRemap target,
        string collection,
        Dictionary<int, int> remap)
    {
        if (collection == "accessors") target.Accessors = remap;
        if (collection == "animations") target.Animations = remap;
        if (collection == "buffers") target.Buffers = remap;
        if (collection == "bufferViews") target.BufferViews = remap;
        if (collection == "cameras") target.Cameras = remap;
        if (collection == "images") target.Images = remap;
        if (collection == "materials") target.Materials = remap;
        if (collection == "meshes") target.Meshes = remap;
        if (collection == "nodes") target.Nodes = remap;
        if (collection == "resources") target.Resources = remap;
        if (collection == "samplers") target.Samplers = remap;
        if (collection == "scenes") target.Scenes = remap;
        if (collection == "skins") target.Skins = remap;
        if (collection == "textures") target.Textures = remap;
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
                    $"Extension '{names[index]}' cannot safely remap references during canonicalization.");
            }

            index++;
        }
    }
}
