namespace barcode.Tests;

public class CodabarLayoutTests
{
    [Test]
    public void DefaultLayoutCreatesFramedSymbolWithStandardQuietZones()
    {
        BarcodePayload payload = CreatePayload("1", new byte[] { 49 });

        CodabarLayoutResult result = CodabarLayout.CreateLayout("1", payload, null, null);
        payload.RawBytes![0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("A1A"));
            Assert.That(result.Encoded.TransmittedText, Is.EqualTo("1"));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Codabar));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(31));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(31));
            Assert.That(result.Symbol.Modules![0], Is.EqualTo(1));
            Assert.That(result.Symbol.Modules[^1], Is.EqualTo(1));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(51));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Payload!.RawBytes, Is.EqualTo(new byte[] { 49 }));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]F0"));
            Assert.That(result.Symbol.Codewords, Is.EqualTo(new int[] { 16, 1, 16 }));
            Assert.That(result.Symbol.HasChecksum, Is.False);
            Assert.That(result.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(result.IntercharacterGapModules, Is.EqualTo(1));
            Assert.That(result.SymbolWidthModules, Is.EqualTo(31.0));
            Assert.That(result.ElementWidths, Has.Length.EqualTo(23));
            Assert.That(result.ElementIsDark[0], Is.True);
            Assert.That(result.ElementIsDark[7], Is.False);
            Assert.That(result.ElementIsDark[8], Is.True);
        });
    }

    [Test]
    public void ChecksumAndTerminalTransmissionPoliciesControlStandardHrt()
    {
        CodabarOptions hidden = new CodabarOptions();
        hidden.ChecksumMode = BarcodeChecksumMode.Calculate;
        CodabarOptions shown = new CodabarOptions();
        shown.ChecksumMode = BarcodeChecksumMode.Calculate;
        shown.TransmitCheckCharacter = true;
        shown.TransmitStartStop = true;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Standard;

        CodabarLayoutResult hiddenResult = Create("A1234B", hidden, common);
        CodabarLayoutResult shownResult = Create("A1234B", shown, common);

        Assert.Multiple(() =>
        {
            Assert.That(hiddenResult.Encoded.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(hiddenResult.Encoded.TransmittedText, Is.EqualTo("1234"));
            Assert.That(hiddenResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("1234"));
            Assert.That(hiddenResult.Symbol.HasChecksum, Is.True);
            Assert.That(hiddenResult.Symbol.ChecksumValue, Is.EqualTo(5));
            Assert.That(shownResult.Encoded.TransmittedText, Is.EqualTo("A12345B"));
            Assert.That(shownResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("A12345B"));
        });
    }

    [Test]
    public void VerifiedCheckModesKeepOrStripWithoutChangingCarrier()
    {
        CodabarOptions keep = new CodabarOptions();
        keep.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        CodabarOptions strip = new CodabarOptions();
        strip.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        strip.TransmitCheckCharacter = true;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Standard;

        CodabarLayoutResult kept = Create("A12345B", keep, common);
        CodabarLayoutResult stripped = Create("A12345B", strip, common);

        Assert.Multiple(() =>
        {
            Assert.That(kept.Encoded.DataText, Is.EqualTo("1234"));
            Assert.That(kept.Encoded.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(kept.Encoded.TransmittedText, Is.EqualTo("12345"));
            Assert.That(kept.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("12345"));
            Assert.That(stripped.Encoded.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(stripped.Encoded.TransmittedText, Is.EqualTo("1234"));
            Assert.That(stripped.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("1234"));
        });
    }

    [Test]
    public void AliasTerminalsUseCanonicalPatternsAndPreservedHrt()
    {
        CodabarOptions aliases = new CodabarOptions();
        aliases.StartCharacter = "T";
        aliases.StopCharacter = "E";
        aliases.AcceptAliases = true;
        aliases.TransmitStartStop = true;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Standard;

        CodabarLayoutResult result = Create("12", aliases, common);

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("A12D"));
            Assert.That(result.Encoded.TransmittedText, Is.EqualTo("T12E"));
            Assert.That(result.Encoded.PatternBits[0], Is.EqualTo(CodabarPatterns.GetCharacterPatternBits('A')));
            Assert.That(result.Encoded.PatternBits[^1], Is.EqualTo(CodabarPatterns.GetCharacterPatternBits('D')));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("T12E"));
        });
    }

    [Test]
    public void FractionalRatioAndCustomGapPreserveExactElementGeometry()
    {
        CodabarOptions fractionalOptions = new CodabarOptions();
        fractionalOptions.WideToNarrowRatio = 2.5;
        fractionalOptions.IntercharacterGapModules = 2;
        CodabarOptions integralOptions = new CodabarOptions();
        integralOptions.WideToNarrowRatio = 3.0;
        integralOptions.IntercharacterGapModules = 2;

        CodabarLayoutResult fractional = Create("1", fractionalOptions, null);
        CodabarLayoutResult integral = Create("1", integralOptions, null);
        int wideCount = fractional.ElementWidths.Count(width => width == 2.5);

        Assert.Multiple(() =>
        {
            Assert.That(fractional.SymbolWidthModules, Is.EqualTo(37.0));
            Assert.That(fractional.Symbol.WidthModules, Is.EqualTo(37));
            Assert.That(fractional.Symbol.Modules, Is.Null);
            Assert.That(fractional.Symbol.Size!.WidthModules, Is.EqualTo(57));
            Assert.That(wideCount, Is.EqualTo(8));
            Assert.That(fractional.ElementWidths[7], Is.EqualTo(2.0));
            Assert.That(fractional.ElementWidths[15], Is.EqualTo(2.0));
            Assert.That(integral.SymbolWidthModules, Is.EqualTo(41.0));
            Assert.That(integral.Symbol.Modules, Has.Length.EqualTo(41));
        });
    }

    [Test]
    public void QuietZonesPhysicalSizingAndHrtUseSelectedLayout()
    {
        BarcodeQuietZone quietZone = CreateQuietZone(2, 1, 3, 2);
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = 0.5;
        sizing.BarHeightMillimetres = 10.0;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        common.QuietZone = quietZone;
        common.Sizing = sizing;
        common.TextMode = BarcodeTextMode.Standard;

        CodabarLayoutResult result = Create("1", null, common);
        quietZone.LeftModules = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(2));
            Assert.That(result.Symbol.Size.QuietZone.RightModules, Is.EqualTo(3));
            Assert.That(result.Symbol.Size.WidthModules, Is.EqualTo(36));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(4));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(18.0));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(11.5));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("1"));
            Assert.That(result.Symbol.TextLayout.Runs[0].X, Is.EqualTo(2.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].BaselineY, Is.EqualTo(5.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].Width, Is.EqualTo(31.0));
        });
    }

    [Test]
    public void RawCustomAndHiddenTextModesSelectRequestedContent()
    {
        BarcodePayload payload = CreatePayload("interpreted", new byte[] { 0, 65, 255 });
        BarcodeEncodeOptions raw = new BarcodeEncodeOptions();
        raw.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions custom = new BarcodeEncodeOptions();
        custom.TextMode = BarcodeTextMode.Custom;
        custom.CustomText = "label";
        BarcodeEncodeOptions hidden = new BarcodeEncodeOptions();
        hidden.TextMode = BarcodeTextMode.Hidden;

        CodabarLayoutResult rawResult = CodabarLayout.CreateLayout("1", payload, null, raw);
        CodabarLayoutResult customResult = CodabarLayout.CreateLayout("1", payload, null, custom);
        CodabarLayoutResult hiddenResult = CodabarLayout.CreateLayout("1", payload, null, hidden);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("\0A\u00ff"));
            Assert.That(customResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("label"));
            Assert.That(hiddenResult.Symbol.TextLayout, Is.Null);
        });
    }

    [Test]
    public void GenericChecksumPolicyIsUsedWhenCodabarOptionsAreAbsent()
    {
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.ChecksumMode = BarcodeChecksumMode.Calculate;

        CodabarLayoutResult result = Create("1234", null, common);

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("A12346A"));
            Assert.That(result.Encoded.CheckCharacter, Is.EqualTo('6'));
            Assert.That(result.Symbol.HasChecksum, Is.True);
            Assert.That(result.Symbol.ChecksumValue, Is.EqualTo(6));
        });
    }

    [Test]
    public void LayoutRejectsInvalidRatioGapPoliciesSizingAndText()
    {
        CodabarOptions lowRatio = new CodabarOptions();
        lowRatio.WideToNarrowRatio = 1.99;
        CodabarOptions highRatio = new CodabarOptions();
        highRatio.WideToNarrowRatio = 3.01;
        CodabarOptions notFinite = new CodabarOptions();
        notFinite.WideToNarrowRatio = double.NaN;
        CodabarOptions negativeGap = new CodabarOptions();
        negativeGap.IntercharacterGapModules = -1;
        CodabarOptions invalidChecksum = new CodabarOptions();
        invalidChecksum.ChecksumMode = (BarcodeChecksumMode)99;
        BarcodeEncodeOptions missingCustomQuiet = new BarcodeEncodeOptions();
        missingCustomQuiet.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        BarcodeEncodeOptions tooSmallQuiet = new BarcodeEncodeOptions();
        tooSmallQuiet.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        tooSmallQuiet.QuietZone = CreateQuietZone(9, 0, 10, 0);
        BarcodeSizing wrongWidth = new BarcodeSizing();
        wrongWidth.RequestedWidthModules = 32;
        BarcodeEncodeOptions wrongSizing = new BarcodeEncodeOptions();
        wrongSizing.Sizing = wrongWidth;
        BarcodeEncodeOptions missingText = new BarcodeEncodeOptions();
        missingText.TextMode = BarcodeTextMode.Custom;
        BarcodeEncodeOptions invalidTextMode = new BarcodeEncodeOptions();
        invalidTextMode.TextMode = (BarcodeTextMode)99;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create("1", lowRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", highRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", notFinite, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", negativeGap, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", invalidChecksum, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", null, missingCustomQuiet), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", null, tooSmallQuiet), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", null, wrongSizing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", null, missingText), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("1", null, invalidTextMode), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => CodabarLayout.CreateLayout("1", null!, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static CodabarLayoutResult Create(
        string text,
        CodabarOptions? codabarOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        return CodabarLayout.CreateLayout(
            text,
            CreatePayload(text, null),
            codabarOptions,
            encodeOptions);
    }

    static BarcodePayload CreatePayload(string? text, byte[]? rawBytes)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = rawBytes;
        return payload;
    }

    static BarcodeQuietZone CreateQuietZone(int left, int top, int right, int bottom)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = left;
        quietZone.TopModules = top;
        quietZone.RightModules = right;
        quietZone.BottomModules = bottom;
        return quietZone;
    }
}
