using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PackageFailureModeTests
{
    [Test]
    public void OpenBytes_MalformedZipThrowsInvalidDataException()
    {
        byte[] bytes = new byte[] { 80, 75, 3, 4, 0, 1, 2 };

        Assert.Throws<InvalidDataException>(() => PptxPackage.OpenBytes(bytes));
    }

    [Test]
    public void OpenBytes_BadUriEntryReportsDiagnosticsAndSkipsEntry()
    {
        byte[] bytes = CreateZip(
            new string[] { "ppt/../evil.xml" },
            new byte[][] { Encoding.UTF8.GetBytes("<evil/>") },
            CompressionLevel.NoCompression);

        PptxPackage package = PptxPackage.OpenBytes(bytes);

        Assert.That(package.Failed(), Is.True);
        Assert.That(package.RawEntryCount(), Is.EqualTo(0));
        Assert.That(package.PartCount(), Is.EqualTo(0));
        Assert.That(package.Diagnostics.FormatDiagnostics(), Does.Contain("URI010"));
    }

    [Test]
    public void OpenBytes_BadRelationshipXmlReportsRelationshipDiagnostics()
    {
        byte[] bytes = CreatePackageWithBadRootRelationships();

        PptxPackage package = PptxPackage.OpenBytes(bytes);
        PptxResult validation = package.Validate();

        Assert.That(package.Failed(), Is.True);
        Assert.That(package.RootRelationships.Count(), Is.EqualTo(1));
        Assert.That(package.Diagnostics.FormatDiagnostics(), Does.Contain("REL013"));
        Assert.That(package.Diagnostics.FormatDiagnostics(), Does.Contain("REL017"));
        Assert.That(validation.Failed(), Is.True);
        Assert.That(validation.FormatDiagnostics(), Does.Contain("REL013"));
        Assert.That(validation.FormatDiagnostics(), Does.Contain("REL017"));
    }

    [Test]
    public void Validate_MalformedXmlPartReportsPackageXmlDiagnostic()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld><broken></p:sld>"));
        PptxPackage reopened = PptxPackage.OpenBytes(package.SaveBytes());

        PptxResult validation = reopened.Validate();

        Assert.That(validation.Failed(), Is.True);
        Assert.That(validation.FormatDiagnostics(), Does.Contain("PKGV042"));
        Assert.That(validation.FormatDiagnostics(), Does.Contain(PptxPartNames.FirstSlide));
    }

    [Test]
    public void OpenBytes_OversizedEntryReportsZipLimitDiagnostic()
    {
        byte[] bytes = CreateZip(
            new string[] { "ppt/presentation.xml" },
            new byte[][] { Encoding.UTF8.GetBytes(new string('A', 128)) },
            CompressionLevel.NoCompression);
        PptxZipReadOptions options = PptxZipReadOptions.CreateDefault();
        options.MaxEntryBytes = 32;

        PptxPackage package = PptxPackage.OpenBytes(bytes, options);

        Assert.That(package.Failed(), Is.True);
        Assert.That(package.RawEntryCount(), Is.EqualTo(0));
        Assert.That(package.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP020"));
    }

    static byte[] CreatePackageWithBadRootRelationships()
    {
        string relationshipsXml =
            "<Relationships xmlns=\"" + PptxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + PptxRelationshipTypes.OfficeDocument + "\" Target=\"ppt/presentation.xml\"/>" +
            "<Relationship Id=\"rIdBadType\" Type=\"not-relative\" Target=\"ppt/presentation.xml\"/>" +
            "<Relationship Id=\"rIdBadTarget\" Type=\"" + PptxRelationshipTypes.Hyperlink + "\" Target=\"relative.html\" TargetMode=\"External\"/>" +
            "</Relationships>";

        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("rels", PptxContentTypes.Relationships);
        manifest.AddDefault("xml", PptxContentTypes.Xml);
        manifest.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);

        return CreateZip(
            new string[]
            {
                PptxPartNames.ContentTypesEntry,
                "_rels/.rels",
                "ppt/presentation.xml"
            },
            new byte[][]
            {
                PptxContentTypesWriter.WriteBytes(manifest),
                Encoding.UTF8.GetBytes(relationshipsXml),
                Encoding.UTF8.GetBytes("<p:presentation/>")
            },
            CompressionLevel.NoCompression);
    }

    static byte[] CreateZip(string[] names, byte[][] contents, CompressionLevel compressionLevel)
    {
        using MemoryStream stream = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            for (int index = 0; index < names.Length; index++)
            {
                ZipArchiveEntry entry = archive.CreateEntry(names[index], compressionLevel);
                using Stream entryStream = entry.Open();
                entryStream.Write(contents[index], 0, contents[index].Length);
            }
        }

        return stream.ToArray();
    }
}
