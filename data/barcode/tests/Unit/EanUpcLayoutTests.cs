namespace barcode.Tests;

public class EanUpcLayoutTests
{
    [TestCase("12", 32)]
    [TestCase("51234", 59)]
    public void StandaloneAddOnLayoutProvidesDecoderQuietZones(
        string text,
        int physicalModules)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(text);

        BarcodeSymbol symbol = EanUpcLayout.CreateAddOnSymbol(
            encoded,
            CreatePayload(text),
            null);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.RequiredQuietZone!.LeftModules,
                Is.EqualTo(7));
            Assert.That(symbol.RequiredQuietZone.RightModules,
                Is.EqualTo(5));
            Assert.That(symbol.Size!.WidthModules,
                Is.EqualTo(physicalModules));
        });
    }

    [TestCase(BarcodeFormat.Ean13, 95, 11, 7, 113, 37.29, 22.85)]
    [TestCase(BarcodeFormat.Ean8, 67, 7, 7, 81, 26.73, 18.23)]
    [TestCase(BarcodeFormat.UpcA, 95, 9, 9, 113, 37.29, 22.85)]
    [TestCase(BarcodeFormat.UpcE, 51, 9, 7, 67, 22.11, 22.85)]
    public void StandardLayoutUsesExactModuleCountsQuietZonesAndNominalSize(
        BarcodeFormat format,
        int symbolModules,
        int leftQuietZone,
        int rightQuietZone,
        int totalModules,
        double widthMillimetres,
        double heightMillimetres)
    {
        EanUpcEncodedData encoded = CreateMain(format);
        EanUpcAddonAssociation association = CreateAssociation(encoded);

        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(
            association,
            CreatePayload(encoded.Text),
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Format, Is.EqualTo(format));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(symbolModules));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Modules, Is.EqualTo(encoded.Modules));
            Assert.That(result.Symbol.Modules, Is.Not.SameAs(encoded.Modules));
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(leftQuietZone));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(rightQuietZone));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(totalModules));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Size.ModuleSizeMillimetres, Is.EqualTo(0.330));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(widthMillimetres).Within(0.000001));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(heightMillimetres).Within(0.000001));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]E0"));
            Assert.That(result.Symbol.HasChecksum, Is.True);
            Assert.That(result.Symbol.ChecksumValue, Is.EqualTo(encoded.ChecksumValue));
            Assert.That(result.MainModuleCount, Is.EqualTo(symbolModules));
            Assert.That(result.AddOnOffsetModules, Is.Zero);
            Assert.That(result.AddOnModuleCount, Is.Zero);
            Assert.That(result.AddOnSeparationModules, Is.Zero);
        });
    }

    [TestCase(BarcodeFormat.Ean13, "12", 7, 102, 20, 122, 138)]
    [TestCase(BarcodeFormat.Ean13, "51234", 7, 102, 47, 149, 165)]
    [TestCase(BarcodeFormat.UpcA, "12", 9, 104, 20, 124, 138)]
    [TestCase(BarcodeFormat.UpcE, "51234", 7, 58, 47, 105, 119)]
    public void AddOnPlacementUsesFormatSeparationAndTrailingQuietZone(
        BarcodeFormat format,
        string addOn,
        int separation,
        int addOnOffset,
        int addOnModules,
        int combinedModules,
        int physicalModules)
    {
        EanUpcEncodedData main = CreateMain(format);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.AddOn = addOn;
        EanUpcAddonAssociation association = EanUpcAddon.Associate(main, eanOptions);

        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(
            association,
            CreatePayload(main.Text),
            null,
            eanOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddOnSeparationModules, Is.EqualTo(separation));
            Assert.That(result.AddOnOffsetModules, Is.EqualTo(addOnOffset));
            Assert.That(result.AddOnModuleCount, Is.EqualTo(addOnModules));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(combinedModules));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(physicalModules));
            Assert.That(result.Symbol.RequiredQuietZone!.RightModules, Is.EqualTo(5));
            Assert.That(result.Symbol.Modules![..main.Modules.Length], Is.EqualTo(main.Modules));
            Assert.That(result.Symbol.Modules[main.Modules.Length..addOnOffset], Is.All.Zero);
            Assert.That(result.Symbol.Modules[addOnOffset..], Is.EqualTo(association.AddOn!.Modules));
        });
    }

    [Test]
    public void CombinedLayoutDoesNotAliasMainAddOnOrPayloadData()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.AddOn = "12";
        EanUpcAddonAssociation association = EanUpcAddon.Associate(main, eanOptions);
        BarcodePayload payload = CreatePayload(main.Text);
        payload.RawBytes = new byte[] { 1, 2, 3 };

        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(association, payload, null, eanOptions);
        main.Modules[0] = 0;
        association.AddOn!.Modules[0] = 0;
        payload.RawBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Modules![0], Is.EqualTo(1));
            Assert.That(result.Symbol.Modules[result.AddOnOffsetModules], Is.EqualTo(1));
            Assert.That(result.Symbol.Payload, Is.Not.SameAs(payload));
            Assert.That(result.Symbol.Payload!.RawBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void GuardDescentIdentifiesGuardsAndUpcAOuterCharacters()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.UpcA);
        EanUpcAddonAssociation association = EanUpcAddon.Associate(main, null);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.ExtendGuardBars = true;

        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(
            association,
            CreatePayload(main.Text),
            null,
            eanOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.GuardDescentModules, Is.EqualTo(5.0));
            Assert.That(result.GuardBars, Has.Length.EqualTo(10));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 0 && bar.ModuleCount == 1));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 46));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 94));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 6 && bar.ModuleCount == 2));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 85 && bar.ModuleCount == 2));
            Assert.That(result.GuardBars, Is.All.Matches<EanUpcGuardBar>(bar => bar.DescentModules == 5.0));
        });
    }

    [Test]
    public void GuardDescentCanBeSuppressedWithoutLosingGuardGeometry()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.UpcE);
        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(
            CreateAssociation(main),
            CreatePayload(main.Text),
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.GuardBars, Has.Length.EqualTo(5));
            Assert.That(result.GuardBars, Has.Some.Matches<EanUpcGuardBar>(bar => bar.ModuleOffset == 50));
            Assert.That(result.GuardDescentModules, Is.Zero);
            Assert.That(result.GuardBars, Is.All.Matches<EanUpcGuardBar>(bar => bar.DescentModules == 0.0));
        });
    }

    [Test]
    public void StandardHumanReadableTextGroupsEanAndUpcDigits()
    {
        BarcodeTextRun[] ean13 = CreateStandardTextRuns(BarcodeFormat.Ean13);
        BarcodeTextRun[] ean8 = CreateStandardTextRuns(BarcodeFormat.Ean8);
        BarcodeTextRun[] upcA = CreateStandardTextRuns(BarcodeFormat.UpcA);
        BarcodeTextRun[] upcE = CreateStandardTextRuns(BarcodeFormat.UpcE);

        Assert.Multiple(() =>
        {
            Assert.That(ReadText(ean13), Is.EqualTo(new string[] { "4", "006381", "333931" }));
            Assert.That(ReadX(ean13), Is.EqualTo(new double[] { 4.0, 14.0, 61.0 }));
            Assert.That(ReadText(ean8), Is.EqualTo(new string[] { "5512", "3457" }));
            Assert.That(ReadX(ean8), Is.EqualTo(new double[] { 10.0, 43.0 }));
            Assert.That(ReadText(upcA), Is.EqualTo(new string[] { "0", "36000", "29145", "2" }));
            Assert.That(ReadX(upcA), Is.EqualTo(new double[] { 2.0, 19.0, 59.0, 104.0 }));
            Assert.That(ReadText(upcE), Is.EqualTo(new string[] { "0", "123455", "8" }));
            Assert.That(ReadX(upcE), Is.EqualTo(new double[] { 2.0, 12.0, 60.0 }));
        });
    }

    [Test]
    public void AddOnTextIsPlacedAboveItsBarsAndIndicatorsUseOuterQuietZones()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.AddOn = "12";
        eanOptions.ShowQuietZoneIndicators = true;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;

        EanUpcLayoutResult result = EanUpcLayout.CreateLayout(
            EanUpcAddon.Associate(main, eanOptions),
            CreatePayload(main.Text),
            options,
            eanOptions);
        BarcodeTextLayout layout = result.Symbol.TextLayout!;

        Assert.Multiple(() =>
        {
            Assert.That(layout.Runs, Has.Length.EqualTo(5));
            Assert.That(layout.Runs![3].Text, Is.EqualTo("12"));
            Assert.That(layout.Runs[3].X, Is.EqualTo(117.0));
            Assert.That(layout.Runs[3].BaselineY, Is.Zero);
            Assert.That(layout.Runs[4].Text, Is.EqualTo(">"));
            Assert.That(layout.Runs[4].X, Is.EqualTo(133.0));
            Assert.That(layout.Bounds!.Y, Is.EqualTo(-7.0));
            Assert.That(layout.ReservedHeight, Is.EqualTo(14.0));
            Assert.That(layout.Size!.HeightModules, Is.EqualTo(15));
        });
    }

    [Test]
    public void Ean8QuietZoneIndicatorsMarkBothSides()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean8);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.ShowQuietZoneIndicators = true;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;

        BarcodeSymbol symbol = EanUpcLayout.CreateSymbol(
            CreateAssociation(main),
            CreatePayload(main.Text),
            options,
            eanOptions);

        Assert.Multiple(() =>
        {
            Assert.That(ReadText(symbol.TextLayout!.Runs!), Is.EqualTo(new string[] { "5512", "3457", "<", ">" }));
            Assert.That(ReadX(symbol.TextLayout.Runs!), Is.EqualTo(new double[] { 10.0, 43.0, 0.0, 74.0 }));
        });
    }

    [Test]
    public void RawCustomAndHiddenTextModesAreHonoured()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        EanUpcAddonAssociation association = EanUpcAddon.Associate(main, null);
        BarcodePayload payload = CreatePayload("interpreted");
        payload.RawBytes = new byte[] { 65, 0, 255 };
        BarcodeEncodeOptions rawOptions = new BarcodeEncodeOptions();
        rawOptions.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions customOptions = new BarcodeEncodeOptions();
        customOptions.TextMode = BarcodeTextMode.Custom;
        customOptions.CustomText = "shelf label";

        BarcodeSymbol raw = EanUpcLayout.CreateSymbol(association, payload, rawOptions, null);
        BarcodeSymbol custom = EanUpcLayout.CreateSymbol(association, payload, customOptions, null);
        BarcodeSymbol hidden = EanUpcLayout.CreateSymbol(association, payload, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(raw.TextLayout!.Runs![0].Text, Is.EqualTo("A\0\u00ff"));
            Assert.That(custom.TextLayout!.Runs![0].Text, Is.EqualTo("shelf label"));
            Assert.That(hidden.TextLayout, Is.Null);
        });
    }

    [Test]
    public void PhysicalSizingSupportsMagnificationAndCustomVerticalQuietZones()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        BarcodeQuietZone quietZone = CreateQuietZone(11, 1, 7, 1);
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthModules = 95;
        sizing.RequestedHeightModules = 1;
        sizing.RequestedWidthPixels = 452;
        sizing.RequestedHeightPixels = 288;
        sizing.RequestedWidthMillimetres = 45.2;
        sizing.RequestedHeightMillimetres = 28.8;
        sizing.ModuleSizeMillimetres = 0.4;
        sizing.BarHeightMillimetres = 28.0;
        sizing.DotsPerInch = 300.0;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        options.QuietZone = quietZone;
        options.Sizing = sizing;

        BarcodeSymbol symbol = EanUpcLayout.CreateSymbol(
            CreateAssociation(main),
            CreatePayload(main.Text),
            options,
            null);
        quietZone.LeftModules = 99;

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Size!.WidthModules, Is.EqualTo(113));
            Assert.That(symbol.Size.HeightModules, Is.EqualTo(3));
            Assert.That(symbol.Size.WidthPixels, Is.EqualTo(452));
            Assert.That(symbol.Size.HeightPixels, Is.EqualTo(288));
            Assert.That(symbol.Size.ModuleSizeMillimetres, Is.EqualTo(0.4));
            Assert.That(symbol.Size.WidthMillimetres, Is.EqualTo(45.2).Within(0.000001));
            Assert.That(symbol.Size.HeightMillimetres, Is.EqualTo(28.8).Within(0.000001));
            Assert.That(symbol.Size.DotsPerInch, Is.EqualTo(300.0));
            Assert.That(symbol.Size.QuietZone!.LeftModules, Is.EqualTo(11));
            Assert.That(symbol.Size.QuietZone.TopModules, Is.EqualTo(1));
        });
    }

    [Test]
    public void WidthRequestCanDeriveAStandardsCompliantModuleSize()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean8);
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthMillimetres = 32.4;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;

        BarcodeSymbol symbol = EanUpcLayout.CreateSymbol(
            CreateAssociation(main),
            CreatePayload(main.Text),
            options,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.4).Within(0.000001));
            Assert.That(symbol.Size.WidthMillimetres, Is.EqualTo(32.4).Within(0.000001));
            Assert.That(symbol.Size.HeightMillimetres, Is.EqualTo(22.0969696969).Within(0.000001));
        });
    }

    [Test]
    public void LayoutRejectsMalformedPatternsAssociationsAndSizing()
    {
        EanUpcEncodedData malformedGuard = CreateMain(BarcodeFormat.Ean13);
        malformedGuard.Modules[0] = 0;
        EanUpcEncodedData malformedCheck = CreateMain(BarcodeFormat.Ean13);
        malformedCheck.ChecksumValue = 9;
        EanUpcEncodedData malformedDimensions = CreateMain(BarcodeFormat.UpcE);
        malformedDimensions.Modules = new byte[50];
        EanUpcAddonAssociation invalidAddOn = EanUpcAddon.Associate(CreateMain(BarcodeFormat.Ean13), CreateAddonOptions("12"));
        invalidAddOn.AddOn!.Modules[11] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(() => CreateMalformedLayout(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CreateMalformedLayout(CreateAssociation(malformedGuard)), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CreateMalformedLayout(CreateAssociation(malformedCheck)), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CreateMalformedLayout(CreateAssociation(malformedDimensions)), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CreateMalformedLayout(invalidAddOn), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcLayout.CreateLayout(CreateAssociation(CreateMain(BarcodeFormat.Ean13)), null!, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void LayoutRejectsUnderQuietZonesAndNonCompliantPhysicalDimensions()
    {
        BarcodeEncodeOptions underQuietZone = new BarcodeEncodeOptions();
        underQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        underQuietZone.QuietZone = CreateQuietZone(10, 0, 7, 0);
        BarcodeEncodeOptions exactQuietZone = new BarcodeEncodeOptions();
        exactQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        exactQuietZone.QuietZone = CreateQuietZone(1, 0, 2, 0);
        BarcodeSizing tooNarrow = new BarcodeSizing();
        tooNarrow.ModuleSizeMillimetres = 0.2;
        BarcodeSizing tooWide = new BarcodeSizing();
        tooWide.ModuleSizeMillimetres = 0.7;
        BarcodeSizing tooShort = new BarcodeSizing();
        tooShort.BarHeightMillimetres = 20.0;
        BarcodeSizing wrongModules = new BarcodeSizing();
        wrongModules.RequestedWidthModules = 94;
        BarcodeSizing notFinite = new BarcodeSizing();
        notFinite.RequestedHeightMillimetres = double.PositiveInfinity;

        BarcodeSymbol diagnostic = CreateWithOptions(exactQuietZone);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Size!.WidthModules, Is.EqualTo(98));
            Assert.That(diagnostic.RequiredQuietZone!.LeftModules, Is.EqualTo(11));
            Assert.That(() => CreateWithOptions(underQuietZone), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(tooNarrow), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(tooWide), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(tooShort), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(wrongModules), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CreateWithSizing(notFinite), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static BarcodeTextRun[] CreateStandardTextRuns(BarcodeFormat format)
    {
        EanUpcEncodedData main = CreateMain(format);
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;
        BarcodeSymbol symbol = EanUpcLayout.CreateSymbol(
            CreateAssociation(main),
            CreatePayload(main.Text),
            options,
            null);
        return symbol.TextLayout!.Runs!;
    }

    static string[] ReadText(BarcodeTextRun[] runs)
    {
        return runs.Select(run => run.Text!).ToArray();
    }

    static double[] ReadX(BarcodeTextRun[] runs)
    {
        return runs.Select(run => run.X).ToArray();
    }

    static EanUpcEncodedData CreateMain(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Ean13:
                return EanUpcEncoder.Encode("4006381333931", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.Ean8:
                return EanUpcEncoder.Encode("55123457", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.UpcA:
                return EanUpcEncoder.Encode("036000291452", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.UpcE:
                return EanUpcEncoder.EncodeUpcE("01234558", 0, BarcodeChecksumMode.Require);
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    static EanUpcAddonAssociation CreateAssociation(EanUpcEncodedData main)
    {
        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = main;
        return association;
    }

    static EanUpcOptions CreateAddonOptions(string addOn)
    {
        EanUpcOptions options = new EanUpcOptions();
        options.AddOn = addOn;
        return options;
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
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

    static EanUpcLayoutResult CreateMalformedLayout(EanUpcAddonAssociation association)
    {
        return EanUpcLayout.CreateLayout(association, CreatePayload("payload"), null, null);
    }

    static BarcodeSymbol CreateWithOptions(BarcodeEncodeOptions options)
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        return EanUpcLayout.CreateSymbol(
            EanUpcAddon.Associate(main, null),
            CreatePayload(main.Text),
            options,
            null);
    }

    static BarcodeSymbol CreateWithSizing(BarcodeSizing sizing)
    {
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;
        return CreateWithOptions(options);
    }
}
