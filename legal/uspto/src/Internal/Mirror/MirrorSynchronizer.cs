namespace uspto;

internal interface IMirrorFileTransfer
{
    DownloadResult TransferMirrorFile(DownloadRequest request, DownloadResumeState resumeState);
}

internal class BulkMirrorFileTransfer : IMirrorFileTransfer
{
    internal BulkFileResumeOperation Operation;

    internal BulkMirrorFileTransfer(global::System.Net.Http.HttpClient client, UsptoClientOptions options, DownloadHostPolicy hostPolicy)
    {
        Operation = new BulkFileResumeOperation(client, options, hostPolicy);
    }

    public DownloadResult TransferMirrorFile(DownloadRequest request, DownloadResumeState resumeState)
    {
        return Operation.Resume(request, resumeState);
    }
}

internal class MirrorSynchronizationRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal MirrorPlanEntry[] Entries = new MirrorPlanEntry[0];
    internal DatasetManifest Manifest = new DatasetManifest();
    internal int MaximumParallelTransfers = 1;
    internal int StaleLockSeconds = 3600;
    internal global::System.DateTimeOffset Now = default(global::System.DateTimeOffset);
    internal RequestContext Context = new RequestContext();
    internal string[] Secrets = new string[0];
}

internal class MirrorSynchronizationEntryResult
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorAction Action = MirrorAction.Unknown;
    internal DownloadResult Download = new DownloadResult();
    internal bool WasTransferred = false;
    internal bool WasSkipped = false;
    internal string? SkipReason = null;
}

internal class MirrorSynchronizationResult
{
    internal MirrorSynchronizationEntryResult[] Entries = new MirrorSynchronizationEntryResult[0];
    internal DatasetManifest Manifest = new DatasetManifest();
    internal bool LockAcquired = false;
    internal int TransferredCount = 0;
    internal int SkippedCount = 0;
    internal int FailedCount = 0;
    internal int CancelledCount = 0;
}

internal class MirrorSynchronizationLockLease : global::System.IDisposable
{
    internal MirrorLockHandle Handle;

    internal MirrorSynchronizationLockLease(MirrorLockHandle handle)
    {
        Handle = handle;
    }

    public void Dispose()
    {
        MirrorStateCoordinator.Release(Handle);
    }
}

internal class MirrorSynchronizationSession
{
    internal MirrorSynchronizationRequest Request;
    internal IMirrorFileTransfer Transfer;
    internal MirrorLockHandle Lock;
    internal DatasetManifest Manifest;
    internal MirrorSynchronizationEntryResult[] Results;
    internal object QueueSync = new object();
    internal object ManifestSync = new object();
    internal int NextIndex = 0;

    internal MirrorSynchronizationSession(MirrorSynchronizationRequest request, IMirrorFileTransfer transfer, MirrorLockHandle mirrorLock, DatasetManifest manifest)
    {
        Request = request;
        Transfer = transfer;
        Lock = mirrorLock;
        Manifest = manifest;
        Results = new MirrorSynchronizationEntryResult[request.Entries.Length];
    }

    internal void RunWorker()
    {
        while (true)
        {
            int index = TakeNextIndex();
            if (index < 0)
            {
                return;
            }

            MirrorPlanEntry entry = Request.Entries[index];
            if (!MirrorSynchronizer.IsTransferAction(entry.Action))
            {
                Results[index] = MirrorSynchronizer.CreateSkippedEntry(entry, "ACTION_NOT_TRANSFERABLE");
            }
            else
            {
                Results[index] = SynchronizeEntry(entry);
            }
        }
    }

    internal int TakeNextIndex()
    {
        lock (QueueSync)
        {
            if (NextIndex >= Request.Entries.Length)
            {
                return -1;
            }

            int result = NextIndex;
            NextIndex += 1;
            return result;
        }
    }

    internal MirrorSynchronizationEntryResult SynchronizeEntry(MirrorPlanEntry entry)
    {
        DatasetManifestFile manifestFile;
        lock (ManifestSync)
        {
            manifestFile = MirrorSynchronizer.FindManifestFile(Manifest, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            MirrorSynchronizer.SetDownloadStage(manifestFile, MirrorStageState.InProgress, null, null);
            Manifest = DatasetManifestStore.Save(Lock.MirrorDirectory, Manifest, Request.Secrets);
        }

        string localPath = MirrorSynchronizer.ResolveManifestPath(Lock.MirrorDirectory, manifestFile.LocalPath);
        BulkProductFile catalogFile = entry.CatalogFile ?? new BulkProductFile();
        if (entry.Action != MirrorAction.Corrupt && MirrorSynchronizer.IsAlreadySynchronized(localPath, catalogFile, Request.Context))
        {
            MirrorSynchronizationEntryResult skipped = MirrorSynchronizer.CreateCompletedSkip(entry, localPath);
            lock (ManifestSync)
            {
                manifestFile = MirrorSynchronizer.FindManifestFile(Manifest, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
                MirrorSynchronizer.ApplyCompletedDownload(manifestFile, catalogFile, skipped.Download, localPath);
                Manifest = DatasetManifestStore.Save(Lock.MirrorDirectory, Manifest, Request.Secrets);
            }

            return skipped;
        }

        MirrorSynchronizer.PreserveSupersededArtifact(Lock.MirrorDirectory, manifestFile, localPath);
        string partialPath = MirrorSynchronizer.ResolveManifestPath(Lock.MirrorDirectory, manifestFile.PartialPath);
        DownloadRequest download = MirrorSynchronizer.CreateDownloadRequest(catalogFile, localPath, Request.Context);
        DownloadResumeState resumeState = MirrorSynchronizer.CreateResumeState(catalogFile, manifestFile, partialPath);
        DownloadResult transferred = Transfer.TransferMirrorFile(download, resumeState);
        MirrorSynchronizationEntryResult result = new MirrorSynchronizationEntryResult();
        result.ProductIdentifier = entry.ProductIdentifier;
        result.FileName = entry.FileName;
        result.Action = entry.Action;
        result.Download = transferred;
        result.WasTransferred = true;
        lock (ManifestSync)
        {
            manifestFile = MirrorSynchronizer.FindManifestFile(Manifest, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            if (transferred.State == DownloadState.Completed)
            {
                MirrorSynchronizer.ApplyCompletedDownload(manifestFile, catalogFile, transferred, localPath);
            }
            else
            {
                MirrorSynchronizer.ApplyFailedDownload(manifestFile, transferred);
            }

            Manifest = DatasetManifestStore.Save(Lock.MirrorDirectory, Manifest, Request.Secrets);
        }

        return result;
    }

    internal void CompleteUnfinishedEntries()
    {
        int index;
        for (index = 0; index < Results.Length; index += 1)
        {
            if (Results[index] == null)
            {
                MirrorPlanEntry entry = Request.Entries[index];
                MirrorSynchronizationEntryResult failure = MirrorSynchronizer.CreateFaultedEntry(entry, Request.Context.CancellationToken.IsCancellationRequested);
                Results[index] = failure;
                if (MirrorSynchronizer.IsTransferAction(entry.Action))
                {
                    lock (ManifestSync)
                    {
                        DatasetManifestFile file = MirrorSynchronizer.FindManifestFile(Manifest, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
                        MirrorSynchronizer.ApplyFailedDownload(file, failure.Download);
                        Manifest = DatasetManifestStore.Save(Lock.MirrorDirectory, Manifest, Request.Secrets);
                    }
                }
            }
        }
    }
}

internal class MirrorSynchronizer
{
    internal IMirrorFileTransfer Transfer;

    internal MirrorSynchronizer(IMirrorFileTransfer transfer)
    {
        if (transfer == null)
        {
            throw new ValidationException("A mirror file transfer is required.", "transfer", "REQUIRED", null);
        }

        Transfer = transfer;
    }

    internal MirrorSynchronizationResult Synchronize(MirrorSynchronizationRequest request)
    {
        ValidateSynchronizationRequest(request);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        global::System.DateTimeOffset now = request.Now == default(global::System.DateTimeOffset) ? global::System.DateTimeOffset.UtcNow : request.Now;
        MirrorLockRequest lockRequest = new MirrorLockRequest();
        lockRequest.MirrorDirectory = request.MirrorDirectory;
        lockRequest.StaleAfterSeconds = request.StaleLockSeconds;
        lockRequest.Now = now;
        lockRequest.CancellationToken = request.Context.CancellationToken;
        MirrorLockHandle mirrorLock = MirrorStateCoordinator.Acquire(lockRequest);
        if (!mirrorLock.IsAcquired)
        {
            return CreateLockBusyResult(request);
        }

        using (MirrorSynchronizationLockLease lease = new MirrorSynchronizationLockLease(mirrorLock))
        {
            DatasetManifest manifest = LoadOrCreateManifest(mirrorLock.MirrorDirectory, request.Manifest, request.Secrets);
            PrepareManifest(mirrorLock.MirrorDirectory, manifest, request);
            manifest = DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
            MirrorSynchronizationSession session = new MirrorSynchronizationSession(request, Transfer, mirrorLock, manifest);
            int workerCount = request.MaximumParallelTransfers;
            if (workerCount > request.Entries.Length)
            {
                workerCount = request.Entries.Length;
            }

            global::System.Threading.Tasks.Task[] workers = new global::System.Threading.Tasks.Task[workerCount];
            int index;
            for (index = 0; index < workerCount; index += 1)
            {
                workers[index] = global::System.Threading.Tasks.Task.Run(() => session.RunWorker());
            }

            if (workers.Length != 0)
            {
                global::System.Threading.Tasks.Task allWorkers = global::System.Threading.Tasks.Task.WhenAll(workers);
                global::System.IAsyncResult asynchronousResult = allWorkers;
                using (global::System.Threading.WaitHandle completion = asynchronousResult.AsyncWaitHandle)
                {
                    completion.WaitOne();
                }
            }

            session.CompleteUnfinishedEntries();
            return CreateSynchronizationResult(session.Results, session.Manifest, true);
        }
    }

    internal static void ValidateSynchronizationRequest(MirrorSynchronizationRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory))
        {
            throw new ValidationException("A mirror synchronization directory is required.", "MirrorDirectory", "REQUIRED", null);
        }

        if (!BulkDatasetPresetResolver.IsSupportedPresetFamily(request.Family))
        {
            throw new ValidationException("A supported mirror dataset family is required.", "Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }

        if (request.Entries == null || request.Manifest == null || request.Context == null || request.Secrets == null)
        {
            throw new ValidationException("Mirror synchronization inputs are required.", "request", "REQUIRED", null);
        }

        if (request.MaximumParallelTransfers <= 0 || request.StaleLockSeconds <= 0)
        {
            throw new ValidationException("Mirror synchronization limits must be positive.", "MaximumParallelTransfers", "INVALID_LIMIT", null);
        }

        DatasetManifestStore.Validate(request.Manifest);
        global::System.Collections.Generic.Dictionary<string, bool> keys = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < request.Entries.Length; index += 1)
        {
            MirrorPlanEntry entry = request.Entries[index];
            if (entry == null || global::System.String.IsNullOrWhiteSpace(entry.ProductIdentifier) || global::System.String.IsNullOrWhiteSpace(entry.FileName))
            {
                throw new ValidationException("A mirror synchronization entry is invalid.", "Entries", "MIRROR_ENTRY_INVALID", null);
            }

            string key = MirrorPlanner.CreateKey(entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
            if (keys.ContainsKey(key))
            {
                throw new ValidationException("Mirror synchronization entries cannot be repeated.", "Entries", "MIRROR_ENTRY_DUPLICATE", null);
            }

            keys[key] = true;
            if (IsTransferAction(entry.Action) && (entry.CatalogFile == null || entry.CatalogFile.DownloadUri == null && entry.CatalogFile.CatalogUri == null))
            {
                throw new ValidationException("A transferable mirror entry requires a source URI.", "Entries", "MIRROR_SOURCE_REQUIRED", null);
            }
        }
    }

    internal static bool IsTransferAction(MirrorAction action)
    {
        return action == MirrorAction.New || action == MirrorAction.Changed || action == MirrorAction.Missing || action == MirrorAction.Corrupt || action == MirrorAction.Resumable;
    }

    internal static DatasetManifest LoadOrCreateManifest(string mirrorDirectory, DatasetManifest supplied, string[] secrets)
    {
        string path = DatasetManifestStore.GetPath(mirrorDirectory);
        if (global::System.IO.File.Exists(path))
        {
            return DatasetManifestStore.Load(mirrorDirectory);
        }

        return DatasetManifestStore.Save(mirrorDirectory, supplied, secrets);
    }

    internal static void PrepareManifest(string mirrorDirectory, DatasetManifest manifest, MirrorSynchronizationRequest request)
    {
        int index;
        for (index = 0; index < request.Entries.Length; index += 1)
        {
            MirrorPlanEntry entry = request.Entries[index];
            if (IsTransferAction(entry.Action))
            {
                DatasetManifestFile? file = FindOptionalManifestFile(manifest, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
                if (file == null)
                {
                    file = AddManifestFile(manifest, request.Family, entry);
                }

                EnsureManifestPaths(file, entry.ProductIdentifier ?? string.Empty, entry.FileName ?? string.Empty);
                EnsureDownloadStage(file);
                string localPath = ResolveManifestPath(mirrorDirectory, file.LocalPath);
                string partialPath = ResolveManifestPath(mirrorDirectory, file.PartialPath);
                global::System.IO.Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(localPath) ?? string.Empty);
                global::System.IO.Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(partialPath) ?? string.Empty);
            }
        }
    }

    internal static DatasetManifestFile AddManifestFile(DatasetManifest manifest, DatasetFamily family, MirrorPlanEntry entry)
    {
        DatasetManifestProduct? product = FindOptionalManifestProduct(manifest, entry.ProductIdentifier ?? string.Empty, family);
        if (product == null)
        {
            product = new DatasetManifestProduct();
            product.ProductIdentifier = entry.ProductIdentifier;
            product.Family = family;
            product.Provenance = new PatentDataProvenance();
            product.Provenance.Source = DataProvenance.LocalMirror;
            product.Provenance.IsAuthoritative = false;
            DatasetManifestProduct[] products = new DatasetManifestProduct[manifest.Products.Length + 1];
            global::System.Array.Copy(manifest.Products, products, manifest.Products.Length);
            products[products.Length - 1] = product;
            manifest.Products = products;
        }

        DatasetManifestFile file = new DatasetManifestFile();
        file.ProductIdentifier = entry.ProductIdentifier;
        file.FileName = entry.FileName;
        file.Provenance.Source = DataProvenance.LocalMirror;
        file.Provenance.IsAuthoritative = false;
        DatasetManifestFile[] files = new DatasetManifestFile[product.Files.Length + 1];
        global::System.Array.Copy(product.Files, files, product.Files.Length);
        files[files.Length - 1] = file;
        product.Files = files;
        return file;
    }

    internal static DatasetManifestProduct? FindOptionalManifestProduct(DatasetManifest manifest, string productIdentifier, DatasetFamily family)
    {
        int index;
        for (index = 0; index < manifest.Products.Length; index += 1)
        {
            DatasetManifestProduct product = manifest.Products[index];
            if (product.Family == family && global::System.String.Equals(product.ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal))
            {
                return product;
            }
        }

        return null;
    }

    internal static DatasetManifestFile? FindOptionalManifestFile(DatasetManifest manifest, string productIdentifier, string fileName)
    {
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                DatasetManifestFile file = product.Files[fileIndex];
                if (global::System.String.Equals(file.ProductIdentifier, productIdentifier, global::System.StringComparison.Ordinal) && global::System.String.Equals(file.FileName, fileName, global::System.StringComparison.Ordinal))
                {
                    return file;
                }
            }
        }

        return null;
    }

    internal static DatasetManifestFile FindManifestFile(DatasetManifest manifest, string productIdentifier, string fileName)
    {
        DatasetManifestFile? result = FindOptionalManifestFile(manifest, productIdentifier, fileName);
        if (result == null)
        {
            throw new ValidationException("The synchronized manifest file was not found.", "Manifest", "MANIFEST_FILE_NOT_FOUND", null);
        }

        return result;
    }

    internal static void EnsureManifestPaths(DatasetManifestFile file, string productIdentifier, string fileName)
    {
        if (global::System.String.IsNullOrWhiteSpace(file.LocalPath))
        {
            string productSegment = CreateSafeProductSegment(productIdentifier);
            string safeFileName = BulkFileNameSelector.NormalizeBulkFileName(fileName, "FileName");
            file.LocalPath = global::System.IO.Path.Combine("files", productSegment, safeFileName);
        }

        if (global::System.String.IsNullOrWhiteSpace(file.PartialPath))
        {
            file.PartialPath = (file.LocalPath ?? string.Empty) + ".partial";
        }

        DatasetManifestStore.ValidatePath(file.LocalPath, "LocalPath");
        DatasetManifestStore.ValidatePath(file.PartialPath, "PartialPath");
    }

    internal static string CreateSafeProductSegment(string productIdentifier)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < productIdentifier.Length; index += 1)
        {
            char character = productIdentifier[index];
            if (global::System.Char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('_');
            }
        }

        string result = builder.ToString().Trim('.');
        if (result.Length == 0)
        {
            throw new ValidationException("The mirror product identifier has no safe path characters.", "ProductIdentifier", "MIRROR_PRODUCT_PATH_INVALID", null);
        }

        return result;
    }

    internal static string ResolveManifestPath(string root, string? relativePath)
    {
        string? result = MirrorPlanner.ResolveRelativePath(global::System.IO.Path.GetFullPath(root), relativePath);
        if (result == null)
        {
            throw new ValidationException("A mirror artifact path is required.", "Manifest", "MANIFEST_PATH_INVALID", null);
        }

        return result;
    }

    internal static void EnsureDownloadStage(DatasetManifestFile file)
    {
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (global::System.String.Equals(file.Stages[index].Name, "download", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        DatasetManifestStage[] stages = new DatasetManifestStage[file.Stages.Length + 1];
        global::System.Array.Copy(file.Stages, stages, file.Stages.Length);
        stages[stages.Length - 1] = new DatasetManifestStage { Name = "download" };
        file.Stages = stages;
    }

    internal static DatasetManifestStage GetDownloadStage(DatasetManifestFile file)
    {
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (global::System.String.Equals(file.Stages[index].Name, "download", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return file.Stages[index];
            }
        }

        throw new ValidationException("The manifest download stage was not found.", "Manifest", "MANIFEST_STAGE_NOT_FOUND", null);
    }

    internal static void SetDownloadStage(DatasetManifestFile file, MirrorStageState state, string? failureCode, string? failureMessage)
    {
        DatasetManifestStage stage = GetDownloadStage(file);
        if (!MirrorStateCoordinator.CanTransition(stage.State, state))
        {
            throw new ValidationException("The manifest download stage transition is invalid.", "State", "MANIFEST_STAGE_TRANSITION_INVALID", null);
        }

        stage.State = state;
        stage.UpdatedAt = global::System.DateTimeOffset.UtcNow;
        stage.HasUpdatedAt = true;
        stage.FailureCode = state == MirrorStageState.Failed ? failureCode : null;
        stage.FailureMessage = state == MirrorStageState.Failed ? failureMessage : null;
    }

    internal static bool IsAlreadySynchronized(string localPath, BulkProductFile catalogFile, RequestContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (!global::System.IO.File.Exists(localPath))
        {
            return false;
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(localPath);
        if (catalogFile.HasSize && information.Length != catalogFile.Size)
        {
            return false;
        }

        if (catalogFile.Checksums.Length != 0)
        {
            ChecksumResult[] results = BulkChecksumVerifier.VerifyFile(localPath, catalogFile.Checksums, 81920, context.CancellationToken);
            return !BulkChecksumVerifier.HasMismatch(results);
        }

        return true;
    }

    internal static void PreserveSupersededArtifact(string mirrorDirectory, DatasetManifestFile file, string localPath)
    {
        if (!global::System.IO.File.Exists(localPath))
        {
            return;
        }

        string productSegment = CreateSafeProductSegment(file.ProductIdentifier ?? string.Empty);
        string fileName = BulkFileNameSelector.NormalizeBulkFileName(file.FileName ?? string.Empty, "FileName");
        string version = file.Digest ?? string.Empty;
        if (global::System.String.IsNullOrWhiteSpace(version))
        {
            version = file.HasLastModified ? file.LastModified.UtcDateTime.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture) : file.Size.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        if (version.Length > 16)
        {
            version = version.Substring(0, 16);
        }

        string directory = global::System.IO.Path.Combine(mirrorDirectory, ".superseded", productSegment);
        global::System.IO.Directory.CreateDirectory(directory);
        string backup = global::System.IO.Path.Combine(directory, fileName + "." + version);
        if (!global::System.IO.File.Exists(backup))
        {
            global::System.IO.File.Copy(localPath, backup, false);
        }
    }

    internal static DownloadRequest CreateDownloadRequest(BulkProductFile catalogFile, string localPath, RequestContext context)
    {
        DownloadRequest result = new DownloadRequest();
        result.CatalogFileName = catalogFile.FileName;
        result.SourceUri = catalogFile.DownloadUri ?? catalogFile.CatalogUri;
        result.DestinationPath = localPath;
        result.OverwriteExisting = true;
        result.ResumePartial = true;
        result.ExpectedSize = catalogFile.Size;
        result.HasExpectedSize = catalogFile.HasSize;
        result.ExpectedChecksums = catalogFile.Checksums;
        result.Context = context;
        return result;
    }

    internal static DownloadResumeState CreateResumeState(BulkProductFile catalogFile, DatasetManifestFile manifestFile, string partialPath)
    {
        if (!global::System.IO.File.Exists(partialPath))
        {
            using (global::System.IO.FileStream output = new global::System.IO.FileStream(partialPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None))
            {
                output.Flush(true);
            }
        }

        global::System.Uri sourceUri = catalogFile.DownloadUri ?? catalogFile.CatalogUri ?? new global::System.Uri("https://invalid.example/");
        DownloadResumeState? persisted = PartialDownloadMetadataStore.Load(partialPath);
        if (persisted != null && BulkFileResumeOperation.IsSameResumeSource(sourceUri, persisted.SourceUri ?? new global::System.Uri("https://invalid.example/")))
        {
            return persisted;
        }

        DownloadResumeState result = new DownloadResumeState();
        result.SourceUri = sourceUri;
        result.PartialPath = partialPath;
        result.CompletedBytes = new global::System.IO.FileInfo(partialPath).Length;
        result.ExpectedTotalBytes = catalogFile.HasSize ? catalogFile.Size : 0L;
        result.HasExpectedTotalBytes = catalogFile.HasSize;
        result.ETag = manifestFile.ETag;
        result.LastModified = manifestFile.LastModified;
        result.HasLastModified = manifestFile.HasLastModified;
        return result;
    }

    internal static void ApplyCompletedDownload(DatasetManifestFile file, BulkProductFile catalogFile, DownloadResult download, string localPath)
    {
        file.CatalogUri = catalogFile.CatalogUri;
        file.DownloadUri = catalogFile.DownloadUri;
        file.Size = new global::System.IO.FileInfo(localPath).Length;
        file.HasSize = true;
        if (download.Content.ETag != null)
        {
            file.ETag = download.Content.ETag;
        }

        if (download.Content.HasLastModified)
        {
            file.LastModified = download.Content.LastModified;
            file.HasLastModified = true;
        }
        else if (catalogFile.HasLastModified)
        {
            file.LastModified = catalogFile.LastModified;
            file.HasLastModified = true;
        }

        ApplyCompletedDigest(file, download.Checksums);
        SetDownloadStage(file, MirrorStageState.Complete, null, null);
    }

    internal static void ApplyCompletedDigest(DatasetManifestFile file, ChecksumResult[] checksums)
    {
        int index;
        for (index = 0; index < checksums.Length; index += 1)
        {
            ChecksumResult checksum = checksums[index];
            if (checksum.IsComplete && checksum.ActualValue != null && (file.Digest == null || checksum.Expectation.Algorithm == ChecksumAlgorithm.Sha256))
            {
                file.DigestAlgorithm = checksum.Expectation.Algorithm;
                file.Digest = checksum.ActualValue;
                file.DigestVerified = true;
            }
        }
    }

    internal static void ApplyFailedDownload(DatasetManifestFile file, DownloadResult download)
    {
        if (download.HasResumeState)
        {
            file.ETag = download.ResumeState.ETag;
            file.LastModified = download.ResumeState.LastModified;
            file.HasLastModified = download.ResumeState.HasLastModified;
        }

        string code = global::System.String.IsNullOrWhiteSpace(download.FailureCode) ? "MIRROR_TRANSFER_FAILED" : download.FailureCode ?? "MIRROR_TRANSFER_FAILED";
        string message = global::System.String.IsNullOrWhiteSpace(download.FailureMessage) ? "The mirror transfer did not complete." : download.FailureMessage ?? "The mirror transfer did not complete.";
        DatasetManifestStage stage = GetDownloadStage(file);
        if (stage.State != MirrorStageState.InProgress)
        {
            SetDownloadStage(file, MirrorStageState.InProgress, null, null);
        }

        SetDownloadStage(file, MirrorStageState.Failed, code, message);
    }

    internal static MirrorSynchronizationEntryResult CreateSkippedEntry(MirrorPlanEntry entry, string reason)
    {
        MirrorSynchronizationEntryResult result = new MirrorSynchronizationEntryResult();
        result.ProductIdentifier = entry.ProductIdentifier;
        result.FileName = entry.FileName;
        result.Action = entry.Action;
        result.WasSkipped = true;
        result.SkipReason = reason;
        return result;
    }

    internal static MirrorSynchronizationEntryResult CreateCompletedSkip(MirrorPlanEntry entry, string localPath)
    {
        MirrorSynchronizationEntryResult result = CreateSkippedEntry(entry, "ALREADY_SYNCHRONIZED");
        result.Download.State = DownloadState.Completed;
        result.Download.FinalPath = localPath;
        result.Download.Progress.State = DownloadState.Completed;
        result.Download.Progress.BytesTransferred = new global::System.IO.FileInfo(localPath).Length;
        return result;
    }

    internal static MirrorSynchronizationEntryResult CreateFaultedEntry(MirrorPlanEntry entry, bool cancelled)
    {
        MirrorSynchronizationEntryResult result = new MirrorSynchronizationEntryResult();
        result.ProductIdentifier = entry.ProductIdentifier;
        result.FileName = entry.FileName;
        result.Action = entry.Action;
        result.WasTransferred = IsTransferAction(entry.Action);
        result.Download.State = cancelled ? DownloadState.Cancelled : DownloadState.Failed;
        result.Download.FailureCode = cancelled ? "CANCELLED" : "MIRROR_TRANSFER_EXCEPTION";
        result.Download.FailureMessage = cancelled ? "The mirror synchronization was cancelled." : "The mirror transfer ended with an exception.";
        return result;
    }

    internal static MirrorSynchronizationResult CreateLockBusyResult(MirrorSynchronizationRequest request)
    {
        MirrorSynchronizationEntryResult[] entries = new MirrorSynchronizationEntryResult[request.Entries.Length];
        int index;
        for (index = 0; index < entries.Length; index += 1)
        {
            entries[index] = CreateSkippedEntry(request.Entries[index], "MIRROR_LOCK_BUSY");
        }

        return CreateSynchronizationResult(entries, request.Manifest, false);
    }

    internal static MirrorSynchronizationResult CreateSynchronizationResult(MirrorSynchronizationEntryResult[] entries, DatasetManifest manifest, bool lockAcquired)
    {
        MirrorSynchronizationResult result = new MirrorSynchronizationResult();
        result.Entries = entries;
        result.Manifest = manifest;
        result.LockAcquired = lockAcquired;
        int index;
        for (index = 0; index < entries.Length; index += 1)
        {
            MirrorSynchronizationEntryResult entry = entries[index];
            if (entry.WasSkipped)
            {
                result.SkippedCount += 1;
            }

            if (entry.WasTransferred && entry.Download.State == DownloadState.Completed)
            {
                result.TransferredCount += 1;
            }
            else if (entry.Download.State == DownloadState.Failed)
            {
                result.FailedCount += 1;
            }
            else if (entry.Download.State == DownloadState.Cancelled)
            {
                result.CancelledCount += 1;
            }
        }

        return result;
    }
}
