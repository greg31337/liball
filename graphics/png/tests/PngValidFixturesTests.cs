using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngValidFixturesTests
{
    [Test]
    public void CreatePng_ReturnsDeterministicBytes()
    {
        byte[] first = PngValidFixtures.CreatePng(PngPixelFormat.Rgba8, 3, 2);
        byte[] second = PngValidFixtures.CreatePng(PngPixelFormat.Rgba8, 3, 2);
        byte[] different = PngValidFixtures.CreatePng(PngPixelFormat.Rgba8, 2, 2);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.EqualTo(different));
    }

    [TestCase(PngPixelFormat.Grayscale1)]
    [TestCase(PngPixelFormat.Grayscale2)]
    [TestCase(PngPixelFormat.Grayscale4)]
    [TestCase(PngPixelFormat.Grayscale8)]
    [TestCase(PngPixelFormat.Grayscale16)]
    [TestCase(PngPixelFormat.Indexed1)]
    [TestCase(PngPixelFormat.Indexed2)]
    [TestCase(PngPixelFormat.Indexed4)]
    [TestCase(PngPixelFormat.Indexed8)]
    [TestCase(PngPixelFormat.Rgb8)]
    [TestCase(PngPixelFormat.Rgb16)]
    [TestCase(PngPixelFormat.GrayscaleAlpha8)]
    [TestCase(PngPixelFormat.GrayscaleAlpha16)]
    [TestCase(PngPixelFormat.Rgba8)]
    [TestCase(PngPixelFormat.Rgba16)]
    public void CreatePng_ReturnsValidPngForSupportedFormats(PngPixelFormat pixelFormat)
    {
        byte[] png = PngValidFixtures.CreatePng(pixelFormat, 5, 3);

        PngValidationResult validation = PngValidator.ValidateStrict(png);
        PngIdentifyResult identify = Png.IdentifyBytes(png);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.Info.Width, Is.EqualTo(5));
        Assert.That(identify.Info.Height, Is.EqualTo(3));
        Assert.That(identify.Info.PixelFormat, Is.EqualTo(pixelFormat));
    }

    [Test]
    public void CreateImage_FillsStablePixelData()
    {
        PngImage image = PngValidFixtures.CreateImage(PngPixelFormat.Rgba8, 2, 1);

        Assert.That(image.Pixels.Data, Is.EqualTo(new byte[] { 240, 21, 58, 95, 132, 169, 206, 243 }));
    }

    [Test]
    public void CreateImage_AddsDeterministicPaletteForIndexedFormats()
    {
        PngImage image = PngValidFixtures.CreateImage(PngPixelFormat.Indexed2, 3, 2);

        Assert.That(image.Metadata.Palette.Entries, Has.Length.EqualTo(4));
        Assert.That(image.Metadata.Palette.Entries[0].Red, Is.EqualTo(59));
        Assert.That(image.Metadata.Palette.Entries[0].Green, Is.EqualTo(87));
        Assert.That(image.Metadata.Palette.Entries[0].Blue, Is.EqualTo(123));
        Assert.That(image.Metadata.Palette.Entries[3].Red, Is.EqualTo(4));
        Assert.That(image.Metadata.Palette.Entries[3].Green, Is.EqualTo(174));
        Assert.That(image.Metadata.Palette.Entries[3].Blue, Is.EqualTo(156));
    }

    [Test]
    public void ConvenienceMethods_ReturnMatchingFormats()
    {
        Assert.That(Png.IdentifyBytes(PngValidFixtures.CreateRgba8Png(2, 2)).Info.PixelFormat, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(Png.IdentifyBytes(PngValidFixtures.CreateRgb8Png(2, 2)).Info.PixelFormat, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(Png.IdentifyBytes(PngValidFixtures.CreateGrayscale8Png(2, 2)).Info.PixelFormat, Is.EqualTo(PngPixelFormat.Grayscale8));
        Assert.That(Png.IdentifyBytes(PngValidFixtures.CreateIndexed8Png(2, 2)).Info.PixelFormat, Is.EqualTo(PngPixelFormat.Indexed8));
        Assert.That(PngValidFixtures.CreateRgba8Image(2, 2).Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(PngValidFixtures.CreateRgb8Image(2, 2).Pixels.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(PngValidFixtures.CreateGrayscale8Image(2, 2).Pixels.Format, Is.EqualTo(PngPixelFormat.Grayscale8));
        Assert.That(PngValidFixtures.CreateIndexed8Image(2, 2).Pixels.Format, Is.EqualTo(PngPixelFormat.Indexed8));
    }

    [Test]
    public void CreateImage_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PngValidFixtures.CreateImage(PngPixelFormat.Rgba8, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngValidFixtures.CreateImage(PngPixelFormat.Rgba8, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngValidFixtures.CreateImage(PngPixelFormat.Unknown, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngValidFixtures.CreatePng(PngPixelFormat.Rgba8, -1, 1));
    }
}
