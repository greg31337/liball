using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class CommonFormatMetadataExtractionTests
{
    [TestCaseSource(nameof(FormatCases))]
    public void ReadStream_FromCurrentPositionDetectsAndExtractsMetadata(byte[] payload, ImageFormatKind kind, int width, int height)
    {
        byte[] prefixed = new byte[payload.Length + 7];
        int index = 0;
        while (index < payload.Length)
        {
            prefixed[index + 7] = payload[index];
            index = index + 1;
        }

        MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 7;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(kind));
        Assert.That(document.ByteLength, Is.EqualTo(payload.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary.Width, Is.EqualTo(width));
        Assert.That(document.Summary.Height, Is.EqualTo(height));
        Assert.That(document.Summary.FrameCount, Is.GreaterThanOrEqualTo(1));
    }

    [TestCaseSource(nameof(FormatCases))]
    public void ReadStream_NonSeekableInputBuffersAndExtractsMetadata(byte[] payload, ImageFormatKind kind, int width, int height)
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(payload);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(kind));
        Assert.That(document.ByteLength, Is.EqualTo(payload.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary.Width, Is.EqualTo(width));
        Assert.That(document.Summary.Height, Is.EqualTo(height));
    }

    [Test]
    public void ReadStream_Gif87aExtractsVersionFrameAndComment()
    {
        byte[] gif = CreateGif87a(20, 10, "legacy comment");

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(gif));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Gif));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Header.Version").Value.Text, Is.EqualTo("87a"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Comment.Text").Value.Text, Is.EqualTo("legacy comment"));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Lzw));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Indexed));
    }

    [Test]
    public void ReadStream_WebPCombinesCanvasAlphaExifXmpAndSummary()
    {
        byte[] tiff = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
        byte[] exif = WebPMetadataExtractorTests.Join(Encoding.ASCII.GetBytes("Exif\0\0"), tiff);
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x='adobe:ns:meta/'/>");
        byte[] webp = WebPMetadataExtractorTests.CreateWebP(
            new string[] { "VP8X", "ALPH", "VP8 ", "EXIF", "XMP " },
            new byte[][]
            {
                WebPMetadataExtractorTests.CreateVp8x(20, 10, 28),
                new byte[] { 0 },
                WebPMetadataExtractorTests.CreateVp8(20, 10),
                exif,
                xmp
            });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(webp));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.WebP));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Exif));
        Assert.That(document.Profiles[1].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.EXIF.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(document.Summary.Width, Is.EqualTo(20));
        Assert.That(document.Summary.Height, Is.EqualTo(10));
    }

    [Test]
    public void ReadStream_BmpExtractsTopDownFrameResolutionAndMasks()
    {
        byte[] bmp = BmpHeaderParserTests.CreateBmp(56);
        BmpHeaderParserTests.WriteSigned32(bmp, 18, 20);
        BmpHeaderParserTests.WriteSigned32(bmp, 22, -10);
        BmpHeaderParserTests.WriteUnsigned16(bmp, 28, 32);
        BmpHeaderParserTests.WriteUnsigned32(bmp, 30, BmpMetadataExtractor.BiBitfields);
        BmpHeaderParserTests.WriteSigned32(bmp, 38, 3780);
        BmpHeaderParserTests.WriteSigned32(bmp, 42, 3780);
        BmpHeaderParserTests.WriteUnsigned32(bmp, 54, 0x00ff0000);
        BmpHeaderParserTests.WriteUnsigned32(bmp, 58, 0x0000ff00);
        BmpHeaderParserTests.WriteUnsigned32(bmp, 62, 0x000000ff);
        BmpHeaderParserTests.WriteUnsigned32(bmp, 66, 0xff000000);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bmp));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Bmp));
        Assert.That(document.Frames[0].Width, Is.EqualTo(20));
        Assert.That(document.Frames[0].Height, Is.EqualTo(10));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.TopDown").Value.BooleanValue, Is.True);
        Assert.That(document.Summary.ResolutionText, Is.EqualTo("3780 x 3780 pixels/meter"));
    }

    [Test]
    public void ReadStream_CurDispatchesAndExtractsHotspotAndEmbeddedPng()
    {
        byte[] png = IcoMetadataParserTests.CreatePng(20, 10, 8, 6);
        byte[] cur = IcoMetadataParserTests.CreateContainer(2, new byte[][] { png }, new int[] { 20 }, new int[] { 10 }, new int[] { 0 });
        IcoMetadataParserTests.WriteUnsigned16(cur, 10, 4);
        IcoMetadataParserTests.WriteUnsigned16(cur, 12, 6);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(cur));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cur));
        Assert.That(document.Format.Name, Is.EqualTo("CUR"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Cur.Entry.HotspotX").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Cur.Entry.HotspotY").Value.IntegerValue, Is.EqualTo(6));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Width").Value.UnsignedIntegerValue, Is.EqualTo(20));
        Assert.That(document.Summary.Width, Is.EqualTo(20));
        Assert.That(document.Summary.Height, Is.EqualTo(10));
    }

    [Test]
    public void ReadStream_SvgzAutoDetectsAndExtractsSemanticMetadata()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"><title>Compressed icon</title><desc>Small graphic</desc></svg>";
        byte[] svgz = SvgMetadataParserTests.Compress(Encoding.UTF8.GetBytes(xml));

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(svgz));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svgz));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Compressed").Value.BooleanValue, Is.True);
        Assert.That(document.Summary.Width, Is.EqualTo(20));
        Assert.That(document.Summary.Height, Is.EqualTo(10));
        Assert.That(document.Summary.Title, Is.EqualTo("Compressed icon"));
        Assert.That(document.Summary.Description, Is.EqualTo("Small graphic"));
    }

    public static object[] FormatCases()
    {
        return new object[]
        {
            new object[] { CreateGif87a(20, 10, ""), ImageFormatKind.Gif, 20, 10 },
            new object[] { WebPMetadataExtractorTests.CreateWebP(new string[] { "VP8 " }, new byte[][] { WebPMetadataExtractorTests.CreateVp8(20, 10) }), ImageFormatKind.WebP, 20, 10 },
            new object[] { CreateBmp(20, 10), ImageFormatKind.Bmp, 20, 10 },
            new object[] { IcoMetadataParserTests.CreateContainer(1, new byte[][] { IcoMetadataParserTests.CreatePng(20, 10, 8, 6) }, new int[] { 20 }, new int[] { 10 }, new int[] { 32 }), ImageFormatKind.Ico, 20, 10 },
            new object[] { Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"/>"), ImageFormatKind.Svg, 20, 10 }
        };
    }

    public static byte[] CreateGif87a(int width, int height, string comment)
    {
        List<byte> bytes = GifBlockParserTests.CreateHeader(width, height, 0, 0, 0);
        byte[] version = Encoding.ASCII.GetBytes("87a");
        bytes[3] = version[0];
        bytes[4] = version[1];
        bytes[5] = version[2];
        if (comment.Length > 0)
        {
            byte[] text = Encoding.ASCII.GetBytes(comment);
            bytes.Add(33);
            bytes.Add(254);
            bytes.Add((byte)text.Length);
            GifBlockParserTests.AddBytes(bytes, text);
            bytes.Add(0);
        }

        GifBlockParserTests.AddImageDescriptor(bytes, 0, 0, width, height, 0);
        bytes.Add(2);
        bytes.Add(0);
        bytes.Add(59);
        return bytes.ToArray();
    }

    public static byte[] CreateBmp(int width, int height)
    {
        byte[] bmp = BmpHeaderParserTests.CreateBmp(40);
        BmpHeaderParserTests.WriteSigned32(bmp, 18, width);
        BmpHeaderParserTests.WriteSigned32(bmp, 22, height);
        return bmp;
    }
}
