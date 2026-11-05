namespace uspto;

internal enum MirrorPipelineStage
{
    Download = 0,
    Checksum = 1,
    Extraction = 2,
    Parse = 3
}

internal class MirrorPipelineStageRequest
{
    internal MirrorPipelineStage Stage = MirrorPipelineStage.Download;
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string MirrorDirectory = string.Empty;
    internal string LocalPath = string.Empty;
    internal DatasetManifestFile ManifestFile = new DatasetManifestFile();
    internal RequestContext Context = new RequestContext();
}

internal class MirrorPipelineStageExecutionResult
{
    internal bool IsComplete = false;
    internal bool IsCancelled = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
    internal long ActualSize = 0L;
    internal bool HasActualSize = false;
    internal ChecksumAlgorithm DigestAlgorithm = ChecksumAlgorithm.None;
    internal string? Digest = null;
    internal bool HasDigest = false;
    internal string[] ExtractedPaths = new string[0];
    internal bool HasExtractedPaths = false;
}

internal interface IMirrorPipelineStageExecutor
{
    MirrorPipelineStageExecutionResult ExecuteMirrorPipelineStage(MirrorPipelineStageRequest request);
}

internal class MirrorStagePipelineRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorPipelineStage LastStage = MirrorPipelineStage.Parse;
    internal int StaleLockSeconds = 3600;
    internal global::System.DateTimeOffset Now = default(global::System.DateTimeOffset);
    internal RequestContext Context = new RequestContext();
    internal string[] Secrets = new string[0];
}

internal class MirrorPipelineStageResult
{
    internal MirrorPipelineStage Stage = MirrorPipelineStage.Download;
    internal MirrorStageState State = MirrorStageState.NotStarted;
    internal bool WasExecuted = false;
    internal bool WasReused = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal class MirrorStagePipelineResult
{
    internal MirrorPipelineStageResult[] Stages = new MirrorPipelineStageResult[0];
    internal DatasetManifest Manifest = new DatasetManifest();
    internal bool LockAcquired = false;
    internal bool IsComplete = false;
    internal bool IsCancelled = false;
    internal int ExecutedCount = 0;
    internal int ReusedCount = 0;
}

internal class MirrorStagePipeline
{
    internal IMirrorPipelineStageExecutor Executor;

    internal MirrorStagePipeline(IMirrorPipelineStageExecutor executor)
    {
        if (executor == null)
        {
            throw new ValidationException("A mirror pipeline stage executor is required.", "executor", "REQUIRED", null);
        }

        Executor = executor;
    }

    internal MirrorStagePipelineResult Run(MirrorStagePipelineRequest request)
    {
        ValidatePipelineRequest(request);
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
            return new MirrorStagePipelineResult { Manifest = new DatasetManifest(), LockAcquired = false };
        }

        using (MirrorSynchronizationLockLease lease = new MirrorSynchronizationLockLease(mirrorLock))
        {
            DatasetManifest manifest = DatasetManifestStore.Load(mirrorLock.MirrorDirectory);
            DatasetManifestFile file = MirrorSynchronizer.FindManifestFile(manifest, request.ProductIdentifier ?? string.Empty, request.FileName ?? string.Empty);
            EnsurePipelineStages(file);
            manifest = DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
            global::System.Collections.Generic.List<MirrorPipelineStageResult> results = new global::System.Collections.Generic.List<MirrorPipelineStageResult>();
            int stageValue;
            for (stageValue = 0; stageValue <= (int)request.LastStage; stageValue += 1)
            {
                request.Context.CancellationToken.ThrowIfCancellationRequested();
                MirrorPipelineStage pipelineStage = (MirrorPipelineStage)stageValue;
                file = MirrorSynchronizer.FindManifestFile(manifest, request.ProductIdentifier ?? string.Empty, request.FileName ?? string.Empty);
                DatasetManifestStage stage = FindPipelineStage(file, pipelineStage);
                if (stage.State == MirrorStageState.Complete)
                {
                    MirrorPipelineStageResult reused = new MirrorPipelineStageResult();
                    reused.Stage = pipelineStage;
                    reused.State = MirrorStageState.Complete;
                    reused.WasReused = true;
                    results.Add(reused);
                    continue;
                }

                InvalidateDownstreamStages(file, pipelineStage);
                SetPipelineStageState(stage, MirrorStageState.InProgress, null, null);
                manifest = DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
                file = MirrorSynchronizer.FindManifestFile(manifest, request.ProductIdentifier ?? string.Empty, request.FileName ?? string.Empty);
                MirrorPipelineStageRequest executionRequest = CreateExecutionRequest(request, mirrorLock.MirrorDirectory, file, pipelineStage);
                MirrorPipelineStageExecutionResult execution = Executor.ExecuteMirrorPipelineStage(executionRequest);
                if (execution == null)
                {
                    throw new ValidationException("The mirror stage executor returned no result.", "executor", "MIRROR_STAGE_RESULT_REQUIRED", null);
                }

                file = MirrorSynchronizer.FindManifestFile(manifest, request.ProductIdentifier ?? string.Empty, request.FileName ?? string.Empty);
                stage = FindPipelineStage(file, pipelineStage);
                MirrorPipelineStageResult stageResult = CreateStageResult(pipelineStage, execution);
                if (execution.IsComplete)
                {
                    ValidateCompletedStageOutput(pipelineStage, execution, executionRequest.LocalPath);
                    ApplyCompletedStageOutput(file, pipelineStage, execution);
                    SetPipelineStageState(stage, MirrorStageState.Complete, null, null);
                    stageResult.State = MirrorStageState.Complete;
                    manifest = DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
                    results.Add(stageResult);
                }
                else
                {
                    string failureCode = global::System.String.IsNullOrWhiteSpace(execution.FailureCode) ? "MIRROR_STAGE_FAILED" : execution.FailureCode ?? "MIRROR_STAGE_FAILED";
                    string failureMessage = global::System.String.IsNullOrWhiteSpace(execution.FailureMessage) ? "The mirror pipeline stage did not complete." : execution.FailureMessage ?? "The mirror pipeline stage did not complete.";
                    SetPipelineStageState(stage, MirrorStageState.Failed, failureCode, failureMessage);
                    stageResult.State = MirrorStageState.Failed;
                    stageResult.FailureCode = failureCode;
                    stageResult.FailureMessage = SecretRedactor.RedactText(failureMessage, request.Secrets);
                    manifest = DatasetManifestStore.Save(mirrorLock.MirrorDirectory, manifest, request.Secrets);
                    results.Add(stageResult);
                    return CreatePipelineResult(results, manifest, true, false, execution.IsCancelled);
                }
            }

            return CreatePipelineResult(results, manifest, true, true, false);
        }
    }

    internal static void ValidatePipelineRequest(MirrorStagePipelineRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory) || global::System.String.IsNullOrWhiteSpace(request.ProductIdentifier) || global::System.String.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ValidationException("A complete mirror pipeline request is required.", "request", "REQUIRED", null);
        }

        if (!BulkDatasetPresetResolver.IsSupportedPresetFamily(request.Family))
        {
            throw new ValidationException("A supported mirror dataset family is required.", "Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }

        if (request.LastStage < MirrorPipelineStage.Download || request.LastStage > MirrorPipelineStage.Parse || request.StaleLockSeconds <= 0 || request.Context == null || request.Secrets == null)
        {
            throw new ValidationException("Mirror pipeline settings are invalid.", "request", "MIRROR_PIPELINE_INVALID", null);
        }
    }

    internal static void EnsurePipelineStages(DatasetManifestFile file)
    {
        ValidateUniqueStages(file);
        EnsureNamedStage(file, MirrorPipelineStage.Download);
        EnsureNamedStage(file, MirrorPipelineStage.Checksum);
        EnsureNamedStage(file, MirrorPipelineStage.Extraction);
        EnsureNamedStage(file, MirrorPipelineStage.Parse);
    }

    internal static void ValidateUniqueStages(DatasetManifestFile file)
    {
        global::System.Collections.Generic.HashSet<string> names = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (!names.Add(file.Stages[index].Name ?? string.Empty))
            {
                throw new ValidationException("Manifest stage names must be unique.", "Stages", "MANIFEST_STAGE_DUPLICATE", null);
            }
        }
    }

    internal static void EnsureNamedStage(DatasetManifestFile file, MirrorPipelineStage pipelineStage)
    {
        string name = GetStageName(pipelineStage);
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (global::System.String.Equals(file.Stages[index].Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        DatasetManifestStage[] stages = new DatasetManifestStage[file.Stages.Length + 1];
        global::System.Array.Copy(file.Stages, stages, file.Stages.Length);
        stages[stages.Length - 1] = new DatasetManifestStage { Name = name };
        file.Stages = stages;
    }

    internal static DatasetManifestStage FindPipelineStage(DatasetManifestFile file, MirrorPipelineStage pipelineStage)
    {
        string name = GetStageName(pipelineStage);
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (global::System.String.Equals(file.Stages[index].Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return file.Stages[index];
            }
        }

        throw new ValidationException("The requested mirror pipeline stage was not found.", "Stages", "MANIFEST_STAGE_NOT_FOUND", null);
    }

    internal static string GetStageName(MirrorPipelineStage pipelineStage)
    {
        switch (pipelineStage)
        {
            case MirrorPipelineStage.Download:
                return "download";
            case MirrorPipelineStage.Checksum:
                return "checksum";
            case MirrorPipelineStage.Extraction:
                return "extract";
            case MirrorPipelineStage.Parse:
                return "parse";
            default:
                throw new ValidationException("The mirror pipeline stage is invalid.", "Stage", "MIRROR_PIPELINE_INVALID", null);
        }
    }

    internal static void InvalidateDownstreamStages(DatasetManifestFile file, MirrorPipelineStage pipelineStage)
    {
        int stageValue;
        for (stageValue = (int)pipelineStage + 1; stageValue <= (int)MirrorPipelineStage.Parse; stageValue += 1)
        {
            DatasetManifestStage downstream = FindPipelineStage(file, (MirrorPipelineStage)stageValue);
            downstream.State = MirrorStageState.NotStarted;
            downstream.FailureCode = null;
            downstream.FailureMessage = null;
            downstream.UpdatedAt = default(global::System.DateTimeOffset);
            downstream.HasUpdatedAt = false;
        }
    }

    internal static void SetPipelineStageState(DatasetManifestStage stage, MirrorStageState state, string? failureCode, string? failureMessage)
    {
        if (!MirrorStateCoordinator.CanTransition(stage.State, state))
        {
            throw new ValidationException("The mirror pipeline stage transition is invalid.", "State", "MANIFEST_STAGE_TRANSITION_INVALID", null);
        }

        stage.State = state;
        stage.UpdatedAt = global::System.DateTimeOffset.UtcNow;
        stage.HasUpdatedAt = true;
        stage.FailureCode = state == MirrorStageState.Failed ? failureCode : null;
        stage.FailureMessage = state == MirrorStageState.Failed ? failureMessage : null;
    }

    internal static MirrorPipelineStageRequest CreateExecutionRequest(MirrorStagePipelineRequest request, string mirrorDirectory, DatasetManifestFile file, MirrorPipelineStage pipelineStage)
    {
        MirrorPipelineStageRequest result = new MirrorPipelineStageRequest();
        result.Stage = pipelineStage;
        result.Family = request.Family;
        result.MirrorDirectory = mirrorDirectory;
        result.LocalPath = MirrorSynchronizer.ResolveManifestPath(mirrorDirectory, file.LocalPath);
        result.ManifestFile = file;
        result.Context = request.Context;
        return result;
    }

    internal static MirrorPipelineStageResult CreateStageResult(MirrorPipelineStage pipelineStage, MirrorPipelineStageExecutionResult execution)
    {
        MirrorPipelineStageResult result = new MirrorPipelineStageResult();
        result.Stage = pipelineStage;
        result.WasExecuted = true;
        result.FailureCode = execution.FailureCode;
        result.FailureMessage = execution.FailureMessage;
        return result;
    }

    internal static void ValidateCompletedStageOutput(MirrorPipelineStage pipelineStage, MirrorPipelineStageExecutionResult execution, string localPath)
    {
        if (pipelineStage == MirrorPipelineStage.Download && !global::System.IO.File.Exists(localPath))
        {
            throw new ValidationException("A completed download stage must produce its local artifact.", "LocalPath", "MIRROR_DOWNLOAD_ARTIFACT_MISSING", null);
        }

        if (pipelineStage == MirrorPipelineStage.Checksum)
        {
            ChecksumExpectation expectation = new ChecksumExpectation();
            expectation.Algorithm = execution.DigestAlgorithm;
            expectation.Value = execution.Digest;
            NormalizedChecksumValue normalized = BulkChecksumVerifier.NormalizeExpected(expectation);
            if (!execution.HasDigest || !normalized.IsValid)
            {
                throw new ValidationException("A completed checksum stage must produce a supported digest.", "Digest", "MIRROR_CHECKSUM_RESULT_INVALID", null);
            }
        }

        if (pipelineStage == MirrorPipelineStage.Extraction)
        {
            if (!execution.HasExtractedPaths || execution.ExtractedPaths == null)
            {
                throw new ValidationException("A completed extraction stage must report its output paths.", "ExtractedPaths", "MIRROR_EXTRACTION_RESULT_INVALID", null);
            }

            int index;
            for (index = 0; index < execution.ExtractedPaths.Length; index += 1)
            {
                DatasetManifestStore.ValidatePath(execution.ExtractedPaths[index], "ExtractedPaths");
            }
        }
    }

    internal static void ApplyCompletedStageOutput(DatasetManifestFile file, MirrorPipelineStage pipelineStage, MirrorPipelineStageExecutionResult execution)
    {
        if (pipelineStage == MirrorPipelineStage.Download && execution.HasActualSize)
        {
            file.Size = execution.ActualSize;
            file.HasSize = true;
        }
        else if (pipelineStage == MirrorPipelineStage.Checksum)
        {
            ChecksumExpectation expectation = new ChecksumExpectation();
            expectation.Algorithm = execution.DigestAlgorithm;
            expectation.Value = execution.Digest;
            NormalizedChecksumValue normalized = BulkChecksumVerifier.NormalizeExpected(expectation);
            file.DigestAlgorithm = execution.DigestAlgorithm;
            file.Digest = normalized.Value;
            file.DigestVerified = true;
        }
        else if (pipelineStage == MirrorPipelineStage.Extraction)
        {
            string[] paths = new string[execution.ExtractedPaths.Length];
            global::System.Array.Copy(execution.ExtractedPaths, paths, paths.Length);
            file.ExtractedPaths = paths;
        }
    }

    internal static MirrorStagePipelineResult CreatePipelineResult(global::System.Collections.Generic.List<MirrorPipelineStageResult> stages, DatasetManifest manifest, bool lockAcquired, bool isComplete, bool isCancelled)
    {
        MirrorStagePipelineResult result = new MirrorStagePipelineResult();
        result.Stages = stages.ToArray();
        result.Manifest = manifest;
        result.LockAcquired = lockAcquired;
        result.IsComplete = isComplete;
        result.IsCancelled = isCancelled;
        int index;
        for (index = 0; index < result.Stages.Length; index += 1)
        {
            if (result.Stages[index].WasExecuted)
            {
                result.ExecutedCount += 1;
            }

            if (result.Stages[index].WasReused)
            {
                result.ReusedCount += 1;
            }
        }

        return result;
    }
}
