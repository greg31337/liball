namespace uspto;

internal class ConfiguredMirrorPipelineStageExecutor : IMirrorPipelineStageExecutor
{
    internal ConfiguredClientComponents Components;

    internal ConfiguredMirrorPipelineStageExecutor(ConfiguredClientComponents components)
    {
        Components = components;
    }

    public MirrorPipelineStageExecutionResult ExecuteMirrorPipelineStage(MirrorPipelineStageRequest request)
    {
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        if (request.Stage == MirrorPipelineStage.Download)
        {
            return ExecuteExistingDownloadStage(request);
        }

        if (request.Stage == MirrorPipelineStage.Checksum)
        {
            return ExecuteChecksumStage(request);
        }

        if (request.Stage == MirrorPipelineStage.Extraction)
        {
            return ExecuteExtractionStage(request);
        }

        return new MirrorPipelineStageExecutionResult { IsComplete = true };
    }

    internal static MirrorPipelineStageExecutionResult ExecuteExistingDownloadStage(MirrorPipelineStageRequest request)
    {
        MirrorPipelineStageExecutionResult result = new MirrorPipelineStageExecutionResult();
        if (!global::System.IO.File.Exists(request.LocalPath))
        {
            result.FailureCode = "MIRROR_FILE_MISSING";
            result.FailureMessage = "The mirror download stage has no local artifact.";
            return result;
        }

        result.IsComplete = true;
        result.ActualSize = new global::System.IO.FileInfo(request.LocalPath).Length;
        result.HasActualSize = true;
        return result;
    }

    internal static MirrorPipelineStageExecutionResult ExecuteChecksumStage(MirrorPipelineStageRequest request)
    {
        MirrorPipelineStageExecutionResult result = new MirrorPipelineStageExecutionResult();
        if (!global::System.IO.File.Exists(request.LocalPath))
        {
            result.FailureCode = "MIRROR_FILE_MISSING";
            result.FailureMessage = "The checksum stage has no local artifact.";
            return result;
        }

        ChecksumExpectation expectation = new ChecksumExpectation();
        expectation.Algorithm = ChecksumAlgorithm.Sha256;
        ChecksumResult[] checksums = BulkChecksumVerifier.VerifyFile(request.LocalPath, new ChecksumExpectation[] { expectation }, 81920, request.Context.CancellationToken);
        result.IsComplete = checksums.Length == 1 && checksums[0].IsComplete;
        if (result.IsComplete)
        {
            result.DigestAlgorithm = ChecksumAlgorithm.Sha256;
            result.Digest = checksums[0].ActualValue;
            result.HasDigest = true;
        }
        else
        {
            result.FailureCode = "CHECKSUM_FAILED";
            result.FailureMessage = "The checksum stage did not read the complete artifact.";
        }

        return result;
    }

    internal MirrorPipelineStageExecutionResult ExecuteExtractionStage(MirrorPipelineStageRequest request)
    {
        MirrorPipelineStageExecutionResult result = new MirrorPipelineStageExecutionResult();
        CompressionDetectionResult detection = ArchiveFormatDetector.Detect(request.LocalPath);
        if (detection.Format == CompressionFormat.None)
        {
            result.IsComplete = true;
            result.ExtractedPaths = new string[0];
            result.HasExtractedPaths = true;
            return result;
        }

        if (!detection.IsSupported || detection.Format == CompressionFormat.Gzip)
        {
            result.FailureCode = "UNSUPPORTED_ARCHIVE_FORMAT";
            result.FailureMessage = "The mirror archive format cannot be extracted.";
            return result;
        }

        string product = MirrorSynchronizer.CreateSafeProductSegment(request.ManifestFile.ProductIdentifier ?? string.Empty);
        string file = BulkFileNameSelector.NormalizeBulkFileName(request.ManifestFile.FileName ?? string.Empty, "FileName");
        string relativeDestination = global::System.IO.Path.Combine("extracted", product, file);
        ExtractionOptions options = new ExtractionOptions();
        options.ArchivePath = request.LocalPath;
        options.DestinationDirectory = global::System.IO.Path.Combine(request.MirrorDirectory, relativeDestination);
        options.Limits = Components.Options.Extraction;
        options.Context = request.Context;
        ExtractionResult extraction;
        if (detection.Format == CompressionFormat.Zip)
        {
            extraction = ZipExtractionOperation.Extract(options);
        }
        else
        {
            extraction = TarExtractionOperation.Extract(options);
        }

        result.IsComplete = extraction.IsComplete;
        result.IsCancelled = extraction.IsCancelled;
        result.FailureCode = extraction.FailureCode;
        result.FailureMessage = extraction.FailureMessage;
        if (extraction.IsComplete)
        {
            global::System.Collections.Generic.List<string> paths = new global::System.Collections.Generic.List<string>();
            int index;
            for (index = 0; index < extraction.Entries.Length; index += 1)
            {
                ExtractionEntryResult entry = extraction.Entries[index];
                if (entry.IsExtracted && entry.DestinationPath != null)
                {
                    paths.Add(global::System.IO.Path.GetRelativePath(request.MirrorDirectory, entry.DestinationPath));
                }
            }

            result.ExtractedPaths = paths.ToArray();
            result.HasExtractedPaths = true;
        }

        return result;
    }
}

internal class ConfiguredDatasetMirror : IDatasetMirror
{
    internal ConfiguredClientComponents Components;
    internal BulkCatalogSearchOperation CatalogSearch;
    internal BulkDatasetPresetOperation Presets;
    internal MirrorSynchronizer Synchronizer;
    internal MirrorStagePipeline Pipeline;

    internal ConfiguredDatasetMirror(ConfiguredClientComponents components)
    {
        Components = components;
        CatalogSearch = new BulkCatalogSearchOperation(components.Client, components.Options);
        Presets = new BulkDatasetPresetOperation(CatalogSearch);
        BulkMirrorFileTransfer transfer = new BulkMirrorFileTransfer(components.Client, components.Options, components.DownloadHostPolicy);
        transfer.Operation.RateLimiter = components.RateLimiter;
        transfer.Operation.RetryEngine = components.RetryEngine;
        Synchronizer = new MirrorSynchronizer(transfer);
        Pipeline = new MirrorStagePipeline(new ConfiguredMirrorPipelineStageExecutor(components));
    }

    public MirrorAction[] PlanDatasetMirror(MirrorPolicy policy, RequestContext context)
    {
        ValidateMirrorPolicy(policy, context);
        BulkDatasetPreset[] presets = Presets.GetPresets(policy.DatasetFamilies, context);
        DatasetManifest manifest = LoadPlanningManifest(policy.Directory ?? string.Empty);
        global::System.Collections.Generic.List<MirrorAction> actions = new global::System.Collections.Generic.List<MirrorAction>();
        int index;
        for (index = 0; index < presets.Length; index += 1)
        {
            MirrorPlanRequest request = new MirrorPlanRequest();
            request.MirrorDirectory = policy.Directory;
            request.CatalogProducts = presets[index].Products;
            request.Manifest = BulkLocalRefreshCoordinator.CreateFamilyManifest(manifest, presets[index].Family);
            request.VerifyDigests = policy.VerifyChecksums;
            request.CancellationToken = context.CancellationToken;
            MirrorPlanResult plan = MirrorPlanner.Plan(request);
            int entryIndex;
            for (entryIndex = 0; entryIndex < plan.Entries.Length; entryIndex += 1)
            {
                actions.Add(plan.Entries[entryIndex].Action);
            }
        }

        return actions.ToArray();
    }

    public DownloadResult[] SynchronizeDatasetMirror(MirrorPolicy policy, RequestContext context)
    {
        ValidateMirrorPolicy(policy, context);
        BulkLocalRefreshRequest request = CreateRefreshRequest(policy, context, MirrorPipelineStage.Download);
        BulkLocalRefreshResult refresh = new BulkLocalRefreshCoordinator(Presets, Synchronizer, Pipeline).Refresh(request);
        return CollectSynchronizationDownloads(refresh.Files);
    }

    public MirrorWorkflowResult RefreshDatasetMirror(MirrorPolicy policy, RequestContext context)
    {
        ValidateMirrorPolicy(policy, context);
        BulkLocalRefreshRequest request = CreateRefreshRequest(policy, context, MirrorPipelineStage.Parse);
        BulkLocalRefreshResult refresh = new BulkLocalRefreshCoordinator(Presets, Synchronizer, Pipeline).Refresh(request);
        return ConvertRefreshResult(refresh, context);
    }

    public DatasetManifest GetDatasetManifest(string mirrorDirectory, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A mirror manifest context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        return DatasetManifestStore.Load(mirrorDirectory);
    }

    public DownloadResult[] VerifyDatasetMirror(MirrorPolicy policy, RequestContext context)
    {
        ValidateMirrorPolicy(policy, context);
        DatasetManifest manifest = DatasetManifestStore.Load(policy.Directory ?? string.Empty);
        MirrorVerificationRequest request = new MirrorVerificationRequest();
        request.MirrorDirectory = policy.Directory;
        request.Manifest = BulkLocalRefreshCoordinator.CreateSelectedManifest(manifest, policy.DatasetFamilies);
        request.VerifyChecksums = policy.VerifyChecksums;
        request.VerifyArchives = true;
        request.ArchiveLimits = Components.Options.Extraction;
        request.Context = context;
        return ConvertVerificationResults(MirrorVerifier.Verify(request));
    }

    public DownloadResult[] RepairDatasetMirror(MirrorPolicy policy, RequestContext context)
    {
        ValidateMirrorPolicy(policy, context);
        BulkDatasetPreset[] presets = Presets.GetPresets(policy.DatasetFamilies, context);
        global::System.Collections.Generic.List<DownloadResult> downloads = new global::System.Collections.Generic.List<DownloadResult>();
        int index;
        for (index = 0; index < presets.Length; index += 1)
        {
            MirrorRepairRequest request = new MirrorRepairRequest();
            request.MirrorDirectory = policy.Directory;
            request.Family = presets[index].Family;
            request.CatalogProducts = presets[index].Products;
            request.MaximumParallelTransfers = policy.MaximumParallelTransfers;
            request.ArchiveLimits = Components.Options.Extraction;
            request.Context = context;
            MirrorRepairResult repair = new MirrorRepairCoordinator(Synchronizer, Pipeline).Repair(request);
            int fileIndex;
            for (fileIndex = 0; fileIndex < repair.Files.Length; fileIndex += 1)
            {
                MirrorSynchronizationEntryResult? synchronization = repair.Files[fileIndex].Synchronization;
                if (synchronization != null)
                {
                    downloads.Add(synchronization.Download);
                }
            }
        }

        return downloads.ToArray();
    }

    internal static void ValidateMirrorPolicy(MirrorPolicy policy, RequestContext context)
    {
        if (policy == null || global::System.String.IsNullOrWhiteSpace(policy.Directory) || policy.DatasetFamilies == null || context == null)
        {
            throw new ValidationException("A configured mirror policy and context are required.", "policy", "REQUIRED", null);
        }

        if (policy.DatasetFamilies.Length == 0 || policy.MaximumParallelTransfers <= 0)
        {
            throw new ValidationException("Mirror policy selections and limits are required.", "policy", "INVALID_MIRROR_POLICY", null);
        }

        BulkDatasetPresetResolver.ValidatePresetFamilies(policy.DatasetFamilies);
        context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static MirrorWorkflowResult ConvertRefreshResult(BulkLocalRefreshResult refresh, RequestContext context)
    {
        MirrorWorkflowResult result = new MirrorWorkflowResult();
        result.Manifest = refresh.Manifest;
        result.UnavailableFamilyCount = refresh.UnavailableFamilyCount;
        result.ProcessedFileCount = refresh.ProcessedFileCount;
        result.SkippedFileCount = refresh.SkippedFileCount;
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.DatasetIdentifier = "Bulk-to-local mirror refresh";
        result.Provenance.RequestIdentifier = context.CorrelationIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        global::System.Collections.Generic.List<MirrorWorkflowFileResult> files = new global::System.Collections.Generic.List<MirrorWorkflowFileResult>();
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();
        int index;
        for (index = 0; index < refresh.Files.Length; index += 1)
        {
            BulkLocalRefreshFileResult source = refresh.Files[index];
            MirrorWorkflowFileResult file = new MirrorWorkflowFileResult();
            file.Family = source.Family;
            file.ProductIdentifier = source.ProductIdentifier;
            file.FileName = source.FileName;
            file.Action = source.Plan.Action;
            file.WasProcessed = source.WasProcessed;
            file.WasSkipped = source.WasSkipped;
            file.IsComplete = source.IsComplete;
            if (source.Synchronization != null)
            {
                file.Transfer = source.Synchronization.Download;
            }

            if (source.IsComplete)
            {
                result.CompletedFileCount += 1;
            }
            else
            {
                SectionError error = CreateMirrorWorkflowError(source);
                file.Error = error;
                errors.Add(error);
                result.FailedFileCount += 1;
            }

            files.Add(file);
        }

        for (index = 0; index < refresh.Families.Length; index += 1)
        {
            if (!refresh.Families[index].IsAvailable)
            {
                SectionError error = new SectionError();
                error.Section = "family." + ((int)refresh.Families[index].Family).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
                error.ExceptionType = "ServiceUnavailableException";
                error.Message = "The selected mirror dataset family is unavailable.";
                error.ErrorCode = "MIRROR_FAMILY_UNAVAILABLE";
                error.Service = ApiService.Odp;
                errors.Add(error);
            }
            else if (!refresh.Families[index].MetadataPersisted)
            {
                SectionError error = new SectionError();
                error.Section = "family." + ((int)refresh.Families[index].Family).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
                error.ExceptionType = "MirrorException";
                error.Message = "The mirror family metadata could not be persisted.";
                error.ErrorCode = "MIRROR_METADATA_NOT_PERSISTED";
                errors.Add(error);
            }
        }

        result.Files = files.ToArray();
        if (!refresh.IsComplete && errors.Count == 0)
        {
            SectionError error = new SectionError();
            error.Section = "refresh";
            error.ExceptionType = "MirrorException";
            error.Message = "The mirror refresh did not complete.";
            error.ErrorCode = "MIRROR_REFRESH_INCOMPLETE";
            errors.Add(error);
        }

        result.Errors = errors.ToArray();
        result.IsComplete = refresh.IsComplete && result.Errors.Length == 0;
        result.HasPartialSuccess = result.CompletedFileCount > 0 && result.Errors.Length > 0;
        return result;
    }

    internal static SectionError CreateMirrorWorkflowError(BulkLocalRefreshFileResult source)
    {
        SectionError result = new SectionError();
        result.Section = (source.ProductIdentifier ?? "product") + "/" + (source.FileName ?? "file");
        result.ExceptionType = "MirrorException";
        result.Message = source.FailureMessage ?? "The mirror file refresh did not complete.";
        result.ErrorCode = source.FailureCode ?? "MIRROR_REFRESH_FAILED";
        if (source.Synchronization != null && source.Synchronization.Download.State != DownloadState.Completed)
        {
            DownloadResult download = source.Synchronization.Download;
            result.ExceptionType = "DownloadException";
            result.Message = source.FailureMessage ?? download.FailureMessage ?? "The mirror file transfer did not complete.";
            result.ErrorCode = source.FailureCode ?? download.FailureCode ?? "MIRROR_TRANSFER_FAILED";
        }

        return result;
    }

    internal static DatasetManifest LoadPlanningManifest(string directory)
    {
        if (global::System.IO.File.Exists(DatasetManifestStore.GetPath(directory)))
        {
            return DatasetManifestStore.Load(directory);
        }

        return new DatasetManifest();
    }

    internal BulkLocalRefreshRequest CreateRefreshRequest(MirrorPolicy policy, RequestContext context, MirrorPipelineStage lastStage)
    {
        BulkLocalRefreshRequest result = new BulkLocalRefreshRequest();
        result.MirrorDirectory = policy.Directory;
        result.DatasetFamilies = policy.DatasetFamilies;
        result.MaximumParallelTransfers = policy.MaximumParallelTransfers;
        result.HighWaterMark = policy.HighWaterMark;
        result.HasHighWaterMark = policy.HasHighWaterMark;
        result.VerifyChecksums = policy.VerifyChecksums;
        result.VerifyArchives = true;
        result.ArchiveLimits = Components.Options.Extraction;
        result.LastStage = lastStage;
        result.Context = context;
        return result;
    }

    internal static DownloadResult[] CollectSynchronizationDownloads(BulkLocalRefreshFileResult[] files)
    {
        global::System.Collections.Generic.List<DownloadResult> results = new global::System.Collections.Generic.List<DownloadResult>();
        int index;
        for (index = 0; index < files.Length; index += 1)
        {
            MirrorSynchronizationEntryResult? synchronization = files[index].Synchronization;
            if (synchronization != null)
            {
                results.Add(synchronization.Download);
            }
        }

        return results.ToArray();
    }

    internal static DownloadResult[] ConvertVerificationResults(MirrorVerificationResult verification)
    {
        DownloadResult[] results = new DownloadResult[verification.Files.Length];
        int index;
        for (index = 0; index < verification.Files.Length; index += 1)
        {
            MirrorFileVerificationResult file = verification.Files[index];
            DownloadResult result = new DownloadResult();
            result.Request.CatalogFileName = file.FileName;
            result.State = file.IsValid ? DownloadState.Completed : DownloadState.Failed;
            result.Progress.State = result.State;
            if (!file.IsValid)
            {
                result.FailureCode = "MIRROR_VERIFICATION_FAILED";
                result.FailureMessage = "The mirrored file did not pass verification.";
            }

            results[index] = result;
        }

        return results;
    }
}

internal class ConfiguredLocalDatasetQuery : ILocalDatasetQuery
{
    internal ConfiguredClientComponents Components;

    internal ConfiguredLocalDatasetQuery(ConfiguredClientComponents components)
    {
        Components = components;
    }

    public string[] ListLocalDatasetTables(string mirrorDirectory, RequestContext context)
    {
        PatentsViewLocalTableInfo[] tables = DiscoverLocalTables(mirrorDirectory, context);
        string[] results = new string[tables.Length];
        int index;
        for (index = 0; index < tables.Length; index += 1)
        {
            results[index] = tables[index].TableIdentifier;
        }

        return results;
    }

    public SearchResponseMetadata QueryLocalRows(string tableName, SearchRequest request)
    {
        PatentsViewLocalTableInfo table = FindLocalTable(tableName, request);
        return PatentsViewLocalRowQuery.Execute(CreateLocalRowRequest(table, request)).Metadata;
    }

    public SearchResponseMetadata QueryLocalPatents(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 1);
    }

    public SearchResponseMetadata QueryLocalPublications(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 2);
    }

    public SearchResponseMetadata QueryLocalInventors(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 3);
    }

    public SearchResponseMetadata QueryLocalAssignees(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 4);
    }

    public SearchResponseMetadata QueryLocalClassifications(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 5);
    }

    public SearchResponseMetadata QueryLocalCitations(SearchRequest request)
    {
        return ExecuteTypedLocalQuery(request, 6);
    }

    internal PatentsViewLocalTableInfo[] DiscoverLocalTables(string mirrorDirectory, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(mirrorDirectory) || context == null)
        {
            throw new ValidationException("A local mirror directory and context are required.", "mirrorDirectory", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        DatasetManifest manifest = DatasetManifestStore.Load(mirrorDirectory);
        global::System.Collections.Generic.List<string> paths = new global::System.Collections.Generic.List<string>();
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            int fileIndex;
            for (fileIndex = 0; fileIndex < manifest.Products[productIndex].Files.Length; fileIndex += 1)
            {
                DatasetManifestFile file = manifest.Products[productIndex].Files[fileIndex];
                int pathIndex;
                for (pathIndex = 0; pathIndex < file.ExtractedPaths.Length; pathIndex += 1)
                {
                    paths.Add(file.ExtractedPaths[pathIndex]);
                }
            }
        }

        PatentsViewLocalTableDiscoveryRequest request = new PatentsViewLocalTableDiscoveryRequest();
        request.MirrorDirectory = mirrorDirectory;
        request.ExtractedFiles = paths.ToArray();
        request.Limits = Components.Options.Limits;
        request.CancellationToken = context.CancellationToken;
        return PatentsViewLocalTableDiscovery.Discover(request).Tables;
    }

    internal PatentsViewLocalTableInfo FindLocalTable(string tableName, SearchRequest request)
    {
        if (global::System.String.IsNullOrWhiteSpace(tableName) || request == null || request.Context == null || global::System.String.IsNullOrWhiteSpace(Components.Options.Mirror.Directory))
        {
            throw new ValidationException("A configured local table query is required.", "request", "REQUIRED", null);
        }

        PatentsViewLocalTableInfo[] tables = DiscoverLocalTables(Components.Options.Mirror.Directory ?? string.Empty, request.Context);
        int index;
        for (index = 0; index < tables.Length; index += 1)
        {
            if (global::System.String.Equals(tables[index].TableIdentifier, tableName, global::System.StringComparison.Ordinal))
            {
                return tables[index];
            }
        }

        throw new NotFoundException("The local dataset table was not found.", ApiService.PatentsView, tableName, 0, false, null, null);
    }

    internal SearchResponseMetadata ExecuteTypedLocalQuery(SearchRequest request, int queryKind)
    {
        PatentsViewLocalTableInfo table = FindTypedLocalTable(request, queryKind);
        PatentsViewLocalTypedQueryResult result;
        PatentsViewLocalRowQueryRequest local = CreateLocalRowRequest(table, request);
        if (queryKind == 1)
        {
            result = PatentsViewLocalTypedQuery.QueryPatents(local);
        }
        else if (queryKind == 2)
        {
            result = PatentsViewLocalTypedQuery.QueryPublications(local);
        }
        else if (queryKind == 3)
        {
            result = PatentsViewLocalTypedQuery.QueryInventors(local);
        }
        else if (queryKind == 4)
        {
            result = PatentsViewLocalTypedQuery.QueryAssignees(local);
        }
        else if (queryKind == 5)
        {
            result = PatentsViewLocalTypedQuery.QueryClassifications(local);
        }
        else
        {
            result = PatentsViewLocalTypedQuery.QueryCitations(local);
        }

        return result.Metadata;
    }

    internal PatentsViewLocalTableInfo FindTypedLocalTable(SearchRequest request, int queryKind)
    {
        if (request == null || request.Context == null || global::System.String.IsNullOrWhiteSpace(Components.Options.Mirror.Directory))
        {
            throw new ValidationException("A configured typed local query is required.", "request", "REQUIRED", null);
        }

        PatentsViewLocalTableInfo[] tables = DiscoverLocalTables(Components.Options.Mirror.Directory ?? string.Empty, request.Context);
        int index;
        for (index = 0; index < tables.Length; index += 1)
        {
            if (MatchesTypedLocalTable(tables[index], queryKind))
            {
                return tables[index];
            }
        }

        throw new NotFoundException("A compatible local dataset table was not found.", ApiService.PatentsView, queryKind.ToString(global::System.Globalization.CultureInfo.InvariantCulture), 0, false, null, null);
    }

    internal static bool MatchesTypedLocalTable(PatentsViewLocalTableInfo table, int queryKind)
    {
        string name = table.TableIdentifier.ToLowerInvariant();
        if (queryKind == 1)
        {
            return table.Family == DatasetFamily.PatentsViewGrant && name.IndexOf("patent", global::System.StringComparison.Ordinal) >= 0;
        }

        if (queryKind == 2)
        {
            return table.Family == DatasetFamily.PatentsViewPreGrant && (name.IndexOf("publication", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("pgpub", global::System.StringComparison.Ordinal) >= 0);
        }

        if (queryKind == 3)
        {
            return name.IndexOf("inventor", global::System.StringComparison.Ordinal) >= 0;
        }

        if (queryKind == 4)
        {
            return name.IndexOf("assignee", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("applicant", global::System.StringComparison.Ordinal) >= 0;
        }

        if (queryKind == 5)
        {
            return name.IndexOf("cpc", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("ipc", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("uspc", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("wipo", global::System.StringComparison.Ordinal) >= 0;
        }

        return name.IndexOf("citation", global::System.StringComparison.Ordinal) >= 0 || name.IndexOf("reference", global::System.StringComparison.Ordinal) >= 0;
    }

    internal PatentsViewLocalRowQueryRequest CreateLocalRowRequest(PatentsViewLocalTableInfo table, SearchRequest request)
    {
        PatentsViewLocalRowQueryRequest result = new PatentsViewLocalRowQueryRequest();
        result.Table = table;
        result.Search = request;
        result.Limits = Components.Options.Limits;
        result.MaximumScannedRows = Components.Options.Limits.MaximumResults;
        return result;
    }
}
