namespace barcode.Tests;

public class UpcEEncoderTests
{
    [Test]
    public void CompleteUpcEVectorMatchesIndependentModuleOracle()
    {
        string expectedModules =
            "101" +
            "0110011" +
            "0010011" +
            "0100001" +
            "0100011" +
            "0110001" +
            "0111001" +
            "010101";

        EanUpcEncodedData encoded = EanUpcEncoder.EncodeUpcE(
            "01234558",
            0,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.UpcE));
            Assert.That(encoded.Text, Is.EqualTo("01234558"));
            Assert.That(encoded.ExpandedUpcAText, Is.EqualTo("012345000058"));
            Assert.That(encoded.Ean13CompatibleText, Is.EqualTo("0012345000058"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 5, 8 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("GLGLLG"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcEncoder.UpcEModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(8));
            Assert.That(encoded.ChecksumCalculated, Is.False);
        });
    }

    [TestCase("123455")]
    [TestCase("0123455")]
    public void PayloadAndCoreFormsCalculateTheSameCompleteSymbol(string digits)
    {
        EanUpcEncodedData automatic = EanUpcEncoder.EncodeUpcE(
            digits,
            0,
            BarcodeChecksumMode.None);
        EanUpcEncodedData calculated = EanUpcEncoder.EncodeUpcE(
            digits,
            0,
            BarcodeChecksumMode.Calculate);
        EanUpcEncodedData complete = EanUpcEncoder.EncodeUpcE(
            "01234558",
            0,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Text, Is.EqualTo("01234558"));
            Assert.That(automatic.ChecksumCalculated, Is.True);
            Assert.That(automatic.Modules, Is.EqualTo(complete.Modules));
            Assert.That(calculated.Text, Is.EqualTo(automatic.Text));
            Assert.That(calculated.ChecksumCalculated, Is.True);
            Assert.That(calculated.Modules, Is.EqualTo(automatic.Modules));
        });
    }

    [TestCase(BarcodeChecksumMode.None)]
    [TestCase(BarcodeChecksumMode.Calculate)]
    [TestCase(BarcodeChecksumMode.Require)]
    [TestCase(BarcodeChecksumMode.VerifyKeep)]
    [TestCase(BarcodeChecksumMode.VerifyStrip)]
    public void CompleteInputIsStrictlyVerifiedInEveryChecksumMode(BarcodeChecksumMode checksumMode)
    {
        EanUpcEncodedData encoded = EanUpcEncoder.EncodeUpcE("01234558", 0, checksumMode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Text, Is.EqualTo("01234558"));
            Assert.That(encoded.ChecksumCalculated, Is.False);
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("01234559", 0, checksumMode),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The supplied UPC-E check digit is invalid."));
        });
    }

    [Test]
    public void NumberSystemOneUsesItsOwnCheckDigitParityFamily()
    {
        EanUpcEncodedData encoded = EanUpcEncoder.EncodeUpcE(
            "11234555",
            1,
            BarcodeChecksumMode.Require);
        EanUpcEncodedData expanded = EanUpcEncoder.Encode(
            encoded.ExpandedUpcAText,
            BarcodeFormat.UpcA,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.UpcE));
            Assert.That(encoded.Text, Is.EqualTo("11234555"));
            Assert.That(encoded.ExpandedUpcAText, Is.EqualTo("112345000055"));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("LGGLLG"));
            Assert.That(expanded.Format, Is.EqualTo(BarcodeFormat.UpcA));
            Assert.That(UpcECompressor.CompressCompleteUpcA(expanded.Text), Is.EqualTo(encoded.Text));
        });
    }

    [Test]
    public void RequestedNumberSystemMustMatchEncodedNumberSystem()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("0123455", 1, BarcodeChecksumMode.Calculate),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("11234555", 0, BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("123455", -1, BarcodeChecksumMode.Calculate),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("123455", 2, BarcodeChecksumMode.Calculate),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void SuppliedCheckModesRejectPayloadAndCoreForms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("123455", 0, BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("0123455", 0, BarcodeChecksumMode.VerifyKeep),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("123455", 0, BarcodeChecksumMode.VerifyStrip),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsMissingWrongLengthNonAsciiAndInvalidMode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Encode(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode(""), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("12345"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("012345588"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("12A455"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("123 55"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Encode("１２３４５５"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => EanUpcEncoder.EncodeUpcE("123455", 0, (BarcodeChecksumMode)99),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncodedResultsDoNotAliasParityOrModuleTables()
    {
        EanUpcEncodedData first = EanUpcEncoder.EncodeUpcE(
            "01234558",
            0,
            BarcodeChecksumMode.Require);
        first.Modules[0] = 0;
        first.Digits[0] = 1;
        first.LeftParity[0] = EanUpcDigitSet.L;

        EanUpcEncodedData second = EanUpcEncoder.EncodeUpcE(
            "01234558",
            0,
            BarcodeChecksumMode.Require);

        Assert.Multiple(() =>
        {
            Assert.That(second.Modules[0], Is.EqualTo(1));
            Assert.That(second.Digits[0], Is.EqualTo(0));
            Assert.That(ToParityString(second.LeftParity), Is.EqualTo("GLGLLG"));
        });
    }

    static EanUpcEncodedData Encode(string digits)
    {
        return EanUpcEncoder.EncodeUpcE(digits, 0, BarcodeChecksumMode.None);
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
