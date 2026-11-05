using System;
using System.IO;

namespace tftp;

internal class TftpPendingClientDownload : IDisposable
{
    internal string DestinationPath;
    internal string TemporaryPath;
    internal FileStream Stream;
    internal bool Published = false;

    internal TftpPendingClientDownload(string destinationPath)
    {
        string? directoryPath = Path.GetDirectoryName(destinationPath);
        if (directoryPath == null || directoryPath.Length == 0)
        {
            throw new TftpProtocolException(
                "The client download destination directory is invalid.",
                false,
                0);
        }

        string destinationName = Path.GetFileName(destinationPath);
        string temporaryName = "." + destinationName + "." +
            Guid.NewGuid().ToString("N") + ".tftp.tmp";
        string temporaryPath = Path.Combine(directoryPath, temporaryName);
        DestinationPath = destinationPath;
        TemporaryPath = temporaryPath;
        Stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.SequentialScan);
    }

    internal void PublishDownload()
    {
        Stream.Flush(true);
        Stream.Close();
        File.Move(TemporaryPath, DestinationPath, true);
        Published = true;
    }

    /// <summary>
    /// Closes the pending stream and removes unpublished temporary content.
    /// </summary>
    public void Dispose()
    {
        Stream.Dispose();
        if (!Published && File.Exists(TemporaryPath))
        {
            File.Delete(TemporaryPath);
        }
    }
}
