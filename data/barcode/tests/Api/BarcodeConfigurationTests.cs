namespace barcode.Tests;

public class BarcodeConfigurationTests
{
    [Test]
    public void StoresEveryConfigurationSelection()
    {
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.BinarizationMode = BarcodeBinarizationMode.Hybrid;
        configuration.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        configuration.Dimension = BarcodeDimension.StackedLinear;
        configuration.Effort = BarcodeEffort.Thorough;
        configuration.Format = BarcodeFormat.MacroPdf417;
        configuration.LocalizationMode = BarcodeLocalizationMode.FinderPatterns;
        configuration.MirrorMode = BarcodeMirrorMode.Both;
        configuration.PixelFormat = BarcodePixelFormat.Bgra32;
        configuration.Polarity = BarcodePolarity.LightOnDark;
        configuration.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        configuration.RenderFormat = BarcodeRenderFormat.Svg;
        configuration.Rotation = BarcodeRotation.Clockwise270;
        configuration.TextMode = BarcodeTextMode.Custom;
        configuration.ValidationMode = BarcodeValidationMode.Tolerant;
        configuration.ValidationSeverity = BarcodeValidationSeverity.Warning;
        configuration.Code128CodeSet = Code128CodeSet.C;
        configuration.Code39CharacterMode = Code39CharacterMode.FullAscii;
        configuration.DataMatrixEncodingMode = DataMatrixEncodingMode.Base256;
        configuration.DataMatrixShape = DataMatrixShape.StandardRectangular;
        configuration.Gs1Syntax = Gs1Syntax.DigitalLinkUri;
        configuration.Pdf417CompactionMode = Pdf417CompactionMode.Numeric;
        configuration.Pdf417Variant = Pdf417Variant.Compact;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.BinarizationMode, Is.EqualTo(BarcodeBinarizationMode.Hybrid));
            Assert.That(configuration.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.VerifyStrip));
            Assert.That(configuration.Dimension, Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(configuration.Effort, Is.EqualTo(BarcodeEffort.Thorough));
            Assert.That(configuration.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(configuration.LocalizationMode, Is.EqualTo(BarcodeLocalizationMode.FinderPatterns));
            Assert.That(configuration.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Both));
            Assert.That(configuration.PixelFormat, Is.EqualTo(BarcodePixelFormat.Bgra32));
            Assert.That(configuration.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(configuration.QuietZonePolicy, Is.EqualTo(BarcodeQuietZonePolicy.ExactCustom));
            Assert.That(configuration.RenderFormat, Is.EqualTo(BarcodeRenderFormat.Svg));
            Assert.That(configuration.Rotation, Is.EqualTo(BarcodeRotation.Clockwise270));
            Assert.That(configuration.TextMode, Is.EqualTo(BarcodeTextMode.Custom));
            Assert.That(configuration.ValidationMode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(configuration.ValidationSeverity, Is.EqualTo(BarcodeValidationSeverity.Warning));
            Assert.That(configuration.Code128CodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(configuration.Code39CharacterMode, Is.EqualTo(Code39CharacterMode.FullAscii));
            Assert.That(configuration.DataMatrixEncodingMode, Is.EqualTo(DataMatrixEncodingMode.Base256));
            Assert.That(configuration.DataMatrixShape, Is.EqualTo(DataMatrixShape.StandardRectangular));
            Assert.That(configuration.Gs1Syntax, Is.EqualTo(Gs1Syntax.DigitalLinkUri));
            Assert.That(configuration.Pdf417CompactionMode, Is.EqualTo(Pdf417CompactionMode.Numeric));
            Assert.That(configuration.Pdf417Variant, Is.EqualTo(Pdf417Variant.Compact));
        });
    }
}
