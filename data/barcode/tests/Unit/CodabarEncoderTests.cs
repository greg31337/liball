namespace barcode.Tests;

public class CodabarEncoderTests
{
    [Test]
    public void UnframedDataUsesDefaultTerminalsAndCreatesCanonicalPatterns()
    {
        CodabarOptions options = new CodabarOptions();

        CodabarEncodedData encoded = CodabarEncoder.Encode(
            "1234",
            options,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.InputText, Is.EqualTo("1234"));
            Assert.That(encoded.DataText, Is.EqualTo("1234"));
            Assert.That(encoded.CarrierDataText, Is.EqualTo("1234"));
            Assert.That(encoded.EncodedText, Is.EqualTo("A1234A"));
            Assert.That(encoded.TransmittedText, Is.EqualTo("1234"));
            Assert.That(encoded.StartCharacter, Is.EqualTo('A'));
            Assert.That(encoded.StopCharacter, Is.EqualTo('A'));
            Assert.That(encoded.DisplayStartCharacter, Is.EqualTo('A'));
            Assert.That(encoded.DisplayStopCharacter, Is.EqualTo('A'));
            Assert.That(encoded.WasFramedInput, Is.False);
            Assert.That(encoded.HasChecksum, Is.False);
            Assert.That(encoded.Values, Is.EqualTo(new int[] { 16, 1, 2, 3, 4, 16 }));
            Assert.That(encoded.PatternBits, Is.EqualTo(new int[]
            {
                0x1A,
                0x06,
                0x09,
                0x60,
                0x12,
                0x1A
            }));
        });
    }

    [Test]
    public void FramedInputPreservesSelectedCanonicalTerminals()
    {
        CodabarOptions options = new CodabarOptions();
        options.TransmitStartStop = true;

        CodabarEncodedData encoded = CodabarEncoder.Encode(
            "B12D",
            options,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataText, Is.EqualTo("12"));
            Assert.That(encoded.EncodedText, Is.EqualTo("B12D"));
            Assert.That(encoded.TransmittedText, Is.EqualTo("B12D"));
            Assert.That(encoded.StartCharacter, Is.EqualTo('B'));
            Assert.That(encoded.StopCharacter, Is.EqualTo('D'));
            Assert.That(encoded.DisplayStartCharacter, Is.EqualTo('B'));
            Assert.That(encoded.DisplayStopCharacter, Is.EqualTo('D'));
            Assert.That(encoded.WasFramedInput, Is.True);
            Assert.That(encoded.Values, Is.EqualTo(new int[] { 17, 1, 2, 19 }));
        });
    }

    [Test]
    public void CallerSelectedTerminalsFrameUnframedData()
    {
        CodabarOptions options = new CodabarOptions();
        options.StartCharacter = "C";
        options.StopCharacter = "D";
        options.TransmitStartStop = true;

        CodabarEncodedData encoded = CodabarEncoder.Encode(
            "12",
            options,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.EncodedText, Is.EqualTo("C12D"));
            Assert.That(encoded.TransmittedText, Is.EqualTo("C12D"));
            Assert.That(encoded.StartCharacter, Is.EqualTo('C'));
            Assert.That(encoded.StopCharacter, Is.EqualTo('D'));
            Assert.That(encoded.WasFramedInput, Is.False);
        });
    }

    [Test]
    public void EnabledAliasesPreserveDisplayTextAndNormalizeCarrierTerminals()
    {
        CodabarOptions selectedAliases = new CodabarOptions();
        selectedAliases.StartCharacter = "T";
        selectedAliases.StopCharacter = "E";
        selectedAliases.AcceptAliases = true;
        selectedAliases.TransmitStartStop = true;
        CodabarOptions framedAliases = new CodabarOptions();
        framedAliases.AcceptAliases = true;
        framedAliases.TransmitStartStop = true;

        CodabarEncodedData selected = CodabarEncoder.Encode(
            "12",
            selectedAliases,
            BarcodeChecksumMode.None);
        CodabarEncodedData framed = CodabarEncoder.Encode(
            "T12E",
            framedAliases,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(selected.EncodedText, Is.EqualTo("A12D"));
            Assert.That(selected.TransmittedText, Is.EqualTo("T12E"));
            Assert.That(selected.DisplayStartCharacter, Is.EqualTo('T'));
            Assert.That(selected.DisplayStopCharacter, Is.EqualTo('E'));
            Assert.That(framed.EncodedText, Is.EqualTo("A12D"));
            Assert.That(framed.TransmittedText, Is.EqualTo("T12E"));
            Assert.That(framed.WasFramedInput, Is.True);
        });
    }

    [Test]
    public void CalculateInsertsModulo16BeforeStopAndHonorsTransmissionFlags()
    {
        CodabarOptions hidden = new CodabarOptions();
        CodabarOptions shown = new CodabarOptions();
        shown.TransmitCheckCharacter = true;
        shown.TransmitStartStop = true;

        CodabarEncodedData hiddenResult = CodabarEncoder.Encode(
            "A1234B",
            hidden,
            BarcodeChecksumMode.Calculate);
        CodabarEncodedData shownResult = CodabarEncoder.Encode(
            "A1234B",
            shown,
            BarcodeChecksumMode.Calculate);

        Assert.Multiple(() =>
        {
            Assert.That(hiddenResult.DataText, Is.EqualTo("1234"));
            Assert.That(hiddenResult.CarrierDataText, Is.EqualTo("12345"));
            Assert.That(hiddenResult.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(hiddenResult.TransmittedText, Is.EqualTo("1234"));
            Assert.That(hiddenResult.CheckCharacter, Is.EqualTo('5'));
            Assert.That(hiddenResult.ChecksumValue, Is.EqualTo(5));
            Assert.That(hiddenResult.HasChecksum, Is.True);
            Assert.That(hiddenResult.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.Calculate));
            Assert.That(shownResult.TransmittedText, Is.EqualTo("A12345B"));
        });
    }

    [Test]
    public void Modulo16CalculationIncludesThePreservedTerminalValues()
    {
        CodabarOptions options = new CodabarOptions();

        CodabarEncodedData encoded = CodabarEncoder.Encode(
            "B1A",
            options,
            BarcodeChecksumMode.Calculate);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataText, Is.EqualTo("1"));
            Assert.That(encoded.EncodedText, Is.EqualTo("B1.A"));
            Assert.That(encoded.CheckCharacter, Is.EqualTo('.'));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(14));
        });
    }

    [Test]
    public void SuppliedCheckModesVerifyAndKeepOrStripTheCheckCharacter()
    {
        CodabarOptions required = new CodabarOptions();
        CodabarOptions requiredAndShown = new CodabarOptions();
        requiredAndShown.TransmitCheckCharacter = true;
        CodabarOptions keep = new CodabarOptions();
        CodabarOptions strip = new CodabarOptions();
        strip.TransmitCheckCharacter = true;
        CodabarOptions keepFraming = new CodabarOptions();
        keepFraming.TransmitStartStop = true;

        CodabarEncodedData defaultRequired = CodabarEncoder.Encode(
            "A12345B",
            required,
            BarcodeChecksumMode.Require);
        CodabarEncodedData shownRequired = CodabarEncoder.Encode(
            "A12345B",
            requiredAndShown,
            BarcodeChecksumMode.Require);
        CodabarEncodedData kept = CodabarEncoder.Encode(
            "A12345B",
            keep,
            BarcodeChecksumMode.VerifyKeep);
        CodabarEncodedData stripped = CodabarEncoder.Encode(
            "A12345B",
            strip,
            BarcodeChecksumMode.VerifyStrip);
        CodabarEncodedData framed = CodabarEncoder.Encode(
            "A12345B",
            keepFraming,
            BarcodeChecksumMode.VerifyKeep);

        Assert.Multiple(() =>
        {
            Assert.That(defaultRequired.DataText, Is.EqualTo("1234"));
            Assert.That(defaultRequired.TransmittedText, Is.EqualTo("1234"));
            Assert.That(shownRequired.TransmittedText, Is.EqualTo("12345"));
            Assert.That(kept.TransmittedText, Is.EqualTo("12345"));
            Assert.That(stripped.TransmittedText, Is.EqualTo("1234"));
            Assert.That(stripped.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(framed.TransmittedText, Is.EqualTo("A12345B"));
        });
    }

    [Test]
    public void InvalidChecksFramingTerminalsAndAliasesAreRejected()
    {
        CodabarOptions options = new CodabarOptions();
        CodabarOptions badStartLength = new CodabarOptions();
        badStartLength.StartCharacter = "AB";
        CodabarOptions badStop = new CodabarOptions();
        badStop.StopCharacter = "X";
        CodabarOptions disabledAlias = new CodabarOptions();
        disabledAlias.StartCharacter = "T";
        CodabarOptions conflict = new CodabarOptions();
        conflict.StartCharacter = "C";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CodabarEncoder.Encode("A12346B", options, BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("A1B", options, BarcodeChecksumMode.Require),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("A12", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("12B", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("AB", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("A1AB", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("T12E", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("12", badStartLength, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("12", badStop, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("12", disabledAlias, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("A12B", conflict, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncoderRejectsMissingInvalidDataOptionsAndChecksumMode()
    {
        CodabarOptions options = new CodabarOptions();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CodabarEncoder.Encode(null!, options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("12_3", options, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("123", null!, BarcodeChecksumMode.None),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarEncoder.Encode("123", options, (BarcodeChecksumMode)99),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ReturnedArraysDoNotShareCanonicalOrLaterEncodingState()
    {
        CodabarOptions options = new CodabarOptions();
        CodabarEncodedData first = CodabarEncoder.Encode("12", options, BarcodeChecksumMode.None);
        first.Values[0] = 0;
        first.PatternBits[0] = 0;

        CodabarEncodedData second = CodabarEncoder.Encode("12", options, BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.Values, Is.EqualTo(new int[] { 16, 1, 2, 16 }));
            Assert.That(second.PatternBits[0], Is.EqualTo(0x1A));
            Assert.That(CodabarPatterns.GetCharacterPatternBits('A'), Is.EqualTo(0x1A));
        });
    }
}
