namespace uspto;

internal class TrademarkDocumentOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal TrademarkDocumentOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal TrademarkDocumentCollection GetDocuments(string identifier, RequestContext sourceContext)
    {
        TrademarkIdentifierResult parsed = ValidateRequest(identifier, sourceContext);
        BufferedHttpResponse buffered = ReadMetadata(parsed, null, sourceContext);
        return TsdrDocumentXmlParser.ParseCollection(buffered.Text, identifier, parsed, buffered.Metadata, RawResponse(buffered), Options.Limits);
    }

    internal TrademarkDocument GetDocument(string identifier, string documentIdentifier, RequestContext sourceContext)
    {
        TrademarkIdentifierResult parsed = ValidateRequest(identifier, sourceContext);
        string normalizedDocumentIdentifier = NormalizeDocumentIdentifier(documentIdentifier);
        BufferedHttpResponse buffered = ReadMetadata(parsed, normalizedDocumentIdentifier, sourceContext);
        return TsdrDocumentXmlParser.ParseDocument(buffered.Text, identifier, parsed, normalizedDocumentIdentifier, buffered.Metadata, RawResponse(buffered), Options.Limits);
    }

    internal DownloadResult GetContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        ValidateFormat(format);
        return Transfer(identifier, documentIdentifier, format, "content", request);
    }

    internal DownloadResult Download(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        ValidateFormat(format);
        return Transfer(identifier, documentIdentifier, format, "download", request);
    }

    internal DownloadResult DownloadPageMedia(string identifier, string documentIdentifier, int pageNumber, DownloadRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A trademark page-media request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A trademark page-media context is required.", "Context", "REQUIRED", null);
        }

        TrademarkIdentifierResult parsed = ValidateRequest(identifier, request.Context);
        string normalizedDocumentIdentifier = NormalizeDocumentIdentifier(documentIdentifier);
        if (pageNumber <= 0)
        {
            throw new ValidationException("A trademark document page number must be positive.", "pageNumber", "INVALID_PAGE_NUMBER", null);
        }

        string pageIdentifier = pageNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        global::System.Uri sourceUri;
        using (global::System.Net.Http.HttpRequestMessage route = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casedoc", parsed.CanonicalNumber, normalizedDocumentIdentifier, pageIdentifier, "media" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            global::System.Uri builtUri = route.RequestUri ?? new global::System.Uri("https://invalid.example/");
            sourceUri = new global::System.Uri(builtUri.OriginalString, global::System.UriKind.Absolute);
        }

        DownloadRequest resolvedRequest = TrademarkStatusContentOperation.CopyRequest(request, sourceUri);
        PatentDocumentDownloadOperation operation = new PatentDocumentDownloadOperation(Client, Options);
        operation.HostPolicy = HostPolicy;
        operation.Service = TsdrPolicy.ClassifyRateService(DocumentFormat.Unknown);
        operation.StrictImageMediaType = true;
        DownloadResult result = operation.Download(normalizedDocumentIdentifier + "-page-" + pageIdentifier, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal BufferedHttpResponse ReadMetadata(TrademarkIdentifierResult parsed, string? documentIdentifier, RequestContext sourceContext)
    {
        RequestContext context = TrademarkStatusOperation.CreateRequestContext(sourceContext);
        string[] path;
        if (documentIdentifier == null)
        {
            path = new string[] { "ts", "cd", "casedocs", parsed.CanonicalNumber, "info" };
        }
        else
        {
            path = new string[] { "ts", "cd", "casedoc", parsed.CanonicalNumber, documentIdentifier, "info" };
        }

        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, path, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Tsdr, Options, false, DocumentFormat.Xml);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                int statusCode = (int)response.StatusCode;
                string errorIdentifier = documentIdentifier ?? parsed.CanonicalNumber;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Tsdr, errorIdentifier, Options.Limits, CredentialSecrets(), context.CancellationToken);
                }

                return TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Xml, context, Options.Limits, CredentialSecrets(), timeout);
            }
        }
    }

    internal DownloadResult Transfer(string identifier, string documentIdentifier, DocumentFormat format, string routeCategory, DownloadRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A trademark document transfer request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A trademark document transfer context is required.", "Context", "REQUIRED", null);
        }

        TrademarkIdentifierResult parsed = ValidateRequest(identifier, request.Context);
        string normalizedDocumentIdentifier = NormalizeDocumentIdentifier(documentIdentifier);
        string extension = FormatExtension(format);
        global::System.Uri sourceUri;
        using (global::System.Net.Http.HttpRequestMessage route = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casedoc", parsed.CanonicalNumber, normalizedDocumentIdentifier, routeCategory + "." + extension }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            global::System.Uri builtUri = route.RequestUri ?? new global::System.Uri("https://invalid.example/");
            sourceUri = new global::System.Uri(builtUri.OriginalString, global::System.UriKind.Absolute);
        }

        DownloadRequest resolvedRequest = TrademarkStatusContentOperation.CopyRequest(request, sourceUri);
        PatentDocumentDownloadOperation operation = new PatentDocumentDownloadOperation(Client, Options);
        operation.HostPolicy = HostPolicy;
        operation.Service = TsdrPolicy.ClassifyRateService(format);
        operation.RequestedFormat = format;
        operation.StrictMediaType = true;
        DownloadResult result = operation.Download(normalizedDocumentIdentifier + "." + extension, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal static TrademarkIdentifierResult ValidateRequest(string identifier, RequestContext sourceContext)
    {
        if (sourceContext == null)
        {
            throw new ValidationException("A trademark document request context is required.", "context", "REQUIRED", null);
        }

        sourceContext.CancellationToken.ThrowIfCancellationRequested();
        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "identifier", parsed.FailureCode, null);
        }

        return parsed;
    }

    internal static string NormalizeDocumentIdentifier(string documentIdentifier)
    {
        if (documentIdentifier == null)
        {
            throw new ValidationException("A trademark document identifier is required.", "documentIdentifier", "REQUIRED", null);
        }

        string normalized = documentIdentifier.Trim();
        if (normalized.Length == 0)
        {
            throw new ValidationException("A trademark document identifier cannot be empty.", "documentIdentifier", "EMPTY_DOCUMENT_IDENTIFIER", null);
        }

        return normalized;
    }

    internal static void ValidateFormat(DocumentFormat format)
    {
        if (format != DocumentFormat.Pdf && format != DocumentFormat.Zip)
        {
            throw new ValidationException("Trademark document content requires PDF or ZIP.", "format", "UNSUPPORTED_DOCUMENT_FORMAT", null);
        }
    }

    internal static string FormatExtension(DocumentFormat format)
    {
        if (format == DocumentFormat.Pdf)
        {
            return "pdf";
        }

        if (format == DocumentFormat.Zip)
        {
            return "zip";
        }

        throw new ValidationException("Trademark document content requires PDF or ZIP.", "format", "UNSUPPORTED_DOCUMENT_FORMAT", null);
    }

    internal static string? RawResponse(BufferedHttpResponse buffered)
    {
        if (buffered.HasRawText)
        {
            return buffered.RawText;
        }

        return null;
    }

    internal string[] CredentialSecrets()
    {
        if (Options.TsdrApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.TsdrApiKey };
    }
}
