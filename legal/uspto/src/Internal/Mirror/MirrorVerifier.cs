namespace uspto;

internal enum MirrorVerificationCheck
{
    ManifestConsistency = 0,
    FileExistence = 1,
    FileSize = 2,
    Checksum = 3,
    ArchiveReadability = 4,
    ExtractedArtifacts = 5,
    ParserState = 6
}

internal class MirrorVerificationRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetManifest Manifest = new DatasetManifest();
    internal bool VerifyChecksums = true;
    internal bool VerifyArchives = true;
    internal int ChecksumBufferSize = 81920;
    internal ExtractionLimits ArchiveLimits = new ExtractionLimits();
    internal RequestContext Context = new RequestContext();
}

internal class MirrorVerificationCheckResult
{
    internal MirrorVerificationCheck Check = MirrorVerificationCheck.ManifestConsistency;
    internal bool WasEvaluated = false;
    internal bool IsValid = true;
    internal string? Code = null;
    internal string? Message = null;
    internal string? Path = null;
}

internal class MirrorFileVerificationResult
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal MirrorAction Action = MirrorAction.Unknown;
    internal MirrorVerificationCheckResult[] Checks = new MirrorVerificationCheckResult[0];
    internal bool IsValid = false;
}

internal class MirrorVerificationResult
{
    internal MirrorVerificationCheckResult[] ManifestChecks = new MirrorVerificationCheckResult[0];
    internal MirrorFileVerificationResult[] Files = new MirrorFileVerificationResult[0];
    internal bool IsValid = false;
    internal int ValidFileCount = 0;
    internal int MissingFileCount = 0;
    internal int CorruptFileCount = 0;
}

internal class ArchiveVerificationTaskResult
{
    internal ArchiveListingResult? Listing = null;
    internal bool IsFaulted = false;
}

internal static class MirrorVerifier
{
    internal static MirrorVerificationResult Verify(MirrorVerificationRequest request)
    {
        ValidateVerificationRequest(request);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        string root = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
        global::System.Collections.Generic.List<MirrorVerificationCheckResult> manifestChecks = new global::System.Collections.Generic.List<MirrorVerificationCheckResult>();
        bool manifestValid = VerifyManifestStructure(request.Manifest, manifestChecks);
        global::System.Collections.Generic.List<MirrorFileVerificationResult> files = new global::System.Collections.Generic.List<MirrorFileVerificationResult>();
        if (request.Manifest.Products != null)
        {
            int productIndex;
            for (productIndex = 0; productIndex < request.Manifest.Products.Length; productIndex += 1)
            {
                request.Context.CancellationToken.ThrowIfCancellationRequested();
                DatasetManifestProduct product = request.Manifest.Products[productIndex];
                if (product != null && product.Files != null)
                {
                    int fileIndex;
                    for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
                    {
                        request.Context.CancellationToken.ThrowIfCancellationRequested();
                        DatasetManifestFile file = product.Files[fileIndex];
                        if (file != null)
                        {
                            files.Add(VerifyFile(root, product, file, request));
                        }
                    }
                }
            }
        }

        return CreateVerificationResult(manifestChecks, files, manifestValid);
    }

    internal static void ValidateVerificationRequest(MirrorVerificationRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory) || request.Manifest == null || request.Context == null || request.ArchiveLimits == null)
        {
            throw new ValidationException("Complete mirror verification inputs are required.", "request", "REQUIRED", null);
        }

        if (request.ChecksumBufferSize <= 0)
        {
            throw new ValidationException("The mirror verification checksum buffer must be positive.", "ChecksumBufferSize", "INVALID_LIMIT", null);
        }
    }

    internal static bool VerifyManifestStructure(DatasetManifest manifest, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        bool valid = true;
        if (manifest.FormatVersion != DatasetManifestStore.FormatVersion || manifest.Products == null || manifest.Provenance == null)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_INVALID", "The dataset manifest root is incomplete or unsupported.", null));
            return false;
        }

        global::System.Collections.Generic.HashSet<string> products = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> files = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> localPaths = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            if (product == null || global::System.String.IsNullOrWhiteSpace(product.ProductIdentifier) || product.Files == null || product.Provenance == null)
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PRODUCT_INVALID", "A manifest product is incomplete.", null));
                valid = false;
                continue;
            }

            string productKey = ((int)product.Family).ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + product.ProductIdentifier;
            if (!products.Add(productKey))
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PRODUCT_DUPLICATE", "A manifest product is repeated.", product.ProductIdentifier));
                valid = false;
            }

            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                DatasetManifestFile file = product.Files[fileIndex];
                if (!VerifyManifestFileStructure(product, file, files, localPaths, checks))
                {
                    valid = false;
                }
            }
        }

        if (valid)
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_CONSISTENT", null));
        }

        return valid;
    }

    internal static bool VerifyManifestFileStructure(DatasetManifestProduct product, DatasetManifestFile file, global::System.Collections.Generic.HashSet<string> files, global::System.Collections.Generic.HashSet<string> localPaths, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        if (file == null || global::System.String.IsNullOrWhiteSpace(file.ProductIdentifier) || global::System.String.IsNullOrWhiteSpace(file.FileName) || file.ExtractedPaths == null || file.Stages == null || file.Provenance == null)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_FILE_INVALID", "A manifest file is incomplete.", null));
            return false;
        }

        bool valid = true;
        string identity = MirrorPlanner.CreateKey(file.ProductIdentifier ?? string.Empty, file.FileName ?? string.Empty);
        if (!files.Add(identity))
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_FILE_DUPLICATE", "A manifest file is repeated.", file.FileName));
            valid = false;
        }

        if (!global::System.String.Equals(product.ProductIdentifier, file.ProductIdentifier, global::System.StringComparison.Ordinal))
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PRODUCT_MISMATCH", "A manifest file belongs to a different product.", file.FileName));
            valid = false;
        }

        if (!IsSafeRelativePath(file.LocalPath) || !IsSafeRelativePath(file.PartialPath))
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PATH_INVALID", "A manifest artifact path is invalid.", file.FileName));
            valid = false;
        }

        if (file.LocalPath != null && !localPaths.Add(file.LocalPath.Replace('\\', '/')))
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PATH_DUPLICATE", "Multiple manifest files use the same local path.", file.LocalPath));
            valid = false;
        }

        bool digestInvalid = false;
        if (file.DigestVerified)
        {
            ChecksumExpectation digestExpectation = new ChecksumExpectation();
            digestExpectation.Algorithm = file.DigestAlgorithm;
            digestExpectation.Value = file.Digest;
            digestInvalid = !BulkChecksumVerifier.NormalizeExpected(digestExpectation).IsValid;
        }

        if (file.HasSize && file.Size < 0L || digestInvalid)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_FILE_METADATA_INVALID", "Manifest size or digest state is inconsistent.", file.FileName));
            valid = false;
        }

        int pathIndex;
        for (pathIndex = 0; pathIndex < file.ExtractedPaths.Length; pathIndex += 1)
        {
            if (!IsSafeRelativePath(file.ExtractedPaths[pathIndex]))
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_PATH_INVALID", "An extracted artifact path is invalid.", file.ExtractedPaths[pathIndex]));
                valid = false;
            }
        }

        if (!VerifyStageConsistency(file, checks))
        {
            valid = false;
        }

        return valid;
    }

    internal static bool VerifyStageConsistency(DatasetManifestFile file, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        global::System.Collections.Generic.HashSet<string> names = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        bool valid = true;
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            DatasetManifestStage stage = file.Stages[index];
            if (stage == null || global::System.String.IsNullOrWhiteSpace(stage.Name) || !names.Add(stage.Name ?? string.Empty))
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_STAGE_INVALID", "A manifest stage is missing or repeated.", file.FileName));
                valid = false;
            }
        }

        DatasetManifestStage? download = FindOptionalStage(file, "download");
        DatasetManifestStage? checksum = FindOptionalStage(file, "checksum");
        DatasetManifestStage? extraction = FindOptionalStage(file, "extract");
        DatasetManifestStage? parse = FindOptionalStage(file, "parse");
        if (checksum != null && checksum.State == MirrorStageState.Complete && (download == null || download.State != MirrorStageState.Complete))
        {
            valid = false;
        }

        if (extraction != null && extraction.State == MirrorStageState.Complete && (checksum == null || checksum.State != MirrorStageState.Complete))
        {
            valid = false;
        }

        if (parse != null && parse.State == MirrorStageState.Complete && (extraction == null || extraction.State != MirrorStageState.Complete))
        {
            valid = false;
        }

        if (!valid)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ManifestConsistency, "MANIFEST_STAGE_DEPENDENCY_INVALID", "Completed manifest stages do not have completed prerequisites.", file.FileName));
        }

        return valid;
    }

    internal static bool IsSafeRelativePath(string? path)
    {
        if (path == null)
        {
            return true;
        }

        if (global::System.String.IsNullOrWhiteSpace(path) || global::System.IO.Path.IsPathRooted(path))
        {
            return false;
        }

        string[] parts = path.Replace('\\', '/').Split('/');
        int index;
        for (index = 0; index < parts.Length; index += 1)
        {
            if (parts[index].Length == 0 || parts[index] == "." || parts[index] == "..")
            {
                return false;
            }
        }

        return true;
    }

    internal static MirrorFileVerificationResult VerifyFile(string root, DatasetManifestProduct product, DatasetManifestFile file, MirrorVerificationRequest request)
    {
        global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks = new global::System.Collections.Generic.List<MirrorVerificationCheckResult>();
        string? localPath = ResolveVerificationPath(root, file.LocalPath);
        bool exists = localPath != null && global::System.IO.File.Exists(localPath);
        if (!exists)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.FileExistence, "MIRROR_FILE_MISSING", "The mirrored local file does not exist.", localPath ?? file.LocalPath));
            AddUnevaluatedFileChecks(checks, request);
            return CreateFileResult(product, file, checks, MirrorAction.Missing);
        }

        DatasetManifestStage? download = FindOptionalStage(file, "download");
        if (download != null && download.State != MirrorStageState.Complete)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.FileExistence, "MIRROR_DOWNLOAD_INCOMPLETE", "The local artifact exists but its download stage is incomplete.", localPath));
        }
        else
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.FileExistence, "MIRROR_FILE_PRESENT", localPath));
        }
        VerifyFileSize(localPath ?? string.Empty, file, checks);
        VerifyFileChecksum(localPath ?? string.Empty, file, request, checks);
        VerifyArchive(localPath ?? string.Empty, request, checks);
        VerifyExtractedArtifacts(root, file, checks);
        VerifyParserStage(file, checks);
        MirrorAction action = HasFailedCheck(checks) ? MirrorAction.Corrupt : MirrorAction.Unchanged;
        return CreateFileResult(product, file, checks, action);
    }

    internal static string? ResolveVerificationPath(string root, string? relativePath)
    {
        if (!IsSafeRelativePath(relativePath) || relativePath == null)
        {
            return null;
        }

        return global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(root, relativePath));
    }

    internal static void VerifyFileSize(string localPath, DatasetManifestFile file, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        if (!file.HasSize)
        {
            checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.FileSize, "MIRROR_SIZE_UNAVAILABLE", localPath));
            return;
        }

        long actual = new global::System.IO.FileInfo(localPath).Length;
        if (actual == file.Size)
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.FileSize, "MIRROR_SIZE_MATCH", localPath));
        }
        else
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.FileSize, "MIRROR_SIZE_MISMATCH", "The mirrored file size does not match its manifest.", localPath));
        }
    }

    internal static void VerifyFileChecksum(string localPath, DatasetManifestFile file, MirrorVerificationRequest request, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        if (!request.VerifyChecksums)
        {
            checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_DISABLED", localPath));
            return;
        }

        DatasetManifestStage? checksumStage = FindOptionalStage(file, "checksum");
        if (checksumStage != null && checksumStage.State != MirrorStageState.Complete)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_INCOMPLETE", "The manifest checksum stage has not completed successfully.", localPath));
            return;
        }

        if (!file.DigestVerified || global::System.String.IsNullOrWhiteSpace(file.Digest) || !BulkChecksumVerifier.IsSupportedAlgorithm(file.DigestAlgorithm))
        {
            if (checksumStage != null)
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_EVIDENCE_MISSING", "The completed checksum stage has no verified digest.", localPath));
            }
            else
            {
                checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_UNAVAILABLE", localPath));
            }

            return;
        }

        ChecksumExpectation expectation = new ChecksumExpectation();
        expectation.Algorithm = file.DigestAlgorithm;
        expectation.Value = file.Digest;
        ChecksumResult[] results = BulkChecksumVerifier.VerifyFile(localPath, new ChecksumExpectation[] { expectation }, request.ChecksumBufferSize, request.Context.CancellationToken);
        if (BulkChecksumVerifier.HasMismatch(results))
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_MISMATCH", "The mirrored file digest does not match its manifest.", localPath));
        }
        else
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.Checksum, "MIRROR_CHECKSUM_MATCH", localPath));
        }
    }

    internal static void VerifyArchive(string localPath, MirrorVerificationRequest request, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        if (!request.VerifyArchives || !HasArchiveExtension(localPath))
        {
            checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ArchiveReadability, "MIRROR_ARCHIVE_NOT_APPLICABLE", localPath));
            return;
        }

        ArchiveVerificationTaskResult archive = ReadArchiveSafely(localPath, request.Context, request.ArchiveLimits);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        if (archive.IsFaulted || archive.Listing == null || !archive.Listing.IsComplete)
        {
            string code = archive.Listing == null || global::System.String.IsNullOrWhiteSpace(archive.Listing.FailureCode) ? "MIRROR_ARCHIVE_UNREADABLE" : archive.Listing.FailureCode ?? "MIRROR_ARCHIVE_UNREADABLE";
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ArchiveReadability, code, "The mirrored archive could not be read completely.", localPath));
        }
        else
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.ArchiveReadability, "MIRROR_ARCHIVE_READABLE", localPath));
        }
    }

    internal static ArchiveVerificationTaskResult ReadArchiveSafely(string localPath, RequestContext context, ExtractionLimits limits)
    {
        global::System.Threading.Tasks.Task<ArchiveListingResult> task = global::System.Threading.Tasks.Task.Run(() => ArchiveListingOperation.ListWithLimits(localPath, context, limits));
        global::System.IAsyncResult asynchronous = task;
        using (global::System.Threading.WaitHandle completion = asynchronous.AsyncWaitHandle)
        {
            completion.WaitOne();
        }

        ArchiveVerificationTaskResult result = new ArchiveVerificationTaskResult();
        result.IsFaulted = task.IsFaulted || task.IsCanceled;
        if (!result.IsFaulted)
        {
            result.Listing = task.Result;
        }

        return result;
    }

    internal static bool HasArchiveExtension(string path)
    {
        return path.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".gz", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tgz", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tar", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".bz2", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".bzip2", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".7z", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".xz", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".rar", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".zst", global::System.StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".zstd", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static void VerifyExtractedArtifacts(string root, DatasetManifestFile file, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        DatasetManifestStage? extraction = FindOptionalStage(file, "extract");
        if (extraction == null)
        {
            checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ExtractedArtifacts, "MIRROR_EXTRACTION_NOT_TRACKED", null));
            return;
        }

        if (extraction.State != MirrorStageState.Complete)
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ExtractedArtifacts, "MIRROR_EXTRACTION_INCOMPLETE", "The manifest extraction stage has not completed successfully.", null));
            return;
        }

        bool valid = true;
        int index;
        for (index = 0; index < file.ExtractedPaths.Length; index += 1)
        {
            string? path = ResolveVerificationPath(root, file.ExtractedPaths[index]);
            if (path == null || !global::System.IO.File.Exists(path) && !global::System.IO.Directory.Exists(path))
            {
                checks.Add(CreateFailedCheck(MirrorVerificationCheck.ExtractedArtifacts, "MIRROR_EXTRACTED_ARTIFACT_MISSING", "A manifested extracted artifact does not exist.", path ?? file.ExtractedPaths[index]));
                valid = false;
            }
        }

        if (valid)
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.ExtractedArtifacts, "MIRROR_EXTRACTED_ARTIFACTS_PRESENT", null));
        }
    }

    internal static void VerifyParserStage(DatasetManifestFile file, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        DatasetManifestStage? parse = FindOptionalStage(file, "parse");
        if (parse == null)
        {
            checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ParserState, "MIRROR_PARSE_NOT_TRACKED", null));
        }
        else if (parse.State == MirrorStageState.Complete)
        {
            checks.Add(CreatePassedCheck(MirrorVerificationCheck.ParserState, "MIRROR_PARSE_COMPLETE", null));
        }
        else
        {
            checks.Add(CreateFailedCheck(MirrorVerificationCheck.ParserState, "MIRROR_PARSE_INCOMPLETE", "The manifest parser stage has not completed successfully.", null));
        }
    }

    internal static DatasetManifestStage? FindOptionalStage(DatasetManifestFile file, string name)
    {
        int index;
        for (index = 0; index < file.Stages.Length; index += 1)
        {
            DatasetManifestStage stage = file.Stages[index];
            if (stage != null && global::System.String.Equals(stage.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return stage;
            }
        }

        return null;
    }

    internal static void AddUnevaluatedFileChecks(global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks, MirrorVerificationRequest request)
    {
        checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.FileSize, "MIRROR_FILE_MISSING", null));
        checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.Checksum, request.VerifyChecksums ? "MIRROR_FILE_MISSING" : "MIRROR_CHECKSUM_DISABLED", null));
        checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ArchiveReadability, request.VerifyArchives ? "MIRROR_FILE_MISSING" : "MIRROR_ARCHIVE_DISABLED", null));
        checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ExtractedArtifacts, "MIRROR_FILE_MISSING", null));
        checks.Add(CreateUnevaluatedCheck(MirrorVerificationCheck.ParserState, "MIRROR_FILE_MISSING", null));
    }

    internal static bool HasFailedCheck(global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks)
    {
        int index;
        for (index = 0; index < checks.Count; index += 1)
        {
            if (checks[index].WasEvaluated && !checks[index].IsValid)
            {
                return true;
            }
        }

        return false;
    }

    internal static MirrorFileVerificationResult CreateFileResult(DatasetManifestProduct product, DatasetManifestFile file, global::System.Collections.Generic.List<MirrorVerificationCheckResult> checks, MirrorAction action)
    {
        MirrorFileVerificationResult result = new MirrorFileVerificationResult();
        result.ProductIdentifier = file.ProductIdentifier ?? product.ProductIdentifier;
        result.FileName = file.FileName;
        result.Action = action;
        result.Checks = checks.ToArray();
        result.IsValid = action == MirrorAction.Unchanged;
        return result;
    }

    internal static MirrorVerificationCheckResult CreatePassedCheck(MirrorVerificationCheck check, string code, string? path)
    {
        MirrorVerificationCheckResult result = new MirrorVerificationCheckResult();
        result.Check = check;
        result.WasEvaluated = true;
        result.IsValid = true;
        result.Code = code;
        result.Path = path;
        return result;
    }

    internal static MirrorVerificationCheckResult CreateFailedCheck(MirrorVerificationCheck check, string code, string message, string? path)
    {
        MirrorVerificationCheckResult result = new MirrorVerificationCheckResult();
        result.Check = check;
        result.WasEvaluated = true;
        result.IsValid = false;
        result.Code = code;
        result.Message = message;
        result.Path = path;
        return result;
    }

    internal static MirrorVerificationCheckResult CreateUnevaluatedCheck(MirrorVerificationCheck check, string code, string? path)
    {
        MirrorVerificationCheckResult result = new MirrorVerificationCheckResult();
        result.Check = check;
        result.Code = code;
        result.Path = path;
        return result;
    }

    internal static MirrorVerificationResult CreateVerificationResult(global::System.Collections.Generic.List<MirrorVerificationCheckResult> manifestChecks, global::System.Collections.Generic.List<MirrorFileVerificationResult> files, bool manifestValid)
    {
        MirrorVerificationResult result = new MirrorVerificationResult();
        result.ManifestChecks = manifestChecks.ToArray();
        result.Files = files.ToArray();
        result.IsValid = manifestValid;
        int index;
        for (index = 0; index < result.Files.Length; index += 1)
        {
            if (result.Files[index].Action == MirrorAction.Unchanged)
            {
                result.ValidFileCount += 1;
            }
            else if (result.Files[index].Action == MirrorAction.Missing)
            {
                result.MissingFileCount += 1;
            }
            else
            {
                result.CorruptFileCount += 1;
            }

            if (!result.Files[index].IsValid)
            {
                result.IsValid = false;
            }
        }

        return result;
    }
}
