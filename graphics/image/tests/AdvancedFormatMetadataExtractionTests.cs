using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class AdvancedFormatMetadataExtractionTests
{
    [TestCaseSource(nameof(FormatCases))]
    public void ReadStream_FromCurrentPositionDetectsAndExtractsMetadata(byte[] payload, ImageFormatKind kind, string metadataKey, int width, int height)
    {
        byte[] prefixed = new byte[payload.Length + 7];
        System.Array.Copy(payload, 0, prefixed, 7, payload.Length);
        MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 7;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        AssertAdvancedDocument(document, payload, kind, metadataKey, width, height);
        Assert.That(stream.Position, Is.EqualTo(7));
    }

    [TestCaseSource(nameof(FormatCases))]
    public void ReadStream_NonSeekableInputBuffersAndExtractsMetadata(byte[] payload, ImageFormatKind kind, string metadataKey, int width, int height)
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(payload);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        AssertAdvancedDocument(document, payload, kind, metadataKey, width, height);
        Assert.That(stream.PositionValue, Is.EqualTo(payload.Length));
    }

    public static object[] FormatCases()
    {
        byte[] avif = BmffMetadataExtractorTests.BuildCompleteAvif();
        return new object[]
        {
            new object[] { avif, ImageFormatKind.Avif, "BMFF/Items/1/Width", 640, 480 },
            new object[] { CreateBrandedBmff(avif, "heic"), ImageFormatKind.Heic, "BMFF/Items/1/Width", 640, 480 },
            new object[] { CreateBrandedBmff(avif, "mif1"), ImageFormatKind.Heif, "BMFF/Items/1/Width", 640, 480 },
            new object[] { CreateJpeg2000(), ImageFormatKind.Jpeg2000, "Jpeg2000.ihdr.Width", 1920, 1080 },
            new object[] { CreateJpegXl(), ImageFormatKind.JpegXl, "JpegXl.Codestream.Width", 64, 32 },
            new object[] { CreatePsd(1, 1920, 1080), ImageFormatKind.Psd, "Psd.Header.Width", 1920, 1080 },
            new object[] { CreatePsd(2, 40000, 50000), ImageFormatKind.Psb, "Psd.Header.Width", 40000, 50000 },
            new object[] { Cr2MetadataParserTests.CreateCr2(true, 16, 22, false), ImageFormatKind.Cr2, "Raw.CR2RawIFD.ImageWidth", 0, 0 },
            new object[] { Cr3MetadataExtractorTests.BuildCompleteCr3(), ImageFormatKind.Cr3, "Tiff.IFD0.Make", 0, 0 },
            new object[] { CreateCrw(), ImageFormatKind.Crw, "Tiff.IFD0.Make", 4000, 3000 },
            new object[] { CreateRaf(), ImageFormatKind.Raf, "Tiff.IFD0.Make", 4000, 3000 }
        };
    }

    public static byte[] CreateBrandedBmff(byte[] avif, string brand)
    {
        byte[] bytes = new byte[avif.Length];
        System.Array.Copy(avif, bytes, avif.Length);
        byte[] encodedBrand = Encoding.ASCII.GetBytes(brand);
        System.Array.Copy(encodedBrand, 0, bytes, 8, encodedBrand.Length);
        return bytes;
    }

    public static byte[] CreateJpeg2000()
    {
        byte[] imageHeader = Jpeg2000MetadataExtractorTests.CreateImageHeader(1920, 1080, 3, 7, 7, 0, 0);
        byte[] color = Jpeg2000MetadataExtractorTests.CreateBox("colr", Jpeg2000MetadataExtractorTests.Join(new byte[] { 1, 0, 0 }, Jpeg2000MetadataExtractorTests.Unsigned32(16)));
        byte[] header = Jpeg2000MetadataExtractorTests.CreateBox("jp2h", Jpeg2000MetadataExtractorTests.Join(imageHeader, color));
        return Jpeg2000MetadataExtractorTests.Join(Jpeg2000MetadataExtractorTests.CreateSignatureBox(), header);
    }

    public static byte[] CreateJpegXl()
    {
        return JpegXlBoxTraverserTests.Join(
            JpegXlBoxTraverserTests.CreateSignatureBox(),
            JpegXlBoxTraverserTests.CreateFileTypeBox(0),
            JpegXlBoxTraverserTests.CreateBox("jxlc", JpegXlBoxTraverserTests.CreateSmallCodestream(64, 32)));
    }

    public static byte[] CreatePsd(int version, uint width, uint height)
    {
        return PsdMetadataParserTests.CreateDocument(version, 4, height, width, 8, 3, new byte[0], new byte[0], new byte[0], 0, new byte[0]);
    }

    public static byte[] CreateCrw()
    {
        byte[] imageInfo = CiffMetadataParserTests.Join(
            CiffMetadataParserTests.Unsigned32(4000, true),
            CiffMetadataParserTests.Unsigned32(3000, true),
            CiffMetadataParserTests.Unsigned32(0, true),
            CiffMetadataParserTests.Unsigned32(90, true));
        byte[] camera = CiffMetadataParserTests.CreateDirectory(
            true,
            CiffMetadataParserTests.External(0x080a, Encoding.ASCII.GetBytes("Canon\0EOS 10D\0")),
            CiffMetadataParserTests.External(0x1810, imageInfo));
        byte[] root = CiffMetadataParserTests.CreateDirectory(true, CiffMetadataParserTests.External(0x2807, camera));
        return CiffMetadataParserTests.CreateCrw(true, 26, root);
    }

    public static byte[] CreateRaf()
    {
        byte[] jpeg = RafMetadataParserTests.CreateJpegWithExif("FUJIFILM", 640, 480);
        byte[] directory = RafMetadataParserTests.CreateRafDirectory(
            new RafTestEntry(0x0100, RafMetadataParserTests.Join(RafMetadataParserTests.Unsigned16(3000), RafMetadataParserTests.Unsigned16(4000))));
        byte[] tiff = RafMetadataParserTests.CreateTiff(305, "RAF TIFF");
        return RafMetadataParserTests.CreateRaf(jpeg, directory, tiff, new byte[0], new byte[0]);
    }

    public static void AssertAdvancedDocument(ImageMetadataDocument document, byte[] payload, ImageFormatKind kind, string metadataKey, int width, int height)
    {
        Assert.That(document.Format.Kind, Is.EqualTo(kind));
        Assert.That(document.ByteLength, Is.EqualTo(payload.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, metadataKey), Is.Not.Null, metadataKey);
        if (width > 0)
        {
            Assert.That(document.Summary.Width, Is.EqualTo(width));
            Assert.That(document.Summary.Height, Is.EqualTo(height));
            Assert.That(document.Summary.FrameCount, Is.GreaterThanOrEqualTo(1));
        }
    }
}
