namespace uspto;

internal class PatentDocumentDownloadValidation
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
    internal string? FileName = null;
    internal DocumentFormat Format = DocumentFormat.Unknown;
}

internal class PatentDocumentResponseLease : global::System.IDisposable
{
    internal global::System.Net.Http.HttpRequestMessage? Request = null;
    internal global::System.Net.Http.HttpResponseMessage? Response = null;

    internal PatentDocumentResponseLease(global::System.Net.Http.HttpRequestMessage request)
    {
        Request = request;
    }

    internal void SendResponse(global::System.Net.Http.HttpClient client, HttpTimeoutScope timeout)
    {
        if (Request == null)
        {
            throw new ValidationException("A patent document request is required.", "Request", "REQUIRED", null);
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

internal class AtomicPatentDocumentFile : global::System.IDisposable
{
    internal string DestinationPath;
    internal string TemporaryPath;
    internal global::System.IO.FileStream? Output = null;
    internal bool IsCommitted = false;

    internal AtomicPatentDocumentFile(string destinationPath, int bufferSize)
    {
        DestinationPath = global::System.IO.Path.GetFullPath(destinationPath);
        TemporaryPath = DestinationPath + "." + global::System.Guid.NewGuid().ToString("N") + ".partial";
        Output = new global::System.IO.FileStream(TemporaryPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, bufferSize, global::System.IO.FileOptions.SequentialScan);
    }

    internal void Commit(bool overwrite)
    {
        if (Output == null)
        {
            throw new ValidationException("The patent document temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
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

internal class PatentDocumentDownloadOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;
    internal string[] AdditionalSupportedMediaTypes = new string[0];
    internal ApiService Service = ApiService.Odp;
    internal DocumentFormat RequestedFormat = DocumentFormat.Unknown;
    internal bool StrictMediaType = false;
    internal bool StrictImageMediaType = false;
    internal bool IsBundleRequest = false;

    internal PatentDocumentDownloadOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Client = client;
        Options = options;
        HostPolicy = DownloadHostPolicy.CreateDefault();
    }

    internal DownloadResult Download(string documentIdentifier, DownloadRequest download)
    {
        ValidateRequest(documentIdentifier, download);
        if (download.Context.CancellationToken.IsCancellationRequested)
        {
            return CreateTerminalResult(download, DownloadState.Cancelled, new HttpResponseMetadata(), null, documentIdentifier, DocumentFormat.Unknown, 0L, "CANCELLED", "The patent document download was cancelled.");
        }

        DownloadResult? destinationFailure = ValidateDestinationAvailability(download, documentIdentifier);
        if (destinationFailure != null)
        {
            return destinationFailure;
        }

        global::System.Uri sourceUri = download.SourceUri ?? new global::System.Uri("https://invalid.example/");
        DownloadHostValidation initialHost = HostPolicy.Validate(sourceUri, Options, download.Context.CancellationToken);
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(download.Context);
        context.Service = Service;
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, true);
        DocumentFormat expectedFormat = RequestedFormat;
        if (expectedFormat == DocumentFormat.Unknown)
        {
            expectedFormat = OdpPatentDocumentParser.DetectUriFormat(sourceUri);
        }
        global::System.Net.Http.HttpRequestMessage initialRequest = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, sourceUri);
        PrepareHeaders(initialRequest, initialHost);
        RedirectState? redirectState = null;
        if (global::System.String.Equals(sourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            redirectState = new RedirectState(Service, sourceUri, Options.Download.RedirectHopCount);
        }

        using (PatentDocumentResponseLease lease = new PatentDocumentResponseLease(initialRequest))
        {
            lease.SendResponse(Client, timeout);
            FollowRedirects(lease, redirectState, context, timeout);
            if (lease.Response == null)
            {
                throw new ValidationException("The patent document response is required.", "Response", "REQUIRED", null);
            }

            string[] secrets = CredentialSecrets();
            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(lease.Response, secrets);
            int statusCode = metadata.StatusCode;
            if ((statusCode < 200 || statusCode >= 300) && TsdrPolicy.IsTsdrService(Service))
            {
                throw ErrorMapper.MapTsdrDownloadResponse(lease.Response, Service, documentIdentifier, Options.Limits, secrets, IsBundleRequest, context.CancellationToken);
            }

            PatentDocumentDownloadValidation validation = ValidateResponse(lease.Response, metadata, expectedFormat, documentIdentifier, download);
            if (!validation.IsValid)
            {
                return CreateTerminalResult(download, DownloadState.Failed, metadata, lease.Response, validation.FileName, validation.Format, 0L, validation.FailureCode, validation.FailureMessage);
            }

            ReportProgress(download, DownloadState.Downloading, 0L, metadata, validation.FileName);
            if (download.Output != null)
            {
                StreamedHttpResponse streamed = TimeoutController.CopyBoundedBinaryBodyPhase(lease.Response, download.Output, context, secrets, timeout, Options.Download, download, validation.FileName);
                if (streamed.FailureCode != null)
                {
                    return CreateTerminalResult(download, DownloadState.Failed, metadata, lease.Response, validation.FileName, validation.Format, streamed.BytesCopied, streamed.FailureCode, streamed.FailureMessage);
                }

                return CreateTerminalResult(download, DownloadState.Completed, metadata, lease.Response, validation.FileName, validation.Format, streamed.BytesCopied, null, null);
            }

            string destinationPath = download.DestinationPath ?? string.Empty;
            using (AtomicPatentDocumentFile atomicFile = new AtomicPatentDocumentFile(destinationPath, Options.Download.BufferSize))
            {
                if (atomicFile.Output == null)
                {
                    throw new ValidationException("The patent document temporary file is unavailable.", "Output", "TEMPORARY_FILE_UNAVAILABLE", null);
                }

                StreamedHttpResponse streamed = TimeoutController.CopyBoundedBinaryBodyPhase(lease.Response, atomicFile.Output, context, secrets, timeout, Options.Download, download, validation.FileName);
                if (streamed.FailureCode != null)
                {
                    return CreateTerminalResult(download, DownloadState.Failed, metadata, lease.Response, validation.FileName, validation.Format, streamed.BytesCopied, streamed.FailureCode, streamed.FailureMessage);
                }

                bool overwrite = download.OverwriteExisting || Options.Download.OverwriteCompletedFiles;
                atomicFile.Commit(overwrite);
                DownloadResult result = CreateTerminalResult(download, DownloadState.Completed, metadata, lease.Response, validation.FileName, validation.Format, streamed.BytesCopied, null, null);
                result.FinalPath = atomicFile.DestinationPath;
                return result;
            }
        }
    }

    internal void FollowRedirects(PatentDocumentResponseLease lease, RedirectState? redirectState, RequestContext context, HttpTimeoutScope timeout)
    {
        while (lease.Response != null && RedirectPolicy.IsRedirectStatus((int)lease.Response.StatusCode))
        {
            if (redirectState == null || lease.Request == null)
            {
                throw new ApiException("Document redirects require an HTTPS source URI.", Service, (int)lease.Response.StatusCode, true, "REDIRECT_HTTPS_REQUIRED", null, null, false, null);
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
                throw new ApiException("The HTTP client followed a redirect outside the validated redirect policy.", Service, (int)lease.Response.StatusCode, false, "AUTOMATIC_REDIRECT_REJECTED", null, null, false, null);
            }
        }
    }

    internal void PrepareHeaders(global::System.Net.Http.HttpRequestMessage request, DownloadHostValidation host)
    {
        RequestHeaderPolicy.Apply(request, Service, Options, false, RequestedFormat);
        if (RequestedFormat == DocumentFormat.Unknown)
        {
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new global::System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
        }

        RemoveUnapprovedCredentials(request, host);
    }

    internal void RemoveUnapprovedCredentials(global::System.Net.Http.HttpRequestMessage request, DownloadHostValidation host)
    {
        string approvedCredentialHost = "api.uspto.gov";
        if ((Service == ApiService.Tsdr || Service == ApiService.TsdrPdfAndZip) && Options.TsdrBaseUri != null)
        {
            approvedCredentialHost = Options.TsdrBaseUri.IdnHost;
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

    internal PatentDocumentDownloadValidation ValidateResponse(global::System.Net.Http.HttpResponseMessage response, HttpResponseMetadata metadata, DocumentFormat expectedFormat, string documentIdentifier, DownloadRequest download)
    {
        PatentDocumentDownloadValidation validation = new PatentDocumentDownloadValidation();
        validation.Format = metadata.Format;
        validation.FileName = ResolveFileName(response, metadata.RequestUri, documentIdentifier);
        int statusCode = metadata.StatusCode;
        if (statusCode < 200 || statusCode >= 300)
        {
            validation.FailureCode = "HTTP_STATUS_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
            validation.FailureMessage = "The patent document service returned a non-success status.";
            return validation;
        }

        if (!IsSafeFileName(validation.FileName))
        {
            validation.FailureCode = "INVALID_DOCUMENT_FILENAME";
            validation.FailureMessage = "The patent document response filename is invalid.";
            return validation;
        }

        string? mediaType = metadata.MediaType;
        bool mediaTypeSupported = IsSupportedMediaType(mediaType, expectedFormat) || IsAdditionalSupportedMediaType(mediaType);
        if (StrictMediaType)
        {
            mediaTypeSupported = IsStrictMediaType(mediaType, expectedFormat);
        }
        else if (StrictImageMediaType)
        {
            mediaTypeSupported = IsPageMediaType(mediaType);
        }

        if (!mediaTypeSupported)
        {
            validation.FailureCode = "UNEXPECTED_MEDIA_TYPE";
            validation.FailureMessage = "The patent document response media type is not supported.";
            return validation;
        }

        if (validation.Format == DocumentFormat.Unknown)
        {
            validation.Format = expectedFormat;
        }

        if (expectedFormat != DocumentFormat.Unknown && metadata.Format != DocumentFormat.Unknown && expectedFormat != metadata.Format)
        {
            validation.FailureCode = "DOCUMENT_FORMAT_MISMATCH";
            validation.FailureMessage = "The patent document response format does not match the requested URI.";
            return validation;
        }

        DocumentFormat fileNameFormat = DetectFileNameFormat(validation.FileName);
        if (expectedFormat != DocumentFormat.Unknown && fileNameFormat != DocumentFormat.Unknown && expectedFormat != fileNameFormat)
        {
            validation.FailureCode = "DOCUMENT_FILENAME_FORMAT_MISMATCH";
            validation.FailureMessage = "The patent document response filename does not match the requested format.";
            return validation;
        }

        if (StrictImageMediaType && !IsPageMediaFileNameCompatible(mediaType, validation.FileName))
        {
            validation.FailureCode = "PAGE_MEDIA_FILENAME_MISMATCH";
            validation.FailureMessage = "The document page filename does not match the returned media type.";
            return validation;
        }

        if (metadata.HasContentLength && Options.Download.HasMaximumBytes && metadata.ContentLength > Options.Download.MaximumBytes)
        {
            validation.FailureCode = "PAYLOAD_TOO_LARGE";
            validation.FailureMessage = "The patent document exceeds the configured download limit.";
            return validation;
        }

        if (metadata.HasContentLength && download.HasExpectedSize && metadata.ContentLength != download.ExpectedSize)
        {
            validation.FailureCode = "RESPONSE_LENGTH_MISMATCH";
            validation.FailureMessage = "The patent document response length does not match its expected size.";
            return validation;
        }

        validation.IsValid = true;
        return validation;
    }

    internal static bool IsSupportedMediaType(string? mediaType, DocumentFormat expectedFormat)
    {
        if (mediaType == null)
        {
            return expectedFormat != DocumentFormat.Unknown;
        }

        string normalized = mediaType.ToLowerInvariant();
        if (normalized == UsptoMediaTypes.Pdf || normalized == UsptoMediaTypes.Zip || normalized == UsptoMediaTypes.Xml || normalized == UsptoMediaTypes.TextXml || normalized == UsptoMediaTypes.Binary || normalized == UsptoMediaTypes.Text || normalized.StartsWith("image/", global::System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static bool IsStrictMediaType(string? mediaType, DocumentFormat expectedFormat)
    {
        if (mediaType == null)
        {
            return false;
        }

        string normalized = mediaType.ToLowerInvariant();
        switch (expectedFormat)
        {
            case DocumentFormat.Html:
                return normalized == UsptoMediaTypes.Html;
            case DocumentFormat.Xml:
                return normalized == UsptoMediaTypes.Xml;
            case DocumentFormat.Pdf:
                return normalized == UsptoMediaTypes.Pdf;
            case DocumentFormat.Zip:
                return normalized == UsptoMediaTypes.Zip;
            default:
                return false;
        }
    }

    internal static bool IsPageMediaType(string? mediaType)
    {
        if (mediaType == null)
        {
            return false;
        }

        string normalized = mediaType.ToLowerInvariant();
        return normalized == UsptoMediaTypes.Tiff || normalized == UsptoMediaTypes.Jpeg || normalized == UsptoMediaTypes.Png;
    }

    internal static bool IsPageMediaFileNameCompatible(string? mediaType, string? fileName)
    {
        if (mediaType == null || fileName == null)
        {
            return false;
        }

        string normalizedMediaType = mediaType.ToLowerInvariant();
        if (fileName.EndsWith(".tif", global::System.StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tiff", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedMediaType == UsptoMediaTypes.Tiff;
        }

        if (fileName.EndsWith(".jpg", global::System.StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpeg", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedMediaType == UsptoMediaTypes.Jpeg;
        }

        if (fileName.EndsWith(".png", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedMediaType == UsptoMediaTypes.Png;
        }

        return DetectFileNameFormat(fileName) == DocumentFormat.Unknown;
    }

    internal bool IsAdditionalSupportedMediaType(string? mediaType)
    {
        if (mediaType == null)
        {
            return false;
        }

        bool supported = false;
        int index;
        for (index = 0; index < AdditionalSupportedMediaTypes.Length; index += 1)
        {
            if (global::System.String.Equals(mediaType, AdditionalSupportedMediaTypes[index], global::System.StringComparison.OrdinalIgnoreCase))
            {
                supported = true;
            }
        }

        return supported;
    }

    internal static DocumentFormat DetectFileNameFormat(string? fileName)
    {
        if (fileName == null)
        {
            return DocumentFormat.Unknown;
        }

        if (fileName.EndsWith(".pdf", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Pdf;
        }

        if (fileName.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Zip;
        }

        if (fileName.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Xml;
        }

        if (fileName.EndsWith(".json", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Json;
        }

        if (fileName.EndsWith(".html", global::System.StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".htm", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Html;
        }

        if (fileName.EndsWith(".txt", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Text;
        }

        return DocumentFormat.Unknown;
    }

    internal static string ResolveFileName(global::System.Net.Http.HttpResponseMessage response, global::System.Uri? responseUri, string documentIdentifier)
    {
        string? fileName = null;
        global::System.Net.Http.Headers.ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;
        if (disposition != null)
        {
            fileName = disposition.FileNameStar;
            if (global::System.String.IsNullOrWhiteSpace(fileName))
            {
                fileName = disposition.FileName;
            }
        }

        if (fileName != null)
        {
            fileName = fileName.Trim();
            if (fileName.Length >= 2 && fileName[0] == '"' && fileName[fileName.Length - 1] == '"')
            {
                fileName = fileName.Substring(1, fileName.Length - 2);
            }
        }

        if (global::System.String.IsNullOrWhiteSpace(fileName) && responseUri != null)
        {
            string pathName = global::System.IO.Path.GetFileName(responseUri.AbsolutePath);
            if (!global::System.String.IsNullOrWhiteSpace(pathName))
            {
                fileName = global::System.Uri.UnescapeDataString(pathName);
            }
        }

        if (global::System.String.IsNullOrWhiteSpace(fileName))
        {
            fileName = documentIdentifier;
        }

        return fileName;
    }

    internal static bool IsSafeFileName(string? fileName)
    {
        if (global::System.String.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == ".." || fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0 || fileName.IndexOf(':') >= 0)
        {
            return false;
        }

        int index;
        for (index = 0; index < fileName.Length; index += 1)
        {
            if (global::System.Char.IsControl(fileName[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal DownloadResult? ValidateDestinationAvailability(DownloadRequest download, string documentIdentifier)
    {
        if (download.DestinationPath == null)
        {
            return null;
        }

        string fullPath = global::System.IO.Path.GetFullPath(download.DestinationPath);
        if (global::System.IO.Directory.Exists(fullPath))
        {
            throw new ValidationException("The patent document destination must be a file path.", "DestinationPath", "FILE_PATH_REQUIRED", null);
        }

        string? directory = global::System.IO.Path.GetDirectoryName(fullPath);
        if (directory == null || !global::System.IO.Directory.Exists(directory))
        {
            throw new ValidationException("The patent document destination directory does not exist.", "DestinationPath", "DESTINATION_DIRECTORY_NOT_FOUND", null);
        }

        bool overwrite = download.OverwriteExisting || Options.Download.OverwriteCompletedFiles;
        if (!overwrite && global::System.IO.File.Exists(fullPath))
        {
            return CreateTerminalResult(download, DownloadState.Failed, new HttpResponseMetadata(), null, documentIdentifier, DocumentFormat.Unknown, 0L, "DESTINATION_EXISTS", "The patent document destination already exists.");
        }

        return null;
    }

    internal void ValidateRequest(string documentIdentifier, DownloadRequest download)
    {
        if (global::System.String.IsNullOrWhiteSpace(documentIdentifier))
        {
            throw new ValidationException("A patent document identifier is required.", "documentIdentifier", "REQUIRED", null);
        }

        if (download == null)
        {
            throw new ValidationException("A patent document download request is required.", "request", "REQUIRED", null);
        }

        if (download.Context == null)
        {
            throw new ValidationException("A patent document download context is required.", "Context", "REQUIRED", null);
        }

        if (download.SourceUri == null)
        {
            throw new ValidationException("A patent document source URI is required.", "SourceUri", "REQUIRED", null);
        }

        if (Options.Download == null || Options.Download.BufferSize <= 0 || Options.Download.RedirectHopCount < 0 || Options.Download.TimeoutMilliseconds <= 0)
        {
            throw new ValidationException("A valid patent document download policy is required.", "Download", "INVALID_DOWNLOAD_POLICY", null);
        }

        if (Options.Download.HasMaximumBytes && Options.Download.MaximumBytes <= 0L)
        {
            throw new ValidationException("The maximum patent document size must be positive.", "Download.MaximumBytes", "INVALID_LIMIT", null);
        }

        bool hasOutput = download.Output != null;
        bool hasDestination = !global::System.String.IsNullOrWhiteSpace(download.DestinationPath);
        if (hasOutput == hasDestination)
        {
            throw new ValidationException("Specify exactly one patent document output stream or destination path.", "request", "DESTINATION_REQUIRED", null);
        }

        if (download.Output != null && !download.Output.CanWrite)
        {
            throw new ValidationException("The patent document output stream must be writable.", "Output", "STREAM_NOT_WRITABLE", null);
        }

        if (download.HasExpectedSize && download.ExpectedSize < 0L)
        {
            throw new ValidationException("The expected patent document size cannot be negative.", "ExpectedSize", "INVALID_SIZE", null);
        }
    }

    internal DownloadResult CreateTerminalResult(DownloadRequest download, DownloadState state, HttpResponseMetadata metadata, global::System.Net.Http.HttpResponseMessage? response, string? fileName, DocumentFormat format, long bytesTransferred, string? failureCode, string? failureMessage)
    {
        DownloadResult result = new DownloadResult();
        result.Request = download;
        result.State = state;
        result.FailureCode = failureCode;
        result.FailureMessage = failureMessage;
        result.Content.FinalUri = metadata.RequestUri;
        result.Content.FileName = fileName;
        result.Content.MediaType = metadata.MediaType;
        result.Content.Format = format;
        result.Content.ContentLength = metadata.ContentLength;
        result.Content.HasContentLength = metadata.HasContentLength;
        if (response != null)
        {
            PopulateResponseValidators(result.Content, response);
        }

        result.Progress = CreateProgress(state, bytesTransferred, metadata, fileName);
        if (download.Progress != null)
        {
            download.Progress.Report(result.Progress);
        }

        return result;
    }

    internal static void PopulateResponseValidators(DownloadContentMetadata content, global::System.Net.Http.HttpResponseMessage response)
    {
        if (response.Headers.ETag != null)
        {
            content.ETag = response.Headers.ETag.ToString();
        }

        if (response.Content.Headers.LastModified.HasValue)
        {
            content.LastModified = response.Content.Headers.LastModified.GetValueOrDefault();
            content.HasLastModified = true;
        }

        foreach (string range in response.Headers.AcceptRanges)
        {
            if (global::System.String.Equals(range, "bytes", global::System.StringComparison.OrdinalIgnoreCase))
            {
                content.AcceptsByteRanges = true;
            }
        }
    }

    internal static DownloadProgress CreateProgress(DownloadState state, long bytesTransferred, HttpResponseMetadata metadata, string? fileName)
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

        return progress;
    }

    internal static void ReportProgress(DownloadRequest download, DownloadState state, long bytesTransferred, HttpResponseMetadata metadata, string? fileName)
    {
        if (download.Progress != null)
        {
            download.Progress.Report(CreateProgress(state, bytesTransferred, metadata, fileName));
        }
    }

    internal string[] CredentialSecrets()
    {
        string? credential = Options.OdpApiKey;
        if (Service == ApiService.Tsdr || Service == ApiService.TsdrPdfAndZip)
        {
            credential = Options.TsdrApiKey;
        }

        if (credential == null)
        {
            return new string[0];
        }

        return new string[] { credential };
    }
}
