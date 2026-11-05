namespace barcode.Tests;

public class ItfEncoderTests
{
    [Test]
    public void EvenDigitTextEncodesIntoOrderedBarAndSpacePairs()
    {
        ItfEncodedData encoded = ItfEncoder.Encode("1234", BarcodeChecksumMode.None, null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.InputText, Is.EqualTo("1234"));
            Assert.That(encoded.DataText, Is.EqualTo("1234"));
            Assert.That(encoded.EncodedText, Is.EqualTo("1234"));
            Assert.That(encoded.TransmittedText, Is.EqualTo("1234"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            Assert.That(encoded.BarDigitPatternBits, Is.EqualTo(new int[] { 0x11, 0x18 }));
            Assert.That(encoded.SpaceDigitPatternBits, Is.EqualTo(new int[] { 0x09, 0x05 }));
            Assert.That(encoded.HasChecksum, Is.False);
            Assert.That(encoded.ChecksumValue, Is.EqualTo(0));
            Assert.That(encoded.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.None));
        });
    }

    [Test]
    public void CalculateAppendsModulo10AndProducesAnEvenCarrier()
    {
        ItfEncodedData encoded = ItfEncoder.Encode("12345", BarcodeChecksumMode.Calculate, null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataText, Is.EqualTo("12345"));
            Assert.That(encoded.EncodedText, Is.EqualTo("123457"));
            Assert.That(encoded.TransmittedText, Is.EqualTo("123457"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 7 }));
            Assert.That(encoded.BarDigitPatternBits, Has.Length.EqualTo(3));
            Assert.That(encoded.SpaceDigitPatternBits, Has.Length.EqualTo(3));
            Assert.That(encoded.HasChecksum, Is.True);
            Assert.That(encoded.ChecksumValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void SuppliedCheckModesVerifyAndKeepOrStripTransmittedDigit()
    {
        ItfEncodedData required = ItfEncoder.Encode("123457", BarcodeChecksumMode.Require, null);
        ItfEncodedData kept = ItfEncoder.Encode("123457", BarcodeChecksumMode.VerifyKeep, null);
        ItfEncodedData stripped = ItfEncoder.Encode("123457", BarcodeChecksumMode.VerifyStrip, null);

        Assert.Multiple(() =>
        {
            Assert.That(required.DataText, Is.EqualTo("12345"));
            Assert.That(required.EncodedText, Is.EqualTo("123457"));
            Assert.That(required.TransmittedText, Is.EqualTo("123457"));
            Assert.That(kept.TransmittedText, Is.EqualTo("123457"));
            Assert.That(stripped.DataText, Is.EqualTo("12345"));
            Assert.That(stripped.EncodedText, Is.EqualTo("123457"));
            Assert.That(stripped.TransmittedText, Is.EqualTo("12345"));
            Assert.That(stripped.ChecksumValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void AllowedLengthsApplyToTheCompleteEncodedCarrier()
    {
        ItfEncodedData plain = ItfEncoder.Encode(
            "1234",
            BarcodeChecksumMode.None,
            new int[] { 4, 6 });
        ItfEncodedData calculated = ItfEncoder.Encode(
            "12345",
            BarcodeChecksumMode.Calculate,
            new int[] { 6 });

        Assert.Multiple(() =>
        {
            Assert.That(plain.EncodedText, Has.Length.EqualTo(4));
            Assert.That(calculated.EncodedText, Has.Length.EqualTo(6));
            Assert.That(
                () => ItfEncoder.Encode("12", BarcodeChecksumMode.None, new int[] { 4, 6 }),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsMissingNonNumericAndParityInvalidText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => ItfEncoder.Encode(null!, BarcodeChecksumMode.None, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("", BarcodeChecksumMode.None, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("12A4", BarcodeChecksumMode.None, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("１２", BarcodeChecksumMode.None, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("123", BarcodeChecksumMode.None, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("1234", BarcodeChecksumMode.Calculate, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("1", BarcodeChecksumMode.Require, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfEncoder.Encode("123456", BarcodeChecksumMode.Require, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsInvalidChecksumAndLengthPolicies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => ItfEncoder.Encode("1234", (BarcodeChecksumMode)99, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => ItfEncoder.Encode("1234", BarcodeChecksumMode.None, new int[0]),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => ItfEncoder.Encode("1234", BarcodeChecksumMode.None, new int[] { 0, 4 }),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => ItfEncoder.Encode("1234", BarcodeChecksumMode.None, new int[] { 3, 4 }),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }
}
