using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ImageDetectorTests
{
    [Test]
    public void Detect_ReadsImageTypesAndDimensionsFromHeaders()
    {
        AssertImage(PptxImageDetectorCore.Detect(CreatePngBytes()), PptxImageType.Png, PptxContentTypes.Png, "png", 320, 200);
        AssertImage(PptxImageDetectorCore.Detect(CreateJpegBytes()), PptxImageType.Jpeg, PptxContentTypes.Jpeg, "jpg", 640, 480);
        AssertImage(PptxImageDetectorCore.Detect(CreateGifBytes()), PptxImageType.Gif, PptxContentTypes.Gif, "gif", 120, 80);
        AssertImage(PptxImageDetectorCore.Detect(CreateBmpBytes()), PptxImageType.Bmp, PptxContentTypes.Bmp, "bmp", 1024, 768);
        AssertImage(PptxImageDetectorCore.Detect(CreateTiffBytes()), PptxImageType.Tiff, PptxContentTypes.Tiff, "tif", 300, 150);
        AssertImage(PptxImageDetectorCore.Detect(CreateSvgBytes()), PptxImageType.Svg, PptxContentTypes.Svg, "svg", 500, 250);
        AssertImage(PptxImageDetectorCore.Detect(CreateSvgViewBoxBytes()), PptxImageType.Svg, PptxContentTypes.Svg, "svg", 640, 360);
        AssertImage(PptxImageDetectorCore.Detect(CreateEmfBytes()), PptxImageType.Emf, PptxContentTypes.Emf, "emf", 800, 600);
        AssertImage(PptxImageDetectorCore.Detect(CreateWmfBytes()), PptxImageType.Wmf, PptxContentTypes.Wmf, "wmf", 400, 300);
    }

    [Test]
    public void Detect_ReportsUnknownWhenHeaderIsNotRecognized()
    {
        PptxImageInfo info = PptxImageDetectorCore.Detect(new byte[] { 1, 2, 3, 4 });

        Assert.That(info.Type, Is.EqualTo(PptxImageType.Unknown));
        Assert.That(info.ContentType, Is.EqualTo(PptxContentTypes.Binary));
        Assert.That(info.Extension, Is.Empty);
        Assert.That(info.HasDimensions(), Is.False);
    }

    [Test]
    public void Detect_CanReadImagePartBytes()
    {
        PptxPart part = new PptxPart("/ppt/media/image1.png", PptxContentTypes.Png, CreatePngBytes());

        PptxImageInfo info = PptxImageDetectorCore.DetectPart(part);

        Assert.That(info.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(info.WidthPixels, Is.EqualTo(320));
        Assert.That(info.HeightPixels, Is.EqualTo(200));
    }

    [Test]
    public void Detect_MalformedLargeDimensionsDoNotOverflow()
    {
        byte[] bmp = CreateBmpBytes();
        WriteInt32Little(bmp, 22, int.MinValue);
        byte[] tiff = CreateTiffBytes();
        WriteInt32Little(tiff, 4, int.MaxValue);
        byte[] emf = CreateEmfBytes();
        WriteInt32Little(emf, 8, int.MinValue);
        WriteInt32Little(emf, 16, int.MaxValue);

        PptxImageInfo bmpInfo = PptxImageDetectorCore.Detect(bmp);
        PptxImageInfo tiffInfo = PptxImageDetectorCore.Detect(tiff);
        PptxImageInfo emfInfo = PptxImageDetectorCore.Detect(emf);

        Assert.That(bmpInfo.Type, Is.EqualTo(PptxImageType.Bmp));
        Assert.That(bmpInfo.HeightPixels, Is.EqualTo(0));
        Assert.That(tiffInfo.Type, Is.EqualTo(PptxImageType.Tiff));
        Assert.That(tiffInfo.HasDimensions(), Is.False);
        Assert.That(emfInfo.Type, Is.EqualTo(PptxImageType.Emf));
        Assert.That(emfInfo.WidthPixels, Is.EqualTo(0));
    }

    [Test]
    public void Detect_OversizedSvgDimensionsDoNotOverflow()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg width=\"999999999999999999999999\" height=\"999999999999999999999999\"></svg>");

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);

        Assert.That(info.Type, Is.EqualTo(PptxImageType.Svg));
        Assert.That(info.HasDimensions(), Is.False);
    }

    static void AssertImage(PptxImageInfo info, string type, string contentType, string extension, int width, int height)
    {
        Assert.That(info.Type, Is.EqualTo(type));
        Assert.That(info.ContentType, Is.EqualTo(contentType));
        Assert.That(info.Extension, Is.EqualTo(extension));
        Assert.That(info.WidthPixels, Is.EqualTo(width));
        Assert.That(info.HeightPixels, Is.EqualTo(height));
        Assert.That(info.HasDimensions(), Is.True);
    }

    static byte[] CreatePngBytes()
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        WriteInt32Big(bytes, 16, 320);
        WriteInt32Big(bytes, 20, 200);
        return bytes;
    }

    static byte[] CreateJpegBytes()
    {
        byte[] bytes = new byte[39];
        bytes[0] = 255;
        bytes[1] = 216;
        bytes[2] = 255;
        bytes[3] = 224;
        WriteInt16Big(bytes, 4, 16);
        bytes[20] = 255;
        bytes[21] = 192;
        WriteInt16Big(bytes, 22, 17);
        bytes[24] = 8;
        WriteInt16Big(bytes, 25, 480);
        WriteInt16Big(bytes, 27, 640);
        bytes[29] = 3;
        return bytes;
    }

    static byte[] CreateGifBytes()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("GIF89a0000");
        WriteInt16Little(bytes, 6, 120);
        WriteInt16Little(bytes, 8, 80);
        return bytes;
    }

    static byte[] CreateBmpBytes()
    {
        byte[] bytes = new byte[54];
        bytes[0] = 66;
        bytes[1] = 77;
        WriteInt32Little(bytes, 18, 1024);
        WriteInt32Little(bytes, 22, 768);
        return bytes;
    }

    static byte[] CreateTiffBytes()
    {
        byte[] bytes = new byte[38];
        bytes[0] = 73;
        bytes[1] = 73;
        WriteInt16Little(bytes, 2, 42);
        WriteInt32Little(bytes, 4, 8);
        WriteInt16Little(bytes, 8, 2);
        WriteTiffLongEntry(bytes, 10, 256, 300);
        WriteTiffLongEntry(bytes, 22, 257, 150);
        return bytes;
    }

    static byte[] CreateSvgBytes()
    {
        return Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"500px\" height=\"250px\"></svg>");
    }

    static byte[] CreateSvgViewBoxBytes()
    {
        return Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 360\"></svg>");
    }

    static byte[] CreateEmfBytes()
    {
        byte[] bytes = new byte[44];
        WriteInt32Little(bytes, 0, 1);
        WriteInt32Little(bytes, 8, 0);
        WriteInt32Little(bytes, 12, 0);
        WriteInt32Little(bytes, 16, 800);
        WriteInt32Little(bytes, 20, 600);
        bytes[40] = 32;
        bytes[41] = 69;
        bytes[42] = 77;
        bytes[43] = 70;
        return bytes;
    }

    static byte[] CreateWmfBytes()
    {
        byte[] bytes = new byte[22];
        bytes[0] = 215;
        bytes[1] = 205;
        bytes[2] = 198;
        bytes[3] = 154;
        WriteInt16Little(bytes, 6, 0);
        WriteInt16Little(bytes, 8, 0);
        WriteInt16Little(bytes, 10, 400);
        WriteInt16Little(bytes, 12, 300);
        WriteInt16Little(bytes, 14, 1440);
        return bytes;
    }

    static void WriteTiffLongEntry(byte[] bytes, int offset, int tag, int value)
    {
        WriteInt16Little(bytes, offset, tag);
        WriteInt16Little(bytes, offset + 2, 4);
        WriteInt32Little(bytes, offset + 4, 1);
        WriteInt32Little(bytes, offset + 8, value);
    }

    static void WriteInt16Little(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 255);
        bytes[offset + 1] = (byte)((value / 256) & 255);
    }

    static void WriteInt16Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value / 256) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }

    static void WriteInt32Little(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 255);
        bytes[offset + 1] = (byte)((value / 256) & 255);
        bytes[offset + 2] = (byte)((value / 65536) & 255);
        bytes[offset + 3] = (byte)((value / 16777216) & 255);
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value / 16777216) & 255);
        bytes[offset + 1] = (byte)((value / 65536) & 255);
        bytes[offset + 2] = (byte)((value / 256) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
