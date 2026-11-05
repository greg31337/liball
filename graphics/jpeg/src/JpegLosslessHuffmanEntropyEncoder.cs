using System.Collections.Generic;

namespace jpeg;

internal class JpegLosslessHuffmanEntropyEncoder
{
    internal JpegBaselineHuffmanEntropyEncoder BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
    internal JpegBitWriter BitWriter = new JpegBitWriter();
    internal int MaximumDifferenceCategory = 16;

    internal JpegLosslessHuffmanEntropyEncoder()
    {
        BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
        InitializeAliases();
    }

    internal static JpegLosslessHuffmanEntropyEncoder FromBitWriter(JpegBitWriter bitWriter)
    {
        if (bitWriter == null)
        {
            throw new System.ArgumentNullException("bitWriter");
        }

        JpegLosslessHuffmanEntropyEncoder encoder = new JpegLosslessHuffmanEntropyEncoder();
        encoder.BaselineEncoder = JpegBaselineHuffmanEntropyEncoder.FromBitWriter(bitWriter);
        encoder.InitializeAliases();
        return encoder;
    }

    internal void InitializeAliases()
    {
        BitWriter = BaselineEncoder.BitWriter;
    }

    internal void EncodeDifference(JpegHuffmanTable dcTable, int difference)
    {
        BaselineEncoder.ValidateTable(dcTable, 0);
        int category = GetMagnitudeCategory(difference);
        ValidateDifferenceCategory(category);
        BaselineEncoder.EncodeSymbol(dcTable, category);
        WriteMagnitudeBits(difference, category);
    }

    internal void EncodeDifferenceForScanComponent(List<JpegHuffmanTable> tables, JpegScanComponent component, int difference)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineEncoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        EncodeDifference(dcTable, difference);
    }

    internal void WriteMagnitudeBits(int value, int category)
    {
        ValidateDifferenceCategory(category);
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

    internal byte[] ToArray()
    {
        return BitWriter.ToArray();
    }

    internal void FlushToByteBoundary()
    {
        BitWriter.FlushToByteBoundary();
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
        InitializeAliases();
    }

    internal void ValidateDifferenceCategory(int category)
    {
        if (category < 0 || category > MaximumDifferenceCategory)
        {
            throw JpegWriteValidationException.Create("Lossless difference magnitude category exceeds the configured range.");
        }
    }
}
