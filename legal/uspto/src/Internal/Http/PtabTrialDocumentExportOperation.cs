namespace uspto;

internal class PtabTrialDocumentExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PtabTrialDocumentExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(SearchRequest search, DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A PTAB trial document export download request is required.", "download", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "trials", "documents", "search", "download" }, "PTAB trial document search export", "ptab-trial-document-search");
    }
}
