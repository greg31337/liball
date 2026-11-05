namespace barcode.Tests;

public class UpcAEncoderTests
{
    [Test]
    public void CompleteUpcAVectorMatchesIndependentModuleOracle()
    {
        string expectedModules =
            "101" +
            "0001101" +
            "0111101" +
            "0101111" +
            "0001101" +
            "0001101" +
            "0001101" +
            "01010" +
            "1101100" +
            "1110100" +
            "1100110" +
            "1011100" +
            "1001110" +
            "1101100" +
            "101";

        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "036000291452",
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(encoded.Text, Is.EqualTo("036000291452"));
            Assert.That(encoded.Ean13CompatibleText, Is.EqualTo("0036000291452"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 0, 3, 6, 0, 0, 0, 2, 9, 1, 4, 5, 2 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("LLLLLL"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcEncoder.UpcAModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(2));
            Assert.That(encoded.ChecksumCalculated, Is.False);
        });
    }

    [Test]
    public void UpcAAndZeroPrefixedEan13SharePatternButPreserveDistinctIdentity()
    {
        EanUpcEncodedData upcA = EanUpcEncoder.Encode(
            "036000291452",
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.Require);
        EanUpcEncodedData ean13 = EanUpcEncoder.Encode(
            "0036000291452",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(upcA.Modules, Is.EqualTo(ean13.Modules));
            Assert.That(upcA.LeftParity, Is.EqualTo(ean13.LeftParity));
            Assert.That(upcA.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(ean13.Format, Is.EqualTo(BarcodeFormat.Ean13));
            Assert.That(upcA.Text, Is.EqualTo("036000291452"));
            Assert.That(ean13.Text, Is.EqualTo("0036000291452"));
            Assert.That(upcA.Ean13CompatibleText, Is.EqualTo(ean13.Text));
            Assert.That(upcA.Digits, Has.Length.EqualTo(12));
            Assert.That(ean13.Digits, Has.Length.EqualTo(13));
        });
    }

    [Test]
    public void MissingUpcACheckDigitIsCalculatedAndAppended()
    {
        EanUpcEncodedData automatic = EanUpcEncoder.Encode(
            "03600029145",
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.None);
        EanUpcEncodedData calculated = EanUpcEncoder.Encode(
            "03600029145",
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.Calculate);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Text, Is.EqualTo("036000291452"));
            Assert.That(automatic.Ean13CompatibleText, Is.EqualTo("0036000291452"));
            Assert.That(automatic.ChecksumValue, Is.EqualTo(2));
            Assert.That(automatic.ChecksumCalculated, Is.True);
            Assert.That(calculated.Text, Is.EqualTo(automatic.Text));
            Assert.That(calculated.Modules, Is.EqualTo(automatic.Modules));
            Assert.That(calculated.ChecksumCalculated, Is.True);
        });
    }

    [TestCase(BarcodeChecksumMode.None)]
    [TestCase(BarcodeChecksumMode.Calculate)]
    [TestCase(BarcodeChecksumMode.Require)]
    [TestCase(BarcodeChecksumMode.VerifyKeep)]
    [TestCase(BarcodeChecksumMode.VerifyStrip)]
    public void CompleteUpcAIsStrictlyVerifiedInEveryChecksumMode(BarcodeChecksumMode checksumMode)
    {
        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "036000291452",
            BarcodeFormat.UpcA,
            checksumMode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(encoded.ChecksumCalculated, Is.False);
            Assert.That(
                () => EanUpcEncoder.Encode("036000291453", BarcodeFormat.UpcA, checksumMode),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The supplied EAN/UPC check digit is invalid."));
        });
    }

    [Test]
    public void SuppliedCheckModesRejectUpcADataWithoutCheckDigit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Encode("03600029145", BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Encode("03600029145", BarcodeChecksumMode.VerifyKeep),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Encode("03600029145", BarcodeChecksumMode.VerifyStrip),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void UpcARejectsMissingWrongLengthAndNonAsciiInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Encode(null!, BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("0360002914", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("0360002914522", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("0360002914A", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("036000291 5", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("０３６０００２９１４５", BarcodeChecksumMode.None), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void UpcANumberSystemDigitRemainsEncodedDataNotEan13ParitySelector()
    {
        EanUpcEncodedData upcA = EanUpcEncoder.Encode(
            "71234567890",
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.Calculate);
        EanUpcEncodedData compatibleEan13 = EanUpcEncoder.Encode(
            "0" + upcA.Text,
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(upcA.Text[0], Is.EqualTo('7'));
            Assert.That(upcA.Ean13CompatibleText[0], Is.EqualTo('0'));
            Assert.That(ToParityString(upcA.LeftParity), Is.EqualTo("LLLLLL"));
            Assert.That(upcA.Modules, Is.EqualTo(compatibleEan13.Modules));
            Assert.That(upcA.Format, Is.EqualTo(BarcodeFormat.UpcA));
        });
    }

    static EanUpcEncodedData Encode(string digits, BarcodeChecksumMode checksumMode)
    {
        return EanUpcEncoder.Encode(digits, BarcodeFormat.UpcA, checksumMode);
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
