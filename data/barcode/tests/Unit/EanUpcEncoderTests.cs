namespace barcode.Tests;

public class EanUpcEncoderTests
{
    [Test]
    public void Ean13CompleteVectorMatchesIndependentModuleOracle()
    {
        string expectedModules =
            "101" +
            "0001101" +
            "0100111" +
            "0101111" +
            "0111101" +
            "0001001" +
            "0110011" +
            "01010" +
            "1000010" +
            "1000010" +
            "1000010" +
            "1110100" +
            "1000010" +
            "1100110" +
            "101";

        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.Ean13));
            Assert.That(encoded.Text, Is.EqualTo("4006381333931"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 4, 0, 0, 6, 3, 8, 1, 3, 3, 3, 9, 3, 1 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("LGLLGG"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcEncoder.Ean13ModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(1));
            Assert.That(encoded.ChecksumCalculated, Is.False);
        });
    }

    [Test]
    public void Ean8CompleteVectorMatchesIndependentModuleOracle()
    {
        string expectedModules =
            "101" +
            "0110001" +
            "0110001" +
            "0011001" +
            "0010011" +
            "01010" +
            "1000010" +
            "1011100" +
            "1001110" +
            "1000100" +
            "101";

        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "55123457",
            BarcodeFormat.Ean8,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.Ean8));
            Assert.That(encoded.Text, Is.EqualTo("55123457"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 5, 5, 1, 2, 3, 4, 5, 7 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("LLLL"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcEncoder.Ean8ModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(7));
            Assert.That(encoded.ChecksumCalculated, Is.False);
        });
    }

    [TestCase(BarcodeFormat.Ean13, "400638133393", "4006381333931", 95)]
    [TestCase(BarcodeFormat.Ean8, "5512345", "55123457", 67)]
    public void MissingCheckDigitIsCalculatedAndAppended(
        BarcodeFormat format,
        string dataDigits,
        string expectedCompleteDigits,
        int expectedModuleCount)
    {
        EanUpcEncodedData automatic = EanUpcEncoder.Encode(
            dataDigits,
            format,
            BarcodeChecksumMode.None);
        EanUpcEncodedData calculated = EanUpcEncoder.Encode(
            dataDigits,
            format,
            BarcodeChecksumMode.Calculate);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Text, Is.EqualTo(expectedCompleteDigits));
            Assert.That(automatic.Modules, Has.Length.EqualTo(expectedModuleCount));
            Assert.That(automatic.ChecksumCalculated, Is.True);
            Assert.That(calculated.Text, Is.EqualTo(expectedCompleteDigits));
            Assert.That(calculated.Modules, Is.EqualTo(automatic.Modules));
            Assert.That(calculated.ChecksumCalculated, Is.True);
        });
    }

    [Test]
    public void Ean13LeadingDigitSelectsEveryPublishedParityPattern()
    {
        string[] expected =
            "LLLLLL LLGLGG LLGGLG LLGGGL LGLLGG LGGLLG LGGGLL LGLGLG LGLGGL LGGLGL".Split(' ');
        int leadingDigit = 0;
        while (leadingDigit < expected.Length)
        {
            string prefix = leadingDigit + "12345678901";
            EanUpcEncodedData encoded = EanUpcEncoder.Encode(
                prefix,
                BarcodeFormat.Ean13,
                BarcodeChecksumMode.Calculate);

            Assert.That(
                ToParityString(encoded.LeftParity),
                Is.EqualTo(expected[leadingDigit]),
                $"Leading digit {leadingDigit}");
            leadingDigit = leadingDigit + 1;
        }
    }

    [TestCase(BarcodeChecksumMode.None)]
    [TestCase(BarcodeChecksumMode.Calculate)]
    [TestCase(BarcodeChecksumMode.Require)]
    [TestCase(BarcodeChecksumMode.VerifyKeep)]
    [TestCase(BarcodeChecksumMode.VerifyStrip)]
    public void CompleteInputIsStrictlyVerifiedInEveryChecksumMode(BarcodeChecksumMode checksumMode)
    {
        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            checksumMode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Text, Is.EqualTo("4006381333931"));
            Assert.That(encoded.ChecksumCalculated, Is.False);
            Assert.That(
                () => EanUpcEncoder.Encode("4006381333932", BarcodeFormat.Ean13, checksumMode),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The supplied EAN/UPC check digit is invalid."));
        });
    }

    [TestCase(BarcodeFormat.Ean13, "400638133393")]
    [TestCase(BarcodeFormat.Ean8, "5512345")]
    public void SuppliedCheckModesRejectDataOnlyInput(BarcodeFormat format, string dataDigits)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => EanUpcEncoder.Encode(dataDigits, format, BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.Encode(dataDigits, format, BarcodeChecksumMode.VerifyKeep),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.Encode(dataDigits, format, BarcodeChecksumMode.VerifyStrip),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsMissingWrongLengthAndNonAsciiInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => EncodeEan13(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13(""), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13("40063813339"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13("40063813339311"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13("40063813339A"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13("400638133 93"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan13("４００６３８１３３３９３"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan8("551234"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan8("551234577"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeEan8("55123-5"), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsUnsupportedFormatAndChecksumMode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => EanUpcEncoder.Encode("400638133393", BarcodeFormat.Code128, BarcodeChecksumMode.Calculate),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.Encode("400638133393", BarcodeFormat.Ean13, (BarcodeChecksumMode)99),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncodedResultsDoNotAliasCanonicalTablesOrLaterResults()
    {
        EanUpcEncodedData first = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);
        first.Modules[0] = 0;
        first.Digits[0] = 9;
        first.LeftParity[0] = EanUpcDigitSet.G;

        EanUpcEncodedData second = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(second.Modules[0], Is.EqualTo(1));
            Assert.That(second.Digits[0], Is.EqualTo(4));
            Assert.That(ToParityString(second.LeftParity), Is.EqualTo("LGLLGG"));
        });
    }

    static void EncodeEan13(string digits)
    {
        EanUpcEncoder.Encode(digits, BarcodeFormat.Ean13, BarcodeChecksumMode.None);
    }

    static void EncodeEan8(string digits)
    {
        EanUpcEncoder.Encode(digits, BarcodeFormat.Ean8, BarcodeChecksumMode.None);
    }

    static string ToModuleString(byte[] modules)
    {
        char[] characters = new char[modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            characters[index] = modules[index] == 0 ? '0' : '1';
            index = index + 1;
        }

        return new string(characters);
    }

    static string ToParityString(EanUpcDigitSet[] parity)
    {
        char[] characters = new char[parity.Length];
        int index = 0;
        while (index < parity.Length)
        {
            characters[index] = parity[index] == EanUpcDigitSet.L ? 'L' : 'G';
            index = index + 1;
        }

        return new string(characters);
    }
}
