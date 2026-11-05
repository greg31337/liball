using System;
using System.Collections.Generic;

namespace gltf;

internal class ValidationStatisticsCollector
{
    internal DocumentInspector Inspector = new DocumentInspector();

    internal GltfDocumentStatistics CollectDocumentStatistics(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (!HasCompleteCollections(document))
        {
            return CollectPartialDocumentStatistics(document);
        }

        GltfInspectionOptions options = new GltfInspectionOptions();
        options.IncludeDependencies = false;
        options.IncludeReverseDependencies = false;
        options.CalculateBounds = true;
        options.DecodeAccessors = false;
        GltfInspectionResult inspection = Inspector.InspectDocumentData(document, options);
        return inspection.Statistics;
    }

    internal bool HasCompleteCollections(GltfDocument document)
    {
        return document.Accessors != null
            && document.Animations != null
            && document.Buffers != null
            && document.BufferViews != null
            && document.Cameras != null
            && document.Images != null
            && document.Materials != null
            && document.Meshes != null
            && document.Nodes != null
            && document.Samplers != null
            && document.Scenes != null
            && document.Skins != null
            && document.Textures != null;
    }

    internal GltfDocumentStatistics CollectPartialDocumentStatistics(
        GltfDocument document)
    {
        GltfDocumentStatistics result = new GltfDocumentStatistics();
        result.AccessorCount = document.Accessors == null ? 0 : document.Accessors.Count;
        result.AnimationCount = document.Animations == null ? 0 : document.Animations.Count;
        result.BufferCount = document.Buffers == null ? 0 : document.Buffers.Count;
        result.BufferViewCount = document.BufferViews == null ? 0 : document.BufferViews.Count;
        result.CameraCount = document.Cameras == null ? 0 : document.Cameras.Count;
        result.ImageCount = document.Images == null ? 0 : document.Images.Count;
        result.MaterialCount = document.Materials == null ? 0 : document.Materials.Count;
        result.MeshCount = document.Meshes == null ? 0 : document.Meshes.Count;
        result.NodeCount = document.Nodes == null ? 0 : document.Nodes.Count;
        result.SamplerCount = document.Samplers == null ? 0 : document.Samplers.Count;
        result.SceneCount = document.Scenes == null ? 0 : document.Scenes.Count;
        result.SkinCount = document.Skins == null ? 0 : document.Skins.Count;
        result.TextureCount = document.Textures == null ? 0 : document.Textures.Count;
        PopulatePartialResourceStatistics(document, result);
        List<string> extensions = new List<string>();
        Inspector.Extensions.AddDeclaredExtensions(extensions, document.ExtensionsUsed);
        Inspector.Extensions.AddDeclaredExtensions(extensions, document.ExtensionsRequired);
        extensions.Sort(StringComparer.Ordinal);
        result.Extensions = extensions;
        return result;
    }

    internal void PopulatePartialResourceStatistics(
        GltfDocument document,
        GltfDocumentStatistics statistics)
    {
        int bufferIndex = 0;
        while (document.Buffers != null && bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer != null)
            {
                if (buffer.ByteLength > 0)
                {
                    statistics.BufferByteLength += buffer.ByteLength;
                }

                if (!buffer.Resolved)
                {
                    statistics.UnresolvedResourceCount++;
                }
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (document.Images != null && imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image != null)
            {
                if (image.Resolved && image.ResolvedBytes != null)
                {
                    statistics.ImageByteLength += image.ResolvedBytes.Length;
                }

                if (!image.Resolved)
                {
                    statistics.UnresolvedResourceCount++;
                }
            }

            imageIndex++;
        }
    }

    internal void PopulateValidationStatistics(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        report.Statistics = CollectDocumentStatistics(document);
    }
}
