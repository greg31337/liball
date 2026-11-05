namespace qrcode;

internal class MatrixValidator
{
    internal static QrValidationResult ValidateSymbol(QrSymbol symbol)
    {
        if (symbol == null)
        {
            throw new QrInputException("The symbol is required.", "symbol");
        }

        if (symbol.Matrix == null)
        {
            throw new QrInputException("The symbol matrix is required.", "Matrix");
        }

        QrMatrix matrix = symbol.Matrix;
        int matrixArea = ValidateMatrixStorage(matrix);
        MatrixValidationState state = new MatrixValidationState(matrixArea);
        int errorCorrectionIndex = GetErrorCorrectionIndex(
            symbol.SymbolKind,
            symbol.Version,
            symbol.ErrorCorrectionLevel);
        int[] expected = GetExpectedMetadata(
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex);
        int mask = ValidateMask(symbol.SymbolKind, symbol.MaskPattern);

        ValidateSymbolMetadata(symbol, expected, state);
        int encodingWidth = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int encodingHeight = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        bool dimensionsValid = encodingWidth == expected[0]
            && encodingHeight == expected[1];
        bool quietZoneValid = ValidateQuietZone(
            matrix,
            expected[7],
            state);
        CountUnknownAndInvalidModules(matrix, state);
        if (!dimensionsValid)
        {
            state.AddFailure(
                QrFailureReason.InvalidGeometry,
                "The matrix encoding-region dimensions do not match the selected version.");
            return state.CreateResult(false, false, quietZoneValid, false);
        }

        QrModuleRole[] encodingRoles = ModuleClassifier.CreateEncodingRoleMap(
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex);
        QrModuleRole[] expectedRoles = CreateRenderedRoleMap(
            matrix,
            encodingRoles,
            encodingWidth,
            encodingHeight);
        ValidateRoleMetadata(matrix, expectedRoles, state);

        BitMatrixBuffer observed = CreateEncodingMatrix(matrix, encodingWidth, encodingHeight);
        bool formatInformationConsistent = ValidateFormatInformation(
            observed,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            mask);
        bool versionInformationConsistent = ValidateVersionInformation(
            observed,
            symbol.SymbolKind,
            symbol.Version);
        if (!formatInformationConsistent)
        {
            state.AddFailure(
                QrFailureReason.InvalidFormat,
                "The format information is inconsistent with the selected metadata.");
        }

        if (!versionInformationConsistent)
        {
            state.AddFailure(
                QrFailureReason.InvalidVersion,
                "The version information is inconsistent with the selected dimensions.");
        }

        BitMatrixBuffer unmasked = CopyBitMatrix(observed);
        ApplyMask(
            unmasked,
            symbol.SymbolKind,
            symbol.Version,
            mask);
        byte[] extractedCodewords = ExtractCodewords(
            unmasked,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex);
        BlockCorrectionResult correction = BlockDeinterleaver.CorrectCodewords(
            extractedCodewords,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            new int[0]);
        bool isRecoverable = correction.Success
            && formatInformationConsistent
            && versionInformationConsistent;
        if (!correction.Success)
        {
            state.AddFailure(
                QrFailureReason.ChecksumFailure,
                "The extracted codewords exceed the error-correction allowance.");
        }
        else if (correction.CorrectedErrorCount > 0
            || correction.CorrectedErasureCount > 0)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The extracted codewords require error correction.");
        }

        byte[]? retainedCodewords = symbol.Codewords;
        bool codewordMetadataValid = retainedCodewords != null
            && retainedCodewords.Length == expected[4];
        if (!codewordMetadataValid)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The retained codeword count does not match the selected configuration.");
        }
        else if (retainedCodewords != null
            && !ByteArraysEqual(retainedCodewords, extractedCodewords))
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "Matrix extraction does not reproduce the retained placement-order codewords.");
        }

        if (!correction.Success
            || !ValidatePadding(
                correction.DataCodewords,
                symbol.SymbolKind,
                symbol.Version,
                symbol.UsedDataBitCount,
                expected[6]))
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The data terminator, alignment bits, or pad codewords are invalid.");
        }

        byte[] reconstructionCodewords = extractedCodewords;
        if (correction.Success)
        {
            reconstructionCodewords = correction.Codewords;
        }
        else if (codewordMetadataValid && retainedCodewords != null)
        {
            reconstructionCodewords = retainedCodewords;
        }

        BitMatrixBuffer expectedMatrix = BuildMatrix(
            reconstructionCodewords,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            mask);
        CompareMatrixModules(
            matrix,
            expectedMatrix,
            expectedRoles,
            state);
        if (state.FormatModuleDamage)
        {
            state.AddFailure(
                QrFailureReason.InvalidFormat,
                "One or more format-information modules differ from the selected metadata.");
        }

        if (state.VersionModuleDamage)
        {
            state.AddFailure(
                QrFailureReason.InvalidVersion,
                "One or more version-information modules differ from the selected version.");
        }

        return state.CreateResult(
            formatInformationConsistent,
            versionInformationConsistent,
            quietZoneValid,
            isRecoverable);
    }

    static int ValidateMatrixStorage(QrMatrix matrix)
    {
        CheckedDimensions.ValidateDimensionBounds(
            matrix.Width,
            matrix.Height,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImagePixels);
        int area = CheckedDimensions.CalculateArea(
            matrix.Width,
            matrix.Height,
            QrLimits.DefaultMaximumImagePixels);
        if (matrix.Values == null || matrix.Values.Length != area)
        {
            throw new QrInputException(
                "The module values must match the matrix dimensions.",
                "Values");
        }

        if (matrix.Roles != null && matrix.Roles.Length != area)
        {
            throw new QrInputException(
                "The module roles must match the matrix dimensions.",
                "Roles");
        }

        if (matrix.Confidence != null && matrix.Confidence.Length != area)
        {
            throw new QrInputException(
                "The confidence values must match the matrix dimensions.",
                "Confidence");
        }

        if (matrix.QuietZoneLeft < 0
            || matrix.QuietZoneTop < 0
            || matrix.QuietZoneRight < 0
            || matrix.QuietZoneBottom < 0)
        {
            throw new QrInputException(
                "Quiet-zone extents cannot be negative.",
                "matrix");
        }

        long horizontalQuietZone = (long)matrix.QuietZoneLeft + matrix.QuietZoneRight;
        long verticalQuietZone = (long)matrix.QuietZoneTop + matrix.QuietZoneBottom;
        if (horizontalQuietZone >= matrix.Width
            || verticalQuietZone >= matrix.Height)
        {
            throw new QrInputException(
                "Quiet-zone extents must leave a nonempty encoding region.",
                "matrix");
        }

        CheckedDimensions.CalculateAllocationBytes(
            area,
            14,
            QrLimits.DefaultMaximumAllocationBytes);
        return area;
    }

    static int GetErrorCorrectionIndex(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel level)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            int value = (int)level;
            EccBlockTable.GetDataCodewordCount(version, value);
            return value;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            int value = (int)level;
            MicroVersionTable.GetDataBitCapacity(version, value);
            return value;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            if (level == QrErrorCorrectionLevel.Medium)
            {
                RectangularMicroVersionTable.GetDataBitCapacity(
                    version,
                    RectangularMicroVersionTable.Medium);
                return RectangularMicroVersionTable.Medium;
            }

            if (level == QrErrorCorrectionLevel.High)
            {
                RectangularMicroVersionTable.GetDataBitCapacity(
                    version,
                    RectangularMicroVersionTable.High);
                return RectangularMicroVersionTable.High;
            }

            throw new QrInputException(
                "rMQR supports only medium and high error correction.",
                "ErrorCorrectionLevel");
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            throw new QrUnsupportedFeatureException(
                "Legacy Model 1 matrix validation is part of the decode-only parser.",
                QrFailureReason.InvalidInput,
                "LegacyModel1MatrixValidation",
                version);
        }

        throw new QrInputException("The symbol kind is not defined.", "SymbolKind");
    }

    static int[] GetExpectedMetadata(
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        int[] result = new int[9];
        if (symbolKind == QrSymbolKind.Model2)
        {
            result[0] = Model2VersionTable.GetDimension(version);
            result[1] = result[0];
            result[2] = EccBlockTable.GetDataCodewordCount(version, errorCorrectionIndex);
            result[4] = Model2VersionTable.GetTotalCodewordCount(version);
            result[3] = result[4] - result[2];
            result[5] = Model2VersionTable.GetRemainderBitCount(version);
            result[6] = result[2] * 8;
            result[7] = QrLimits.Model2QuietZoneModules;
            result[8] = 4;
            return result;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            result[0] = MicroVersionTable.GetDimension(version);
            result[1] = result[0];
            result[2] = MicroVersionTable.GetDataSymbolCharacterCount(
                version,
                errorCorrectionIndex);
            result[3] = MicroVersionTable.GetEccCodewordCount(
                version,
                errorCorrectionIndex);
            result[4] = result[2] + result[3];
            result[5] = 0;
            result[6] = MicroVersionTable.GetDataBitCapacity(version, errorCorrectionIndex);
            result[7] = QrLimits.MicroQuietZoneModules;
            result[8] = MicroVersionTable.GetTerminatorBitCount(version);
            return result;
        }

        result[0] = RectangularMicroVersionTable.GetWidth(version);
        result[1] = RectangularMicroVersionTable.GetHeight(version);
        result[2] = RectangularMicroVersionTable.GetDataCodewordCount(
            version,
            errorCorrectionIndex);
        result[4] = RectangularMicroVersionTable.GetTotalCodewordCount(version);
        result[3] = result[4] - result[2];
        result[5] = RectangularMicroVersionTable.GetRemainderBitCount(version);
        result[6] = RectangularMicroVersionTable.GetDataBitCapacity(
            version,
            errorCorrectionIndex);
        result[7] = QrLimits.RectangularMicroQuietZoneModules;
        result[8] = RectangularMicroVersionTable.TerminatorBitCount;
        return result;
    }

    static int ValidateMask(QrSymbolKind symbolKind, QrMaskPattern maskPattern)
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
            "The mask is not valid for the selected symbol profile.",
            "MaskPattern");
    }

    static void ValidateSymbolMetadata(
        QrSymbol symbol,
        int[] expected,
        MatrixValidationState state)
    {
        if (symbol.Width != expected[0] || symbol.Height != expected[1])
        {
            state.AddFailure(
                QrFailureReason.InvalidGeometry,
                "The retained symbol dimensions do not match the selected version.");
        }

        if (symbol.DataCodewordCount != expected[2]
            || symbol.ErrorCorrectionCodewordCount != expected[3]
            || symbol.TotalCodewordCount != expected[4]
            || symbol.RemainderBitCount != expected[5]
            || symbol.DataBitCapacity != expected[6])
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The retained capacity metadata does not match the selected configuration.");
        }

        if (symbol.UsedDataBitCount < 0
            || symbol.UsedDataBitCount > expected[6]
            || symbol.UnusedDataBitCount != expected[6] - symbol.UsedDataBitCount)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The retained used and unused data-bit counts are inconsistent.");
        }
    }

    static bool ValidateQuietZone(
        QrMatrix matrix,
        int requiredWidth,
        MatrixValidationState state)
    {
        bool valid = matrix.QuietZoneLeft >= requiredWidth
            && matrix.QuietZoneTop >= requiredWidth
            && matrix.QuietZoneRight >= requiredWidth
            && matrix.QuietZoneBottom >= requiredWidth;
        for (int row = 0; row < matrix.Height; row++)
        {
            for (int column = 0; column < matrix.Width; column++)
            {
                bool quiet = column < matrix.QuietZoneLeft
                    || column >= matrix.Width - matrix.QuietZoneRight
                    || row < matrix.QuietZoneTop
                    || row >= matrix.Height - matrix.QuietZoneBottom;
                int index = row * matrix.Width + column;
                if (quiet && matrix.Values[index] != QrModuleValue.Light)
                {
                    valid = false;
                    state.AddDamage(index, QrModuleRole.QuietZone);
                }
            }
        }

        if (!valid)
        {
            state.AddFailure(
                QrFailureReason.InvalidGeometry,
                "The quiet zone is missing, undersized, or not uniformly light.");
        }

        return valid;
    }

    static void CountUnknownAndInvalidModules(
        QrMatrix matrix,
        MatrixValidationState state)
    {
        bool invalidFound = false;
        for (int index = 0; index < matrix.Values.Length; index++)
        {
            QrModuleValue value = matrix.Values[index];
            if (value == QrModuleValue.Unknown)
            {
                state.UnknownModuleCount++;
            }
            else if (value != QrModuleValue.Light && value != QrModuleValue.Dark)
            {
                invalidFound = true;
                state.AddDamage(index, QrModuleRole.Unknown);
            }
        }

        if (state.UnknownModuleCount > 0)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The matrix contains unknown modules.");
        }

        if (invalidFound)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The matrix contains undefined module values.");
        }
    }

    static QrModuleRole[] CreateRenderedRoleMap(
        QrMatrix matrix,
        QrModuleRole[] encodingRoles,
        int encodingWidth,
        int encodingHeight)
    {
        QrModuleRole[] result = new QrModuleRole[matrix.Values.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = QrModuleRole.QuietZone;
        }

        for (int row = 0; row < encodingHeight; row++)
        {
            int sourceOffset = row * encodingWidth;
            int destinationOffset = (row + matrix.QuietZoneTop) * matrix.Width
                + matrix.QuietZoneLeft;
            for (int column = 0; column < encodingWidth; column++)
            {
                result[destinationOffset + column] = encodingRoles[sourceOffset + column];
            }
        }

        return result;
    }

    static void ValidateRoleMetadata(
        QrMatrix matrix,
        QrModuleRole[] expectedRoles,
        MatrixValidationState state)
    {
        if (matrix.Roles == null)
        {
            return;
        }

        bool mismatch = false;
        for (int index = 0; index < expectedRoles.Length; index++)
        {
            if (matrix.Roles[index] != expectedRoles[index])
            {
                mismatch = true;
                state.AddDamage(index, expectedRoles[index]);
            }
        }

        if (mismatch)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "The retained module-role metadata is inconsistent with placement.");
        }
    }

    static BitMatrixBuffer CreateEncodingMatrix(
        QrMatrix matrix,
        int encodingWidth,
        int encodingHeight)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(encodingWidth, encodingHeight);
        for (int row = 0; row < encodingHeight; row++)
        {
            for (int column = 0; column < encodingWidth; column++)
            {
                int sourceIndex = (row + matrix.QuietZoneTop) * matrix.Width
                    + column
                    + matrix.QuietZoneLeft;
                result.SetModule(
                    row,
                    column,
                    matrix.Values[sourceIndex] == QrModuleValue.Dark);
            }
        }

        return result;
    }

    static bool ValidateFormatInformation(
        BitMatrixBuffer matrix,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            int first = ReadModel2PrimaryFormat(matrix);
            int second = ReadModel2SecondaryFormat(matrix, version);
            BchCorrectionResult corrected = FormatInformationCodec.CorrectModel2Copies(
                first,
                second);
            return corrected.Success
                && corrected.CopiesAgree
                && corrected.Data == errorCorrectionIndex * 8 + mask;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            int code = ReadMicroFormat(matrix);
            BchCorrectionResult corrected = FormatInformationCodec.CorrectMicroCopies(
                code,
                code);
            int expectedData = MicroVersionTable.GetSymbolNumber(
                version,
                errorCorrectionIndex) * 4 + mask;
            return corrected.Success && corrected.Data == expectedData;
        }

        int finder = ReadRectangularFinderFormat(matrix);
        int subfinder = ReadRectangularSubfinderFormat(matrix, version);
        BchCorrectionResult rectangular = FormatInformationCodec.CorrectRectangularMicroCopies(
            finder,
            subfinder);
        return rectangular.Success
            && rectangular.CopiesAgree
            && rectangular.Data == errorCorrectionIndex * 32 + version;
    }

    static bool ValidateVersionInformation(
        BitMatrixBuffer matrix,
        QrSymbolKind symbolKind,
        int version)
    {
        if (symbolKind != QrSymbolKind.Model2 || version < 7)
        {
            return true;
        }

        int first = 0;
        int second = 0;
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            if (matrix.GetModule(
                Model2PlacementTable.GetVersionPrimaryRow(version, bitIndex),
                Model2PlacementTable.GetVersionPrimaryColumn(version, bitIndex)))
            {
                first |= 1 << bitIndex;
            }

            if (matrix.GetModule(
                Model2PlacementTable.GetVersionSecondaryRow(version, bitIndex),
                Model2PlacementTable.GetVersionSecondaryColumn(version, bitIndex)))
            {
                second |= 1 << bitIndex;
            }
        }

        BchCorrectionResult corrected = VersionInformationCodec.CorrectCopies(first, second);
        return corrected.Success && corrected.CopiesAgree && corrected.Data == version;
    }

    static int ReadModel2PrimaryFormat(BitMatrixBuffer matrix)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 15; bitIndex++)
        {
            if (matrix.GetModule(
                Model2PlacementTable.GetFormatPrimaryRow(bitIndex),
                Model2PlacementTable.GetFormatPrimaryColumn(bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static int ReadModel2SecondaryFormat(BitMatrixBuffer matrix, int version)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 15; bitIndex++)
        {
            if (matrix.GetModule(
                Model2PlacementTable.GetFormatSecondaryRow(version, bitIndex),
                Model2PlacementTable.GetFormatSecondaryColumn(version, bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static int ReadMicroFormat(BitMatrixBuffer matrix)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
        {
            if (matrix.GetModule(
                MicroPlacementTable.GetFormatVerticalRow(bitIndex),
                MicroPlacementTable.GetFormatVerticalColumn(bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        for (int bitIndex = 8; bitIndex < 15; bitIndex++)
        {
            if (matrix.GetModule(
                MicroPlacementTable.GetFormatHorizontalRow(bitIndex),
                MicroPlacementTable.GetFormatHorizontalColumn(bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static int ReadRectangularFinderFormat(BitMatrixBuffer matrix)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            if (matrix.GetModule(
                RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static int ReadRectangularSubfinderFormat(
        BitMatrixBuffer matrix,
        int versionIndex)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            if (matrix.GetModule(
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex)))
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static BitMatrixBuffer CopyBitMatrix(BitMatrixBuffer source)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(source.GetWidth(), source.GetHeight());
        for (int row = 0; row < source.GetHeight(); row++)
        {
            for (int column = 0; column < source.GetWidth(); column++)
            {
                result.SetModule(row, column, source.GetModule(row, column));
            }
        }

        return result;
    }

    static void ApplyMask(
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

    static byte[] ExtractCodewords(
        BitMatrixBuffer matrix,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return DataModuleWalker.ExtractModel2Codewords(version, matrix);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return DataModuleWalker.ExtractMicroCodewords(
                version,
                errorCorrectionIndex,
                matrix);
        }

        return DataModuleWalker.ExtractRectangularMicroCodewords(version, matrix);
    }

    static BitMatrixBuffer BuildMatrix(
        byte[] codewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2MatrixBuilder.CreateSelectedModel2Matrix(
                codewords,
                version,
                errorCorrectionIndex,
                (QrMaskPattern)mask);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroMatrixBuilder.CreateSelectedMicroMatrix(
                codewords,
                version,
                errorCorrectionIndex,
                (QrMaskPattern)mask);
        }

        return RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(
            codewords,
            version,
            errorCorrectionIndex,
            QrMaskPattern.Four);
    }

    static bool ValidatePadding(
        byte[] dataCodewords,
        QrSymbolKind symbolKind,
        int version,
        int usedBitCount,
        int capacityBitCount)
    {
        if (usedBitCount < 0 || usedBitCount > capacityBitCount)
        {
            return false;
        }

        int terminatorBitCount = 4;
        if (symbolKind == QrSymbolKind.Micro)
        {
            terminatorBitCount = MicroVersionTable.GetTerminatorBitCount(version);
        }
        else if (symbolKind == QrSymbolKind.rMQR)
        {
            terminatorBitCount = RectangularMicroVersionTable.TerminatorBitCount;
        }

        int position = usedBitCount;
        int remaining = capacityBitCount - position;
        if (terminatorBitCount > remaining)
        {
            terminatorBitCount = remaining;
        }

        for (int index = 0; index < terminatorBitCount; index++)
        {
            if (GetDataBit(dataCodewords, position))
            {
                return false;
            }

            position++;
        }

        while (position < capacityBitCount && position % 8 != 0)
        {
            if (GetDataBit(dataCodewords, position))
            {
                return false;
            }

            position++;
        }

        bool firstPadCodeword = true;
        while (capacityBitCount - position >= 8)
        {
            int expected = firstPadCodeword ? 0xEC : 0x11;
            int actual = 0;
            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                actual <<= 1;
                if (GetDataBit(dataCodewords, position))
                {
                    actual++;
                }

                position++;
            }

            if (actual != expected)
            {
                return false;
            }

            firstPadCodeword = !firstPadCodeword;
        }

        while (position < capacityBitCount)
        {
            if (GetDataBit(dataCodewords, position))
            {
                return false;
            }

            position++;
        }

        return true;
    }

    static bool GetDataBit(byte[] dataCodewords, int bitIndex)
    {
        int byteIndex = bitIndex / 8;
        int bitOffset = 7 - bitIndex % 8;
        return ((dataCodewords[byteIndex] >> bitOffset) & 1) != 0;
    }

    static bool ByteArraysEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    static void CompareMatrixModules(
        QrMatrix observed,
        BitMatrixBuffer expectedEncoding,
        QrModuleRole[] expectedRoles,
        MatrixValidationState state)
    {
        ModuleDamageMeasurement measurement = FixedPatternDamageMeasurer.Measure(
            observed, expectedEncoding, expectedRoles, QrLimits.DefaultMaximumImagePixels);
        for (int index = 0; index < measurement.DamagedModules.Length; index++)
        {
            if (measurement.DamagedModules[index]) { state.AddDamage(index, expectedRoles[index]); }
        }
        state.FixedPatternDamageCount = measurement.FixedPatternDamageCount;
        state.CriticalMetadataDamageCount = measurement.CriticalMetadataDamageCount;
        state.PayloadDamageCount = measurement.PayloadDamageCount;
        state.ErrorCorrectionDamageCount = measurement.ErrorCorrectionDamageCount;
        state.FormatModuleDamage = measurement.DamageCounts[(int)QrModuleRole.Format] > 0;
        state.VersionModuleDamage = measurement.DamageCounts[(int)QrModuleRole.Version] > 0;
        if (measurement.FixedPatternDamageCount > 0)
        {
            state.AddFailure(
                QrFailureReason.InvalidGeometry,
                "One or more fixed function-pattern modules are damaged.");
        }

        if (measurement.PayloadDamageCount > 0 || measurement.ErrorCorrectionDamageCount > 0
            || measurement.DamageCounts[(int)QrModuleRole.Remainder] > 0)
        {
            state.AddFailure(
                QrFailureReason.InvalidData,
                "One or more placed data, error-correction, or remainder modules are inconsistent.");
        }
    }
}
