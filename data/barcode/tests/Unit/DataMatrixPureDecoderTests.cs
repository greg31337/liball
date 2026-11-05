namespace barcode.Tests;

public class DataMatrixPureDecoderTests
{
    [Test]
    public void EveryStandardSymbolDimensionIsInferredAndRoundTrips()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);

            DataMatrixPureDecodeResult decoded = DataMatrixPureDecoder.Decode(
                encoded.SymbolMatrix,
                null);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.SymbolInfo.Version, Is.EqualTo(version), $"Version {version}");
                Assert.That(decoded.Result.Payload!.RawBytes, Is.EqualTo(data), $"Version {version}");
                Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.DataMatrix), $"Version {version}");
                Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Matrix), $"Version {version}");
                Assert.That(decoded.Result.Metadata!.CorrectedErrorCount, Is.Zero, $"Version {version}");
                Assert.That(decoded.Result.Metadata.Codewords, Is.EqualTo(encoded.SymbolCodewords), $"Version {version}");
                Assert.That(decoded.MappingMatrix.CopyModules(), Is.EqualTo(encoded.MappingMatrix.CopyModules()), $"Version {version}");
            });
            version++;
        }
    }

    [Test]
    public void RightAngleRotationsOfRectangularSymbolsRecoverOrientation()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("ROTATE");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(28);
        DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);
        ModuleMatrix[] matrices = new ModuleMatrix[]
        {
            TestModuleTransforms.Rotate(
                encoded.SymbolMatrix,
                BarcodeRotation.None),
            TestModuleTransforms.Rotate(
                encoded.SymbolMatrix,
                BarcodeRotation.Clockwise90),
            TestModuleTransforms.Rotate(
                encoded.SymbolMatrix,
                BarcodeRotation.Clockwise180),
            TestModuleTransforms.Rotate(
                encoded.SymbolMatrix,
                BarcodeRotation.Clockwise270)
        };
        BarcodeRotation[] rotations = new BarcodeRotation[]
        {
            BarcodeRotation.None,
            BarcodeRotation.Clockwise90,
            BarcodeRotation.Clockwise180,
            BarcodeRotation.Clockwise270
        };
        double[] degrees = new double[] { 0.0, 90.0, 180.0, 270.0 };

        int index = 0;
        while (index < matrices.Length)
        {
            DataMatrixPureDecodeResult decoded = DataMatrixPureDecoder.Decode(matrices[index], null);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Result.Payload!.RawBytes, Is.EqualTo(data));
                Assert.That(decoded.Result.Rotation, Is.EqualTo(rotations[index]));
                Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(degrees[index]));
                Assert.That(decoded.SymbolInfo.SymbolRows, Is.EqualTo(12));
                Assert.That(decoded.SymbolInfo.SymbolColumns, Is.EqualTo(36));
            });
            index++;
        }
    }

    [Test]
    public void ReflectionAndInvertedPolarityAreRecoveredTogether()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("MIRROR");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(29);
        DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);
        ModuleMatrix observed = TestModuleTransforms.Invert(
            TestModuleTransforms.Rotate(
                TestModuleTransforms.ReflectHorizontal(encoded.SymbolMatrix),
                BarcodeRotation.Clockwise90));
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;

        DataMatrixPureDecodeResult decoded = DataMatrixPureDecoder.Decode(observed, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise90));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(90.0));
            Assert.That(decoded.Result.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(decoded.Result.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Mirrored));
            Assert.That(decoded.SymbolMatrix.CopyModules(), Is.EqualTo(encoded.SymbolMatrix.CopyModules()));
        });
    }

    [Test]
    public void ReedSolomonCorrectsErrorsIndependentlyAcrossInterleavedBlocks()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("CORRECT BOTH BLOCKS");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(15);
        DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);
        DataMatrixInterleavedBlock[] damagedBlocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            encoded.SymbolCodewords,
            symbol);
        damagedBlocks[0].Codewords[0] = damagedBlocks[0].Codewords[0] ^ 0x55;
        damagedBlocks[1].Codewords[0] = damagedBlocks[1].Codewords[0] ^ 0xaa;
        int[] damagedCodewords = DataMatrixBlockInterleaver.InterleaveBlocks(damagedBlocks, symbol);
        ModuleMatrix damagedMapping = DataMatrixPlacement.PlaceCodewords(damagedCodewords, symbol);
        ModuleMatrix damagedSymbol = DataMatrixEncoder.CreateSymbolMatrix(damagedMapping, symbol);

        DataMatrixPureDecodeResult decoded = DataMatrixPureDecoder.Decode(damagedSymbol, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.ReceivedCodewords, Is.EqualTo(damagedCodewords));
            Assert.That(decoded.CorrectedCodewords, Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.Result.Metadata!.ErrorCorrectionApplied, Is.True);
            Assert.That(decoded.Result.Metadata.CorrectedErrorCount, Is.EqualTo(2));
            Assert.That(decoded.Result.Metadata.ErasureCount, Is.Zero);
            Assert.That(decoded.Result.Metadata.Codewords, Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.Result.Metadata.Confidence, Is.LessThan(1.0));
        });
    }

    [Test]
    public void DamageBeyondCorrectionCapacityAndStructuralDamageAreRejected()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(1);
        DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            encoded.SymbolCodewords,
            symbol);
        blocks[0].Codewords[0] = blocks[0].Codewords[0] ^ 1;
        blocks[0].Codewords[1] = blocks[0].Codewords[1] ^ 2;
        blocks[0].Codewords[2] = blocks[0].Codewords[2] ^ 4;
        int[] excessiveErrors = DataMatrixBlockInterleaver.InterleaveBlocks(blocks, symbol);
        ModuleMatrix excessiveMatrix = DataMatrixEncoder.CreateSymbolMatrix(
            DataMatrixPlacement.PlaceCodewords(excessiveErrors, symbol),
            symbol);
        ModuleMatrix finderDamage = BarcodeTestBuilder.CopyMatrix(
            encoded.SymbolMatrix);
        finderDamage.SetModule(0, 0, !finderDamage.GetModule(0, 0));
        DataMatrixSymbolInfo fixedSymbol = FindSymbolWithFixedModule();
        DataMatrixEncodingResult fixedEncoding = EncodeAtSize(data, fixedSymbol, false);
        ModuleMatrix fixedDamage = BarcodeTestBuilder.CopyMatrix(
            fixedEncoding.SymbolMatrix);
        int[] fixedCoordinate = FindFixedSymbolCoordinate(fixedSymbol);
        fixedDamage.SetModule(
            fixedCoordinate[0],
            fixedCoordinate[1],
            !fixedDamage.GetModule(fixedCoordinate[0], fixedCoordinate[1]));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixPureDecoder.Decode(excessiveMatrix, null),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => DataMatrixPureDecoder.Decode(finderDamage, null),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => DataMatrixPureDecoder.Decode(fixedDamage, null),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void TransformFormatAndLengthOptionsLimitDecodeAttempts()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("FILTER");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(27);
        DataMatrixEncodingResult encoded = EncodeAtSize(data, symbol, false);
        ModuleMatrix rotated = encoded.SymbolMatrix.RotateClockwise90();
        ModuleMatrix mirrored = encoded.SymbolMatrix.ReflectHorizontal();
        ModuleMatrix inverted = TestModuleTransforms.Invert(
            encoded.SymbolMatrix);
        BarcodeDecodeOptions noRotation = new BarcodeDecodeOptions();
        noRotation.Rotations = new BarcodeRotation[] { BarcodeRotation.None };
        BarcodeDecodeOptions noMirror = new BarcodeDecodeOptions();
        noMirror.MirrorMode = BarcodeMirrorMode.Normal;
        BarcodeDecodeOptions noInversion = new BarcodeDecodeOptions();
        noInversion.Polarity = BarcodePolarity.DarkOnLight;
        BarcodeDecodeOptions wrongFormat = new BarcodeDecodeOptions();
        wrongFormat.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.Gs1DataMatrix };
        BarcodeDecodeOptions wrongLength = new BarcodeDecodeOptions();
        wrongLength.AllowedLengths = new int[] { data.Length + 1 };

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixPureDecoder.Decode(rotated, noRotation), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(mirrored, noMirror), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(inverted, noInversion), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(encoded.SymbolMatrix, wrongFormat), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(encoded.SymbolMatrix, wrongLength), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void Gs1AndStructuredPayloadMetadataAreReconstructedAndOwned()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("0101234567890128");
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(10);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.Gs1DataMatrix;
        append.Index = 1;
        append.Count = 3;
        append.FileIdentifier = "001002";
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.StructuredAppend = append;
        DataMatrixEncodingResult encoded = DataMatrixEncoder.Encode(
            data,
            CreatePayload(data),
            options,
            common,
            true);
        BarcodeDecodeOptions decodeOptions = new BarcodeDecodeOptions();
        decodeOptions.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.Gs1DataMatrix };

        DataMatrixPureDecodeResult decoded = DataMatrixPureDecoder.Decode(
            encoded.SymbolMatrix,
            decodeOptions);
        encoded.SymbolMatrix.SetModule(0, 0, false);
        decoded.CorrectedCodewords[0] = decoded.CorrectedCodewords[0] ^ 1;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(decoded.Result.Payload!.IsGs1, Is.True);
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Result.Payload.StructuredAppend!.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(decoded.Result.Payload.StructuredAppend.Index, Is.EqualTo(1));
            Assert.That(decoded.Result.Metadata!.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]d2"));
            Assert.That(decoded.Result.Metadata.StructuredAppend!.Index, Is.EqualTo(1));
            Assert.That(decoded.Result.Metadata.Codewords![0], Is.EqualTo(encoded.SymbolCodewords[0]));
            Assert.That(decoded.SymbolMatrix.GetModule(0, 0), Is.True);
        });
    }

    [Test]
    public void InvalidDimensionsAndDecodeControlsAreRejected()
    {
        ModuleMatrix invalidSize = new ModuleMatrix(9, 9);
        BarcodeDecodeOptions emptyRotations = new BarcodeDecodeOptions();
        emptyRotations.Rotations = new BarcodeRotation[0];
        BarcodeDecodeOptions invalidPolarity = new BarcodeDecodeOptions();
        invalidPolarity.Polarity = (BarcodePolarity)99;
        BarcodeDecodeOptions invalidMirror = new BarcodeDecodeOptions();
        invalidMirror.MirrorMode = (BarcodeMirrorMode)99;
        BarcodeDecodeOptions invalidRotation = new BarcodeDecodeOptions();
        invalidRotation.Rotations = new BarcodeRotation[] { (BarcodeRotation)99 };
        BarcodeDecodeOptions invalidLength = new BarcodeDecodeOptions();
        invalidLength.AllowedLengths = new int[] { -1 };

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixPureDecoder.TryDecode(invalidSize, null), Is.Null);
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, null), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, emptyRotations), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, invalidPolarity), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, invalidMirror), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, invalidRotation), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixPureDecoder.Decode(invalidSize, invalidLength), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static DataMatrixEncodingResult EncodeAtSize(
        byte[] data,
        DataMatrixSymbolInfo symbol,
        bool isGs1)
    {
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        return DataMatrixEncoder.Encode(data, CreatePayload(data), options, null, isGs1);
    }

    static BarcodePayload CreatePayload(byte[] data)
    {
        string text = global::System.Text.Encoding.ASCII.GetString(data);
        return BarcodeTestBuilder.CreatePayload(
            text,
            data,
            "ASCII",
            false);
    }

    static int[] FindFixedSymbolCoordinate(DataMatrixSymbolInfo symbol)
    {
        DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                if (map.IsFixed(column, row))
                {
                    int regionColumn = column / symbol.DataRegionColumns;
                    int regionRow = row / symbol.DataRegionRows;
                    int symbolColumn = (regionColumn * (symbol.DataRegionColumns + 2))
                        + 1
                        + (column % symbol.DataRegionColumns);
                    int symbolRow = (regionRow * (symbol.DataRegionRows + 2))
                        + 1
                        + (row % symbol.DataRegionRows);
                    return new int[] { symbolColumn, symbolRow };
                }

                column++;
            }

            row++;
        }

        throw new global::System.InvalidOperationException("The selected Data Matrix version has no fixed module.");
    }

    static DataMatrixSymbolInfo FindSymbolWithFixedModule()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
            int index = 0;
            while (index < map.Assignments.Length)
            {
                if (map.Assignments[index] < 0)
                {
                    return symbol;
                }

                index++;
            }

            version++;
        }

        throw new global::System.InvalidOperationException("No Data Matrix version has a fixed module.");
    }

}
