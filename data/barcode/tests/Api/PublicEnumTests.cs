namespace barcode.Tests;

public class PublicEnumTests
{
    [TestCase(BarcodeFormat.Automatic, 0)]
    [TestCase(BarcodeFormat.Code128, 1)]
    [TestCase(BarcodeFormat.Gs1128, 2)]
    [TestCase(BarcodeFormat.Ean8, 3)]
    [TestCase(BarcodeFormat.Ean13, 4)]
    [TestCase(BarcodeFormat.UpcA, 5)]
    [TestCase(BarcodeFormat.UpcE, 6)]
    [TestCase(BarcodeFormat.EanUpcAddon2, 7)]
    [TestCase(BarcodeFormat.EanUpcAddon5, 8)]
    [TestCase(BarcodeFormat.Code39, 9)]
    [TestCase(BarcodeFormat.Itf, 10)]
    [TestCase(BarcodeFormat.Itf14, 11)]
    [TestCase(BarcodeFormat.Codabar, 12)]
    [TestCase(BarcodeFormat.DataMatrix, 13)]
    [TestCase(BarcodeFormat.Gs1DataMatrix, 14)]
    [TestCase(BarcodeFormat.Pdf417, 15)]
    [TestCase(BarcodeFormat.CompactPdf417, 16)]
    [TestCase(BarcodeFormat.MacroPdf417, 17)]
    public void BarcodeFormatHasStableValue(BarcodeFormat value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeDimension.Linear, 0)]
    [TestCase(BarcodeDimension.Matrix, 1)]
    [TestCase(BarcodeDimension.StackedLinear, 2)]
    public void BarcodeDimensionHasStableValue(BarcodeDimension value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodePixelFormat.Gray8, 0)]
    [TestCase(BarcodePixelFormat.Rgb24, 1)]
    [TestCase(BarcodePixelFormat.Bgr24, 2)]
    [TestCase(BarcodePixelFormat.Rgba32, 3)]
    [TestCase(BarcodePixelFormat.Bgra32, 4)]
    public void BarcodePixelFormatHasStableValue(BarcodePixelFormat value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeRotation.None, 0)]
    [TestCase(BarcodeRotation.Clockwise90, 1)]
    [TestCase(BarcodeRotation.Clockwise180, 2)]
    [TestCase(BarcodeRotation.Clockwise270, 3)]
    public void BarcodeRotationHasStableValue(BarcodeRotation value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeMirrorMode.Normal, 0)]
    [TestCase(BarcodeMirrorMode.Mirrored, 1)]
    [TestCase(BarcodeMirrorMode.Both, 2)]
    public void BarcodeMirrorModeHasStableValue(BarcodeMirrorMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodePolarity.DarkOnLight, 0)]
    [TestCase(BarcodePolarity.LightOnDark, 1)]
    [TestCase(BarcodePolarity.Automatic, 2)]
    public void BarcodePolarityHasStableValue(BarcodePolarity value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeEffort.Fast, 0)]
    [TestCase(BarcodeEffort.Balanced, 1)]
    [TestCase(BarcodeEffort.Thorough, 2)]
    public void BarcodeEffortHasStableValue(BarcodeEffort value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeBinarizationMode.Global, 0)]
    [TestCase(BarcodeBinarizationMode.Otsu, 1)]
    [TestCase(BarcodeBinarizationMode.Row, 2)]
    [TestCase(BarcodeBinarizationMode.Adaptive, 3)]
    [TestCase(BarcodeBinarizationMode.Hybrid, 4)]
    [TestCase(BarcodeBinarizationMode.Automatic, 5)]
    public void BarcodeBinarizationModeHasStableValue(BarcodeBinarizationMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeLocalizationMode.Direct, 0)]
    [TestCase(BarcodeLocalizationMode.ConnectedBlocks, 1)]
    [TestCase(BarcodeLocalizationMode.Lines, 2)]
    [TestCase(BarcodeLocalizationMode.FinderPatterns, 3)]
    [TestCase(BarcodeLocalizationMode.Automatic, 4)]
    public void BarcodeLocalizationModeHasStableValue(BarcodeLocalizationMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeChecksumMode.None, 0)]
    [TestCase(BarcodeChecksumMode.Calculate, 1)]
    [TestCase(BarcodeChecksumMode.Require, 2)]
    [TestCase(BarcodeChecksumMode.VerifyKeep, 3)]
    [TestCase(BarcodeChecksumMode.VerifyStrip, 4)]
    public void BarcodeChecksumModeHasStableValue(BarcodeChecksumMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeQuietZonePolicy.Standard, 0)]
    [TestCase(BarcodeQuietZonePolicy.CustomAtLeastStandard, 1)]
    [TestCase(BarcodeQuietZonePolicy.ExactCustom, 2)]
    public void BarcodeQuietZonePolicyHasStableValue(BarcodeQuietZonePolicy value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeTextMode.Hidden, 0)]
    [TestCase(BarcodeTextMode.Standard, 1)]
    [TestCase(BarcodeTextMode.RawPayload, 2)]
    [TestCase(BarcodeTextMode.Custom, 3)]
    public void BarcodeTextModeHasStableValue(BarcodeTextMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeRenderFormat.Raw1, 0)]
    [TestCase(BarcodeRenderFormat.Gray8, 1)]
    [TestCase(BarcodeRenderFormat.Png, 2)]
    [TestCase(BarcodeRenderFormat.Bmp, 3)]
    [TestCase(BarcodeRenderFormat.Svg, 4)]
    public void BarcodeRenderFormatHasStableValue(BarcodeRenderFormat value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeValidationSeverity.Information, 0)]
    [TestCase(BarcodeValidationSeverity.Warning, 1)]
    [TestCase(BarcodeValidationSeverity.Error, 2)]
    public void BarcodeValidationSeverityHasStableValue(BarcodeValidationSeverity value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BarcodeValidationMode.Strict, 0)]
    [TestCase(BarcodeValidationMode.Tolerant, 1)]
    public void BarcodeValidationModeHasStableValue(BarcodeValidationMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(Code128CodeSet.Automatic, 0)]
    [TestCase(Code128CodeSet.A, 1)]
    [TestCase(Code128CodeSet.B, 2)]
    [TestCase(Code128CodeSet.C, 3)]
    public void Code128CodeSetHasStableValue(Code128CodeSet value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(Code39CharacterMode.Standard, 0)]
    [TestCase(Code39CharacterMode.FullAscii, 1)]
    public void Code39CharacterModeHasStableValue(Code39CharacterMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(DataMatrixShape.Automatic, 0)]
    [TestCase(DataMatrixShape.Square, 1)]
    [TestCase(DataMatrixShape.StandardRectangular, 2)]
    public void DataMatrixShapeHasStableValue(DataMatrixShape value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(DataMatrixEncodingMode.Automatic, 0)]
    [TestCase(DataMatrixEncodingMode.Ascii, 1)]
    [TestCase(DataMatrixEncodingMode.C40, 2)]
    [TestCase(DataMatrixEncodingMode.Text, 3)]
    [TestCase(DataMatrixEncodingMode.X12, 4)]
    [TestCase(DataMatrixEncodingMode.Edifact, 5)]
    [TestCase(DataMatrixEncodingMode.Base256, 6)]
    public void DataMatrixEncodingModeHasStableValue(DataMatrixEncodingMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(Pdf417CompactionMode.Automatic, 0)]
    [TestCase(Pdf417CompactionMode.Text, 1)]
    [TestCase(Pdf417CompactionMode.Byte, 2)]
    [TestCase(Pdf417CompactionMode.Numeric, 3)]
    public void Pdf417CompactionModeHasStableValue(Pdf417CompactionMode value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(Pdf417Variant.Standard, 0)]
    [TestCase(Pdf417Variant.Compact, 1)]
    public void Pdf417VariantHasStableValue(Pdf417Variant value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(Gs1Syntax.Bracketed, 0)]
    [TestCase(Gs1Syntax.ElementString, 1)]
    [TestCase(Gs1Syntax.BarcodeMessage, 2)]
    [TestCase(Gs1Syntax.ScannerData, 3)]
    [TestCase(Gs1Syntax.HumanReadableInterpretation, 4)]
    [TestCase(Gs1Syntax.DigitalLinkUri, 5)]
    public void Gs1SyntaxHasStableValue(Gs1Syntax value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }
}
