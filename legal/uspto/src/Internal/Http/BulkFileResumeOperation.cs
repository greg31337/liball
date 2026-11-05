namespace uspto;

internal class BulkRangeValidation
{
    internal bool IsValid = false;
    internal long TotalBytes = -1L;
    internal long RemainingBytes = 0L;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal class BulkResumeProgressAdapter : global::System.IProgress<DownloadProgress>
{
    internal global::System.IProgress<DownloadProgress>? Target = null;
    internal long ResumedBytes = 0L;
    internal long TotalBytes = 0L;
    internal string? FileName = null;

    internal BulkResumeProgressAdapter(global::System.IProgress<DownloadProgress>? target, long resumedBytes, long totalBytes, string? fileName)
    {
        Target = target;
        ResumedBytes = resumedBytes;
        TotalBytes = totalBytes;
        FileName = fileName;
    }

    public void Report(DownloadProgress value)
    {
        if (Target == null)
        {
            return;
        }

        DownloadProgress progress = new DownloadProgress();
        progress.State = value.State;
        progress.BytesTransferred = ResumedBytes + value.BytesTransferred;
        progress.ResumedBytes = ResumedBytes;
        progress.TotalBytes = TotalBytes;
        progress.HasTotalBytes = true;
        progress.FileName = FileName;
        progress.Timestamp = value.Timestamp;
        progress.HasTimestamp = value.HasTimestamp;
        Target.Report(progress);
    }
}

internal class BulkFileResumeOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;
    internal BulkFileDownloadOperation DownloadOperation;
    internal RetryEngine RetryEngine;
    internal RateLimiter RateLimiter;

    internal BulkFileResumeOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options, DownloadHostPolicy hostPolicy)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        if (hostPolicy == null)
        {
            throw new ValidationException("A download host policy is required.", "hostPolicy", "REQUIRED", null);
        }

        Client = client;
        Options = options;
        HostPolicy = hostPolicy;
        DownloadOperation = new BulkFileDownloadOperation(client, options, hostPolicy);
        RetryEngine = RetryEngine.CreateDefault(options.Retry);
        RateLimiter = new RateLimiter(options.RateLimits);
    }

    internal DownloadResult Resume(DownloadRequest download, DownloadResumeState resumeState)
    {
        ValidateResumeRequest(download, resumeState);
        if (download.Context.CancellationToken.IsCancellationRequested)
        {
            return CreateResumeResult(download, resumeState, DownloadState.Cancelled, new HttpResponseMetadata(), null, download.CatalogFileName, resumeState.CompletedBytes, resumeState.CompletedBytes, ResolveExpectedTotal(download, resumeState), "CANCELLED", "The bulk download resume was cancelled.");
        }

        DownloadResult? destinationFailure = DownloadOperation.ValidateDestinationAvailability(download);
        if (destinationFailure != null)
        {
            destinationFailure.ResumeState = resumeState;
            destinationFailure.HasResumeState = true;
            return destinationFailure;
        }

        global::System.Uri sourceUri = download.SourceUri ?? new global::System.Uri("https://invalid.example/");
        DownloadHostValidation initialHost = HostPolicy.Validate(sourceUri, Options, download.Context.CancellationToken);
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(download.Context);
        context.Service = ApiService.Odp;
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, true);
        bool attemptRange = download.ResumePartial && Options.Download.AllowResume && resumeState.CompletedBytes > 0L && HasResumeValidator(resumeState);
        global::System.Net.Http.HttpRequestMessage initialRequest = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, sourceUri);
        DownloadOperation.PrepareHeaders(initialRequest, initialHost);
        if (attemptRange)
        {
            ApplyResumeHeaders(initialRequest, resumeState);
        }

        RedirectState? redirectState = null;
        if (global::System.String.Equals(sourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            redirectState = new RedirectState(ApiService.Odp, sourceUri, Options.Download.RedirectHopCount);
        }

        using (BulkDownloadResponseLease lease = new BulkDownloadResponseLease(initialRequest))
        {
            RateLimiter.Acquire(ApiService.Odp, context.CancellationToken);
            lease.SendResponse(Client, timeout);
            DownloadOperation.FollowRedirects(lease, redirectState, context, timeout);
            if (lease.Response == null)
            {
                throw new ValidationException("The bulk resume response is required.", "Response", "REQUIRED", null);
            }

            RateLimiter.ObserveResponse(ApiService.Odp, lease.Response);

            if (!attemptRange)
            {
                return TransferRestart(download, resumeState, lease.Response, context, timeout);
            }

            int statusCode = (int)lease.Response.StatusCode;
            if (statusCode == 206)
            {
                return TransferPartial(download, resumeState, lease, sourceUri, context, timeout);
            }

            if (statusCode == 200)
            {
                return TransferRestart(download, resumeState, lease.Response, context, timeout);
            }

            if (statusCode == 416 && IsCompleteRangeResponse(download, resumeState, lease.Response))
            {
                return CommitExistingPartial(download, resumeState, lease.Response);
            }

            if (statusCode == 416)
            {
                RestartResponse(lease, sourceUri, context, timeout);
                if (lease.Response == null)
                {
                    throw new ValidationException("The bulk restart response is required.", "Response", "REQUIRED", null);
                }

                return TransferRestart(download, resumeState, lease.Response, context, timeout);
            }

            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(lease.Response, DownloadOperation.CredentialSecrets());
            string code = "HTTP_STATUS_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
            return CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, lease.Response, download.CatalogFileName, resumeState.CompletedBytes, resumeState.CompletedBytes, ResolveExpectedTotal(download, resumeState), code, "The bulk file service returned a non-success status while resuming.");
        }
    }

    internal DownloadResult TransferPartial(DownloadRequest download, DownloadResumeState resumeState, BulkDownloadResponseLease lease, global::System.Uri sourceUri, RequestContext context, HttpTimeoutScope timeout)
    {
        string[] secrets = DownloadOperation.CredentialSecrets();
        long originalResumedBytes = resumeState.CompletedBytes;
        long completedBytes = resumeState.CompletedBytes;
        long totalBytes = ResolveExpectedTotal(download, resumeState);
        string partialPath = resumeState.PartialPath ?? string.Empty;
        RetryExecutionState retryState = new RetryExecutionState();
        while (true)
        {
            if (lease.Response == null)
            {
                throw new ValidationException("The bulk recovery response is required.", "Response", "REQUIRED", null);
            }

            global::System.Net.Http.HttpResponseMessage response = lease.Response;
            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, secrets);
            int statusCode = metadata.StatusCode;
            if (statusCode == 200)
            {
                return TransferRestart(download, resumeState, response, context, timeout);
            }

            if (statusCode == 416 && IsCompleteRangeResponse(download, resumeState, response))
            {
                return CommitExistingPartial(download, resumeState, response);
            }

            if (statusCode != 206)
            {
                string statusFailure = "HTTP_STATUS_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
                return CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, response, download.CatalogFileName, completedBytes, originalResumedBytes, totalBytes, statusFailure, "The bulk recovery request did not return a partial response.");
            }

            string fileName = BulkFileNameSelector.SelectBulkFileName(download.CatalogFileName, response);
            DownloadResumeState boundaryState = CreateBoundaryState(resumeState, completedBytes, totalBytes);
            BulkRangeValidation validation = ValidatePartialResponse(download, boundaryState, response, metadata);
            if (!validation.IsValid)
            {
                DownloadResumeState invalidState = PartialDownloadMetadataStore.Save(boundaryState, global::System.DateTimeOffset.UtcNow, secrets);
                return CreateResumeResult(download, invalidState, DownloadState.Failed, metadata, response, fileName, completedBytes, originalResumedBytes, validation.TotalBytes, validation.FailureCode, validation.FailureMessage);
            }

            totalBytes = validation.TotalBytes;
            ReportResumeProgress(download, DownloadState.Downloading, completedBytes, originalResumedBytes, totalBytes, fileName);
            RecoverableBulkBodyResult copied;
            using (global::System.IO.FileStream output = new global::System.IO.FileStream(partialPath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, Options.Download.BufferSize, global::System.IO.FileOptions.SequentialScan))
            {
                output.SetLength(completedBytes);
                output.Seek(completedBytes, global::System.IO.SeekOrigin.Begin);
                copied = RecoverableBulkBodyCopier.CopyRecoverableBody(response, output, context, timeout, Options.Download, download, fileName, completedBytes, originalResumedBytes, totalBytes, validation.RemainingBytes);
                output.Flush(true);
            }

            completedBytes += copied.BytesCopied;
            DownloadResumeState persisted = PersistRecoveryState(download, boundaryState, response, metadata, completedBytes, totalBytes, secrets);
            if (copied.FailureCode != null)
            {
                return CreateResumeResult(download, persisted, DownloadState.Failed, metadata, response, fileName, completedBytes, originalResumedBytes, totalBytes, copied.FailureCode, copied.FailureMessage);
            }

            if (copied.Failure == null)
            {
                return CommitResumedPartial(download, persisted, response, metadata, fileName, completedBytes, originalResumedBytes, totalBytes);
            }

            if (lease.Request == null || !RetryEngine.TryWaitForRetry(retryState, lease.Request, false, copied.Failure, context.CancellationToken))
            {
                return CreateResumeResult(download, persisted, DownloadState.Failed, metadata, response, fileName, completedBytes, originalResumedBytes, totalBytes, "MID_STREAM_RECOVERY_EXHAUSTED", "The bulk response stream ended before recovery could complete it.");
            }

            ReopenAtBoundary(lease, sourceUri, persisted, context, timeout);
            resumeState = persisted;
        }
    }

    internal void ReopenAtBoundary(BulkDownloadResponseLease lease, global::System.Uri sourceUri, DownloadResumeState resumeState, RequestContext context, HttpTimeoutScope timeout)
    {
        RateLimiter.Acquire(ApiService.Odp, context.CancellationToken);
        DownloadHostValidation host = HostPolicy.Validate(sourceUri, Options, context.CancellationToken);
        global::System.Net.Http.HttpRequestMessage request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, sourceUri);
        DownloadOperation.PrepareHeaders(request, host);
        ApplyResumeHeaders(request, resumeState);
        lease.ReplaceRequest(request);
        lease.SendResponse(Client, timeout);
        RedirectState? redirectState = null;
        if (global::System.String.Equals(sourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            redirectState = new RedirectState(ApiService.Odp, sourceUri, Options.Download.RedirectHopCount);
        }

        DownloadOperation.FollowRedirects(lease, redirectState, context, timeout);
        if (lease.Response != null)
        {
            RateLimiter.ObserveResponse(ApiService.Odp, lease.Response);
        }
    }

    internal DownloadResult TransferRestart(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response, RequestContext context, HttpTimeoutScope timeout)
    {
        string[] secrets = DownloadOperation.CredentialSecrets();
        HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, secrets);
        int statusCode = metadata.StatusCode;
        if (statusCode != 200)
        {
            string code = "HTTP_STATUS_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
            return CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, response, download.CatalogFileName, resumeState.CompletedBytes, resumeState.CompletedBytes, ResolveExpectedTotal(download, resumeState), code, "The bulk file service did not return a complete restart response.");
        }

        string fileName = BulkFileNameSelector.SelectBulkFileName(download.CatalogFileName, response);
        long expectedTotal = ResolveExpectedTotal(download, resumeState);
        bool hasExpectedTotal = download.HasExpectedSize || resumeState.HasExpectedTotalBytes;
        if (metadata.HasContentLength && Options.Download.HasMaximumBytes && metadata.ContentLength > Options.Download.MaximumBytes)
        {
            return CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, response, fileName, resumeState.CompletedBytes, resumeState.CompletedBytes, expectedTotal, "PAYLOAD_TOO_LARGE", "The restarted bulk file exceeds the configured download limit.");
        }

        if (metadata.HasContentLength && hasExpectedTotal && metadata.ContentLength != expectedTotal)
        {
            return CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, response, fileName, resumeState.CompletedBytes, resumeState.CompletedBytes, expectedTotal, "RESPONSE_LENGTH_MISMATCH", "The restarted bulk file length does not match its expected size.");
        }

        if (!hasExpectedTotal && metadata.HasContentLength)
        {
            expectedTotal = metadata.ContentLength;
            hasExpectedTotal = true;
        }

        ReportResumeProgress(download, DownloadState.Downloading, 0L, 0L, expectedTotal, fileName);
        DownloadRequest transferRequest = CreateRestartTransferRequest(download, expectedTotal, hasExpectedTotal);
        StreamedHttpResponse streamed;
        string partialPath = resumeState.PartialPath ?? string.Empty;
        using (global::System.IO.FileStream output = new global::System.IO.FileStream(partialPath, global::System.IO.FileMode.Create, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, Options.Download.BufferSize, global::System.IO.FileOptions.SequentialScan))
        {
            streamed = TimeoutController.CopyBoundedBinaryBodyPhase(response, output, context, secrets, timeout, Options.Download, transferRequest, fileName);
            output.Flush(true);
        }

        if (streamed.FailureCode != null)
        {
            DownloadResumeState persisted = PersistRestartState(download, resumeState, response, metadata, streamed.BytesCopied, expectedTotal, hasExpectedTotal, secrets);
            return CreateResumeResult(download, persisted, DownloadState.Failed, metadata, response, fileName, streamed.BytesCopied, 0L, expectedTotal, streamed.FailureCode, streamed.FailureMessage);
        }

        return CommitResumedPartial(download, resumeState, response, metadata, fileName, streamed.BytesCopied, 0L, expectedTotal);
    }

    internal BulkRangeValidation ValidatePartialResponse(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response, HttpResponseMetadata metadata)
    {
        BulkRangeValidation result = new BulkRangeValidation();
        global::System.Net.Http.Headers.ContentRangeHeaderValue? range = response.Content.Headers.ContentRange;
        if (range == null || !global::System.String.Equals(range.Unit, "bytes", global::System.StringComparison.OrdinalIgnoreCase) || !range.From.HasValue || !range.To.HasValue || !range.Length.HasValue)
        {
            result.FailureCode = "INVALID_CONTENT_RANGE";
            result.FailureMessage = "The partial bulk response did not contain a complete byte range.";
            return result;
        }

        long from = range.From.GetValueOrDefault();
        long to = range.To.GetValueOrDefault();
        long total = range.Length.GetValueOrDefault();
        result.TotalBytes = total;
        if (from != resumeState.CompletedBytes || to < from || total <= to || to != total - 1L)
        {
            result.FailureCode = "INVALID_CONTENT_RANGE";
            result.FailureMessage = "The partial bulk response range does not begin at the verified byte boundary.";
            return result;
        }

        long segmentLength = to - from + 1L;
        if (metadata.HasContentLength && metadata.ContentLength != segmentLength)
        {
            result.FailureCode = "RANGE_LENGTH_MISMATCH";
            result.FailureMessage = "The partial bulk response length does not match its content range.";
            return result;
        }

        if (download.HasExpectedSize && download.ExpectedSize != total || resumeState.HasExpectedTotalBytes && resumeState.ExpectedTotalBytes != total)
        {
            result.FailureCode = "RANGE_TOTAL_MISMATCH";
            result.FailureMessage = "The partial bulk response total does not match the expected file size.";
            return result;
        }

        if (Options.Download.HasMaximumBytes && total > Options.Download.MaximumBytes)
        {
            result.FailureCode = "PAYLOAD_TOO_LARGE";
            result.FailureMessage = "The partial bulk response exceeds the configured download limit.";
            return result;
        }

        if (!IsValidatorUnchanged(resumeState, response))
        {
            result.FailureCode = "RESUME_VALIDATOR_CHANGED";
            result.FailureMessage = "The partial bulk response validator changed.";
            return result;
        }

        result.RemainingBytes = segmentLength;
        result.IsValid = true;
        return result;
    }

    internal static bool IsValidatorUnchanged(DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response)
    {
        bool strongEntityTag = !global::System.String.IsNullOrWhiteSpace(resumeState.ETag) && !resumeState.ETag.StartsWith("W/", global::System.StringComparison.OrdinalIgnoreCase);
        if (strongEntityTag && response.Headers.ETag != null && !global::System.String.Equals(resumeState.ETag, response.Headers.ETag.ToString(), global::System.StringComparison.Ordinal))
        {
            return false;
        }

        if (!strongEntityTag && resumeState.HasLastModified && response.Content.Headers.LastModified.HasValue && resumeState.LastModified.ToUniversalTime() != response.Content.Headers.LastModified.GetValueOrDefault().ToUniversalTime())
        {
            return false;
        }

        return true;
    }

    internal static bool IsCompleteRangeResponse(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response)
    {
        global::System.Net.Http.Headers.ContentRangeHeaderValue? range = response.Content.Headers.ContentRange;
        if (range == null || !global::System.String.Equals(range.Unit, "bytes", global::System.StringComparison.OrdinalIgnoreCase) || !range.Length.HasValue || range.From.HasValue || range.To.HasValue)
        {
            return false;
        }

        long total = range.Length.GetValueOrDefault();
        if (total != resumeState.CompletedBytes || download.HasExpectedSize && download.ExpectedSize != total || resumeState.HasExpectedTotalBytes && resumeState.ExpectedTotalBytes != total)
        {
            return false;
        }

        return IsValidatorUnchanged(resumeState, response);
    }

    internal void RestartResponse(BulkDownloadResponseLease lease, global::System.Uri sourceUri, RequestContext context, HttpTimeoutScope timeout)
    {
        RateLimiter.Acquire(ApiService.Odp, context.CancellationToken);
        DownloadHostValidation host = HostPolicy.Validate(sourceUri, Options, context.CancellationToken);
        global::System.Net.Http.HttpRequestMessage request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, sourceUri);
        DownloadOperation.PrepareHeaders(request, host);
        lease.ReplaceRequest(request);
        lease.SendResponse(Client, timeout);
        RedirectState? redirectState = null;
        if (global::System.String.Equals(sourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            redirectState = new RedirectState(ApiService.Odp, sourceUri, Options.Download.RedirectHopCount);
        }

        DownloadOperation.FollowRedirects(lease, redirectState, context, timeout);
        if (lease.Response != null)
        {
            RateLimiter.ObserveResponse(ApiService.Odp, lease.Response);
        }
    }

    internal DownloadResult CommitExistingPartial(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response)
    {
        string[] secrets = DownloadOperation.CredentialSecrets();
        HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, secrets);
        string fileName = BulkFileNameSelector.SelectBulkFileName(download.CatalogFileName, response);
        long total = resumeState.CompletedBytes;
        string partialPath = resumeState.PartialPath ?? string.Empty;
        using (global::System.IO.FileStream output = new global::System.IO.FileStream(partialPath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None))
        {
            output.SetLength(total);
            output.Flush(true);
        }

        return CommitResumedPartial(download, resumeState, response, metadata, fileName, total, total, total);
    }

    internal DownloadResult CommitResumedPartial(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response, HttpResponseMetadata metadata, string fileName, long completedBytes, long resumedBytes, long totalBytes)
    {
        string partialPath = global::System.IO.Path.GetFullPath(resumeState.PartialPath ?? string.Empty);
        string destinationPath = global::System.IO.Path.GetFullPath(download.DestinationPath ?? string.Empty);
        ChecksumResult[] checksums = BulkChecksumVerifier.VerifyFile(partialPath, download.ExpectedChecksums, Options.Download.BufferSize, download.Context.CancellationToken);
        if (BulkChecksumVerifier.HasMismatch(checksums))
        {
            DownloadResult failure = CreateResumeResult(download, resumeState, DownloadState.Failed, metadata, response, fileName, completedBytes, resumedBytes, totalBytes, "CHECKSUM_MISMATCH", "The completed bulk download did not match its expected checksum.");
            failure.Checksums = checksums;
            return failure;
        }

        bool overwrite = download.OverwriteExisting || Options.Download.OverwriteCompletedFiles;
        global::System.IO.File.Move(partialPath, destinationPath, overwrite);
        PartialDownloadMetadataStore.Delete(partialPath);
        DownloadResult result = CreateResumeResult(download, resumeState, DownloadState.Completed, metadata, response, fileName, completedBytes, resumedBytes, totalBytes, null, null);
        result.Checksums = checksums;
        result.FinalPath = destinationPath;
        result.HasResumeState = false;
        result.ResumeState = new DownloadResumeState();
        return result;
    }

    internal static DownloadResumeState CreateBoundaryState(DownloadResumeState resumeState, long completedBytes, long totalBytes)
    {
        DownloadResumeState state = new DownloadResumeState();
        state.SourceUri = resumeState.SourceUri;
        state.PartialPath = resumeState.PartialPath;
        state.CompletedBytes = completedBytes;
        state.ExpectedTotalBytes = totalBytes >= 0L ? totalBytes : 0L;
        state.HasExpectedTotalBytes = totalBytes >= 0L;
        state.ETag = resumeState.ETag;
        state.LastModified = resumeState.LastModified;
        state.HasLastModified = resumeState.HasLastModified;
        state.UpdatedAt = resumeState.UpdatedAt;
        state.HasUpdatedAt = resumeState.HasUpdatedAt;
        return state;
    }

    internal DownloadResumeState PersistRecoveryState(DownloadRequest download, DownloadResumeState boundaryState, global::System.Net.Http.HttpResponseMessage response, HttpResponseMetadata metadata, long completedBytes, long totalBytes, string[] secrets)
    {
        DownloadResumeState state = CreateBoundaryState(boundaryState, completedBytes, totalBytes);
        state.SourceUri = metadata.RequestUri ?? download.SourceUri;
        if (response.Headers.ETag != null)
        {
            state.ETag = response.Headers.ETag.ToString();
        }

        if (response.Content.Headers.LastModified.HasValue)
        {
            state.LastModified = response.Content.Headers.LastModified.GetValueOrDefault();
            state.HasLastModified = true;
        }

        return PartialDownloadMetadataStore.Save(state, global::System.DateTimeOffset.UtcNow, secrets);
    }

    internal DownloadResumeState PersistRestartState(DownloadRequest download, DownloadResumeState resumeState, global::System.Net.Http.HttpResponseMessage response, HttpResponseMetadata metadata, long completedBytes, long expectedTotal, bool hasExpectedTotal, string[] secrets)
    {
        DownloadResumeState state = new DownloadResumeState();
        state.SourceUri = metadata.RequestUri ?? download.SourceUri;
        state.PartialPath = resumeState.PartialPath;
        state.CompletedBytes = completedBytes;
        state.ExpectedTotalBytes = hasExpectedTotal ? expectedTotal : 0L;
        state.HasExpectedTotalBytes = hasExpectedTotal;
        if (response.Headers.ETag != null)
        {
            state.ETag = response.Headers.ETag.ToString();
        }

        if (response.Content.Headers.LastModified.HasValue)
        {
            state.LastModified = response.Content.Headers.LastModified.GetValueOrDefault();
            state.HasLastModified = true;
        }

        return PartialDownloadMetadataStore.Save(state, global::System.DateTimeOffset.UtcNow, secrets);
    }

    internal DownloadResult CreateResumeResult(DownloadRequest download, DownloadResumeState resumeState, DownloadState state, HttpResponseMetadata metadata, global::System.Net.Http.HttpResponseMessage? response, string? fileName, long bytesTransferred, long resumedBytes, long totalBytes, string? failureCode, string? failureMessage)
    {
        DownloadResult result = new DownloadResult();
        result.Request = download;
        result.State = state;
        result.ResumeState = resumeState;
        result.HasResumeState = state != DownloadState.Completed;
        result.FailureCode = failureCode;
        result.FailureMessage = failureMessage;
        result.Content.FinalUri = metadata.RequestUri;
        result.Content.FileName = fileName;
        result.Content.MediaType = metadata.MediaType;
        result.Content.Format = metadata.Format;
        result.Content.ContentLength = metadata.ContentLength;
        result.Content.HasContentLength = metadata.HasContentLength;
        if (response != null)
        {
            PatentDocumentDownloadOperation.PopulateResponseValidators(result.Content, response);
        }

        result.Progress = CreateResumeProgress(state, bytesTransferred, resumedBytes, totalBytes, fileName);
        if (download.Progress != null)
        {
            download.Progress.Report(result.Progress);
        }

        return result;
    }

    internal static DownloadProgress CreateResumeProgress(DownloadState state, long bytesTransferred, long resumedBytes, long totalBytes, string? fileName)
    {
        DownloadProgress progress = new DownloadProgress();
        progress.State = state;
        progress.BytesTransferred = bytesTransferred;
        progress.ResumedBytes = resumedBytes;
        progress.TotalBytes = totalBytes;
        progress.HasTotalBytes = totalBytes >= 0L;
        progress.FileName = fileName;
        progress.Timestamp = global::System.DateTimeOffset.UtcNow;
        progress.HasTimestamp = true;
        return progress;
    }

    internal static void ReportResumeProgress(DownloadRequest download, DownloadState state, long bytesTransferred, long resumedBytes, long totalBytes, string? fileName)
    {
        if (download.Progress != null)
        {
            download.Progress.Report(CreateResumeProgress(state, bytesTransferred, resumedBytes, totalBytes, fileName));
        }
    }

    internal static DownloadRequest CreateRangeTransferRequest(DownloadRequest download, long remainingBytes, long resumedBytes, long totalBytes, string fileName)
    {
        DownloadRequest request = new DownloadRequest();
        request.ExpectedSize = remainingBytes;
        request.HasExpectedSize = true;
        request.Context = download.Context;
        request.Progress = new BulkResumeProgressAdapter(download.Progress, resumedBytes, totalBytes, fileName);
        return request;
    }

    internal static DownloadRequest CreateRestartTransferRequest(DownloadRequest download, long expectedTotal, bool hasExpectedTotal)
    {
        DownloadRequest request = new DownloadRequest();
        request.ExpectedSize = hasExpectedTotal ? expectedTotal : 0L;
        request.HasExpectedSize = hasExpectedTotal;
        request.Context = download.Context;
        request.Progress = download.Progress;
        return request;
    }

    internal static void ApplyResumeHeaders(global::System.Net.Http.HttpRequestMessage request, DownloadResumeState resumeState)
    {
        request.Headers.Range = new global::System.Net.Http.Headers.RangeHeaderValue(resumeState.CompletedBytes, null);
        if (!global::System.String.IsNullOrWhiteSpace(resumeState.ETag) && !resumeState.ETag.StartsWith("W/", global::System.StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("If-Range", resumeState.ETag);
        }
        else
        {
            request.Headers.TryAddWithoutValidation("If-Range", resumeState.LastModified.ToUniversalTime().ToString("R", global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    internal static bool HasResumeValidator(DownloadResumeState resumeState)
    {
        bool strongEntityTag = !global::System.String.IsNullOrWhiteSpace(resumeState.ETag) && !resumeState.ETag.StartsWith("W/", global::System.StringComparison.OrdinalIgnoreCase);
        return strongEntityTag || resumeState.HasLastModified;
    }

    internal static long ResolveExpectedTotal(DownloadRequest download, DownloadResumeState resumeState)
    {
        if (download.HasExpectedSize)
        {
            return download.ExpectedSize;
        }

        if (resumeState.HasExpectedTotalBytes)
        {
            return resumeState.ExpectedTotalBytes;
        }

        return -1L;
    }

    internal static bool IsSameResumeSource(global::System.Uri requestUri, global::System.Uri resumeUri)
    {
        return global::System.String.Equals(requestUri.Scheme, resumeUri.Scheme, global::System.StringComparison.OrdinalIgnoreCase)
            && global::System.String.Equals(requestUri.IdnHost, resumeUri.IdnHost, global::System.StringComparison.OrdinalIgnoreCase)
            && requestUri.Port == resumeUri.Port
            && global::System.String.Equals(requestUri.AbsolutePath, resumeUri.AbsolutePath, global::System.StringComparison.Ordinal);
    }

    internal void ValidateResumeRequest(DownloadRequest download, DownloadResumeState resumeState)
    {
        DownloadOperation.ValidateRequest(download);
        if (download.Output != null || global::System.String.IsNullOrWhiteSpace(download.DestinationPath))
        {
            throw new ValidationException("A bulk resume requires a completed destination path.", "DestinationPath", "FILE_DESTINATION_REQUIRED", null);
        }

        if (resumeState == null || resumeState.SourceUri == null || global::System.String.IsNullOrWhiteSpace(resumeState.PartialPath))
        {
            throw new ValidationException("Valid partial download metadata is required.", "resumeState", "RESUME_STATE_REQUIRED", null);
        }

        string partialPath = global::System.IO.Path.GetFullPath(resumeState.PartialPath);
        string destinationPath = global::System.IO.Path.GetFullPath(download.DestinationPath ?? string.Empty);
        if (global::System.String.Equals(partialPath, destinationPath, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The partial and completed download paths must differ.", "PartialPath", "PARTIAL_PATH_CONFLICT", null);
        }

        if (!global::System.IO.File.Exists(partialPath))
        {
            throw new ValidationException("The partial download file does not exist.", "PartialPath", "PARTIAL_FILE_NOT_FOUND", null);
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(partialPath);
        if (resumeState.CompletedBytes < 0L || information.Length < resumeState.CompletedBytes)
        {
            throw new ValidationException("The verified partial length is invalid.", "CompletedBytes", "INVALID_PARTIAL_LENGTH", null);
        }

        if (resumeState.HasExpectedTotalBytes && (resumeState.ExpectedTotalBytes < resumeState.CompletedBytes || download.HasExpectedSize && download.ExpectedSize != resumeState.ExpectedTotalBytes))
        {
            throw new ValidationException("The resume size expectations are inconsistent.", "ExpectedTotalBytes", "RESUME_SIZE_MISMATCH", null);
        }

        global::System.Uri requestUri = download.SourceUri ?? new global::System.Uri("https://invalid.example/");
        if (!IsSameResumeSource(requestUri, resumeState.SourceUri))
        {
            throw new ValidationException("The resume source does not match the requested download.", "SourceUri", "RESUME_SOURCE_MISMATCH", null);
        }
    }
}
