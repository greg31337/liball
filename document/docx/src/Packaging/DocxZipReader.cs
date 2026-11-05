using System.IO.Compression;
using System.Text;

namespace docx;

internal class DocxZipReader
{
    internal List<DocxZipEntry> ReadEntries(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        List<DocxZipEntry> entries = new List<DocxZipEntry>();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            foreach (ZipArchiveEntry archiveEntry in archive.Entries)
            {
                DocxZipEntry entry = new DocxZipEntry(
                    archiveEntry.FullName,
                    archiveEntry.Name,
                    archiveEntry.Length,
                    archiveEntry.CompressedLength,
                    IsDirectoryEntry(archiveEntry));

                entries.Add(entry);
            }
        }

        return entries;
    }

    internal List<DocxZipEntry> ReadEntriesFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        using (FileStream stream = File.OpenRead(path))
        {
            return ReadEntries(stream);
        }
    }

    internal bool HasEntry(Stream stream, string fullName)
    {
        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        List<DocxZipEntry> entries = ReadEntries(stream);

        foreach (DocxZipEntry entry in entries)
        {
            if (entry.FullName == fullName)
            {
                return true;
            }
        }

        return false;
    }

    internal string ReadXmlEntry(Stream stream, string fullName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            ZipArchiveEntry? entry = FindEntry(archive, fullName);

            if (entry == null)
            {
                throw new FileNotFoundException("ZIP entry was not found.", fullName);
            }

            using (Stream entryStream = entry.Open())
            {
                using (StreamReader reader = new StreamReader(entryStream, Encoding.UTF8, true, 1024, false))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    internal byte[] ReadBinaryEntry(Stream stream, string fullName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            ZipArchiveEntry? entry = FindEntry(archive, fullName);

            if (entry == null)
            {
                throw new FileNotFoundException("ZIP entry was not found.", fullName);
            }

            using (Stream entryStream = entry.Open())
            {
                using (MemoryStream bytes = new MemoryStream())
                {
                    entryStream.CopyTo(bytes);
                    return bytes.ToArray();
                }
            }
        }
    }

    internal ZipArchiveEntry? FindEntry(ZipArchive archive, string fullName)
    {
        if (archive == null)
        {
            throw new ArgumentNullException("archive");
        }

        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName == fullName)
            {
                return entry;
            }
        }

        return null;
    }

    internal bool IsDirectoryEntry(ZipArchiveEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException("entry");
        }

        return entry.FullName.EndsWith("/");
    }
}

internal class DocxZipEntry
{
    internal string FullName;
    internal string Name;
    internal long Length;
    internal long CompressedLength;
    internal bool IsDirectory;

    internal DocxZipEntry(string fullName, string name, long length, long compressedLength, bool isDirectory)
    {
        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        this.FullName = fullName;
        this.Name = name;
        this.Length = length;
        this.CompressedLength = compressedLength;
        this.IsDirectory = isDirectory;
    }
}
