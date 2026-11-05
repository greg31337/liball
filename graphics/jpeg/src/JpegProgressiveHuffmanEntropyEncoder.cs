using System.Collections.Generic;

namespace jpeg;

internal class JpegProgressiveHuffmanEntropyEncoder
{
    internal JpegBaselineHuffmanEntropyEncoder BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
    internal JpegBitWriter BitWriter = new JpegBitWriter();
    internal int[] DcPredictors = new int[256];
    internal int EobRun;

    internal JpegProgressiveHuffmanEntropyEncoder()
    {
        BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
        InitializeAliases();
    }

    internal static JpegProgressiveHuffmanEntropyEncoder FromBitWriter(JpegBitWriter bitWriter)
    {
        if (bitWriter == null)
        {
            throw new System.ArgumentNullException("bitWriter");
        }

        JpegProgressiveHuffmanEntropyEncoder encoder = new JpegProgressiveHuffmanEntropyEncoder();
        encoder.BaselineEncoder = JpegBaselineHuffmanEntropyEncoder.FromBitWriter(bitWriter);
        encoder.InitializeAliases();
        return encoder;
    }

    internal static JpegProgressiveHuffmanEntropyEncoder FromStream(System.IO.Stream stream)
    {
        return FromBitWriter(JpegBitWriter.FromStream(stream));
    }

    internal void InitializeAliases()
    {
        BitWriter = BaselineEncoder.BitWriter;
        DcPredictors = BaselineEncoder.DcPredictors;
    }

    internal void EncodeDcFirstBlock(int[] coefficients, JpegHuffmanTable dcTable, int componentIndex, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);
        BaselineEncoder.ValidateComponentIndex(componentIndex);
        BaselineEncoder.ValidateTable(dcTable, 0);

        int coefficient = ShiftCoefficientDown(coefficients[0], successiveLow);
        int difference = BaselineEncoder.CalculateDifference(coefficient, DcPredictors[componentIndex]);
        int category = BaselineEncoder.GetMagnitudeCategory(difference);
        BaselineEncoder.ValidateDcCategory(category);
        BaselineEncoder.EncodeSymbol(dcTable, category);
        BaselineEncoder.WriteMagnitudeBits(difference, category);
        DcPredictors[componentIndex] = coefficient;
    }

    internal void EncodeDcFirstBlockForScanComponent(List<JpegHuffmanTable> tables, JpegScanHeader scan, JpegScanComponent component, int[] coefficients, int componentIndex)
    {
        ValidateDcFirstScan(scan);
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineEncoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        EncodeDcFirstBlock(coefficients, dcTable, componentIndex, scan.SuccessiveLow);
    }

    internal void EncodeDcRefinementBlock(int[] coefficients, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);

        BitWriter.WriteBit(GetRefinementBit(coefficients[0], successiveLow));
    }

    internal void EncodeDcRefinementBlockForScan(JpegScanHeader scan, int[] coefficients)
    {
        ValidateDcRefinementScan(scan);
        EncodeDcRefinementBlock(coefficients, scan.SuccessiveLow);
    }

    internal void EncodeAcFirstBlock(int[] coefficients, JpegHuffmanTable acTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcFirstRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        BaselineEncoder.ValidateTable(acTable, 1);

        if (!HasNonzeroCoefficient(coefficients, spectralStart, spectralEnd))
        {
            AccumulateEobRun(acTable);
            return;
        }

        FlushEobRun(acTable);

        int zeroRun = 0;
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            int coefficient = ShiftCoefficientDown(coefficients[index], successiveLow);
            if (coefficient == 0)
            {
                zeroRun = zeroRun + 1;
            }
            else
            {
                while (zeroRun > 15)
                {
                    BaselineEncoder.EncodeSymbol(acTable, 0xF0);
                    zeroRun = zeroRun - 16;
                }

                int category = BaselineEncoder.GetMagnitudeCategory(coefficient);
                BaselineEncoder.ValidateAcCategory(category);
                BaselineEncoder.EncodeSymbol(acTable, (zeroRun << 4) + category);
                BaselineEncoder.WriteMagnitudeBits(coefficient, category);
                zeroRun = 0;
            }

            index = index + 1;
        }

        if (zeroRun > 0)
        {
            EncodeEobRun(acTable, 1);
        }
    }

    internal void EncodeAcFirstBlockForScan(JpegScanHeader scan, JpegHuffmanTable acTable, int[] coefficients)
    {
        ValidateAcFirstScan(scan);
        EncodeAcFirstBlock(coefficients, acTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal void EncodeAcRefinementBlock(int[] coefficients, JpegHuffmanTable acTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcFirstRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        BaselineEncoder.ValidateTable(acTable, 1);
        FlushEobRun(acTable);

        int bitValue = 1 << successiveLow;
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            if (IsExistingRefinementCoefficient(coefficients[index], bitValue))
            {
                WriteRefinementBit(coefficients[index], bitValue);
                index = index + 1;
                continue;
            }

            int nextNewIndex = FindNextNewCoefficient(coefficients, index, spectralEnd, bitValue);
            if (nextNewIndex < 0)
            {
                EncodeEobRun(acTable, 1);
                WriteExistingRefinementBits(coefficients, index, spectralEnd, bitValue);
                return;
            }

            int zeroRun = CountZeroCoefficients(coefficients, index, nextNewIndex - 1, bitValue);
            while (zeroRun > 15)
            {
                BaselineEncoder.EncodeSymbol(acTable, 0xF0);
                index = AdvanceAfterZeroRun(coefficients, index, spectralEnd, 16, bitValue);
                zeroRun = zeroRun - 16;
            }

            BaselineEncoder.EncodeSymbol(acTable, (zeroRun << 4) + 1);
            index = AdvanceAfterZeroRun(coefficients, index, spectralEnd, zeroRun, bitValue);
            WriteNewCoefficientSignBit(coefficients[index]);
            index = index + 1;
        }
    }

    internal void EncodeAcRefinementBlockForScan(JpegScanHeader scan, JpegHuffmanTable acTable, int[] coefficients)
    {
        ValidateAcRefinementScan(scan);
        EncodeAcRefinementBlock(coefficients, acTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal void AccumulateEobRun(JpegHuffmanTable acTable)
    {
        BaselineEncoder.ValidateTable(acTable, 1);
        EobRun = EobRun + 1;
        if (EobRun == 32767)
        {
            FlushEobRun(acTable);
        }
    }

    internal void FlushEobRun(JpegHuffmanTable acTable)
    {
        if (EobRun == 0)
        {
            return;
        }

        EncodeEobRun(acTable, EobRun);
        EobRun = 0;
    }

    internal void EncodeEobRun(JpegHuffmanTable acTable, int run)
    {
        if (run < 1 || run > 32767)
        {
            throw new System.ArgumentOutOfRangeException("run");
        }

        int runLength = 0;
        int baseRun = 1;
        while ((baseRun << 1) <= run)
        {
            baseRun = baseRun << 1;
            runLength = runLength + 1;
        }

        BaselineEncoder.EncodeSymbol(acTable, runLength << 4);
        if (runLength > 0)
        {
            BitWriter.WriteBits(run - baseRun, runLength);
        }
    }

    internal int FindNextNewCoefficient(int[] coefficients, int index, int spectralEnd, int bitValue)
    {
        while (index <= spectralEnd)
        {
            if (IsNewRefinementCoefficient(coefficients[index], bitValue))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int CountZeroCoefficients(int[] coefficients, int index, int spectralEnd, int bitValue)
    {
        int zeroRun = 0;
        while (index <= spectralEnd)
        {
            if (coefficients[index] == 0)
            {
                zeroRun = zeroRun + 1;
            }
            else if (!IsExistingRefinementCoefficient(coefficients[index], bitValue))
            {
                throw JpegWriteValidationException.Create("AC refinement coefficient magnitude does not match the scan bit position.");
            }

            index = index + 1;
        }

        return zeroRun;
    }

    internal int AdvanceAfterZeroRun(int[] coefficients, int index, int spectralEnd, int zeroRun, int bitValue)
    {
        while (index <= spectralEnd)
        {
            if (IsExistingRefinementCoefficient(coefficients[index], bitValue))
            {
                WriteRefinementBit(coefficients[index], bitValue);
                index = index + 1;
                continue;
            }

            if (zeroRun == 0)
            {
                return index;
            }

            if (coefficients[index] != 0)
            {
                throw JpegWriteValidationException.Create("AC refinement run skipped a new coefficient.");
            }

            zeroRun = zeroRun - 1;
            index = index + 1;
        }

        return index;
    }

    internal void WriteExistingRefinementBits(int[] coefficients, int spectralStart, int spectralEnd, int bitValue)
    {
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            if (IsExistingRefinementCoefficient(coefficients[index], bitValue))
            {
                WriteRefinementBit(coefficients[index], bitValue);
            }

            index = index + 1;
        }
    }

    internal void WriteRefinementBit(int coefficient, int bitValue)
    {
        int magnitude = AbsoluteValue(coefficient);
        if ((magnitude & bitValue) != 0)
        {
            BitWriter.WriteBit(1);
            return;
        }

        BitWriter.WriteBit(0);
    }

    internal void WriteNewCoefficientSignBit(int coefficient)
    {
        if (coefficient > 0)
        {
            BitWriter.WriteBit(1);
            return;
        }

        if (coefficient < 0)
        {
            BitWriter.WriteBit(0);
            return;
        }

        throw JpegWriteValidationException.Create("New AC refinement coefficient cannot be zero.");
    }

    internal int GetRefinementBit(int coefficient, int successiveLow)
    {
        int bitValue = 1 << successiveLow;
        int magnitude = AbsoluteValue(coefficient);
        if ((magnitude & bitValue) != 0)
        {
            return 1;
        }

        return 0;
    }

    internal bool HasNonzeroCoefficient(int[] coefficients, int spectralStart, int spectralEnd)
    {
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            if (coefficients[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsNewRefinementCoefficient(int coefficient, int bitValue)
    {
        return AbsoluteValue(coefficient) == bitValue;
    }

    internal bool IsExistingRefinementCoefficient(int coefficient, int bitValue)
    {
        int magnitude = AbsoluteValue(coefficient);
        return magnitude > bitValue;
    }

    internal int ShiftCoefficientDown(int coefficient, int successiveLow)
    {
        int bitValue = 1 << successiveLow;
        if (coefficient % bitValue != 0)
        {
            throw JpegWriteValidationException.Create("Progressive coefficient is not aligned to the successive approximation bit.");
        }

        return coefficient / bitValue;
    }

    internal int AbsoluteValue(int value)
    {
        if (value == int.MinValue)
        {
            throw JpegWriteValidationException.Create("Coefficient magnitude is outside the supported integer range.");
        }

        if (value < 0)
        {
            return -value;
        }

        return value;
    }

    internal byte[] ToArray()
    {
        return BitWriter.ToArray();
    }

    internal void FlushToByteBoundary()
    {
        BitWriter.FlushToByteBoundary();
    }

    internal void ResetDcPredictors()
    {
        BaselineEncoder.ResetDcPredictors();
    }

    internal void ConfigureRestartInterval(int restartInterval)
    {
        BaselineEncoder.ConfigureRestartInterval(restartInterval);
    }

    internal void RecordMcuEncoded()
    {
        BaselineEncoder.RecordMcuEncoded();
    }

    internal bool ShouldWriteRestartMarker()
    {
        return BaselineEncoder.ShouldWriteRestartMarker();
    }

    internal void WriteRestartMarker()
    {
        BaselineEncoder.WriteRestartMarker();
        EobRun = 0;
    }

    internal void ValidateDcFirstScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 0)
        {
            throw JpegWriteValidationException.Create("Progressive DC first scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh != 0)
        {
            throw JpegWriteValidationException.Create("Progressive DC first scans must have successive high approximation 0.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
    }

    internal void ValidateDcRefinementScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 0)
        {
            throw JpegWriteValidationException.Create("Progressive DC refinement scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh <= 0)
        {
            throw JpegWriteValidationException.Create("Progressive DC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegWriteValidationException.Create("Progressive DC refinement scans must decrease successive approximation by one bit.");
        }
    }

    internal void ValidateAcFirstScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        ValidateAcFirstRange(scan.SpectralStart, scan.SpectralEnd);
        if (scan.SuccessiveHigh != 0)
        {
            throw JpegWriteValidationException.Create("Progressive AC first scans must have successive high approximation 0.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegWriteValidationException.Create("Progressive AC scans must contain exactly one component.");
        }
    }

    internal void ValidateAcRefinementScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        ValidateAcFirstRange(scan.SpectralStart, scan.SpectralEnd);
        if (scan.SuccessiveHigh <= 0)
        {
            throw JpegWriteValidationException.Create("Progressive AC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegWriteValidationException.Create("Progressive AC refinement scans must decrease successive approximation by one bit.");
        }

        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegWriteValidationException.Create("Progressive AC scans must contain exactly one component.");
        }
    }

    internal void ValidateAcFirstRange(int spectralStart, int spectralEnd)
    {
        if (spectralStart < 1 || spectralStart > 63)
        {
            throw JpegWriteValidationException.Create("Progressive AC scans must start between coefficients 1 and 63.");
        }

        if (spectralEnd < spectralStart || spectralEnd > 63)
        {
            throw JpegWriteValidationException.Create("Progressive AC scan spectral end is invalid.");
        }
    }

    internal int GetScanComponentCount(JpegScanHeader scan)
    {
        if (scan.ComponentCount != 0)
        {
            return scan.ComponentCount;
        }

        if (scan.Components == null)
        {
            return 0;
        }

        return scan.Components.Count;
    }

    internal void ValidateCoefficientBlock(int[] coefficients)
    {
        if (coefficients == null)
        {
            throw new System.ArgumentNullException("coefficients");
        }

        if (coefficients.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegWriteValidationException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateSuccessiveLow(int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw JpegWriteValidationException.Create("Progressive successive low approximation must be between 0 and 15.");
        }
    }
}
