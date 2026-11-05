using System.Collections.Generic;

namespace jpeg;

internal class JpegDefaultQuantizationTables
{
    internal int[] LuminanceValues = new int[]
    {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    };

    internal int[] ChrominanceValues = new int[]
    {
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99
    };

    internal JpegQuantizationTable CreateLuminanceTable()
    {
        return CreateLuminanceTableWithIdentifier(0);
    }

    internal JpegQuantizationTable CreateLuminanceTableWithIdentifier(int identifier)
    {
        return CreateTable(identifier, LuminanceValues);
    }

    internal JpegQuantizationTable CreateLuminanceTableWithQuality(int identifier, int quality)
    {
        return CreateTableWithQuality(identifier, LuminanceValues, quality);
    }

    internal JpegQuantizationTable CreateChrominanceTable()
    {
        return CreateChrominanceTableWithIdentifier(1);
    }

    internal JpegQuantizationTable CreateChrominanceTableWithIdentifier(int identifier)
    {
        return CreateTable(identifier, ChrominanceValues);
    }

    internal JpegQuantizationTable CreateChrominanceTableWithQuality(int identifier, int quality)
    {
        return CreateTableWithQuality(identifier, ChrominanceValues, quality);
    }

    internal List<JpegQuantizationTable> CreateDefaultTables()
    {
        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        tables.Add(CreateLuminanceTable());
        tables.Add(CreateChrominanceTable());
        return tables;
    }

    internal List<JpegQuantizationTable> CreateDefaultTablesWithQuality(int quality)
    {
        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        tables.Add(CreateLuminanceTableWithQuality(0, quality));
        tables.Add(CreateChrominanceTableWithQuality(1, quality));
        return tables;
    }

    internal List<JpegQuantizationTable> CreateDefaultTablesWithSeparateQuality(int luminanceQuality, int chrominanceQuality)
    {
        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        tables.Add(CreateLuminanceTableWithQuality(0, luminanceQuality));
        tables.Add(CreateChrominanceTableWithQuality(1, chrominanceQuality));
        return tables;
    }

    internal List<JpegQuantizationTable> CreateDefaultTablesWithOptions(JpegWriteOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        return CreateDefaultTablesWithSeparateQuality(ResolveLuminanceQuality(options), ResolveChrominanceQuality(options));
    }

    internal JpegQuantizationTable CreateTable(int identifier, int[] naturalValues)
    {
        ValidateIdentifier(identifier);
        ValidateNaturalValues(naturalValues);

        JpegQuantizationTable table = new JpegQuantizationTable();
        table.Identifier = identifier;
        table.Precision = 8;
        table.Is16Bit = false;
        CopyNaturalValues(naturalValues, table.Values);
        FillZigZagValues(table);
        return table;
    }

    internal JpegQuantizationTable CreateTableWithQuality(int identifier, int[] naturalValues, int quality)
    {
        ValidateQuality(quality);
        int[] scaledValues = ScaleNaturalValues(naturalValues, quality);
        return CreateTable(identifier, scaledValues);
    }

    internal int[] ScaleNaturalValues(int[] naturalValues, int quality)
    {
        ValidateNaturalValues(naturalValues);
        ValidateQuality(quality);

        int scale = CalculateQualityScale(quality);
        int[] scaledValues = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            scaledValues[index] = ScaleValue(naturalValues[index], scale);
            index = index + 1;
        }

        return scaledValues;
    }

    internal int CalculateQualityScale(int quality)
    {
        ValidateQuality(quality);
        if (quality < 50)
        {
            return 5000 / quality;
        }

        return 200 - quality * 2;
    }

    internal int ScaleValue(int value, int scale)
    {
        int scaledValue = (value * scale + 50) / 100;
        if (scaledValue < 1)
        {
            return 1;
        }

        if (scaledValue > 255)
        {
            return 255;
        }

        return scaledValue;
    }

    internal int ResolveLuminanceQuality(JpegWriteOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.LuminanceQuality != 0)
        {
            ValidateQuality(options.LuminanceQuality);
            return options.LuminanceQuality;
        }

        ValidateQuality(options.Quality);
        return options.Quality;
    }

    internal int ResolveChrominanceQuality(JpegWriteOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.ChrominanceQuality != 0)
        {
            ValidateQuality(options.ChrominanceQuality);
            return options.ChrominanceQuality;
        }

        ValidateQuality(options.Quality);
        return options.Quality;
    }

    internal void CopyNaturalValues(int[] source, int[] destination)
    {
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }

    internal void FillZigZagValues(JpegQuantizationTable table)
    {
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            table.ZigZagValues[index] = table.Values[JpegConstants.ZigZagOrder[index]];
            index = index + 1;
        }
    }

    internal void ValidateIdentifier(int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw new System.ArgumentOutOfRangeException("identifier");
        }
    }

    internal void ValidateQuality(int quality)
    {
        if (quality < 1 || quality > 100)
        {
            throw new System.ArgumentOutOfRangeException("quality");
        }
    }

    internal void ValidateNaturalValues(int[] naturalValues)
    {
        if (naturalValues == null)
        {
            throw new System.ArgumentNullException("naturalValues");
        }

        if (naturalValues.Length < JpegConstants.BlockSampleCount)
        {
            throw new System.ArgumentException("Quantization table values must contain 64 entries.", "naturalValues");
        }
    }
}
