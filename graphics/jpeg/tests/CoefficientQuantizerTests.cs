namespace jpeg.Tests;

internal class CoefficientQuantizerTests
{
    [Test]
    public void Quantizer_QuantizesNaturalBlockWithSignPreservingRounding()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegQuantizationTable table = CreateTable(10);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 24;
        coefficients[1] = 25;
        coefficients[2] = 26;
        coefficients[3] = -24;
        coefficients[4] = -25;
        coefficients[5] = -26;

        int[] quantized = quantizer.QuantizeBlock(coefficients, table);

        Assert.That(quantized[0], Is.EqualTo(2));
        Assert.That(quantized[1], Is.EqualTo(3));
        Assert.That(quantized[2], Is.EqualTo(3));
        Assert.That(quantized[3], Is.EqualTo(-2));
        Assert.That(quantized[4], Is.EqualTo(-3));
        Assert.That(quantized[5], Is.EqualTo(-3));
    }

    [Test]
    public void Quantizer_DequantizesNaturalBlock()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegQuantizationTable table = CreateTable(7);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 2;
        coefficients[1] = -3;
        coefficients[2] = 0;

        int[] dequantized = quantizer.DequantizeBlock(coefficients, table);

        Assert.That(dequantized[0], Is.EqualTo(14));
        Assert.That(dequantized[1], Is.EqualTo(-21));
        Assert.That(dequantized[2], Is.EqualTo(0));
    }

    [Test]
    public void Quantizer_UsesZigZagTableValuesWhenPresent()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegDefaultQuantizationTables defaults = new global::jpeg.JpegDefaultQuantizationTables();
        global::jpeg.JpegQuantizationTable table = defaults.CreateLuminanceTable();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 32;
        coefficients[1] = 22;
        coefficients[2] = 30;

        int[] quantized = quantizer.QuantizeZigZagBlock(coefficients, table);
        int[] dequantized = quantizer.DequantizeZigZagBlock(quantized, table);

        Assert.That(table.ZigZagValues[2], Is.EqualTo(12));
        Assert.That(quantized[0], Is.EqualTo(2));
        Assert.That(quantized[1], Is.EqualTo(2));
        Assert.That(quantized[2], Is.EqualTo(3));
        Assert.That(dequantized[0], Is.EqualTo(32));
        Assert.That(dequantized[1], Is.EqualTo(22));
        Assert.That(dequantized[2], Is.EqualTo(36));
    }

    [Test]
    public void Quantizer_FallsBackToNaturalValuesForZigZagBlocks()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegQuantizationTable table = CreateSequentialTable();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[2] = 27;

        int[] quantized = quantizer.QuantizeZigZagBlock(coefficients, table);

        Assert.That(quantizer.HasZigZagValues(table), Is.False);
        Assert.That(quantizer.GetZigZagQuantizationValue(table, 2), Is.EqualTo(9));
        Assert.That(quantized[2], Is.EqualTo(3));
    }

    [Test]
    public void Quantizer_InPlaceMethodsModifyOnlyFirstBlock()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegQuantizationTable table = CreateTable(8);
        int[] coefficients = new int[70];
        coefficients[0] = 20;
        coefficients[1] = -20;
        coefficients[64] = 777;

        quantizer.QuantizeBlockInPlace(coefficients, table);

        Assert.That(coefficients[0], Is.EqualTo(3));
        Assert.That(coefficients[1], Is.EqualTo(-3));
        Assert.That(coefficients[64], Is.EqualTo(777));

        quantizer.DequantizeBlockInPlace(coefficients, table);

        Assert.That(coefficients[0], Is.EqualTo(24));
        Assert.That(coefficients[1], Is.EqualTo(-24));
        Assert.That(coefficients[64], Is.EqualTo(777));
    }

    [Test]
    public void Quantizer_ClampsLargeDequantizedCoefficients()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();

        Assert.That(quantizer.DequantizeCoefficient(int.MaxValue, 2), Is.EqualTo(int.MaxValue));
        Assert.That(quantizer.DequantizeCoefficient(int.MinValue, 2), Is.EqualTo(int.MinValue));
    }

    [Test]
    public void Quantizer_RejectsInvalidBlocksTablesAndValues()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegQuantizationTable table = CreateTable(1);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                quantizer.QuantizeBlock(null!, table);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                quantizer.DequantizeBlock(new int[1], table);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                quantizer.QuantizeBlock(coefficients, null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                quantizer.QuantizeBlock(coefficients, new global::jpeg.JpegQuantizationTable());
            });

        table.Values[0] = 0;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                quantizer.QuantizeBlock(coefficients, table);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                quantizer.GetNaturalQuantizationValue(CreateTable(1), 64);
            });
    }

    public static global::jpeg.JpegQuantizationTable CreateTable(int value)
    {
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTable();
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            table.Values[index] = value;
            index = index + 1;
        }

        return table;
    }

    public static global::jpeg.JpegQuantizationTable CreateSequentialTable()
    {
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTable();
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            table.Values[index] = index + 1;
            index = index + 1;
        }

        return table;
    }
}
