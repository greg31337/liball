namespace barcode.Tests;

public class DataMatrixEncoderTests
{
    [Test]
    public void CompletePipelineEncodesTheSmallestSymbolAndRoundTripsItsPayload()
    {
        byte[] data = new byte[] { 65 };
        BarcodePayload payload = CreatePayload(data, "A");

        DataMatrixEncodingResult result = DataMatrixEncoder.Encode(
            data,
            payload,
            null,
            null,
            false);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            result.SymbolCodewords,
            result.SymbolInfo);
        int[] recoveredDataCodewords = DataMatrixBlockInterleaver.ExtractDataCodewords(
            blocks,
            result.SymbolInfo);
        DataMatrixHighLevelDecodeResult decoded = DataMatrixHighLevelDecoder.Decode(
            recoveredDataCodewords);

        Assert.Multiple(() =>
        {
            Assert.That(result.SymbolInfo.Version, Is.EqualTo(1));
            Assert.That(result.MessagePlan.Codewords, Is.EqualTo(new int[] { 66 }));
            Assert.That(result.DataCodewords, Has.Length.EqualTo(3));
            Assert.That(result.SymbolCodewords, Has.Length.EqualTo(8));
            Assert.That(result.MappingMatrix.Width, Is.EqualTo(8));
            Assert.That(result.MappingMatrix.Height, Is.EqualTo(8));
            Assert.That(result.SymbolMatrix.Width, Is.EqualTo(10));
            Assert.That(result.SymbolMatrix.Height, Is.EqualTo(10));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Matrix));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(10));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(100));
            Assert.That(result.Symbol.Codewords, Is.EqualTo(result.SymbolCodewords));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]d1"));
            Assert.That(result.Symbol.HasChecksum, Is.False);
            Assert.That(decoded.RawBytes, Is.EqualTo(data));
        });
    }

    [Test]
    public void EveryStandardSizeWrapsMappingModulesInExactFinderRegions()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            int[] codewords = CreateCodewords(symbol.TotalCodewordCount, version);
            ModuleMatrix mapping = DataMatrixPlacement.PlaceCodewords(codewords, symbol);
            ModuleMatrix matrix = DataMatrixEncoder.CreateSymbolMatrix(mapping, symbol);

            Assert.Multiple(() =>
            {
                Assert.That(matrix.Width, Is.EqualTo(symbol.SymbolColumns), $"Version {version}");
                Assert.That(matrix.Height, Is.EqualTo(symbol.SymbolRows), $"Version {version}");
            });
            AssertFinderRegions(matrix, mapping, symbol, version);
            version++;
        }
    }

    [Test]
    public void ShapeExactDimensionsAndTargetAspectSelectDeterministically()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        BarcodePayload payload = CreatePayload(data, "A");
        DataMatrixOptions rectangular = new DataMatrixOptions();
        rectangular.Shape = DataMatrixShape.StandardRectangular;
        DataMatrixOptions exact = new DataMatrixOptions();
        exact.Rows = 8;
        exact.Columns = 32;
        BarcodeSizing aspectSizing = new BarcodeSizing();
        aspectSizing.TargetAspectRatio = 4.0;
        BarcodeEncodeOptions aspect = new BarcodeEncodeOptions();
        aspect.Sizing = aspectSizing;

        DataMatrixEncodingResult rectangularResult = DataMatrixEncoder.Encode(
            data,
            payload,
            rectangular,
            null,
            false);
        DataMatrixEncodingResult exactResult = DataMatrixEncoder.Encode(
            data,
            payload,
            exact,
            null,
            false);
        DataMatrixEncodingResult aspectResult = DataMatrixEncoder.Encode(
            data,
            payload,
            null,
            aspect,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(rectangularResult.SymbolInfo.Version, Is.EqualTo(25));
            Assert.That(rectangularResult.SymbolInfo.IsRectangular, Is.True);
            Assert.That(exactResult.SymbolInfo.Version, Is.EqualTo(26));
            Assert.That(exactResult.Symbol.WidthModules, Is.EqualTo(32));
            Assert.That(exactResult.Symbol.HeightModules, Is.EqualTo(8));
            Assert.That(aspectResult.SymbolInfo.Version, Is.EqualTo(26));
        });
    }

    [Test]
    public void LogicalSizingConstraintsCanSelectOneDimensionAndRejectConflicts()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        BarcodePayload payload = CreatePayload(data, "A");
        BarcodeSizing width = new BarcodeSizing();
        width.RequestedWidthModules = 32;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.Sizing = width;
        DataMatrixOptions conflict = new DataMatrixOptions();
        conflict.Columns = 18;

        DataMatrixEncodingResult selected = DataMatrixEncoder.Encode(
            data,
            payload,
            null,
            common,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(selected.SymbolInfo.Version, Is.EqualTo(26));
            Assert.That(selected.Symbol.WidthModules, Is.EqualTo(32));
            Assert.That(
                () => DataMatrixEncoder.Encode(data, payload, conflict, common, false),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void StandardQuietZoneIsReportedOnceOutsideTheLogicalMatrix()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        DataMatrixEncodingResult result = DataMatrixEncoder.Encode(
            data,
            CreatePayload(data, "A"),
            null,
            null,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(1));
            Assert.That(result.Symbol.RequiredQuietZone.TopModules, Is.EqualTo(1));
            Assert.That(result.Symbol.RequiredQuietZone.RightModules, Is.EqualTo(1));
            Assert.That(result.Symbol.RequiredQuietZone.BottomModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(12));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(12));
            Assert.That(result.Symbol.Size.QuietZone!.LeftModules, Is.EqualTo(1));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(10 * 10));
        });
    }

    [Test]
    public void CustomQuietZoneIntegralPixelsPhysicalSizeAndHrtShareOneLayout()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        BarcodeQuietZone quietZone = CreateQuietZone(2, 3, 4, 5);
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthPixels = 48;
        sizing.RequestedHeightPixels = 54;
        sizing.DotsPerInch = 254.0;
        sizing.ModuleSizeMillimetres = 0.3;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = quietZone;
        options.Sizing = sizing;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "label";

        DataMatrixEncodingResult result = DataMatrixEncoder.Encode(
            data,
            CreatePayload(data, "A"),
            null,
            options,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(10));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(10));
            Assert.That(result.Symbol.Size!.WidthModules, Is.EqualTo(16));
            Assert.That(result.Symbol.Size.HeightModules, Is.EqualTo(18));
            Assert.That(result.Symbol.Size.WidthPixels, Is.EqualTo(48));
            Assert.That(result.Symbol.Size.HeightPixels, Is.EqualTo(54));
            Assert.That(result.Symbol.Size.ModuleSizeMillimetres, Is.EqualTo(0.3).Within(0.000001));
            Assert.That(result.Symbol.Size.WidthMillimetres, Is.EqualTo(4.8).Within(0.000001));
            Assert.That(result.Symbol.Size.HeightMillimetres, Is.EqualTo(5.4).Within(0.000001));
            Assert.That(result.Symbol.Size.QuietZone!.LeftPixels, Is.EqualTo(6));
            Assert.That(result.Symbol.Size.QuietZone.TopPixels, Is.EqualTo(9));
            Assert.That(result.Symbol.Size.QuietZone.RightPixels, Is.EqualTo(12));
            Assert.That(result.Symbol.Size.QuietZone.BottomPixels, Is.EqualTo(15));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("label"));
            Assert.That(result.Symbol.TextLayout.Runs[0].X, Is.EqualTo(2.0));
            Assert.That(result.Symbol.TextLayout.Runs[0].Width, Is.EqualTo(10.0));
            Assert.That(result.Symbol.TextLayout.Bounds!.Y, Is.EqualTo(18.0));
        });
    }

    [Test]
    public void Gs1EciStructuredAppendAndPayloadMetadataAreOwnedAndComplete()
    {
        byte[] data = global::System.Text.Encoding.UTF8.GetBytes("123456");
        BarcodePayload payload = CreatePayload(data, "123456");
        StructuredAppendInfo append = CreateAppend(1, 3, "254254");
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.EciAssignmentNumber = 26;
        common.CharacterEncoding = "UTF-8";
        common.StructuredAppend = append;
        common.TextMode = BarcodeTextMode.Standard;

        DataMatrixEncodingResult result = DataMatrixEncoder.Encode(
            data,
            payload,
            null,
            common,
            true);
        data[0] = 90;
        append.Index = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]d2"));
            Assert.That(result.Symbol.Payload!.RawBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes("123456")));
            Assert.That(result.Symbol.Payload.Text, Is.EqualTo("123456"));
            Assert.That(result.Symbol.Payload.CharacterEncoding, Is.EqualTo("UTF-8"));
            Assert.That(result.Symbol.Payload.IsGs1, Is.True);
            Assert.That(result.Symbol.Payload.EciSegments, Has.Length.EqualTo(1));
            Assert.That(result.Symbol.Payload.EciSegments![0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(result.Symbol.Payload.EciSegments[0].ByteCount, Is.EqualTo(6));
            Assert.That(result.Symbol.Payload.StructuredAppend!.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(result.Symbol.Payload.StructuredAppend.Index, Is.EqualTo(1));
            Assert.That(result.Symbol.Payload.StructuredAppend.IsLastSegment, Is.False);
            Assert.That(result.MessagePlan.ControlCodewords[0], Is.EqualTo(233));
            Assert.That(result.MessagePlan.ControlCodewords, Does.Contain(232));
            Assert.That(result.MessagePlan.ControlCodewords, Does.Contain(241));
            Assert.That(result.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("123456"));
        });
    }

    [Test]
    public void EncodingResultDoesNotAliasCallerOrSymbolBuffers()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("ABC");
        BarcodePayload payload = CreatePayload(data, "ABC");
        DataMatrixEncodingResult result = DataMatrixEncoder.Encode(
            data,
            payload,
            null,
            null,
            false);
        byte expectedModule = result.Symbol.Modules![0];
        int expectedCodeword = result.Symbol.Codewords![0];
        data[0] = 90;
        payload.RawBytes![0] = 90;
        result.SymbolMatrix.SetModule(0, 0, !result.SymbolMatrix.GetModule(0, 0));
        result.SymbolCodewords[0] = result.SymbolCodewords[0] ^ 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Payload!.RawBytes![0], Is.EqualTo((byte)65));
            Assert.That(result.MessagePlan.DataPlan.Data[0], Is.EqualTo((byte)65));
            Assert.That(result.Symbol.Modules![0], Is.EqualTo(expectedModule));
            Assert.That(result.Symbol.Codewords![0], Is.EqualTo(expectedCodeword));
        });
    }

    [Test]
    public void InvalidSelectionQuietZoneSizingAndTextRequestsAreRejected()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        BarcodePayload payload = CreatePayload(data, "A");
        DataMatrixOptions invalidShape = new DataMatrixOptions();
        invalidShape.Shape = (DataMatrixShape)99;
        DataMatrixOptions missingSize = new DataMatrixOptions();
        missingSize.Rows = 9;
        missingSize.Columns = 9;
        DataMatrixOptions tooSmall = new DataMatrixOptions();
        tooSmall.Rows = 10;
        tooSmall.Columns = 10;
        BarcodeEncodeOptions noCustomQuietZone = new BarcodeEncodeOptions();
        noCustomQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        BarcodeEncodeOptions smallQuietZone = new BarcodeEncodeOptions();
        smallQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.CustomAtLeastStandard;
        smallQuietZone.QuietZone = CreateQuietZone(1, 0, 1, 1);
        BarcodeSizing nonintegralPixels = new BarcodeSizing();
        nonintegralPixels.RequestedWidthPixels = 13;
        BarcodeEncodeOptions nonintegral = new BarcodeEncodeOptions();
        nonintegral.Sizing = nonintegralPixels;
        BarcodeSizing distortionPixels = new BarcodeSizing();
        distortionPixels.RequestedWidthPixels = 24;
        distortionPixels.RequestedHeightPixels = 36;
        BarcodeEncodeOptions distortion = new BarcodeEncodeOptions();
        distortion.Sizing = distortionPixels;
        BarcodeSizing conflictingPhysical = new BarcodeSizing();
        conflictingPhysical.ModuleSizeMillimetres = 0.5;
        conflictingPhysical.RequestedWidthMillimetres = 12.0;
        BarcodeEncodeOptions physical = new BarcodeEncodeOptions();
        physical.Sizing = conflictingPhysical;
        BarcodeSizing barHeight = new BarcodeSizing();
        barHeight.BarHeightMillimetres = 1.0;
        BarcodeEncodeOptions bars = new BarcodeEncodeOptions();
        bars.Sizing = barHeight;
        BarcodeEncodeOptions customText = new BarcodeEncodeOptions();
        customText.TextMode = BarcodeTextMode.Custom;

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixEncoder.Encode(null!, payload, null, null, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, null!, null, null, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(new byte[0], payload, null, null, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, invalidShape, null, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, missingSize, null, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixEncoder.Encode(global::System.Text.Encoding.ASCII.GetBytes("ABCD"), payload, tooSmall, null, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, noCustomQuietZone, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, smallQuietZone, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, nonintegral, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, distortion, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, physical, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, bars, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixEncoder.Encode(data, payload, null, customText, false), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static void AssertFinderRegions(
        ModuleMatrix matrix,
        ModuleMatrix mapping,
        DataMatrixSymbolInfo symbol,
        int version)
    {
        int regionRow = 0;
        while (regionRow < symbol.VerticalDataRegionCount)
        {
            int regionColumn = 0;
            while (regionColumn < symbol.HorizontalDataRegionCount)
            {
                int startX = regionColumn * (symbol.DataRegionColumns + 2);
                int startY = regionRow * (symbol.DataRegionRows + 2);
                int localX = 0;
                while (localX < symbol.DataRegionColumns + 2)
                {
                    Assert.That(
                        matrix.GetModule(startX + localX, startY),
                        Is.EqualTo((startX + localX) % 2 == 0),
                        $"Version {version}, top {startX + localX},{startY}");
                    Assert.That(
                        matrix.GetModule(startX + localX, startY + symbol.DataRegionRows + 1),
                        Is.True,
                        $"Version {version}, bottom {startX + localX}");
                    localX++;
                }

                int localY = 0;
                while (localY < symbol.DataRegionRows)
                {
                    int mappingX = (regionColumn * symbol.DataRegionColumns);
                    int mappingY = (regionRow * symbol.DataRegionRows) + localY;
                    Assert.That(matrix.GetModule(startX, startY + localY + 1), Is.True);
                    Assert.That(
                        matrix.GetModule(startX + symbol.DataRegionColumns + 1, startY + localY + 1),
                        Is.EqualTo(mappingY % 2 == 0));
                    localX = 0;
                    while (localX < symbol.DataRegionColumns)
                    {
                        Assert.That(
                            matrix.GetModule(startX + localX + 1, startY + localY + 1),
                            Is.EqualTo(mapping.GetModule(mappingX + localX, mappingY)),
                            $"Version {version}, data {mappingX + localX},{mappingY}");
                        localX++;
                    }

                    localY++;
                }

                regionColumn++;
            }

            regionRow++;
        }
    }

    static BarcodePayload CreatePayload(byte[] data, string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = CopyBytes(data);
        return payload;
    }

    static StructuredAppendInfo CreateAppend(int index, int count, string fileIdentifier)
    {
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = index;
        append.Count = count;
        append.FileIdentifier = fileIdentifier;
        return append;
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

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        global::System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static int[] CreateCodewords(int count, int seed)
    {
        int[] codewords = new int[count];
        int index = 0;
        while (index < codewords.Length)
        {
            codewords[index] = ((index * 149) + (seed * 31)) & 0xFF;
            index++;
        }

        return codewords;
    }
}
