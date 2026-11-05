namespace uspto;

internal enum MirrorSelectionMode
{
    CurrentState = 0,
    Delta = 1,
    Snapshot = 2
}

internal class IncrementalMirrorSelectionRequest
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal BulkProduct[] CatalogProducts = new BulkProduct[0];
    internal DatasetManifest Manifest = new DatasetManifest();
    internal global::System.DateTimeOffset HighWaterMark = default(global::System.DateTimeOffset);
    internal bool HasHighWaterMark = false;
    internal global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);
}

internal class IncrementalMirrorSelectionEntry
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorSelectionMode Mode = MirrorSelectionMode.CurrentState;
    internal BulkProductFile? CatalogFile = null;
    internal DatasetManifestFile? ManifestFile = null;
    internal global::System.DateTimeOffset ReleaseTime = default(global::System.DateTimeOffset);
    internal bool HasReleaseTime = false;
    internal bool IsSelected = false;
    internal string? Reason = null;
}

internal class IncrementalMirrorSelectionResult
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal IncrementalMirrorSelectionEntry[] Entries = new IncrementalMirrorSelectionEntry[0];
    internal IncrementalMirrorSelectionEntry[] SelectedEntries = new IncrementalMirrorSelectionEntry[0];
    internal global::System.DateTimeOffset NextHighWaterMark = default(global::System.DateTimeOffset);
    internal bool HasNextHighWaterMark = false;
    internal bool SnapshotChanged = false;
}

internal static class IncrementalMirrorSelector
{
    internal static IncrementalMirrorSelectionResult SelectIncrementalMirror(IncrementalMirrorSelectionRequest request)
    {
        ValidateSelectionRequest(request);
        request.CancellationToken.ThrowIfCancellationRequested();
        global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> manifestFiles = IndexSelectedManifestFiles(request.Manifest, request.Family);
        global::System.Collections.Generic.Dictionary<string, bool> catalogKeys = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry> entries = new global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry>();
        bool snapshotFamily = IsSnapshotFamily(request.Family);
        bool snapshotChanged = false;
        int productIndex;
        for (productIndex = 0; productIndex < request.CatalogProducts.Length; productIndex += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            BulkProduct product = request.CatalogProducts[productIndex];
            MirrorPlanner.ValidateCatalogProduct(product);
            DatasetManifestProduct? manifestProduct = FindSelectedManifestProduct(request.Manifest, request.Family, product.ProductIdentifier ?? string.Empty);
            bool productChanged = snapshotFamily && SnapshotProductChanged(product, manifestProduct);
            bool deltaProduct = !snapshotFamily && IsDeltaProduct(request.Family, product);
            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                BulkProductFile catalogFile = product.Files[fileIndex];
                string productIdentifier = MirrorPlanner.ResolveProductIdentifier(product, catalogFile);
                MirrorPlanner.ValidateCatalogFile(catalogFile, productIdentifier);
                string key = MirrorPlanner.CreateKey(productIdentifier, catalogFile.FileName ?? string.Empty);
                if (catalogKeys.ContainsKey(key))
                {
                    throw new ValidationException("The catalog contains a duplicate incremental mirror file.", "CatalogProducts", "MIRROR_CATALOG_DUPLICATE", null);
                }

                catalogKeys[key] = true;
                DatasetManifestFile? manifestFile = null;
                if (manifestFiles.ContainsKey(key))
                {
                    manifestFile = manifestFiles[key];
                }

                IncrementalMirrorSelectionEntry entry = CreateSelectionEntry(productIdentifier, catalogFile, manifestFile, snapshotFamily, deltaProduct);
                ResolveReleaseTime(catalogFile, entry);
                if (snapshotFamily)
                {
                    SelectSnapshotEntry(entry, productChanged);
                    if (entry.IsSelected)
                    {
                        snapshotChanged = true;
                    }
                }
                else if (deltaProduct)
                {
                    SelectDeltaEntry(entry, request);
                }
                else
                {
                    SelectCurrentEntry(entry);
                }

                entries.Add(entry);
            }
        }

        if (snapshotFamily && AddRemovedSnapshotEntries(request.Manifest, request.Family, catalogKeys, entries, request.CancellationToken))
        {
            snapshotChanged = true;
        }

        return CreateSelectionResult(request, entries, snapshotChanged);
    }

    internal static void ValidateSelectionRequest(IncrementalMirrorSelectionRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("An incremental mirror selection request is required.", "request", "REQUIRED", null);
        }

        if (!BulkDatasetPresetResolver.IsSupportedPresetFamily(request.Family))
        {
            throw new ValidationException("A supported dataset family is required.", "Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }

        if (request.CatalogProducts == null)
        {
            throw new ValidationException("Catalog products are required.", "CatalogProducts", "REQUIRED", null);
        }

        if (request.Manifest == null)
        {
            throw new ValidationException("A dataset manifest is required.", "Manifest", "REQUIRED", null);
        }

        DatasetManifestStore.Validate(request.Manifest);
    }

    internal static global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> IndexSelectedManifestFiles(DatasetManifest manifest, DatasetFamily family)
    {
        global::System.Collections.Generic.Dictionary<string, DatasetManifestFile> result = new global::System.Collections.Generic.Dictionary<string, DatasetManifestFile>(global::System.StringComparer.Ordinal);
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            if (product.Family == family)
            {
                int fileIndex;
                for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
                {
                    DatasetManifestFile file = product.Files[fileIndex];
                    string key = MirrorPlanner.CreateKey(file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
                    if (result.ContainsKey(key))
                    {
                        throw new ValidationException("The manifest contains a duplicate incremental mirror file.", "Manifest", "MIRROR_MANIFEST_DUPLICATE", null);
                    }

                    result[key] = file;
                }
            }
        }

        return result;
    }

    internal static DatasetManifestProduct? FindSelectedManifestProduct(DatasetManifest manifest, DatasetFamily family, string productIdentifier)
    {
        DatasetManifestProduct? result = null;
        int index;
        for (index = 0; index < manifest.Products.Length; index += 1)
        {
            DatasetManifestProduct product = manifest.Products[index];
            if (product.Family == family && global::System.String.Equals(product.ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal))
            {
                if (result != null)
                {
                    throw new ValidationException("The manifest contains a duplicate product snapshot.", "Manifest", "MIRROR_MANIFEST_DUPLICATE", null);
                }

                result = product;
            }
        }

        return result;
    }

    internal static bool IsSnapshotFamily(DatasetFamily family)
    {
        switch (family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
            case DatasetFamily.PatentsViewGrant:
            case DatasetFamily.PatentsViewPreGrant:
            case DatasetFamily.PatentsViewAnnualized:
            case DatasetFamily.PatentsViewSorted:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsDeltaProduct(DatasetFamily family, BulkProduct product)
    {
        switch (family)
        {
            case DatasetFamily.PatentFileWrapperDelta:
            case DatasetFamily.PatentGrantFullText:
            case DatasetFamily.PatentApplicationFullText:
            case DatasetFamily.OfficeAction:
            case DatasetFamily.TrademarkApplicationAndStatus:
                return true;
            default:
                break;
        }

        string frequency = product.Frequency == null ? string.Empty : product.Frequency;
        return ContainsCadenceText(frequency, "daily") || ContainsCadenceText(frequency, "weekly") || ContainsCadenceText(frequency, "delta") || ContainsCadenceText(frequency, "incremental");
    }

    internal static bool ContainsCadenceText(string value, string expected)
    {
        return value.IndexOf(expected, global::System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool SnapshotProductChanged(BulkProduct product, DatasetManifestProduct? manifestProduct)
    {
        if (manifestProduct == null)
        {
            return false;
        }

        return product.HasLastModified && manifestProduct.HasLastModified && product.LastModified != manifestProduct.LastModified;
    }

    internal static IncrementalMirrorSelectionEntry CreateSelectionEntry(string productIdentifier, BulkProductFile catalogFile, DatasetManifestFile? manifestFile, bool snapshotFamily, bool deltaProduct)
    {
        IncrementalMirrorSelectionEntry result = new IncrementalMirrorSelectionEntry();
        result.ProductIdentifier = productIdentifier;
        result.FileName = catalogFile.FileName;
        result.CatalogFile = catalogFile;
        result.ManifestFile = manifestFile;
        if (snapshotFamily)
        {
            result.Mode = MirrorSelectionMode.Snapshot;
        }
        else if (deltaProduct)
        {
            result.Mode = MirrorSelectionMode.Delta;
        }

        return result;
    }

    internal static void ResolveReleaseTime(BulkProductFile file, IncrementalMirrorSelectionEntry entry)
    {
        if (file.HasReleaseDate)
        {
            entry.ReleaseTime = file.ReleaseDate;
            entry.HasReleaseTime = true;
        }
        else if (file.HasFileDate)
        {
            entry.ReleaseTime = file.FileDate;
            entry.HasReleaseTime = true;
        }
        else if (file.DataDateRange != null && file.DataDateRange.HasToDate)
        {
            entry.ReleaseTime = file.DataDateRange.ToDate;
            entry.HasReleaseTime = true;
        }
        else if (file.HasLastModified)
        {
            entry.ReleaseTime = file.LastModified;
            entry.HasReleaseTime = true;
        }
    }

    internal static void SelectSnapshotEntry(IncrementalMirrorSelectionEntry entry, bool productChanged)
    {
        if (entry.ManifestFile == null)
        {
            entry.IsSelected = true;
            entry.Reason = "SNAPSHOT_FILE_NEW";
        }
        else if (productChanged)
        {
            entry.IsSelected = true;
            entry.Reason = "SNAPSHOT_PRODUCT_CHANGED";
        }
        else if (entry.CatalogFile != null && MirrorPlanner.CatalogStateChanged(entry.CatalogFile, entry.ManifestFile))
        {
            entry.IsSelected = true;
            entry.Reason = "SNAPSHOT_FILE_CHANGED";
        }
        else
        {
            entry.Reason = "SNAPSHOT_UNCHANGED";
        }
    }

    internal static void SelectDeltaEntry(IncrementalMirrorSelectionEntry entry, IncrementalMirrorSelectionRequest request)
    {
        if (entry.ManifestFile != null)
        {
            if (entry.CatalogFile != null && MirrorPlanner.CatalogStateChanged(entry.CatalogFile, entry.ManifestFile))
            {
                entry.IsSelected = true;
                entry.Reason = "DELTA_CORRECTED";
            }
            else
            {
                entry.Reason = "DELTA_ALREADY_MANIFESTED";
            }

            return;
        }

        if (!request.HasHighWaterMark || !entry.HasReleaseTime)
        {
            entry.IsSelected = true;
            entry.Reason = entry.HasReleaseTime ? "DELTA_NEW" : "DELTA_DATE_UNAVAILABLE";
        }
        else if (entry.ReleaseTime > request.HighWaterMark)
        {
            entry.IsSelected = true;
            entry.Reason = "DELTA_AFTER_HIGH_WATER_MARK";
        }
        else
        {
            entry.Reason = "DELTA_AT_OR_BEFORE_HIGH_WATER_MARK";
        }
    }

    internal static void SelectCurrentEntry(IncrementalMirrorSelectionEntry entry)
    {
        if (entry.ManifestFile == null)
        {
            entry.IsSelected = true;
            entry.Reason = "FILE_NEW";
        }
        else if (entry.CatalogFile != null && MirrorPlanner.CatalogStateChanged(entry.CatalogFile, entry.ManifestFile))
        {
            entry.IsSelected = true;
            entry.Reason = "FILE_CHANGED";
        }
        else
        {
            entry.Reason = "FILE_UNCHANGED";
        }
    }

    internal static bool AddRemovedSnapshotEntries(DatasetManifest manifest, DatasetFamily family, global::System.Collections.Generic.Dictionary<string, bool> catalogKeys, global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry> entries, global::System.Threading.CancellationToken cancellationToken)
    {
        bool changed = false;
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            if (product.Family == family)
            {
                int fileIndex;
                for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DatasetManifestFile file = product.Files[fileIndex];
                    string key = MirrorPlanner.CreateKey(file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
                    if (!catalogKeys.ContainsKey(key))
                    {
                        IncrementalMirrorSelectionEntry entry = new IncrementalMirrorSelectionEntry();
                        entry.ProductIdentifier = file.ProductIdentifier;
                        entry.FileName = file.FileName;
                        entry.Mode = MirrorSelectionMode.Snapshot;
                        entry.ManifestFile = file;
                        entry.Reason = "SNAPSHOT_FILE_REMOVED";
                        entries.Add(entry);
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }

    internal static IncrementalMirrorSelectionResult CreateSelectionResult(IncrementalMirrorSelectionRequest request, global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry> entries, bool snapshotChanged)
    {
        IncrementalMirrorSelectionResult result = new IncrementalMirrorSelectionResult();
        result.Family = request.Family;
        result.Entries = entries.ToArray();
        result.SnapshotChanged = snapshotChanged;
        global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry> selected = new global::System.Collections.Generic.List<IncrementalMirrorSelectionEntry>();
        if (request.HasHighWaterMark)
        {
            result.NextHighWaterMark = request.HighWaterMark;
            result.HasNextHighWaterMark = true;
        }

        int index;
        for (index = 0; index < result.Entries.Length; index += 1)
        {
            IncrementalMirrorSelectionEntry entry = result.Entries[index];
            if (entry.IsSelected)
            {
                selected.Add(entry);
                if (entry.Mode == MirrorSelectionMode.Delta && entry.HasReleaseTime && (!result.HasNextHighWaterMark || entry.ReleaseTime > result.NextHighWaterMark))
                {
                    result.NextHighWaterMark = entry.ReleaseTime;
                    result.HasNextHighWaterMark = true;
                }
            }
        }

        result.SelectedEntries = selected.ToArray();
        return result;
    }
}
