using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfImageCacheTests
{
    [Test]
    public void AddRgbImage_ReusesIdenticalImageData()
    {
        PdfImageCache cache = new PdfImageCache();

        PdfImage first = cache.AddRgbImage(1, 1, new byte[] { 1, 2, 3 });
        PdfImage second = cache.AddRgbImage(1, 1, new byte[] { 1, 2, 3 });

        Assert.That(second, Is.SameAs(first));
        Assert.That(first.Name, Is.EqualTo("Im1"));
        Assert.That(cache.Images.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddRgbImage_CreatesNewResourceForDifferentDataOrDimensions()
    {
        PdfImageCache cache = new PdfImageCache();

        PdfImage first = cache.AddRgbImage(1, 1, new byte[] { 1, 2, 3 });
        PdfImage second = cache.AddRgbImage(1, 1, new byte[] { 1, 2, 4 });
        PdfImage third = cache.AddRgbImage(2, 1, new byte[] { 1, 2, 3, 1, 2, 3 });

        Assert.That(first.Name, Is.EqualTo("Im1"));
        Assert.That(second.Name, Is.EqualTo("Im2"));
        Assert.That(third.Name, Is.EqualTo("Im3"));
        Assert.That(cache.Images.Count, Is.EqualTo(3));
    }

    [Test]
    public void AddRgbImage_CopiesImageDataBeforeCaching()
    {
        PdfImageCache cache = new PdfImageCache();
        byte[] data = new byte[] { 1, 2, 3 };

        PdfImage first = cache.AddRgbImage(1, 1, data);
        data[0] = 9;
        PdfImage second = cache.AddRgbImage(1, 1, data);

        Assert.That(first.RgbData[0], Is.EqualTo(1));
        Assert.That(second.RgbData[0], Is.EqualTo(9));
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(cache.Images.Count, Is.EqualTo(2));
    }

    [Test]
    public void Document_AddImageReusesCachedImageResources()
    {
        PdfDocument document = new PdfDocument();

        PdfImage first = document.AddImage(1, 1, new byte[] { 1, 2, 3 });
        PdfImage second = document.AddImage(1, 1, new byte[] { 1, 2, 3 });

        Assert.That(second, Is.SameAs(first));
        Assert.That(document.Images.Count, Is.EqualTo(1));
        Assert.That(document.Images[0].Name, Is.EqualTo("Im1"));
    }

    [Test]
    public void Save_WritesSingleImageObjectForRepeatedImageData()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfImage first = document.AddImage(1, 1, new byte[] { 1, 2, 3 });
        PdfImage second = document.AddImage(1, 1, new byte[] { 1, 2, 3 });

        page.Canvas.DrawImage(first, 10, 20, 30, 40);
        page.Canvas.DrawImage(second, 50, 20, 30, 40);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("/XObject << /Im1 5 0 R >>"));
        Assert.That(pdf, Does.Contain("/Im1 Do"));
        Assert.That(pdf, Does.Not.Contain("/Im2"));
        Assert.That(pdf, Does.Not.Contain("6 0 obj\n<< /Type /XObject /Subtype /Image"));
    }

    [Test]
    public void AddRgbImage_RejectsInvalidInputs()
    {
        PdfImageCache cache = new PdfImageCache();

        Assert.Throws<ArgumentOutOfRangeException>(() => cache.AddRgbImage(0, 1, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.AddRgbImage(1, 0, new byte[3]));
        Assert.Throws<ArgumentNullException>(() => cache.AddRgbImage(1, 1, null!));
        Assert.Throws<ArgumentException>(() => cache.AddRgbImage(1, 1, new byte[2]));
        Assert.Throws<ArgumentException>(() => cache.AddRgbImage(int.MaxValue, int.MaxValue, new byte[0]));
    }
}
