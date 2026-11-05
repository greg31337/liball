using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageFormatMappingTests
{
    [Test]
    public void CreateInfo_MapsCommonWebFormats()
    {
        AssertMapping(ImageFormatKind.Jpeg, "JPEG", "image/jpeg", ".jpg");
        AssertMapping(ImageFormatKind.Png, "PNG", "image/png", ".png");
        AssertMapping(ImageFormatKind.Gif, "GIF", "image/gif", ".gif");
        AssertMapping(ImageFormatKind.WebP, "WebP", "image/webp", ".webp");
        AssertMapping(ImageFormatKind.Svg, "SVG", "image/svg+xml", ".svg");
        AssertMapping(ImageFormatKind.Avif, "AVIF", "image/avif", ".avif");
    }

    [Test]
    public void CreateInfo_MapsContainersAndProfessionalFormats()
    {
        AssertMapping(ImageFormatKind.Heif, "HEIF", "image/heif", ".heif");
        AssertMapping(ImageFormatKind.Heic, "HEIC", "image/heic", ".heic");
        AssertMapping(ImageFormatKind.Cr3, "CR3", "image/x-canon-cr3", ".cr3");
        AssertMapping(ImageFormatKind.Jpeg2000, "JPEG 2000", "image/jp2", ".jp2");
        AssertMapping(ImageFormatKind.JpegXl, "JPEG XL", "image/jxl", ".jxl");
        AssertMapping(ImageFormatKind.Psd, "PSD", "image/vnd.adobe.photoshop", ".psd");
        AssertMapping(ImageFormatKind.Psb, "PSB", "image/vnd.adobe.photoshop", ".psb");
    }

    [Test]
    public void CreateInfo_MapsRawAndLegacyFormats()
    {
        AssertMapping(ImageFormatKind.Dng, "DNG", "image/x-adobe-dng", ".dng");
        AssertMapping(ImageFormatKind.Nef, "NEF", "image/x-nikon-nef", ".nef");
        AssertMapping(ImageFormatKind.Arw, "ARW", "image/x-sony-arw", ".arw");
        AssertMapping(ImageFormatKind.Orf, "ORF", "image/x-olympus-orf", ".orf");
        AssertMapping(ImageFormatKind.Rw2, "RW2", "image/x-panasonic-rw2", ".rw2");
        AssertMapping(ImageFormatKind.Raf, "RAF", "image/x-fuji-raf", ".raf");
        AssertMapping(ImageFormatKind.Netpbm, "Netpbm", "image/x-portable-anymap", ".pnm");
        AssertMapping(ImageFormatKind.Dds, "DDS", "image/vnd.ms-dds", ".dds");
        AssertMapping(ImageFormatKind.Eps, "EPS", "application/postscript", ".eps");
    }

    [Test]
    public void EveryPlannedFormatExceptUnknown_HasMimeTypeAndExtension()
    {
        foreach (ImageFormatKind kind in System.Enum.GetValues<ImageFormatKind>())
        {
            ImageFormatInfo info = ImageFormatMapping.CreateInfo(kind);

            if (kind == ImageFormatKind.Unknown)
            {
                Assert.That(info.MimeType, Is.EqualTo("application/octet-stream"));
                Assert.That(info.PreferredExtension, Is.EqualTo(""));
            }
            else
            {
                Assert.That(info.Name, Is.Not.Empty, kind.ToString());
                Assert.That(info.MimeType, Is.Not.Empty, kind.ToString());
                Assert.That(info.PreferredExtension, Does.StartWith("."), kind.ToString());
            }
        }
    }

    [Test]
    public void DetectedFormats_ArePopulatedWithMimeTypeAndExtension()
    {
        ImageFormatInfo png = new ImageFormatDetector().DetectBytes(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        Assert.That(png.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(png.Name, Is.EqualTo("PNG"));
        Assert.That(png.MimeType, Is.EqualTo("image/png"));
        Assert.That(png.PreferredExtension, Is.EqualTo(".png"));
    }

    [Test]
    public void Accessors_ReturnMappedFields()
    {
        Assert.That(ImageFormatMapping.GetName(ImageFormatKind.Qoi), Is.EqualTo("QOI"));
        Assert.That(ImageFormatMapping.GetMimeType(ImageFormatKind.Qoi), Is.EqualTo("image/qoi"));
        Assert.That(ImageFormatMapping.GetPreferredExtension(ImageFormatKind.Qoi), Is.EqualTo(".qoi"));
    }

    static void AssertMapping(ImageFormatKind kind, string name, string mimeType, string preferredExtension)
    {
        ImageFormatInfo info = ImageFormatMapping.CreateInfo(kind);

        Assert.That(info.Kind, Is.EqualTo(kind));
        Assert.That(info.Name, Is.EqualTo(name));
        Assert.That(info.MimeType, Is.EqualTo(mimeType));
        Assert.That(info.PreferredExtension, Is.EqualTo(preferredExtension));
    }
}
