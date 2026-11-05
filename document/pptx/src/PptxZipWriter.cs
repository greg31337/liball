using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace pptx;

internal class PptxZipWriter : IPptxPackageWriter
{
    /// <summary>
    /// Validates and writes package entries to a ZIP stream.
    /// </summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="entries">The package entries to write.</param>
    /// <param name="options">The options applied while writing the archive.</param>
    /// <returns>Diagnostics produced by validation and writing.</returns>
    public PptxResult Write(Stream stream, List<PptxZipEntry> entries, PptxZipWriteOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (entries == null)
        {
            throw new System.ArgumentNullException("entries");
        }

        PptxZipWriteOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = PptxZipWriteOptions.CreateDefault();
        }

        PptxResult result = ValidateEntries(entries);
        if (result.Failed())
        {
            return result;
        }

        List<PptxZipEntry> orderedEntries = CreateOrderedEntries(entries);

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            for (int index = 0; index < orderedEntries.Count; index++)
            {
                PptxZipEntry source = orderedEntries[index];
                string entryName = GetEntryNameForWrite(source);
                ZipArchiveEntry zipEntry = archive.CreateEntry(entryName, effectiveOptions.CompressionLevel);
                zipEntry.LastWriteTime = FixedTimestamp();

                using (Stream output = zipEntry.Open())
                {
                    output.Write(source.Bytes, 0, source.Bytes.Length);
                }
            }
        }

        return result;
    }

    internal static PptxResult ValidateEntries(List<PptxZipEntry> entries)
    {
        PptxResult result = new PptxResult();
        Dictionary<string, string> seenNames = new Dictionary<string, string>();

        for (int index = 0; index < entries.Count; index++)
        {
            PptxZipEntry entry = entries[index];
            if (entry == null)
            {
                result.AddError("ZIPW001", "ZIP entry cannot be null.", "", "", "");
                continue;
            }

            string entryName = GetEntryNameForWrite(entry);
            if (!PptxZipReader.ValidateEntryName(entryName, seenNames, result))
            {
                continue;
            }

            if (entry.Bytes == null)
            {
                result.AddError("ZIPW002", "ZIP entry bytes cannot be null.", entryName, "", "");
            }
        }

        return result;
    }

    internal static List<PptxZipEntry> CreateOrderedEntries(List<PptxZipEntry> entries)
    {
        List<PptxZipEntry> orderedEntries = new List<PptxZipEntry>();

        for (int index = 0; index < entries.Count; index++)
        {
            PptxZipEntry source = entries[index];
            string entryName = GetEntryNameForWrite(source);
            string partName = PptxZipReader.GetPartNameForEntry(entryName);
            orderedEntries.Add(new PptxZipEntry(entryName, partName, source.Bytes, source.Bytes.Length, 0));
        }

        SortEntries(orderedEntries);
        return orderedEntries;
    }

    internal static void SortEntries(List<PptxZipEntry> entries)
    {
        for (int index = 1; index < entries.Count; index++)
        {
            PptxZipEntry current = entries[index];
            int position = index - 1;

            while (position >= 0 && CompareEntryNames(entries[position].Name, current.Name) > 0)
            {
                entries[position + 1] = entries[position];
                position = position - 1;
            }

            entries[position + 1] = current;
        }
    }

    internal static int CompareEntryNames(string left, string right)
    {
        string leftKey = GetEntrySortKey(left);
        string rightKey = GetEntrySortKey(right);
        return string.Compare(leftKey, rightKey, System.StringComparison.Ordinal);
    }

    internal static string GetEntrySortKey(string entryName)
    {
        if (entryName == PptxPartNames.ContentTypesEntry)
        {
            return "000:" + entryName;
        }

        if (entryName == "_rels/.rels")
        {
            return "001:" + entryName;
        }

        return "100:" + entryName;
    }

    internal static string GetEntryNameForWrite(PptxZipEntry entry)
    {
        if (entry.Name.Length > 0)
        {
            return entry.Name;
        }

        if (entry.PartName.Length > 0 && PptxUriCore.IsValidPartName(entry.PartName))
        {
            return PptxUriCore.ToZipEntryName(entry.PartName);
        }

        return entry.PartName;
    }

    internal static System.DateTimeOffset FixedTimestamp()
    {
        return new System.DateTimeOffset(1980, 1, 1, 0, 0, 0, System.TimeSpan.Zero);
    }
}
