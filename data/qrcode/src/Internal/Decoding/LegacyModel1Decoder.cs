namespace qrcode;

internal class LegacyModel1Decoder
{
    // Reads canonical, normal-reflectance geometry. Legacy Model 1 does not
    // support mirrored or reversed-reflectance symbols. No new symbols are emitted.
    internal static QrDecodeResult Decode(QrMatrix matrix, QrDecodeOptions options)
    {
        MatrixCodewordResult codewords = ReadCodewords(matrix, options);
        if (!codewords.Success)
        {
            throw new QrDecodeException("The legacy Model 1 matrix could not be decoded.", codewords.FailureReason, -1, 0);
        }

        return DecodedSymbolBuilder.Create(codewords, options);
    }

    internal static MatrixCodewordResult ReadCodewords(QrMatrix matrix, QrDecodeOptions options)
    {
        return ReadWithFormatCopy(matrix, options, -1);
    }

    internal static MatrixCodewordResult ReadWithFormatCopy(QrMatrix matrix, QrDecodeOptions options, int formatCopy)
    {
        MatrixInformationSupport.ValidateInput(matrix, options);
        if (!options.AllowLegacyModel1)
        {
            throw new QrUnsupportedFeatureException("Legacy Model 1 decoding is disabled.", QrFailureReason.InvalidInput, "LegacyModel1", -1);
        }

        MatrixCodewordResult result = new MatrixCodewordResult { SymbolKind = QrSymbolKind.LegacyModel1 };
        int dimension = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        if (dimension != height || dimension < 21 || dimension > 73 || (dimension - 17) % 4 != 0)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        int version = (dimension - 17) / 4;
        result.Version = version;
        int workBytes = CheckedDimensions.CalculateAllocationBytes(dimension * dimension, 256, options.MaximumAllocationBytes);
        CheckedDimensions.CalculateSum(workBytes, 4096, options.MaximumAllocationBytes, "MaximumAllocationBytes");
        MatrixInformationCopy[] copies = new MatrixInformationCopy[] { ReadFormat(matrix, version, false), ReadFormat(matrix, version, true) };
        int selected = MatrixInformationSupport.ChooseRequestedCopy(copies, formatCopy);
        if (selected < 0)
        {
            result.FailureReason = QrFailureReason.InvalidFormat;
            return result;
        }

        int level = copies[selected].Data / 8;
        int mask = copies[selected].Data % 8;
        result.ErrorCorrectionLevel = (QrErrorCorrectionLevel)level;
        result.MaskPattern = (QrMaskPattern)mask;
        SymbolFamilyResult families = SymbolFamilyDiscriminator.Discriminate(matrix, options);
        bool legacyEvidence = false;
        foreach (SymbolFamilyCandidate candidate in families.Candidates)
        {
            if (candidate.SymbolKind == QrSymbolKind.LegacyModel1 && !candidate.Rejected && candidate.HasSufficientEvidence)
            {
                legacyEvidence = true;
            }
        }

        if (!legacyEvidence)
        {
            result.FailureReason = QrFailureReason.InvalidGeometry;
            return result;
        }

        int[] offsets = LegacyModel1Placement.CreateOffsets(version);
        byte[] extracted = new byte[offsets.Length / 8];
        byte[] erasureMarkers = new byte[extracted.Length];
        int remainderCount = LegacyModel1Table.GetRemainderCodewordCount(version, level);
        int protectedCount = extracted.Length - remainderCount;
        result.RemainderBitCount = remainderCount * 8;
        result.DataBitCount = LegacyModel1Table.GetDataBitCount(version, level);
        for (int bit = 4; bit < offsets.Length; bit++)
        {
            int row = offsets[bit] / dimension;
            int column = offsets[bit] % dimension;
            int index = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
            bool missing = matrix.Values[index] == QrModuleValue.Unknown
                || matrix.Confidence != null && matrix.Confidence[index] == 0;
            bool uncertain = CodewordErasureMapper.IsUncertain(matrix, index);
            bool dark = (matrix.Values[index] == QrModuleValue.Dark) != Model2MaskTable.IsInverted(mask, row, column);
            if (bit / 8 >= protectedCount)
            {
                if (uncertain || dark) { result.InvalidRemainderBitCount++; }
            }
            else if (uncertain)
            {
                erasureMarkers[bit / 8] = 1;
                if (missing) { result.UnknownPayloadModuleCount++; }
                else { result.LowConfidencePayloadModuleCount++; }
            }

            if (dark) { extracted[bit / 8] = (byte)(extracted[bit / 8] | 1 << (7 - bit % 8)); }
        }

        result.ExtractedCodewords = extracted;
        result.ErasurePositions = CodewordErasureMapper.GetPositions(erasureMarkers);
        if (result.InvalidRemainderBitCount > 0)
        {
            result.FailureReason = QrFailureReason.InvalidData;
            return result;
        }

        BlockCorrectionResult correction = BlockDeinterleaver.CorrectLegacyModel1Codewords(extracted, version, level, result.ErasurePositions);
        result.Correction = correction;
        if (!correction.Success)
        {
            result.FailureReason = QrFailureReason.ChecksumFailure;
        }
        else if ((correction.DataCodewords[0] & 0xF0) != 0)
        {
            result.FailureReason = QrFailureReason.InvalidData;
        }
        else
        {
            result.DataCodewords = correction.DataCodewords;
            result.Success = true;
        }

        return result;
    }

    internal static MatrixInformationCopy ReadFormat(QrMatrix matrix, int version, bool secondary)
    {
        MatrixInformationCopy copy = new MatrixInformationCopy();
        for (int bit = 0; bit < 15; bit++)
        {
            int row = secondary ? LegacyModel1Table.GetFormatSecondaryRow(version, bit) : LegacyModel1Table.GetFormatPrimaryRow(bit);
            int column = secondary ? LegacyModel1Table.GetFormatSecondaryColumn(version, bit) : LegacyModel1Table.GetFormatPrimaryColumn(bit);
            MatrixInformationSupport.ReadBit(matrix, copy, row, column, bit);
        }

        MatrixInformationSupport.CorrectCopy(copy, 15, 0, 31, data => LegacyModel1Table.GetFormatCode(data / 8, data % 8));
        return copy;
    }
}
