namespace barcode.Tests;

public class Itf14ProfileTests
{
    const string DataDigits = "0950110153001";
    const string CompleteDigits = "09501101530010";

    [Test]
    public void DefaultProfileCalculatesCheckAndCreatesNominalGs1Layout()
    {
        BarcodePayload payload = CreatePayload(DataDigits, new byte[] { 48, 57, 53 });

        Itf14LayoutResult result = Itf14Profile.CreateLayout(DataDigits, payload, null, null);
        payload.RawBytes![0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.DataText, Is.EqualTo(DataDigits));
            Assert.That(result.Encoded.EncodedText, Is.EqualTo(CompleteDigits));
            Assert.That(result.Encoded.ChecksumValue, Is.EqualTo(0));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Itf14));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(121));
            Assert.That(result.Symbol.Modules, Is.Null);
            Assert.That(result.Symbol.Codewords, Is.EqualTo(CompleteDigits.Select(character => character - '0')));
            Assert.That(result.Symbol.HasChecksum, Is.True);
            Assert.That(result.Symbol.ChecksumValue, Is.EqualTo(0));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(result.Symbol.Payload!.Text, Is.EqualTo(CompleteDigits));
            Assert.That(result.Symbol.Payload.RawBytes, Is.EqualTo(CompleteDigits.Select(character => (byte)character)));
            Assert.That(result.Symbol.Payload.CharacterEncoding, Is.EqualTo("ISO/IEC 646"));
            Assert.That(result.Symbol.Payload.IsGs1, Is.True);
            Assert.That(result.WideToNarrowRatio, Is.EqualTo(2.5));
            Assert.That(result.CarrierWidthModules, Is.EqualTo(120.5));
            Assert.That(result.ElementWidths, Has.Length.EqualTo(77));
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(1.016));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(142.748).Within(0.000001));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(41.41).Within(0.000001));
            Assert.That(result.BarHeightModules, Is.EqualTo(31.25).Within(0.000001));
            Assert.That(result.BearerBars, Has.Length.EqualTo(2));
            Assert.That(result.UsesBearerBox, Is.False);
        });
    }

    [Test]
    public void CompleteGtinIsVerifiedAndWrongCheckIsRejected()
    {
        Itf14LayoutResult result = Create(CompleteDigits, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.InputText, Is.EqualTo(CompleteDigits));
            Assert.That(result.Encoded.DataText, Is.EqualTo(DataDigits));
            Assert.That(result.Encoded.EncodedText, Is.EqualTo(CompleteDigits));
            Assert.That(() => Create("09501101530011", null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("123456789012", null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("123456789012345", null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("0950110153001A", null, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ExplicitChecksumModesCannotWeakenMandatoryGtinCheck()
    {
        ItfOptions calculate = new ItfOptions();
        calculate.ChecksumMode = BarcodeChecksumMode.Calculate;
        ItfOptions require = new ItfOptions();
        require.ChecksumMode = BarcodeChecksumMode.Require;
        ItfOptions keep = new ItfOptions();
        keep.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        ItfOptions strip = new ItfOptions();
        strip.ChecksumMode = BarcodeChecksumMode.VerifyStrip;

        Assert.Multiple(() =>
        {
            Assert.That(Create(DataDigits, calculate, null).Encoded.EncodedText, Is.EqualTo(CompleteDigits));
            Assert.That(Create(CompleteDigits, require, null).Encoded.EncodedText, Is.EqualTo(CompleteDigits));
            Assert.That(Create(CompleteDigits, keep, null).Encoded.EncodedText, Is.EqualTo(CompleteDigits));
            Assert.That(() => Create(CompleteDigits, calculate, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(DataDigits, require, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, strip, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void TopBottomBearerBarsAreMandatoryAndBoxAddsVerticalSides()
    {
        ItfOptions horizontalOptions = new ItfOptions();
        horizontalOptions.UseBearerBars = true;
        ItfOptions boxOptions = new ItfOptions();
        boxOptions.UseBearerBox = true;

        Itf14LayoutResult horizontal = Create(CompleteDigits, horizontalOptions, null);
        Itf14LayoutResult box = Create(CompleteDigits, boxOptions, null);

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.BearerBars, Has.Length.EqualTo(2));
            Assert.That(horizontal.BearerBars[0].X, Is.EqualTo(0.0));
            Assert.That(horizontal.BearerBars[0].Y, Is.EqualTo(0.0));
            Assert.That(horizontal.BearerBars[0].Width, Is.EqualTo(horizontal.CompleteWidthModules));
            Assert.That(horizontal.BearerBars[0].Height, Is.EqualTo(horizontal.BearerThicknessModules));
            Assert.That(horizontal.BearerBars[1].Y,
                Is.EqualTo(horizontal.CompleteHeightModules - horizontal.BearerThicknessModules));
            Assert.That(horizontal.CarrierXModules, Is.EqualTo(10.0));
            Assert.That(box.BearerBars, Has.Length.EqualTo(4));
            Assert.That(box.UsesBearerBox, Is.True);
            Assert.That(box.CarrierXModules, Is.EqualTo(box.BearerThicknessModules + 10.0));
            Assert.That(box.BearerBars[2].Width, Is.EqualTo(box.BearerThicknessModules));
            Assert.That(box.BearerBars[2].Height, Is.EqualTo(box.CompleteHeightModules));
            Assert.That(box.CompleteWidthModules,
                Is.EqualTo(horizontal.CompleteWidthModules + (2.0 * box.BearerThicknessModules)));
            Assert.That(box.Symbol.Size!.WidthMillimetres, Is.EqualTo(152.408).Within(0.000001));
        });
    }

    [Test]
    public void CorrugatedProfileForcesBoxAndStricterModuleAndBearerMinimums()
    {
        ItfOptions corrugated = new ItfOptions();
        corrugated.UseCorrugatedProfile = true;
        corrugated.BearerBarThicknessModules = 8;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = 0.635;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.Sizing = sizing;

        Itf14LayoutResult result = Create(CompleteDigits, corrugated, common);
        BarcodeSizing tooSmallSizing = new BarcodeSizing();
        tooSmallSizing.ModuleSizeMillimetres = 0.634;
        BarcodeEncodeOptions tooSmall = new BarcodeEncodeOptions();
        tooSmall.Sizing = tooSmallSizing;
        ItfOptions thinCorrugated = new ItfOptions();
        thinCorrugated.UseCorrugatedProfile = true;
        thinCorrugated.BearerBarThicknessModules = 4;

        Assert.Multiple(() =>
        {
            Assert.That(result.UsesCorrugatedProfile, Is.True);
            Assert.That(result.UsesBearerBox, Is.True);
            Assert.That(result.BearerBars, Has.Length.EqualTo(4));
            Assert.That(result.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.635));
            Assert.That(result.BearerThicknessModules, Is.EqualTo(8.0));
            Assert.That(() => Create(CompleteDigits, corrugated, tooSmall), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, thinCorrugated, common), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ProfileEnforcesGs1RatioModuleAndBarHeightRanges()
    {
        ItfOptions lowRatio = new ItfOptions();
        lowRatio.WideToNarrowRatio = 2.24;
        ItfOptions highRatio = new ItfOptions();
        highRatio.WideToNarrowRatio = 3.01;
        BarcodeSizing lowModule = new BarcodeSizing();
        lowModule.ModuleSizeMillimetres = 0.494;
        BarcodeSizing highModule = new BarcodeSizing();
        highModule.ModuleSizeMillimetres = 1.017;
        BarcodeSizing shortBars = new BarcodeSizing();
        shortBars.BarHeightMillimetres = 31.74;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create(CompleteDigits, lowRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, highRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(lowModule), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(highModule), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(shortBars), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void RequestedBoxWidthDerivesModuleSizeWithFixedBearerThickness()
    {
        ItfOptions box = new ItfOptions();
        box.UseBearerBox = true;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthModules = 121;
        sizing.RequestedHeightModules = 1;
        sizing.RequestedWidthMillimetres = 79.91;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.Sizing = sizing;

        Itf14LayoutResult result = Create(CompleteDigits, box, common);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(79.91).Within(0.000001));
            Assert.That(result.BearerThicknessModules, Is.EqualTo(9.66).Within(0.000001));
        });
    }

    [Test]
    public void QuietZonesCannotUndercutPartialReadProtection()
    {
        BarcodeEncodeOptions exactMinimum = new BarcodeEncodeOptions();
        exactMinimum.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        exactMinimum.QuietZone = CreateQuietZone(10, 1, 10, 2);
        BarcodeEncodeOptions tooSmall = new BarcodeEncodeOptions();
        tooSmall.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        tooSmall.QuietZone = CreateQuietZone(9, 0, 10, 0);

        Itf14LayoutResult result = Create(CompleteDigits, null, exactMinimum);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size.QuietZone.TopModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Size.QuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size.QuietZone.BottomModules, Is.EqualTo(2));
            Assert.That(result.MinimumCompleteScanWidthModules, Is.EqualTo(140.5));
            Assert.That(result.ExpectedDigitCount, Is.EqualTo(14));
            Assert.That(result.ExpectedPairCount, Is.EqualTo(7));
            Assert.That(() => Create(CompleteDigits, null, tooSmall), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void AllowedLengthPolicyCannotPermitShortEmbeddedSymbols()
    {
        ItfOptions exact = new ItfOptions();
        exact.AllowedLengths = new int[] { 14 };
        ItfOptions weakened = new ItfOptions();
        weakened.AllowedLengths = new int[] { 12, 14 };
        ItfOptions empty = new ItfOptions();
        empty.AllowedLengths = new int[0];

        Assert.Multiple(() =>
        {
            Assert.That(Create(CompleteDigits, exact, null).ExpectedDigitCount, Is.EqualTo(14));
            Assert.That(() => Create(CompleteDigits, weakened, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, empty, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void StandardHrtUsesGs1GroupingAndOtherModesRemainAvailable()
    {
        BarcodePayload payload = CreatePayload(DataDigits, new byte[] { 49, 50, 51 });
        BarcodeEncodeOptions standard = new BarcodeEncodeOptions();
        standard.TextMode = BarcodeTextMode.Standard;
        BarcodeEncodeOptions raw = new BarcodeEncodeOptions();
        raw.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions custom = new BarcodeEncodeOptions();
        custom.TextMode = BarcodeTextMode.Custom;
        custom.CustomText = "case label";
        BarcodeEncodeOptions hidden = new BarcodeEncodeOptions();
        hidden.TextMode = BarcodeTextMode.Hidden;

        Itf14LayoutResult standardResult = Itf14Profile.CreateLayout(DataDigits, payload, null, standard);
        Itf14LayoutResult rawResult = Itf14Profile.CreateLayout(DataDigits, payload, null, raw);
        Itf14LayoutResult customResult = Itf14Profile.CreateLayout(DataDigits, payload, null, custom);
        Itf14LayoutResult hiddenResult = Itf14Profile.CreateLayout(DataDigits, payload, null, hidden);

        Assert.Multiple(() =>
        {
            Assert.That(standardResult.Symbol.TextLayout!.Runs![0].Text,
                Is.EqualTo("0 95 01101 53001 0"));
            Assert.That(standardResult.Symbol.TextLayout.Runs[0].X, Is.EqualTo(10.0));
            Assert.That(standardResult.Symbol.TextLayout.Runs[0].Width, Is.EqualTo(120.5));
            Assert.That(rawResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("123"));
            Assert.That(customResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("case label"));
            Assert.That(hiddenResult.Symbol.TextLayout, Is.Null);
        });
    }

    [Test]
    public void InvalidBearerSizingTextAndPayloadAreRejected()
    {
        ItfOptions tooThin = new ItfOptions();
        tooThin.BearerBarThicknessModules = 1;
        BarcodeSizing minimumModule = new BarcodeSizing();
        minimumModule.ModuleSizeMillimetres = 0.495;
        BarcodeEncodeOptions thinCommon = new BarcodeEncodeOptions();
        thinCommon.Sizing = minimumModule;
        BarcodeEncodeOptions missingCustom = new BarcodeEncodeOptions();
        missingCustom.TextMode = BarcodeTextMode.Custom;
        BarcodeSizing invalidMeasurement = new BarcodeSizing();
        invalidMeasurement.ModuleSizeMillimetres = double.NaN;
        BarcodeEncodeOptions invalidSizing = new BarcodeEncodeOptions();
        invalidSizing.Sizing = invalidMeasurement;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create(CompleteDigits, tooThin, thinCommon), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, null, missingCustom), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create(CompleteDigits, null, invalidSizing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Itf14Profile.CreateLayout(CompleteDigits, null!, null, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static Itf14LayoutResult Create(
        string text,
        ItfOptions? itfOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        return Itf14Profile.CreateLayout(text, CreatePayload(text, null), itfOptions, encodeOptions);
    }

    static Itf14LayoutResult CreateWithSizing(BarcodeSizing sizing)
    {
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.Sizing = sizing;
        return Create(CompleteDigits, null, common);
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
