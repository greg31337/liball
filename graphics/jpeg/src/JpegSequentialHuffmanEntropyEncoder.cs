using System.Collections.Generic;

namespace jpeg;

internal class JpegSequentialHuffmanEntropyEncoder
{
    internal JpegBaselineHuffmanEntropyEncoder BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
    internal JpegBitWriter BitWriter = new JpegBitWriter();
    internal int[] DcPredictors = new int[256];
    internal int MaximumDcCategory = 15;
    internal int MaximumAcCategory = 15;

    internal JpegSequentialHuffmanEntropyEncoder()
    {
        BaselineEncoder = new JpegBaselineHuffmanEntropyEncoder();
        InitializeAliases();
    }

    internal void InitializeAliases()
    {
        BitWriter = BaselineEncoder.BitWriter;
        DcPredictors = BaselineEncoder.DcPredictors;
    }

    internal void EncodeBlock(int[] coefficients, JpegHuffmanTable dcTable, JpegHuffmanTable acTable, int componentIndex)
    {
        BaselineEncoder.ValidateCoefficientBlock(coefficients);
        BaselineEncoder.ValidateComponentIndex(componentIndex);
        BaselineEncoder.ValidateTable(dcTable, 0);
        BaselineEncoder.ValidateTable(acTable, 1);

        int difference = BaselineEncoder.CalculateDifference(coefficients[0], DcPredictors[componentIndex]);
        int dcCategory = BaselineEncoder.GetMagnitudeCategory(difference);
        ValidateDcCategory(dcCategory);
        BaselineEncoder.EncodeSymbol(dcTable, dcCategory);
        BaselineEncoder.WriteMagnitudeBits(difference, dcCategory);
        DcPredictors[componentIndex] = coefficients[0];
        EncodeAcCoefficients(coefficients, acTable);
    }

    internal void EncodeBlockForScanComponent(List<JpegHuffmanTable> tables, JpegScanComponent component, int[] coefficients, int componentIndex)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegHuffmanTable dcTable = BaselineEncoder.FindTable(tables, 0, component.DcEntropyTableIdentifier);
        JpegHuffmanTable acTable = BaselineEncoder.FindTable(tables, 1, component.AcEntropyTableIdentifier);
        EncodeBlock(coefficients, dcTable, acTable, componentIndex);
    }

    internal void EncodeAcCoefficients(int[] coefficients, JpegHuffmanTable acTable)
    {
        BaselineEncoder.ValidateCoefficientBlock(coefficients);
        BaselineEncoder.ValidateTable(acTable, 1);

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
                    BaselineEncoder.EncodeSymbol(acTable, 0xF0);
                    zeroRun = zeroRun - 16;
                }

                int category = BaselineEncoder.GetMagnitudeCategory(coefficient);
                ValidateAcCategory(category);
                BaselineEncoder.EncodeSymbol(acTable, (zeroRun << 4) + category);
                BaselineEncoder.WriteMagnitudeBits(coefficient, category);
                zeroRun = 0;
            }

            index = index + 1;
        }

        if (zeroRun > 0)
        {
            BaselineEncoder.EncodeSymbol(acTable, 0);
        }
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

    internal void ValidateDcCategory(int category)
    {
        if (category < 0 || category > MaximumDcCategory)
        {
            throw JpegWriteValidationException.Create("Sequential DC magnitude category exceeds the configured range.");
        }
    }

    internal void ValidateAcCategory(int category)
    {
        if (category < 1 || category > MaximumAcCategory)
        {
            throw JpegWriteValidationException.Create("Sequential AC magnitude category exceeds the configured range.");
        }
    }
}
