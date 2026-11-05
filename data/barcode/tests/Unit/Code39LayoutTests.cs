namespace barcode.Tests;

public class Code39LayoutTests
{
    [Test]
    public void DefaultLayoutCreatesFramedCode39SymbolWithStandardQuietZones()
    {
        BarcodePayload payload = CreatePayload("A", new byte[] { 65 });

        Code39LayoutResult result = Code39Layout.CreateLayout("A", payload, null, null);
        payload.RawBytes![0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("A"));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Code39));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(38));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(38));
            Assert.That(result.Symbol.Modules![0], Is.EqualTo(1));
            Assert.That(result.Symbol.Modules[^1], Is.EqualTo(1));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(58));
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Payload!.RawBytes, Is.EqualTo(new byte[] { 65 }));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]A0"));
            Assert.That(result.Symbol.HasChecksum, Is.False);
            Assert.That(result.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(result.IntercharacterGapModules, Is.EqualTo(1));
            Assert.That(result.ElementWidths, Has.Length.EqualTo(29));
            Assert.That(result.ElementIsDark[0], Is.True);
            Assert.That(result.ElementIsDark[9], Is.False);
        });
    }

    [Test]
    public void CalculateAppendsModulo43AndTransmissionPolicyControlsHrt()
    {
        Code39Options hiddenCheck = new Code39Options();
        hiddenCheck.ChecksumMode = BarcodeChecksumMode.Calculate;
        Code39Options shownCheck = new Code39Options();
        shownCheck.ChecksumMode = BarcodeChecksumMode.Calculate;
        shownCheck.TransmitCheckCharacter = true;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Standard;

        Code39LayoutResult hidden = Create("CODE39", hiddenCheck, common);
        Code39LayoutResult shown = Create("CODE39", shownCheck, common);

        Assert.Multiple(() =>
        {
            Assert.That(hidden.Encoded.EncodedText, Is.EqualTo("CODE39W"));
            Assert.That(hidden.Encoded.PatternBits, Has.Length.EqualTo(9));
            Assert.That(hidden.Symbol.HasChecksum, Is.True);
            Assert.That(hidden.Symbol.ChecksumValue, Is.EqualTo(32));
            Assert.That(hidden.CheckCharacter, Is.EqualTo('W'));
            Assert.That(hidden.TransmittedText, Is.EqualTo("CODE39"));
            Assert.That(hidden.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("CODE39"));
            Assert.That(shown.TransmittedText, Is.EqualTo("CODE39W"));
            Assert.That(shown.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("CODE39W"));
        });
    }

    [Test]
    public void SuppliedCheckModesVerifyAndKeepOrStripTheCheckCharacter()
    {
        Code39Options required = new Code39Options();
        required.ChecksumMode = BarcodeChecksumMode.Require;
        Code39Options requiredAndTransmitted = new Code39Options();
        requiredAndTransmitted.ChecksumMode = BarcodeChecksumMode.Require;
        requiredAndTransmitted.TransmitCheckCharacter = true;
        Code39Options kept = new Code39Options();
        kept.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        Code39Options stripped = new Code39Options();
        stripped.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        stripped.TransmitCheckCharacter = true;

        Code39LayoutResult defaultRequired = Create("CODE39W", required, null);
        Code39LayoutResult transmittedRequired = Create("CODE39W", requiredAndTransmitted, null);
        Code39LayoutResult verifyKeep = Create("CODE39W", kept, null);
        Code39LayoutResult verifyStrip = Create("CODE39W", stripped, null);

        Assert.Multiple(() =>
        {
            Assert.That(defaultRequired.DataText, Is.EqualTo("CODE39"));
            Assert.That(defaultRequired.TransmittedText, Is.EqualTo("CODE39"));
            Assert.That(transmittedRequired.TransmittedText, Is.EqualTo("CODE39W"));
            Assert.That(verifyKeep.TransmittedText, Is.EqualTo("CODE39W"));
            Assert.That(verifyStrip.TransmittedText, Is.EqualTo("CODE39"));
            Assert.That(verifyStrip.Encoded.EncodedText, Is.EqualTo("CODE39W"));
            Assert.That(verifyStrip.Symbol.ChecksumValue, Is.EqualTo(32));
        });
    }

    [Test]
    public void FullAsciiChecksumUsesExpandedCarrierCharacters()
    {
        Code39Options options = new Code39Options();
        options.CharacterMode = Code39CharacterMode.FullAscii;
        options.ChecksumMode = BarcodeChecksumMode.Calculate;
        options.TransmitCheckCharacter = true;

        Code39LayoutResult result = Create("a", options, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.DataText, Is.EqualTo("a"));
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("+A8"));
            Assert.That(result.CheckCharacter, Is.EqualTo('8'));
            Assert.That(result.TransmittedText, Is.EqualTo("a8"));
            Assert.That(result.Symbol.ChecksumValue, Is.EqualTo(8));
        });
    }

    [Test]
    public void InvalidSuppliedChecksAreRejected()
    {
        Code39Options options = new Code39Options();
        options.ChecksumMode = BarcodeChecksumMode.VerifyStrip;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create("CODE39X", options, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", options, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("CODE39*", options, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void AppendMetadataMarksContinuationAndReleaseWithoutJoiningSegments()
    {
        Code39Options options = new Code39Options();
        options.EnableAppend = true;

        Code39LayoutResult continuation = Create(" PART", options, null);
        Code39LayoutResult release = Create("FINAL", options, null);
        Code39LayoutResult disabled = Create(" PART", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(continuation.AppendMetadata, Is.Not.Null);
            Assert.That(continuation.AppendMetadata!.IsContinuation, Is.True);
            Assert.That(continuation.AppendMetadata.ReleasesBufferedData, Is.False);
            Assert.That(continuation.AppendMetadata.SegmentText, Is.EqualTo("PART"));
            Assert.That(continuation.DataText, Is.EqualTo(" PART"));
            Assert.That(continuation.Encoded.EncodedText, Is.EqualTo(" PART"));
            Assert.That(release.AppendMetadata!.IsContinuation, Is.False);
            Assert.That(release.AppendMetadata.ReleasesBufferedData, Is.True);
            Assert.That(release.AppendMetadata.SegmentText, Is.EqualTo("FINAL"));
            Assert.That(disabled.AppendMetadata, Is.Null);
        });
    }

    [Test]
    public void FractionalRatioAndCustomGapPreserveExactElementGeometry()
    {
        Code39Options fractionalOptions = new Code39Options();
        fractionalOptions.WideToNarrowRatio = 2.5;
        fractionalOptions.IntercharacterGapModules = 2;
        Code39Options integralOptions = new Code39Options();
        integralOptions.WideToNarrowRatio = 3.0;
        integralOptions.IntercharacterGapModules = 2;

        Code39LayoutResult fractional = Create("A", fractionalOptions, null);
        Code39LayoutResult integral = Create("A", integralOptions, null);

        int wideCount = 0;
        int index = 0;
        while (index < fractional.ElementWidths.Length)
        {
            if (fractional.ElementWidths[index] == 2.5)
            {
                wideCount = wideCount + 1;
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(fractional.SymbolWidthModules, Is.EqualTo(44.5));
            Assert.That(fractional.Symbol.WidthModules, Is.EqualTo(45));
            Assert.That(fractional.Symbol.Modules, Is.Null);
            Assert.That(fractional.Symbol.Size!.WidthModules, Is.EqualTo(65));
            Assert.That(wideCount, Is.EqualTo(9));
            Assert.That(fractional.ElementWidths[9], Is.EqualTo(2.0));
            Assert.That(integral.SymbolWidthModules, Is.EqualTo(49.0));
            Assert.That(integral.Symbol.Modules, Has.Length.EqualTo(49));
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

        Code39LayoutResult result = Create("A", null, common);
        quietZone.LeftModules = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(2));
            Assert.That(result.Symbol.Size.QuietZone.RightModules, Is.EqualTo(3));
            Assert.That(result.Symbol.Size.WidthModules, Is.EqualTo(43));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(4));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(21.5));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(11.5));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("A"));
            Assert.That(result.Symbol.TextLayout.Runs[0].X, Is.EqualTo(2.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].BaselineY, Is.EqualTo(5.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].Width, Is.EqualTo(38.0));
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

        Code39LayoutResult rawResult = Code39Layout.CreateLayout("A", payload, null, raw);
        Code39LayoutResult customResult = Code39Layout.CreateLayout("A", payload, null, custom);
        Code39LayoutResult hiddenResult = Code39Layout.CreateLayout("A", payload, null, hidden);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("\0A\u00ff"));
            Assert.That(customResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("label"));
            Assert.That(hiddenResult.Symbol.TextLayout, Is.Null);
        });
    }

    [Test]
    public void GenericChecksumPolicyIsUsedWhenCode39OptionsAreAbsent()
    {
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.ChecksumMode = BarcodeChecksumMode.Calculate;

        Code39LayoutResult result = Create("CODE39", null, common);

        Assert.That(result.Encoded.EncodedText, Is.EqualTo("CODE39W"));
    }

    [Test]
    public void LayoutRejectsInvalidRatioGapPoliciesAndSizing()
    {
        Code39Options lowRatio = new Code39Options();
        lowRatio.WideToNarrowRatio = 1.99;
        Code39Options highRatio = new Code39Options();
        highRatio.WideToNarrowRatio = 3.01;
        Code39Options notFinite = new Code39Options();
        notFinite.WideToNarrowRatio = double.NaN;
        Code39Options negativeGap = new Code39Options();
        negativeGap.IntercharacterGapModules = -1;
        Code39Options invalidChecksum = new Code39Options();
        invalidChecksum.ChecksumMode = (BarcodeChecksumMode)99;
        BarcodeEncodeOptions missingCustom = new BarcodeEncodeOptions();
        missingCustom.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        BarcodeEncodeOptions tooSmall = new BarcodeEncodeOptions();
        tooSmall.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        tooSmall.QuietZone = CreateQuietZone(9, 0, 10, 0);
        BarcodeSizing wrongWidth = new BarcodeSizing();
        wrongWidth.RequestedWidthModules = 39;
        BarcodeEncodeOptions wrongSizing = new BarcodeEncodeOptions();
        wrongSizing.Sizing = wrongWidth;
        BarcodeEncodeOptions missingText = new BarcodeEncodeOptions();
        missingText.TextMode = BarcodeTextMode.Custom;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create("A", lowRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", highRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", notFinite, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", negativeGap, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", invalidChecksum, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", null, missingCustom), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", null, tooSmall), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", null, wrongSizing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("A", null, missingText), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code39Layout.CreateLayout("A", null!, null, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static Code39LayoutResult Create(
        string text,
        Code39Options? code39Options,
        BarcodeEncodeOptions? encodeOptions)
    {
        return Code39Layout.CreateLayout(text, CreatePayload(text, null), code39Options, encodeOptions);
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
