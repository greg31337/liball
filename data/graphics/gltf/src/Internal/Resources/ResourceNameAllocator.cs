using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace gltf;

internal class ResourceNameAllocator
{
    internal HashSet<string> AllocatedNames;
    internal UriPercentCodec PercentCodec;

    internal ResourceNameAllocator()
    {
        AllocatedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        PercentCodec = new UriPercentCodec();
    }

    internal string AllocateResourceName(
        GltfResource resource,
        string? suggestedPath,
        bool useContentHashName)
    {
        if (resource == null)
        {
            throw new System.ArgumentNullException("resource");
        }

        if (resource.Bytes == null)
        {
            throw new System.ArgumentException("Resource bytes must not be null.", "resource");
        }

        string sourcePath = ChooseSuggestedPath(resource, suggestedPath);
        string normalizedPath = NormalizeRelativePath(sourcePath);
        string directory = GetDirectoryPath(normalizedPath);
        string extension = SelectResourceExtension(resource, normalizedPath);
        string stem;
        if (useContentHashName)
        {
            stem = ComputeContentHashName(resource.Bytes);
        }
        else
        {
            stem = GetFileStem(normalizedPath);
            if (stem.Length == 0)
            {
                stem = GetDefaultStem(resource.Kind);
            }
        }

        string candidate = CombineRelativePath(directory, stem + extension);
        if (AllocatedNames.Add(candidate))
        {
            return candidate;
        }

        int suffix = 1;
        while (true)
        {
            candidate = CombineRelativePath(
                directory,
                stem + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture) + extension);
            if (AllocatedNames.Add(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    internal void ReserveResourceName(string relativePath)
    {
        if (relativePath == null)
        {
            throw new System.ArgumentNullException("relativePath");
        }

        string normalizedPath = NormalizeRelativePath(relativePath);
        if (normalizedPath.Length == 0)
        {
            throw new GltfResourceException("A reserved resource name must not be empty.");
        }

        AllocatedNames.Add(normalizedPath);
    }

    internal string ChooseSuggestedPath(GltfResource resource, string? suggestedPath)
    {
        if (suggestedPath != null && suggestedPath.Length > 0)
        {
            return suggestedPath;
        }

        if (resource.SourceUri != null && resource.SourceUri.Length > 0)
        {
            return resource.SourceUri;
        }

        if (resource.EffectiveUri != null && resource.EffectiveUri.Length > 0)
        {
            return resource.EffectiveUri;
        }

        return "";
    }

    internal string NormalizeRelativePath(string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        string value = path.Replace('\\', '/');
        if (value.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        int fragmentIndex = value.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            value = value.Substring(0, fragmentIndex);
        }

        int queryIndex = value.IndexOf('?');
        if (queryIndex >= 0)
        {
            value = value.Substring(0, queryIndex);
        }

        if (value.StartsWith("//", System.StringComparison.Ordinal))
        {
            int pathIndex = value.IndexOf('/', 2);
            if (pathIndex < 0)
            {
                value = "";
            }
            else
            {
                value = value.Substring(pathIndex);
            }
        }
        else
        {
            int authorityIndex = value.IndexOf("://", System.StringComparison.Ordinal);
            if (authorityIndex >= 0)
            {
                int pathIndex = value.IndexOf('/', authorityIndex + 3);
                if (pathIndex < 0)
                {
                    value = "";
                }
                else
                {
                    value = value.Substring(pathIndex);
                }
            }
            else if (value.Length >= 2
                && IsAsciiLetter(value[0])
                && value[1] == ':')
            {
                value = value.Substring(2);
            }
            else
            {
                int colonIndex = value.IndexOf(':');
                int slashIndex = value.IndexOf('/');
                if (colonIndex >= 0 && (slashIndex < 0 || colonIndex < slashIndex))
                {
                    value = value.Substring(colonIndex + 1);
                }
            }
        }

        while (value.StartsWith("/", System.StringComparison.Ordinal))
        {
            value = value.Substring(1);
        }

        string[] sourceSegments = value.Split('/');
        List<string> resultSegments = new List<string>();
        int segmentIndex = 0;
        while (segmentIndex < sourceSegments.Length)
        {
            string segment = sourceSegments[segmentIndex];
            if (segment.Length == 0 || segment == ".")
            {
                segmentIndex++;
                continue;
            }

            if (segment == "..")
            {
                if (resultSegments.Count > 0)
                {
                    resultSegments.RemoveAt(resultSegments.Count - 1);
                }

                segmentIndex++;
                continue;
            }

            resultSegments.Add(segment);
            segmentIndex++;
        }

        StringBuilder result = new StringBuilder();
        segmentIndex = 0;
        while (segmentIndex < resultSegments.Count)
        {
            if (segmentIndex > 0)
            {
                result.Append('/');
            }

            result.Append(resultSegments[segmentIndex]);
            segmentIndex++;
        }

        return PercentCodec.NormalizePath(result.ToString());
    }

    internal string SelectResourceExtension(GltfResource resource, string normalizedPath)
    {
        switch (resource.ImageFormat)
        {
            case GltfImageFormat.Png:
                return ".png";
            case GltfImageFormat.Jpeg:
                return ".jpg";
            case GltfImageFormat.Ktx2:
                return ".ktx2";
            case GltfImageFormat.WebP:
                return ".webp";
            case GltfImageFormat.Avif:
                return ".avif";
            case GltfImageFormat.Dds:
                return ".dds";
            case GltfImageFormat.Astc:
                return ".astc";
        }

        string? mimeExtension = GetMimeTypeExtension(resource.MimeType);
        if (mimeExtension != null)
        {
            return mimeExtension;
        }

        switch (resource.Kind)
        {
            case GltfResourceKind.Buffer:
                return ".bin";
            case GltfResourceKind.Schema:
                return ".json";
        }

        string suggestedExtension = GetPathExtension(normalizedPath);
        if (suggestedExtension.Length > 0)
        {
            return suggestedExtension.ToLowerInvariant();
        }

        return ".bin";
    }

    internal string? GetMimeTypeExtension(string? mimeType)
    {
        if (mimeType == null)
        {
            return null;
        }

        if (string.Equals(mimeType, GltfMimeTypes.Png, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".png";
        }

        if (string.Equals(mimeType, GltfMimeTypes.Jpeg, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".jpg";
        }

        if (string.Equals(mimeType, GltfMimeTypes.Ktx2, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".ktx2";
        }

        if (string.Equals(mimeType, GltfMimeTypes.WebP, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".webp";
        }

        if (string.Equals(mimeType, GltfMimeTypes.Avif, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".avif";
        }

        if (string.Equals(mimeType, GltfMimeTypes.Dds, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".dds";
        }

        if (string.Equals(mimeType, GltfMimeTypes.GltfJson, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".gltf";
        }

        if (string.Equals(mimeType, GltfMimeTypes.GltfBinary, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".glb";
        }

        if (string.Equals(mimeType, GltfMimeTypes.GltfBuffer, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(mimeType, GltfMimeTypes.OctetStream, System.StringComparison.OrdinalIgnoreCase))
        {
            return ".bin";
        }

        return null;
    }

    internal string GetDirectoryPath(string path)
    {
        int slashIndex = path.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return "";
        }

        return path.Substring(0, slashIndex);
    }

    internal string GetFileStem(string path)
    {
        int slashIndex = path.LastIndexOf('/');
        int fileIndex = slashIndex + 1;
        string fileName = path.Substring(fileIndex);
        int dotIndex = fileName.LastIndexOf('.');
        if (dotIndex <= 0)
        {
            return fileName;
        }

        return fileName.Substring(0, dotIndex);
    }

    internal string GetPathExtension(string path)
    {
        int slashIndex = path.LastIndexOf('/');
        int dotIndex = path.LastIndexOf('.');
        if (dotIndex <= slashIndex + 1 || dotIndex == path.Length - 1)
        {
            return "";
        }

        return path.Substring(dotIndex);
    }

    internal string GetDefaultStem(GltfResourceKind kind)
    {
        switch (kind)
        {
            case GltfResourceKind.Buffer:
                return "buffer";
            case GltfResourceKind.Image:
                return "image";
            case GltfResourceKind.Schema:
                return "schema";
            case GltfResourceKind.Extension:
                return "extension";
            default:
                return "resource";
        }
    }

    internal string ComputeContentHashName(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        byte[] hash = SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal string CombineRelativePath(string directory, string fileName)
    {
        if (directory.Length == 0)
        {
            return fileName;
        }

        return directory + "/" + fileName;
    }

    internal bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z');
    }
}
