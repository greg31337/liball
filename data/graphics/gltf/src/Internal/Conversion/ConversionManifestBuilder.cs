using System;
using System.Collections.Generic;

namespace gltf;

internal class ConversionManifestBuilder
{
    internal ResourcePlanner Resources = new ResourcePlanner();
    internal ExtensionUsageCollector ExtensionUsage = new ExtensionUsageCollector();
    internal RequiredExtensionCapabilityChecker RequiredExtensions =
        new RequiredExtensionCapabilityChecker();
    internal ValidationIssueAggregator IssueAggregator =
        new ValidationIssueAggregator();

    internal void FinalizeResult(
        GltfDocument source,
        GltfConversionResult result,
        GltfConversionOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (result.PrimaryContentHash == null && result.PrimaryBytes.Length > 0)
        {
            result.PrimaryContentHash = Resources.ComputeContentHash(
                result.PrimaryBytes);
        }

        RebuildManifest(result);
        RebuildDecisions(source, result, options);
        IssueAggregator.SortIssues(result.Diagnostics);
    }

    internal void RebuildManifest(GltfConversionResult result)
    {
        List<GltfResourceManifestEntry> previous = result.Manifest;
        List<GltfResourceManifestEntry> manifest =
            new List<GltfResourceManifestEntry>();
        int resourceIndex = 0;
        int manifestCount = result.Resources.Resources.Count;
        if (previous.Count > manifestCount)
        {
            manifestCount = previous.Count;
        }

        while (resourceIndex < manifestCount)
        {
            GltfResourceManifestEntry entry;
            if (resourceIndex < result.Resources.Resources.Count)
            {
                GltfResource resource = result.Resources.Resources[resourceIndex];
                GltfResourceMode mode = DetermineMode(
                    resource,
                    previous,
                    resourceIndex);
                string hash = Resources.ComputeContentHash(resource.Bytes);
                string? effectiveUri = FindEffectiveUri(
                    result.Document,
                    resourceIndex,
                    resource);
                entry = new GltfResourceManifestEntry();
                entry.Kind = resource.Kind;
                entry.Uri = effectiveUri;
                entry.EffectiveUri = effectiveUri;
                entry.MimeType = DetermineMimeType(resource);
                entry.ByteLength = resource.Bytes.Length;
                entry.ContentHash = hash;
                entry.Embedded = resource.Embedded;
                entry.External = !resource.Embedded;
                entry.Mode = mode;
            }
            else
            {
                entry = CopyPlannedEntry(previous[resourceIndex]);
            }

            manifest.Add(entry);
            resourceIndex++;
        }

        AddMissingGlbBinEntry(result.Document, manifest);
        NormalizeGlbBinEntries(result.Document, manifest);

        result.Manifest = manifest;
    }

    internal void NormalizeGlbBinEntries(
        GltfDocument? document,
        List<GltfResourceManifestEntry> manifest)
    {
        if (document == null
            || document.Container == null
            || document.Container.Format != GltfContainerFormat.Glb)
        {
            return;
        }

        int index = 0;
        while (index < manifest.Count)
        {
            GltfResourceManifestEntry entry = manifest[index];
            if (entry.Kind == GltfResourceKind.Buffer && entry.Embedded)
            {
                entry.Mode = GltfResourceMode.GlbBin;
            }

            index++;
        }
    }

    internal void AddMissingGlbBinEntry(
        GltfDocument? document,
        List<GltfResourceManifestEntry> manifest)
    {
        if (document == null
            || document.Container == null
            || document.Container.Format != GltfContainerFormat.Glb
            || !document.Container.HasBinChunk)
        {
            return;
        }

        int index = 0;
        while (index < manifest.Count)
        {
            GltfResourceManifestEntry existing = manifest[index];
            if (existing.Kind == GltfResourceKind.Buffer
                && existing.Embedded)
            {
                return;
            }

            index++;
        }

        byte[] bytes = document.Container.BinBytes;
        GltfResourceManifestEntry entry = new GltfResourceManifestEntry();
        entry.Kind = GltfResourceKind.Buffer;
        entry.MimeType = GltfMimeTypes.GltfBuffer;
        entry.ByteLength = bytes.Length;
        entry.ContentHash = Resources.ComputeContentHash(bytes);
        entry.Embedded = true;
        entry.External = false;
        entry.Mode = GltfResourceMode.GlbBin;
        manifest.Insert(0, entry);
    }

    internal GltfResourceManifestEntry CopyPlannedEntry(
        GltfResourceManifestEntry source)
    {
        GltfResourceManifestEntry result = new GltfResourceManifestEntry();
        result.Kind = source.Kind;
        result.Uri = source.Uri;
        result.EffectiveUri = source.EffectiveUri ?? source.Uri;
        result.MimeType = source.MimeType;
        if (result.MimeType == null || result.MimeType.Length == 0)
        {
            result.MimeType = source.Kind == GltfResourceKind.Buffer
                ? GltfMimeTypes.GltfBuffer
                : GltfMimeTypes.OctetStream;
        }

        result.ByteLength = source.ByteLength;
        result.ContentHash = source.ContentHash;
        result.Embedded = source.Embedded;
        result.External = !source.Embedded;
        result.Mode = source.Mode;
        return result;
    }

    internal GltfResourceMode DetermineMode(
        GltfResource resource,
        List<GltfResourceManifestEntry> previous,
        int index)
    {
        if (index < previous.Count)
        {
            return previous[index].Mode;
        }

        if (!resource.Embedded)
        {
            return GltfResourceMode.External;
        }

        if (resource.UriKind == GltfUriKind.Data)
        {
            return GltfResourceMode.DataUri;
        }

        return GltfResourceMode.Preserve;
    }

    internal string? FindEffectiveUri(
        GltfDocument? document,
        int resourceIndex,
        GltfResource resource)
    {
        if (document != null)
        {
            int bufferIndex = 0;
            while (bufferIndex < document.Buffers.Count)
            {
                GltfBuffer buffer = document.Buffers[bufferIndex];
                if (buffer.HasResource
                    && buffer.Resource == resourceIndex
                    && buffer.Uri != null)
                {
                    return buffer.Uri;
                }

                bufferIndex++;
            }

            int imageIndex = 0;
            while (imageIndex < document.Images.Count)
            {
                GltfImage image = document.Images[imageIndex];
                if (image.HasResource
                    && image.Resource == resourceIndex
                    && image.Uri != null)
                {
                    return image.Uri;
                }

                imageIndex++;
            }
        }

        return Resources.GetOutputUri(resource);
    }

    internal string DetermineMimeType(GltfResource resource)
    {
        if (resource.MimeType != null && resource.MimeType.Length > 0)
        {
            return resource.MimeType;
        }

        if (resource.Kind == GltfResourceKind.Buffer)
        {
            return GltfMimeTypes.GltfBuffer;
        }

        return Resources.GetEmbeddedMimeType(resource);
    }

    internal void RebuildDecisions(
        GltfDocument source,
        GltfConversionResult result,
        GltfConversionOptions options)
    {
        result.Decisions.Clear();
        List<string> before = ExtensionUsage.CollectDocumentExtensions(source);
        List<string> after = new List<string>();
        GltfDocument? output = result.Document;
        if (output != null)
        {
            after = ExtensionUsage.CollectDocumentExtensions(output);
        }

        List<string> names = new List<string>();
        AddUniqueStrings(names, before);
        AddUniqueStrings(names, after);
        names.Sort(StringComparer.Ordinal);
        List<IGltfExtensionHandler> handlers =
            new List<IGltfExtensionHandler>();
        List<IGltfCompressionCodec> compressionCodecs =
            new List<IGltfCompressionCodec>();
        List<IGltfImageCodec> imageCodecs = new List<IGltfImageCodec>();
        RequiredExtensions.AddReadCapabilities(
            options.ReadOptions,
            handlers,
            compressionCodecs,
            imageCodecs);
        RequiredExtensions.AddWriteCapabilities(
            options.WriteOptions,
            handlers,
            compressionCodecs,
            imageCodecs);
        RequiredExtensions.AddTransformCapabilities(
            options.TransformOptions,
            handlers,
            compressionCodecs,
            imageCodecs);

        int nameIndex = 0;
        while (nameIndex < names.Count)
        {
            string name = names[nameIndex];
            AddExtensionDecision(source, output, result, before, after, name);
            if (after.Contains(name))
            {
                AddCodecCapabilityDecision(
                    result,
                    name,
                    handlers,
                    compressionCodecs,
                    imageCodecs);
            }

            nameIndex++;
        }

        AddAppliedCodecDecisions(result);
        SortDecisions(result.Decisions);
    }

    internal void AddExtensionDecision(
        GltfDocument source,
        GltfDocument? output,
        GltfConversionResult result,
        List<string> before,
        List<string> after,
        string name)
    {
        bool wasPresent = before.Contains(name);
        bool isPresent = after.Contains(name);
        GltfConversionDecision decision = new GltfConversionDecision();
        decision.Kind = GltfConversionDecisionKind.Extension;
        decision.Name = name;
        decision.Required = source.ExtensionsRequired.Contains(name)
            || (output != null && output.ExtensionsRequired.Contains(name));
        if (wasPresent && isPresent)
        {
            decision.Action = GltfConversionDecisionAction.Preserved;
            decision.Detail = "The extension data and declaration were preserved.";
        }
        else if (isPresent)
        {
            decision.Action = GltfConversionDecisionAction.Added;
            decision.Detail = "The extension declaration was added by conversion.";
        }
        else
        {
            decision.Action = GltfConversionDecisionAction.Removed;
            decision.Detail = "The extension declaration was removed by conversion.";
        }

        result.Decisions.Add(decision);
    }

    internal void AddCodecCapabilityDecision(
        GltfConversionResult result,
        string extension,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        GltfExtensionDescriptor? descriptor =
            RequiredExtensions.FindDescriptor(extension, handlers);
        if (descriptor == null || descriptor.RequiredCodec == null)
        {
            return;
        }

        bool available = RequiredExtensions.HasCodec(
            descriptor.RequiredCodec,
            compressionCodecs,
            imageCodecs);
        GltfConversionDecision decision = new GltfConversionDecision();
        decision.Kind = GltfConversionDecisionKind.Codec;
        decision.Name = descriptor.RequiredCodec;
        decision.RelatedExtension = extension;
        decision.Required = result.Document != null
            && result.Document.ExtensionsRequired.Contains(extension);
        decision.Action = available
            ? GltfConversionDecisionAction.Available
            : GltfConversionDecisionAction.Unavailable;
        decision.Status = available
            ? GltfOperationStatus.Success
            : GltfOperationStatus.Unavailable;
        decision.Detail = available
            ? "A registered codec capability was available."
            : "The encoded payload was preserved without an available codec capability.";
        result.Decisions.Add(decision);
    }

    internal void AddAppliedCodecDecisions(GltfConversionResult result)
    {
        GltfTransformReport? report = result.TransformReport;
        if (report == null)
        {
            return;
        }

        int index = 0;
        while (index < report.Codecs.Count)
        {
            GltfConversionDecision decision = new GltfConversionDecision();
            decision.Kind = GltfConversionDecisionKind.Codec;
            decision.Name = report.Codecs[index];
            decision.Action = GltfConversionDecisionAction.Applied;
            decision.Detail = "The codec was applied by the conversion transform plan.";
            result.Decisions.Add(decision);
            index++;
        }
    }

    internal void AddUniqueStrings(List<string> target, List<string> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            string value = source[index];
            if (!target.Contains(value))
            {
                target.Add(value);
            }

            index++;
        }
    }

    internal void SortDecisions(List<GltfConversionDecision> decisions)
    {
        int index = 1;
        while (index < decisions.Count)
        {
            GltfConversionDecision decision = decisions[index];
            int insertion = index;
            while (insertion > 0
                && CompareDecisions(decisions[insertion - 1], decision) > 0)
            {
                decisions[insertion] = decisions[insertion - 1];
                insertion--;
            }

            decisions[insertion] = decision;
            index++;
        }
    }

    internal int CompareDecisions(
        GltfConversionDecision left,
        GltfConversionDecision right)
    {
        int result = (int)left.Kind - (int)right.Kind;
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(left.Name, right.Name);
        if (result != 0)
        {
            return result;
        }

        result = CompareNullableStrings(
            left.RelatedExtension,
            right.RelatedExtension);
        if (result != 0)
        {
            return result;
        }

        return (int)left.Action - (int)right.Action;
    }

    internal int CompareNullableStrings(string? left, string? right)
    {
        if (left == null)
        {
            return right == null ? 0 : -1;
        }

        if (right == null)
        {
            return 1;
        }

        return string.CompareOrdinal(left, right);
    }
}
