using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ZipPackageSecurityLimitTests
{
    [Test]
    public void EntryCountLimitStopsBeforeEntryInspection()
    {
        byte[] archive = CreateArchive(
            ["first.txt", "second.txt"],
            ["one", "two"],
            [CompressionLevel.NoCompression, CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumPackageEntries = 1;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Is.Empty);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.EntryLimitExceeded).Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
        });
    }

    [Test]
    public void TotalExpandedSizeLimitStopsTheEntryThatCrossesTheBoundary()
    {
        byte[] archive = CreateArchive(
            ["first.txt", "second.txt"],
            ["1234", "5678"],
            [CompressionLevel.NoCompression, CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumTotalExpandedBytes = 7;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries, Has.Length.EqualTo(2));
            Assert.That(inspection.Entries[0].HasValidCrc, Is.True);
            Assert.That(inspection.Entries[1].HasValidCrc, Is.False);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.ExpandedSizeLimitExceeded).Location.PartPath, Is.EqualTo("second.txt"));
        });
    }

    [Test]
    public void SingleEntrySizeLimitStopsOversizedEntryExpansion()
    {
        byte[] archive = CreateArchive(
            ["content.xml"],
            ["12345"],
            [CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumSingleEntryBytes = 4;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.ExpandedSizeLimitExceeded).Location.PartPath, Is.EqualTo("content.xml"));
        });
    }

    [Test]
    public void CompressionRatioLimitStopsHighlyCompressedEntryExpansion()
    {
        byte[] archive = CreateArchive(
            ["content.xml"],
            [new string('x', 4096)],
            [CompressionLevel.SmallestSize]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumCompressionRatio = 2;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.CompressionRatioExceeded).Location.PartPath, Is.EqualTo("content.xml"));
        });
    }

    [Test]
    public void PathLengthLimitRejectsLongEntryNameBeforeExpansion()
    {
        byte[] archive = CreateArchive(
            ["long-name.txt"],
            ["content"],
            [CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumPathLength = 8;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.InvalidEntryPath).Message, Does.Contain("length"));
        });
    }

    [Test]
    public void NestedPackageDepthLimitStopsBeforeParsingTheNestedPackage()
    {
        byte[] archive = CreateArchive(
            ["content.xml"],
            ["content"],
            [CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumEmbeddedPackageDepth = 1;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 2);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.NestedPackageDepth, Is.EqualTo(2));
            Assert.That(inspection.Entries, Is.Empty);
            Assert.That(GetDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage).Message, Does.Contain("depth"));
        });
    }

    [Test]
    public void ValuesExactlyAtEveryConfiguredBoundaryAreAccepted()
    {
        int mediaTypeLength = Encoding.UTF8.GetByteCount(OdfMediaTypes.Text);
        byte[] archive = CreateArchive(
            [OdfPartNames.Mimetype],
            [OdfMediaTypes.Text],
            [CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumPackageEntries = 1;
        limits.MaximumTotalExpandedBytes = mediaTypeLength;
        limits.MaximumSingleEntryBytes = mediaTypeLength;
        limits.MaximumCompressionRatio = 1;
        limits.MaximumPathLength = OdfPartNames.Mimetype.Length;
        limits.MaximumEmbeddedPackageDepth = 0;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.Diagnostics, Is.Empty);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.True);
        });
    }

    [Test]
    public void ActualExpansionIsBoundedWhenDeclaredSizeIsForgedSmaller()
    {
        byte[] archive = CreateArchive(
            ["content.xml"],
            [new string('x', 4096)],
            [CompressionLevel.SmallestSize]);
        int centralOffset = FindSignature(archive, 33639248);
        WriteUInt32(archive, centralOffset + 24, 1);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumSingleEntryBytes = 100;
        limits.MaximumTotalExpandedBytes = 100;

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(archive, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.ExpandedSizeLimitExceeded), Is.True);
        });
    }

    [Test]
    public void CustomLimitsApplyToPathAndStreamInputs()
    {
        byte[] archive = CreateArchive(
            ["first.txt", "second.txt"],
            ["one", "two"],
            [CompressionLevel.NoCompression, CompressionLevel.NoCompression]);
        OdfSecurityLimits limits = CreatePermissiveLimits();
        limits.MaximumPackageEntries = 1;
        string path = Path.GetTempFileName();
        File.WriteAllBytes(path, archive);
        try
        {
            ZipPackageInspection pathInspection = ZipPackageReader.InspectPathWithLimits(path, limits, 0);
            using (MemoryStream stream = new MemoryStream(archive))
            {
                ZipPackageInspection streamInspection = ZipPackageReader.InspectStreamWithLimits(stream, limits, 0);

                Assert.Multiple(() =>
                {
                    Assert.That(HasDiagnostic(pathInspection, OdfDiagnosticCodes.EntryLimitExceeded), Is.True);
                    Assert.That(HasDiagnostic(streamInspection, OdfDiagnosticCodes.EntryLimitExceeded), Is.True);
                    Assert.That(stream.CanRead, Is.True);
                });
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    static OdfSecurityLimits CreatePermissiveLimits()
    {
        return new OdfSecurityLimits
        {
            MaximumPackageEntries = 100,
            MaximumTotalExpandedBytes = 1000000,
            MaximumSingleEntryBytes = 1000000,
            MaximumCompressionRatio = 10000,
            MaximumPathLength = 1000,
            MaximumEmbeddedPackageDepth = 10
        };
    }

    static byte[] CreateArchive(string[] paths, string[] contents, CompressionLevel[] compressionLevels)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                int index = 0;
                while (index < paths.Length)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(paths[index], compressionLevels[index]);
                    using (Stream output = entry.Open())
                    using (StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false)))
                    {
                        writer.Write(contents[index]);
                    }
                    index++;
                }
            }
            return stream.ToArray();
        }
    }

    static bool HasDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static OdfDiagnostic GetDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return diagnostic;
            }
        }
        Assert.Fail($"Expected diagnostic {code}.");
        return new OdfDiagnostic();
    }

    static int FindSignature(byte[] data, uint signature)
    {
        int offset = 0;
        while (offset <= data.Length - 4)
        {
            uint value = (uint)data[offset]
                | ((uint)data[offset + 1] << 8)
                | ((uint)data[offset + 2] << 16)
                | ((uint)data[offset + 3] << 24);
            if (value == signature)
            {
                return offset;
            }
            offset++;
        }
        Assert.Fail($"Signature {signature} was not found.");
        return -1;
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }
}
