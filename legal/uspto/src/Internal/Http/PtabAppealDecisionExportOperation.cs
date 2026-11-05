namespace uspto;

internal class PtabAppealDecisionExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PtabAppealDecisionExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(SearchRequest search, DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A PTAB appeal decision export download request is required.", "download", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "appeals", "decisions", "search", "download" }, "PTAB appeal decision search export", "ptab-appeal-decision-search");
    }
}
