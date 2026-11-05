using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfGeneralizedImageTests
{
    [Test]
    public void RawImageConstructorValidatesInputsAndOwnsPixelData()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PdfImage image = new PdfImage("Im1", 1, 1, data);
        data[0] = 99;

        Assert.That(image.RgbData, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => new PdfImage(null!, 1, 1, new byte[3]));
        Assert.Throws<ArgumentException>(() => new PdfImage(string.Empty, 1, 1, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImage("Im1", 0, 1, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImage("Im1", 1, 0, new byte[3]));
        Assert.Throws<ArgumentNullException>(() => new PdfImage("Im1", 1, 1, null!));
        Assert.Throws<ArgumentException>(() => new PdfImage("Im1", 1, 1, new byte[2]));
        Assert.Throws<ArgumentException>(() => new PdfImage("Im1", int.MaxValue, int.MaxValue, new byte[0]));
    }

    [Test]
    public void RawImageApisExposeFormatColorDimensionsAlphaAndPreserveCaching()
    {
        PdfDocument document = new PdfDocument();
        byte[] grayscaleData = new byte[] { 10, 20 };
        byte[] rgbData = new byte[] { 1, 2, 3, 4, 5, 6 };
        byte[] rgbaData = new byte[] { 7, 8, 9, 10, 11, 12, 13, 14 };

        PdfImage grayscale = document.AddGrayscaleImage(2, 1, grayscaleData);
        PdfImage grayscaleAgain = document.AddGrayscaleImage(2, 1, new byte[] { 10, 20 });
        PdfImage rgb = document.AddRgbImage(2, 1, rgbData);
        PdfImage rgbLegacy = document.AddImage(2, 1, new byte[] { 1, 2, 3, 4, 5, 6 });
        PdfImage rgba = document.AddRgbaImage(2, 1, rgbaData);
        PdfImage rgbaAgain = document.AddRgbaImage(2, 1, new byte[] { 7, 8, 9, 10, 11, 12, 13, 14 });
        grayscaleData[0] = 99;
        rgbaData[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(grayscaleAgain, Is.SameAs(grayscale));
            Assert.That(rgbLegacy, Is.SameAs(rgb));
            Assert.That(rgbaAgain, Is.SameAs(rgba));
            Assert.That(document.Images.Count, Is.EqualTo(3));
            Assert.That(grayscale.GetFormat(), Is.EqualTo(PdfImageFormat.Grayscale));
            Assert.That(grayscale.GetColorSpace(), Is.EqualTo(PdfImageColorSpace.Grayscale));
            Assert.That(rgb.GetFormat(), Is.EqualTo(PdfImageFormat.Rgb));
            Assert.That(rgb.GetColorSpace(), Is.EqualTo(PdfImageColorSpace.Rgb));
            Assert.That(rgba.GetFormat(), Is.EqualTo(PdfImageFormat.Rgba));
            Assert.That(rgba.GetColorSpace(), Is.EqualTo(PdfImageColorSpace.Rgb));
            Assert.That(rgba.HasAlpha(), Is.True);
            Assert.That(grayscale.HasAlpha(), Is.False);
            Assert.That(grayscale.HasDpi(), Is.False);
            Assert.That(grayscale.GetHorizontalDpi(), Is.EqualTo(0));
            Assert.That(grayscale.GetVerticalDpi(), Is.EqualTo(0));
            Assert.That(rgba.GetPixelWidth(), Is.EqualTo(2));
            Assert.That(rgba.GetPixelHeight(), Is.EqualTo(1));
            Assert.That(grayscale.RgbData[0], Is.EqualTo(10));
            Assert.That(rgba.EncodedData[0], Is.EqualTo(7));
            Assert.That(rgba.RgbData, Is.EqualTo(new byte[] { 7, 8, 9, 11, 12, 13 }));
            Assert.That(rgba.SoftMaskData, Is.EqualTo(new byte[] { 10, 14 }));
        });
    }

    [Test]
    public void ImageDpiApiExposesEncodedMetadataAndSupportsValidatedRawOverrides()
    {
        PdfDocument document = new PdfDocument();
        byte[] jpegData = TestJpeg.Create(3, 2, 3, 1, 300, 150, false);
        PdfImage jpeg = document.AddEncodedImage(jpegData);
        PdfImage raw = document.AddGrayscaleImage(1, 1, new byte[] { 0 });
        IPdfImage rawApi = raw;

        rawApi.SetDpi(96, 72);

        Assert.Multiple(() =>
        {
            Assert.That(jpeg.GetFormat(), Is.EqualTo(PdfImageFormat.Jpeg));
            Assert.That(jpeg.GetPixelWidth(), Is.EqualTo(3));
            Assert.That(jpeg.GetPixelHeight(), Is.EqualTo(2));
            Assert.That(jpeg.HasDpi(), Is.True);
            Assert.That(jpeg.GetHorizontalDpi(), Is.EqualTo(300));
            Assert.That(jpeg.GetVerticalDpi(), Is.EqualTo(150));
            Assert.That(rawApi.HasDpi(), Is.True);
            Assert.That(rawApi.GetHorizontalDpi(), Is.EqualTo(96));
            Assert.That(rawApi.GetVerticalDpi(), Is.EqualTo(72));
        });

        Assert.Throws<ArgumentOutOfRangeException>(() => raw.SetDpi(0, 72));
        Assert.Throws<ArgumentOutOfRangeException>(() => raw.SetDpi(96, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => raw.SetDpi(double.PositiveInfinity, 72));
    }

    [Test]
    public void ImageAccessibilityMetadataValidatesAndSwitchesModes()
    {
        PdfImage image = new PdfImage("Im1", 1, 1, new byte[3]);

        image.MarkDecorative();
        Assert.That(image.IsDecorative, Is.True);
        Assert.That(image.AlternateText, Is.Empty);

        image.SetAlternateText("Quarterly revenue");
        Assert.That(image.IsDecorative, Is.False);
        Assert.That(image.AlternateText, Is.EqualTo("Quarterly revenue"));
        Assert.Throws<ArgumentNullException>(() => image.SetAlternateText(null!));
        Assert.Throws<ArgumentException>(() => image.SetAlternateText(string.Empty));
    }

    [Test]
    public void EncodedImageApisDetectJpegAndPngAcrossAllInputsAndReuseSpecificCaches()
    {
        byte[] jpegData = TestJpeg.Create(2, 3, 1, -1, 0, 0, false);
        int[] samples = TestPng.CreateSamples(2, 2, 6, 8);
        byte[] pngData = TestPng.Create(2, 2, 6, 8, samples, false, 0, new byte[0], new byte[0], -1, -1);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "general-image-test-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(path, pngData);
        try
        {
            IPdfDocument documentApi = new PdfDocument();
            PdfImage jpeg = documentApi.AddEncodedImage(jpegData);
            PdfImage jpegAgain = documentApi.AddEncodedImageFromSpan(jpegData.AsSpan());
            PdfImage jpegSpecific = documentApi.AddJpeg(jpegData);
            PdfImage png = documentApi.AddEncodedImage(pngData);
            using MemoryStream stream = new MemoryStream(pngData);
            PdfImage pngStream = documentApi.AddEncodedImageFromStream(stream);
            PdfImage pngFile = documentApi.AddEncodedImageFromFile(path);
            PdfImage pngSpecific = documentApi.AddPng(pngData);
            PdfDocument document = (PdfDocument)documentApi;

            Assert.That(jpegAgain, Is.SameAs(jpeg));
            Assert.That(jpegSpecific, Is.SameAs(jpeg));
            Assert.That(pngStream, Is.SameAs(png));
            Assert.That(pngFile, Is.SameAs(png));
            Assert.That(pngSpecific, Is.SameAs(png));
            Assert.That(jpeg.GetFormat(), Is.EqualTo(PdfImageFormat.Jpeg));
            Assert.That(jpeg.GetColorSpace(), Is.EqualTo(PdfImageColorSpace.Grayscale));
            Assert.That(png.GetFormat(), Is.EqualTo(PdfImageFormat.Png));
            Assert.That(png.HasAlpha(), Is.True);
            Assert.That(document.Images.Count, Is.EqualTo(2));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RawImageWriterEmitsGrayscaleAndRgbaSoftMaskSamples()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(100, 100);
        document.AddGrayscaleImage(2, 1, new byte[] { 1, 2 });
        document.AddRgbaImage(2, 1, new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 });

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/ColorSpace /DeviceGray /BitsPerComponent 8"));
        Assert.That(pdf, Does.Contain("/ColorSpace /DeviceRGB /BitsPerComponent 8 /SMask "));
        Assert.That(pdf, Does.Contain("0102>"));
        Assert.That(pdf, Does.Contain("0A141E323C46>"));
        Assert.That(pdf, Does.Contain("2850>"));
        Assert.That(pdf, Does.Contain("/Filter /ASCIIHexDecode"));
    }

    [Test]
    public void CanvasImageDrawingEmitsAlternateAndDecorativeMarkedContentWithCropClipping()
    {
        PdfImage informative = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);
        PdfImage decorative = new PdfImage("Im2", 1, 1, new byte[3]);
        informative.SetAlternateText("Revenue (Q1)");
        decorative.MarkDecorative();
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawImageCropFill(informative, 10, 20, 100, 100);
        canvas.DrawImage(decorative, 1, 2, 3, 4);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 100 100 re"));
        Assert.That(content, Does.Contain("/Figure << /Alt (Revenue \\(Q1\\)) >> BDC"));
        Assert.That(content, Does.Contain("/Artifact BMC"));
        Assert.That(content, Does.Contain("/Im1 Do\nQ\nEMC"));
        Assert.That(content, Does.Contain("/Im2 Do\nQ\nEMC"));
    }

    [Test]
    public void ImageBlockUsesConfiguredFitClippingAndImageMetadata()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);
        image.SetAlternateText("Wide chart");
        PdfImageBlock block = new PdfImageBlock(image, 100, 100);
        PdfPage page = new PdfPage(200, 200);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 100, 100);

        Assert.That(block.GetFitMode(), Is.EqualTo(PdfImageFitMode.Stretch));
        block.SetFitMode(PdfImageFitMode.CropFill);
        block.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(block.GetFitMode(), Is.EqualTo(PdfImageFitMode.CropFill));
        Assert.That(content, Does.Contain("10 20 100 100 re"));
        Assert.That(content, Does.Contain("200 0 0 100 -40 20 cm"));
        Assert.That(content, Does.Contain("/Figure << /Alt (Wide chart) >> BDC"));
        Assert.Throws<ArgumentOutOfRangeException>(() => block.SetFitMode((PdfImageFitMode)99));
    }

    [Test]
    public void ImageBlockPreserveAspectFitUsesLetterboxedPlacement()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);
        PdfImageBlock block = new PdfImageBlock(image, 100, 100);
        PdfPage page = new PdfPage(200, 200);

        block.SetFitMode(PdfImageFitMode.PreserveAspectFit);
        block.Draw(new PdfLayoutDrawContext(page, 10, 20, 100, 100));

        string content = page.Canvas.ToPdfString();
        Assert.That(content, Does.Contain("100 0 0 50 10 45 cm"));
        Assert.That(content, Does.Not.Contain("10 20 100 100 re"));
    }

    [Test]
    public void GeneralizedImageApisRejectInvalidRawAndEncodedInputs()
    {
        PdfDocument document = new PdfDocument();
        MemoryStream unreadable = new MemoryStream();
        unreadable.Dispose();

        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddGrayscaleImage(0, 1, new byte[1]));
        Assert.Throws<ArgumentNullException>(() => document.AddGrayscaleImage(1, 1, null!));
        Assert.Throws<ArgumentException>(() => document.AddGrayscaleImage(2, 1, new byte[1]));
        Assert.Throws<ArgumentException>(() => document.AddRgbImage(1, 1, new byte[2]));
        Assert.Throws<ArgumentNullException>(() => document.AddRgbaImage(1, 1, null!));
        Assert.Throws<ArgumentException>(() => document.AddRgbaImage(1, 1, new byte[3]));
        Assert.Throws<ArgumentNullException>(() => document.AddEncodedImage(null!));
        Assert.Throws<InvalidDataException>(() => document.AddEncodedImage(new byte[] { 1, 2, 3, 4 }));
        Assert.Throws<ArgumentNullException>(() => document.AddEncodedImageFromStream(null!));
        Assert.Throws<ArgumentException>(() => document.AddEncodedImageFromStream(unreadable));
        Assert.Throws<ArgumentNullException>(() => document.AddEncodedImageFromFile(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfCanvas().DrawImage(null!, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfCanvas().DrawImage(new PdfImage("Im1", 1, 1, new byte[3]), 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageBlock(new PdfImage("Im1", 1, 1, new byte[3]), double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageBlock(new PdfImage("Im1", 1, 1, new byte[3]), 1, double.PositiveInfinity));
    }
}
