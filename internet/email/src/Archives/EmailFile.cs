using System;
using System.IO;

namespace email;
internal class EmailFileStagedSave
{
    string? path;

    internal EmailFileStagedSave(string path)
    {
        this.path = path;
    }

    internal void Write(byte[] bytes, bool flushToDisk)
    {
        using (FileStream output = new FileStream((path ?? throw new InvalidOperationException()), FileMode.CreateNew, FileAccess.Write, FileShare.None, EmailFileConstants.FileBufferBytes, FileOptions.SequentialScan))
        {
            output.Write(bytes, 0, bytes.Length);
            output.Flush(flushToDisk);
        }
    }

    internal void Publish(string destinationPath, bool overwrite)
    {
        if (path == null) throw new InvalidOperationException("The staged .eml file is no longer available.");
        File.Move(path, destinationPath, overwrite);
        path = null;
    }

    internal void Close()
    {
        if (path != null && File.Exists(path)) File.Delete(path);
        path = null;
    }
}
