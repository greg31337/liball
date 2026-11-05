namespace barcode.Tests;

public class StaticDataTests
{
    [TestCase(BarcodeStandards.Code128, "ISO/IEC 15417:2007")]
    [TestCase(BarcodeStandards.Code128Amendment, "ISO/IEC 15417:2007/Amd 1:2026")]
    [TestCase(BarcodeStandards.EanUpc, "ISO/IEC 15420:2025")]
    [TestCase(BarcodeStandards.Code39, "ISO/IEC 16388:2023")]
    [TestCase(BarcodeStandards.DataMatrix, "ISO/IEC 16022:2024")]
    [TestCase(BarcodeStandards.Pdf417, "ISO/IEC 15438:2015")]
    [TestCase(BarcodeStandards.Itf, "ISO/IEC 16390:2007")]
    [TestCase(BarcodeStandards.SymbologyIdentifiers, "ISO/IEC 15424:2025")]
    [TestCase(BarcodeStandards.Codabar, "AIM BC3-2000 / USS Codabar")]
    [TestCase(BarcodeStandards.Gs1GeneralSpecifications, "GS1 General Specifications 26.0")]
    [TestCase(BarcodeStandards.Gs1SyntaxDictionarySnapshot, "GS1 Barcode Syntax Dictionary, 2026-08-26 snapshot")]
    [TestCase(BarcodeStandards.Gs1DigitalLinkUriSyntax, "GS1 Digital Link URI Syntax 1.7.0")]
    public void StandardsExposeThePlannedEdition(string actual, string expected)
    {
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(BarcodeSymbologyIdentifiers.Code128, "]C0")]
    [TestCase(BarcodeSymbologyIdentifiers.Gs1128, "]C1")]
    [TestCase(BarcodeSymbologyIdentifiers.EanUpc, "]E0")]
    [TestCase(BarcodeSymbologyIdentifiers.Code39, "]A0")]
    [TestCase(BarcodeSymbologyIdentifiers.Itf, "]I0")]
    [TestCase(BarcodeSymbologyIdentifiers.Itf14, "]I1")]
    [TestCase(BarcodeSymbologyIdentifiers.Codabar, "]F0")]
    [TestCase(BarcodeSymbologyIdentifiers.DataMatrix, "]d1")]
    [TestCase(BarcodeSymbologyIdentifiers.Gs1DataMatrix, "]d2")]
    [TestCase(BarcodeSymbologyIdentifiers.Pdf417, "]L0")]
    public void SymbologyIdentifiersExposeTheCarrierAndModifier(string actual, string expected)
    {
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(BarcodeMediaTypes.Png, "image/png")]
    [TestCase(BarcodeMediaTypes.Bmp, "image/bmp")]
    [TestCase(BarcodeMediaTypes.Svg, "image/svg+xml")]
    [TestCase(BarcodeMediaTypes.Raw1, "application/octet-stream")]
    [TestCase(BarcodeMediaTypes.Gray8, "application/octet-stream")]
    public void MediaTypesExposeTheRenderedContentType(string actual, string expected)
    {
        Assert.That(actual, Is.EqualTo(expected));
    }
}
