using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace CurrencyCodes.DataGenerator;

internal sealed class SourceChecksumManifestReader
{
    internal SourceChecksumManifest Read(string manifestPath, string repositoryRoot)
    {
        string[] lines = File.ReadAllLines(manifestPath);
        if (lines.Length < 5)
        {
            throw Invalid("The source-checksum manifest is incomplete.");
        }

        int manifestVersion = ParseManifestVersion(lines[0]);
        string isoPublicationDate = ParseDateMetadata(lines[1], "iso-publication-date");
        string cldrRelease = ParseCldrRelease(lines[2]);
        string retrievalDate = ParseDateMetadata(lines[3], "retrieval-date");
        List<SourceChecksumEntry> entries = new List<SourceChecksumEntry>();
        string? previousPath = null;
        for (int lineIndex = 4; lineIndex < lines.Length; lineIndex++)
        {
            SourceChecksumEntry entry = ParseEntry(lines[lineIndex], lineIndex + 1);
            if (previousPath != null
                && string.CompareOrdinal(previousPath, entry.Path) >= 0)
            {
                throw Invalid(
                    "Source-checksum entries are not in unique canonical path order at line "
                        + (lineIndex + 1).ToString(CultureInfo.InvariantCulture)
                        + ".");
            }

            VerifyEntry(entry, repositoryRoot);
            entries.Add(entry);
            previousPath = entry.Path;
        }

        ValidateDeclaredSourceVersions(
            isoPublicationDate,
            cldrRelease,
            entries,
            repositoryRoot);

        return new SourceChecksumManifest(
            manifestVersion,
            isoPublicationDate,
            cldrRelease,
            retrievalDate,
            entries);
    }

    private static void ValidateDeclaredSourceVersions(
        string isoPublicationDate,
        string cldrRelease,
        IReadOnlyList<SourceChecksumEntry> entries,
        string repositoryRoot)
    {
        string fullRoot = Path.GetFullPath(repositoryRoot);
        string listOnePath = Path.Combine(fullRoot, "specs", "iso-4217-list-one.xml");
        string listThreePath = Path.Combine(fullRoot, "specs", "iso-4217-list-three.xml");
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        if (listOneData.PublicationDate != isoPublicationDate
            || listThreeData.PublicationDate != isoPublicationDate)
        {
            throw Invalid(
                "The manifest ISO publication date does not match List One and List Three.");
        }

        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (SourceChecksumEntry entry in entries)
        {
            paths.Add(entry.Path);
        }

        string cldrPrefix = "specs/cldr-" + cldrRelease + "-";
        RequireManifestPath(paths, cldrPrefix + "license.txt");
        RequireManifestPath(paths, cldrPrefix + "numberingSystems.xml");
        RequireManifestPath(paths, cldrPrefix + "supplementalData.xml");
        RequireManifestPath(paths, cldrPrefix + "supplementalMetadata.xml");
    }

    private static void RequireManifestPath(HashSet<string> paths, string path)
    {
        if (!paths.Contains(path))
        {
            throw Invalid("The manifest CLDR release does not match required path '" + path + "'.");
        }
    }

    private static int ParseManifestVersion(string line)
    {
        string value = ParseMetadata(line, "manifest-version");
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int version)
            || version != 1)
        {
            throw Invalid("The source-checksum manifest version must be '1'.");
        }

        return version;
    }

    private static string ParseDateMetadata(string line, string name)
    {
        string value = ParseMetadata(line, name);
        if (!DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly parsed)
            || parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != value)
        {
            throw Invalid("Source-checksum metadata '" + name + "' must be a canonical date.");
        }

        return value;
    }

    private static string ParseCldrRelease(string line)
    {
        string value = ParseMetadata(line, "cldr-release");
        bool previousWasDot = true;
        foreach (char character in value)
        {
            if (character == '.')
            {
                if (previousWasDot)
                {
                    throw Invalid("The CLDR release must contain dot-separated ASCII numbers.");
                }

                previousWasDot = true;
            }
            else if (character >= '0' && character <= '9')
            {
                previousWasDot = false;
            }
            else
            {
                throw Invalid("The CLDR release must contain dot-separated ASCII numbers.");
            }
        }

        if (previousWasDot)
        {
            throw Invalid("The CLDR release must contain dot-separated ASCII numbers.");
        }

        return value;
    }

    private static string ParseMetadata(string line, string expectedName)
    {
        string[] fields = line.Split('\t');
        if (fields.Length != 2 || fields[0] != expectedName || fields[1].Length == 0)
        {
            throw Invalid("Expected source-checksum metadata '" + expectedName + "'.");
        }

        return fields[1];
    }

    private static SourceChecksumEntry ParseEntry(string line, int lineNumber)
    {
        string[] fields = line.Split('\t');
        if (fields.Length != 4 || fields[0] != "file")
        {
            throw Invalid("Invalid source-checksum file entry at line " + FormatInteger(lineNumber) + ".");
        }

        ValidateRelativePath(fields[1], lineNumber);
        if (!long.TryParse(
            fields[2],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long byteLength)
            || byteLength < 0
            || byteLength.ToString(CultureInfo.InvariantCulture) != fields[2])
        {
            throw Invalid("Invalid source byte length at line " + FormatInteger(lineNumber) + ".");
        }

        if (!IsLowercaseSha256(fields[3]))
        {
            throw Invalid("Invalid SHA-256 digest at line " + FormatInteger(lineNumber) + ".");
        }

        return new SourceChecksumEntry(fields[1], byteLength, fields[3]);
    }

    private static void ValidateRelativePath(string path, int lineNumber)
    {
        if (!path.StartsWith("specs/", StringComparison.Ordinal)
            || path.IndexOf('\\') >= 0
            || path.IndexOf(':') >= 0)
        {
            throw Invalid("Invalid source path at line " + FormatInteger(lineNumber) + ".");
        }

        string[] segments = path.Split('/');
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                throw Invalid("Invalid source path at line " + FormatInteger(lineNumber) + ".");
            }
        }
    }

    private static void VerifyEntry(SourceChecksumEntry entry, string repositoryRoot)
    {
        string fullRoot = Path.GetFullPath(repositoryRoot);
        string nativePath = entry.Path.Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, nativePath));
        string relativePath = Path.GetRelativePath(fullRoot, fullPath);
        if (Path.IsPathRooted(relativePath)
            || relativePath == ".."
            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw Invalid("Source path escapes the repository root: '" + entry.Path + "'.");
        }

        FileInfo file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            throw Invalid("Manifest source file is missing: '" + entry.Path + "'.");
        }

        if (file.Length != entry.ByteLength)
        {
            throw Invalid("Source byte length differs from the manifest for '" + entry.Path + "'.");
        }

        string actualDigest = ComputeSha256(fullPath);
        if (!string.Equals(actualDigest, entry.Sha256, StringComparison.Ordinal))
        {
            throw Invalid("Source SHA-256 differs from the manifest for '" + entry.Path + "'.");
        }
    }

    internal static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsLowercaseSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!((character >= '0' && character <= '9')
                || (character >= 'a' && character <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatInteger(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static InvalidDataException Invalid(string message)
    {
        return new InvalidDataException(message);
    }
}
