namespace barcode.Tests;

public class EanUpcScanlineDecoderTests
{
    [TestCase(BarcodeFormat.Ean13, "4006381333931", 11, 7, 95)]
    [TestCase(BarcodeFormat.Ean8, "55123457", 7, 7, 67)]
    [TestCase(BarcodeFormat.UpcA, "036000291452", 9, 9, 95)]
    [TestCase(BarcodeFormat.UpcE, "01234558", 9, 7, 51)]
    public void ForwardDecodeRecoversExactFormatPayloadAndGeometry(
        BarcodeFormat format,
        string expectedText,
        int leftQuietZone,
        int rightQuietZone,
        int moduleCount)
    {
        EanUpcEncodedData encoded = CreateEncoded(format, expectedText);
        BitRow row = CreateScanline(encoded.Modules, leftQuietZone, rightQuietZone, 1);

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(format));
            Assert.That(decoded.Text, Is.EqualTo(expectedText));
            Assert.That(decoded.Digits, Is.EqualTo(ToDigits(expectedText)));
            Assert.That(decoded.ChecksumValue, Is.EqualTo(expectedText[^1] - '0'));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.StartPosition, Is.EqualTo(leftQuietZone));
            Assert.That(decoded.EndPosition, Is.EqualTo(leftQuietZone + moduleCount));
            Assert.That(decoded.ModuleSize, Is.EqualTo(1.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
            Assert.That(decoded.Result.Format, Is.EqualTo(format));
            Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo(expectedText));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(ToAsciiBytes(expectedText)));
            Assert.That(decoded.Result.Payload.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata!.HasChecksum, Is.True);
            Assert.That(decoded.Result.Metadata.ChecksumValid, Is.True);
            Assert.That(decoded.Result.Metadata.ChecksumValue, Is.EqualTo(expectedText[^1] - '0'));
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]E0"));
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
            Assert.That(decoded.Result.Metadata.IsGs1, Is.True);
        });
    }

    [TestCase(BarcodeFormat.Ean13, "4006381333931", 11, 7, 95)]
    [TestCase(BarcodeFormat.Ean8, "55123457", 7, 7, 67)]
    [TestCase(BarcodeFormat.UpcA, "036000291452", 9, 9, 95)]
    [TestCase(BarcodeFormat.UpcE, "11234555", 9, 7, 51)]
    public void ReversedDecodePreservesLogicalPayloadAndOriginalCoordinates(
        BarcodeFormat format,
        string expectedText,
        int leftQuietZone,
        int rightQuietZone,
        int moduleCount)
    {
        EanUpcEncodedData encoded = CreateEncoded(format, expectedText);
        BitRow row = CreateScanline(encoded.Modules, leftQuietZone, rightQuietZone, 2);
        row.ReverseBits();

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(format));
            Assert.That(decoded.Text, Is.EqualTo(expectedText));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(rightQuietZone * 2));
            Assert.That(decoded.EndPosition, Is.EqualTo((rightQuietZone + moduleCount) * 2));
            Assert.That(decoded.ModuleSize, Is.EqualTo(2.0));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(180.0));
        });
    }

    [Test]
    public void Ean13LeadingParityReconstructsEveryLeadingDigit()
    {
        int leadingDigit = 0;
        while (leadingDigit <= 9)
        {
            string data = (char)('0' + leadingDigit) + "12345678901";
            string complete = Gs1Modulo10.AppendCheckDigit(data);
            EanUpcEncodedData encoded = EanUpcEncoder.Encode(
                complete,
                BarcodeFormat.Ean13,
                BarcodeChecksumMode.Require);
            int rightQuietZone = leadingDigit == 0 ? 7 : 9;
            EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(
                CreateScanline(encoded.Modules, 11, rightQuietZone, 1));

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.Ean13));
                Assert.That(decoded.Text, Is.EqualTo(complete));
                Assert.That(ToParityString(decoded.LeftParity), Is.EqualTo(ToParityString(encoded.LeftParity)));
            });
            leadingDigit++;
        }
    }

    [TestCase("01234558", "012345000058", "0012345000058")]
    [TestCase("11234555", "112345000055", "0112345000055")]
    [TestCase("09847531", "098400000751", "0098400000751")]
    public void UpcEParityReconstructsNumberSystemCheckAndExpansion(
        string upcE,
        string expandedUpcA,
        string ean13Compatible)
    {
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.UpcE, upcE);

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(
            CreateScanline(encoded.Modules, 9, 7, 1));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.UpcE));
            Assert.That(decoded.Text, Is.EqualTo(upcE));
            Assert.That(decoded.ExpandedUpcAText, Is.EqualTo(expandedUpcA));
            Assert.That(decoded.Ean13CompatibleText, Is.EqualTo(ean13Compatible));
            Assert.That(ToParityString(decoded.LeftParity), Is.EqualTo(ToParityString(encoded.LeftParity)));
        });
    }

    [Test]
    public void UpcAPreservesIdentityAndEan13CompatibleMetadata()
    {
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.UpcA, "036000291452");

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(
            CreateScanline(encoded.Modules, 9, 9, 1));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(decoded.Text, Is.EqualTo("036000291452"));
            Assert.That(decoded.Ean13CompatibleText, Is.EqualTo("0036000291452"));
            Assert.That(decoded.ExpandedUpcAText, Is.EqualTo("036000291452"));
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(decoded.Result.Metadata!.Codewords, Is.EqualTo(ToDigits("036000291452")));
        });
    }

    [Test]
    public void LeadingZeroWithEan13MarginsRemainsEan13WhenUpcRightMarginIsAbsent()
    {
        string ean13Text = "0036000291452";
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.Ean13, ean13Text);

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(
            CreateScanline(encoded.Modules, 11, 7, 1));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.Ean13));
            Assert.That(decoded.Text, Is.EqualTo(ean13Text));
            Assert.That(decoded.Ean13CompatibleText, Is.EqualTo(ean13Text));
            Assert.That(decoded.ExpandedUpcAText, Is.Empty);
        });
    }

    [Test]
    public void DecoderAcceptsIntegralScalingAndBoundedRunNoise()
    {
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.Ean13, "4006381333931");
        BitRow scaled = CreateScanline(encoded.Modules, 11, 7, 3);
        BitRow noisy = CreateNoisyScanline(encoded.Modules, 11, 7);

        EanUpcDecodedData scaledResult = EanUpcScanlineDecoder.DecodeRow(scaled);
        EanUpcDecodedData noisyResult = EanUpcScanlineDecoder.DecodeRow(noisy);

        Assert.Multiple(() =>
        {
            Assert.That(scaledResult.Text, Is.EqualTo("4006381333931"));
            Assert.That(scaledResult.ModuleSize, Is.EqualTo(3.0));
            Assert.That(noisyResult.Text, Is.EqualTo("4006381333931"));
            Assert.That(noisyResult.AverageVariance, Is.GreaterThan(0.0));
            Assert.That(noisyResult.AverageVariance, Is.LessThan(EanUpcScanlineDecoder.MaximumAverageVariance));
            Assert.That(noisyResult.Result.Metadata!.Confidence, Is.LessThan(1.0));
            Assert.That(noisyResult.Result.Metadata.Confidence, Is.GreaterThan(0.0));
        });
    }

    [TestCase(BarcodeFormat.Ean13, "4006381333931", 11, 7)]
    [TestCase(BarcodeFormat.Ean8, "55123457", 7, 7)]
    [TestCase(BarcodeFormat.UpcA, "036000291452", 9, 9)]
    [TestCase(BarcodeFormat.UpcE, "01234558", 9, 7)]
    public void DecoderRequiresBothFormatSpecificQuietZones(
        BarcodeFormat format,
        string text,
        int leftQuietZone,
        int rightQuietZone)
    {
        EanUpcEncodedData encoded = CreateEncoded(format, text);

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(
                CreateScanline(encoded.Modules, leftQuietZone - 1, rightQuietZone, 2)), Is.Null);
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(
                CreateScanline(encoded.Modules, leftQuietZone, rightQuietZone - 1, 2)), Is.Null);
        });
    }

    [Test]
    public void DecoderRejectsChecksumDamageGuardDamageAndAddonOnlyRows()
    {
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.Ean13, "4006381333931");
        byte[] wrongChecksumModules = CopyModules(encoded.Modules);
        byte[] replacement = EanUpcPatterns.GetDigitModules(1, encoded.LeftParity[0]);
        CopyModulesAt(replacement, wrongChecksumModules, 3);
        byte[] damagedGuardModules = CopyModules(encoded.Modules);
        damagedGuardModules[47] = 1;
        EanUpcEncodedData addOn = EanUpcAddon.Encode("12");

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(
                CreateScanline(wrongChecksumModules, 11, 7, 1)), Is.Null);
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(
                CreateScanline(damagedGuardModules, 11, 7, 1)), Is.Null);
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(
                CreateScanline(addOn.Modules, 10, 10, 1)), Is.Null);
        });
    }

    [Test]
    public void DecoderSkipsFalseDarkRunsBeforeAValidSymbol()
    {
        EanUpcEncodedData encoded = CreateEncoded(BarcodeFormat.Ean8, "55123457");
        BitRow symbol = CreateScanline(encoded.Modules, 7, 7, 1);
        BitRow row = new BitRow(symbol.Length + 20);
        row.SetBit(1, true);
        int index = 0;
        while (index < symbol.Length)
        {
            if (symbol.GetBit(index))
            {
                row.SetBit(index + 20, true);
            }

            index++;
        }

        EanUpcDecodedData decoded = EanUpcScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.Ean8));
            Assert.That(decoded.Text, Is.EqualTo("55123457"));
            Assert.That(decoded.StartPosition, Is.EqualTo(27));
            Assert.That(decoded.EndPosition, Is.EqualTo(94));
        });
    }

    [Test]
    public void ExceptionAndAttemptApisHandleInvalidRows()
    {
        BitRow empty = new BitRow(40);

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcScanlineDecoder.TryDecodeRow(empty), Is.Null);
            Assert.That(
                () => EanUpcScanlineDecoder.DecodeRow(empty),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid EAN/UPC symbol."));
            Assert.That(
                () => EanUpcScanlineDecoder.TryDecodeRow(null!),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static EanUpcEncodedData CreateEncoded(BarcodeFormat format, string text)
    {
        if (format == BarcodeFormat.UpcE)
        {
            return EanUpcEncoder.EncodeUpcE(
                text,
                text[0] - '0',
                BarcodeChecksumMode.Require);
        }

        return EanUpcEncoder.Encode(text, format, BarcodeChecksumMode.Require);
    }

    static BitRow CreateScanline(byte[] modules, int leftQuietModules, int rightQuietModules, int scale)
    {
        int length = (leftQuietModules + modules.Length + rightQuietModules) * scale;
        BitRow row = new BitRow(length);
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            if (modules[moduleIndex] != 0)
            {
                int pixel = 0;
                while (pixel < scale)
                {
                    row.SetBit(((leftQuietModules + moduleIndex) * scale) + pixel, true);
                    pixel++;
                }
            }

            moduleIndex++;
        }

        return row;
    }

    static BitRow CreateNoisyScanline(byte[] modules, int leftQuietModules, int rightQuietModules)
    {
        BitRow scaled = CreateScanline(modules, leftQuietModules, rightQuietModules, 3);
        int[] runs = RunLength.ExtractAll(scaled);
        runs[5]++;
        runs[6]--;
        return RunLength.ExpandRuns(runs, false);
    }

    static int[] ToDigits(string text)
    {
        return text.Select(character => character - '0').ToArray();
    }

    static byte[] ToAsciiBytes(string text)
    {
        return text.Select(character => (byte)character).ToArray();
    }

    static string ToParityString(EanUpcDigitSet[] parity)
    {
        return new string(parity.Select(value => value == EanUpcDigitSet.L ? 'L' : 'G').ToArray());
    }

    static byte[] CopyModules(byte[] modules)
    {
        byte[] copy = new byte[modules.Length];
        global::System.Array.Copy(modules, copy, modules.Length);
        return copy;
    }

    static void CopyModulesAt(byte[] source, byte[] destination, int offset)
    {
        global::System.Array.Copy(source, 0, destination, offset, source.Length);
    }
}
