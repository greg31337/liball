namespace uspto;

internal class PtabTrialDecisionExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PtabTrialDecisionExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(SearchRequest search, DownloadRequest download)
    {
        if (download == null)
        {
            throw new ValidationException("A PTAB trial decision export download request is required.", "download", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(search, download, download.ExportFormat, new string[] { "api", "v1", "patent", "trials", "decisions", "search", "download" }, "PTAB trial decision search export", "ptab-trial-decision-search");
    }
}
