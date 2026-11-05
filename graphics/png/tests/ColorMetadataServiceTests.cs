using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ColorMetadataServiceTests
{
    [Test]
    public void ResolveColorMetadata_UsesTypedMetadataPrecedenceThroughPublicContract()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngMetadata metadata = new PngMetadata();
        metadata.Gamma = CreateGamma(100000);
        metadata.StandardRgb = new PngStandardRgb();
        metadata.IccProfile = new PngIccProfile();
        metadata.CodingIndependentCodePoints = CreateCicp(1, 13, 0, 1);

        PngColorMetadata result = service.ResolveColorMetadata(metadata);

        Assert.That(result.SourceChunkType, Is.EqualTo("cICP"));
        Assert.That(result.Precedence, Is.EqualTo(1));
        Assert.That(result.HasCodingIndependentCodePoints, Is.True);
        Assert.That(result.HasIccProfile, Is.False);
        Assert.That(result.HasStandardRgb, Is.False);
        Assert.That(result.HasGamma, Is.False);
    }

    [Test]
    public void NormalizeToSrgb_HonorsCicpPrecedenceOverLowerGamma()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 40 });
        image.Metadata.CodingIndependentCodePoints = CreateCicp(1, 13, 0, 1);
        image.Metadata.Gamma = CreateGamma(100000);

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, new PngColorNormalizationOptions());

        Assert.That(result.ColorMetadata.SourceChunkType, Is.EqualTo("cICP"));
        Assert.That(result.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
        Assert.That(result.Pixels.Data, Is.Not.SameAs(image.Pixels.Data));
        Assert.That(result.AppliedGammaTransform, Is.False);
        Assert.That(result.IsSrgbLike, Is.True);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void NormalizeToSrgb_AppliesSignificantBitsBackgroundAndGamma()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Rgba8, new byte[] { 0xAF, 0xAF, 0xAF, 128 });
        image.Metadata.Gamma = CreateGamma(100000);
        PngSignificantBits bits = new PngSignificantBits();
        bits.Red = 4;
        bits.Green = 4;
        bits.Blue = 4;
        bits.Alpha = 8;
        image.Metadata.SignificantBits = bits;
        PngColorNormalizationOptions options = new PngColorNormalizationOptions();
        options.CompositeOverBackground = true;
        options.Background = new PngBackgroundColor();
        byte expected = (byte)(Math.Pow(85.0 / 255.0, 0.45455) * 255.0 + 0.5);

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, options);

        Assert.That(result.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { expected, expected, expected }));
        Assert.That(result.AppliedSignificantBits, Is.True);
        Assert.That(result.AppliedBackgroundComposite, Is.True);
        Assert.That(result.AppliedGammaTransform, Is.True);
        Assert.That(result.IsSrgbLike, Is.True);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void NormalizeToSrgb_ReportsUnavailableIccTransformWithoutUsingLowerMetadata()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Rgb8, new byte[] { 40, 80, 120 });
        PngIccProfile profile = new PngIccProfile();
        profile.Name = "Custom";
        profile.Profile = new byte[] { 1, 2, 3, 4 };
        image.Metadata.IccProfile = profile;
        image.Metadata.StandardRgb = new PngStandardRgb();
        image.Metadata.Gamma = CreateGamma(100000);

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, new PngColorNormalizationOptions());

        Assert.That(result.ColorMetadata.SourceChunkType, Is.EqualTo("iCCP"));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 40, 80, 120 }));
        Assert.That(result.AppliedGammaTransform, Is.False);
        Assert.That(result.IsSrgbLike, Is.False);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("IccProfileTransformUnavailable"));
        Assert.That(result.Warnings[0].ChunkType, Is.EqualTo("iCCP"));
        Assert.That(result.Warnings[0].Message, Does.Contain("built-in .NET"));
    }

    [Test]
    public void NormalizeToSrgb_ReportsUnsupportedCicpWithoutFallingBackToGamma()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Rgb8, new byte[] { 64, 128, 192 });
        image.Metadata.CodingIndependentCodePoints = CreateCicp(9, 16, 0, 1);
        image.Metadata.Gamma = CreateGamma(100000);

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, new PngColorNormalizationOptions());

        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 64, 128, 192 }));
        Assert.That(result.IsSrgbLike, Is.False);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("UnsupportedCicpTransform"));
        Assert.That(result.Warnings[0].ChunkType, Is.EqualTo("cICP"));
    }

    [Test]
    public void NormalizeToSrgb_UsesIndexedPaletteBackgroundForCompositing()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Indexed8, new byte[] { 0 });
        PngPaletteEntry transparent = new PngPaletteEntry();
        transparent.Red = 200;
        transparent.Green = 100;
        transparent.Blue = 50;
        PngPaletteEntry backgroundEntry = new PngPaletteEntry();
        backgroundEntry.Red = 20;
        backgroundEntry.Green = 40;
        backgroundEntry.Blue = 60;
        image.Metadata.Palette.Entries = new PngPaletteEntry[] { transparent, backgroundEntry };
        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = new byte[] { 0 };
        image.Metadata.IndexedTransparency = transparency;
        PngBackgroundColor background = new PngBackgroundColor();
        background.PaletteIndex = 1;
        image.Metadata.BackgroundColor = background;
        image.Metadata.StandardRgb = new PngStandardRgb();
        PngColorNormalizationOptions options = new PngColorNormalizationOptions();
        options.CompositeOverBackground = true;

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, options);

        Assert.That(result.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 20, 40, 60 }));
        Assert.That(result.AppliedBackgroundComposite, Is.True);
        Assert.That(result.IsSrgbLike, Is.True);
    }

    [Test]
    public void NormalizeToSrgb_ExpandsTransparencyBeforeBackgroundCompositing()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Grayscale8, new byte[] { 77 });
        PngGrayscaleTransparency transparency = new PngGrayscaleTransparency();
        transparency.Gray = 77;
        image.Metadata.GrayscaleTransparency = transparency;
        image.Metadata.StandardRgb = new PngStandardRgb();
        PngColorNormalizationOptions options = new PngColorNormalizationOptions();
        options.CompositeOverBackground = true;
        PngBackgroundColor background = new PngBackgroundColor();
        background.Gray = 25;
        options.Background = background;

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, options);

        Assert.That(result.Pixels.Format, Is.EqualTo(PngPixelFormat.Grayscale8));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 25 }));
        Assert.That(result.AppliedBackgroundComposite, Is.True);
    }

    [Test]
    public void NormalizeToSrgb_AppliesGammaForStandardChromaticitiesTier()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.Rgb8, new byte[] { 64, 64, 64 });
        image.Metadata.Chromaticities = CreateStandardChromaticities();
        image.Metadata.Gamma = CreateGamma(100000);
        byte expected = (byte)(Math.Pow(64.0 / 255.0, 0.45455) * 255.0 + 0.5);

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, new PngColorNormalizationOptions());

        Assert.That(result.ColorMetadata.SourceChunkType, Is.EqualTo("cHRM/gAMA"));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { expected, expected, expected }));
        Assert.That(result.AppliedGammaTransform, Is.True);
        Assert.That(result.IsSrgbLike, Is.True);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void NormalizeToSrgb_ReportsMissingRequestedBackground()
    {
        IPngColorMetadataService service = new PngColorMetadataService();
        PngImage image = CreateImage(PngPixelFormat.GrayscaleAlpha8, new byte[] { 100, 128 });
        image.Metadata.StandardRgb = new PngStandardRgb();
        PngColorNormalizationOptions options = new PngColorNormalizationOptions();
        options.CompositeOverBackground = true;

        PngColorNormalizationResult result = service.NormalizeToSrgb(image, options);

        Assert.That(result.Pixels.Format, Is.EqualTo(PngPixelFormat.GrayscaleAlpha8));
        Assert.That(result.Pixels.Data, Is.EqualTo(new byte[] { 100, 128 }));
        Assert.That(result.AppliedBackgroundComposite, Is.False);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("BackgroundUnavailable"));
    }

    static PngImage CreateImage(PngPixelFormat format, byte[] data)
    {
        PngImage image = new PngImage();
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = format;
        pixels.Width = 1;
        pixels.Height = 1;
        pixels.Stride = data.Length;
        pixels.Data = data;
        image.Pixels = pixels;
        return image;
    }

    static PngGamma CreateGamma(uint value)
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = value;
        return gamma;
    }

    static PngCodingIndependentCodePoints CreateCicp(byte primaries, byte transfer, byte matrix, byte fullRange)
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = primaries;
        codePoints.TransferCharacteristics = transfer;
        codePoints.MatrixCoefficients = matrix;
        codePoints.VideoFullRangeFlag = fullRange;
        return codePoints;
    }

    static PngChromaticities CreateStandardChromaticities()
    {
        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = 31270;
        chromaticities.WhitePointY = 32900;
        chromaticities.RedX = 64000;
        chromaticities.RedY = 33000;
        chromaticities.GreenX = 30000;
        chromaticities.GreenY = 60000;
        chromaticities.BlueX = 15000;
        chromaticities.BlueY = 6000;
        return chromaticities;
    }
}
