using System.Collections.Generic;

namespace jpeg;

internal class JpegBaselineHuffmanEntropyEncoder
{
    internal JpegBitWriter BitWriter = new JpegBitWriter();
    internal int[] DcPredictors = new int[256];
    internal int RestartInterval;
    internal int McuCountSinceRestart;
    internal int NextRestartMarkerIndex;
    internal int RestartMarkerCount;

    internal JpegBaselineHuffmanEntropyEncoder()
    {
        BitWriter = new JpegBitWriter();
        DcPredictors = new int[256];
        ResetRestartState();
    }

    internal static JpegBaselineHuffmanEntropyEncoder FromBitWriter(JpegBitWriter bitWriter)
    {
        if (bitWriter == null)
        {
            throw new System.ArgumentNullException("bitWriter");
        }

        JpegBaselineHuffmanEntropyEncoder encoder = new JpegBaselineHuffmanEntropyEncoder();
        encoder.BitWriter = bitWriter;
        encoder.ResetRestartState();
        return encoder;
    }

    internal static JpegBaselineHuffmanEntropyEncoder FromStream(System.IO.Stream stream)
    {
        return FromBitWriter(JpegBitWriter.FromStream(stream));
    }

    internal void EncodeBlock(int[] coefficients, JpegHuffmanTable dcTable, JpegHuffmanTable acTable, int componentIndex)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateComponentIndex(componentIndex);
        ValidateTable(dcTable, 0);
        ValidateTable(acTable, 1);

        int difference = CalculateDifference(coefficients[0], DcPredictors[componentIndex]);
        int dcCategory = GetMagnitudeCategory(difference);
        ValidateDcCategory(dcCategory);
        EncodeSymbol(dcTable, dcCategory);
        WriteMagnitudeBits(difference, dcCategory);
        DcPredictors[componentIndex] = coefficients[0];

        EncodeAcCoefficients(coefficients, acTable);
    }

    internal void EncodeBlockForScanComponent(List<JpegHuffmanTable> tables, JpegScanComponent component, int[] coefficients, int componentIndex)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = FindTable(tables, 0, component.DcEntropyTableIdentifier);
        JpegHuffmanTable acTable = FindTable(tables, 1, component.AcEntropyTableIdentifier);
        EncodeBlock(coefficients, dcTable, acTable, componentIndex);
    }

    internal void EncodeAcCoefficients(int[] coefficients, JpegHuffmanTable acTable)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateTable(acTable, 1);

        int zeroRun = 0;
        int index = 1;
        while (index < JpegConstants.BlockSampleCount)
        {
            int coefficient = coefficients[index];
            if (coefficient == 0)
            {
                zeroRun = zeroRun + 1;
            }
            else
            {
                while (zeroRun > 15)
                {
                    EncodeSymbol(acTable, 0xF0);
                    zeroRun = zeroRun - 16;
                }

                int category = GetMagnitudeCategory(coefficient);
                ValidateAcCategory(category);
                EncodeSymbol(acTable, (zeroRun << 4) + category);
                WriteMagnitudeBits(coefficient, category);
                zeroRun = 0;
            }

            index = index + 1;
        }

        if (zeroRun > 0)
        {
            EncodeSymbol(acTable, 0);
        }
    }

    internal void EncodeSymbol(JpegHuffmanTable table, int symbol)
    {
        ValidateEncodedTable(table);
        int index = 0;
        while (index < table.SymbolCount)
        {
            if (table.Symbols[index] == symbol)
            {
                BitWriter.WriteBits(table.Codes[index], table.CodeLengths[index]);
                return;
            }

            index = index + 1;
        }

        throw JpegWriteValidationException.Create("Huffman table does not contain the required symbol.");
    }

    internal void WriteMagnitudeBits(int value, int category)
    {
        ValidateMagnitudeCategory(category);
        if (category == 0)
        {
            if (value != 0)
            {
                throw JpegWriteValidationException.Create("Zero magnitude category can only encode a zero value.");
            }

            return;
        }

        int bits = value;
        if (value < 0)
        {
            bits = value + (1 << category) - 1;
        }

        BitWriter.WriteBits(bits, category);
    }

    internal int GetMagnitudeCategory(int value)
    {
        if (value == 0)
        {
            return 0;
        }

        long magnitude = value;
        if (magnitude < 0)
        {
            magnitude = -magnitude;
        }

        int category = 0;
        while (magnitude > 0)
        {
            category = category + 1;
            magnitude = magnitude >> 1;
        }

        return category;
    }

    internal int CalculateDifference(int value, int predictor)
    {
        long difference = (long)value - (long)predictor;
        if (difference < int.MinValue || difference > int.MaxValue)
        {
            throw JpegWriteValidationException.Create("DC coefficient difference is outside the supported integer range.");
        }

        return (int)difference;
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

        throw JpegWriteValidationException.Create("Required Huffman table is missing.");
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
        NextRestartMarkerIndex = 0;
        RestartMarkerCount = 0;
    }

    internal void RecordMcuEncoded()
    {
        McuCountSinceRestart = McuCountSinceRestart + 1;
    }

    internal bool ShouldWriteRestartMarker()
    {
        if (RestartInterval <= 0)
        {
            return false;
        }

        return McuCountSinceRestart == RestartInterval;
    }

    internal void WriteRestartMarker()
    {
        if (!ShouldWriteRestartMarker())
        {
            throw JpegWriteValidationException.Create("Restart marker cannot be written before the configured interval.");
        }

        BitWriter.WriteMarkerAfterByteAlign(JpegConstants.MarkerRestart0 + NextRestartMarkerIndex);
        ResetDcPredictors();
        McuCountSinceRestart = 0;
        NextRestartMarkerIndex = (NextRestartMarkerIndex + 1) & 7;
        RestartMarkerCount = RestartMarkerCount + 1;
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

    internal void ValidateTable(JpegHuffmanTable table, int tableClass)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != tableClass)
        {
            throw JpegWriteValidationException.Create("Huffman table class does not match the entropy coding context.");
        }

        if (table.SymbolCount < 1)
        {
            throw JpegWriteValidationException.Create("Huffman table must contain at least one symbol.");
        }
    }

    internal void ValidateEncodedTable(JpegHuffmanTable table)
    {
        ValidateTable(table, table.TableClass);
        if (table.Symbols == null || table.Symbols.Length < table.SymbolCount)
        {
            throw JpegWriteValidationException.Create("Huffman table symbols are incomplete.");
        }

        if (table.Codes == null || table.Codes.Length < table.SymbolCount)
        {
            throw JpegWriteValidationException.Create("Huffman table codes are incomplete.");
        }

        if (table.CodeLengths == null || table.CodeLengths.Length < table.SymbolCount)
        {
            throw JpegWriteValidationException.Create("Huffman table code lengths are incomplete.");
        }
    }

    internal void ValidateDcCategory(int category)
    {
        if (category < 0 || category > 11)
        {
            throw JpegWriteValidationException.Create("Baseline DC magnitude category must be between 0 and 11.");
        }
    }

    internal void ValidateAcCategory(int category)
    {
        if (category < 1 || category > 10)
        {
            throw JpegWriteValidationException.Create("Baseline AC magnitude category must be between 1 and 10.");
        }
    }

    internal void ValidateMagnitudeCategory(int category)
    {
        if (category < 0 || category > 15)
        {
            throw JpegWriteValidationException.Create("Magnitude category must be between 0 and 15.");
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
