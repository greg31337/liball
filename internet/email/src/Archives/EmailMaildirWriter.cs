using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailMaildirDeliveryPlan
{
    internal string uniqueName;
    internal string fileName;
    internal int collisionCount;
    internal EmailMaildirEntry? overwrittenEntry;

    internal EmailMaildirDeliveryPlan(string uniqueName, string fileName, int collisionCount, EmailMaildirEntry? overwrittenEntry)
    {
        this.uniqueName = uniqueName;
        this.fileName = fileName;
        this.collisionCount = collisionCount;
        this.overwrittenEntry = overwrittenEntry;
    }
}
internal class EmailMaildirStagedFile
{
    string? path;
    FileStream? stream;
    long byteCount;
    bool complete;

    internal EmailMaildirStagedFile(string? temporaryDirectory, int maximumPathCharacters)
    {
        string fileName = ".email-stage-" + Guid.NewGuid().ToString("N");
        path = Path.GetFullPath(Path.Combine((temporaryDirectory ?? throw new InvalidOperationException()), fileName));
        if (path.Length > maximumPathCharacters) throw new EmailLimitException("MaxMaildirPathCharacters", maximumPathCharacters, path.Length);
        stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, EmailMaildirWriterConstants.FileBufferBytes, FileOptions.SequentialScan);
        byteCount = 0;
        complete = false;
    }

    internal Stream GetStream()
    {
        if (stream == null) throw new InvalidOperationException("The staged Maildir file is no longer writable.");
        return stream;
    }

    internal void Complete(bool flushToDisk)
    {
        if (stream == null) throw new InvalidOperationException("The staged Maildir file is no longer writable.");
        stream.Flush(flushToDisk);
        byteCount = stream.Length;
        stream.Close();
        stream = null;
        complete = true;
    }

    internal long GetByteCount()
    {
        if (!complete) throw new InvalidOperationException("The staged Maildir file has not been completed.");
        return byteCount;
    }

    internal void Publish(string destinationPath, bool overwrite)
    {
        if (!complete || path == null) throw new InvalidOperationException("The staged Maildir file is not ready to publish.");
        File.Move(path, destinationPath, overwrite);
        path = null;
    }

    internal void Close()
    {
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (path != null && File.Exists(path)) File.Delete(path);
        path = null;
    }

    internal void Dispose() { Close(); }
}
internal class EmailMaildirCollisionState
{
    int count;
    bool exactDestination;

    internal EmailMaildirCollisionState()
    {
        count = 0;
        exactDestination = false;
    }

    internal void Add(bool exact)
    {
        count++;
        if (exact)
            exactDestination = true;
    }

    internal int GetCount()
    {
        return count;
    }

    internal bool HasExactDestination()
    {
        return exactDestination;
    }
}
