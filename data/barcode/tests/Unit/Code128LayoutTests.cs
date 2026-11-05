namespace barcode.Tests;

public class Code128LayoutTests
{
    [Test]
    public void DefaultLayoutCreatesIndependentLogicalCode128Symbol()
    {
        Code128EncodedData encoded = EncodeAbc();
        BarcodePayload payload = CreatePayload("ABC", new byte[] { 65, 66, 67 });

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(encoded, payload, null);
        encoded.Modules[0] = 0;
        encoded.Codewords[0] = 0;
        payload.RawBytes![0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(symbol.WidthModules, Is.EqualTo(68));
            Assert.That(symbol.HeightModules, Is.EqualTo(1));
            Assert.That(symbol.Modules, Has.Length.EqualTo(68));
            Assert.That(symbol.Modules![0], Is.EqualTo(1));
            Assert.That(symbol.Modules, Is.Not.SameAs(encoded.Modules));
            Assert.That(symbol.Payload, Is.Not.SameAs(payload));
            Assert.That(symbol.Payload!.RawBytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(symbol.Size!.WidthModules, Is.EqualTo(88));
            Assert.That(symbol.Size.HeightModules, Is.EqualTo(1));
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.Size.QuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.Size.QuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.TextLayout, Is.Null);
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo("]C0"));
            Assert.That(symbol.Codewords, Is.EqualTo(new int[] { 104, 33, 34, 35, 1, 106 }));
            Assert.That(symbol.Codewords, Is.Not.SameAs(encoded.Codewords));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.ChecksumValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void StandardTextCreatesPositionedHumanReadablePrimitive()
    {
        BarcodePayload payload = CreatePayload("ABC", new byte[] { 88, 89, 90 });
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(EncodeAbc(), payload, options);
        BarcodeTextLayout layout = symbol.TextLayout!;
        BarcodeTextRun run = layout.Runs![0];

        Assert.Multiple(() =>
        {
            Assert.That(layout.Runs, Has.Length.EqualTo(1));
            Assert.That(run.Text, Is.EqualTo("ABC"));
            Assert.That(run.X, Is.EqualTo(10.0));
            Assert.That(run.BaselineY, Is.EqualTo(2.0));
            Assert.That(run.Width, Is.EqualTo(68.0));
            Assert.That(run.Height, Is.EqualTo(1.0));
            Assert.That(layout.ReservedHeight, Is.EqualTo(1.0));
            Assert.That(layout.Bounds!.X, Is.EqualTo(10.0));
            Assert.That(layout.Bounds.Y, Is.EqualTo(1.0));
            Assert.That(layout.Bounds.Width, Is.EqualTo(68.0));
            Assert.That(layout.Bounds.Height, Is.EqualTo(1.0));
            Assert.That(layout.Size!.WidthModules, Is.EqualTo(88));
            Assert.That(layout.Size.HeightModules, Is.EqualTo(2));
        });
    }

    [Test]
    public void RawAndCustomTextModesSelectTheRequestedContent()
    {
        BarcodePayload payload = CreatePayload("interpreted", new byte[] { 0, 65, 255 });
        BarcodeEncodeOptions rawOptions = new BarcodeEncodeOptions();
        rawOptions.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions customOptions = new BarcodeEncodeOptions();
        customOptions.TextMode = BarcodeTextMode.Custom;
        customOptions.CustomText = "label";

        BarcodeSymbol raw = Code128Layout.CreateSymbol(EncodeAbc(), payload, rawOptions);
        BarcodeSymbol custom = Code128Layout.CreateSymbol(EncodeAbc(), payload, customOptions);

        Assert.Multiple(() =>
        {
            Assert.That(raw.TextLayout!.Runs![0].Text, Is.EqualTo("\0A\u00ff"));
            Assert.That(custom.TextLayout!.Runs![0].Text, Is.EqualTo("label"));
        });
    }

    [Test]
    public void CustomQuietZonePoliciesPreserveStandardRequirement()
    {
        BarcodeQuietZone custom = CreateQuietZone(12, 2, 14, 3);
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = 0.5;
        sizing.BarHeightMillimetres = 12.0;
        sizing.RequestedWidthMillimetres = 47.0;
        sizing.RequestedHeightMillimetres = 14.5;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        options.QuietZone = custom;
        options.Sizing = sizing;
        options.TextMode = BarcodeTextMode.Standard;

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(
            EncodeAbc(),
            CreatePayload("ABC", new byte[] { 65, 66, 67 }),
            options);
        custom.LeftModules = 99;

        Assert.Multiple(() =>
        {
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(12));
            Assert.That(symbol.Size.QuietZone.TopModules, Is.EqualTo(2));
            Assert.That(symbol.Size.QuietZone.RightModules, Is.EqualTo(14));
            Assert.That(symbol.Size.QuietZone.BottomModules, Is.EqualTo(3));
            Assert.That(symbol.Size.WidthModules, Is.EqualTo(94));
            Assert.That(symbol.Size.HeightModules, Is.EqualTo(6));
            Assert.That(symbol.Size.ModuleSizeMillimetres, Is.EqualTo(0.5));
            Assert.That(symbol.Size.WidthMillimetres, Is.EqualTo(47.0));
            Assert.That(symbol.Size.HeightMillimetres, Is.EqualTo(14.5));
            Assert.That(symbol.TextLayout!.Runs![0].X, Is.EqualTo(12.0));
            Assert.That(symbol.TextLayout.Runs[0].BaselineY, Is.EqualTo(7.0));
        });
    }

    [Test]
    public void ExactCustomQuietZoneAllowsDiagnosticUnderSizing()
    {
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = CreateQuietZone(1, 0, 2, 0);

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(EncodeAbc(), CreatePayload("ABC", null), options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Size!.WidthModules, Is.EqualTo(71));
            Assert.That(symbol.Size.QuietZone!.LeftModules, Is.EqualTo(1));
            Assert.That(symbol.Size.QuietZone.RightModules, Is.EqualTo(2));
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
        });
    }

    [Test]
    public void CustomQuietZoneCanComeFromSizingOptions()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.QuietZone = CreateQuietZone(11, 1, 12, 2);
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        options.Sizing = sizing;

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(
            EncodeAbc(),
            CreatePayload("ABC", null),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(11));
            Assert.That(symbol.Size.QuietZone.TopModules, Is.EqualTo(1));
            Assert.That(symbol.Size.QuietZone.RightModules, Is.EqualTo(12));
            Assert.That(symbol.Size.QuietZone.BottomModules, Is.EqualTo(2));
        });
    }

    [Test]
    public void TextModesFallBackBetweenTextAndRawLatin1Bytes()
    {
        BarcodeEncodeOptions standard = new BarcodeEncodeOptions();
        standard.TextMode = BarcodeTextMode.Standard;
        BarcodeEncodeOptions raw = new BarcodeEncodeOptions();
        raw.TextMode = BarcodeTextMode.RawPayload;

        BarcodeSymbol standardBytes = Code128Layout.CreateSymbol(
            EncodeAbc(),
            CreatePayload(null, new byte[] { 65, 200 }),
            standard);
        BarcodeSymbol rawText = Code128Layout.CreateSymbol(
            EncodeAbc(),
            CreatePayload("fallback", null),
            raw);
        BarcodeSymbol missing = Code128Layout.CreateSymbol(
            EncodeAbc(),
            CreatePayload(null, null),
            standard);

        Assert.Multiple(() =>
        {
            Assert.That(standardBytes.TextLayout!.Runs![0].Text, Is.EqualTo("A\u00c8"));
            Assert.That(rawText.TextLayout!.Runs![0].Text, Is.EqualTo("fallback"));
            Assert.That(missing.TextLayout, Is.Null);
        });
    }

    [Test]
    public void PhysicalSizingDerivesModuleAndBarDimensions()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthModules = 68;
        sizing.RequestedHeightModules = 1;
        sizing.RequestedWidthMillimetres = 44.0;
        sizing.RequestedHeightMillimetres = 12.0;
        sizing.DotsPerInch = 300.0;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;

        BarcodeSymbol symbol = Code128Layout.CreateSymbol(EncodeAbc(), CreatePayload("ABC", null), options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.5));
            Assert.That(symbol.Size.WidthMillimetres, Is.EqualTo(44.0));
            Assert.That(symbol.Size.HeightMillimetres, Is.EqualTo(12.0));
            Assert.That(symbol.Size.DotsPerInch, Is.EqualTo(300.0));
        });
    }

    [Test]
    public void LayoutRejectsInvalidQuietZonesTextAndSizing()
    {
        BarcodeEncodeOptions missingCustom = new BarcodeEncodeOptions();
        missingCustom.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        BarcodeEncodeOptions tooSmall = new BarcodeEncodeOptions();
        tooSmall.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        tooSmall.QuietZone = CreateQuietZone(9, 0, 10, 0);
        BarcodeEncodeOptions negative = new BarcodeEncodeOptions();
        negative.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        negative.QuietZone = CreateQuietZone(-1, 0, 0, 0);
        BarcodeEncodeOptions invalidPolicy = new BarcodeEncodeOptions();
        invalidPolicy.QuietZonePolicy = (BarcodeQuietZonePolicy)99;
        BarcodeEncodeOptions missingText = new BarcodeEncodeOptions();
        missingText.TextMode = BarcodeTextMode.Custom;
        BarcodeEncodeOptions invalidText = new BarcodeEncodeOptions();
        invalidText.TextMode = (BarcodeTextMode)99;

        Assert.Multiple(() =>
        {
            Assert.That(() => CreateWithOptions(missingCustom), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(tooSmall), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(negative), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(invalidPolicy), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(missingText), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(invalidText), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void LayoutRejectsConflictingAndNonFiniteMeasurements()
    {
        BarcodeSizing wrongWidth = new BarcodeSizing();
        wrongWidth.RequestedWidthModules = 69;
        BarcodeSizing wrongHeight = new BarcodeSizing();
        wrongHeight.RequestedHeightModules = 2;
        BarcodeSizing negativePixels = new BarcodeSizing();
        negativePixels.RequestedWidthPixels = -1;
        BarcodeSizing notFinite = new BarcodeSizing();
        notFinite.ModuleSizeMillimetres = double.NaN;
        BarcodeSizing infinite = new BarcodeSizing();
        infinite.BarHeightMillimetres = double.PositiveInfinity;
        BarcodeSizing overflowing = new BarcodeSizing();
        overflowing.ModuleSizeMillimetres = double.MaxValue;
        BarcodeSizing conflictingWidth = new BarcodeSizing();
        conflictingWidth.ModuleSizeMillimetres = 1.0;
        conflictingWidth.RequestedWidthMillimetres = 44.0;
        BarcodeSizing conflictingHeight = new BarcodeSizing();
        conflictingHeight.BarHeightMillimetres = 10.0;
        conflictingHeight.RequestedHeightMillimetres = 11.0;
        BarcodeSizing unresolvedHeight = new BarcodeSizing();
        unresolvedHeight.RequestedHeightMillimetres = 10.0;
        BarcodeEncodeOptions unresolvedVerticalQuietZone = new BarcodeEncodeOptions();
        unresolvedVerticalQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        unresolvedVerticalQuietZone.QuietZone = CreateQuietZone(10, 1, 10, 1);
        unresolvedVerticalQuietZone.Sizing = unresolvedHeight;
        BarcodeSizing noBarHeight = new BarcodeSizing();
        noBarHeight.ModuleSizeMillimetres = 1.0;
        noBarHeight.RequestedHeightMillimetres = 2.0;
        BarcodeEncodeOptions noPositiveBars = new BarcodeEncodeOptions();
        noPositiveBars.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        noPositiveBars.QuietZone = CreateQuietZone(0, 1, 0, 1);
        noPositiveBars.Sizing = noBarHeight;
        BarcodeSizing overflowingHeight = new BarcodeSizing();
        overflowingHeight.ModuleSizeMillimetres = 1.0e306;
        overflowingHeight.BarHeightMillimetres = 1.0e308;
        BarcodeEncodeOptions overflowingPhysicalHeight = new BarcodeEncodeOptions();
        overflowingPhysicalHeight.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        overflowingPhysicalHeight.QuietZone = CreateQuietZone(0, 50, 0, 50);
        overflowingPhysicalHeight.Sizing = overflowingHeight;

        Assert.Multiple(() =>
        {
            Assert.That(() => CreateWithSizing(wrongWidth), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(wrongHeight), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(negativePixels), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(notFinite), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(infinite), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(overflowing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(conflictingWidth), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(conflictingHeight), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(unresolvedVerticalQuietZone), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(noPositiveBars), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithOptions(overflowingPhysicalHeight), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void LayoutRejectsMalformedEncodedDataAndMissingPayload()
    {
        Code128EncodedData missingCodewords = EncodeAbc();
        missingCodewords.Codewords = null!;
        Code128EncodedData shortCodewords = EncodeAbc();
        shortCodewords.Codewords = new int[] { 104, 1, 106 };
        Code128EncodedData missingModules = EncodeAbc();
        missingModules.Modules = null!;
        Code128EncodedData wrongStop = EncodeAbc();
        wrongStop.Codewords[^1] = 0;
        Code128EncodedData wrongCheck = EncodeAbc();
        wrongCheck.ChecksumValue = 2;
        Code128EncodedData wrongCheckCodeword = EncodeAbc();
        wrongCheckCodeword.Codewords[^2] = 2;
        Code128EncodedData wrongStart = EncodeAbc();
        wrongStart.StartCodeSet = Code128CodeSet.A;
        Code128EncodedData wrongLength = EncodeAbc();
        wrongLength.Modules = new byte[1];

        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Layout.CreateSymbol(null!, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(EncodeAbc(), null!, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Layout.CreateSymbol(missingCodewords, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(shortCodewords, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(missingModules, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(wrongStop, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(wrongCheck, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(wrongCheckCodeword, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(wrongStart, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Layout.CreateSymbol(wrongLength, CreatePayload("ABC", null), null), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static Code128EncodedData EncodeAbc()
    {
        return Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
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

    static BarcodeSymbol CreateWithOptions(BarcodeEncodeOptions options)
    {
        return Code128Layout.CreateSymbol(EncodeAbc(), CreatePayload("ABC", null), options);
    }

    static BarcodeSymbol CreateWithSizing(BarcodeSizing sizing)
    {
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;
        return CreateWithOptions(options);
    }
}
