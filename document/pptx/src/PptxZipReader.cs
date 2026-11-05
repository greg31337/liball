using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace pptx;

internal class PptxZipReader : IPptxPackageReader
{
    /// <summary>
    /// Reads and validates package entries from a ZIP stream.
    /// </summary>
    /// <param name="stream">The readable ZIP stream.</param>
    /// <param name="options">The limits applied while reading the archive.</param>
    /// <returns>The entries and diagnostics produced by the read.</returns>
    public PptxZipReadResult Read(Stream stream, PptxZipReadOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The stream must be readable.");
        }

        PptxZipReadOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = PptxZipReadOptions.CreateDefault();
        }

        effectiveOptions.Validate();

        PptxZipReadResult result = new PptxZipReadResult();
        Dictionary<string, string> seenNames = new Dictionary<string, string>();
        long totalBytes = 0;

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            for (int index = 0; index < archive.Entries.Count; index++)
            {
                ZipArchiveEntry entry = archive.Entries[index];

                if (index + 1 > effectiveOptions.MaxEntryCount)
                {
                    result.Diagnostics.AddError("ZIP001", "ZIP entry count exceeds the configured limit.", entry.FullName, "", "");
                    continue;
                }

                if (!ValidateEntryName(entry.FullName, seenNames, result.Diagnostics))
                {
                    continue;
                }

                if (!ValidateEntrySize(entry, effectiveOptions, totalBytes, result.Diagnostics))
                {
                    continue;
                }

                totalBytes = totalBytes + entry.Length;
                string partName = GetPartNameForEntry(entry.FullName);
                byte[] bytes = ReadEntryBytes(entry);
                result.AddEntry(new PptxZipEntry(entry.FullName, partName, bytes, entry.Length, entry.CompressedLength));
            }
        }

        return result;
    }

    internal static bool ValidateEntryName(string entryName, Dictionary<string, string> seenNames, PptxResult diagnostics)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            diagnostics.AddError("ZIP010", "ZIP entry name is required.", "", "", "");
            return false;
        }

        if (entryName[entryName.Length - 1] == '/')
        {
            diagnostics.AddError("ZIP011", "ZIP directory entries are not allowed.", entryName, "", "");
            return false;
        }

        if (HasUnsafeEntryNameCharacters(entryName))
        {
            diagnostics.AddError("ZIP012", "ZIP entry name contains unsafe characters.", entryName, "", "");
            return false;
        }

        if (entryName == PptxPartNames.ContentTypesEntry)
        {
            if (seenNames.ContainsKey(entryName))
            {
                diagnostics.AddError("ZIP013", "ZIP entry name is duplicated.", entryName, "", "");
                return false;
            }

            seenNames.Add(entryName, entryName);
            return true;
        }

        PptxResult partNameResult = PptxUriCore.ValidatePartName(entryName);
        if (partNameResult.Failed())
        {
            PptxUriCore.CopyDiagnostics(partNameResult, diagnostics);
            return false;
        }

        string partName = PptxUriCore.NormalizePartName(entryName);
        if (seenNames.ContainsKey(partName))
        {
            diagnostics.AddError("ZIP013", "ZIP entry name is duplicated.", entryName, "", "");
            return false;
        }

        seenNames.Add(partName, entryName);
        return true;
    }

    internal static bool ValidateEntrySize(ZipArchiveEntry entry, PptxZipReadOptions options, long currentTotalBytes, PptxResult diagnostics)
    {
        if (entry.Length > options.MaxEntryBytes)
        {
            diagnostics.AddError("ZIP020", "ZIP entry inflated size exceeds the configured limit.", entry.FullName, "", "");
            return false;
        }

        if (currentTotalBytes < 0L || currentTotalBytes > options.MaxTotalBytes || entry.Length > options.MaxTotalBytes - currentTotalBytes)
        {
            diagnostics.AddError("ZIP021", "ZIP total inflated size exceeds the configured limit.", entry.FullName, "", "");
            return false;
        }

        if (options.MaxCompressionRatio > 0.0 && entry.Length > 0)
        {
            if (entry.CompressedLength <= 0)
            {
                diagnostics.AddError("ZIP022", "ZIP entry has an unsafe compression ratio.", entry.FullName, "", "");
                return false;
            }

            double ratio = (double)entry.Length / (double)entry.CompressedLength;
            if (ratio > options.MaxCompressionRatio)
            {
                diagnostics.AddError("ZIP022", "ZIP entry has an unsafe compression ratio.", entry.FullName, "", "");
                return false;
            }
        }

        return true;
    }

    internal static bool HasUnsafeEntryNameCharacters(string entryName)
    {
        if (entryName[0] == '/' || entryName[0] == '\\')
        {
            return true;
        }

        for (int index = 0; index < entryName.Length; index++)
        {
            char current = entryName[index];

            if (current <= ' ')
            {
                return true;
            }

            if (current == '\\' || current == '"' || current == '<' || current == '>' || current == '|')
            {
                return true;
            }
        }

        return false;
    }

    internal static string GetPartNameForEntry(string entryName)
    {
        if (entryName == PptxPartNames.ContentTypesEntry)
        {
            return "";
        }

        return PptxUriCore.NormalizePartName(entryName);
    }

    internal static byte[] ReadEntryBytes(ZipArchiveEntry entry)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (Stream input = entry.Open())
            {
                input.CopyTo(output);
            }

            return output.ToArray();
        }
    }
}
