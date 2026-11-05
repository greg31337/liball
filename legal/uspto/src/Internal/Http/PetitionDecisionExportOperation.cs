namespace uspto;

internal class PetitionDecisionExportOperation
{
    internal PatentApplicationExportOperation ExportOperation;

    internal PetitionDecisionExportOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        ExportOperation = new PatentApplicationExportOperation(client, options);
    }

    internal DownloadResult Download(PetitionDecisionExportRequest export, DownloadRequest download)
    {
        if (export == null)
        {
            throw new ValidationException("A petition decision export request is required.", "export", "REQUIRED", null);
        }

        if (export.Search == null)
        {
            throw new ValidationException("A petition decision export search is required.", "Search", "REQUIRED", null);
        }

        return ExportOperation.DownloadConfigured(export.Search, download, export.Format, new string[] { "api", "v1", "petition", "decisions", "search", "download" }, "petition decision search export", "petition-decision-search");
    }
}
