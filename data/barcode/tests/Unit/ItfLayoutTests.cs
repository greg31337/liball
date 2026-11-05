namespace barcode.Tests;

public class ItfLayoutTests
{
    [Test]
    public void DefaultLayoutCreatesFramedInterleavedSymbol()
    {
        BarcodePayload payload = CreatePayload("1234", new byte[] { 49, 50, 51, 52 });

        ItfLayoutResult result = ItfLayout.CreateLayout("1234", payload, null, null);
        payload.RawBytes![0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Itf));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(45));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(45));
            Assert.That(result.Symbol.Modules![0], Is.EqualTo(1));
            Assert.That(result.Symbol.Modules[^1], Is.EqualTo(1));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(65));
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Payload!.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51, 52 }));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]I0"));
            Assert.That(result.Symbol.Codewords, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            Assert.That(result.Symbol.HasChecksum, Is.False);
            Assert.That(result.WideToNarrowRatio, Is.EqualTo(3.0));
            Assert.That(result.SymbolWidthModules, Is.EqualTo(45.0));
            Assert.That(result.ElementWidths, Has.Length.EqualTo(27));
            Assert.That(result.ElementWidths.Take(4), Is.EqualTo(new double[] { 1, 1, 1, 1 }));
            Assert.That(result.ElementWidths.Skip(4).Take(10),
                Is.EqualTo(new double[] { 3, 1, 1, 3, 1, 1, 1, 1, 3, 3 }));
            Assert.That(result.ElementWidths.TakeLast(3), Is.EqualTo(new double[] { 3, 1, 1 }));
        });
    }

    [Test]
    public void ChecksumPoliciesControlCarrierHrtAndIdentifier()
    {
        ItfOptions calculatedOptions = new ItfOptions();
        calculatedOptions.ChecksumMode = BarcodeChecksumMode.Calculate;
        ItfOptions keptOptions = new ItfOptions();
        keptOptions.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        ItfOptions strippedOptions = new ItfOptions();
        strippedOptions.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Standard;

        ItfLayoutResult calculated = Create("12345", calculatedOptions, common);
        ItfLayoutResult kept = Create("123457", keptOptions, common);
        ItfLayoutResult stripped = Create("123457", strippedOptions, common);

        Assert.Multiple(() =>
        {
            Assert.That(calculated.Encoded.EncodedText, Is.EqualTo("123457"));
            Assert.That(calculated.Symbol.HasChecksum, Is.True);
            Assert.That(calculated.Symbol.ChecksumValue, Is.EqualTo(7));
            Assert.That(calculated.Symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(calculated.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("123457"));
            Assert.That(kept.Symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(kept.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("123457"));
            Assert.That(stripped.Symbol.SymbologyIdentifier, Is.EqualTo("]I3"));
            Assert.That(stripped.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("12345"));
            Assert.That(stripped.Symbol.Codewords, Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 7 }));
        });
    }

    [Test]
    public void FractionalRatioPreservesExactGeometryWithoutIntegerRasterClaim()
    {
        ItfOptions fractionalOptions = new ItfOptions();
        fractionalOptions.WideToNarrowRatio = 2.5;
        ItfOptions integralOptions = new ItfOptions();
        integralOptions.WideToNarrowRatio = 2.0;

        ItfLayoutResult fractional = Create("12", fractionalOptions, null);
        ItfLayoutResult integral = Create("12", integralOptions, null);

        Assert.Multiple(() =>
        {
            Assert.That(fractional.SymbolWidthModules, Is.EqualTo(24.5));
            Assert.That(fractional.Symbol.WidthModules, Is.EqualTo(25));
            Assert.That(fractional.Symbol.Modules, Is.Null);
            Assert.That(fractional.Symbol.Size!.WidthModules, Is.EqualTo(45));
            Assert.That(fractional.ElementWidths.Count(width => width == 2.5), Is.EqualTo(5));
            Assert.That(integral.SymbolWidthModules, Is.EqualTo(22.0));
            Assert.That(integral.Symbol.Modules, Has.Length.EqualTo(22));
        });
    }

    [Test]
    public void CustomQuietZonesPhysicalSizingAndHrtUseExactSymbolWidth()
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

        ItfLayoutResult result = Create("12", null, common);
        quietZone.LeftModules = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size!.QuietZone!.LeftModules, Is.EqualTo(2));
            Assert.That(result.Symbol.Size.QuietZone.RightModules, Is.EqualTo(3));
            Assert.That(result.Symbol.Size.WidthModules, Is.EqualTo(32));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(4));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(16.0));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(11.5));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("12"));
            Assert.That(result.Symbol.TextLayout.Runs[0].X, Is.EqualTo(2.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].BaselineY, Is.EqualTo(5.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].Width, Is.EqualTo(27.0));
        });
    }

    [Test]
    public void RequestedPhysicalWidthDerivesNarrowModuleSize()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthModules = 27;
        sizing.RequestedHeightModules = 1;
        sizing.RequestedWidthMillimetres = 23.5;
        sizing.RequestedHeightMillimetres = 12.0;
        sizing.DotsPerInch = 300.0;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.Sizing = sizing;

        ItfLayoutResult result = Create("12", null, common);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.5));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(23.5));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(12.0));
            Assert.That(result.Symbol.Size.DotsPerInch, Is.EqualTo(300.0));
        });
    }

    [Test]
    public void RawCustomAndHiddenTextModesSelectRequestedContent()
    {
        BarcodePayload payload = CreatePayload("12", new byte[] { 0, 65, 255 });
        BarcodeEncodeOptions raw = new BarcodeEncodeOptions();
        raw.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions custom = new BarcodeEncodeOptions();
        custom.TextMode = BarcodeTextMode.Custom;
        custom.CustomText = "label";
        BarcodeEncodeOptions hidden = new BarcodeEncodeOptions();
        hidden.TextMode = BarcodeTextMode.Hidden;

        ItfLayoutResult rawResult = ItfLayout.CreateLayout("12", payload, null, raw);
        ItfLayoutResult customResult = ItfLayout.CreateLayout("12", payload, null, custom);
        ItfLayoutResult hiddenResult = ItfLayout.CreateLayout("12", payload, null, hidden);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("\0A\u00ff"));
            Assert.That(customResult.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("label"));
            Assert.That(hiddenResult.Symbol.TextLayout, Is.Null);
        });
    }

    [Test]
    public void GenericChecksumPolicyIsUsedWhenItfOptionsAreAbsent()
    {
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.ChecksumMode = BarcodeChecksumMode.Calculate;

        ItfLayoutResult result = Create("12345", null, common);

        Assert.Multiple(() =>
        {
            Assert.That(result.Encoded.EncodedText, Is.EqualTo("123457"));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
        });
    }

    [Test]
    public void LayoutEnforcesAllowedEncodedLengths()
    {
        ItfOptions options = new ItfOptions();
        options.AllowedLengths = new int[] { 4, 6 };

        Assert.Multiple(() =>
        {
            Assert.That(Create("1234", options, null).Encoded.EncodedText, Has.Length.EqualTo(4));
            Assert.That(() => Create("12", options, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void LayoutRejectsInvalidRatioQuietZonesSizingAndProfileOptions()
    {
        ItfOptions lowRatio = new ItfOptions();
        lowRatio.WideToNarrowRatio = 1.99;
        ItfOptions highRatio = new ItfOptions();
        highRatio.WideToNarrowRatio = 3.01;
        ItfOptions notFinite = new ItfOptions();
        notFinite.WideToNarrowRatio = double.NaN;
        ItfOptions bearer = new ItfOptions();
        bearer.UseBearerBars = true;
        BarcodeEncodeOptions missingCustom = new BarcodeEncodeOptions();
        missingCustom.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        BarcodeEncodeOptions tooSmall = new BarcodeEncodeOptions();
        tooSmall.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        tooSmall.QuietZone = CreateQuietZone(9, 0, 10, 0);
        BarcodeSizing wrongWidth = new BarcodeSizing();
        wrongWidth.RequestedWidthModules = 28;
        BarcodeEncodeOptions wrongSizing = new BarcodeEncodeOptions();
        wrongSizing.Sizing = wrongWidth;
        BarcodeSizing notFiniteSizing = new BarcodeSizing();
        notFiniteSizing.ModuleSizeMillimetres = double.PositiveInfinity;
        BarcodeEncodeOptions invalidSizing = new BarcodeEncodeOptions();
        invalidSizing.Sizing = notFiniteSizing;
        BarcodeEncodeOptions missingText = new BarcodeEncodeOptions();
        missingText.TextMode = BarcodeTextMode.Custom;

        Assert.Multiple(() =>
        {
            Assert.That(() => Create("12", lowRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", highRatio, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", notFinite, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", bearer, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", null, missingCustom), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", null, tooSmall), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", null, wrongSizing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", null, invalidSizing), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Create("12", null, missingText), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => ItfLayout.CreateLayout("12", null!, null, null), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static ItfLayoutResult Create(
        string text,
        ItfOptions? itfOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        return ItfLayout.CreateLayout(text, CreatePayload(text, null), itfOptions, encodeOptions);
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
