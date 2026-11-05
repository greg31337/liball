namespace qrcode;

internal class MatrixCodewordReader
{
    // One canonical, polarity-normalized matrix and an explicit family hypothesis.
    // Does not parse payloads, retry orientations, or guess unknown data modules.
    internal static MatrixCodewordResult Read(QrMatrix matrix, QrSymbolKind kind, QrDecodeOptions options)
    {
        return ReadWithCopyChoices(matrix, kind, options, -1, -1);
    }

    internal static MatrixCodewordResult ReadWithCopyChoices(QrMatrix matrix, QrSymbolKind kind, QrDecodeOptions options, int formatCopy, int versionCopy)
    {
        if (kind == QrSymbolKind.LegacyModel1)
        {
            return LegacyModel1Decoder.ReadWithFormatCopy(matrix, options, formatCopy);
        }

        MatrixCodewordResult result = ReadInformation(matrix, kind, options, formatCopy, versionCopy);
        if (result.FailureReason != QrFailureReason.None)
        {
            return result;
        }

        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        // Conservative bound covering maps, extracted/repaired blocks, and RS
        // work arrays before allocating any grid-sized intermediate storage.
        int workBytes = CheckedDimensions.CalculateAllocationBytes(width * height, 256, options.MaximumAllocationBytes);
        CheckedDimensions.CalculateSum(workBytes, 4096, options.MaximumAllocationBytes, "MaximumAllocationBytes");
        int version = result.Version;
        int level = (int)result.ErrorCorrectionLevel;
        if (kind == QrSymbolKind.rMQR)
        {
            level = result.ErrorCorrectionLevel == QrErrorCorrectionLevel.Medium
                ? RectangularMicroVersionTable.Medium : RectangularMicroVersionTable.High;
        }

        QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, level);
        BitMatrixBuffer unmasked = new BitMatrixBuffer(width, height);
        BitMatrixBuffer uncertain = new BitMatrixBuffer(width, height);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int sourceIndex = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
                QrModuleValue value = matrix.Values[sourceIndex];
                bool missing = value == QrModuleValue.Unknown
                    || matrix.Confidence != null && matrix.Confidence[sourceIndex] == 0;
                bool unreliable = CodewordErasureMapper.IsUncertain(matrix, sourceIndex);
                QrModuleRole role = roles[row * width + column];
                if (role == QrModuleRole.Data || role == QrModuleRole.ErrorCorrection)
                {
                    if (missing)
                    {
                        result.UnknownPayloadModuleCount++;
                    }
                    else if (unreliable)
                    {
                        result.LowConfidencePayloadModuleCount++;
                    }
                    uncertain.SetModule(row, column, unreliable);
                }

                unmasked.SetModule(row, column, value == QrModuleValue.Dark);
                if (role == QrModuleRole.Remainder && unreliable)
                {
                    result.InvalidRemainderBitCount++;
                }
            }
        }

        if (kind == QrSymbolKind.Model2)
        {
            MaskEvaluator.ApplyModel2Mask(unmasked, FunctionPatternBuilder.CreateModel2ReservedMap(version), (int)result.MaskPattern);
            result.DataBitCount = EccBlockTable.GetDataCodewordCount(version, level) * 8;
        }
        else if (kind == QrSymbolKind.Micro)
        {
            MaskEvaluator.ApplyMicroMask(unmasked, FunctionPatternBuilder.CreateMicroReservedMap(version), (int)result.MaskPattern);
            result.DataBitCount = MicroVersionTable.GetDataBitCapacity(version, level);
        }
        else
        {
            MaskEvaluator.ApplyRectangularMicroMask(unmasked, FunctionPatternBuilder.CreateRectangularMicroReservedMap(version));
            result.DataBitCount = RectangularMicroVersionTable.GetDataBitCapacity(version, level);
        }

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (roles[row * width + column] == QrModuleRole.Remainder)
                {
                    result.RemainderBitCount++;
                    int sourceIndex = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
                    bool known = !CodewordErasureMapper.IsUncertain(matrix, sourceIndex);
                    if (known && unmasked.GetModule(row, column))
                    {
                        result.InvalidRemainderBitCount++;
                    }
                }
            }
        }

        if (result.InvalidRemainderBitCount > 0)
        {
            result.FailureReason = QrFailureReason.InvalidData;
            return result;
        }

        result.ExtractedCodewords = ReadPlacedBytes(unmasked, kind, version, level);
        result.ErasurePositions = CodewordErasureMapper.GetPositions(ReadPlacedBytes(uncertain, kind, version, level));

        BlockCorrectionResult correction = BlockDeinterleaver.CorrectCodewords(
            result.ExtractedCodewords, kind, version, level, result.ErasurePositions);
        result.Correction = correction;
        // M1's two parity bytes provide detection, not a correcting decode mode.
        if (!correction.Success || kind == QrSymbolKind.Micro && version == 1 && (correction.CorrectedErrorCount != 0 || result.ErasurePositions.Length > 0))
        {
            result.FailureReason = QrFailureReason.ChecksumFailure;
            return result;
        }

        // M1/M3 store only the high nibble of the last data byte. RS arithmetic
        // includes an implicit zero low nibble, which must still be zero after repair.
        if (kind == QrSymbolKind.Micro && result.DataBitCount % 8 == 4
            && (correction.DataCodewords[correction.DataCodewords.Length - 1] & 15) != 0)
        {
            result.FailureReason = QrFailureReason.InvalidData;
            return result;
        }

        result.DataCodewords = correction.DataCodewords;
        result.Success = true;
        return result;
    }

    static byte[] ReadPlacedBytes(BitMatrixBuffer matrix, QrSymbolKind kind, int version, int level)
    {
        if (kind == QrSymbolKind.Model2) { return DataModuleWalker.ExtractModel2Codewords(version, matrix); }
        if (kind == QrSymbolKind.Micro) { return DataModuleWalker.ExtractMicroCodewords(version, level, matrix); }
        return DataModuleWalker.ExtractRectangularMicroCodewords(version, matrix);
    }

    static MatrixCodewordResult ReadInformation(QrMatrix matrix, QrSymbolKind kind, QrDecodeOptions options, int formatCopy, int versionCopy)
    {
        MatrixCodewordResult result = new MatrixCodewordResult { SymbolKind = kind };
        if (kind == QrSymbolKind.Model2)
        {
            Model2InformationResult information = Model2InformationReader.ReadWithCopyChoices(matrix, options, formatCopy, versionCopy);
            result.FailureReason = information.FailureReason;
            result.Version = information.Version;
            result.ErrorCorrectionLevel = information.ErrorCorrectionLevel;
            result.MaskPattern = information.MaskPattern;
        }
        else if (kind == QrSymbolKind.Micro)
        {
            MicroInformationResult information = MicroInformationReader.Read(matrix, options);
            result.FailureReason = information.FailureReason;
            result.Version = information.Version;
            result.ErrorCorrectionLevel = information.ErrorCorrectionLevel;
            result.MaskPattern = information.MaskPattern;
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            RectangularMicroInformationResult information = RectangularMicroInformationReader.ReadWithCopyChoices(matrix, options, formatCopy);
            result.FailureReason = information.FailureReason;
            result.Version = information.VersionIndex;
            result.ErrorCorrectionLevel = information.ErrorCorrectionLevel;
            result.MaskPattern = information.MaskPattern;
        }
        else
        {
            throw new QrInputException("Codeword reading requires Model 2, Micro QR, or rMQR.", "kind");
        }

        return result;
    }
}
