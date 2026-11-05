using System.Collections.Generic;

namespace jpeg;

internal class JpegBaselineHuffmanEntropyDecoder
{
    internal JpegBitReader BitReader = new JpegBitReader(new byte[0]);
    internal JpegHuffmanTableParser HuffmanParser = new JpegHuffmanTableParser();
    internal int[] DcPredictors = new int[256];
    internal int RestartInterval;
    internal int McuCountSinceRestart;
    internal int ExpectedRestartMarkerIndex;
    internal int RestartMarkerCount;

    internal JpegBaselineHuffmanEntropyDecoder(byte[] entropyData)
    {
        BitReader = new JpegBitReader(entropyData);
        HuffmanParser = new JpegHuffmanTableParser();
        DcPredictors = new int[256];
        ResetRestartState();
    }

    internal static JpegBaselineHuffmanEntropyDecoder FromStream(System.IO.Stream stream)
    {
        return FromBitReader(JpegBitReader.FromStream(stream));
    }

    internal static JpegBaselineHuffmanEntropyDecoder FromBitReader(JpegBitReader bitReader)
    {
        if (bitReader == null)
        {
            throw new System.ArgumentNullException("bitReader");
        }

        JpegBaselineHuffmanEntropyDecoder decoder = new JpegBaselineHuffmanEntropyDecoder(new byte[0]);
        decoder.BitReader = bitReader;
        decoder.ResetRestartState();
        return decoder;
    }

    internal int[] DecodeBlock(JpegHuffmanTable dcTable, JpegHuffmanTable acTable, int componentIndex)
    {
        ValidateComponentIndex(componentIndex);
        ValidateTable(dcTable, 0);
        ValidateTable(acTable, 1);

        int[] coefficients = new int[JpegConstants.BlockSampleCount];
        int dcCategory = DecodeSymbol(dcTable);
        ValidateDcCategory(dcCategory);
        int dcDifference = ReceiveExtend(dcCategory);
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

        JpegHuffmanTable dcTable = FindTable(tables, 0, component.DcEntropyTableIdentifier);
        JpegHuffmanTable acTable = FindTable(tables, 1, component.AcEntropyTableIdentifier);
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
            int symbol = DecodeSymbol(acTable);
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

                throw JpegInvalidDataException.Create("Baseline AC Huffman symbol has an invalid zero-size run.");
            }

            index = index + runLength;
            if (index >= JpegConstants.BlockSampleCount)
            {
                throw JpegInvalidDataException.Create("Baseline AC Huffman run exceeds the coefficient block.");
            }

            coefficients[index] = ReceiveExtend(size);
            index = index + 1;
        }
    }

    internal int DecodeSymbol(JpegHuffmanTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        int code = 0;
        int length = 1;
        while (length <= 16)
        {
            code = (code << 1) | BitReader.ReadBit();
            int symbol = HuffmanParser.DecodeSymbol(table, code, length);
            if (symbol >= 0)
            {
                return symbol;
            }

            length = length + 1;
        }

        throw JpegInvalidDataException.Create("Entropy data does not match any Huffman code.");
    }

    internal int ReceiveExtend(int size)
    {
        ValidateMagnitudeCategory(size);
        if (size == 0)
        {
            return 0;
        }

        int value = BitReader.ReadBits(size);
        return ExtendValue(value, size);
    }

    internal int ExtendValue(int value, int size)
    {
        ValidateMagnitudeCategory(size);
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

    internal JpegHuffmanTable FindTable(List<JpegHuffmanTable> tables, int tableClass, int identifier)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        foreach (JpegHuffmanTable table in tables)
        {
            if (table.TableClass == tableClass && table.Identifier == identifier)
            {
                return table;
            }
        }

        throw JpegInvalidDataException.Create("Required Huffman table is missing.");
    }

    internal void ResetDcPredictors()
    {
        int index = 0;
        while (index < DcPredictors.Length)
        {
            DcPredictors[index] = 0;
            index = index + 1;
        }
    }

    internal void ConfigureRestartInterval(int restartInterval)
    {
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }

        RestartInterval = restartInterval;
        ResetRestartState();
    }

    internal void ResetRestartState()
    {
        McuCountSinceRestart = 0;
        ExpectedRestartMarkerIndex = 0;
        RestartMarkerCount = 0;
    }

    internal void RecordMcuDecoded()
    {
        McuCountSinceRestart = McuCountSinceRestart + 1;
    }

    internal bool ShouldExpectRestartMarker()
    {
        if (RestartInterval <= 0)
        {
            return false;
        }

        return McuCountSinceRestart == RestartInterval;
    }

    internal bool IsRestartMarker(JpegScannedMarker marker)
    {
        if (marker == null)
        {
            throw new System.ArgumentNullException("marker");
        }

        return marker.Value >= JpegConstants.MarkerRestart0 && marker.Value <= JpegConstants.MarkerRestart7;
    }

    internal void ValidateRestartMarker(JpegScannedMarker marker)
    {
        if (!IsRestartMarker(marker))
        {
            throw JpegInvalidDataException.Create("Expected a JPEG restart marker.");
        }

        if (RestartInterval <= 0)
        {
            throw JpegInvalidDataException.Create("Restart marker appeared when no restart interval is configured.");
        }

        if (McuCountSinceRestart != RestartInterval)
        {
            throw JpegInvalidDataException.Create("Restart marker appeared before the configured restart interval.");
        }

        int markerIndex = marker.Value - JpegConstants.MarkerRestart0;
        if (markerIndex != ExpectedRestartMarkerIndex)
        {
            throw JpegInvalidDataException.Create("Restart marker sequence is out of order.");
        }
    }

    internal void AcceptRestartMarker(JpegScannedMarker marker)
    {
        ValidateRestartMarker(marker);
        ResetDcPredictors();
        McuCountSinceRestart = 0;
        ExpectedRestartMarkerIndex = (ExpectedRestartMarkerIndex + 1) & 7;
        RestartMarkerCount = RestartMarkerCount + 1;
    }

    internal void ConsumeRestartMarkerLookahead()
    {
        JpegScannedMarker marker = BitReader.GetMarkerLookahead();
        AcceptRestartMarker(marker);
        BitReader.ConsumeMarkerLookahead();
    }

    internal void ValidateTable(JpegHuffmanTable table, int tableClass)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != tableClass)
        {
            throw JpegInvalidDataException.Create("Huffman table class does not match the entropy coding context.");
        }

        if (table.SymbolCount < 1)
        {
            throw JpegInvalidDataException.Create("Huffman table must contain at least one symbol.");
        }
    }

    internal void ValidateDcCategory(int category)
    {
        if (category < 0 || category > 11)
        {
            throw JpegInvalidDataException.Create("Baseline DC magnitude category must be between 0 and 11.");
        }
    }

    internal void ValidateMagnitudeCategory(int category)
    {
        if (category < 0 || category > 15)
        {
            throw JpegInvalidDataException.Create("Magnitude category must be between 0 and 15.");
        }
    }

    internal void ValidateComponentIndex(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= DcPredictors.Length)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }
    }
}
