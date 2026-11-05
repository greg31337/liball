namespace uspto;

internal interface IBulkDatasetPresetSource
{
    BulkDatasetPreset[] GetBulkDatasetPresets(DatasetFamily[] families, RequestContext context);
}

internal class BulkLocalRefreshRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetFamily[] DatasetFamilies = new DatasetFamily[0];
    internal int MaximumParallelTransfers = 1;
    internal int StaleLockSeconds = 3600;
    internal global::System.DateTimeOffset Now = default(global::System.DateTimeOffset);
    internal global::System.DateTimeOffset HighWaterMark = default(global::System.DateTimeOffset);
    internal bool HasHighWaterMark = false;
    internal bool VerifyChecksums = true;
    internal bool VerifyArchives = true;
    internal int ChecksumBufferSize = 81920;
    internal ExtractionLimits ArchiveLimits = new ExtractionLimits();
    internal MirrorPipelineStage LastStage = MirrorPipelineStage.Parse;
    internal RequestContext Context = new RequestContext();
    internal string[] Secrets = new string[0];
}

internal class BulkLocalRefreshFileResult
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal IncrementalMirrorSelectionEntry? Selection = null;
    internal MirrorPlanEntry Plan = new MirrorPlanEntry();
    internal MirrorSynchronizationEntryResult? Synchronization = null;
    internal MirrorStagePipelineResult? Pipeline = null;
    internal MirrorFileVerificationResult? Verification = null;
    internal bool WasIncrementallySelected = false;
    internal bool WasProcessed = false;
    internal bool WasSkipped = false;
    internal bool IsComplete = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal class BulkLocalRefreshFamilyResult
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal BulkDatasetPreset Preset = new BulkDatasetPreset();
    internal IncrementalMirrorSelectionResult Selection = new IncrementalMirrorSelectionResult();
    internal MirrorPlanResult Plan = new MirrorPlanResult();
    internal MirrorSynchronizationResult Synchronization = new MirrorSynchronizationResult();
    internal bool IsAvailable = false;
    internal bool MetadataPersisted = false;
    internal bool IsComplete = false;
}

internal class BulkLocalRefreshResult
{
    internal BulkDatasetPreset[] Presets = new BulkDatasetPreset[0];
    internal BulkLocalRefreshFamilyResult[] Families = new BulkLocalRefreshFamilyResult[0];
    internal BulkLocalRefreshFileResult[] Files = new BulkLocalRefreshFileResult[0];
    internal MirrorVerificationResult Verification = new MirrorVerificationResult();
    internal DatasetManifest Manifest = new DatasetManifest();
    internal bool IsComplete = false;
    internal int AvailableFamilyCount = 0;
    internal int UnavailableFamilyCount = 0;
    internal int ProcessedFileCount = 0;
    internal int SkippedFileCount = 0;
    internal int FailedFileCount = 0;
}

internal class BulkLocalRefreshCoordinator
{
    internal IBulkDatasetPresetSource Discovery;
    internal MirrorSynchronizer Synchronizer;
    internal MirrorStagePipeline Pipeline;

    internal BulkLocalRefreshCoordinator(IBulkDatasetPresetSource discovery, MirrorSynchronizer synchronizer, MirrorStagePipeline pipeline)
    {
        if (discovery == null || synchronizer == null || pipeline == null)
        {
            throw new ValidationException("Bulk-to-local refresh dependencies are required.", "refresh", "REQUIRED", null);
        }

        Discovery = discovery;
        Synchronizer = synchronizer;
        Pipeline = pipeline;
    }

    internal BulkLocalRefreshResult Refresh(BulkLocalRefreshRequest request)
    {
        ValidateRefreshRequest(request);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        BulkDatasetPreset[] presets = Discovery.GetBulkDatasetPresets(request.DatasetFamilies, request.Context);
        ValidateDiscoveredPresets(request.DatasetFamilies, presets);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        string directory = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
        DatasetManifest manifest = LoadOrCreateRefreshManifest(directory, request);
        global::System.Collections.Generic.List<BulkLocalRefreshFamilyResult> familyResults = new global::System.Collections.Generic.List<BulkLocalRefreshFamilyResult>();
        global::System.Collections.Generic.List<BulkLocalRefreshFileResult> fileResults = new global::System.Collections.Generic.List<BulkLocalRefreshFileResult>();
        int familyIndex;
        for (familyIndex = 0; familyIndex < request.DatasetFamilies.Length; familyIndex += 1)
        {
            request.Context.CancellationToken.ThrowIfCancellationRequested();
            DatasetFamily family = request.DatasetFamilies[familyIndex];
            BulkDatasetPreset preset = FindPreset(presets, family);
            manifest = DatasetManifestStore.Load(directory);
            BulkLocalRefreshFamilyResult familyResult = RefreshFamily(request, preset, manifest, fileResults);
            familyResults.Add(familyResult);
        }

        DatasetManifest finalManifest = DatasetManifestStore.Load(directory);
        DatasetManifest selectedManifest = CreateSelectedManifest(finalManifest, request.DatasetFamilies);
        MirrorVerificationResult verification = MirrorVerifier.Verify(CreateVerificationRequest(request, selectedManifest));
        ApplyVerification(fileResults, verification);
        return CreateRefreshResult(presets, familyResults, fileResults, verification, finalManifest);
    }

    internal BulkLocalRefreshFamilyResult RefreshFamily(BulkLocalRefreshRequest request, BulkDatasetPreset preset, DatasetManifest manifest, global::System.Collections.Generic.List<BulkLocalRefreshFileResult> allFiles)
    {
        DatasetManifest familyManifest = CreateFamilyManifest(manifest, preset.Family);
        IncrementalMirrorSelectionRequest selectionRequest = new IncrementalMirrorSelectionRequest();
        selectionRequest.Family = preset.Family;
        selectionRequest.CatalogProducts = preset.Products;
        selectionRequest.Manifest = familyManifest;
        selectionRequest.HighWaterMark = request.HighWaterMark;
        selectionRequest.HasHighWaterMark = request.HasHighWaterMark;
        selectionRequest.CancellationToken = request.Context.CancellationToken;
        IncrementalMirrorSelectionResult selection = IncrementalMirrorSelector.SelectIncrementalMirror(selectionRequest);

        MirrorPlanRequest planRequest = new MirrorPlanRequest();
        planRequest.MirrorDirectory = request.MirrorDirectory;
        planRequest.CatalogProducts = preset.Products;
        planRequest.Manifest = familyManifest;
        planRequest.VerifyDigests = request.VerifyChecksums;
        planRequest.ChecksumBufferSize = request.ChecksumBufferSize;
        planRequest.CancellationToken = request.Context.CancellationToken;
        MirrorPlanResult plan = MirrorPlanner.Plan(planRequest);
        BulkLocalRefreshFileResult[] familyFiles = CreateFileResults(preset.Family, plan, selection);
        AddFileResults(allFiles, familyFiles);
        MirrorPlanEntry[] transfers = SelectTransferEntries(plan, selection);
        MirrorSynchronizationResult synchronization = Synchronize(request, preset.Family, manifest, transfers);
        ApplySynchronization(familyFiles, synchronization);
        RunPipelines(request, familyFiles);
        bool metadataPersisted = PersistCatalogMetadata(request, preset);

        BulkLocalRefreshFamilyResult result = new BulkLocalRefreshFamilyResult();
        result.Family = preset.Family;
        result.Preset = preset;
        result.Selection = selection;
        result.Plan = plan;
        result.Synchronization = synchronization;
        result.IsAvailable = preset.IsAvailable;
        result.MetadataPersisted = metadataPersisted;
        return result;
    }

    internal static void ValidateRefreshRequest(BulkLocalRefreshRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory) || request.DatasetFamilies == null || request.Context == null || request.Secrets == null || request.ArchiveLimits == null)
        {
            throw new ValidationException("Complete bulk-to-local refresh inputs are required.", "request", "REQUIRED", null);
        }

        if (request.DatasetFamilies.Length == 0)
        {
            throw new ValidationException("At least one dataset family is required.", "DatasetFamilies", "REQUIRED", null);
        }

        BulkDatasetPresetResolver.ValidatePresetFamilies(request.DatasetFamilies);
        if (request.MaximumParallelTransfers <= 0 || request.StaleLockSeconds <= 0 || request.ChecksumBufferSize <= 0)
        {
            throw new ValidationException("Bulk-to-local refresh limits must be positive.", "request", "INVALID_LIMIT", null);
        }

        if (request.LastStage < MirrorPipelineStage.Download || request.LastStage > MirrorPipelineStage.Parse)
        {
            throw new ValidationException("The final refresh stage is invalid.", "LastStage", "MIRROR_PIPELINE_INVALID", null);
        }
    }

    internal static void ValidateDiscoveredPresets(DatasetFamily[] families, BulkDatasetPreset[] presets)
    {
        if (presets == null || presets.Length != families.Length)
        {
            throw new ValidationException("Discovery must return one preset for every selected family.", "presets", "MIRROR_DISCOVERY_INCOMPLETE", null);
        }

        global::System.Collections.Generic.HashSet<DatasetFamily> discovered = new global::System.Collections.Generic.HashSet<DatasetFamily>();
        int index;
        for (index = 0; index < presets.Length; index += 1)
        {
            BulkDatasetPreset preset = presets[index];
            if (preset == null || preset.Products == null || !ContainsFamily(families, preset.Family) || !discovered.Add(preset.Family))
            {
                throw new ValidationException("Discovery returned an invalid dataset preset.", "presets", "MIRROR_DISCOVERY_INVALID", null);
            }
        }
    }

    internal static bool ContainsFamily(DatasetFamily[] families, DatasetFamily family)
    {
        int index;
        for (index = 0; index < families.Length; index += 1)
        {
            if (families[index] == family)
            {
                return true;
            }
        }

        return false;
    }

    internal static BulkDatasetPreset FindPreset(BulkDatasetPreset[] presets, DatasetFamily family)
    {
        int index;
        for (index = 0; index < presets.Length; index += 1)
        {
            if (presets[index].Family == family)
            {
                return presets[index];
            }
        }

        throw new ValidationException("The selected dataset preset was not discovered.", "presets", "MIRROR_DISCOVERY_INCOMPLETE", null);
    }

    internal static DatasetManifest LoadOrCreateRefreshManifest(string directory, BulkLocalRefreshRequest request)
    {
        if (global::System.IO.File.Exists(DatasetManifestStore.GetPath(directory)))
        {
            return DatasetManifestStore.Load(directory);
        }

        global::System.DateTimeOffset now = request.Now == default(global::System.DateTimeOffset) ? global::System.DateTimeOffset.UtcNow : request.Now;
        DatasetManifest manifest = new DatasetManifest();
        manifest.ManifestIdentifier = "bulk-local-refresh";
        manifest.CreatedAt = now;
        manifest.HasCreatedAt = true;
        manifest.Provenance.Source = DataProvenance.LocalMirror;
        manifest.Provenance.IsAuthoritative = false;
        return DatasetManifestStore.Save(directory, manifest, request.Secrets);
    }

    internal static DatasetManifest CreateFamilyManifest(DatasetManifest manifest, DatasetFamily family)
    {
        return CreateSelectedManifest(manifest, new DatasetFamily[] { family });
    }

    internal static DatasetManifest CreateSelectedManifest(DatasetManifest manifest, DatasetFamily[] families)
    {
        global::System.Collections.Generic.List<DatasetManifestProduct> products = new global::System.Collections.Generic.List<DatasetManifestProduct>();
        int index;
        for (index = 0; index < manifest.Products.Length; index += 1)
        {
            if (ContainsFamily(families, manifest.Products[index].Family))
            {
                products.Add(manifest.Products[index]);
            }
        }

        DatasetManifest result = new DatasetManifest();
        result.FormatVersion = manifest.FormatVersion;
        result.ManifestIdentifier = manifest.ManifestIdentifier;
        result.CreatedAt = manifest.CreatedAt;
        result.HasCreatedAt = manifest.HasCreatedAt;
        result.UpdatedAt = manifest.UpdatedAt;
        result.HasUpdatedAt = manifest.HasUpdatedAt;
        result.Products = products.ToArray();
        result.Provenance = manifest.Provenance;
        return result;
    }

    internal static BulkLocalRefreshFileResult[] CreateFileResults(DatasetFamily family, MirrorPlanResult plan, IncrementalMirrorSelectionResult selection)
    {
        BulkLocalRefreshFileResult[] results = new BulkLocalRefreshFileResult[plan.Entries.Length];
        int index;
        for (index = 0; index < plan.Entries.Length; index += 1)
        {
            MirrorPlanEntry entry = plan.Entries[index];
            IncrementalMirrorSelectionEntry? selected = FindSelection(selection.Entries, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            BulkLocalRefreshFileResult result = new BulkLocalRefreshFileResult();
            result.Family = family;
            result.ProductIdentifier = entry.ProductIdentifier;
            result.FileName = entry.FileName;
            result.Selection = selected;
            result.Plan = entry;
            result.WasIncrementallySelected = selected != null && selected.IsSelected;
            results[index] = result;
        }

        return results;
    }

    internal static IncrementalMirrorSelectionEntry? FindSelection(IncrementalMirrorSelectionEntry[] entries, string productIdentifier, string fileName)
    {
        int index;
        for (index = 0; index < entries.Length; index += 1)
        {
            if (global::System.String.Equals(entries[index].ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && global::System.String.Equals(entries[index].FileName, fileName, global::System.StringComparison.Ordinal))
            {
                return entries[index];
            }
        }

        return null;
    }

    internal static void AddFileResults(global::System.Collections.Generic.List<BulkLocalRefreshFileResult> destination, BulkLocalRefreshFileResult[] files)
    {
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            destination.Add(files[index]);
        }
    }

    internal static MirrorPlanEntry[] SelectTransferEntries(MirrorPlanResult plan, IncrementalMirrorSelectionResult selection)
    {
        global::System.Collections.Generic.List<MirrorPlanEntry> results = new global::System.Collections.Generic.List<MirrorPlanEntry>();
        int index;
        for (index = 0; index < plan.Entries.Length; index += 1)
        {
            MirrorPlanEntry entry = plan.Entries[index];
            IncrementalMirrorSelectionEntry? selected = FindSelection(selection.Entries, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            if (selected != null && selected.IsSelected)
            {
                if (entry.Action == MirrorAction.Unchanged)
                {
                    results.Add(CreateChangedTransferEntry(entry));
                }
                else if (MirrorSynchronizer.IsTransferAction(entry.Action))
                {
                    results.Add(entry);
                }
            }
            else if (entry.Action == MirrorAction.Missing || entry.Action == MirrorAction.Corrupt || entry.Action == MirrorAction.Resumable)
            {
                results.Add(entry);
            }
        }

        return results.ToArray();
    }

    internal static MirrorPlanEntry CreateChangedTransferEntry(MirrorPlanEntry entry)
    {
        MirrorPlanEntry result = new MirrorPlanEntry();
        result.ProductIdentifier = entry.ProductIdentifier;
        result.FileName = entry.FileName;
        result.Action = MirrorAction.Changed;
        result.Reason = "INCREMENTAL_SNAPSHOT_CHANGED";
        result.CatalogFile = entry.CatalogFile;
        result.ManifestFile = entry.ManifestFile;
        result.LocalPath = entry.LocalPath;
        result.PartialPath = entry.PartialPath;
        result.LocalSize = entry.LocalSize;
        result.HasLocalSize = entry.HasLocalSize;
        return result;
    }

    internal MirrorSynchronizationResult Synchronize(BulkLocalRefreshRequest request, DatasetFamily family, DatasetManifest manifest, MirrorPlanEntry[] entries)
    {
        if (entries.Length == 0)
        {
            return new MirrorSynchronizationResult { Manifest = manifest };
        }

        MirrorSynchronizationRequest synchronizationRequest = new MirrorSynchronizationRequest();
        synchronizationRequest.MirrorDirectory = request.MirrorDirectory;
        synchronizationRequest.Family = family;
        synchronizationRequest.Entries = entries;
        synchronizationRequest.Manifest = manifest;
        synchronizationRequest.MaximumParallelTransfers = request.MaximumParallelTransfers;
        synchronizationRequest.StaleLockSeconds = request.StaleLockSeconds;
        synchronizationRequest.Now = request.Now;
        synchronizationRequest.Context = request.Context;
        synchronizationRequest.Secrets = request.Secrets;
        return Synchronizer.Synchronize(synchronizationRequest);
    }

    internal static void ApplySynchronization(BulkLocalRefreshFileResult[] files, MirrorSynchronizationResult synchronization)
    {
        int index;
        for (index = 0; index < synchronization.Entries.Length; index += 1)
        {
            MirrorSynchronizationEntryResult entry = synchronization.Entries[index];
            BulkLocalRefreshFileResult? file = FindFile(files, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            if (file != null)
            {
                file.Synchronization = entry;
                file.WasProcessed = true;
                if (entry.Download.State != DownloadState.Completed)
                {
                    file.FailureCode = entry.Download.FailureCode ?? "MIRROR_REFRESH_TRANSFER_FAILED";
                    file.FailureMessage = entry.Download.FailureMessage ?? "The refresh transfer did not complete.";
                }
            }
        }
    }

    internal void RunPipelines(BulkLocalRefreshRequest request, BulkLocalRefreshFileResult[] files)
    {
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            request.Context.CancellationToken.ThrowIfCancellationRequested();
            BulkLocalRefreshFileResult file = files[index];
            if (file.FailureCode != null)
            {
                continue;
            }

            bool synchronized = file.Synchronization != null && file.Synchronization.Download.State == DownloadState.Completed;
            bool unchanged = file.Plan.Action == MirrorAction.Unchanged;
            if (!synchronized && !unchanged)
            {
                file.WasSkipped = true;
                continue;
            }

            MirrorStagePipelineRequest pipelineRequest = new MirrorStagePipelineRequest();
            pipelineRequest.MirrorDirectory = request.MirrorDirectory;
            pipelineRequest.Family = file.Family;
            pipelineRequest.ProductIdentifier = file.ProductIdentifier;
            pipelineRequest.FileName = file.FileName;
            pipelineRequest.LastStage = request.LastStage;
            pipelineRequest.StaleLockSeconds = request.StaleLockSeconds;
            pipelineRequest.Now = request.Now;
            pipelineRequest.Context = request.Context;
            pipelineRequest.Secrets = request.Secrets;
            file.Pipeline = Pipeline.Run(pipelineRequest);
            file.WasProcessed = true;
            if (!file.Pipeline.IsComplete)
            {
                MirrorPipelineStageResult? failed = MirrorRepairCoordinator.FindFailedPipelineStage(file.Pipeline);
                file.FailureCode = failed == null ? "MIRROR_REFRESH_PIPELINE_FAILED" : failed.FailureCode;
                file.FailureMessage = failed == null ? "The refresh pipeline did not complete." : failed.FailureMessage;
            }
        }
    }

    internal static BulkLocalRefreshFileResult? FindFile(BulkLocalRefreshFileResult[] files, string productIdentifier, string fileName)
    {
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            if (global::System.String.Equals(files[index].ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && global::System.String.Equals(files[index].FileName, fileName, global::System.StringComparison.Ordinal))
            {
                return files[index];
            }
        }

        return null;
    }

    internal static bool PersistCatalogMetadata(BulkLocalRefreshRequest request, BulkDatasetPreset preset)
    {
        MirrorLockRequest lockRequest = new MirrorLockRequest();
        lockRequest.MirrorDirectory = request.MirrorDirectory;
        lockRequest.StaleAfterSeconds = request.StaleLockSeconds;
        lockRequest.Now = request.Now == default(global::System.DateTimeOffset) ? global::System.DateTimeOffset.UtcNow : request.Now;
        lockRequest.CancellationToken = request.Context.CancellationToken;
        MirrorLockHandle mirrorLock = MirrorStateCoordinator.Acquire(lockRequest);
        if (!mirrorLock.IsAcquired)
        {
            return false;
        }

        using (MirrorSynchronizationLockLease lease = new MirrorSynchronizationLockLease(mirrorLock))
        {
            DatasetManifest manifest = DatasetManifestStore.Load(mirrorLock.MirrorDirectory);
            int index;
            for (index = 0; index < preset.Products.Length; index += 1)
            {
                BulkProduct catalog = preset.Products[index];
                DatasetManifestProduct? product = MirrorSynchronizer.FindOptionalManifestProduct(manifest, catalog.ProductIdentifier ?? string.Empty, preset.Family);
                if (product != null)
                {
                    product.Title = catalog.Title;
                    product.CatalogUri = catalog.Provenance.SourceUri;
                    product.LastModified = catalog.LastModified;
                    product.HasLastModified = catalog.HasLastModified;
                }
            }

            DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
        }

        return true;
    }

    internal static MirrorVerificationRequest CreateVerificationRequest(BulkLocalRefreshRequest request, DatasetManifest manifest)
    {
        MirrorVerificationRequest result = new MirrorVerificationRequest();
        result.MirrorDirectory = request.MirrorDirectory;
        result.Manifest = manifest;
        result.VerifyChecksums = request.VerifyChecksums;
        result.VerifyArchives = request.VerifyArchives;
        result.ChecksumBufferSize = request.ChecksumBufferSize;
        result.ArchiveLimits = request.ArchiveLimits;
        result.Context = request.Context;
        return result;
    }

    internal static void ApplyVerification(global::System.Collections.Generic.List<BulkLocalRefreshFileResult> files, MirrorVerificationResult verification)
    {
        int index;
        for (index = 0; index < files.Count; index += 1)
        {
            BulkLocalRefreshFileResult file = files[index];
            MirrorFileVerificationResult? verified = FindVerification(verification.Files, file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
            file.Verification = verified;
            if (file.WasSkipped && (file.Plan.Action == MirrorAction.New || file.Plan.Action == MirrorAction.Changed || file.Plan.Action == MirrorAction.Orphaned))
            {
                file.IsComplete = true;
            }
            else if (verified != null && verified.IsValid && file.FailureCode == null)
            {
                file.IsComplete = true;
            }
            else if (file.FailureCode == null)
            {
                file.FailureCode = "MIRROR_REFRESH_VERIFICATION_FAILED";
                file.FailureMessage = "The refreshed file did not pass verification.";
            }
        }
    }

    internal static MirrorFileVerificationResult? FindVerification(MirrorFileVerificationResult[] files, string productIdentifier, string fileName)
    {
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            if (global::System.String.Equals(files[index].ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && global::System.String.Equals(files[index].FileName, fileName, global::System.StringComparison.Ordinal))
            {
                return files[index];
            }
        }

        return null;
    }

    internal static BulkLocalRefreshResult CreateRefreshResult(BulkDatasetPreset[] presets, global::System.Collections.Generic.List<BulkLocalRefreshFamilyResult> families, global::System.Collections.Generic.List<BulkLocalRefreshFileResult> files, MirrorVerificationResult verification, DatasetManifest manifest)
    {
        BulkLocalRefreshResult result = new BulkLocalRefreshResult();
        result.Presets = presets;
        result.Families = families.ToArray();
        result.Files = files.ToArray();
        result.Verification = verification;
        result.Manifest = manifest;
        result.IsComplete = verification.IsValid;
        int index;
        for (index = 0; index < result.Families.Length; index += 1)
        {
            BulkLocalRefreshFamilyResult family = result.Families[index];
            if (family.IsAvailable)
            {
                result.AvailableFamilyCount += 1;
            }
            else
            {
                result.UnavailableFamilyCount += 1;
                result.IsComplete = false;
            }

            family.IsComplete = family.IsAvailable && family.MetadataPersisted;
            if (!family.IsComplete)
            {
                result.IsComplete = false;
            }
        }

        for (index = 0; index < result.Files.Length; index += 1)
        {
            BulkLocalRefreshFileResult file = result.Files[index];
            if (file.WasProcessed)
            {
                result.ProcessedFileCount += 1;
            }

            if (file.WasSkipped)
            {
                result.SkippedFileCount += 1;
            }

            if (!file.IsComplete)
            {
                result.FailedFileCount += 1;
                result.IsComplete = false;
                MarkFamilyIncomplete(result.Families, file.Family);
            }
        }

        return result;
    }

    internal static void MarkFamilyIncomplete(BulkLocalRefreshFamilyResult[] families, DatasetFamily family)
    {
        int index;
        for (index = 0; index < families.Length; index += 1)
        {
            if (families[index].Family == family)
            {
                families[index].IsComplete = false;
            }
        }
    }
}
