namespace barcode;

internal class DataMatrixPureDecodeResult
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal DataMatrixSymbolInfo SymbolInfo = new DataMatrixSymbolInfo();
    internal ModuleMatrix SymbolMatrix = new ModuleMatrix(1, 1);
    internal ModuleMatrix MappingMatrix = new ModuleMatrix(1, 1);
    internal int[] ReceivedCodewords = new int[0];
    internal int[] CorrectedCodewords = new int[0];
    internal int[] DataCodewords = new int[0];
}

internal class DataMatrixCorrectionSummary
{
    internal bool Success;
    internal DataMatrixInterleavedBlock[] Blocks = new DataMatrixInterleavedBlock[0];
    internal int CorrectedErrorCount;
    internal int ErasureCount;
}

internal static class DataMatrixPureDecoder
{
    internal static DataMatrixPureDecodeResult Decode(
        ModuleMatrix modules,
        BarcodeDecodeOptions? options)
    {
        DataMatrixPureDecodeResult? result = TryDecode(modules, options);
        if (result == null)
        {
            throw new BarcodeDecodingException("The pure module matrix is not a decodable Data Matrix ECC 200 symbol.");
        }

        return result;
    }

    internal static DataMatrixPureDecodeResult? TryDecode(
        ModuleMatrix modules,
        BarcodeDecodeOptions? options)
    {
        if (modules == null)
        {
            throw new BarcodeDecodingException("Data Matrix pure modules must not be null.");
        }

        BarcodeDecodeOptions settings = options == null
            ? new BarcodeDecodeOptions()
            : options;
        ValidateOptions(settings);
        BarcodePolarity[] polarities = CreatePolarityAttempts(settings.Polarity);
        BarcodeMirrorMode[] mirrors = CreateMirrorAttempts(settings.MirrorMode);
        BarcodeRotation[] rotations = CreateRotationAttempts(settings.Rotations);
        int polarityIndex = 0;
        while (polarityIndex < polarities.Length)
        {
            ModuleMatrix polarityMatrix = polarities[polarityIndex] == BarcodePolarity.LightOnDark
                ? InvertModules(modules)
                : CopyMatrix(modules);
            int mirrorIndex = 0;
            while (mirrorIndex < mirrors.Length)
            {
                int rotationIndex = 0;
                while (rotationIndex < rotations.Length)
                {
                    BarcodeRotation rotation = rotations[rotationIndex];
                    ModuleMatrix normalized = NormalizeModules(
                        polarityMatrix,
                        mirrors[mirrorIndex],
                        rotation);
                    DataMatrixSymbolInfo? symbol = DataMatrixSymbolInfoTable.FindSymbolByDimensions(
                        normalized.Height,
                        normalized.Width);
                    if (symbol != null)
                    {
                        ModuleMatrix? mapping = ExtractMappingMatrix(normalized, symbol);
                        if (mapping != null)
                        {
                            int[]? received = ExtractReceivedCodewords(mapping, symbol);
                            if (received != null)
                            {
                                DataMatrixCorrectionSummary correction = CorrectBlocks(received, symbol);
                                if (correction.Success)
                                {
                                    int[] dataCodewords = DataMatrixBlockInterleaver.ExtractDataCodewords(
                                        correction.Blocks,
                                        symbol);
                                    DataMatrixHighLevelDecodeResult decoded = DataMatrixHighLevelDecoder.Decode(
                                        dataCodewords);
                                    BarcodeFormat format = decoded.IsGs1
                                        ? BarcodeFormat.Gs1DataMatrix
                                        : BarcodeFormat.DataMatrix;
                                    if (IsAllowedFormat(format, settings.AllowedFormats)
                                        && IsAllowedLength(decoded.RawBytes.Length, settings.AllowedLengths)
                                        && (!decoded.IsGs1
                                            || Gs1DataMatrixCodec.IsValidElementString(decoded.RawBytes)))
                                    {
                                        return CreateDecodeResult(
                                            decoded,
                                            received,
                                            correction,
                                            dataCodewords,
                                            normalized,
                                            mapping,
                                            symbol,
                                            format,
                                            polarities[polarityIndex],
                                            mirrors[mirrorIndex],
                                            rotation);
                                    }
                                }
                            }
                        }
                    }

                    rotationIndex = rotationIndex + 1;
                }

                mirrorIndex = mirrorIndex + 1;
            }

            polarityIndex = polarityIndex + 1;
        }

        return null;
    }

    static ModuleMatrix NormalizeModules(
        ModuleMatrix modules,
        BarcodeMirrorMode mirror,
        BarcodeRotation rotation)
    {
        ModuleMatrix rotated;
        switch (rotation)
        {
            case BarcodeRotation.None:
                rotated = CopyMatrix(modules);
                break;
            case BarcodeRotation.Clockwise90:
                rotated = modules.RotateClockwise270();
                break;
            case BarcodeRotation.Clockwise180:
                rotated = modules.Rotate180();
                break;
            default:
                rotated = modules.RotateClockwise90();
                break;
        }

        if (mirror == BarcodeMirrorMode.Mirrored)
        {
            return rotated.ReflectHorizontal();
        }

        return rotated;
    }

    static ModuleMatrix? ExtractMappingMatrix(
        ModuleMatrix symbolMatrix,
        DataMatrixSymbolInfo symbol)
    {
        if (symbolMatrix.Width != symbol.SymbolColumns
            || symbolMatrix.Height != symbol.SymbolRows)
        {
            return null;
        }

        ModuleMatrix mapping = new ModuleMatrix(
            symbol.MappingMatrixColumns,
            symbol.MappingMatrixRows);
        int regionRow = 0;
        while (regionRow < symbol.VerticalDataRegionCount)
        {
            int symbolTop = regionRow * (symbol.DataRegionRows + 2);
            int regionColumn = 0;
            while (regionColumn < symbol.HorizontalDataRegionCount)
            {
                int symbolLeft = regionColumn * (symbol.DataRegionColumns + 2);
                if (!HasValidRegionBorders(symbolMatrix, symbol, symbolLeft, symbolTop))
                {
                    return null;
                }

                int localRow = 0;
                while (localRow < symbol.DataRegionRows)
                {
                    int localColumn = 0;
                    while (localColumn < symbol.DataRegionColumns)
                    {
                        int mappingColumn = (regionColumn * symbol.DataRegionColumns) + localColumn;
                        int mappingRow = (regionRow * symbol.DataRegionRows) + localRow;
                        mapping.SetModule(
                            mappingColumn,
                            mappingRow,
                            symbolMatrix.GetModule(
                                symbolLeft + 1 + localColumn,
                                symbolTop + 1 + localRow));
                        localColumn = localColumn + 1;
                    }

                    localRow = localRow + 1;
                }

                regionColumn = regionColumn + 1;
            }

            regionRow = regionRow + 1;
        }

        return mapping;
    }

    static bool HasValidRegionBorders(
        ModuleMatrix matrix,
        DataMatrixSymbolInfo symbol,
        int left,
        int top)
    {
        int regionWidth = symbol.DataRegionColumns + 2;
        int regionHeight = symbol.DataRegionRows + 2;
        int column = 0;
        while (column < regionWidth)
        {
            bool expectedTop = (left + column) % 2 == 0;
            if (matrix.GetModule(left + column, top) != expectedTop
                || !matrix.GetModule(left + column, top + regionHeight - 1))
            {
                return false;
            }

            column = column + 1;
        }

        int row = 1;
        while (row < regionHeight - 1)
        {
            int mappingRow = ((top / regionHeight) * symbol.DataRegionRows) + row - 1;
            if (!matrix.GetModule(left, top + row)
                || matrix.GetModule(left + regionWidth - 1, top + row) != (mappingRow % 2 == 0))
            {
                return false;
            }

            row = row + 1;
        }

        return true;
    }

    static int[]? ExtractReceivedCodewords(
        ModuleMatrix mapping,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixPlacementMap placement = DataMatrixPlacement.CreatePlacementMap(symbol);
        int[] codewords = new int[placement.CodewordCount];
        int row = 0;
        while (row < placement.Rows)
        {
            int column = 0;
            while (column < placement.Columns)
            {
                int assignment = placement.Assignments[(row * placement.Columns) + column];
                bool value = mapping.GetModule(column, row);
                if (assignment == DataMatrixPlacementMap.FixedDarkAssignment)
                {
                    if (!value)
                    {
                        return null;
                    }
                }
                else if (assignment == DataMatrixPlacementMap.FixedLightAssignment)
                {
                    if (value)
                    {
                        return null;
                    }
                }
                else if (value)
                {
                    int codewordIndex = assignment / 8;
                    int bitIndex = assignment % 8;
                    codewords[codewordIndex] = codewords[codewordIndex]
                        | (1 << (7 - bitIndex));
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return codewords;
    }

    static DataMatrixCorrectionSummary CorrectBlocks(
        int[] received,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            received,
            symbol);
        DataMatrixCorrectionSummary summary = new DataMatrixCorrectionSummary();
        summary.Blocks = blocks;
        summary.Success = true;
        int blockIndex = 0;
        while (blockIndex < blocks.Length)
        {
            DataMatrixInterleavedBlock block = blocks[blockIndex];
            DataMatrixCorrectionResult corrected = DataMatrixReedSolomon.CorrectCodewords(
                block.Codewords,
                block.ErrorCodewordCount,
                null);
            if (!corrected.Success)
            {
                summary.Success = false;
                return summary;
            }

            block.Codewords = corrected.CorrectedCodewords;
            summary.CorrectedErrorCount = summary.CorrectedErrorCount
                + corrected.CorrectedErrorCount;
            summary.ErasureCount = summary.ErasureCount + corrected.ErasureCount;
            blockIndex = blockIndex + 1;
        }

        return summary;
    }

    static DataMatrixPureDecodeResult CreateDecodeResult(
        DataMatrixHighLevelDecodeResult decoded,
        int[] received,
        DataMatrixCorrectionSummary correction,
        int[] dataCodewords,
        ModuleMatrix symbolMatrix,
        ModuleMatrix mappingMatrix,
        DataMatrixSymbolInfo symbol,
        BarcodeFormat format,
        BarcodePolarity polarity,
        BarcodeMirrorMode mirror,
        BarcodeRotation rotation)
    {
        int[] correctedCodewords = DataMatrixBlockInterleaver.InterleaveBlocks(
            correction.Blocks,
            symbol);
        BarcodePayload payload = new BarcodePayload();
        payload.Text = decoded.Text;
        payload.RawBytes = CopyBytes(decoded.RawBytes);
        payload.CharacterEncoding = decoded.CharacterEncoding;
        payload.EciSegments = CopyEciSegments(decoded.EciSegments);
        payload.IsGs1 = decoded.IsGs1;
        payload.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(decoded.StructuredAppend);
        if (payload.StructuredAppend != null)
        {
            payload.StructuredAppend.Format = format;
        }

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.ErrorCorrectionApplied = correction.CorrectedErrorCount > 0;
        metadata.CorrectedErrorCount = correction.CorrectedErrorCount;
        metadata.ErasureCount = correction.ErasureCount;
        metadata.SymbologyIdentifier = decoded.IsGs1
            ? BarcodeSymbologyIdentifiers.Gs1DataMatrix
            : BarcodeSymbologyIdentifiers.DataMatrix;
        metadata.Codewords = CopyIntegers(correctedCodewords);
        metadata.Confidence = CalculateConfidence(
            correction.CorrectedErrorCount,
            symbol.ErrorCodewordCount);
        metadata.IsGs1 = decoded.IsGs1;
        if (decoded.IsGs1)
        {
            metadata.Gs1ElementString =
                Gs1DataMatrixCodec.ConvertBytesToString(decoded.RawBytes);
        }

        metadata.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(payload.StructuredAppend);

        BarcodeDecodeResult publicResult = new BarcodeDecodeResult();
        publicResult.Payload = payload;
        publicResult.Format = format;
        publicResult.Dimension = BarcodeDimension.Matrix;
        publicResult.Rotation = rotation;
        publicResult.OrientationDegrees = GetOrientationDegrees(rotation);
        publicResult.Polarity = polarity;
        publicResult.MirrorMode = mirror;
        publicResult.Metadata = metadata;
        publicResult.Diagnostics = new BarcodeDiagnostic[0];

        DataMatrixPureDecodeResult result = new DataMatrixPureDecodeResult();
        result.Result = publicResult;
        result.SymbolInfo = symbol.CopySymbolInfo();
        result.SymbolMatrix = CopyMatrix(symbolMatrix);
        result.MappingMatrix = CopyMatrix(mappingMatrix);
        result.ReceivedCodewords = CopyIntegers(received);
        result.CorrectedCodewords = CopyIntegers(correctedCodewords);
        result.DataCodewords = CopyIntegers(dataCodewords);
        return result;
    }

    static BarcodePolarity[] CreatePolarityAttempts(BarcodePolarity polarity)
    {
        if (polarity == BarcodePolarity.Automatic)
        {
            return new BarcodePolarity[]
            {
                BarcodePolarity.DarkOnLight,
                BarcodePolarity.LightOnDark
            };
        }

        return new BarcodePolarity[] { polarity };
    }

    static BarcodeMirrorMode[] CreateMirrorAttempts(BarcodeMirrorMode mirror)
    {
        if (mirror == BarcodeMirrorMode.Both)
        {
            return new BarcodeMirrorMode[]
            {
                BarcodeMirrorMode.Normal,
                BarcodeMirrorMode.Mirrored
            };
        }

        return new BarcodeMirrorMode[] { mirror };
    }

    static BarcodeRotation[] CreateRotationAttempts(BarcodeRotation[]? rotations)
    {
        if (rotations == null)
        {
            return new BarcodeRotation[]
            {
                BarcodeRotation.None,
                BarcodeRotation.Clockwise90,
                BarcodeRotation.Clockwise180,
                BarcodeRotation.Clockwise270
            };
        }

        BarcodeRotation[] copy = new BarcodeRotation[rotations.Length];
        int index = 0;
        while (index < rotations.Length)
        {
            copy[index] = rotations[index];
            index = index + 1;
        }

        return copy;
    }

    static ModuleMatrix InvertModules(ModuleMatrix source)
    {
        ModuleMatrix inverted = new ModuleMatrix(source.Width, source.Height);
        int row = 0;
        while (row < source.Height)
        {
            int column = 0;
            while (column < source.Width)
            {
                inverted.SetModule(column, row, !source.GetModule(column, row));
                column = column + 1;
            }

            row = row + 1;
        }

        return inverted;
    }

    static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }

    static int[] CopyIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static EciSegment[] CopyEciSegments(EciSegment[] source)
    {
        EciSegment[] copy = new EciSegment[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            EciSegment segment = new EciSegment();
            segment.AssignmentNumber = source[index].AssignmentNumber;
            segment.ByteOffset = source[index].ByteOffset;
            segment.ByteCount = source[index].ByteCount;
            copy[index] = segment;
            index = index + 1;
        }

        return copy;
    }

    static bool IsAllowedFormat(BarcodeFormat format, BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null)
        {
            return true;
        }

        int index = 0;
        while (index < allowedFormats.Length)
        {
            if (allowedFormats[index] == BarcodeFormat.Automatic
                || allowedFormats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsAllowedLength(int length, int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return true;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static double CalculateConfidence(int correctedErrorCount, int errorCodewordCount)
    {
        if (correctedErrorCount <= 0)
        {
            return 1.0;
        }

        double confidence = 1.0
            - ((double)correctedErrorCount / (double)(errorCodewordCount + 1));
        return confidence < 0.0 ? 0.0 : confidence;
    }

    static double GetOrientationDegrees(BarcodeRotation rotation)
    {
        switch (rotation)
        {
            case BarcodeRotation.None:
                return 0.0;
            case BarcodeRotation.Clockwise90:
                return 90.0;
            case BarcodeRotation.Clockwise180:
                return 180.0;
            default:
                return 270.0;
        }
    }

    static void ValidateOptions(BarcodeDecodeOptions options)
    {
        if (options.Polarity != BarcodePolarity.DarkOnLight
            && options.Polarity != BarcodePolarity.LightOnDark
            && options.Polarity != BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException("Data Matrix decode polarity is invalid.");
        }

        if (options.MirrorMode != BarcodeMirrorMode.Normal
            && options.MirrorMode != BarcodeMirrorMode.Mirrored
            && options.MirrorMode != BarcodeMirrorMode.Both)
        {
            throw new BarcodeValidationException("Data Matrix decode mirror mode is invalid.");
        }

        if (options.Rotations != null)
        {
            if (options.Rotations.Length == 0)
            {
                throw new BarcodeValidationException("Data Matrix decode rotations must not be empty.");
            }

            int rotationIndex = 0;
            while (rotationIndex < options.Rotations.Length)
            {
                BarcodeRotation rotation = options.Rotations[rotationIndex];
                if (rotation != BarcodeRotation.None
                    && rotation != BarcodeRotation.Clockwise90
                    && rotation != BarcodeRotation.Clockwise180
                    && rotation != BarcodeRotation.Clockwise270)
                {
                    throw new BarcodeValidationException("Data Matrix decode rotation is invalid.");
                }

                rotationIndex = rotationIndex + 1;
            }
        }

        if (options.AllowedLengths != null)
        {
            int lengthIndex = 0;
            while (lengthIndex < options.AllowedLengths.Length)
            {
                if (options.AllowedLengths[lengthIndex] < 0)
                {
                    throw new BarcodeValidationException("Data Matrix allowed payload lengths must not be negative.");
                }

                lengthIndex = lengthIndex + 1;
            }
        }
    }
}
