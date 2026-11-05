using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class MimetypeRulesTests
{
    [Test]
    public void ValidMimetypeIsFirstStoredWithoutExtrasAndMatchesRootManifest()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Mimetype, OdfPartNames.Manifest, OdfPartNames.Content],
            [OdfMediaTypes.Text, CreateManifest(OdfMediaTypes.Text), "<office:document-content/>"],
            [CompressionLevel.NoCompression, CompressionLevel.SmallestSize, CompressionLevel.SmallestSize]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        OdfPackageEntry mimetype = FindEntry(inspection, OdfPartNames.Mimetype);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.True);
            Assert.That(inspection.HasMimetype, Is.True);
            Assert.That(inspection.MimetypeIsValid, Is.True);
            Assert.That(inspection.MediaTypeWasInferred, Is.False);
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.ManifestMediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.DocumentKind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(mimetype.IsFirstEntry, Is.True);
            Assert.That(mimetype.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(mimetype.LocalExtraFieldLength, Is.Zero);
            Assert.That(mimetype.CentralExtraFieldLength, Is.Zero);
        });
    }

    [Test]
    public void MissingMimetypeIsReportedAndInferredFromRootManifest()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Manifest, OdfPartNames.Content],
            [CreateManifest(OdfMediaTypes.Spreadsheet), "content"],
            [CompressionLevel.SmallestSize, CompressionLevel.NoCompression]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.HasMimetype, Is.False);
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(inspection.MediaTypeWasInferred, Is.True);
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
            Assert.That(inspection.DocumentKind, Is.EqualTo(OdfDocumentKind.Spreadsheet));
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MissingMimetype), Is.True);
        });
    }

    [Test]
    public void MissingMimetypeCanBeInferredFromPathExtension()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Content],
            ["content"],
            [CompressionLevel.NoCompression]);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".odp");
        File.WriteAllBytes(path, package);
        try
        {
            ZipPackageInspection inspection = ZipPackageReader.InspectPath(path);

            Assert.Multiple(() =>
            {
                Assert.That(inspection.MediaTypeWasInferred, Is.True);
                Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Presentation));
                Assert.That(inspection.DocumentKind, Is.EqualTo(OdfDocumentKind.Presentation));
                Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MissingMimetype), Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void MimetypeNotFirstIsReportedUsingLocalHeaderOrder()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Content, OdfPartNames.Mimetype],
            ["content", OdfMediaTypes.Text],
            [CompressionLevel.NoCompression, CompressionLevel.NoCompression]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(FindEntry(inspection, OdfPartNames.Content).IsFirstEntry, Is.True);
            Assert.That(FindEntry(inspection, OdfPartNames.Mimetype).IsFirstEntry, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MimetypeNotFirst), Is.True);
        });
    }

    [Test]
    public void CompressedMimetypeIsReported()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Mimetype],
            [OdfMediaTypes.Text],
            [CompressionLevel.SmallestSize]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);

        Assert.Multiple(() =>
        {
            Assert.That(FindEntry(inspection, OdfPartNames.Mimetype).CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MimetypeCompressed), Is.True);
        });
    }

    [Test]
    public void MimetypeWithLocalExtraFieldIsReported()
    {
        byte[] package = CreateStoredMimetypeWithLocalExtra();

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        OdfPackageEntry mimetype = FindEntry(inspection, OdfPartNames.Mimetype);

        Assert.Multiple(() =>
        {
            Assert.That(mimetype.LocalExtraFieldLength, Is.EqualTo(2));
            Assert.That(mimetype.CentralExtraFieldLength, Is.Zero);
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MimetypeExtraField), Is.True);
        });
    }

    [Test]
    public void InvalidMimetypeFallsBackToRootManifestMediaType()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Mimetype, OdfPartNames.Manifest],
            ["text/plain", CreateManifest(OdfMediaTypes.Graphics)],
            [CompressionLevel.NoCompression, CompressionLevel.SmallestSize]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Graphics));
            Assert.That(inspection.MediaTypeWasInferred, Is.True);
            Assert.That(inspection.DocumentKind, Is.EqualTo(OdfDocumentKind.Drawing));
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.InvalidMimetype), Is.True);
        });
    }

    [Test]
    public void RootManifestMediaTypeMismatchHasRelatedMimetypeLocation()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Mimetype, OdfPartNames.Manifest],
            [OdfMediaTypes.Text, CreateManifestWithAlternatePrefix(OdfMediaTypes.Spreadsheet)],
            [CompressionLevel.NoCompression, CompressionLevel.SmallestSize]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(package);
        OdfDiagnostic diagnostic = GetDiagnostic(
            inspection,
            OdfDiagnosticCodes.ManifestMediaTypeMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(inspection.ManifestMediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
            Assert.That(inspection.MimetypeIsValid, Is.False);
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(OdfPartNames.Manifest));
            Assert.That(diagnostic.HasRelatedLocation, Is.True);
            Assert.That(diagnostic.RelatedLocation.PartPath, Is.EqualTo(OdfPartNames.Mimetype));
        });
    }

    [Test]
    public void ManifestInferenceDoesNotBypassPackageExpansionLimits()
    {
        byte[] package = CreatePackage(
            [OdfPartNames.Manifest],
            [CreateManifest(OdfMediaTypes.Text) + new string('x', 1000)],
            [CompressionLevel.SmallestSize]);
        OdfSecurityLimits limits = new OdfSecurityLimits
        {
            MaximumSingleEntryBytes = 32,
            MaximumTotalExpandedBytes = 32,
            MaximumCompressionRatio = 1000
        };

        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(package, limits, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.ManifestMediaType, Is.Empty);
            Assert.That(inspection.MediaType, Is.Empty);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.ExpandedSizeLimitExceeded), Is.True);
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.MissingMimetype), Is.True);
        });
    }

    [TestCase("document.odt", OdfMediaTypes.Text)]
    [TestCase("template.OTT", OdfMediaTypes.TextTemplate)]
    [TestCase("sheet.ods", OdfMediaTypes.Spreadsheet)]
    [TestCase("slides.otp", OdfMediaTypes.PresentationTemplate)]
    [TestCase("drawing.odg", OdfMediaTypes.Graphics)]
    [TestCase("chart.otc", OdfMediaTypes.ChartTemplate)]
    [TestCase("formula.otf", OdfMediaTypes.FormulaTemplate)]
    [TestCase("image.odi", OdfMediaTypes.Image)]
    [TestCase("database.odb", OdfMediaTypes.Base)]
    [TestCase("archive.zip", "")]
    public void MediaTypeInferenceRecognizesOpenDocumentExtensions(string name, string expected)
    {
        Assert.That(MimetypeRules.InferMediaTypeFromName(name), Is.EqualTo(expected));
    }

    static string CreateManifest(string mediaType)
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.4\">"
            + "<manifest:file-entry manifest:full-path=\"/\" manifest:media-type=\""
            + mediaType
            + "\"/>"
            + "</manifest:manifest>";
    }

    static string CreateManifestWithAlternatePrefix(string mediaType)
    {
        return "<m:manifest xmlns:m='urn:oasis:names:tc:opendocument:xmlns:manifest:1.0' m:version='1.4'>"
            + "<m:file-entry m:media-type='"
            + mediaType
            + "' m:full-path='/'/>"
            + "</m:manifest>";
    }

    static byte[] CreatePackage(
        string[] paths,
        string[] contents,
        CompressionLevel[] compressionLevels)
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

    static byte[] CreateStoredMimetypeWithLocalExtra()
    {
        byte[] name = Encoding.UTF8.GetBytes(OdfPartNames.Mimetype);
        byte[] content = Encoding.UTF8.GetBytes(OdfMediaTypes.Text);
        uint crc = ComputeCrc(content);
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(67324752U);
            writer.Write((ushort)20);
            writer.Write((ushort)2048);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(crc);
            writer.Write((uint)content.Length);
            writer.Write((uint)content.Length);
            writer.Write((ushort)name.Length);
            writer.Write((ushort)2);
            writer.Write(name);
            writer.Write((byte)1);
            writer.Write((byte)2);
            writer.Write(content);

            long directoryOffset = stream.Position;
            writer.Write(33639248U);
            writer.Write((ushort)20);
            writer.Write((ushort)20);
            writer.Write((ushort)2048);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(crc);
            writer.Write((uint)content.Length);
            writer.Write((uint)content.Length);
            writer.Write((ushort)name.Length);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(0U);
            writer.Write(0U);
            writer.Write(name);
            long directorySize = stream.Position - directoryOffset;

            writer.Write(101010256U);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);
            writer.Write((uint)directorySize);
            writer.Write((uint)directoryOffset);
            writer.Write((ushort)0);
            writer.Flush();
            return stream.ToArray();
        }
    }

    static uint ComputeCrc(byte[] data)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in data)
        {
            crc = crc ^ value;
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 3988292384U : crc >> 1;
                bit++;
            }
        }
        return ~crc;
    }

    static OdfPackageEntry FindEntry(ZipPackageInspection inspection, string path)
    {
        foreach (OdfPackageEntry entry in inspection.Entries)
        {
            if (entry.Path == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected package entry {path}.");
        return new OdfPackageEntry();
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
}
