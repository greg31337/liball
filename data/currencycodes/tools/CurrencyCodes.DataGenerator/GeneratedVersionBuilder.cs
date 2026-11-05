using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class GeneratedVersionBuilder
{
    private const string GeneratorSchemaVersion = "1";

    internal GeneratedVersionData Build(SourceChecksumManifest manifest, string repositoryRoot)
    {
        List<SourceChecksumEntry> checksums = new List<SourceChecksumEntry>(manifest.Entries);
        AddReviewedOverlayChecksums(checksums, repositoryRoot);
        checksums.Sort(CompareChecksumEntries);
        ValidateUniqueChecksumPaths(checksums);
        string snapshotIdentifier = ComputeSnapshotIdentifier(manifest, checksums);
        List<CanonicalCSharpField> fields = new List<CanonicalCSharpField>
        {
            CreateStringField("SnapshotIdentifier", snapshotIdentifier),
            CreateStringField("IsoPublicationDate", manifest.IsoPublicationDate),
            CreateStringField("GeneratorSchemaVersion", GeneratorSchemaVersion),
            CreateStringField("CldrVersion", manifest.CldrRelease)
        };
        List<CanonicalCSharpRow> checksumRows = new List<CanonicalCSharpRow>();
        foreach (SourceChecksumEntry checksum in checksums)
        {
            string value = checksum.Path
                + "\t"
                + checksum.ByteLength.ToString(CultureInfo.InvariantCulture)
                + "\t"
                + checksum.Sha256;
            checksumRows.Add(new CanonicalCSharpRow(checksum.Path, value));
        }

        List<CanonicalCSharpTable> tables = new List<CanonicalCSharpTable>
        {
            new CanonicalCSharpTable("SourceChecksums", "string", false, checksumRows)
        };
        CanonicalCSharpFile file = new CanonicalCSharpFile(
            "CurrencyCodes.Generated",
            "GeneratedDataVersions",
            fields,
            tables);
        return new GeneratedVersionData(
            file,
            manifest.IsoPublicationDate,
            manifest.CldrRelease,
            GeneratorSchemaVersion,
            snapshotIdentifier,
            checksums);
    }

    private static CanonicalCSharpField CreateStringField(string name, string value)
    {
        return new CanonicalCSharpField(
            name,
            "string",
            true,
            CanonicalCSharpValue.String(value));
    }

    private static void AddReviewedOverlayChecksums(
        List<SourceChecksumEntry> checksums,
        string repositoryRoot)
    {
        string fullRoot = Path.GetFullPath(repositoryRoot);
        string overlayDirectory = Path.Combine(fullRoot, "data", "overrides");
        if (!Directory.Exists(overlayDirectory))
        {
            throw new InvalidDataException("The reviewed overlay directory is missing.");
        }

        string[] overlayPaths = Directory.GetFiles(
            overlayDirectory,
            "*.csv",
            SearchOption.TopDirectoryOnly);
        if (overlayPaths.Length == 0)
        {
            throw new InvalidDataException("The reviewed overlay directory contains no CSV inputs.");
        }

        foreach (string overlayPath in overlayPaths)
        {
            FileInfo file = new FileInfo(overlayPath);
            string relativePath = Path.GetRelativePath(fullRoot, overlayPath)
                .Replace(Path.DirectorySeparatorChar, '/');
            checksums.Add(new SourceChecksumEntry(
                relativePath,
                file.Length,
                SourceChecksumManifestReader.ComputeSha256(overlayPath)));
        }
    }

    private static int CompareChecksumEntries(SourceChecksumEntry left, SourceChecksumEntry right)
    {
        return string.CompareOrdinal(left.Path, right.Path);
    }

    private static void ValidateUniqueChecksumPaths(IReadOnlyList<SourceChecksumEntry> checksums)
    {
        string? previousPath = null;
        foreach (SourceChecksumEntry checksum in checksums)
        {
            if (string.Equals(previousPath, checksum.Path, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Generated source checksums repeat path '" + checksum.Path + "'.");
            }

            previousPath = checksum.Path;
        }
    }

    private static string ComputeSnapshotIdentifier(
        SourceChecksumManifest manifest,
        IReadOnlyList<SourceChecksumEntry> checksums)
    {
        StringBuilder payload = new StringBuilder();
        AppendPayloadLine(payload, "generator-schema-version", GeneratorSchemaVersion);
        AppendPayloadLine(
            payload,
            "manifest-version",
            manifest.ManifestVersion.ToString(CultureInfo.InvariantCulture));
        AppendPayloadLine(payload, "iso-publication-date", manifest.IsoPublicationDate);
        AppendPayloadLine(payload, "cldr-version", manifest.CldrRelease);
        foreach (SourceChecksumEntry checksum in checksums)
        {
            payload.Append("source\t");
            payload.Append(checksum.Path);
            payload.Append('\t');
            payload.Append(checksum.ByteLength.ToString(CultureInfo.InvariantCulture));
            payload.Append('\t');
            payload.Append(checksum.Sha256);
            payload.Append('\n');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString()));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static void AppendPayloadLine(StringBuilder payload, string name, string value)
    {
        payload.Append(name);
        payload.Append('\t');
        payload.Append(value);
        payload.Append('\n');
    }
}
