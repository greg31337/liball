using System;
using System.Collections;
using System.Collections.Generic;

namespace gltf;

internal class DocumentCompactor
{
    internal DocumentCloner Cloner = new DocumentCloner();
    internal CoreReferenceRemapper CoreReferences = new CoreReferenceRemapper();
    internal ExtensionReferenceRemapper ExtensionReferences =
        new ExtensionReferenceRemapper();
    internal DependencyGraph Dependencies = new DependencyGraph();
    internal DocumentMerger MappingHelpers = new DocumentMerger();
    internal IGltfValidator Validator = new GltfValidator();

    internal GltfMergeResult CompactDocumentData(
        GltfDocument document,
        GltfEditOptions options)
    {
        ValidateArguments(document, options);
        GltfMergeResult result = new GltfMergeResult();
        GltfDocument compacted = Cloner.CloneDocumentData(document, options);
        result.Document = compacted;
        ExtensionReferences.PrepareRootExtensions(compacted, result.Diagnostics);
        GltfIndexRemap remap = CreateCompactionRemap(compacted);
        List<GltfDependency> coreDependencies =
            Dependencies.EnumerateCoreDependencies(compacted);
        EnsureRemovedSlotsUnreferenced(remap, coreDependencies);
        CompactRootCollections(compacted);
        List<GltfDependency> extensionDependencies = Dependencies.ExtensionDependencies
            .EnumerateExtensionDependencies(compacted);
        EnsureRemovedSlotsUnreferenced(remap, extensionDependencies);
        CoreReferences.RemapDocumentReferences(compacted, remap);
        ExtensionReferences.RemapDocumentExtensions(
            compacted,
            remap,
            result.Diagnostics);
        result.SourceRemaps.Add(remap);
        ValidateEditedDocument(compacted, options);
        return result;
    }

    internal GltfMergeResult RebuildDocumentData(
        GltfDocument document,
        GltfEditOptions options)
    {
        ValidateArguments(document, options);
        GltfMergeResult result = new GltfMergeResult();
        GltfDocument rebuilt = Cloner.CloneDocumentData(document, options);
        result.Document = rebuilt;
        ExtensionReferences.PrepareRootExtensions(rebuilt, result.Diagnostics);
        GltfIndexRemap remap = CreateIdentityRemap(rebuilt);
        CoreReferences.RemapDocumentReferences(rebuilt, remap);
        ExtensionReferences.RemapDocumentExtensions(
            rebuilt,
            remap,
            result.Diagnostics);
        result.SourceRemaps.Add(remap);
        ValidateEditedDocument(rebuilt, options);
        return result;
    }

    internal void ValidateArguments(GltfDocument document, GltfEditOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    internal GltfIndexRemap CreateCompactionRemap(GltfDocument document)
    {
        GltfIndexRemap result = new GltfIndexRemap();
        PopulateCompactionMap(result.Accessors, document.Accessors);
        PopulateCompactionMap(result.Animations, document.Animations);
        PopulateCompactionMap(result.Buffers, document.Buffers);
        PopulateCompactionMap(result.BufferViews, document.BufferViews);
        PopulateCompactionMap(result.Cameras, document.Cameras);
        PopulateCompactionMap(result.Images, document.Images);
        PopulateCompactionMap(result.Materials, document.Materials);
        PopulateCompactionMap(result.Meshes, document.Meshes);
        PopulateCompactionMap(result.Nodes, document.Nodes);
        PopulateCompactionMap(result.Resources, document.Resources.Resources);
        PopulateCompactionMap(result.Samplers, document.Samplers);
        PopulateCompactionMap(result.Scenes, document.Scenes);
        PopulateCompactionMap(result.Skins, document.Skins);
        PopulateCompactionMap(result.Textures, document.Textures);
        PopulateIdentityMap(
            result.PunctualLights,
            MappingHelpers.GetPunctualLightCount(document.PropertyJson));
        PopulateIdentityMap(
            result.MaterialVariants,
            MappingHelpers.GetMaterialVariantCount(document.PropertyJson));
        PopulateIdentityMap(
            result.XmpJsonLdPackets,
            MappingHelpers.GetXmpPacketCount(document.PropertyJson));
        return result;
    }

    internal GltfIndexRemap CreateIdentityRemap(GltfDocument document)
    {
        GltfIndexRemap result = new GltfIndexRemap();
        PopulateIdentityMap(result.Accessors, document.Accessors.Count);
        PopulateIdentityMap(result.Animations, document.Animations.Count);
        PopulateIdentityMap(result.Buffers, document.Buffers.Count);
        PopulateIdentityMap(result.BufferViews, document.BufferViews.Count);
        PopulateIdentityMap(result.Cameras, document.Cameras.Count);
        PopulateIdentityMap(result.Images, document.Images.Count);
        PopulateIdentityMap(result.Materials, document.Materials.Count);
        PopulateIdentityMap(result.Meshes, document.Meshes.Count);
        PopulateIdentityMap(result.Nodes, document.Nodes.Count);
        PopulateIdentityMap(result.Resources, document.Resources.Resources.Count);
        PopulateIdentityMap(result.Samplers, document.Samplers.Count);
        PopulateIdentityMap(result.Scenes, document.Scenes.Count);
        PopulateIdentityMap(result.Skins, document.Skins.Count);
        PopulateIdentityMap(result.Textures, document.Textures.Count);
        PopulateIdentityMap(
            result.PunctualLights,
            MappingHelpers.GetPunctualLightCount(document.PropertyJson));
        PopulateIdentityMap(
            result.MaterialVariants,
            MappingHelpers.GetMaterialVariantCount(document.PropertyJson));
        PopulateIdentityMap(
            result.XmpJsonLdPackets,
            MappingHelpers.GetXmpPacketCount(document.PropertyJson));
        return result;
    }

    internal void PopulateCompactionMap(Dictionary<int, int> result, IList source)
    {
        int oldIndex = 0;
        int newIndex = 0;
        while (oldIndex < source.Count)
        {
            if (source[oldIndex] != null)
            {
                result[oldIndex] = newIndex;
                newIndex++;
            }

            oldIndex++;
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

    internal void EnsureRemovedSlotsUnreferenced(
        GltfIndexRemap remap,
        List<GltfDependency> dependencies)
    {
        int index = 0;
        while (index < dependencies.Count)
        {
            GltfDependency dependency = dependencies[index];
            if (IsDependencySourceRetained(dependency, remap)
                && IsDependencyTargetRemoved(dependency, remap))
            {
                throw new GltfValidationException(
                    $"Cannot remove {dependency.TargetCollection} index {dependency.TargetIndex} because {dependency.SourceCollection} index {dependency.SourceIndex} still references it through {dependency.SourceMember}.");
            }

            index++;
        }
    }

    internal bool IsDependencySourceRetained(
        GltfDependency dependency,
        GltfIndexRemap remap)
    {
        Dictionary<int, int>? sourceMap = GetCollectionMap(
            remap,
            dependency.SourceCollection);
        if (sourceMap == null || dependency.SourceIndex < 0)
        {
            return true;
        }

        return sourceMap.ContainsKey(dependency.SourceIndex);
    }

    internal bool IsDependencyTargetRemoved(
        GltfDependency dependency,
        GltfIndexRemap remap)
    {
        Dictionary<int, int>? targetMap = GetCollectionMap(
            remap,
            dependency.TargetCollection);
        if (targetMap == null || dependency.TargetIndex < 0)
        {
            return false;
        }

        return !targetMap.ContainsKey(dependency.TargetIndex);
    }

    internal Dictionary<int, int>? GetCollectionMap(
        GltfIndexRemap remap,
        string collection)
    {
        if (collection == "accessors") return remap.Accessors;
        if (collection == "animations") return remap.Animations;
        if (collection == "buffers") return remap.Buffers;
        if (collection == "bufferViews") return remap.BufferViews;
        if (collection == "cameras") return remap.Cameras;
        if (collection == "images") return remap.Images;
        if (collection == "materials") return remap.Materials;
        if (collection == "meshes") return remap.Meshes;
        if (collection == "nodes") return remap.Nodes;
        if (collection == "resources") return remap.Resources;
        if (collection == "samplers") return remap.Samplers;
        if (collection == "scenes") return remap.Scenes;
        if (collection == "skins") return remap.Skins;
        if (collection == "textures") return remap.Textures;
        if (collection == "extensions/KHR_lights_punctual/lights")
        {
            return remap.PunctualLights;
        }

        if (collection == "extensions/KHR_materials_variants/variants")
        {
            return remap.MaterialVariants;
        }

        if (collection == "xmpJsonLdPackets") return remap.XmpJsonLdPackets;
        return null;
    }

    internal void CompactRootCollections(GltfDocument document)
    {
        CompactNullItems(document.Accessors);
        CompactNullItems(document.Animations);
        CompactNullItems(document.Buffers);
        CompactNullItems(document.BufferViews);
        CompactNullItems(document.Cameras);
        CompactNullItems(document.Images);
        CompactNullItems(document.Materials);
        CompactNullItems(document.Meshes);
        CompactNullItems(document.Nodes);
        CompactNullItems(document.Resources.Resources);
        CompactNullItems(document.Samplers);
        CompactNullItems(document.Scenes);
        CompactNullItems(document.Skins);
        CompactNullItems(document.Textures);
    }

    internal void CompactNullItems(IList values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                values.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    internal void ValidateEditedDocument(GltfDocument document, GltfEditOptions options)
    {
        if (!options.ValidateResult)
        {
            return;
        }

        GltfValidationReport validation = Validator.ValidateDocument(
            document,
            new GltfValidationOptions());
        if (!validation.IsValid)
        {
            string message = "The edited document is invalid.";
            if (validation.Issues.Count > 0)
            {
                message = validation.Issues[0].Message;
            }

            throw new GltfValidationException(message);
        }
    }
}
