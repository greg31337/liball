namespace uspto;

internal class PtabInterferenceDecisionExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PtabInterferenceDecisionExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(SearchRequest search, DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A PTAB interference decision export download request is required.", "download", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "interferences", "decisions", "search", "download" }, "PTAB interference decision search export", "ptab-interference-decision-search");
    }
}
