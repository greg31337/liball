using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ImageResourceManagerTests
{
    [Test]
    public void LoadsPackageExternalAndInlineImagesWithoutFetching()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfImage package = Image(document, "Package Image");
        OdfImage external = Image(document, "External Image");
        OdfImage inline = Image(document, "Inline Image");

        Assert.Multiple(() =>
        {
            Assert.That(document.Images, Has.Length.EqualTo(3));
            Assert.That(package.SourceKind,
                Is.EqualTo(OdfImageSourceKind.Package));
            Assert.That(package.PackagePath,
                Is.EqualTo("Pictures/source.png"));
            Assert.That(package.DetectedFormat,
                Is.EqualTo(OdfImageFormat.Png));
            Assert.That(package.DetectedMediaType,
                Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(package.IsValid, Is.True);
            Assert.That(package.HasMediaTypeMismatch, Is.False);
            Assert.That(package.Data, Is.EqualTo(PngData()));
            Assert.That(external.SourceKind,
                Is.EqualTo(OdfImageSourceKind.External));
            Assert.That(external.Href,
                Is.EqualTo("https://example.com/image.png"));
            Assert.That(external.Data, Is.Empty);
            Assert.That(external.IsValid, Is.True);
            Assert.That(inline.SourceKind,
                Is.EqualTo(OdfImageSourceKind.Base64));
            Assert.That(inline.DetectedFormat,
                Is.EqualTo(OdfImageFormat.Gif));
            Assert.That(inline.Data, Is.EqualTo(GifData(1)));
            Assert.That(inline.BinaryDataNode, Is.Not.Null);
            Assert.That(package.Drawing?.Image, Is.SameAs(package));
        });
    }

    [Test]
    public void AddsDeduplicatesReplacesAndExtractsPackageImages()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        int originalEntries = document.PackageEntries.Length;

        OdfImage duplicate = ImageResourceManager.AddPackageImage(
            document,
            "Pictures/duplicate.png",
            PngData(),
            OdfMediaTypes.Png,
            true);
        OdfImage added = ImageResourceManager.AddPackageImage(
            document,
            "Pictures/added.gif",
            GifData(2),
            "",
            false);
        OdfEditResult replaced = ImageResourceManager.ReplaceImageData(
            document,
            added,
            GifData(3),
            OdfMediaTypes.Gif);
        byte[] extracted = ImageResourceManager.ExtractImageData(
            document,
            added);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.PackagePath,
                Is.EqualTo("Pictures/source.png"));
            Assert.That(document.PackageEntries, Has.Length.EqualTo(
                originalEntries + 1));
            Assert.That(document.PackageEntries.Any(item =>
                item.Path == "Pictures/duplicate.png"), Is.False);
            Assert.That(added.PackagePath,
                Is.EqualTo("Pictures/added.gif"));
            Assert.That(added.DetectedFormat,
                Is.EqualTo(OdfImageFormat.Gif));
            Assert.That(replaced.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(extracted, Is.EqualTo(GifData(3)));
            Assert.That(document.Manifest?.Entries.Single(item =>
                item.FullPath == "Pictures/added.gif").MediaType,
                Is.EqualTo(OdfMediaTypes.Gif));
        });

        OdfDocument reread = ReadPackage(Write(document));
        OdfImage resource = reread.Images.Single(item =>
            item.PackagePath == "Pictures/added.gif");
        Assert.That(resource.Data, Is.EqualTo(GifData(3)));
    }

    [Test]
    public void ReplacesInlineDataAndNeverReadsExternalContent()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfImage inline = Image(document, "Inline Image");
        OdfImage external = Image(document, "External Image");
        OdfEditResult inlineResult = ImageResourceManager.ReplaceImageData(
            document,
            inline,
            PngData(),
            OdfMediaTypes.Png);
        OdfEditResult externalResult = ImageResourceManager.ReplaceImageData(
            document,
            external,
            PngData(),
            OdfMediaTypes.Png);
        ImageResourceManager.SetExternalImage(
            external,
            "https://cdn.example.com/replacement.webp",
            OdfMediaTypes.WebP);

        Assert.Multiple(() =>
        {
            Assert.That(inlineResult.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(externalResult.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.Throws<OdfEditException>(() =>
                ImageResourceManager.ExtractImageData(document, external));
        });

        OdfDocument reread = ReadPackage(Write(document));
        OdfImage changedInline = Image(reread, "Inline Image");
        OdfImage changedExternal = Image(reread, "External Image");
        Assert.Multiple(() =>
        {
            Assert.That(changedInline.Data, Is.EqualTo(PngData()));
            Assert.That(changedInline.DeclaredMediaType,
                Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(changedExternal.Href,
                Is.EqualTo("https://cdn.example.com/replacement.webp"));
            Assert.That(changedExternal.Data, Is.Empty);
        });
    }

    [Test]
    public void SniffsKnownFormatsAndRejectsSpoofedOrMismatchedData()
    {
        Assert.Multiple(() =>
        {
            AssertFormat(PngData(), OdfImageFormat.Png);
            AssertFormat([255, 216, 255, 224], OdfImageFormat.Jpeg);
            AssertFormat(GifData(0), OdfImageFormat.Gif);
            AssertFormat([66, 77, 0, 0], OdfImageFormat.Bmp);
            AssertFormat([73, 73, 42, 0], OdfImageFormat.Tiff);
            AssertFormat(
                [82, 73, 70, 70, 0, 0, 0, 0, 87, 69, 66, 80],
                OdfImageFormat.WebP);
            AssertFormat(
                Encoding.UTF8.GetBytes(
                    "<?xml version='1.0'?><!--safe--><svg xmlns='"
                    + OdfNamespaces.Svg + "'/>") ,
                OdfImageFormat.Svg);
            Assert.That(ImageSniffer.Inspect(
                Encoding.UTF8.GetBytes("<root><svg/></root>"))
                .IsRecognized, Is.False);
            Assert.Throws<OdfEditException>(() =>
                ImageResourceManager.AddPackageImage(
                    ReadPackage(CreatePackage()),
                    "Pictures/wrong.jpg",
                    PngData(),
                    OdfMediaTypes.Jpeg,
                    false));
        });
    }

    static void AssertFormat(byte[] data, OdfImageFormat expected)
    {
        ImageInspection inspection = ImageSniffer.Inspect(data);
        Assert.That(inspection.IsRecognized, Is.True);
        Assert.That(inspection.Format, Is.EqualTo(expected));
    }

    static OdfImage Image(OdfDocument document, string drawingName)
    {
        return document.DrawingObjects.Single(item =>
            item.Name == drawingName).Image ?? new OdfImage();
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "images.odg",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static byte[] CreatePackage()
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Graphics),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(ContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(ManifestXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Pictures/source.png",
                PngData(),
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    static string ContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'>"
            + "<office:body><office:drawing><draw:page draw:name='Page'>"
            + "<draw:image draw:name='Package Image' "
            + "xlink:href='Pictures/source.png' xlink:type='simple' "
            + "draw:mime-type='image/png'/>"
            + "<draw:image draw:name='External Image' "
            + "xlink:href='https://example.com/image.png' "
            + "xlink:type='simple' xlink:show='embed' "
            + "xlink:actuate='onLoad' draw:mime-type='image/png'/>"
            + "<draw:image draw:name='Inline Image' draw:mime-type='image/gif'>"
            + "<office:binary-data>"
            + Convert.ToBase64String(GifData(1))
            + "</office:binary-data></draw:image>"
            + "</draw:page></office:drawing></office:body>"
            + "</office:document-content>";
    }

    static string ManifestXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest + "' manifest:version='1.4'>"
            + Entry("/", OdfMediaTypes.Graphics)
            + Entry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + Entry("Pictures/source.png", OdfMediaTypes.Png)
            + "</manifest:manifest>";
    }

    static string Entry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='" + path
            + "' manifest:media-type='" + mediaType + "'/>";
    }

    static byte[] PngData()
    {
        return [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4];
    }

    static byte[] GifData(byte suffix)
    {
        return [71, 73, 70, 56, 57, 97, suffix];
    }
}
