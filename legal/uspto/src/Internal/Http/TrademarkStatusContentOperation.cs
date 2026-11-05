namespace uspto;

internal class TrademarkStatusContentOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal TrademarkStatusContentOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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
        ValidateFormat(format, true);
        return Transfer(identifier, format, "content", request);
    }

    internal DownloadResult Download(string identifier, DocumentFormat format, DownloadRequest request)
    {
        ValidateFormat(format, false);
        return Transfer(identifier, format, "download", request);
    }

    internal DownloadResult Transfer(string identifier, DocumentFormat format, string routeCategory, DownloadRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A trademark status content request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A trademark status content context is required.", "Context", "REQUIRED", null);
        }

        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "identifier", parsed.FailureCode, null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        string extension = FormatExtension(format);
        global::System.Uri sourceUri;
        using (global::System.Net.Http.HttpRequestMessage route = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casestatus", parsed.CanonicalNumber, routeCategory + "." + extension }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            global::System.Uri builtUri = route.RequestUri ?? new global::System.Uri("https://invalid.example/");
            sourceUri = new global::System.Uri(builtUri.OriginalString, global::System.UriKind.Absolute);
        }

        DownloadRequest resolvedRequest = CopyRequest(request, sourceUri);
        PatentDocumentDownloadOperation operation = new PatentDocumentDownloadOperation(Client, Options);
        operation.HostPolicy = HostPolicy;
        operation.Service = TsdrPolicy.ClassifyRateService(format);
        operation.RequestedFormat = format;
        operation.StrictMediaType = true;
        DownloadResult result = operation.Download(parsed.CanonicalNumber + "." + extension, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal static DownloadRequest CopyRequest(DownloadRequest source, global::System.Uri sourceUri)
    {
        DownloadRequest result = new DownloadRequest();
        result.ExportFormat = source.ExportFormat;
        result.SourceUri = sourceUri;
        result.Output = source.Output;
        result.DestinationPath = source.DestinationPath;
        result.OverwriteExisting = source.OverwriteExisting;
        result.ResumePartial = source.ResumePartial;
        result.ExpectedSize = source.ExpectedSize;
        result.HasExpectedSize = source.HasExpectedSize;
        result.ExpectedChecksums = source.ExpectedChecksums;
        result.Progress = source.Progress;
        result.Context = source.Context;
        return result;
    }

    internal static void ValidateFormat(DocumentFormat format, bool contentRoute)
    {
        bool supported = format == DocumentFormat.Pdf || format == DocumentFormat.Zip;
        if (contentRoute && format == DocumentFormat.Html)
        {
            supported = true;
        }

        if (!supported)
        {
            string failureCode = contentRoute ? "UNSUPPORTED_STATUS_CONTENT_FORMAT" : "UNSUPPORTED_STATUS_DOWNLOAD_FORMAT";
            throw new ValidationException("The requested trademark status representation is not supported by this route.", "format", failureCode, null);
        }
    }

    internal static string FormatExtension(DocumentFormat format)
    {
        switch (format)
        {
            case DocumentFormat.Html:
                return "html";
            case DocumentFormat.Pdf:
                return "pdf";
            case DocumentFormat.Zip:
                return "zip";
            default:
                throw new ValidationException("The trademark status representation is not supported.", "format", "UNSUPPORTED_FORMAT", null);
        }
    }
}
