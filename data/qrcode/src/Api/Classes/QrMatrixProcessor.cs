namespace qrcode;

/// <summary>Provides QR matrix construction, extraction, transformation, classification, and validation.</summary>
public class QrMatrixProcessor : IQrMatrixProcessor
{
    /// <summary>Initializes a stateless matrix processor.</summary>
    public QrMatrixProcessor()
    {
    }

    /// <summary>Builds and classifies a symbol matrix from placement-order codewords.</summary>
    /// <param name="symbol">The symbol metadata and codewords to build; the selected mask and matrix are updated.</param>
    /// <param name="quietZoneModules">The quiet-zone width, or -1 for the profile default.</param>
    /// <returns>The newly allocated rendered module matrix.</returns>
    public QrMatrix BuildMatrix(QrSymbol symbol, int quietZoneModules)
    {
        if (symbol == null)
        {
            throw new QrInputException("The symbol is required.", "symbol");
        }

        if (symbol.Codewords == null)
        {
            throw new QrInputException("The symbol codewords are required.", "Codewords");
        }

        int errorCorrectionIndex = GetErrorCorrectionIndex(
            symbol.SymbolKind,
            symbol.Version,
            symbol.ErrorCorrectionLevel);
        QrMaskPattern selectedMask = SelectMask(
            symbol.Codewords,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            symbol.MaskPattern);
        BitMatrixBuffer bits = BuildBitMatrix(
            symbol.Codewords,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            selectedMask);
        QrMatrix core = ConvertBitMatrix(bits);
        QrMatrix result = MatrixTransformer.AddQuietZone(
            core,
            symbol.SymbolKind,
            quietZoneModules);
        ModuleClassifier.ClassifyModules(
            result,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex);
        symbol.MaskPattern = selectedMask;
        symbol.Width = bits.GetWidth();
        symbol.Height = bits.GetHeight();
        SetCapacityMetadata(symbol, errorCorrectionIndex);
        symbol.Matrix = result;
        return result;
    }

    /// <summary>Extracts placement-order codewords from a completed module matrix.</summary>
    /// <param name="matrix">The rendered or encoding-region matrix.</param>
    /// <param name="symbolKind">The QR profile represented by the matrix.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="errorCorrectionLevel">The selected error-correction level.</param>
    /// <param name="maskPattern">The explicit mask present in the matrix.</param>
    /// <returns>The extracted interleaved codewords in placement order.</returns>
    public byte[] ExtractCodewords(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrMaskPattern maskPattern)
    {
        int errorCorrectionIndex = GetErrorCorrectionIndex(
            symbolKind,
            version,
            errorCorrectionLevel);
        int mask = ValidateExplicitMask(symbolKind, maskPattern);
        QrMatrix core = MatrixTransformer.RemoveQuietZone(matrix);
        ValidateEncodingDimensions(core, symbolKind, version);
        BitMatrixBuffer bits = ConvertQrMatrix(core);
        ApplyMask(bits, symbolKind, version, mask);
        if (symbolKind == QrSymbolKind.Model2)
        {
            return DataModuleWalker.ExtractModel2Codewords(version, bits);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return DataModuleWalker.ExtractMicroCodewords(
                version,
                errorCorrectionIndex,
                bits);
        }

        return DataModuleWalker.ExtractRectangularMicroCodewords(version, bits);
    }

    /// <summary>Applies a deterministic crop, orientation, quiet-zone, polarity, and scaling pipeline.</summary>
    /// <param name="matrix">The source matrix.</param>
    /// <param name="symbolKind">The profile used when replacing the quiet zone.</param>
    /// <param name="quarterTurnsClockwise">The clockwise quarter-turn count from zero through three.</param>
    /// <param name="mirror">Whether to transpose the oriented matrix across its diagonal.</param>
    /// <param name="invert">Whether to exchange dark and light modules.</param>
    /// <param name="cropLeft">The number of columns removed from the left edge.</param>
    /// <param name="cropTop">The number of rows removed from the top edge.</param>
    /// <param name="cropRight">The number of columns removed from the right edge.</param>
    /// <param name="cropBottom">The number of rows removed from the bottom edge.</param>
    /// <param name="replaceQuietZone">Whether to replace retained quiet-zone margins.</param>
    /// <param name="quietZoneModules">The replacement width, or -1 for the profile default.</param>
    /// <param name="scale">The positive integer output scale.</param>
    /// <returns>A newly allocated transformed matrix.</returns>
    public QrMatrix TransformMatrix(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int quarterTurnsClockwise,
        bool mirror,
        bool invert,
        int cropLeft,
        int cropTop,
        int cropRight,
        int cropBottom,
        bool replaceQuietZone,
        int quietZoneModules,
        int scale)
    {
        MatrixTransformer.GetEncodingRegionWidth(matrix);
        MatrixTransformer.GetStandardQuietZoneWidth(symbolKind);
        if (cropLeft < 0 || cropTop < 0 || cropRight < 0 || cropBottom < 0)
        {
            throw new QrInputException("Crop edge counts cannot be negative.", "cropLeft");
        }

        QrMatrix current = matrix;
        if (cropLeft != 0 || cropTop != 0 || cropRight != 0 || cropBottom != 0)
        {
            long croppedWidth = (long)matrix.Width - cropLeft - cropRight;
            long croppedHeight = (long)matrix.Height - cropTop - cropBottom;
            if (croppedWidth <= 0 || croppedHeight <= 0)
            {
                throw new QrInputException(
                    "Crop edge counts must leave a positive matrix area.",
                    "cropRight");
            }

            current = MatrixTransformer.Crop(
                matrix,
                cropLeft,
                cropTop,
                (int)croppedWidth,
                (int)croppedHeight);
        }

        current = MatrixTransformer.Rotate(current, quarterTurnsClockwise);
        if (mirror)
        {
            current = MatrixTransformer.Mirror(current);
        }

        if (replaceQuietZone)
        {
            current = MatrixTransformer.AddQuietZone(
                current,
                symbolKind,
                quietZoneModules);
        }

        if (invert)
        {
            current = MatrixTransformer.InvertPolarity(current);
        }

        return MatrixTransformer.Scale(current, scale);
    }

    /// <summary>Classifies every rendered matrix cell by its structural or payload role.</summary>
    /// <param name="matrix">The matrix to classify; its role array is replaced.</param>
    /// <param name="symbolKind">The QR profile represented by the matrix.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="errorCorrectionLevel">The selected error-correction level.</param>
    /// <returns>The newly allocated row-major role map.</returns>
    public QrModuleRole[] ClassifyModules(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel)
    {
        int errorCorrectionIndex = GetErrorCorrectionIndex(
            symbolKind,
            version,
            errorCorrectionLevel);
        return ModuleClassifier.ClassifyModules(
            matrix,
            symbolKind,
            version,
            errorCorrectionIndex);
    }

    /// <summary>Validates matrix geometry, metadata, placement, padding, and recoverability.</summary>
    /// <param name="symbol">The symbol and retained construction metadata to validate.</param>
    /// <returns>The detailed logical validation result, also attached to the symbol.</returns>
    public QrValidationResult ValidateMatrix(QrSymbol symbol)
    {
        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);
        symbol.Validation = result;
        symbol.IsConformant = result.IsConformant;
        return result;
    }

    internal static int GetErrorCorrectionIndex(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            int value = (int)errorCorrectionLevel;
            EccBlockTable.GetDataCodewordCount(version, value);
            return value;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            int value = (int)errorCorrectionLevel;
            MicroVersionTable.GetDataBitCapacity(version, value);
            return value;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            if (errorCorrectionLevel == QrErrorCorrectionLevel.Medium)
            {
                RectangularMicroVersionTable.GetDataBitCapacity(
                    version,
                    RectangularMicroVersionTable.Medium);
                return RectangularMicroVersionTable.Medium;
            }

            if (errorCorrectionLevel == QrErrorCorrectionLevel.High)
            {
                RectangularMicroVersionTable.GetDataBitCapacity(
                    version,
                    RectangularMicroVersionTable.High);
                return RectangularMicroVersionTable.High;
            }

            throw new QrInputException(
                "rMQR supports only medium and high error correction.",
                "errorCorrectionLevel");
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            throw new QrUnsupportedFeatureException(
                "Legacy Model 1 matrix processing is decode-only.",
                QrFailureReason.InvalidInput,
                "LegacyModel1MatrixProcessing",
                version);
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    internal static QrMaskPattern SelectMask(
        byte[] codewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2MatrixBuilder.SelectModel2Mask(
                codewords,
                version,
                errorCorrectionIndex,
                requestedMask);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroMatrixBuilder.SelectMicroMask(
                codewords,
                version,
                errorCorrectionIndex,
                requestedMask);
        }

        return RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
            codewords,
            version,
            errorCorrectionIndex,
            requestedMask);
    }

    internal static BitMatrixBuffer BuildBitMatrix(
        byte[] codewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern maskPattern)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2MatrixBuilder.CreateSelectedModel2Matrix(
                codewords,
                version,
                errorCorrectionIndex,
                maskPattern);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroMatrixBuilder.CreateSelectedMicroMatrix(
                codewords,
                version,
                errorCorrectionIndex,
                maskPattern);
        }

        return RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(
            codewords,
            version,
            errorCorrectionIndex,
            maskPattern);
    }

    internal static QrMatrix ConvertBitMatrix(BitMatrixBuffer bits)
    {
        int width = bits.GetWidth();
        int height = bits.GetHeight();
        QrMatrix result = new QrMatrix
        {
            Width = width,
            Height = height,
            Values = new QrModuleValue[width * height]
        };
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                result.Values[row * width + column] = bits.GetModule(row, column)
                    ? QrModuleValue.Dark
                    : QrModuleValue.Light;
            }
        }

        return result;
    }

    internal static BitMatrixBuffer ConvertQrMatrix(QrMatrix matrix)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(matrix.Width, matrix.Height);
        for (int row = 0; row < matrix.Height; row++)
        {
            for (int column = 0; column < matrix.Width; column++)
            {
                int index = row * matrix.Width + column;
                QrModuleValue value = matrix.Values[index];
                if (value != QrModuleValue.Light && value != QrModuleValue.Dark)
                {
                    throw new QrInputException(
                        "Codeword extraction requires known light or dark modules.",
                        "Values");
                }

                result.SetModule(row, column, value == QrModuleValue.Dark);
            }
        }

        return result;
    }

    internal static int ValidateExplicitMask(
        QrSymbolKind symbolKind,
        QrMaskPattern maskPattern)
    {
        int mask = (int)maskPattern;
        if (symbolKind == QrSymbolKind.Model2 && mask >= 0 && mask <= 7)
        {
            return mask;
        }

        if (symbolKind == QrSymbolKind.Micro && mask >= 0 && mask <= 3)
        {
            return mask;
        }

        if (symbolKind == QrSymbolKind.rMQR && mask == 4)
        {
            return mask;
        }

        throw new QrInputException(
            "Codeword extraction requires an explicit profile-valid mask.",
            "maskPattern");
    }

    internal static void ValidateEncodingDimensions(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version)
    {
        int expectedWidth = 0;
        int expectedHeight = 0;
        if (symbolKind == QrSymbolKind.Model2)
        {
            expectedWidth = Model2VersionTable.GetDimension(version);
            expectedHeight = expectedWidth;
        }
        else if (symbolKind == QrSymbolKind.Micro)
        {
            expectedWidth = MicroVersionTable.GetDimension(version);
            expectedHeight = expectedWidth;
        }
        else
        {
            expectedWidth = RectangularMicroVersionTable.GetWidth(version);
            expectedHeight = RectangularMicroVersionTable.GetHeight(version);
        }

        if (matrix.Width != expectedWidth || matrix.Height != expectedHeight)
        {
            throw new QrInputException(
                "The encoding-region dimensions do not match the selected version.",
                "matrix");
        }
    }

    internal static void ApplyMask(
        BitMatrixBuffer matrix,
        QrSymbolKind symbolKind,
        int version,
        int mask)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            MaskEvaluator.ApplyModel2Mask(
                matrix,
                FunctionPatternBuilder.CreateModel2ReservedMap(version),
                mask);
        }
        else if (symbolKind == QrSymbolKind.Micro)
        {
            MaskEvaluator.ApplyMicroMask(
                matrix,
                FunctionPatternBuilder.CreateMicroReservedMap(version),
                mask);
        }
        else
        {
            MaskEvaluator.ApplyRectangularMicroMask(
                matrix,
                FunctionPatternBuilder.CreateRectangularMicroReservedMap(version));
        }
    }

    internal static void SetCapacityMetadata(QrSymbol symbol, int errorCorrectionIndex)
    {
        if (symbol.SymbolKind == QrSymbolKind.Model2)
        {
            symbol.DataCodewordCount = EccBlockTable.GetDataCodewordCount(
                symbol.Version,
                errorCorrectionIndex);
            symbol.TotalCodewordCount = Model2VersionTable.GetTotalCodewordCount(
                symbol.Version);
            symbol.RemainderBitCount = Model2VersionTable.GetRemainderBitCount(
                symbol.Version);
            symbol.DataBitCapacity = symbol.DataCodewordCount * 8;
        }
        else if (symbol.SymbolKind == QrSymbolKind.Micro)
        {
            symbol.DataCodewordCount = MicroVersionTable.GetDataSymbolCharacterCount(
                symbol.Version,
                errorCorrectionIndex);
            symbol.ErrorCorrectionCodewordCount = MicroVersionTable.GetEccCodewordCount(
                symbol.Version,
                errorCorrectionIndex);
            symbol.TotalCodewordCount = symbol.DataCodewordCount
                + symbol.ErrorCorrectionCodewordCount;
            symbol.RemainderBitCount = 0;
            symbol.DataBitCapacity = MicroVersionTable.GetDataBitCapacity(
                symbol.Version,
                errorCorrectionIndex);
        }
        else
        {
            symbol.DataCodewordCount = RectangularMicroVersionTable.GetDataCodewordCount(
                symbol.Version,
                errorCorrectionIndex);
            symbol.TotalCodewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(
                symbol.Version);
            symbol.RemainderBitCount = RectangularMicroVersionTable.GetRemainderBitCount(
                symbol.Version);
            symbol.DataBitCapacity = RectangularMicroVersionTable.GetDataBitCapacity(
                symbol.Version,
                errorCorrectionIndex);
        }

        symbol.ErrorCorrectionCodewordCount = symbol.TotalCodewordCount
            - symbol.DataCodewordCount;
        if (symbol.UsedDataBitCount >= 0
            && symbol.UsedDataBitCount <= symbol.DataBitCapacity)
        {
            symbol.UnusedDataBitCount = symbol.DataBitCapacity - symbol.UsedDataBitCount;
        }
    }
}
