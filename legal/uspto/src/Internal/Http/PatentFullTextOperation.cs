namespace uspto;

internal class PatentFullTextFileSelection
{
    internal PatentFileMetadata File = new PatentFileMetadata();
    internal string DocumentCategory = string.Empty;
    internal string ExpectedRootName = string.Empty;
}

internal class PatentFullTextOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal PatentApplicationRetrievalOperation RetrievalOperation;
    internal PatentDocumentDownloadOperation DownloadOperation;

    internal PatentFullTextOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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
        RetrievalOperation = new PatentApplicationRetrievalOperation(client, options);
        DownloadOperation = new PatentDocumentDownloadOperation(client, options);
    }

    internal PatentFullText Get(string identifier, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent full-text request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentAssociatedDocumentList documents = RetrievalOperation.GetAssociatedDocuments(identifier, context);
        return GetFromAssociatedDocuments(documents, context);
    }

    internal PatentFullText GetFromAssociatedDocuments(PatentAssociatedDocumentList documents, RequestContext context)
    {
        if (documents == null)
        {
            throw new ValidationException("Associated patent documents are required.", "documents", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A patent full-text request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        if (Options.Limits == null || Options.Limits.MaximumResponseBytes <= 0L || Options.Limits.MaximumResponseBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The patent full-text response limit must fit in a supported buffer.", "MaximumResponseBytes", "INVALID_LIMIT", null);
        }

        PatentFullTextFileSelection selection = SelectFile(documents);
        PatentFileMetadata file = selection.File;
        if (file.LocationUri == null)
        {
            throw new ValidationException("The selected patent full-text location is required.", "LocationUri", "REQUIRED", null);
        }

        if (file.HasSize && file.Size > Options.Limits.MaximumResponseBytes)
        {
            throw new DownloadException("The patent full-text document exceeds the configured buffered response limit.", SafeSourceUri(file.LocationUri), null, DownloadState.Failed, 0L, 0, false, false, null);
        }

        using (global::System.IO.MemoryStream output = new global::System.IO.MemoryStream())
        {
            DownloadRequest request = new DownloadRequest();
            request.SourceUri = file.LocationUri;
            request.Output = output;
            request.Context = context;
            if (file.HasSize)
            {
                request.ExpectedSize = file.Size;
                request.HasExpectedSize = true;
            }

            string? metadataIdentifier = file.DocumentIdentifier;
            if (global::System.String.IsNullOrWhiteSpace(metadataIdentifier))
            {
                metadataIdentifier = documents.ApplicationNumber;
            }

            string documentIdentifier = global::System.String.IsNullOrWhiteSpace(metadataIdentifier) ? selection.ExpectedRootName : metadataIdentifier;
            DownloadResult download = DownloadOperation.Download(documentIdentifier, request);
            if (download.State != DownloadState.Completed)
            {
                throw CreateDownloadFailure(download, file.LocationUri);
            }

            if (output.Length > Options.Limits.MaximumResponseBytes)
            {
                throw new DownloadException("The patent full-text document exceeds the configured buffered response limit.", ResolveDownloadedSource(download, file.LocationUri), null, DownloadState.Failed, output.Length, 0, false, false, null);
            }

            output.Position = 0L;
            PatentDataProvenance provenance = CreateFullTextProvenance(documents, file, download);
            string? sourceName = download.Content.FileName;
            if (sourceName == null)
            {
                sourceName = file.XmlFileName;
            }

            PatentFullText result = PatentFullTextXmlParser.Parse(output, sourceName, Options.Limits, provenance);
            VerifyDocumentType(result, selection, sourceName);
            result.Provenance.DatasetVersion = result.DtdVersion;
            return result;
        }
    }

    internal static PatentFullTextFileSelection SelectFile(PatentAssociatedDocumentList documents)
    {
        if (documents == null)
        {
            throw new ValidationException("Associated patent documents are required.", "documents", "REQUIRED", null);
        }

        PatentFullTextFileSelection? selection = SelectFromCollection(documents.GrantFiles, "grant", "us-patent-grant");
        if (selection == null)
        {
            selection = SelectFromCollection(documents.PublicationFiles, "published application", "us-patent-application");
        }

        if (selection == null)
        {
            throw new NotFoundException("No directly downloadable patent full-text XML document was found.", ApiService.Odp, documents.ApplicationNumber, 0, false, documents.RequestIdentifier, null);
        }

        return selection;
    }

    internal static PatentFullTextFileSelection? SelectFromCollection(PatentFileMetadata[] files, string documentCategory, string expectedRootName)
    {
        if (files == null)
        {
            return null;
        }

        PatentFileMetadata? best = null;
        int bestScore = 0;
        foreach (PatentFileMetadata file in files)
        {
            int score = XmlCandidateScore(file);
            if (score > bestScore)
            {
                best = file;
                bestScore = score;
            }
        }

        if (best == null)
        {
            return null;
        }

        PatentFullTextFileSelection selection = new PatentFullTextFileSelection();
        selection.File = best;
        selection.DocumentCategory = documentCategory;
        selection.ExpectedRootName = expectedRootName;
        return selection;
    }

    internal static int XmlCandidateScore(PatentFileMetadata file)
    {
        if (file == null || file.LocationUri == null)
        {
            return 0;
        }

        string path = file.LocationUri.AbsolutePath;
        if (path.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase) || file.Format == DocumentFormat.Zip)
        {
            return 0;
        }

        if (path.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (file.XmlFileName != null && file.XmlFileName.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (file.Format == DocumentFormat.Xml || global::System.String.Equals(file.MediaType, UsptoMediaTypes.Xml, global::System.StringComparison.OrdinalIgnoreCase) || global::System.String.Equals(file.MediaType, UsptoMediaTypes.TextXml, global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 0;
    }

    internal PatentDataProvenance CreateFullTextProvenance(PatentAssociatedDocumentList documents, PatentFileMetadata file, DownloadResult download)
    {
        PatentDataProvenance result = OdpJsonParser.CopyProvenance(documents.Provenance);
        result.Source = DataProvenance.Bulk;
        result.SourceUri = ResolveDownloadedSource(download, file.LocationUri ?? new global::System.Uri("https://invalid.example/"));
        result.DatasetIdentifier = file.ProductIdentifier;
        if (documents.RequestIdentifier != null)
        {
            result.RequestIdentifier = documents.RequestIdentifier;
        }
        result.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.HasRetrievedAt = true;
        result.IsAuthoritative = true;
        return result;
    }

    internal static void VerifyDocumentType(PatentFullText result, PatentFullTextFileSelection selection, string? sourceName)
    {
        if (!global::System.String.Equals(result.RootElementName, selection.ExpectedRootName, global::System.StringComparison.Ordinal))
        {
            string path = result.RootElementName == null ? "/" : "/" + result.RootElementName;
            throw new ParseException("The downloaded patent full-text document type does not match its associated-document metadata.", DocumentFormat.Xml, sourceName, path, 0L, false, 0, false, null);
        }

        result.DocumentCategory = selection.DocumentCategory;
    }

    internal DownloadException CreateDownloadFailure(DownloadResult download, global::System.Uri selectedUri)
    {
        string message = download.FailureMessage == null ? "The patent full-text document download failed." : download.FailureMessage;
        int statusCode = ReadFailureStatusCode(download.FailureCode);
        bool hasStatusCode = statusCode > 0;
        bool retryable = IsRetryableDownloadFailure(download.FailureCode, statusCode);
        return new DownloadException(message, ResolveDownloadedSource(download, selectedUri), null, download.State, download.Progress.BytesTransferred, statusCode, hasStatusCode, retryable, null);
    }

    internal static int ReadFailureStatusCode(string? failureCode)
    {
        string prefix = "HTTP_STATUS_";
        if (failureCode == null || !failureCode.StartsWith(prefix, global::System.StringComparison.Ordinal))
        {
            return 0;
        }

        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(failureCode.Substring(prefix.Length));
        if (!parsed.IsValid || parsed.Value < 100L || parsed.Value > 999L)
        {
            return 0;
        }

        return (int)parsed.Value;
    }

    internal static bool IsRetryableDownloadFailure(string? failureCode, int statusCode)
    {
        if (statusCode == 408 || statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
        {
            return true;
        }

        return global::System.String.Equals(failureCode, "RESPONSE_LENGTH_MISMATCH", global::System.StringComparison.Ordinal);
    }

    internal global::System.Uri SafeSourceUri(global::System.Uri sourceUri)
    {
        return SecretRedactor.RedactUri(sourceUri, CredentialSecrets());
    }

    internal global::System.Uri ResolveDownloadedSource(DownloadResult download, global::System.Uri fallback)
    {
        if (download.Content.FinalUri != null)
        {
            return download.Content.FinalUri;
        }

        return SafeSourceUri(fallback);
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
