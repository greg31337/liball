namespace uspto;

internal class PtabProceedingExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PtabProceedingExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(SearchRequest search, DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A PTAB proceeding export download request is required.", "download", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "proceedings", "search", "download" }, "PTAB proceeding search export", "ptab-proceeding-search");
    }
}
