using System.Collections.Generic;

namespace jpeg;

internal class JpegProgressiveHuffmanEntropyDecoder
{
    internal JpegBaselineHuffmanEntropyDecoder BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(new byte[0]);
    internal JpegBitReader BitReader = new JpegBitReader(new byte[0]);
    internal int[] DcPredictors = new int[256];
    internal int EobRun;

    internal JpegProgressiveHuffmanEntropyDecoder(byte[] entropyData)
    {
        BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        InitializeAliases();
    }

    internal static JpegProgressiveHuffmanEntropyDecoder FromStream(System.IO.Stream stream)
    {
        return FromBitReader(JpegBitReader.FromStream(stream));
    }

    internal static JpegProgressiveHuffmanEntropyDecoder FromBitReader(JpegBitReader bitReader)
    {
        if (bitReader == null)
        {
            throw new System.ArgumentNullException("bitReader");
        }

        JpegProgressiveHuffmanEntropyDecoder decoder = new JpegProgressiveHuffmanEntropyDecoder(new byte[0]);
        decoder.BaselineDecoder = JpegBaselineHuffmanEntropyDecoder.FromBitReader(bitReader);
        decoder.InitializeAliases();
        return decoder;
    }

    internal void InitializeAliases()
    {
        BitReader = BaselineDecoder.BitReader;
        DcPredictors = BaselineDecoder.DcPredictors;
    }

    internal int[] DecodeDcFirstBlock(JpegHuffmanTable dcTable, int componentIndex, int successiveLow)
    {
        ValidateSuccessiveLow(successiveLow);
        BaselineDecoder.ValidateComponentIndex(componentIndex);
        BaselineDecoder.ValidateTable(dcTable, 0);

        int[] coefficients = new int[JpegConstants.BlockSampleCount];
        int category = BaselineDecoder.DecodeSymbol(dcTable);
        BaselineDecoder.ValidateDcCategory(category);
        int difference = BaselineDecoder.ReceiveExtend(category);
        int value = DcPredictors[componentIndex] + difference;
        DcPredictors[componentIndex] = value;
        coefficients[0] = value << successiveLow;
        return coefficients;
    }

    internal int[] DecodeDcFirstBlockForScanComponent(List<JpegHuffmanTable> tables, JpegScanHeader scan, JpegScanComponent component, int componentIndex)
    {
        ValidateDcFirstScan(scan);
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineDecoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        return DecodeDcFirstBlock(dcTable, componentIndex, scan.SuccessiveLow);
    }

    internal void DecodeDcRefinementBlock(int[] coefficients, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);

        int bit = BitReader.ReadBit();
        if (bit == 1)
        {
            coefficients[0] = coefficients[0] + (1 << successiveLow);
        }
    }

    internal void DecodeDcRefinementBlockForScan(JpegScanHeader scan, int[] coefficients)
    {
        ValidateDcRefinementScan(scan);
        DecodeDcRefinementBlock(coefficients, scan.SuccessiveLow);
    }

    internal void DecodeAcFirstBlock(int[] coefficients, JpegHuffmanTable acTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcFirstRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        BaselineDecoder.ValidateTable(acTable, 1);

        if (EobRun > 0)
        {
            EobRun = EobRun - 1;
            return;
        }

        int index = spectralStart;
        while (index <= spectralEnd)
        {
            int symbol = BaselineDecoder.DecodeSymbol(acTable);
            if (symbol == 0)
            {
                return;
            }

            int runLength = symbol >> 4;
            int size = symbol & 15;
            if (size == 0)
            {
                if (runLength == 15)
                {
                    index = index + 16;
                    continue;
                }

                EobRun = DecodeEobRun(runLength) - 1;
                return;
            }

            index = index + runLength;
            if (index > spectralEnd)
            {
                throw JpegInvalidDataException.Create("Progressive AC first run exceeds the scan spectral range.");
            }

            coefficients[index] = BaselineDecoder.ReceiveExtend(size) << successiveLow;
            index = index + 1;
        }
    }

    internal void DecodeAcFirstBlockForScan(JpegScanHeader scan, JpegHuffmanTable acTable, int[] coefficients)
    {
        ValidateAcFirstScan(scan);
        DecodeAcFirstBlock(coefficients, acTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal void DecodeAcRefinementBlock(int[] coefficients, JpegHuffmanTable acTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcFirstRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        BaselineDecoder.ValidateTable(acTable, 1);

        int bitValue = 1 << successiveLow;
        int index = spectralStart;
        if (EobRun > 0)
        {
            RefineExistingCoefficients(coefficients, index, spectralEnd, bitValue);
            EobRun = EobRun - 1;
            return;
        }

        while (index <= spectralEnd)
        {
            if (coefficients[index] != 0)
            {
                RefineCoefficient(coefficients, index, bitValue);
                index = index + 1;
                continue;
            }

            int symbol = BaselineDecoder.DecodeSymbol(acTable);
            int runLength = symbol >> 4;
            int size = symbol & 15;
            if (size == 0)
            {
                if (runLength == 15)
                {
                    index = AdvanceAfterZeroRun(coefficients, index, spectralEnd, 16, bitValue);
                    continue;
                }

                EobRun = DecodeEobRun(runLength);
                RefineExistingCoefficients(coefficients, index, spectralEnd, bitValue);
                EobRun = EobRun - 1;
                return;
            }

            if (size != 1)
            {
                throw JpegInvalidDataException.Create("Progressive AC refinement new coefficients must have magnitude category 1.");
            }

            index = AdvanceAfterZeroRun(coefficients, index, spectralEnd, runLength, bitValue);
            if (index > spectralEnd)
            {
                throw JpegInvalidDataException.Create("Progressive AC refinement run exceeds the scan spectral range.");
            }

            int signBit = BitReader.ReadBit();
            if (signBit == 1)
            {
                coefficients[index] = bitValue;
            }
            else
            {
                coefficients[index] = -bitValue;
            }

            index = index + 1;
        }
    }

    internal void DecodeAcRefinementBlockForScan(JpegScanHeader scan, JpegHuffmanTable acTable, int[] coefficients)
    {
        ValidateAcRefinementScan(scan);
        DecodeAcRefinementBlock(coefficients, acTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal int DecodeEobRun(int runLength)
    {
        if (runLength < 0 || runLength > 14)
        {
            throw JpegInvalidDataException.Create("Progressive EOB run length prefix must be between 0 and 14.");
        }

        if (runLength == 0)
        {
            return 1;
        }

        return (1 << runLength) + BitReader.ReadBits(runLength);
    }

    internal void RefineExistingCoefficients(int[] coefficients, int spectralStart, int spectralEnd, int bitValue)
    {
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            if (coefficients[index] != 0)
            {
                RefineCoefficient(coefficients, index, bitValue);
            }

            index = index + 1;
        }
    }

    internal int AdvanceAfterZeroRun(int[] coefficients, int index, int spectralEnd, int zeroRun, int bitValue)
    {
        while (index <= spectralEnd)
        {
            if (coefficients[index] != 0)
            {
                RefineCoefficient(coefficients, index, bitValue);
                index = index + 1;
                continue;
            }

            if (zeroRun == 0)
            {
                return index;
            }

            zeroRun = zeroRun - 1;
            index = index + 1;
        }

        return index;
    }

    internal void RefineCoefficient(int[] coefficients, int index, int bitValue)
    {
        int bit = BitReader.ReadBit();
        if (bit == 0)
        {
            return;
        }

        if (coefficients[index] > 0)
        {
            coefficients[index] = coefficients[index] + bitValue;
            return;
        }

        coefficients[index] = coefficients[index] - bitValue;
    }

    internal void ValidateDcFirstScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 0)
        {
            throw JpegInvalidDataException.Create("Progressive DC first scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh != 0)
        {
            throw JpegInvalidDataException.Create("Progressive DC first scans must have successive high approximation 0.");
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
            throw JpegInvalidDataException.Create("Progressive DC refinement scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh <= 0)
        {
            throw JpegInvalidDataException.Create("Progressive DC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegInvalidDataException.Create("Progressive DC refinement scans must decrease successive approximation by one bit.");
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
            throw JpegInvalidDataException.Create("Progressive AC first scans must have successive high approximation 0.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegInvalidDataException.Create("Progressive AC scans must contain exactly one component.");
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
            throw JpegInvalidDataException.Create("Progressive AC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegInvalidDataException.Create("Progressive AC refinement scans must decrease successive approximation by one bit.");
        }

        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegInvalidDataException.Create("Progressive AC scans must contain exactly one component.");
        }
    }

    internal void ValidateAcFirstRange(int spectralStart, int spectralEnd)
    {
        if (spectralStart < 1 || spectralStart > 63)
        {
            throw JpegInvalidDataException.Create("Progressive AC scans must start between coefficients 1 and 63.");
        }

        if (spectralEnd < spectralStart || spectralEnd > 63)
        {
            throw JpegInvalidDataException.Create("Progressive AC scan spectral end is invalid.");
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
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateSuccessiveLow(int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw JpegInvalidDataException.Create("Progressive successive low approximation must be between 0 and 15.");
        }
    }

    internal void ConfigureRestartInterval(int restartInterval)
    {
        BaselineDecoder.ConfigureRestartInterval(restartInterval);
    }

    internal void RecordMcuDecoded()
    {
        BaselineDecoder.RecordMcuDecoded();
    }

    internal void ConsumeRestartMarkerLookahead()
    {
        BaselineDecoder.ConsumeRestartMarkerLookahead();
        InitializeAliases();
    }

    internal void ResetDcPredictors()
    {
        BaselineDecoder.ResetDcPredictors();
    }
}
