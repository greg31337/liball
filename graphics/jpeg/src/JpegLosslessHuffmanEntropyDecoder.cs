using System.Collections.Generic;

namespace jpeg;

internal class JpegLosslessHuffmanEntropyDecoder
{
    internal JpegBaselineHuffmanEntropyDecoder BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(new byte[0]);
    internal JpegBitReader BitReader = new JpegBitReader(new byte[0]);
    internal int MaximumDifferenceCategory = 16;

    internal JpegLosslessHuffmanEntropyDecoder(byte[] entropyData)
    {
        BaselineDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        InitializeAliases();
    }

    internal static JpegLosslessHuffmanEntropyDecoder FromBitReader(JpegBitReader bitReader)
    {
        if (bitReader == null)
        {
            throw new System.ArgumentNullException("bitReader");
        }

        JpegLosslessHuffmanEntropyDecoder decoder = new JpegLosslessHuffmanEntropyDecoder(new byte[0]);
        decoder.BaselineDecoder = JpegBaselineHuffmanEntropyDecoder.FromBitReader(bitReader);
        decoder.InitializeAliases();
        return decoder;
    }

    internal void InitializeAliases()
    {
        BitReader = BaselineDecoder.BitReader;
    }

    internal int DecodeDifference(JpegHuffmanTable dcTable)
    {
        BaselineDecoder.ValidateTable(dcTable, 0);
        int category = BaselineDecoder.DecodeSymbol(dcTable);
        ValidateDifferenceCategory(category);
        return ReceiveExtend(category);
    }

    internal int DecodeDifferenceForScanComponent(List<JpegHuffmanTable> tables, JpegScanComponent component)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineDecoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        return DecodeDifference(dcTable);
    }

    internal int ReceiveExtend(int size)
    {
        ValidateDifferenceCategory(size);
        if (size == 0)
        {
            return 0;
        }

        int value = BitReader.ReadBits(size);
        return ExtendValue(value, size);
    }

    internal int ExtendValue(int value, int size)
    {
        ValidateDifferenceCategory(size);
        if (size == 0)
        {
            return 0;
        }

        int threshold = 1 << (size - 1);
        if (value >= threshold)
        {
            return value;
        }

        return value - (1 << size) + 1;
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

    internal void ValidateDifferenceCategory(int category)
    {
        if (category < 0 || category > MaximumDifferenceCategory)
        {
            throw JpegInvalidDataException.Create("Lossless difference magnitude category exceeds the configured range.");
        }
    }
}
