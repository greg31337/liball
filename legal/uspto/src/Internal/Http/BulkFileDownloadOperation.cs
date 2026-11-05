namespace uspto;

internal class BulkDownloadResponseLease : global::System.IDisposable
{
    internal global::System.Net.Http.HttpRequestMessage? Request = null;
    internal global::System.Net.Http.HttpResponseMessage? Response = null;

    internal BulkDownloadResponseLease(global::System.Net.Http.HttpRequestMessage request)
    {
        Request = request;
    }

    internal void SendResponse(global::System.Net.Http.HttpClient client, HttpTimeoutScope timeout)
    {
        if (Request == null)
        {
            throw new ValidationException("A bulk download request is required.", "Request", "REQUIRED", null);
        }

        Response = TimeoutController.SendHeaderPhase(client, Request, timeout);
    }

    internal void ReplaceRequest(global::System.Net.Http.HttpRequestMessage request)
    {
        if (Response != null)
        {
            Response.Dispose();
            Response = null;
        }

        if (Request != null)
        {
            Request.Dispose();
        }

        Request = request;
    }

    public void Dispose()
    {
        if (Response != null)
        {
            Response.Dispose();
            Response = null;
        }

        if (Request != null)
        {
            Request.Dispose();
            Request = null;
        }
    }
}

internal class AtomicBulkDownloadFile : global::System.IDisposable
{
    internal string DestinationPath;
    internal string TemporaryPath;
    internal global::System.IO.FileStream? Output = null;
    internal bool IsCommitted = false;

    internal AtomicBulkDownloadFile(string destinationPath, int bufferSize)
    {
        DestinationPath = global::System.IO.Path.GetFullPath(destinationPath);
        TemporaryPath = DestinationPath + "." + global::System.Guid.NewGuid().ToString("N") + ".partial";
        Output = new global::System.IO.FileStream(TemporaryPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, bufferSize, global::System.IO.FileOptions.SequentialScan);
    }

    internal void Commit(bool overwrite)
    {
        if (Output == null)
        {
            throw new ValidationException("The bulk download temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
        }

        Output.Flush(true);
        Output.Dispose();
        Output = null;
        global::System.IO.File.Move(TemporaryPath, DestinationPath, overwrite);
        IsCommitted = true;
    }

    public void Dispose()
    {
        if (Output != null)
        {
            Output.Dispose();
            Output = null;
        }

        if (!IsCommitted && global::System.IO.File.Exists(TemporaryPath))
        {
            global::System.IO.File.Delete(TemporaryPath);
        }
    }
}

internal class BulkFileDownloadOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal BulkFileDownloadOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options, DownloadHostPolicy hostPolicy)
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
    }

    internal DownloadResult Download(DownloadRequest download)
    {
        ValidateRequest(download);
        if (download.Context.CancellationToken.IsCancellationRequested)
        {
            return CreateTerminalResult(download, DownloadState.Cancelled, new HttpResponseMetadata(), null, download.CatalogFileName, 0L, "CANCELLED", "The bulk download was cancelled.");
        }

        DownloadResult? destinationFailure = ValidateDestinationAvailability(download);
        if (destinationFailure != null)
        {
            return destinationFailure;
        }

        global::System.Uri sourceUri = download.SourceUri ?? new global::System.Uri("https://invalid.example/");
        DownloadHostValidation initialHost = HostPolicy.Validate(sourceUri, Options, download.Context.CancellationToken);
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(download.Context);
        context.Service = ApiService.Odp;
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, true);
        global::System.Net.Http.HttpRequestMessage initialRequest = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, sourceUri);
        PrepareHeaders(initialRequest, initialHost);
        RedirectState? redirectState = null;
        if (global::System.String.Equals(sourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            redirectState = new RedirectState(ApiService.Odp, sourceUri, Options.Download.RedirectHopCount);
        }

        using (BulkDownloadResponseLease lease = new BulkDownloadResponseLease(initialRequest))
        {
            lease.SendResponse(Client, timeout);
            FollowRedirects(lease, redirectState, context, timeout);
            if (lease.Response == null)
            {
                throw new ValidationException("The bulk download response is required.", "Response", "REQUIRED", null);
            }

            string[] secrets = CredentialSecrets();
            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(lease.Response, secrets);
            int statusCode = metadata.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                string code = "HTTP_STATUS_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
                return CreateTerminalResult(download, DownloadState.Failed, metadata, lease.Response, download.CatalogFileName, 0L, code, "The bulk file service returned a non-success status.");
            }

            string fileName = BulkFileNameSelector.SelectBulkFileName(download.CatalogFileName, lease.Response);
            DownloadResult? responseFailure = ValidateResponse(download, metadata, lease.Response, fileName);
            if (responseFailure != null)
            {
                return responseFailure;
            }

            ReportProgress(download, DownloadState.Downloading, 0L, metadata, fileName);
            using (BulkChecksumSession checksumSession = new BulkChecksumSession(download.ExpectedChecksums))
            {
                if (download.Output != null)
                {
                    StreamedHttpResponse streamed = TimeoutController.CopyChecksummedBinaryBodyPhase(lease.Response, download.Output, context, secrets, timeout, Options.Download, download, fileName, checksumSession);
                    return CreateChecksummedResult(download, metadata, lease.Response, fileName, streamed, checksumSession);
                }

                string destinationPath = download.DestinationPath ?? string.Empty;
                using (AtomicBulkDownloadFile atomicFile = new AtomicBulkDownloadFile(destinationPath, Options.Download.BufferSize))
                {
                    if (atomicFile.Output == null)
                    {
                        throw new ValidationException("The bulk download temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
                    }

                    StreamedHttpResponse streamed = TimeoutController.CopyChecksummedBinaryBodyPhase(lease.Response, atomicFile.Output, context, secrets, timeout, Options.Download, download, fileName, checksumSession);
                    DownloadResult result = CreateChecksummedResult(download, metadata, lease.Response, fileName, streamed, checksumSession);
                    if (result.State != DownloadState.Completed)
                    {
                        return result;
                    }

                    bool overwrite = download.OverwriteExisting || Options.Download.OverwriteCompletedFiles;
                    atomicFile.Commit(overwrite);
                    result.FinalPath = atomicFile.DestinationPath;
                    return result;
                }
            }
        }
    }

    internal DownloadResult CreateChecksummedResult(DownloadRequest download, HttpResponseMetadata metadata, global::System.Net.Http.HttpResponseMessage response, string fileName, StreamedHttpResponse streamed, BulkChecksumSession checksumSession)
    {
        DownloadResult result;
        if (streamed.FailureCode != null)
        {
            result = CreateTerminalResult(download, DownloadState.Failed, metadata, response, fileName, streamed.BytesCopied, streamed.FailureCode, streamed.FailureMessage);
            result.Checksums = checksumSession.MarkIncomplete();
            return result;
        }

        ChecksumResult[] checksums = checksumSession.Complete();
        if (BulkChecksumVerifier.HasMismatch(checksums))
        {
            result = CreateTerminalResult(download, DownloadState.Failed, metadata, response, fileName, streamed.BytesCopied, "CHECKSUM_MISMATCH", "The completed bulk download did not match its expected checksum.");
            result.Checksums = checksums;
            return result;
        }

        result = CreateTerminalResult(download, DownloadState.Completed, metadata, response, fileName, streamed.BytesCopied, null, null);
        result.Checksums = checksums;
        return result;
    }

    internal void FollowRedirects(BulkDownloadResponseLease lease, RedirectState? redirectState, RequestContext context, HttpTimeoutScope timeout)
    {
        while (lease.Response != null && RedirectPolicy.IsRedirectStatus((int)lease.Response.StatusCode))
        {
            if (redirectState == null || lease.Request == null)
            {
                throw new ApiException("Bulk redirects require an HTTPS source URI.", ApiService.Odp, (int)lease.Response.StatusCode, true, "REDIRECT_HTTPS_REQUIRED", null, null, false, null);
            }

            global::System.Uri target = RedirectPolicy.ResolveRedirectLocation(lease.Request, lease.Response, redirectState);
            DownloadHostValidation targetHost = HostPolicy.Validate(target, Options, context.CancellationToken);
            global::System.Net.Http.HttpRequestMessage nextRequest = RedirectPolicy.CreateRedirectRequest(lease.Request, lease.Response, redirectState, context.CancellationToken);
            RemoveUnapprovedCredentials(nextRequest, targetHost);
            lease.ReplaceRequest(nextRequest);
            lease.SendResponse(Client, timeout);
        }

        if (lease.Response != null && lease.Request != null && lease.Response.RequestMessage != null && lease.Response.RequestMessage.RequestUri != null)
        {
            string sentIdentity = RedirectPolicy.GetRedirectIdentity(lease.Request.RequestUri ?? new global::System.Uri("https://invalid.example/"));
            string responseIdentity = RedirectPolicy.GetRedirectIdentity(lease.Response.RequestMessage.RequestUri);
            if (!global::System.String.Equals(sentIdentity, responseIdentity, global::System.StringComparison.Ordinal))
            {
                throw new ApiException("The HTTP client followed a redirect outside the validated redirect policy.", ApiService.Odp, (int)lease.Response.StatusCode, false, "AUTOMATIC_REDIRECT_REJECTED", null, null, false, null);
            }
        }
    }

    internal void PrepareHeaders(global::System.Net.Http.HttpRequestMessage request, DownloadHostValidation host)
    {
        RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Unknown);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new global::System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
        RemoveUnapprovedCredentials(request, host);
    }

    internal void RemoveUnapprovedCredentials(global::System.Net.Http.HttpRequestMessage request, DownloadHostValidation host)
    {
        string approvedCredentialHost = "api.uspto.gov";
        if (Options.OdpBaseUri != null)
        {
            approvedCredentialHost = Options.OdpBaseUri.IdnHost;
        }

        if (!global::System.String.Equals(host.Host, approvedCredentialHost, global::System.StringComparison.OrdinalIgnoreCase))
        {
            RedirectPolicy.RemoveRequestCredentialHeaders(request.Headers);
            if (request.Content != null)
            {
                RedirectPolicy.RemoveContentCredentialHeaders(request.Content.Headers);
            }
        }
    }

    internal DownloadResult? ValidateResponse(DownloadRequest download, HttpResponseMetadata metadata, global::System.Net.Http.HttpResponseMessage response, string fileName)
    {
        if (metadata.HasContentLength && Options.Download.HasMaximumBytes && metadata.ContentLength > Options.Download.MaximumBytes)
        {
            return CreateTerminalResult(download, DownloadState.Failed, metadata, response, fileName, 0L, "PAYLOAD_TOO_LARGE", "The bulk file exceeds the configured download limit.");
        }

        if (metadata.HasContentLength && download.HasExpectedSize && metadata.ContentLength != download.ExpectedSize)
        {
            return CreateTerminalResult(download, DownloadState.Failed, metadata, response, fileName, 0L, "RESPONSE_LENGTH_MISMATCH", "The bulk file response length does not match its expected size.");
        }

        return null;
    }

    internal DownloadResult? ValidateDestinationAvailability(DownloadRequest download)
    {
        if (download.DestinationPath == null)
        {
            return null;
        }

        string fullPath = global::System.IO.Path.GetFullPath(download.DestinationPath);
        if (global::System.IO.Directory.Exists(fullPath))
        {
            throw new ValidationException("The bulk download destination must be a file path.", "DestinationPath", "FILE_PATH_REQUIRED", null);
        }

        string? directory = global::System.IO.Path.GetDirectoryName(fullPath);
        if (directory == null || !global::System.IO.Directory.Exists(directory))
        {
            throw new ValidationException("The bulk download destination directory does not exist.", "DestinationPath", "DESTINATION_DIRECTORY_NOT_FOUND", null);
        }

        bool overwrite = download.OverwriteExisting || Options.Download.OverwriteCompletedFiles;
        if (!overwrite && global::System.IO.File.Exists(fullPath))
        {
            return CreateTerminalResult(download, DownloadState.Failed, new HttpResponseMetadata(), null, download.CatalogFileName, 0L, "DESTINATION_EXISTS", "The bulk download destination already exists.");
        }

        return null;
    }

    internal void ValidateRequest(DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A bulk download request is required.", "request", "REQUIRED", null);
        }

        if (download.Context == null)
        {
            throw new ValidationException("A bulk download context is required.", "Context", "REQUIRED", null);
        }

        if (download.SourceUri == null)
        {
            throw new ValidationException("A bulk download source URI is required.", "SourceUri", "REQUIRED", null);
        }

        if (Options.Download == null || Options.Download.BufferSize <= 0 || Options.Download.RedirectHopCount < 0 || Options.Download.TimeoutMilliseconds <= 0)
        {
            throw new ValidationException("A valid bulk download policy is required.", "Download", "INVALID_DOWNLOAD_POLICY", null);
        }

        if (Options.Download.HasMaximumBytes && Options.Download.MaximumBytes <= 0L)
        {
            throw new ValidationException("The maximum bulk file size must be positive.", "Download.MaximumBytes", "INVALID_LIMIT", null);
        }

        bool hasOutput = download.Output != null;
        bool hasDestination = !global::System.String.IsNullOrWhiteSpace(download.DestinationPath);
        if (hasOutput == hasDestination)
        {
            throw new ValidationException("Specify exactly one bulk output stream or destination path.", "request", "DESTINATION_REQUIRED", null);
        }

        if (download.Output != null && !download.Output.CanWrite)
        {
            throw new ValidationException("The bulk output stream must be writable.", "Output", "STREAM_NOT_WRITABLE", null);
        }

        if (download.HasExpectedSize && download.ExpectedSize < 0L)
        {
            throw new ValidationException("The expected bulk file size cannot be negative.", "ExpectedSize", "INVALID_SIZE", null);
        }
    }

    internal DownloadResult CreateTerminalResult(DownloadRequest download, DownloadState state, HttpResponseMetadata metadata, global::System.Net.Http.HttpResponseMessage? response, string? fileName, long bytesTransferred, string? failureCode, string? failureMessage)
    {
        DownloadResult result = new DownloadResult();
        result.Request = download;
        result.State = state;
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

        result.Progress = CreateProgress(download, state, bytesTransferred, metadata, fileName);
        if (download.Progress != null)
        {
            download.Progress.Report(result.Progress);
        }

        return result;
    }

    internal static DownloadProgress CreateProgress(DownloadRequest download, DownloadState state, long bytesTransferred, HttpResponseMetadata metadata, string? fileName)
    {
        DownloadProgress progress = new DownloadProgress();
        progress.State = state;
        progress.BytesTransferred = bytesTransferred;
        progress.FileName = fileName;
        progress.Timestamp = global::System.DateTimeOffset.UtcNow;
        progress.HasTimestamp = true;
        if (metadata.HasContentLength)
        {
            progress.TotalBytes = metadata.ContentLength;
            progress.HasTotalBytes = true;
        }
        else if (download.HasExpectedSize)
        {
            progress.TotalBytes = download.ExpectedSize;
            progress.HasTotalBytes = true;
        }

        return progress;
    }

    internal static void ReportProgress(DownloadRequest download, DownloadState state, long bytesTransferred, HttpResponseMetadata metadata, string? fileName)
    {
        if (download.Progress != null)
        {
            download.Progress.Report(CreateProgress(download, state, bytesTransferred, metadata, fileName));
        }
    }

    internal string[] CredentialSecrets()
    {
        if (Options.OdpApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.OdpApiKey };
    }
}
