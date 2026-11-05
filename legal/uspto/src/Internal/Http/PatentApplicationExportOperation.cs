namespace uspto;

internal class PatentApplicationExportOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal PatentApplicationExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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
    }

    internal DownloadResult DownloadSearch(SearchRequest search, DownloadRequest download)
    {
        ValidateRequest(search, download);
        return DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "applications", "search", "download" }, "patent application search export", "patent-application-search");
    }

    internal DownloadResult DownloadConfigured(SearchRequest search, DownloadRequest download, ExportFormat format, string[] pathSegments, string operationName, string defaultFileName)
    {
        ValidateConfiguredRequest(search, download, format);
        DocumentFormat documentFormat = SelectDocumentFormat(format);
        string body = OdpSearchRequestWriter.WriteExportBody(search, format, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateJsonPostRequest(Options.OdpBaseUri, pathSegments, new global::System.Collections.Generic.Dictionary<string, string?>(), body))
        {
            RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, documentFormat);
            RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(download.Context);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, true);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, operationName, Options.Limits, secrets, context.CancellationToken);
                }

                HttpResponseMetadata responseMetadata = ResponseReader.CaptureMetadata(response, secrets);
                ResponseReader.ValidateTextFormat(documentFormat, responseMetadata, ApiService.Odp);
                string fileName = ResolveConfiguredResponseFileName(response, download, format, defaultFileName);
                ValidateResponseSize(responseMetadata, download);
                if (download.Output != null)
                {
                    return CopyToStream(response, download.Output, download, documentFormat, fileName, responseMetadata, context, timeout, secrets);
                }

                return CopyToFile(response, download, documentFormat, fileName, responseMetadata, context, timeout, secrets);
            }
        }
    }

    internal DownloadResult CopyToStream(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream output, DownloadRequest download, DocumentFormat documentFormat, string fileName, HttpResponseMetadata responseMetadata, RequestContext context, HttpTimeoutScope timeout, string[] secrets)
    {
        StreamedHttpResponse streamed = TimeoutController.CopyBinaryBodyPhase(response, output, Options.Download.BufferSize, context, secrets, timeout);
        ValidateTransferredSize(streamed.BytesCopied, download);
        return CreateResult(download, documentFormat, fileName, responseMetadata, streamed.BytesCopied);
    }

    internal DownloadResult CopyToFile(global::System.Net.Http.HttpResponseMessage response, DownloadRequest download, DocumentFormat documentFormat, string fileName, HttpResponseMetadata responseMetadata, RequestContext context, HttpTimeoutScope timeout, string[] secrets)
    {
        string destinationPath = download.DestinationPath ?? string.Empty;
        global::System.IO.FileMode mode = global::System.IO.FileMode.CreateNew;
        if (download.OverwriteExisting || Options.Download.OverwriteCompletedFiles)
        {
            mode = global::System.IO.FileMode.Create;
        }

        using (global::System.IO.FileStream output = new global::System.IO.FileStream(destinationPath, mode, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None, Options.Download.BufferSize, global::System.IO.FileOptions.SequentialScan))
        {
            StreamedHttpResponse streamed = TimeoutController.CopyBinaryBodyPhase(response, output, Options.Download.BufferSize, context, secrets, timeout);
            ValidateTransferredSize(streamed.BytesCopied, download);
            DownloadResult result = CreateResult(download, documentFormat, fileName, responseMetadata, streamed.BytesCopied);
            result.FinalPath = destinationPath;
            return result;
        }
    }

    internal DownloadResult CreateResult(DownloadRequest download, DocumentFormat documentFormat, string fileName, HttpResponseMetadata responseMetadata, long bytesCopied)
    {
        DownloadResult result = new DownloadResult();
        result.Request = download;
        result.State = DownloadState.Completed;
        result.Progress.State = DownloadState.Completed;
        result.Progress.BytesTransferred = bytesCopied;
        result.Progress.FileName = fileName;
        result.Progress.Timestamp = global::System.DateTimeOffset.UtcNow;
        result.Progress.HasTimestamp = true;
        result.Content.FinalUri = responseMetadata.RequestUri;
        result.Content.FileName = fileName;
        result.Content.MediaType = responseMetadata.MediaType;
        result.Content.Format = documentFormat;
        result.Content.ContentLength = responseMetadata.ContentLength;
        result.Content.HasContentLength = responseMetadata.HasContentLength;
        if (responseMetadata.HasContentLength)
        {
            result.Progress.TotalBytes = responseMetadata.ContentLength;
            result.Progress.HasTotalBytes = true;
        }

        if (download.Progress != null)
        {
            download.Progress.Report(result.Progress);
        }

        return result;
    }

    internal void ValidateRequest(SearchRequest search, DownloadRequest download)
    {
        if (search == null)
        {
            throw new ValidationException("A patent application export search is required.", "search", "REQUIRED", null);
        }

        if (download == null)
        {
            throw new ValidationException("A patent application export download request is required.", "download", "REQUIRED", null);
        }

        ValidateConfiguredRequest(search, download, download.ExportFormat);
    }

    internal void ValidateConfiguredRequest(SearchRequest search, DownloadRequest download, ExportFormat format)
    {
        if (search == null)
        {
            throw new ValidationException("A patent application export search is required.", "search", "REQUIRED", null);
        }

        if (download == null)
        {
            throw new ValidationException("A patent application export download request is required.", "download", "REQUIRED", null);
        }

        if (download.Context == null)
        {
            throw new ValidationException("A patent application export context is required.", "Context", "REQUIRED", null);
        }

        download.Context.CancellationToken.ThrowIfCancellationRequested();
        SelectDocumentFormat(format);
        if (Options.Download == null)
        {
            throw new ValidationException("A download policy is required.", "Download", "REQUIRED", null);
        }

        if (Options.Download.BufferSize <= 0)
        {
            throw new ValidationException("The export transfer buffer size must be positive.", "Download.BufferSize", "INVALID_LIMIT", null);
        }

        if (Options.Download.HasMaximumBytes && Options.Download.MaximumBytes <= 0L)
        {
            throw new ValidationException("The maximum export size must be positive when configured.", "Download.MaximumBytes", "INVALID_LIMIT", null);
        }

        bool hasOutput = download.Output != null;
        bool hasDestination = !global::System.String.IsNullOrWhiteSpace(download.DestinationPath);
        if (hasOutput == hasDestination)
        {
            throw new ValidationException("Specify exactly one export output stream or destination path.", "download", "DESTINATION_REQUIRED", null);
        }

        if (download.Output != null && !download.Output.CanWrite)
        {
            throw new ValidationException("The export output stream must be writable.", "Output", "STREAM_NOT_WRITABLE", null);
        }

        if (hasDestination)
        {
            string? destinationName = global::System.IO.Path.GetFileName(download.DestinationPath);
            ValidateFileName(destinationName, format, "DestinationPath");
        }

        if (download.HasExpectedSize && download.ExpectedSize < 0L)
        {
            throw new ValidationException("The expected export size cannot be negative.", "ExpectedSize", "INVALID_SIZE", null);
        }
    }

    internal static DocumentFormat SelectDocumentFormat(ExportFormat format)
    {
        switch (format)
        {
            case ExportFormat.Json:
                return DocumentFormat.Json;
            case ExportFormat.Csv:
                return DocumentFormat.Csv;
            default:
                throw new ValidationException("The patent application export format must be JSON or CSV.", "ExportFormat", "UNSUPPORTED_FORMAT", null);
        }
    }

    internal static string ResolveResponseFileName(global::System.Net.Http.HttpResponseMessage response, DownloadRequest download, ExportFormat format)
    {
        return ResolveConfiguredResponseFileName(response, download, format, "patent-application-search");
    }

    internal static string ResolveConfiguredResponseFileName(global::System.Net.Http.HttpResponseMessage response, DownloadRequest download, ExportFormat format, string defaultFileName)
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

        if (global::System.String.IsNullOrWhiteSpace(fileName))
        {
            if (!global::System.String.IsNullOrWhiteSpace(download.DestinationPath))
            {
                fileName = global::System.IO.Path.GetFileName(download.DestinationPath);
            }
            else if (format == ExportFormat.Csv)
            {
                fileName = defaultFileName + ".csv";
            }
            else
            {
                fileName = defaultFileName + ".json";
            }
        }

        ValidateFileName(fileName, format, "Content-Disposition");
        return fileName;
    }

    internal static void ValidateFileName(string? fileName, ExportFormat format, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == ".." || fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0 || fileName.IndexOf(':') >= 0)
        {
            throw CreateFileNameFailure("The patent application export filename is invalid.", "INVALID_EXPORT_FILENAME", fileName, fieldName);
        }

        int index;
        for (index = 0; index < fileName.Length; index += 1)
        {
            if (global::System.Char.IsControl(fileName[index]))
            {
                throw CreateFileNameFailure("The patent application export filename is invalid.", "INVALID_EXPORT_FILENAME", null, fieldName);
            }
        }

        string expectedExtension = ".json";
        if (format == ExportFormat.Csv)
        {
            expectedExtension = ".csv";
        }

        if (!fileName.EndsWith(expectedExtension, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw CreateFileNameFailure("The patent application export filename does not match its requested format.", "EXPORT_FILENAME_FORMAT_MISMATCH", fileName, fieldName);
        }
    }

    internal static global::System.Exception CreateFileNameFailure(string message, string errorCode, string? fileName, string fieldName)
    {
        if (fieldName == "DestinationPath")
        {
            return new ValidationException(message, fieldName, errorCode, null);
        }

        return new ApiException(message, ApiService.Odp, 0, false, errorCode, fileName, null, false, null);
    }

    internal void ValidateResponseSize(HttpResponseMetadata metadata, DownloadRequest download)
    {
        if (metadata.HasContentLength && Options.Download.HasMaximumBytes && metadata.ContentLength > Options.Download.MaximumBytes)
        {
            throw new ApiException("The patent application export exceeds the configured download limit.", ApiService.Odp, metadata.StatusCode, true, "PAYLOAD_TOO_LARGE", null, metadata.RequestIdentifier, false, null);
        }

        if (metadata.HasContentLength && download.HasExpectedSize && metadata.ContentLength != download.ExpectedSize)
        {
            throw new ApiException("The patent application export size does not match the expected size.", ApiService.Odp, metadata.StatusCode, true, "RESPONSE_LENGTH_MISMATCH", null, metadata.RequestIdentifier, false, null);
        }
    }

    internal static void ValidateTransferredSize(long bytesCopied, DownloadRequest download)
    {
        if (download.HasExpectedSize && bytesCopied != download.ExpectedSize)
        {
            throw new ApiException("The patent application export size does not match the expected size.", ApiService.Odp, 200, true, "RESPONSE_LENGTH_MISMATCH", null, null, false, null);
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
