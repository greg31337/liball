namespace uspto;

internal class PetitionDocumentDownloadOperation
{
    internal PatentDocumentDownloadOperation DownloadOperation;

    internal PetitionDocumentDownloadOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        DownloadOperation = new PatentDocumentDownloadOperation(client, options);
    }

    internal DownloadResult Download(string documentIdentifier, DownloadRequest request)
    {
        if (global::System.String.IsNullOrWhiteSpace(documentIdentifier))
        {
            throw new ValidationException("A petition decision document identifier is required.", "documentIdentifier", "REQUIRED", null);
        }

        return DownloadOperation.Download(documentIdentifier, request);
    }
}
