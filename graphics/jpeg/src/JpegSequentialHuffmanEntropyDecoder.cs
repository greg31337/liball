using System.Collections.Generic;

namespace jpeg;

internal class JpegSequentialHuffmanEntropyDecoder
{
    internal JpegBaselineHuffmanEntropyDecoder BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(new byte[0]);
    internal JpegBitReader BitReader = new JpegBitReader(new byte[0]);
    internal int[] DcPredictors = new int[256];
    internal int MaximumDcCategory = 15;
    internal int MaximumAcCategory = 15;

    internal JpegSequentialHuffmanEntropyDecoder(byte[] entropyData)
    {
        BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        InitializeAliases();
    }

    internal static JpegSequentialHuffmanEntropyDecoder FromBitReader(JpegBitReader bitReader)
    {
        if (bitReader == null)
        {
            throw new System.ArgumentNullException("bitReader");
        }

        JpegSequentialHuffmanEntropyDecoder decoder = new JpegSequentialHuffmanEntropyDecoder(new byte[0]);
        decoder.BaselineDecoder = JpegBaselineHuffmanEntropyDecoder.FromBitReader(bitReader);
        decoder.InitializeAliases();
        return decoder;
    }

    internal void InitializeAliases()
    {
        BitReader = BaselineDecoder.BitReader;
        DcPredictors = BaselineDecoder.DcPredictors;
    }

    internal int[] DecodeBlock(JpegHuffmanTable dcTable, JpegHuffmanTable acTable, int componentIndex)
    {
        BaselineDecoder.ValidateComponentIndex(componentIndex);
        BaselineDecoder.ValidateTable(dcTable, 0);
        BaselineDecoder.ValidateTable(acTable, 1);

        int[] coefficients = new int[JpegConstants.BlockSampleCount];
        int dcCategory = BaselineDecoder.DecodeSymbol(dcTable);
        ValidateDcCategory(dcCategory);
        int dcDifference = BaselineDecoder.ReceiveExtend(dcCategory);
        int dcValue = DcPredictors[componentIndex] + dcDifference;
        DcPredictors[componentIndex] = dcValue;
        coefficients[0] = dcValue;
        DecodeAcCoefficients(coefficients, acTable);
        return coefficients;
    }

    internal int[] DecodeBlockForScanComponent(List<JpegHuffmanTable> tables, JpegScanComponent component, int componentIndex)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineDecoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        JpegHuffmanTable acTable = BaselineDecoder.FindTable(tables, 1, component.AcEntropyTableIdentifier);
        return DecodeBlock(dcTable, acTable, componentIndex);
    }

    internal void DecodeAcCoefficients(int[] coefficients, JpegHuffmanTable acTable)
    {
        if (coefficients == null)
        {
            throw new System.ArgumentNullException("coefficients");
        }

        if (coefficients.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }

        int index = 1;
        while (index < JpegConstants.BlockSampleCount)
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

                throw JpegInvalidDataException.Create("Sequential AC Huffman symbol has an invalid zero-size run.");
            }

            ValidateAcCategory(size);
            index = index + runLength;
            if (index >= JpegConstants.BlockSampleCount)
            {
                throw JpegInvalidDataException.Create("Sequential AC Huffman run exceeds the coefficient block.");
            }

            coefficients[index] = BaselineDecoder.ReceiveExtend(size);
            index = index + 1;
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

    internal bool ShouldExpectRestartMarker()
    {
        return BaselineDecoder.ShouldExpectRestartMarker();
    }

    internal void ConsumeRestartMarkerLookahead()
    {
        BaselineDecoder.ConsumeRestartMarkerLookahead();
        InitializeAliases();
    }

    internal void ValidateDcCategory(int category)
    {
        if (category < 0 || category > MaximumDcCategory)
        {
            throw JpegInvalidDataException.Create("Sequential DC magnitude category exceeds the configured range.");
        }
    }

    internal void ValidateAcCategory(int category)
    {
        if (category < 1 || category > MaximumAcCategory)
        {
            throw JpegInvalidDataException.Create("Sequential AC magnitude category exceeds the configured range.");
        }
    }
}
