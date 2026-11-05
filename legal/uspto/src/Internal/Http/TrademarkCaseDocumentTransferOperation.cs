namespace uspto;

internal class TrademarkCaseDocumentTransferOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal TrademarkCaseDocumentTransferOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal DownloadResult GetContent(string identifier, DocumentFormat format, DownloadRequest request)
    {
        ValidateCaseDocumentFormat(format);
        return TransferCase(identifier, format, "content." + FormatExtension(format), request);
    }

    internal DownloadResult DownloadDocuments(string identifier, DocumentFormat format, DownloadRequest request)
    {
        ValidateCaseDocumentFormat(format);
        return TransferCase(identifier, format, "download." + FormatExtension(format), request);
    }

    internal DownloadResult DownloadBundle(string identifier, DownloadRequest request)
    {
        return TransferCase(identifier, DocumentFormat.Xml, "bundle", request);
    }

    internal DownloadResult TransferCase(string identifier, DocumentFormat format, string routeName, DownloadRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A trademark case-document transfer request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A trademark case-document transfer context is required.", "Context", "REQUIRED", null);
        }

        TrademarkIdentifierResult parsed = TrademarkDocumentOperation.ValidateRequest(identifier, request.Context);
        global::System.Uri sourceUri;
        using (global::System.Net.Http.HttpRequestMessage route = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casedocs", parsed.CanonicalNumber, routeName }, new global::System.Collections.Generic.Dictionary<string, string?>()))
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
        operation.IsBundleRequest = true;
        string extension = FormatExtension(format);
        DownloadResult result = operation.Download(parsed.CanonicalNumber + "-documents." + extension, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal static void ValidateCaseDocumentFormat(DocumentFormat format)
    {
        if (format != DocumentFormat.Pdf && format != DocumentFormat.Zip)
        {
            throw new ValidationException("Trademark case documents require PDF or ZIP.", "format", "UNSUPPORTED_CASE_DOCUMENT_FORMAT", null);
        }
    }

    internal static string FormatExtension(DocumentFormat format)
    {
        switch (format)
        {
            case DocumentFormat.Xml:
                return "xml";
            case DocumentFormat.Pdf:
                return "pdf";
            case DocumentFormat.Zip:
                return "zip";
            default:
                throw new ValidationException("The trademark case-document representation is unsupported.", "format", "UNSUPPORTED_CASE_DOCUMENT_FORMAT", null);
        }
    }
}
