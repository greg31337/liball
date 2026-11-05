using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfBarcodeTests
{
    [Test]
    public void QrEncoder_CreatesStandardByteModeDataAndReedSolomonPolynomial()
    {
        byte[] data = PdfQrEncoder.CreateDataCodewords(Encoding.UTF8.GetBytes("HELLO"), 1, 0);
        byte[] unicodeData = PdfQrEncoder.CreateDataCodewords(Encoding.UTF8.GetBytes("é"), 1, 0);
        byte[] divisor = PdfQrEncoder.CreateReedSolomonDivisor(7);

        Assert.That(data, Is.EqualTo(new byte[] { 64, 84, 132, 84, 196, 196, 240, 236, 17, 236, 17, 236, 17, 236, 17, 236, 17, 236, 17 }));
        Assert.That(unicodeData[0], Is.EqualTo(113));
        Assert.That(unicodeData[1], Is.EqualTo(164));
        Assert.That(divisor, Is.EqualTo(new byte[] { 127, 122, 154, 164, 11, 68, 117 }));
    }

    [Test]
    public void QrEncoder_SelectsVersionAndDrawsFunctionPatterns()
    {
        PdfQrMatrix small = PdfQrEncoder.Encode("HELLO", PdfQrErrorCorrection.Low);
        PdfQrMatrix larger = PdfQrEncoder.Encode(new string('x', 100), PdfQrErrorCorrection.High);
        PdfQrMatrix versionSeven = PdfQrEncoder.Encode(new string('y', 140), PdfQrErrorCorrection.Low);

        Assert.That(small.Size, Is.EqualTo(21));
        Assert.That(larger.Size, Is.GreaterThan(21));
        Assert.That(versionSeven.Size, Is.EqualTo(45));
        AssertFinderPattern(small, 0, 0);
        AssertFinderPattern(small, 14, 0);
        AssertFinderPattern(small, 0, 14);
        Assert.That(small.Modules[8, 13], Is.True);
        Assert.That(versionSeven.Modules[6, 22], Is.True);
        Assert.That(versionSeven.Modules[7, 22], Is.False);
        Assert.That(versionSeven.Modules[8, 22], Is.True);
        Assert.That(versionSeven.FunctionModules[8, 22], Is.True);
        Assert.That(CountDarkModules(small), Is.GreaterThan(100));
        Assert.That(CountDarkModules(small), Is.LessThan(340));
    }

    [Test]
    public void QrEncoder_SupportsFullModelTwoVersionRange()
    {
        PdfQrMatrix matrix = PdfQrEncoder.Encode(new string('z', 2900), PdfQrErrorCorrection.Low);

        Assert.That(matrix.Size, Is.EqualTo(177));
        Assert.That(matrix.FunctionModules[166, 0], Is.True);
        Assert.That(matrix.FunctionModules[0, 166], Is.True);
    }

    [TestCase(PdfQrErrorCorrection.Low)]
    [TestCase(PdfQrErrorCorrection.Medium)]
    [TestCase(PdfQrErrorCorrection.Quartile)]
    [TestCase(PdfQrErrorCorrection.High)]
    public void QrEncoder_SupportsEveryErrorCorrectionLevelAndUtf8(PdfQrErrorCorrection errorCorrection)
    {
        PdfQrMatrix matrix = PdfQrEncoder.Encode("Grüße 東京", errorCorrection);

        Assert.That(matrix.Size, Is.GreaterThanOrEqualTo(21));
        AssertFinderPattern(matrix, 0, 0);
    }

    [Test]
    public void QrApis_CreateReusableVectorAndMonochromeImageResources()
    {
        IPdfDocument document = new PdfDocument();

        PdfFormXObject form = document.AddQrCode("HELLO", 2, PdfQrErrorCorrection.Low, 4);
        PdfImage image = document.AddQrCodeImage("HELLO", 2, PdfQrErrorCorrection.Low, 4);

        Assert.Multiple(() =>
        {
            Assert.That(form.Width, Is.EqualTo(58));
            Assert.That(form.Height, Is.EqualTo(58));
            Assert.That(form.Canvas.ToPdfString(), Does.Contain(" re f"));
            Assert.That(image.GetFormat(), Is.EqualTo(PdfImageFormat.Grayscale));
            Assert.That(image.GetColorSpace(), Is.EqualTo(PdfImageColorSpace.Grayscale));
            Assert.That(image.GetPixelWidth(), Is.EqualTo(58));
            Assert.That(image.GetPixelHeight(), Is.EqualTo(58));
            Assert.That(image.RgbData[0], Is.EqualTo(255));
            Assert.That(image.RgbData[8 * 58 + 8], Is.EqualTo(0));
            Assert.That(image.RgbData[8 * 58 + 22], Is.EqualTo(255));
        });
    }

    [Test]
    public void Ean13Encoder_ProducesPublishedNinetyFiveModulePatternAndCheckDigit()
    {
        PdfLinearBarcode barcode = PdfLinearBarcodeEncoder.Encode("400638133393", PdfBarcodeFormat.Ean13);
        string core = ModulesToString(barcode.Modules, 11, barcode.Modules.Length - 22);
        string expected = "101" +
            "0001101" + "0100111" + "0101111" + "0111101" + "0001001" + "0110011" +
            "01010" +
            "1000010" + "1000010" + "1000010" + "1110100" + "1000010" + "1100110" +
            "101";

        Assert.That(barcode.DisplayText, Is.EqualTo("4006381333931"));
        Assert.That(barcode.Modules.Length, Is.EqualTo(117));
        Assert.That(core, Is.EqualTo(expected));
    }

    [Test]
    public void UpcAndCodeEncoders_CreateExpectedNormalizedSymbols()
    {
        PdfLinearBarcode upc = PdfLinearBarcodeEncoder.Encode("03600029145", PdfBarcodeFormat.UpcA);
        PdfLinearBarcode ean = PdfLinearBarcodeEncoder.Encode("0036000291452", PdfBarcodeFormat.Ean13);
        PdfLinearBarcode code39 = PdfLinearBarcodeEncoder.Encode("abc-12", PdfBarcodeFormat.Code39);
        PdfLinearBarcode code128 = PdfLinearBarcodeEncoder.Encode("AB", PdfBarcodeFormat.Code128);

        Assert.Multiple(() =>
        {
            Assert.That(upc.DisplayText, Is.EqualTo("036000291452"));
            Assert.That(upc.Modules, Is.EqualTo(ean.Modules));
            Assert.That(code39.DisplayText, Is.EqualTo("ABC-12"));
            Assert.That(code128.Modules.Length, Is.EqualTo(77));
            Assert.That(ModulesToString(code128.Modules, 10, 11), Is.EqualTo("11010010000"));
            Assert.That(ModulesToString(code128.Modules, 43, 11), Is.EqualTo("11110101110"));
            Assert.That(ModulesToString(code128.Modules, 54, 13), Is.EqualTo("1100011101011"));
        });
    }

    [Test]
    public void BarcodeApis_RenderBarsOptionalTextAndRasterPixels()
    {
        PdfDocument document = new PdfDocument();
        PdfFormXObject withText = document.AddBarcode("abc-12", PdfBarcodeFormat.Code39, 1, 40, true);
        PdfFormXObject barsOnly = document.AddBarcode("03600029145", PdfBarcodeFormat.UpcA, 1, 40, false);
        PdfImage image = document.AddBarcodeImage("AB", PdfBarcodeFormat.Code128, 2, 20);

        Assert.Multiple(() =>
        {
            Assert.That(withText.Height, Is.EqualTo(54));
            Assert.That(withText.Canvas.ToPdfString(), Does.Contain("(ABC-12) Tj"));
            Assert.That(withText.Canvas.ToPdfString(), Does.Contain(" re f"));
            Assert.That(barsOnly.Height, Is.EqualTo(40));
            Assert.That(barsOnly.Canvas.ToPdfString(), Does.Not.Contain("BT"));
            Assert.That(image.GetPixelWidth(), Is.EqualTo(154));
            Assert.That(image.GetPixelHeight(), Is.EqualTo(20));
            Assert.That(image.RgbData[0], Is.EqualTo(255));
            Assert.That(image.RgbData[20], Is.EqualTo(0));
        });
    }

    [Test]
    public void GeneratedBarcodeResourcesSerializeAsFormsAndGrayscaleImages()
    {
        PdfDocument document = new PdfDocument();
        PdfFormXObject form = document.AddQrCode("invoice:42", 1, PdfQrErrorCorrection.Medium, 4);
        document.AddBarcodeImage("4006381333931", PdfBarcodeFormat.Ean13, 1, 30);
        PdfPage page = document.AddPage(200, 200);
        page.Canvas.DrawForm(form, 10, 10, form.Width, form.Height);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/Subtype /Form"));
        Assert.That(pdf, Does.Contain("/Subtype /Image"));
        Assert.That(pdf, Does.Contain("/ColorSpace /DeviceGray"));
        Assert.That(pdf, Does.Contain("/Fm1 Do"));
    }

    [Test]
    public void BarcodeApis_RejectInvalidPayloadsFormatsAndDimensions()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() => document.AddQrCode(null!, 1, PdfQrErrorCorrection.Low, 4));
        Assert.Throws<ArgumentException>(() => document.AddQrCode(string.Empty, 1, PdfQrErrorCorrection.Low, 4));
        Assert.Throws<ArgumentException>(() => document.AddQrCode(new string('x', 10000), 1, PdfQrErrorCorrection.High, 4));
        Assert.Throws<ArgumentException>(() => document.AddQrCode("x", 1, (PdfQrErrorCorrection)99, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddQrCode("x", 0, PdfQrErrorCorrection.Low, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddQrCodeImage("x", 1, PdfQrErrorCorrection.Low, -1));
        Assert.Throws<ArgumentNullException>(() => document.AddBarcode(null!, PdfBarcodeFormat.Code39, 1, 20, false));
        Assert.Throws<ArgumentException>(() => document.AddBarcode("@", PdfBarcodeFormat.Code39, 1, 20, false));
        Assert.Throws<ArgumentException>(() => document.AddBarcode("line\nbreak", PdfBarcodeFormat.Code128, 1, 20, false));
        Assert.Throws<ArgumentException>(() => document.AddBarcode("4006381333932", PdfBarcodeFormat.Ean13, 1, 20, false));
        Assert.Throws<ArgumentException>(() => document.AddBarcode("036000291453", PdfBarcodeFormat.UpcA, 1, 20, false));
        Assert.Throws<ArgumentException>(() => document.AddBarcode("x", (PdfBarcodeFormat)99, 1, 20, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddBarcode("x", PdfBarcodeFormat.Code128, double.NaN, 20, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddBarcodeImage("x", PdfBarcodeFormat.Code128, 1, 0));
    }

    static void AssertFinderPattern(PdfQrMatrix matrix, int left, int top)
    {
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool expected = x == 0 || x == 6 || y == 0 || y == 6 || x >= 2 && x <= 4 && y >= 2 && y <= 4;
                Assert.That(matrix.Modules[left + x, top + y], Is.EqualTo(expected), "Finder module " + (left + x).ToString() + "," + (top + y).ToString());
            }
        }
    }

    static int CountDarkModules(PdfQrMatrix matrix)
    {
        int count = 0;
        for (int y = 0; y < matrix.Size; y++)
        {
            for (int x = 0; x < matrix.Size; x++)
            {
                if (matrix.Modules[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    static string ModulesToString(bool[] modules, int start, int length)
    {
        StringBuilder result = new StringBuilder(length);
        for (int index = 0; index < length; index++)
        {
            result.Append(modules[start + index] ? '1' : '0');
        }

        return result.ToString();
    }
}
