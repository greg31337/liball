using System.IO.Compression;
using System.Text;

namespace docx;

internal class DocxZipWriter
{
    internal void WriteEntries(Stream stream, List<DocxZipWriteEntry> entries)
    {
        DocxSaveOptions options = new DocxSaveOptions();
        WriteEntriesWithOptions(stream, entries, options);
    }

    internal void WriteEntriesWithOptions(Stream stream, List<DocxZipWriteEntry> entries, DocxSaveOptions options)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        bool deterministic = options.UsesDeterministicOutput();
        List<DocxZipWriteEntry> orderedEntries = GetOrderedEntries(entries, deterministic);
        CompressionLevel compressionLevel = GetCompressionLevel(options.CompressionMode);

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (DocxZipWriteEntry writeEntry in orderedEntries)
            {
                ZipArchiveEntry archiveEntry = archive.CreateEntry(writeEntry.FullName, compressionLevel);

                if (deterministic)
                {
                    archiveEntry.LastWriteTime = GetDeterministicTimestamp();
                }

                if (!writeEntry.IsDirectory)
                {
                    using (Stream entryStream = archiveEntry.Open())
                    {
                        entryStream.Write(writeEntry.Bytes, 0, writeEntry.Bytes.Length);
                    }
                }
            }
        }
    }

    internal void WriteEntriesToFile(string path, List<DocxZipWriteEntry> entries, DocxSaveOptions options)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        using (FileStream stream = File.Create(path))
        {
            WriteEntriesWithOptions(stream, entries, options);
        }
    }

    internal DocxZipWriteEntry CreateXmlEntry(string fullName, string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return new DocxZipWriteEntry(fullName, Encoding.UTF8.GetBytes(xml), false);
    }

    internal DocxZipWriteEntry CreateBinaryEntry(string fullName, byte[] bytes)
    {
        return new DocxZipWriteEntry(fullName, CopyBytes(bytes), false);
    }

    internal DocxZipWriteEntry CreateDirectoryEntry(string fullName)
    {
        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        string directoryName = fullName;
        if (!directoryName.EndsWith("/"))
        {
            directoryName = directoryName + "/";
        }

        return new DocxZipWriteEntry(directoryName, new byte[0], true);
    }

    internal List<DocxZipWriteEntry> GetOrderedEntries(List<DocxZipWriteEntry> entries, bool deterministic)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        List<DocxZipWriteEntry> orderedEntries = new List<DocxZipWriteEntry>();

        foreach (DocxZipWriteEntry entry in entries)
        {
            orderedEntries.Add(entry);
        }

        if (!deterministic)
        {
            return orderedEntries;
        }

        for (int index = 1; index < orderedEntries.Count; index = index + 1)
        {
            DocxZipWriteEntry current = orderedEntries[index];
            int previousIndex = index - 1;

            while (previousIndex >= 0 && string.CompareOrdinal(orderedEntries[previousIndex].FullName, current.FullName) > 0)
            {
                orderedEntries[previousIndex + 1] = orderedEntries[previousIndex];
                previousIndex = previousIndex - 1;
            }

            orderedEntries[previousIndex + 1] = current;
        }

        return orderedEntries;
    }

    internal CompressionLevel GetCompressionLevel(DocxCompressionMode mode)
    {
        switch (mode)
        {
            case DocxCompressionMode.Optimal:
                return CompressionLevel.Optimal;
            case DocxCompressionMode.Fastest:
                return CompressionLevel.Fastest;
            case DocxCompressionMode.NoCompression:
                return CompressionLevel.NoCompression;
            default:
                throw new ArgumentOutOfRangeException("mode");
        }
    }

    internal DateTimeOffset GetDeterministicTimestamp()
    {
        return new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    internal byte[] CopyBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        byte[] copy = new byte[bytes.Length];

        for (int index = 0; index < bytes.Length; index = index + 1)
        {
            copy[index] = bytes[index];
        }

        return copy;
    }
}

internal class DocxZipWriteEntry
{
    internal string FullName;
    internal byte[] Bytes;
    internal bool IsDirectory;

    internal DocxZipWriteEntry(string fullName, byte[] bytes, bool isDirectory)
    {
        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        this.FullName = fullName;
        this.Bytes = bytes;
        this.IsDirectory = isDirectory;
    }
}
