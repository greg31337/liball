namespace uspto;

internal class PtabDocumentDownloadOperation
{
    internal PatentDocumentDownloadOperation DownloadOperation;
    internal UsptoClientOptions Options;

    internal PtabDocumentDownloadOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Options = options;
        DownloadOperation = new PatentDocumentDownloadOperation(client, options);
        DownloadOperation.AdditionalSupportedMediaTypes = new string[] { UsptoMediaTypes.Json };
    }

    internal DownloadResult Download(PtabCollection collection, string documentIdentifier, DownloadRequest request)
    {
        ValidateCollection(collection);
        if (global::System.String.IsNullOrWhiteSpace(documentIdentifier))
        {
            throw new ValidationException("A PTAB document identifier is required.", "documentIdentifier", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("A PTAB document download request is required.", "request", "REQUIRED", null);
        }

        DownloadRequest resolvedRequest = ResolveRequest(request);
        DownloadResult result = DownloadOperation.Download(documentIdentifier, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal DownloadRequest ResolveRequest(DownloadRequest request)
    {
        DownloadRequest result = new DownloadRequest();
        result.ExportFormat = request.ExportFormat;
        result.SourceUri = ResolveSourceUri(request.SourceUri);
        result.Output = request.Output;
        result.DestinationPath = request.DestinationPath;
        result.OverwriteExisting = request.OverwriteExisting;
        result.ResumePartial = request.ResumePartial;
        result.ExpectedSize = request.ExpectedSize;
        result.HasExpectedSize = request.HasExpectedSize;
        result.ExpectedChecksums = request.ExpectedChecksums;
        result.Progress = request.Progress;
        result.Context = request.Context;
        return result;
    }

    internal global::System.Uri? ResolveSourceUri(global::System.Uri? sourceUri)
    {
        if (sourceUri == null || sourceUri.IsAbsoluteUri)
        {
            return sourceUri;
        }

        if (Options.OdpBaseUri == null || !Options.OdpBaseUri.IsAbsoluteUri)
        {
            throw new ValidationException("An absolute ODP base URI is required to resolve a relative PTAB document URI.", "OdpBaseUri", "ABSOLUTE_URI_REQUIRED", null);
        }

        return new global::System.Uri(Options.OdpBaseUri, sourceUri);
    }

    internal static void ValidateCollection(PtabCollection collection)
    {
        bool supported = collection == PtabCollection.TrialDecisions
            || collection == PtabCollection.TrialDocuments
            || collection == PtabCollection.AppealDecisions
            || collection == PtabCollection.InterferenceDecisions;
        if (!supported)
        {
            throw new ValidationException("The PTAB collection does not expose downloadable documents.", "collection", "UNSUPPORTED_PTAB_DOCUMENT_COLLECTION", null);
        }
    }
}
