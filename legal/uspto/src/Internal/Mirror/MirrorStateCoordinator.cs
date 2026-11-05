namespace uspto;

internal class MirrorLockRequest
{
    internal string? MirrorDirectory = null;
    internal int StaleAfterSeconds = 3600;
    internal global::System.DateTimeOffset Now = default(global::System.DateTimeOffset);
    internal global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);
}

internal class MirrorLockHandle
{
    internal string MirrorDirectory = string.Empty;
    internal string LockPath = string.Empty;
    internal string OwnerToken = string.Empty;
    internal int ProcessIdentifier = 0;
    internal bool IsAcquired = false;
    internal bool RecoveredStaleLock = false;
}

internal class MirrorStageTransitionRequest
{
    internal MirrorLockHandle Lock = new MirrorLockHandle();
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal string? StageName = null;
    internal MirrorStageState State = MirrorStageState.NotStarted;
    internal global::System.DateTimeOffset UpdatedAt = default(global::System.DateTimeOffset);
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
    internal string[] Secrets = new string[0];
}

internal static class MirrorStateCoordinator
{
    internal const string LockFileName = ".mirror.lock";
    internal const string WorkDirectoryName = ".work";

    internal static MirrorLockHandle Acquire(MirrorLockRequest request)
    {
        ValidateLockRequest(request);
        request.CancellationToken.ThrowIfCancellationRequested();
        string root = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
        global::System.IO.Directory.CreateDirectory(root);
        string lockPath = global::System.IO.Path.Combine(root, LockFileName);
        bool recovered = false;
        if (global::System.IO.File.Exists(lockPath))
        {
            global::System.DateTimeOffset modified = global::System.IO.File.GetLastWriteTimeUtc(lockPath);
            if (request.Now.ToUniversalTime() - modified >= global::System.TimeSpan.FromSeconds(request.StaleAfterSeconds))
            {
                global::System.IO.File.Delete(lockPath);
                recovered = true;
            }
            else
            {
                return new MirrorLockHandle { MirrorDirectory = root, LockPath = lockPath };
            }
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        string token = global::System.Guid.NewGuid().ToString("N");
        int processIdentifier = global::System.Environment.ProcessId;
        string content = token + "\n" + processIdentifier + "\n" + request.Now.ToUniversalTime().Ticks + "\n";
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(content);
        using (global::System.IO.FileStream output = new global::System.IO.FileStream(lockPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.Read))
        {
            output.Write(bytes, 0, bytes.Length);
            output.Flush(true);
        }

        global::System.IO.File.SetLastWriteTimeUtc(lockPath, request.Now.UtcDateTime);
        return new MirrorLockHandle
        {
            MirrorDirectory = root,
            LockPath = lockPath,
            OwnerToken = token,
            ProcessIdentifier = processIdentifier,
            IsAcquired = true,
            RecoveredStaleLock = recovered
        };
    }

    internal static bool Release(MirrorLockHandle handle)
    {
        if (handle == null || !handle.IsAcquired || global::System.String.IsNullOrWhiteSpace(handle.LockPath) || global::System.String.IsNullOrWhiteSpace(handle.OwnerToken))
        {
            return false;
        }

        if (!OwnsLock(handle))
        {
            handle.IsAcquired = false;
            return false;
        }

        global::System.IO.File.Delete(handle.LockPath);
        handle.IsAcquired = false;
        return true;
    }

    internal static bool OwnsLock(MirrorLockHandle handle)
    {
        if (!global::System.IO.File.Exists(handle.LockPath))
        {
            return false;
        }

        string[] lines = global::System.IO.File.ReadAllLines(handle.LockPath);
        return lines.Length >= 1 && lines[0].Equals(handle.OwnerToken, global::System.StringComparison.Ordinal);
    }

    internal static string CreateWorkDirectory(MirrorLockHandle handle, string operationName)
    {
        ValidateOwnedLock(handle);
        if (global::System.String.IsNullOrWhiteSpace(operationName))
        {
            throw new ValidationException("A mirror work operation name is required.", "operationName", "REQUIRED", null);
        }

        global::System.Text.StringBuilder safeName = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < operationName.Length; index += 1)
        {
            char character = operationName[index];
            if (global::System.Char.IsLetterOrDigit(character) || character == '-' || character == '_')
            {
                safeName.Append(character);
            }
        }

        if (safeName.Length == 0)
        {
            throw new ValidationException("The mirror work operation name has no safe characters.", "operationName", "INVALID_WORK_NAME", null);
        }

        string workRoot = global::System.IO.Path.Combine(handle.MirrorDirectory, WorkDirectoryName);
        global::System.IO.Directory.CreateDirectory(workRoot);
        string path = global::System.IO.Path.Combine(workRoot, safeName + "-" + global::System.Guid.NewGuid().ToString("N"));
        global::System.IO.Directory.CreateDirectory(path);
        return path;
    }

    internal static bool DeleteWorkDirectory(MirrorLockHandle handle, string workDirectory)
    {
        ValidateOwnedLock(handle);
        string workRoot = global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(handle.MirrorDirectory, WorkDirectoryName));
        string path = global::System.IO.Path.GetFullPath(workDirectory);
        string prefix = workRoot + global::System.IO.Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, global::System.StringComparison.Ordinal) || path.Equals(workRoot, global::System.StringComparison.Ordinal))
        {
            throw new ValidationException("Only owned mirror work directories can be removed.", "workDirectory", "WORK_PATH_INVALID", null);
        }

        bool existed = global::System.IO.Directory.Exists(path);
        if (existed)
        {
            global::System.IO.Directory.Delete(path, true);
        }

        return existed;
    }

    internal static DatasetManifest TransitionStage(MirrorStageTransitionRequest request)
    {
        ValidateTransition(request);
        ValidateOwnedLock(request.Lock);
        DatasetManifest manifest = DatasetManifestStore.Load(request.Lock.MirrorDirectory);
        DatasetManifestStage? stage = FindStage(manifest, request.ProductIdentifier ?? string.Empty, request.FileName ?? string.Empty, request.StageName ?? string.Empty);
        if (stage == null)
        {
            throw new ValidationException("The requested manifest stage was not found.", "StageName", "MANIFEST_STAGE_NOT_FOUND", null);
        }

        if (!CanTransition(stage.State, request.State))
        {
            throw new ValidationException("The requested manifest stage transition is invalid.", "State", "MANIFEST_STAGE_TRANSITION_INVALID", null);
        }

        stage.State = request.State;
        stage.UpdatedAt = request.UpdatedAt.ToUniversalTime();
        stage.HasUpdatedAt = true;
        stage.FailureCode = request.FailureCode;
        stage.FailureMessage = request.FailureMessage;
        if (request.State == MirrorStageState.Complete || request.State == MirrorStageState.InProgress)
        {
            stage.FailureCode = null;
            stage.FailureMessage = null;
        }

        manifest.UpdatedAt = request.UpdatedAt.ToUniversalTime();
        manifest.HasUpdatedAt = true;
        return DatasetManifestStore.Save(request.Lock.MirrorDirectory, manifest, request.Secrets);
    }

    internal static DatasetManifestStage? FindStage(DatasetManifest manifest, string productIdentifier, string fileName, string stageName)
    {
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            if (global::System.String.Equals(product.ProductIdentifier, productIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
            {
                int fileIndex;
                for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
                {
                    DatasetManifestFile file = product.Files[fileIndex];
                    if (global::System.String.Equals(file.FileName, fileName, global::System.StringComparison.Ordinal))
                    {
                        int stageIndex;
                        for (stageIndex = 0; stageIndex < file.Stages.Length; stageIndex += 1)
                        {
                            if (global::System.String.Equals(file.Stages[stageIndex].Name, stageName, global::System.StringComparison.OrdinalIgnoreCase))
                            {
                                return file.Stages[stageIndex];
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    internal static bool CanTransition(MirrorStageState current, MirrorStageState next)
    {
        if (current == next)
        {
            return true;
        }

        if (next == MirrorStageState.InProgress)
        {
            return current == MirrorStageState.NotStarted || current == MirrorStageState.Failed || current == MirrorStageState.Complete;
        }

        return current == MirrorStageState.InProgress && (next == MirrorStageState.Complete || next == MirrorStageState.Failed);
    }

    internal static void ValidateOwnedLock(MirrorLockHandle handle)
    {
        if (handle == null || !handle.IsAcquired || !OwnsLock(handle))
        {
            throw new ValidationException("The mirror lock is not owned by this operation.", "Lock", "MIRROR_LOCK_NOT_OWNED", null);
        }
    }

    internal static void ValidateLockRequest(MirrorLockRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory))
        {
            throw new ValidationException("A mirror lock request and directory are required.", "request", "REQUIRED", null);
        }

        if (request.StaleAfterSeconds <= 0 || request.Now == default(global::System.DateTimeOffset))
        {
            throw new ValidationException("Mirror lock age settings are invalid.", "StaleAfterSeconds", "MIRROR_LOCK_POLICY_INVALID", null);
        }
    }

    internal static void ValidateTransition(MirrorStageTransitionRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.ProductIdentifier) || global::System.String.IsNullOrWhiteSpace(request.FileName) || global::System.String.IsNullOrWhiteSpace(request.StageName))
        {
            throw new ValidationException("A complete mirror stage transition request is required.", "request", "REQUIRED", null);
        }

        if (request.State < MirrorStageState.NotStarted || request.State > MirrorStageState.Failed || request.UpdatedAt == default(global::System.DateTimeOffset) || request.Secrets == null)
        {
            throw new ValidationException("The mirror stage transition values are invalid.", "State", "MANIFEST_STAGE_TRANSITION_INVALID", null);
        }

        if (request.State == MirrorStageState.Failed && global::System.String.IsNullOrWhiteSpace(request.FailureCode))
        {
            throw new ValidationException("A failed mirror stage requires a failure code.", "FailureCode", "REQUIRED", null);
        }
    }
}
