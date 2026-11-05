using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ImagePartRawInsertionTests
{
    [Test]
    public void ImagePartReader_DetectsCommonImageFormatsWithoutDecoding()
    {
        XlsxImagePart png = new XlsxImagePartReader().ReadBytes("/xl/media/image1.png", PngBytes());
        XlsxImagePart jpeg = new XlsxImagePartReader().ReadBytesWithContentType("/xl/media/image2.jpg", "image/jpeg", JpegBytes());
        XlsxImagePart gif = new XlsxImagePartReader().ReadBytes("/xl/media/image3.gif", GifBytes());
        XlsxImagePart bmp = new XlsxImagePartReader().ReadBytes("/xl/media/image4.bmp", BmpBytes());
        XlsxImagePart tiff = new XlsxImagePartReader().ReadBytes("/xl/media/image5.tiff", TiffBytes());
        XlsxImagePart webp = new XlsxImagePartReader().ReadBytes("/xl/media/image6.webp", WebpBytes());

        Assert.That(png.IsValid, Is.True);
        Assert.That(png.Format, Is.EqualTo("png"));
        Assert.That(png.ContentType, Is.EqualTo("image/png"));
        Assert.That(png.Extension, Is.EqualTo("png"));
        Assert.That(jpeg.IsValid, Is.True);
        Assert.That(jpeg.Format, Is.EqualTo("jpeg"));
        Assert.That(jpeg.ContentType, Is.EqualTo("image/jpeg"));
        Assert.That(gif.Format, Is.EqualTo("gif"));
        Assert.That(bmp.Format, Is.EqualTo("bmp"));
        Assert.That(tiff.Format, Is.EqualTo("tiff"));
        Assert.That(webp.Format, Is.EqualTo("webp"));
    }

    [Test]
    public void ImagePartWriter_WritesRawBytesAndReportsInvalidInputs()
    {
        byte[] data = PngBytes();
        XlsxImagePart part = new XlsxImagePartReader().ReadBytes("/xl/media/image1.png", data);
        XlsxImagePartWriteResult write = new XlsxImagePartWriter().WriteBytes(part);
        XlsxImagePart mismatch = new XlsxImagePartReader().ReadBytesWithContentType("/xl/media/image1.jpg", "image/jpeg", data);
        XlsxImagePart extensionMismatch = new XlsxImagePartReader().ReadBytes("/xl/media/image1.gif", data);
        XlsxImagePart empty = new XlsxImagePartReader().ReadBytes("/xl/media/image1.png", new byte[0]);
        XlsxImagePartWriteResult? nullWrite = new XlsxImagePartWriter().WriteBytes(null!);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Bytes, Is.EqualTo(data));
        Assert.That(mismatch.IsValid, Is.False);
        Assert.That(extensionMismatch.IsValid, Is.False);
        Assert.That(empty.IsValid, Is.False);
        Assert.That(nullWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(mismatch, "image.contentTypeMismatch"), Is.True);
        Assert.That(HasDiagnostic(extensionMismatch, "image.extensionMismatch"), Is.True);
        Assert.That(HasDiagnostic(empty, "image.dataEmpty"), Is.True);
        Assert.That(HasDiagnostic(nullWrite, "imageWriter.partNull"), Is.True);
    }

    [Test]
    public void WorkbookAddRawImage_AddsMediaPartContentTypeAndSavesRawBytes()
    {
        byte[] data = PngBytes();
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxRawImageInsertResult insert = workbook.AddRawImage(data, "");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(insert.IsValid, Is.True);
        Assert.That(insert.PartPath, Is.EqualTo("/xl/media/image1.png"));
        Assert.That(insert.ContentType, Is.EqualTo("image/png"));
        Assert.That(insert.HasImagePart, Is.True);
        Assert.That(workbook.UnknownParts.ContainsPart("/xl/media/image1.png"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(package.IsValid, Is.True);
        Assert.That(package.GetEntry("/xl/media/image1.png")!.Data, Is.EqualTo(data));
        Assert.That(package.GetEntry(XlsxPackagePaths.ContentTypes)!.GetText(), Does.Contain("PartName=\"/xl/media/image1.png\""));
        Assert.That(package.GetEntry(XlsxPackagePaths.ContentTypes)!.GetText(), Does.Contain("ContentType=\"image/png\""));
    }

    [Test]
    public void WorkbookAddRawImagePart_UsesExplicitMediaPathAndReportsInvalidStates()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxRawImageInsertResult explicitPart = workbook.AddRawImagePart("/xl/media/logo.jpeg", JpegBytes(), "image/jpeg");
        XlsxRawImageInsertResult duplicate = workbook.AddRawImagePart("/xl/media/logo.jpeg", JpegBytes(), "image/jpeg");
        XlsxRawImageInsertResult outsideMedia = workbook.AddRawImagePart("/xl/drawings/logo.png", PngBytes(), "image/png");
        XlsxRawImageInsertResult unsupported = workbook.AddRawImage(UnsupportedBytes(), "");
        XlsxRawImageInsertResult mismatch = workbook.AddRawImage(PngBytes(), "image/jpeg");

        Assert.That(explicitPart.IsValid, Is.True);
        Assert.That(explicitPart.PartPath, Is.EqualTo("/xl/media/logo.jpeg"));
        Assert.That(explicitPart.ImagePart!.Format, Is.EqualTo("jpeg"));
        Assert.That(duplicate.IsValid, Is.False);
        Assert.That(outsideMedia.IsValid, Is.False);
        Assert.That(unsupported.IsValid, Is.False);
        Assert.That(mismatch.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicate, "image.partDuplicate"), Is.True);
        Assert.That(HasDiagnostic(outsideMedia, "image.partPathNotMedia"), Is.True);
        Assert.That(HasDiagnostic(unsupported, "image.formatUnsupported"), Is.True);
        Assert.That(HasDiagnostic(mismatch, "image.contentTypeMismatch"), Is.True);
    }

    static byte[] PngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    }

    static byte[] JpegBytes()
    {
        return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
    }

    static byte[] GifBytes()
    {
        return new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00 };
    }

    static byte[] BmpBytes()
    {
        return new byte[] { 0x42, 0x4D, 0x00, 0x00 };
    }

    static byte[] TiffBytes()
    {
        return new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x00 };
    }

    static byte[] WebpBytes()
    {
        return new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
    }

    static byte[] UnsupportedBytes()
    {
        return new byte[] { 0x00, 0x01, 0x02, 0x03 };
    }

    static bool HasDiagnostic(XlsxImagePart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxImagePartWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxRawImageInsertResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
