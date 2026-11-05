namespace uspto;

internal class MirrorRepairRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal BulkProduct[] CatalogProducts = new BulkProduct[0];
    internal int MaximumParallelTransfers = 1;
    internal int StaleLockSeconds = 3600;
    internal global::System.DateTimeOffset Now = default(global::System.DateTimeOffset);
    internal int ChecksumBufferSize = 81920;
    internal ExtractionLimits ArchiveLimits = new ExtractionLimits();
    internal RequestContext Context = new RequestContext();
    internal string[] Secrets = new string[0];
}

internal class MirrorRepairFileResult
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorAction InitialAction = MirrorAction.Unknown;
    internal MirrorSynchronizationEntryResult? Synchronization = null;
    internal MirrorStagePipelineResult? Pipeline = null;
    internal bool WasRepaired = false;
    internal bool WasSkipped = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal class MirrorRepairResult
{
    internal MirrorVerificationResult InitialVerification = new MirrorVerificationResult();
    internal MirrorSynchronizationResult Synchronization = new MirrorSynchronizationResult();
    internal MirrorRepairFileResult[] Files = new MirrorRepairFileResult[0];
    internal MirrorVerificationResult FinalVerification = new MirrorVerificationResult();
    internal bool IsComplete = false;
    internal bool ManifestBlocked = false;
    internal int RepairedCount = 0;
    internal int SkippedCount = 0;
    internal int FailedCount = 0;
}

internal class MirrorRepairCoordinator
{
    internal MirrorSynchronizer Synchronizer;
    internal MirrorStagePipeline Pipeline;

    internal MirrorRepairCoordinator(MirrorSynchronizer synchronizer, MirrorStagePipeline pipeline)
    {
        if (synchronizer == null || pipeline == null)
        {
            throw new ValidationException("Mirror repair requires synchronization and stage pipeline operations.", "repair", "REQUIRED", null);
        }

        Synchronizer = synchronizer;
        Pipeline = pipeline;
    }

    internal MirrorRepairResult Repair(MirrorRepairRequest request)
    {
        ValidateRepairRequest(request);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        string mirrorDirectory = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
        DatasetManifest manifest = DatasetManifestStore.Load(mirrorDirectory);
        MirrorVerificationResult initial = MirrorVerifier.Verify(CreateVerificationRequest(request, manifest));
        MirrorRepairFileResult[] fileResults = CreateRepairFileResults(initial);
        if (HasManifestFailure(initial))
        {
            return CreateBlockedRepairResult(initial, fileResults);
        }

        global::System.Collections.Generic.List<MirrorPlanEntry> transferEntries = new global::System.Collections.Generic.List<MirrorPlanEntry>();
        int index;
        for (index = 0; index < initial.Files.Length; index += 1)
        {
            MirrorFileVerificationResult verification = initial.Files[index];
            MirrorRepairFileResult repair = fileResults[index];
            if (verification.IsValid)
            {
                repair.WasSkipped = true;
                continue;
            }

            if (NeedsTransferRepair(verification))
            {
                BulkProductFile? catalogFile = FindCatalogFile(request.CatalogProducts, verification.ProductIdentifier ?? string.Empty, verification.FileName ?? string.Empty);
                if (catalogFile == null)
                {
                    repair.FailureCode = "MIRROR_REPAIR_CATALOG_FILE_MISSING";
                    repair.FailureMessage = "The current catalog does not contain the file required for repair.";
                }
                else
                {
                    MirrorPlanEntry entry = new MirrorPlanEntry();
                    entry.ProductIdentifier = verification.ProductIdentifier;
                    entry.FileName = verification.FileName;
                    entry.Action = verification.Action == MirrorAction.Missing ? MirrorAction.Missing : MirrorAction.Corrupt;
                    entry.CatalogFile = catalogFile;
                    entry.ManifestFile = MirrorSynchronizer.FindOptionalManifestFile(manifest, verification.ProductIdentifier ?? string.Empty, verification.FileName ?? string.Empty);
                    transferEntries.Add(entry);
                }
            }
        }

        MirrorSynchronizationResult synchronization = SynchronizeRepairs(request, manifest, transferEntries);
        ApplySynchronizationResults(fileResults, synchronization);
        for (index = 0; index < fileResults.Length; index += 1)
        {
            request.Context.CancellationToken.ThrowIfCancellationRequested();
            MirrorRepairFileResult repair = fileResults[index];
            MirrorFileVerificationResult verification = initial.Files[index];
            if (repair.FailureCode != null || repair.WasSkipped)
            {
                continue;
            }

            MirrorPipelineStage startStage = ResolveRepairStage(verification, repair.Synchronization);
            if (startStage <= MirrorPipelineStage.Parse)
            {
                InvalidateRepairStages(request, repair.ProductIdentifier ?? string.Empty, repair.FileName ?? string.Empty, startStage);
                MirrorStagePipelineRequest pipelineRequest = CreatePipelineRequest(request, repair.ProductIdentifier ?? string.Empty, repair.FileName ?? string.Empty);
                repair.Pipeline = Pipeline.Run(pipelineRequest);
                if (!repair.Pipeline.IsComplete)
                {
                    MirrorPipelineStageResult? failedStage = FindFailedPipelineStage(repair.Pipeline);
                    repair.FailureCode = failedStage == null ? "MIRROR_REPAIR_PIPELINE_FAILED" : failedStage.FailureCode;
                    repair.FailureMessage = failedStage == null ? "The mirror repair pipeline did not complete." : failedStage.FailureMessage;
                }
            }
        }

        DatasetManifest finalManifest = DatasetManifestStore.Load(mirrorDirectory);
        MirrorVerificationResult finalVerification = MirrorVerifier.Verify(CreateVerificationRequest(request, finalManifest));
        ApplyFinalVerification(fileResults, finalVerification);
        return CreateRepairResult(initial, synchronization, fileResults, finalVerification);
    }

    internal static void ValidateRepairRequest(MirrorRepairRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory) || request.CatalogProducts == null || request.Context == null || request.Secrets == null || request.ArchiveLimits == null)
        {
            throw new ValidationException("Complete mirror repair inputs are required.", "request", "REQUIRED", null);
        }

        if (!BulkDatasetPresetResolver.IsSupportedPresetFamily(request.Family))
        {
            throw new ValidationException("A supported mirror repair family is required.", "Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }

        if (request.MaximumParallelTransfers <= 0 || request.StaleLockSeconds <= 0 || request.ChecksumBufferSize <= 0)
        {
            throw new ValidationException("Mirror repair limits must be positive.", "request", "INVALID_LIMIT", null);
        }
    }

    internal static MirrorVerificationRequest CreateVerificationRequest(MirrorRepairRequest request, DatasetManifest manifest)
    {
        MirrorVerificationRequest result = new MirrorVerificationRequest();
        result.MirrorDirectory = request.MirrorDirectory;
        result.Manifest = manifest;
        result.VerifyChecksums = true;
        result.VerifyArchives = true;
        result.ChecksumBufferSize = request.ChecksumBufferSize;
        result.ArchiveLimits = request.ArchiveLimits;
        result.Context = request.Context;
        return result;
    }

    internal static MirrorRepairFileResult[] CreateRepairFileResults(MirrorVerificationResult verification)
    {
        MirrorRepairFileResult[] results = new MirrorRepairFileResult[verification.Files.Length];
        int index;
        for (index = 0; index < verification.Files.Length; index += 1)
        {
            MirrorFileVerificationResult file = verification.Files[index];
            MirrorRepairFileResult result = new MirrorRepairFileResult();
            result.ProductIdentifier = file.ProductIdentifier;
            result.FileName = file.FileName;
            result.InitialAction = file.Action;
            results[index] = result;
        }

        return results;
    }

    internal static bool HasManifestFailure(MirrorVerificationResult verification)
    {
        int index;
        for (index = 0; index < verification.ManifestChecks.Length; index += 1)
        {
            if (verification.ManifestChecks[index].WasEvaluated && !verification.ManifestChecks[index].IsValid)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool NeedsTransferRepair(MirrorFileVerificationResult verification)
    {
        int index;
        for (index = 0; index < verification.Checks.Length; index += 1)
        {
            MirrorVerificationCheckResult check = verification.Checks[index];
            if (check.WasEvaluated && !check.IsValid)
            {
                if (check.Check == MirrorVerificationCheck.FileExistence || check.Check == MirrorVerificationCheck.FileSize || check.Check == MirrorVerificationCheck.ArchiveReadability)
                {
                    return true;
                }

                if (check.Check == MirrorVerificationCheck.Checksum && global::System.String.Equals(check.Code, "MIRROR_CHECKSUM_MISMATCH", global::System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static BulkProductFile? FindCatalogFile(BulkProduct[] products, string productIdentifier, string fileName)
    {
        int productIndex;
        for (productIndex = 0; productIndex < products.Length; productIndex += 1)
        {
            BulkProduct product = products[productIndex];
            if (product != null && global::System.String.Equals(product.ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && product.Files != null)
            {
                int fileIndex;
                for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
                {
                    BulkProductFile file = product.Files[fileIndex];
                    if (file != null && global::System.String.Equals(file.FileName, fileName, global::System.StringComparison.Ordinal))
                    {
                        return file;
                    }
                }
            }
        }

        return null;
    }

    internal MirrorSynchronizationResult SynchronizeRepairs(MirrorRepairRequest request, DatasetManifest manifest, global::System.Collections.Generic.List<MirrorPlanEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new MirrorSynchronizationResult { Manifest = manifest, LockAcquired = false };
        }

        MirrorSynchronizationRequest synchronizationRequest = new MirrorSynchronizationRequest();
        synchronizationRequest.MirrorDirectory = request.MirrorDirectory;
        synchronizationRequest.Family = request.Family;
        synchronizationRequest.Entries = entries.ToArray();
        synchronizationRequest.Manifest = manifest;
        synchronizationRequest.MaximumParallelTransfers = request.MaximumParallelTransfers;
        synchronizationRequest.StaleLockSeconds = request.StaleLockSeconds;
        synchronizationRequest.Now = request.Now;
        synchronizationRequest.Context = request.Context;
        synchronizationRequest.Secrets = request.Secrets;
        return Synchronizer.Synchronize(synchronizationRequest);
    }

    internal static void ApplySynchronizationResults(MirrorRepairFileResult[] repairs, MirrorSynchronizationResult synchronization)
    {
        int syncIndex;
        for (syncIndex = 0; syncIndex < synchronization.Entries.Length; syncIndex += 1)
        {
            MirrorSynchronizationEntryResult synchronized = synchronization.Entries[syncIndex];
            MirrorRepairFileResult? repair = FindRepairFile(repairs, synchronized.ProductIdentifier ?? string.Empty, synchronized.FileName ?? string.Empty);
            if (repair != null)
            {
                repair.Synchronization = synchronized;
                if (synchronized.Download.State != DownloadState.Completed)
                {
                    repair.FailureCode = synchronized.Download.FailureCode ?? "MIRROR_REPAIR_TRANSFER_FAILED";
                    repair.FailureMessage = synchronized.Download.FailureMessage ?? "The mirror repair transfer did not complete.";
                }
            }
        }
    }

    internal static MirrorRepairFileResult? FindRepairFile(MirrorRepairFileResult[] repairs, string productIdentifier, string fileName)
    {
        int index;
        for (index = 0; index < repairs.Length; index += 1)
        {
            if (global::System.String.Equals(repairs[index].ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && global::System.String.Equals(repairs[index].FileName, fileName, global::System.StringComparison.Ordinal))
            {
                return repairs[index];
            }
        }

        return null;
    }

    internal static MirrorPipelineStage ResolveRepairStage(MirrorFileVerificationResult verification, MirrorSynchronizationEntryResult? synchronization)
    {
        if (synchronization != null && synchronization.Download.State == DownloadState.Completed)
        {
            return MirrorPipelineStage.Checksum;
        }

        int earliest = (int)MirrorPipelineStage.Parse + 1;
        int index;
        for (index = 0; index < verification.Checks.Length; index += 1)
        {
            MirrorVerificationCheckResult check = verification.Checks[index];
            if (check.WasEvaluated && !check.IsValid)
            {
                if (check.Check == MirrorVerificationCheck.Checksum && !global::System.String.Equals(check.Code, "MIRROR_CHECKSUM_MISMATCH", global::System.StringComparison.Ordinal) && earliest > (int)MirrorPipelineStage.Checksum)
                {
                    earliest = (int)MirrorPipelineStage.Checksum;
                }
                else if (check.Check == MirrorVerificationCheck.ExtractedArtifacts && earliest > (int)MirrorPipelineStage.Extraction)
                {
                    earliest = (int)MirrorPipelineStage.Extraction;
                }
                else if (check.Check == MirrorVerificationCheck.ParserState && earliest > (int)MirrorPipelineStage.Parse)
                {
                    earliest = (int)MirrorPipelineStage.Parse;
                }
            }
        }

        return (MirrorPipelineStage)earliest;
    }

    internal static void InvalidateRepairStages(MirrorRepairRequest request, string productIdentifier, string fileName, MirrorPipelineStage startStage)
    {
        global::System.DateTimeOffset now = request.Now == default(global::System.DateTimeOffset) ? global::System.DateTimeOffset.UtcNow : request.Now;
        MirrorLockRequest lockRequest = new MirrorLockRequest();
        lockRequest.MirrorDirectory = request.MirrorDirectory;
        lockRequest.StaleAfterSeconds = request.StaleLockSeconds;
        lockRequest.Now = now;
        lockRequest.CancellationToken = request.Context.CancellationToken;
        MirrorLockHandle mirrorLock = MirrorStateCoordinator.Acquire(lockRequest);
        if (!mirrorLock.IsAcquired)
        {
            throw new MirrorException("The mirror repair could not acquire the stage lock.", request.Family, productIdentifier, fileName, MirrorAction.Corrupt, null);
        }

        using (MirrorSynchronizationLockLease lease = new MirrorSynchronizationLockLease(mirrorLock))
        {
            DatasetManifest manifest = DatasetManifestStore.Load(mirrorLock.MirrorDirectory);
            DatasetManifestFile file = MirrorSynchronizer.FindManifestFile(manifest, productIdentifier, fileName);
            MirrorStagePipeline.EnsurePipelineStages(file);
            int stageValue;
            for (stageValue = (int)startStage; stageValue <= (int)MirrorPipelineStage.Parse; stageValue += 1)
            {
                DatasetManifestStage stage = MirrorStagePipeline.FindPipelineStage(file, (MirrorPipelineStage)stageValue);
                stage.State = MirrorStageState.NotStarted;
                stage.FailureCode = null;
                stage.FailureMessage = null;
                stage.UpdatedAt = default(global::System.DateTimeOffset);
                stage.HasUpdatedAt = false;
            }

            DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
        }
    }

    internal static MirrorStagePipelineRequest CreatePipelineRequest(MirrorRepairRequest request, string productIdentifier, string fileName)
    {
        MirrorStagePipelineRequest result = new MirrorStagePipelineRequest();
        result.MirrorDirectory = request.MirrorDirectory;
        result.Family = request.Family;
        result.ProductIdentifier = productIdentifier;
        result.FileName = fileName;
        result.LastStage = MirrorPipelineStage.Parse;
        result.StaleLockSeconds = request.StaleLockSeconds;
        result.Now = request.Now;
        result.Context = request.Context;
        result.Secrets = request.Secrets;
        return result;
    }

    internal static MirrorPipelineStageResult? FindFailedPipelineStage(MirrorStagePipelineResult pipeline)
    {
        int index;
        for (index = 0; index < pipeline.Stages.Length; index += 1)
        {
            if (pipeline.Stages[index].State == MirrorStageState.Failed)
            {
                return pipeline.Stages[index];
            }
        }

        return null;
    }

    internal static void ApplyFinalVerification(MirrorRepairFileResult[] repairs, MirrorVerificationResult verification)
    {
        int index;
        for (index = 0; index < verification.Files.Length; index += 1)
        {
            MirrorFileVerificationResult file = verification.Files[index];
            MirrorRepairFileResult? repair = FindRepairFile(repairs, file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
            if (repair != null && !repair.WasSkipped)
            {
                if (file.IsValid)
                {
                    repair.WasRepaired = true;
                    repair.FailureCode = null;
                    repair.FailureMessage = null;
                }
                else if (repair.FailureCode == null)
                {
                    repair.FailureCode = "MIRROR_REPAIR_VERIFICATION_FAILED";
                    repair.FailureMessage = "The repaired mirror file did not pass final verification.";
                }
            }
        }
    }

    internal static MirrorRepairResult CreateBlockedRepairResult(MirrorVerificationResult initial, MirrorRepairFileResult[] files)
    {
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            files[index].FailureCode = "MIRROR_MANIFEST_INCONSISTENT";
            files[index].FailureMessage = "Automatic repair is blocked until manifest consistency is restored.";
        }

        MirrorRepairResult result = new MirrorRepairResult();
        result.InitialVerification = initial;
        result.FinalVerification = initial;
        result.Files = files;
        result.ManifestBlocked = true;
        result.FailedCount = files.Length;
        return result;
    }

    internal static MirrorRepairResult CreateRepairResult(MirrorVerificationResult initial, MirrorSynchronizationResult synchronization, MirrorRepairFileResult[] files, MirrorVerificationResult finalVerification)
    {
        MirrorRepairResult result = new MirrorRepairResult();
        result.InitialVerification = initial;
        result.Synchronization = synchronization;
        result.Files = files;
        result.FinalVerification = finalVerification;
        result.IsComplete = finalVerification.IsValid;
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            if (files[index].WasRepaired)
            {
                result.RepairedCount += 1;
            }
            else if (files[index].WasSkipped)
            {
                result.SkippedCount += 1;
            }
            else
            {
                result.FailedCount += 1;
            }
        }

        return result;
    }
}
