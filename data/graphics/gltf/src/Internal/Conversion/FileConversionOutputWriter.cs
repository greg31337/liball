using System;
using System.Collections.Generic;
using System.IO;

namespace gltf;

internal class FileConversionOutputItem
{
    internal string FinalPath = "";
    internal string TemporaryPath = "";
    internal byte[] Bytes = [];
    internal bool Primary;
    internal bool Staged;
}

internal class FileConversionOutputTransaction : IDisposable
{
    internal List<FileConversionOutputItem> Items =
        new List<FileConversionOutputItem>();

    internal void StageFiles()
    {
        int index = 0;
        while (index < Items.Count)
        {
            FileConversionOutputItem item = Items[index];
            string? directory = Path.GetDirectoryName(item.TemporaryPath);
            if (directory != null && directory.Length > 0)
            {
                Directory.CreateDirectory(directory);
            }

            item.Staged = true;
            File.WriteAllBytes(item.TemporaryPath, item.Bytes);
            index++;
        }
    }

    internal void CommitFiles()
    {
        int index = 0;
        while (index < Items.Count)
        {
            FileConversionOutputItem item = Items[index];
            if (!item.Primary)
            {
                File.Move(item.TemporaryPath, item.FinalPath, true);
                item.Staged = false;
            }

            index++;
        }

        index = 0;
        while (index < Items.Count)
        {
            FileConversionOutputItem item = Items[index];
            if (item.Primary)
            {
                File.Move(item.TemporaryPath, item.FinalPath, true);
                item.Staged = false;
            }

            index++;
        }
    }

    /// <summary>Removes unpublished sibling temporary files.</summary>
    public void Dispose()
    {
        int index = 0;
        while (index < Items.Count)
        {
            FileConversionOutputItem item = Items[index];
            if (item.Staged && File.Exists(item.TemporaryPath))
            {
                File.Delete(item.TemporaryPath);
                item.Staged = false;
            }

            index++;
        }
    }
}

internal class FileConversionOutputWriter
{
    internal UriReference Uris = new UriReference();
    internal UriPercentCodec PercentCodec = new UriPercentCodec();
    internal FileResourceResolver FileResolver =
        new FileResourceResolver();

    internal void WriteResult(
        string outputPath,
        GltfConversionResult result,
        GltfConversionOptions options)
    {
        if (outputPath == null)
        {
            throw new ArgumentNullException("outputPath");
        }

        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string primaryPath = Path.GetFullPath(outputPath);
        List<FileConversionOutputItem> items = CreateOutputItems(
            primaryPath,
            result,
            options.OverwriteResources);
        if (options.AtomicOutput)
        {
            WriteAtomicFiles(items);
        }
        else
        {
            WriteDirectFiles(items);
        }
    }

    internal List<FileConversionOutputItem> CreateOutputItems(
        string primaryPath,
        GltfConversionResult result,
        bool overwriteResources)
    {
        if (result.Document == null)
        {
            throw new GltfWriteException(
                "Successful file conversion requires an output document.");
        }

        string? outputDirectory = Path.GetDirectoryName(primaryPath);
        if (outputDirectory == null || outputDirectory.Length == 0)
        {
            throw new GltfWriteException(
                "The output file has no containing directory.");
        }

        List<FileConversionOutputItem> resultItems =
            new List<FileConversionOutputItem>();
        List<string> finalPaths = new List<string>();
        int resourceIndex = 0;
        while (resourceIndex < result.Resources.Resources.Count)
        {
            GltfResource resource = result.Resources.Resources[resourceIndex];
            if (!resource.Embedded)
            {
                string? uri = FindDocumentResourceUri(
                    result.Document,
                    resourceIndex,
                    resource);
                if (uri != null && uri.Length > 0)
                {
                    string resourcePath = ResolveResourceOutputPath(
                        outputDirectory,
                        uri);
                    ValidateDistinctTarget(
                        resourcePath,
                        primaryPath,
                        finalPaths);
                    bool retainedAtTarget = ResourceAlreadyAtTarget(
                        resource,
                        resourcePath);
                    if (!retainedAtTarget
                        && !overwriteResources
                        && (File.Exists(resourcePath)
                            || Directory.Exists(resourcePath)))
                    {
                        throw new GltfWriteException(
                            $"Output resource '{uri}' already exists.");
                    }

                    if (!retainedAtTarget)
                    {
                        FileConversionOutputItem resourceItem =
                            new FileConversionOutputItem();
                        resourceItem.FinalPath = resourcePath;
                        resourceItem.Bytes = resource.Bytes;
                        resultItems.Add(resourceItem);
                    }

                    finalPaths.Add(resourcePath);
                }
            }

            resourceIndex++;
        }

        if (Directory.Exists(primaryPath))
        {
            throw new GltfWriteException(
                "The output primary path names an existing directory.");
        }

        FileConversionOutputItem primaryItem =
            new FileConversionOutputItem();
        primaryItem.FinalPath = primaryPath;
        primaryItem.Bytes = result.PrimaryBytes;
        primaryItem.Primary = true;
        resultItems.Add(primaryItem);
        return resultItems;
    }

    internal string? FindDocumentResourceUri(
        GltfDocument document,
        int resourceIndex,
        GltfResource resource)
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

        if (resource.SourceUri != null
            && Uris.ClassifyReference(resource.SourceUri)
                == GltfUriKind.Relative)
        {
            return resource.SourceUri;
        }

        if (resource.EffectiveUri != null
            && Uris.ClassifyReference(resource.EffectiveUri)
                == GltfUriKind.Relative)
        {
            return resource.EffectiveUri;
        }

        return null;
    }

    internal bool ResourceAlreadyAtTarget(
        GltfResource resource,
        string targetPath)
    {
        string? effectiveUri = resource.EffectiveUri;
        if (effectiveUri == null
            || Uris.ClassifyReference(effectiveUri) != GltfUriKind.File)
        {
            return false;
        }

        System.Uri parsed = new System.Uri(
            effectiveUri,
            System.UriKind.Absolute);
        if (!parsed.IsFile)
        {
            return false;
        }

        return PathsEqual(parsed.LocalPath, targetPath)
            && File.Exists(targetPath);
    }

    internal string ResolveResourceOutputPath(
        string outputDirectory,
        string uri)
    {
        if (Uris.ClassifyReference(uri) != GltfUriKind.Relative)
        {
            throw new GltfWriteException(
                $"Output resource URI '{uri}' is not a relative file path.");
        }

        UriReferenceParts parts = Uris.ParseReference(uri);
        string relativePath = PercentCodec.DecodeText(parts.Path);
        if (relativePath.Length == 0
            || Path.IsPathRooted(relativePath))
        {
            throw new GltfWriteException(
                $"Output resource URI '{uri}' is not a relative file path.");
        }

        string normalizedPath = relativePath.Replace(
            '/',
            Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(
            Path.Combine(outputDirectory, normalizedPath));
        if (!FileResolver.IsPathWithinRoot(fullPath, outputDirectory))
        {
            throw new GltfWriteException(
                $"Output resource URI '{uri}' escapes the output directory.");
        }

        return fullPath;
    }

    internal void ValidateDistinctTarget(
        string resourcePath,
        string primaryPath,
        List<string> existingPaths)
    {
        if (PathsEqual(resourcePath, primaryPath))
        {
            throw new GltfWriteException(
                "An output resource path collides with the primary output path.");
        }

        int index = 0;
        while (index < existingPaths.Count)
        {
            if (PathsEqual(resourcePath, existingPaths[index]))
            {
                throw new GltfWriteException(
                    "Multiple output resources resolve to the same file path.");
            }

            index++;
        }
    }

    internal bool PathsEqual(string first, string second)
    {
        StringComparison comparison = StringComparison.Ordinal;
        if (Path.DirectorySeparatorChar == '\\')
        {
            comparison = StringComparison.OrdinalIgnoreCase;
        }

        return string.Equals(
            Path.GetFullPath(first),
            Path.GetFullPath(second),
            comparison);
    }

    internal void WriteAtomicFiles(
        List<FileConversionOutputItem> items)
    {
        AssignTemporaryPaths(items);
        using (FileConversionOutputTransaction transaction =
            new FileConversionOutputTransaction())
        {
            transaction.Items = items;
            transaction.StageFiles();
            transaction.CommitFiles();
        }
    }

    internal void AssignTemporaryPaths(
        List<FileConversionOutputItem> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            FileConversionOutputItem item = items[index];
            string temporaryPath = item.FinalPath
                + ".tmp-"
                + Guid.NewGuid().ToString("N");
            while (File.Exists(temporaryPath)
                || Directory.Exists(temporaryPath))
            {
                temporaryPath = item.FinalPath
                    + ".tmp-"
                    + Guid.NewGuid().ToString("N");
            }

            item.TemporaryPath = temporaryPath;
            index++;
        }
    }

    internal void WriteDirectFiles(
        List<FileConversionOutputItem> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            FileConversionOutputItem item = items[index];
            if (!item.Primary)
            {
                string? directory = Path.GetDirectoryName(item.FinalPath);
                if (directory != null && directory.Length > 0)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(item.FinalPath, item.Bytes);
            }

            index++;
        }

        index = 0;
        while (index < items.Count)
        {
            FileConversionOutputItem item = items[index];
            if (item.Primary)
            {
                string? directory = Path.GetDirectoryName(item.FinalPath);
                if (directory != null && directory.Length > 0)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(item.FinalPath, item.Bytes);
            }

            index++;
        }
    }
}
