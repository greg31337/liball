namespace barcode.Tests;

public class Pdf417LayoutTests
{
    [Test]
    public void StandardRowsUseThreeClustersAndBothRowIndicators()
    {
        Pdf417SymbolPlan plan = CreatePlan(Pdf417Variant.Standard);
        int[] codewords = Pdf417ReedSolomon.EncodeCodewords(
            plan.DataCodewords,
            plan.ErrorCorrectionLevel);

        BarcodeSymbol symbol = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            new byte[] { 65, 66 },
            CreatePayload("AB"),
            new BarcodeEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(symbol.Dimension, Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(symbol.WidthModules, Is.EqualTo(103));
            Assert.That(symbol.HeightModules, Is.EqualTo(9));
            Assert.That(symbol.Modules, Has.Length.EqualTo(927));
            Assert.That(symbol.Codewords, Is.EqualTo(codewords));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(BarcodeSymbologyIdentifiers.Pdf417));
            Assert.That(symbol.HasChecksum, Is.False);
        });

        AssertRow(symbol, plan, codewords, 0, 0, 0, 1);
        AssertRow(symbol, plan, codewords, 1, 1, 2, 0);
        AssertRow(symbol, plan, codewords, 2, 2, 1, 2);
    }

    [Test]
    public void CompactRowsOmitTheRightIndicatorAndUseTheTruncatedStopBar()
    {
        Pdf417SymbolPlan plan = CreatePlan(Pdf417Variant.Compact);
        int[] codewords = Pdf417ReedSolomon.EncodeCodewords(
            plan.DataCodewords,
            plan.ErrorCorrectionLevel);

        BarcodeSymbol symbol = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            new byte[] { 65, 66 },
            CreatePayload("AB"),
            new BarcodeEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.CompactPdf417));
            Assert.That(symbol.WidthModules, Is.EqualTo(69));
            Assert.That(symbol.HeightModules, Is.EqualTo(9));
            Assert.That(ReadPattern(symbol, 0, 0, 17), Is.EqualTo(Pdf417CodewordTable.StartPatternBits));
            Assert.That(ReadPattern(symbol, 0, 17, 17), Is.EqualTo(Pdf417CodewordTable.GetPattern(0, 0)));
            Assert.That(ReadPattern(symbol, 0, 34, 17), Is.EqualTo(Pdf417CodewordTable.GetPattern(0, codewords[0])));
            Assert.That(ReadPattern(symbol, 0, 51, 17), Is.EqualTo(Pdf417CodewordTable.GetPattern(0, codewords[1])));
            Assert.That(ReadPattern(symbol, 0, 68, 1), Is.EqualTo(1));
        });

        AssertRepeatedRows(symbol, plan.Rows);
    }

    [Test]
    public void QuietZonesSizingAndHumanReadableMetadataAreResolvedTogether()
    {
        Pdf417SymbolPlan plan = CreatePlan(Pdf417Variant.Standard);
        int[] codewords = Pdf417ReedSolomon.EncodeCodewords(
            plan.DataCodewords,
            plan.ErrorCorrectionLevel);
        byte[] data = new byte[] { 65, 66 };
        BarcodePayload payload = CreatePayload("AB");
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = 3;
        quietZone.TopModules = 4;
        quietZone.RightModules = 5;
        quietZone.BottomModules = 6;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthPixels = 222;
        sizing.RequestedHeightPixels = 38;
        sizing.ModuleSizeMillimetres = 0.25;
        sizing.BarHeightMillimetres = 0.75;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = quietZone;
        options.Sizing = sizing;
        options.TextMode = BarcodeTextMode.Standard;
        options.CharacterEncoding = "ISO-8859-1";

        BarcodeSymbol symbol = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            data,
            payload,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.RequiredQuietZone?.LeftModules, Is.EqualTo(2));
            Assert.That(symbol.RequiredQuietZone?.TopModules, Is.EqualTo(2));
            Assert.That(symbol.RequiredQuietZone?.RightModules, Is.EqualTo(2));
            Assert.That(symbol.RequiredQuietZone?.BottomModules, Is.EqualTo(2));
            Assert.That(symbol.Size?.WidthModules, Is.EqualTo(111));
            Assert.That(symbol.Size?.HeightModules, Is.EqualTo(19));
            Assert.That(symbol.Size?.WidthPixels, Is.EqualTo(222));
            Assert.That(symbol.Size?.HeightPixels, Is.EqualTo(38));
            Assert.That(symbol.Size?.WidthMillimetres, Is.EqualTo(27.75));
            Assert.That(symbol.Size?.HeightMillimetres, Is.EqualTo(4.75));
            Assert.That(symbol.Size?.QuietZone?.LeftPixels, Is.EqualTo(6));
            Assert.That(symbol.Size?.QuietZone?.BottomPixels, Is.EqualTo(12));
            Assert.That(symbol.Payload?.Text, Is.EqualTo("AB"));
            Assert.That(symbol.Payload?.RawBytes, Is.EqualTo(data));
            Assert.That(symbol.Payload?.CharacterEncoding, Is.EqualTo("ISO-8859-1"));
            Assert.That(symbol.TextLayout?.Runs, Has.Length.EqualTo(1));
            Assert.That(symbol.TextLayout?.Runs?[0].Text, Is.EqualTo("AB"));
            Assert.That(symbol.TextLayout?.Runs?[0].X, Is.EqualTo(3.0));
            Assert.That(symbol.TextLayout?.Runs?[0].BaselineY, Is.EqualTo(20.0));
            Assert.That(symbol.TextLayout?.Runs?[0].Width, Is.EqualTo(103.0));
            Assert.That(symbol.TextLayout?.Bounds?.Y, Is.EqualTo(19.0));
            Assert.That(symbol.TextLayout?.ReservedHeight, Is.EqualTo(1.0));
            Assert.That(symbol.TextLayout?.Size?.HeightModules, Is.EqualTo(20));
        });

        data[0] = 90;
        codewords[0] = 90;
        payload.Text = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(symbol.Payload?.RawBytes?[0], Is.EqualTo(65));
            Assert.That(symbol.Payload?.Text, Is.EqualTo("AB"));
            Assert.That(symbol.Codewords?[0], Is.EqualTo(plan.DataCodewords[0]));
        });
    }

    [Test]
    public void TextModesSelectRawCustomOrHiddenMetadata()
    {
        Pdf417SymbolPlan plan = CreatePlan(Pdf417Variant.Standard);
        int[] codewords = Pdf417ReedSolomon.EncodeCodewords(
            plan.DataCodewords,
            plan.ErrorCorrectionLevel);
        BarcodePayload payload = CreatePayload("interpreted");
        BarcodeEncodeOptions rawOptions = new BarcodeEncodeOptions();
        rawOptions.TextMode = BarcodeTextMode.RawPayload;
        BarcodeEncodeOptions customOptions = new BarcodeEncodeOptions();
        customOptions.TextMode = BarcodeTextMode.Custom;
        customOptions.CustomText = "display";

        BarcodeSymbol raw = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            new byte[] { 65, 255 },
            payload,
            rawOptions);
        BarcodeSymbol custom = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            new byte[] { 65 },
            payload,
            customOptions);
        BarcodeSymbol hidden = Pdf417Layout.CreateSymbol(
            plan,
            codewords,
            new byte[] { 65 },
            payload,
            new BarcodeEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(raw.TextLayout?.Runs?[0].Text, Is.EqualTo("A\u00ff"));
            Assert.That(custom.TextLayout?.Runs?[0].Text, Is.EqualTo("display"));
            Assert.That(hidden.TextLayout, Is.Null);
            Assert.That(hidden.Size?.WidthModules, Is.EqualTo(107));
            Assert.That(hidden.Size?.HeightModules, Is.EqualTo(13));
        });
    }

    [Test]
    public void IndicatorFormulasEncodeRowCountColumnsAndErrorCorrectionLevel()
    {
        int[] expectedLeft = new int[] { 2, 14, 4, 32, 44, 34, 62, 74, 64 };
        int[] expectedRight = new int[] { 4, 2, 14, 34, 32, 44, 64, 62, 74 };

        int row = 0;
        while (row < expectedLeft.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Pdf417Layout.GetClusterIndex(row), Is.EqualTo(row % 3));
                Assert.That(
                    Pdf417Layout.CalculateLeftRowIndicator(row, 9, 5, 4),
                    Is.EqualTo(expectedLeft[row]));
                Assert.That(
                    Pdf417Layout.CalculateRightRowIndicator(row, 9, 5, 4),
                    Is.EqualTo(expectedRight[row]));
            });
            row = row + 1;
        }
    }

    [Test]
    public void InvalidLayoutQuietZoneTextAndSizingInputsAreRejected()
    {
        Pdf417SymbolPlan plan = CreatePlan(Pdf417Variant.Standard);
        int[] codewords = Pdf417ReedSolomon.EncodeCodewords(
            plan.DataCodewords,
            plan.ErrorCorrectionLevel);
        BarcodePayload payload = CreatePayload("AB");
        BarcodeEncodeOptions smallQuietOptions = new BarcodeEncodeOptions();
        smallQuietOptions.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        smallQuietOptions.QuietZone = new BarcodeQuietZone();
        BarcodeEncodeOptions customTextOptions = new BarcodeEncodeOptions();
        customTextOptions.TextMode = BarcodeTextMode.Custom;
        BarcodeSizing distortedSizing = new BarcodeSizing();
        distortedSizing.RequestedWidthPixels = 214;
        distortedSizing.RequestedHeightPixels = 39;
        BarcodeEncodeOptions distortedOptions = new BarcodeEncodeOptions();
        distortedOptions.Sizing = distortedSizing;
        int[] changedCodewords = (int[])codewords.Clone();
        changedCodewords[0] = changedCodewords[0] + 1;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417Layout.CalculateLeftRowIndicator(-1, 3, 2, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417Layout.CalculateRightRowIndicator(3, 3, 2, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417Layout.GetClusterIndex(90),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417Layout.CreateSymbol(plan, changedCodewords, new byte[0], payload, new BarcodeEncodeOptions()),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417Layout.CreateSymbol(plan, codewords, new byte[0], payload, smallQuietOptions),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Layout.CreateSymbol(plan, codewords, new byte[0], payload, customTextOptions),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Layout.CreateSymbol(plan, codewords, new byte[0], payload, distortedOptions),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static Pdf417SymbolPlan CreatePlan(Pdf417Variant variant)
    {
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.Rows = 3;
        options.ErrorCorrectionLevel = 0;
        options.Variant = variant;
        return Pdf417SymbolPlanner.CreateSymbolPlan(
            new int[] { 1, 2, 3 },
            options,
            null);
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        return payload;
    }

    static void AssertRow(
        BarcodeSymbol symbol,
        Pdf417SymbolPlan plan,
        int[] codewords,
        int rowNumber,
        int clusterIndex,
        int leftIndicator,
        int rightIndicator)
    {
        int moduleRow = rowNumber * Pdf417Layout.DefaultRowHeightModules;
        int codewordOffset = rowNumber * plan.Columns;
        Assert.Multiple(() =>
        {
            Assert.That(
                ReadPattern(symbol, moduleRow, 0, 17),
                Is.EqualTo(Pdf417CodewordTable.StartPatternBits));
            Assert.That(
                ReadPattern(symbol, moduleRow, 17, 17),
                Is.EqualTo(Pdf417CodewordTable.GetPattern(clusterIndex, leftIndicator)));
            Assert.That(
                ReadPattern(symbol, moduleRow, 34, 17),
                Is.EqualTo(Pdf417CodewordTable.GetPattern(clusterIndex, codewords[codewordOffset])));
            Assert.That(
                ReadPattern(symbol, moduleRow, 51, 17),
                Is.EqualTo(Pdf417CodewordTable.GetPattern(clusterIndex, codewords[codewordOffset + 1])));
            Assert.That(
                ReadPattern(symbol, moduleRow, 68, 17),
                Is.EqualTo(Pdf417CodewordTable.GetPattern(clusterIndex, rightIndicator)));
            Assert.That(
                ReadPattern(symbol, moduleRow, 85, 18),
                Is.EqualTo(Pdf417CodewordTable.StopPatternBits));
        });

        AssertRepeatedRows(symbol, plan.Rows);
    }

    static void AssertRepeatedRows(BarcodeSymbol symbol, int rowCount)
    {
        byte[] modules = symbol.Modules!;
        int rowNumber = 0;
        while (rowNumber < rowCount)
        {
            int firstRow = rowNumber * Pdf417Layout.DefaultRowHeightModules;
            int repeat = 1;
            while (repeat < Pdf417Layout.DefaultRowHeightModules)
            {
                int column = 0;
                while (column < symbol.WidthModules)
                {
                    Assert.That(
                        modules[((firstRow + repeat) * symbol.WidthModules) + column],
                        Is.EqualTo(modules[(firstRow * symbol.WidthModules) + column]));
                    column = column + 1;
                }

                repeat = repeat + 1;
            }

            rowNumber = rowNumber + 1;
        }
    }

    static int ReadPattern(
        BarcodeSymbol symbol,
        int rowNumber,
        int columnOffset,
        int moduleCount)
    {
        byte[] modules = symbol.Modules!;
        int pattern = 0;
        int index = 0;
        while (index < moduleCount)
        {
            pattern = (pattern << 1)
                | modules[(rowNumber * symbol.WidthModules) + columnOffset + index];
            index = index + 1;
        }

        return pattern;
    }
}
