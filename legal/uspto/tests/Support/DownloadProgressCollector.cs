using uspto;

namespace uspto.Tests;

internal class RecordingDownloadProgress : IProgress<DownloadProgress>
{
    internal List<DownloadProgress> Reports = new List<DownloadProgress>();

    public void Report(DownloadProgress value)
    {
        Reports.Add(value);
    }
}
