namespace uspto;

internal class MirrorPlanRequest
{
    internal string? MirrorDirectory = null;
    internal BulkProduct[] CatalogProducts = new BulkProduct[0];
    internal DatasetManifest Manifest = new DatasetManifest();
    internal bool VerifyDigests = true;
    internal int ChecksumBufferSize = 81920;
    internal global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);
}

internal class MirrorPlanEntry
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorAction Action = MirrorAction.Unknown;
    internal string? Reason = null;
    internal BulkProductFile? CatalogFile = null;
    internal DatasetManifestFile? ManifestFile = null;
    internal string? LocalPath = null;
    internal string? PartialPath = null;
    internal long LocalSize = 0L;
    internal bool HasLocalSize = false;
}

internal class MirrorPlanResult
{
    internal MirrorPlanEntry[] Entries = new MirrorPlanEntry[0];
    internal int UnchangedCount = 0;
    internal int NewCount = 0;
    internal int ChangedCount = 0;
    internal int MissingCount = 0;
    internal int CorruptCount = 0;
    internal int ResumableCount = 0;
    internal int OrphanedCount = 0;
}

internal static class MirrorPlanner
{
    internal static MirrorPlanResult Plan(MirrorPlanRequest request)
    {
        ValidateRequest(request);
        request.CancellationToken.ThrowIfCancellationRequested();
        string root = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
        global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> manifestFiles = IndexManifestFiles(request.Manifest);
        global::System.Collections.Generic.Dictionary<string, bool> catalogKeys = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<MirrorPlanEntry> entries = new global::System.Collections.Generic.List<MirrorPlanEntry>();
        int productIndex;
        for (productIndex = 0; productIndex < request.CatalogProducts.Length; productIndex += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            BulkProduct product = request.CatalogProducts[productIndex];
            ValidateCatalogProduct(product);
            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                BulkProductFile catalogFile = product.Files[fileIndex];
                string productIdentifier = ResolveProductIdentifier(product, catalogFile);
                ValidateCatalogFile(catalogFile, productIdentifier);
                string key = CreateKey(productIdentifier, catalogFile.FileName ?? string.Empty);
                if (catalogKeys.ContainsKey(key))
                {
                    throw new ValidationException("The catalog contains a duplicate mirror file.", "CatalogProducts", "MIRROR_CATALOG_DUPLICATE", null);
                }

                catalogKeys[key] = true;
                DatasetManifestFile? manifestFile = null;
                if (manifestFiles.ContainsKey(key))
                {
                    manifestFile = manifestFiles[key];
                }

                entries.Add(ClassifyCatalogFile(root, productIdentifier, catalogFile, manifestFile, request));
            }
        }

        AddOrphanedEntries(root, request.Manifest, catalogKeys, entries, request.CancellationToken);
        return CreateResult(entries);
    }

    internal static void ValidateRequest(MirrorPlanRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory))
        {
            throw new ValidationException("A mirror directory is required.", "MirrorDirectory", "REQUIRED", null);
        }

        if (request.CatalogProducts == null)
        {
            throw new ValidationException("Catalog products are required.", "CatalogProducts", "REQUIRED", null);
        }

        if (request.Manifest == null)
        {
            throw new ValidationException("A dataset manifest is required.", "Manifest", "REQUIRED", null);
        }

        if (request.ChecksumBufferSize <= 0)
        {
            throw new ValidationException("The checksum buffer size must be positive.", "ChecksumBufferSize", "INVALID_LIMIT", null);
        }

        DatasetManifestStore.Validate(request.Manifest);
    }

    internal static global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> IndexManifestFiles(DatasetManifest manifest)
    {
        global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> result = new global::System.Collections.Generic.Dictionary<string, DatasetManifestFile>(global::System.StringComparer.Ordinal);
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                DatasetManifestFile file = product.Files[fileIndex];
                string key = CreateKey(file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
                if (result.ContainsKey(key))
                {
                    throw new ValidationException("The manifest contains a duplicate mirror file.", "Manifest", "MIRROR_MANIFEST_DUPLICATE", null);
                }

                result[key] = file;
            }
        }

        return result;
    }

    internal static void ValidateCatalogProduct(BulkProduct product)
    {
        if (product == null || global::System.String.IsNullOrWhiteSpace(product.ProductIdentifier) || product.Files == null)
        {
            throw new ValidationException("A catalog product is invalid.", "CatalogProducts", "MIRROR_CATALOG_PRODUCT_INVALID", null);
        }
    }

    internal static string ResolveProductIdentifier(BulkProduct product, BulkProductFile file)
    {
        if (file == null)
        {
            throw new ValidationException("A catalog file is invalid.", "CatalogProducts", "MIRROR_CATALOG_FILE_INVALID", null);
        }

        if (!global::System.String.IsNullOrWhiteSpace(file.ProductIdentifier) && !global::System.String.Equals(product.ProductIdentifier, file.ProductIdentifier, global::System.StringComparison.Ordinal))
        {
            throw new ValidationException("A catalog file belongs to a different product.", "ProductIdentifier", "MIRROR_CATALOG_PRODUCT_MISMATCH", null);
        }

        return product.ProductIdentifier ?? string.Empty;
    }

    internal static void ValidateCatalogFile(BulkProductFile file, string productIdentifier)
    {
        if (file == null || global::System.String.IsNullOrWhiteSpace(productIdentifier) || global::System.String.IsNullOrWhiteSpace(file.FileName) || file.Checksums == null)
        {
            throw new ValidationException("A catalog file is invalid.", "CatalogProducts", "MIRROR_CATALOG_FILE_INVALID", null);
        }

        if (file.HasSize && file.Size < 0L)
        {
            throw new ValidationException("A catalog file size cannot be negative.", "Size", "MIRROR_CATALOG_FILE_INVALID", null);
        }
    }

    internal static MirrorPlanEntry ClassifyCatalogFile(string root, string productIdentifier, BulkProductFile catalogFile, DatasetManifestFile? manifestFile, MirrorPlanRequest request)
    {
        MirrorPlanEntry entry = CreateCatalogEntry(productIdentifier, catalogFile, manifestFile);
        if (manifestFile == null)
        {
            entry.Action = MirrorAction.New;
            entry.Reason = "NOT_IN_MANIFEST";
            return entry;
        }

        if (CatalogStateChanged(catalogFile, manifestFile))
        {
            entry.Action = MirrorAction.Changed;
            entry.Reason = "CATALOG_METADATA_CHANGED";
            SetResolvedPaths(root, manifestFile, entry);
            return entry;
        }

        SetResolvedPaths(root, manifestFile, entry);
        if (entry.LocalPath != null && global::System.IO.File.Exists(entry.LocalPath))
        {
            global::System.IO.FileInfo information = new global::System.IO.FileInfo(entry.LocalPath);
            entry.LocalSize = information.Length;
            entry.HasLocalSize = true;
            if (!LocalSizeMatches(information.Length, catalogFile, manifestFile))
            {
                entry.Action = MirrorAction.Corrupt;
                entry.Reason = "LOCAL_SIZE_MISMATCH";
                return entry;
            }

            if (request.VerifyDigests && manifestFile.DigestVerified && !global::System.String.IsNullOrWhiteSpace(manifestFile.Digest) && BulkChecksumVerifier.IsSupportedAlgorithm(manifestFile.DigestAlgorithm))
            {
                ChecksumExpectation expectation = new ChecksumExpectation();
                expectation.Algorithm = manifestFile.DigestAlgorithm;
                expectation.Value = manifestFile.Digest;
                ChecksumResult[] checksums = BulkChecksumVerifier.VerifyFile(entry.LocalPath, new ChecksumExpectation[] { expectation }, request.ChecksumBufferSize, request.CancellationToken);
                if (BulkChecksumVerifier.HasMismatch(checksums))
                {
                    entry.Action = MirrorAction.Corrupt;
                    entry.Reason = "LOCAL_DIGEST_MISMATCH";
                    return entry;
                }
            }

            entry.Action = MirrorAction.Unchanged;
            entry.Reason = "LOCAL_FILE_CURRENT";
            return entry;
        }

        return ClassifyMissingFinal(entry, catalogFile, manifestFile);
    }

    internal static MirrorPlanEntry CreateCatalogEntry(string productIdentifier, BulkProductFile catalogFile, DatasetManifestFile? manifestFile)
    {
        MirrorPlanEntry result = new MirrorPlanEntry();
        result.ProductIdentifier = productIdentifier;
        result.FileName = catalogFile.FileName;
        result.CatalogFile = catalogFile;
        result.ManifestFile = manifestFile;
        return result;
    }

    internal static bool CatalogStateChanged(BulkProductFile catalogFile, DatasetManifestFile manifestFile)
    {
        if (catalogFile.HasSize && manifestFile.HasSize && catalogFile.Size != manifestFile.Size)
        {
            return true;
        }

        if (catalogFile.HasLastModified && manifestFile.HasLastModified && catalogFile.LastModified != manifestFile.LastModified)
        {
            return true;
        }

        if (global::System.String.IsNullOrWhiteSpace(manifestFile.Digest) || !BulkChecksumVerifier.IsSupportedAlgorithm(manifestFile.DigestAlgorithm))
        {
            return false;
        }

        int index;
        for (index = 0; index < catalogFile.Checksums.Length; index += 1)
        {
            ChecksumExpectation expectation = catalogFile.Checksums[index];
            if (expectation != null && expectation.Algorithm == manifestFile.DigestAlgorithm)
            {
                NormalizedChecksumValue normalized = BulkChecksumVerifier.NormalizeExpected(expectation);
                if (normalized.IsValid)
                {
                    return !global::System.String.Equals(normalized.Value, manifestFile.Digest, global::System.StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return false;
    }

    internal static void SetResolvedPaths(string root, DatasetManifestFile manifestFile, MirrorPlanEntry entry)
    {
        entry.LocalPath = ResolveRelativePath(root, manifestFile.LocalPath);
        entry.PartialPath = ResolveRelativePath(root, manifestFile.PartialPath);
    }

    internal static string? ResolveRelativePath(string root, string? relativePath)
    {
        if (global::System.String.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        string resolved = global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(root, relativePath));
        string prefix = root.EndsWith(global::System.IO.Path.DirectorySeparatorChar) ? root : root + global::System.IO.Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(prefix, global::System.StringComparison.Ordinal))
        {
            throw new ValidationException("A mirror artifact path escapes the mirror directory.", "Manifest", "MANIFEST_PATH_INVALID", null);
        }

        return resolved;
    }

    internal static bool LocalSizeMatches(long localSize, BulkProductFile catalogFile, DatasetManifestFile manifestFile)
    {
        if (manifestFile.HasSize)
        {
            return localSize == manifestFile.Size;
        }

        if (catalogFile.HasSize)
        {
            return localSize == catalogFile.Size;
        }

        return true;
    }

    internal static MirrorPlanEntry ClassifyMissingFinal(MirrorPlanEntry entry, BulkProductFile catalogFile, DatasetManifestFile manifestFile)
    {
        if (entry.PartialPath != null && global::System.IO.File.Exists(entry.PartialPath))
        {
            global::System.IO.FileInfo information = new global::System.IO.FileInfo(entry.PartialPath);
            entry.LocalSize = information.Length;
            entry.HasLocalSize = true;
            long expectedSize = manifestFile.HasSize ? manifestFile.Size : catalogFile.Size;
            bool hasExpectedSize = manifestFile.HasSize || catalogFile.HasSize;
            if (hasExpectedSize && information.Length > expectedSize)
            {
                entry.Action = MirrorAction.Corrupt;
                entry.Reason = "PARTIAL_SIZE_INVALID";
                return entry;
            }

            bool hasValidator = !global::System.String.IsNullOrWhiteSpace(manifestFile.ETag) || manifestFile.HasLastModified;
            if (information.Length > 0L && hasValidator)
            {
                entry.Action = MirrorAction.Resumable;
                entry.Reason = "VALIDATED_PARTIAL_AVAILABLE";
                return entry;
            }
        }

        entry.Action = MirrorAction.Missing;
        entry.Reason = "LOCAL_FILE_MISSING";
        return entry;
    }

    internal static void AddOrphanedEntries(string root, DatasetManifest manifest, global::System.Collections.Generic.Dictionary<string, bool> catalogKeys, global::System.Collections.Generic.List<MirrorPlanEntry> entries, global::System.Threading.CancellationToken cancellationToken)
    {
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DatasetManifestFile file = product.Files[fileIndex];
                string key = CreateKey(file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
                if (!catalogKeys.ContainsKey(key))
                {
                    MirrorPlanEntry entry = new MirrorPlanEntry();
                    entry.ProductIdentifier = file.ProductIdentifier;
                    entry.FileName = file.FileName;
                    entry.Action = MirrorAction.Orphaned;
                    entry.Reason = "NOT_IN_CATALOG";
                    entry.ManifestFile = file;
                    SetResolvedPaths(root, file, entry);
                    if (entry.LocalPath != null && global::System.IO.File.Exists(entry.LocalPath))
                    {
                        entry.LocalSize = new global::System.IO.FileInfo(entry.LocalPath).Length;
                        entry.HasLocalSize = true;
                    }

                    entries.Add(entry);
                }
            }
        }
    }

    internal static MirrorPlanResult CreateResult(global::System.Collections.Generic.List<MirrorPlanEntry> entries)
    {
        MirrorPlanResult result = new MirrorPlanResult();
        result.Entries = entries.ToArray();
        int index;
        for (index = 0; index < result.Entries.Length; index += 1)
        {
            switch (result.Entries[index].Action)
            {
                case MirrorAction.Unchanged:
                    result.UnchangedCount += 1;
                    break;
                case MirrorAction.New:
                    result.NewCount += 1;
                    break;
                case MirrorAction.Changed:
                    result.ChangedCount += 1;
                    break;
                case MirrorAction.Missing:
                    result.MissingCount += 1;
                    break;
                case MirrorAction.Corrupt:
                    result.CorruptCount += 1;
                    break;
                case MirrorAction.Resumable:
                    result.ResumableCount += 1;
                    break;
                case MirrorAction.Orphaned:
                    result.OrphanedCount += 1;
                    break;
                default:
                    break;
            }
        }

        return result;
    }

    internal static string CreateKey(string productIdentifier, string fileName)
    {
        return productIdentifier.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + productIdentifier + fileName;
    }
}
