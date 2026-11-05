using System.IO;

namespace gltf;

internal class FileResourceResolver
{
    internal DataUriCodec DataUriCodec = new DataUriCodec();
    internal UriReference UriReference = new UriReference();
    internal UriPercentCodec PercentCodec = new UriPercentCodec();

    internal GltfResource ResolveResource(GltfResourceRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Policy == null || request.Policy.FileRoots == null)
        {
            throw new System.ArgumentException(
                "The resource policy and file roots must not be null.",
                "request");
        }

        if (request.MaximumByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("request", "The maximum resource byte length must not be negative.");
        }

        GltfResource result = CreateResource(request);
        GltfUriKind uriKind = UriReference.ClassifyReference(request.SourceUri);
        result.UriKind = uriKind;
        switch (uriKind)
        {
            case GltfUriKind.Absent:
                result.Status = GltfOperationStatus.Invalid;
                return result;
            case GltfUriKind.Data:
                return ResolveDataResource(request, result);
            case GltfUriKind.Relative:
            case GltfUriKind.File:
                return ResolveFileResource(request, result);
            case GltfUriKind.Network:
            case GltfUriKind.Absolute:
            case GltfUriKind.Custom:
                result.EffectiveUri = UriReference.NormalizeReference(request.SourceUri ?? "");
                result.CacheKey = "uri:" + result.EffectiveUri;
                result.Status = GltfOperationStatus.Unavailable;
                return result;
            default:
                result.Status = GltfOperationStatus.Unavailable;
                return result;
        }
    }

    internal GltfResource CreateResource(GltfResourceRequest request)
    {
        GltfResource result = new GltfResource();
        result.Kind = request.Kind;
        result.SourceUri = request.SourceUri;
        result.MimeType = request.DeclaredMimeType;
        result.DeclaredMimeType = request.DeclaredMimeType;
        result.HasDeclaredByteLength = request.HasDeclaredByteLength;
        result.DeclaredByteLength = request.DeclaredByteLength;
        return result;
    }

    internal GltfResource ResolveDataResource(GltfResourceRequest request, GltfResource result)
    {
        DataUriData data = DataUriCodec.ParseLimitedDataUri(
            request.SourceUri ?? "",
            request.MaximumByteLength);
        result.Bytes = data.Bytes;
        result.Embedded = true;
        result.SourceMimeType = data.EffectiveMimeType;
        result.EffectiveUri = UriReference.NormalizeReference(request.SourceUri ?? "");
        result.CacheKey = "uri:" + result.EffectiveUri;
        if (data.HasDeclaredMimeType || request.DeclaredMimeType == null)
        {
            result.MimeType = data.EffectiveMimeType;
        }

        return result;
    }

    internal GltfResource ResolveFileResource(GltfResourceRequest request, GltfResource result)
    {
        UriReferenceParts sourceParts = UriReference.ParseReference(request.SourceUri ?? "");
        string decodedPath = PercentCodec.DecodeText(sourceParts.Path);
        bool sourceIsFileUri = result.UriKind == GltfUriKind.File;
        bool sourceIsAbsolutePath = Path.IsPathRooted(decodedPath);
        if ((sourceIsFileUri || sourceIsAbsolutePath) && !request.Policy.AllowAbsolutePaths)
        {
            throw new GltfResourceException(
                $"Absolute resource path '{request.SourceUri}' is denied by the configured policy.");
        }

        if (!request.Policy.AllowParentPathSegments && ContainsParentPathSegment(decodedPath))
        {
            throw new GltfResourceException(
                $"Parent path traversal in resource URI '{request.SourceUri}' is denied by the configured policy.");
        }

        if (sourceIsFileUri)
        {
            string effectiveUri = UriReference.NormalizeReference(request.SourceUri ?? "");
            string path = GetLocalFilePath(effectiveUri, "resource URI");
            return ReadFileResource(request, result, path, effectiveUri, null);
        }

        if (sourceIsAbsolutePath)
        {
            string path = Path.GetFullPath(decodedPath);
            string effectiveUri = AppendQueryAndFragment(
                new System.Uri(path).AbsoluteUri,
                sourceParts);
            return ReadFileResource(request, result, path, effectiveUri, null);
        }

        if (request.BaseUri != null)
        {
            GltfUriKind baseKind = UriReference.ClassifyReference(request.BaseUri);
            string resolvedUri = UriReference.ResolveReference(request.BaseUri, request.SourceUri ?? "");
            result.EffectiveUri = UriReference.NormalizeReference(resolvedUri);
            result.CacheKey = "uri:" + result.EffectiveUri;
            if (baseKind != GltfUriKind.File)
            {
                result.UriKind = UriReference.ClassifyReference(result.EffectiveUri);
                result.Status = GltfOperationStatus.Unavailable;
                return result;
            }

            string path = GetLocalFilePath(result.EffectiveUri, "resolved resource URI");
            string? baseDirectory = Path.GetDirectoryName(
                GetLocalFilePath(request.BaseUri, "base URI"));
            return ReadFileResource(request, result, path, result.EffectiveUri, baseDirectory);
        }

        if (request.Policy.FileRoots.Count == 0)
        {
            throw new GltfResourceException(
                $"Relative resource URI '{request.SourceUri}' requires a file base URI or configured file root.");
        }

        return SearchFileRoots(request, result);
    }

    internal GltfResource SearchFileRoots(GltfResourceRequest request, GltfResource result)
    {
        UriReferenceParts sourceParts = UriReference.ParseReference(request.SourceUri ?? "");
        string relativePath = PercentCodec.DecodeText(sourceParts.Path);
        int rootIndex = 0;
        while (rootIndex < request.Policy.FileRoots.Count)
        {
            string rootPath = GetRootPath(request.Policy.FileRoots[rootIndex]);
            string candidatePath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
            ValidateAllowedPath(request, candidatePath, rootPath);
            string effectiveUri = AppendQueryAndFragment(
                new System.Uri(candidatePath).AbsoluteUri,
                sourceParts);
            if (File.Exists(candidatePath))
            {
                return ReadFileResource(request, result, candidatePath, effectiveUri, rootPath);
            }

            if (rootIndex == 0)
            {
                result.EffectiveUri = UriReference.NormalizeReference(effectiveUri);
                result.CacheKey = "uri:" + result.EffectiveUri;
            }

            rootIndex++;
        }

        result.Status = GltfOperationStatus.Unavailable;
        return result;
    }

    internal GltfResource ReadFileResource(
        GltfResourceRequest request,
        GltfResource result,
        string path,
        string effectiveUri,
        string? inferredRoot)
    {
        string fullPath = Path.GetFullPath(path);
        ValidateAllowedPath(request, fullPath, inferredRoot);
        result.EffectiveUri = UriReference.NormalizeReference(effectiveUri);
        result.CacheKey = "uri:" + result.EffectiveUri;
        if (!File.Exists(fullPath))
        {
            result.Status = GltfOperationStatus.Unavailable;
            return result;
        }

        FileInfo info = new FileInfo(fullPath);
        long fileLength = info.Length;
        if (fileLength > request.MaximumByteLength)
        {
            throw new GltfResourceException(
                $"Resource length {fileLength} exceeds the configured maximum {request.MaximumByteLength}.");
        }

        byte[] bytes = new byte[(int)fileLength];
        using FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        int offset = 0;
        while (offset < bytes.Length)
        {
            int readByteLength = stream.Read(bytes, offset, bytes.Length - offset);
            if (readByteLength == 0)
            {
                throw new GltfResourceException(
                    $"Resource file '{result.EffectiveUri}' ended before its measured length.");
            }

            offset += readByteLength;
        }

        if (stream.ReadByte() >= 0)
        {
            throw new GltfResourceException(
                $"Resource file '{result.EffectiveUri}' grew beyond its measured length while being read.");
        }

        result.Bytes = bytes;
        return result;
    }

    internal void ValidateAllowedPath(
        GltfResourceRequest request,
        string path,
        string? inferredRoot)
    {
        if (request.Policy.FileRoots.Count == 0)
        {
            if (inferredRoot != null && !IsPathWithinRoot(path, inferredRoot))
            {
                throw new GltfResourceException(
                    $"Resolved resource path '{path}' is outside the configured file roots.");
            }

            return;
        }

        int rootIndex = 0;
        while (rootIndex < request.Policy.FileRoots.Count)
        {
            string rootPath = GetRootPath(request.Policy.FileRoots[rootIndex]);
            if (IsPathWithinRoot(path, rootPath))
            {
                return;
            }

            rootIndex++;
        }

        throw new GltfResourceException(
            $"Resolved resource path '{path}' is outside the configured file roots.");
    }

    internal string GetRootPath(string root)
    {
        if (root == null || root.Length == 0)
        {
            throw new GltfResourceException("Configured file roots must not be null or empty.");
        }

        if (Path.IsPathRooted(root))
        {
            return Path.GetFullPath(root);
        }

        GltfUriKind kind = UriReference.ClassifyReference(root);
        if (kind == GltfUriKind.File)
        {
            return Path.GetFullPath(GetLocalFilePath(root, "configured file root"));
        }

        if (kind != GltfUriKind.Relative)
        {
            throw new GltfResourceException(
                $"Configured file root '{root}' must be a file-system path or file URI.");
        }

        return Path.GetFullPath(root);
    }

    internal string GetLocalFilePath(string uri, string subject)
    {
        System.Uri parsed = new System.Uri(uri, System.UriKind.Absolute);
        if (!parsed.IsFile)
        {
            throw new GltfResourceException($"The {subject} must use the file scheme.");
        }

        if (parsed.Host.Length > 0
            && !string.Equals(parsed.Host, "localhost", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfResourceException(
                $"The {subject} uses a non-local file authority, which is denied by policy.");
        }

        return Path.GetFullPath(parsed.LocalPath);
    }

    internal bool ContainsParentPathSegment(string path)
    {
        int segmentStart = 0;
        int index = 0;
        while (index <= path.Length)
        {
            bool atSeparator = index == path.Length;
            if (!atSeparator)
            {
                atSeparator = path[index] == '/' || path[index] == '\\';
            }

            if (atSeparator)
            {
                if (index - segmentStart == 2
                    && path[segmentStart] == '.'
                    && path[segmentStart + 1] == '.')
                {
                    return true;
                }

                segmentStart = index + 1;
            }

            index++;
        }

        return false;
    }

    internal bool IsPathWithinRoot(string path, string root)
    {
        string fullPath = GetCanonicalPath(path);
        string fullRoot = GetCanonicalPath(root);
        System.StringComparison comparison = System.StringComparison.Ordinal;
        if (Path.DirectorySeparatorChar == '\\')
        {
            comparison = System.StringComparison.OrdinalIgnoreCase;
        }

        if (string.Equals(fullPath, fullRoot, comparison))
        {
            return true;
        }

        string rootPrefix = fullRoot;
        if (!rootPrefix.EndsWith(Path.DirectorySeparatorChar.ToString(), comparison)
            && !rootPrefix.EndsWith(Path.AltDirectorySeparatorChar.ToString(), comparison))
        {
            rootPrefix += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(rootPrefix, comparison);
    }

    internal string GetCanonicalPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? pathRoot = Path.GetPathRoot(fullPath);
        if (pathRoot == null)
        {
            return fullPath;
        }

        string remaining = fullPath.Substring(pathRoot.Length);
        char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
        string[] segments = remaining.Split(
            separators,
            System.StringSplitOptions.RemoveEmptyEntries);
        string current = pathRoot;
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            string candidate = Path.Combine(current, segments[segmentIndex]);
            System.IO.FileSystemInfo info;
            if (Directory.Exists(candidate))
            {
                info = new DirectoryInfo(candidate);
            }
            else if (File.Exists(candidate))
            {
                info = new FileInfo(candidate);
            }
            else
            {
                current = candidate;
                segmentIndex++;
                continue;
            }

            System.IO.FileSystemInfo? target = info.ResolveLinkTarget(true);
            if (target == null)
            {
                current = candidate;
            }
            else
            {
                current = Path.GetFullPath(target.FullName);
            }

            segmentIndex++;
        }

        return Path.GetFullPath(current);
    }

    internal string AppendQueryAndFragment(string fileUri, UriReferenceParts sourceParts)
    {
        string result = fileUri;
        if (sourceParts.HasQuery)
        {
            result += "?" + sourceParts.Query;
        }

        if (sourceParts.HasFragment)
        {
            result += "#" + sourceParts.Fragment;
        }

        return UriReference.NormalizeReference(result);
    }
}
