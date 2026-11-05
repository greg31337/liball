using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicReaderFormatEntryPointTests
{
    [TestCaseSource(nameof(RepresentativeFormats))]
    public void ReadFile_RecognizesEveryRepresentativeAdvertisedFormat(PublicReaderFormatFixture fixture)
    {
        string path = Path.Combine(Path.GetTempPath(), "image-public-format-" + Guid.NewGuid().ToString("N") + fixture.Extension);
        File.WriteAllBytes(path, fixture.Bytes);

        try
        {
            IImageMetadataReader reader = new ImageMetadataReader();
            ImageMetadataDocument document = reader.ReadFile(path);

            AssertRepresentativeDocument(document, fixture.FileKind, fixture.Bytes.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCaseSource(nameof(RepresentativeFormats))]
    public void ReadStream_RecognizesEveryRepresentativeAdvertisedFormatFromSeekableInput(PublicReaderFormatFixture fixture)
    {
        MemoryStream stream = new MemoryStream(fixture.Bytes);
        IImageMetadataReader reader = new ImageMetadataReader();

        ImageMetadataDocument document = reader.ReadStream(stream);

        AssertRepresentativeDocument(document, fixture.StreamKind, fixture.Bytes.Length);
        Assert.That(stream.Position, Is.InRange(0, stream.Length));
    }

    [TestCaseSource(nameof(RepresentativeFormats))]
    public void ReadStream_RecognizesEveryRepresentativeAdvertisedFormatFromNonSeekableInput(PublicReaderFormatFixture fixture)
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(fixture.Bytes);
        IImageMetadataReader reader = new ImageMetadataReader();

        ImageMetadataDocument document = reader.ReadStream(stream);

        AssertRepresentativeDocument(document, fixture.StreamKind, fixture.Bytes.Length);
        Assert.That(stream.PositionValue, Is.EqualTo(fixture.Bytes.Length));
    }

    [TestCaseSource(nameof(AdvertisedExtensions))]
    public void ReadFile_UsesEveryAdvertisedExtensionWhenMagicIsUnavailable(ImageFormatKind kind, string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), "image-public-extension-" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });

        try
        {
            ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

            Assert.That(document.Format.Kind, Is.EqualTo(kind));
            Assert.That(document.Format.DetectedByMagic, Is.False);
            Assert.That(document.ByteLength, Is.EqualTo(4));
            Assert.That(document.Format.Name, Is.Not.Empty);
            Assert.That(document.Format.MimeType, Is.Not.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static IEnumerable<TestCaseData> RepresentativeFormats()
    {
        foreach (PublicReaderFormatFixture fixture in CreateRepresentativeFormats())
        {
            yield return new TestCaseData(fixture).SetName("{m}(" + fixture.FileKind + ")");
        }
    }

    public static IEnumerable<TestCaseData> AdvertisedExtensions()
    {
        foreach (ImageFormatKind kind in Enum.GetValues<ImageFormatKind>())
        {
            if (kind != ImageFormatKind.Unknown)
            {
                string extension = ImageFormatMapping.GetPreferredExtension(kind);
                yield return new TestCaseData(kind, extension).SetName("{m}(" + kind + "," + extension + ")");
            }
        }
    }

    public static PublicReaderFormatFixture[] CreateRepresentativeFormats()
    {
        byte[] tiff = RawTiffProfileParserTests.ClassicTiff();
        byte[] avif = BmffMetadataExtractorTests.BuildCompleteAvif();
        byte[] icoPng = IcoMetadataParserTests.CreatePng(20, 10, 8, 6);
        byte[] pcx = PcxMetadataParserTests.CreateHeader(5, 8, 0, 0, 19, 9, 72, 72, 3, 20, 1, 20, 10, PcxMetadataParserTests.CreatePalette());
        byte[] dds = DdsMetadataParserTests.CreateLegacy(20, 10, 4103, 60, 0, 1, 64, "", 24, 16711680, 65280, 255, 0, 4096, 0);
        return new PublicReaderFormatFixture[]
        {
            Magic(".jpg", JpegMetadataExtractionTests.BuildSimpleJpeg(), ImageFormatKind.Jpeg),
            Magic(".tif", TiffMetadataExtractionTests.BuildCompositeClassicTiff(), ImageFormatKind.Tiff),
            Magic(".btf", TiffMetadataExtractionTests.BuildBigTiffWith64BitTypes(true), ImageFormatKind.BigTiff),
            Magic(".png", PngMetadataExtractionIntegrationTests.BuildCompositePng(), ImageFormatKind.Png),
            Magic(".gif", CommonFormatMetadataExtractionTests.CreateGif87a(20, 10, "representative"), ImageFormatKind.Gif),
            Magic(".webp", WebPMetadataExtractorTests.CreateWebP(new string[] { "VP8 " }, new byte[][] { WebPMetadataExtractorTests.CreateVp8(20, 10) }), ImageFormatKind.WebP),
            Magic(".bmp", CommonFormatMetadataExtractionTests.CreateBmp(20, 10), ImageFormatKind.Bmp),
            Magic(".ico", IcoMetadataParserTests.CreateContainer(1, new byte[][] { icoPng }, new int[] { 20 }, new int[] { 10 }, new int[] { 32 }), ImageFormatKind.Ico),
            Magic(".cur", IcoMetadataParserTests.CreateContainer(2, new byte[][] { icoPng }, new int[] { 20 }, new int[] { 10 }, new int[] { 32 }), ImageFormatKind.Cur),
            Magic(".svg", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"><title>Representative</title></svg>"), ImageFormatKind.Svg),
            Magic(".svgz", Gzip("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"><title>Representative</title></svg>"), ImageFormatKind.Svgz),
            Magic(".heif", AdvancedFormatMetadataExtractionTests.CreateBrandedBmff(avif, "mif1"), ImageFormatKind.Heif),
            Magic(".heic", AdvancedFormatMetadataExtractionTests.CreateBrandedBmff(avif, "heic"), ImageFormatKind.Heic),
            Magic(".avif", avif, ImageFormatKind.Avif),
            Magic(".cr3", Cr3MetadataExtractorTests.BuildCompleteCr3(), ImageFormatKind.Cr3),
            Magic(".jp2", AdvancedFormatMetadataExtractionTests.CreateJpeg2000(), ImageFormatKind.Jpeg2000),
            Magic(".jxl", AdvancedFormatMetadataExtractionTests.CreateJpegXl(), ImageFormatKind.JpegXl),
            Magic(".psd", AdvancedFormatMetadataExtractionTests.CreatePsd(1, 1920, 1080), ImageFormatKind.Psd),
            Magic(".psb", AdvancedFormatMetadataExtractionTests.CreatePsd(2, 40000, 50000), ImageFormatKind.Psb),
            ExtensionRaw(".dng", RawTiffProfileParserTests.DngTiff(), ImageFormatKind.Dng),
            ExtensionRaw(".nef", tiff, ImageFormatKind.Nef),
            ExtensionRaw(".arw", tiff, ImageFormatKind.Arw),
            Magic(".orf", RawTiffWithMagic(82, 79), ImageFormatKind.Orf),
            ExtensionRaw(".pef", tiff, ImageFormatKind.Pef),
            Magic(".rw2", RawTiffWithMagic(85, 0), ImageFormatKind.Rw2),
            ExtensionRaw(".srw", tiff, ImageFormatKind.Srw),
            ExtensionRaw(".3fr", tiff, ImageFormatKind.ThreeFr),
            ExtensionRaw(".kdc", tiff, ImageFormatKind.Kdc),
            Magic(".mrw", RawTiffProfileParserTests.MrwWithTtw(tiff), ImageFormatKind.Mrw),
            ExtensionRaw(".rwl", tiff, ImageFormatKind.Rwl),
            ExtensionRaw(".gpr", tiff, ImageFormatKind.Gpr),
            Magic(".cr2", Cr2MetadataParserTests.CreateCr2(true, 16, 22, false), ImageFormatKind.Cr2),
            Magic(".crw", AdvancedFormatMetadataExtractionTests.CreateCrw(), ImageFormatKind.Crw),
            Magic(".raf", AdvancedFormatMetadataExtractionTests.CreateRaf(), ImageFormatKind.Raf),
            Magic(".ppm", NetpbmMetadataParserTests.CreatePnm("P6", 20, 10, 255), ImageFormatKind.Netpbm),
            Magic(".pcx", pcx, ImageFormatKind.Pcx),
            Magic(".tga", TgaMetadataParserTests.CreateVersion2Tga(1), ImageFormatKind.Tga),
            Magic(".dds", dds, ImageFormatKind.Dds),
            Magic(".qoi", QoiMetadataParserTests.CreateQoi(20, 10, 3, 0, new byte[] { 254, 1, 2, 3 }), ImageFormatKind.Qoi),
            Magic(".hdr", RadianceHdrMetadataParserTests.CreateFile("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 10 +X 20\n", new byte[] { 1, 2, 3, 4 }), ImageFormatKind.RadianceHdr),
            Magic(".eps", Encoding.ASCII.GetBytes("%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 20 10\n%%Title: Representative\n%%EOF\n"), ImageFormatKind.Eps),
            Magic(".xmp", Encoding.UTF8.GetBytes(XmpSidecarReaderTests.Packet("<dc:title>Representative</dc:title>")), ImageFormatKind.Xmp)
        };
    }

    public static PublicReaderFormatFixture Magic(string extension, byte[] bytes, ImageFormatKind kind)
    {
        return new PublicReaderFormatFixture(extension, bytes, kind, kind);
    }

    public static PublicReaderFormatFixture ExtensionRaw(string extension, byte[] bytes, ImageFormatKind kind)
    {
        return new PublicReaderFormatFixture(extension, bytes, kind, ImageFormatKind.Tiff);
    }

    public static byte[] RawTiffWithMagic(byte first, byte second)
    {
        byte[] bytes = RawTiffProfileParserTests.ClassicTiff();
        bytes[2] = first;
        bytes[3] = second;
        return bytes;
    }

    public static byte[] Gzip(string text)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                gzip.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }
    }

    public static void AssertRepresentativeDocument(ImageMetadataDocument document, ImageFormatKind expectedKind, int byteLength)
    {
        Assert.That(document.Format.Kind, Is.EqualTo(expectedKind));
        Assert.That(document.Format.Name, Is.Not.Empty);
        Assert.That(document.Format.MimeType, Is.Not.Empty);
        Assert.That(document.Format.PreferredExtension, Is.Not.Empty);
        Assert.That(document.ByteLength, Is.EqualTo(byteLength));
        Assert.That(document.Frames, Is.Not.Null);
        Assert.That(document.Directories, Is.Not.Null);
        Assert.That(document.Profiles, Is.Not.Null);
        Assert.That(document.RawBlocks, Is.Not.Null);
        Assert.That(document.Diagnostics, Is.Not.Null);
        Assert.That(document.Summary, Is.Not.Null);
    }
}

public class PublicReaderFormatFixture
{
    public string Extension;
    public byte[] Bytes;
    public ImageFormatKind FileKind;
    public ImageFormatKind StreamKind;

    public PublicReaderFormatFixture(string extension, byte[] bytes, ImageFormatKind fileKind, ImageFormatKind streamKind)
    {
        Extension = extension;
        Bytes = bytes;
        FileKind = fileKind;
        StreamKind = streamKind;
    }

    public override string ToString()
    {
        return FileKind.ToString();
    }
}
