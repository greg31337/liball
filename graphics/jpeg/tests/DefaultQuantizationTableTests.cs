using System.Collections.Generic;

namespace jpeg.Tests;

internal class DefaultQuantizationTableTests
{
    [Test]
    public void Defaults_CreateStandardLuminanceTableInNaturalAndZigZagOrder()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        global::jpeg.JpegQuantizationTable table = factory.CreateLuminanceTable();

        Assert.That(table.Identifier, Is.EqualTo(0));
        Assert.That(table.Precision, Is.EqualTo(8));
        Assert.That(table.Is16Bit, Is.False);
        Assert.That(table.Values[0], Is.EqualTo(16));
        Assert.That(table.Values[1], Is.EqualTo(11));
        Assert.That(table.Values[8], Is.EqualTo(12));
        Assert.That(table.Values[63], Is.EqualTo(99));
        Assert.That(table.ZigZagValues[0], Is.EqualTo(16));
        Assert.That(table.ZigZagValues[1], Is.EqualTo(11));
        Assert.That(table.ZigZagValues[2], Is.EqualTo(12));
        Assert.That(table.ZigZagValues[63], Is.EqualTo(99));
    }

    [Test]
    public void Defaults_CreateStandardChrominanceTableInNaturalAndZigZagOrder()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        global::jpeg.JpegQuantizationTable table = factory.CreateChrominanceTable();

        Assert.That(table.Identifier, Is.EqualTo(1));
        Assert.That(table.Precision, Is.EqualTo(8));
        Assert.That(table.Is16Bit, Is.False);
        Assert.That(table.Values[0], Is.EqualTo(17));
        Assert.That(table.Values[1], Is.EqualTo(18));
        Assert.That(table.Values[2], Is.EqualTo(24));
        Assert.That(table.Values[3], Is.EqualTo(47));
        Assert.That(table.Values[8], Is.EqualTo(18));
        Assert.That(table.Values[16], Is.EqualTo(24));
        Assert.That(table.Values[63], Is.EqualTo(99));
        Assert.That(table.ZigZagValues[0], Is.EqualTo(17));
        Assert.That(table.ZigZagValues[1], Is.EqualTo(18));
        Assert.That(table.ZigZagValues[2], Is.EqualTo(18));
        Assert.That(table.ZigZagValues[63], Is.EqualTo(99));
    }

    [Test]
    public void Defaults_CreateDefaultTableListWithConventionalIdentifiers()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        List<global::jpeg.JpegQuantizationTable> tables = factory.CreateDefaultTables();

        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(tables[0].Identifier, Is.EqualTo(0));
        Assert.That(tables[1].Identifier, Is.EqualTo(1));
        Assert.That(tables[0].Values[0], Is.EqualTo(16));
        Assert.That(tables[1].Values[0], Is.EqualTo(17));
    }

    [Test]
    public void Defaults_WriteAndParseThroughDqtPayload()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();
        List<global::jpeg.JpegQuantizationTable> tables = factory.CreateDefaultTables();
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        byte[] payload = writer.WritePayloads(tables);
        List<global::jpeg.JpegQuantizationTable> parsed = new global::jpeg.JpegQuantizationTableParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(130));
        Assert.That(payload[0], Is.EqualTo(0));
        Assert.That(payload[65], Is.EqualTo(1));
        Assert.That(parsed.Count, Is.EqualTo(2));
        Assert.That(parsed[0].Values, Is.EqualTo(tables[0].Values));
        Assert.That(parsed[1].Values, Is.EqualTo(tables[1].Values));
        Assert.That(parsed[0].ZigZagValues, Is.EqualTo(tables[0].ZigZagValues));
        Assert.That(parsed[1].ZigZagValues, Is.EqualTo(tables[1].ZigZagValues));
    }

    [Test]
    public void Defaults_QualityFiftyKeepsBaseTables()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        List<global::jpeg.JpegQuantizationTable> baseTables = factory.CreateDefaultTables();
        List<global::jpeg.JpegQuantizationTable> scaledTables = factory.CreateDefaultTablesWithQuality(50);

        Assert.That(scaledTables[0].Values, Is.EqualTo(baseTables[0].Values));
        Assert.That(scaledTables[1].Values, Is.EqualTo(baseTables[1].Values));
        Assert.That(scaledTables[0].ZigZagValues, Is.EqualTo(baseTables[0].ZigZagValues));
        Assert.That(scaledTables[1].ZigZagValues, Is.EqualTo(baseTables[1].ZigZagValues));
    }

    [Test]
    public void Defaults_QualityScalingUsesJpegFormula()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        global::jpeg.JpegQuantizationTable luminance75 = factory.CreateLuminanceTableWithQuality(0, 75);
        global::jpeg.JpegQuantizationTable chrominance75 = factory.CreateChrominanceTableWithQuality(1, 75);
        global::jpeg.JpegQuantizationTable luminance25 = factory.CreateLuminanceTableWithQuality(0, 25);

        Assert.That(factory.CalculateQualityScale(75), Is.EqualTo(50));
        Assert.That(factory.CalculateQualityScale(25), Is.EqualTo(200));
        Assert.That(luminance75.Values[0], Is.EqualTo(8));
        Assert.That(luminance75.Values[1], Is.EqualTo(6));
        Assert.That(luminance75.Values[5], Is.EqualTo(20));
        Assert.That(luminance75.Values[63], Is.EqualTo(50));
        Assert.That(luminance75.ZigZagValues[2], Is.EqualTo(6));
        Assert.That(chrominance75.Values[0], Is.EqualTo(9));
        Assert.That(chrominance75.Values[3], Is.EqualTo(24));
        Assert.That(chrominance75.Values[63], Is.EqualTo(50));
        Assert.That(luminance25.Values[0], Is.EqualTo(32));
        Assert.That(luminance25.Values[1], Is.EqualTo(22));
        Assert.That(luminance25.Values[62], Is.EqualTo(206));
    }

    [Test]
    public void Defaults_QualityScalingClampsToJpegValueRange()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        global::jpeg.JpegQuantizationTable lowestQuality = factory.CreateLuminanceTableWithQuality(0, 1);
        global::jpeg.JpegQuantizationTable highestQuality = factory.CreateChrominanceTableWithQuality(1, 100);

        Assert.That(lowestQuality.Values[0], Is.EqualTo(255));
        Assert.That(lowestQuality.Values[63], Is.EqualTo(255));
        Assert.That(highestQuality.Values[0], Is.EqualTo(1));
        Assert.That(highestQuality.Values[63], Is.EqualTo(1));
        Assert.That(highestQuality.ZigZagValues[0], Is.EqualTo(1));
        Assert.That(highestQuality.ZigZagValues[63], Is.EqualTo(1));
    }

    [Test]
    public void Defaults_WriteAndParseScaledTablesThroughDqtPayload()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();
        List<global::jpeg.JpegQuantizationTable> tables = factory.CreateDefaultTablesWithQuality(75);
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        byte[] payload = writer.WritePayloads(tables);
        List<global::jpeg.JpegQuantizationTable> parsed = new global::jpeg.JpegQuantizationTableParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(130));
        Assert.That(parsed[0].Values, Is.EqualTo(tables[0].Values));
        Assert.That(parsed[1].Values, Is.EqualTo(tables[1].Values));
        Assert.That(parsed[0].ZigZagValues, Is.EqualTo(tables[0].ZigZagValues));
        Assert.That(parsed[1].ZigZagValues, Is.EqualTo(tables[1].ZigZagValues));
    }

    [Test]
    public void Defaults_CreateDefaultTablesWithSeparateComponentQualities()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        List<global::jpeg.JpegQuantizationTable> tables = factory.CreateDefaultTablesWithSeparateQuality(90, 30);

        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(tables[0].Identifier, Is.EqualTo(0));
        Assert.That(tables[1].Identifier, Is.EqualTo(1));
        Assert.That(tables[0].Values[0], Is.EqualTo(3));
        Assert.That(tables[0].Values[1], Is.EqualTo(2));
        Assert.That(tables[0].Values[63], Is.EqualTo(20));
        Assert.That(tables[0].ZigZagValues[2], Is.EqualTo(2));
        Assert.That(tables[1].Values[0], Is.EqualTo(28));
        Assert.That(tables[1].Values[3], Is.EqualTo(78));
        Assert.That(tables[1].Values[63], Is.EqualTo(164));
        Assert.That(tables[1].ZigZagValues[2], Is.EqualTo(30));
    }

    [Test]
    public void Defaults_CreateDefaultTablesFromWriteOptions()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Quality = 25;
        options.ChrominanceQuality = 100;

        List<global::jpeg.JpegQuantizationTable> fallbackTables = factory.CreateDefaultTablesWithOptions(options);

        Assert.That(factory.ResolveLuminanceQuality(options), Is.EqualTo(25));
        Assert.That(factory.ResolveChrominanceQuality(options), Is.EqualTo(100));
        Assert.That(fallbackTables[0].Values[0], Is.EqualTo(32));
        Assert.That(fallbackTables[0].Values[63], Is.EqualTo(198));
        Assert.That(fallbackTables[1].Values[0], Is.EqualTo(1));
        Assert.That(fallbackTables[1].Values[63], Is.EqualTo(1));

        options.LuminanceQuality = 60;
        options.ChrominanceQuality = 90;
        List<global::jpeg.JpegQuantizationTable> separateTables = factory.CreateDefaultTablesWithOptions(options);

        Assert.That(factory.ResolveLuminanceQuality(options), Is.EqualTo(60));
        Assert.That(factory.ResolveChrominanceQuality(options), Is.EqualTo(90));
        Assert.That(separateTables[0].Values[0], Is.EqualTo(13));
        Assert.That(separateTables[1].Values[0], Is.EqualTo(3));
    }

    [Test]
    public void Defaults_CopyArraysAndRejectInvalidInputs()
    {
        global::jpeg.JpegDefaultQuantizationTables factory = new global::jpeg.JpegDefaultQuantizationTables();

        global::jpeg.JpegQuantizationTable table = factory.CreateLuminanceTable();
        table.Values[0] = 1;
        global::jpeg.JpegQuantizationTable second = factory.CreateLuminanceTable();

        Assert.That(second.Values[0], Is.EqualTo(16));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateLuminanceTableWithIdentifier(-1);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateChrominanceTableWithIdentifier(4);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                factory.CreateTable(0, null!);
            });
        Assert.Throws<System.ArgumentException>(
            delegate
            {
                factory.CreateTable(0, new int[63]);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateDefaultTablesWithQuality(0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateLuminanceTableWithQuality(0, 101);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateDefaultTablesWithSeparateQuality(0, 75);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateDefaultTablesWithSeparateQuality(75, 101);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                factory.CreateDefaultTablesWithOptions((global::jpeg.JpegWriteOptions)null!);
            });

        global::jpeg.JpegWriteOptions invalidFallback = new global::jpeg.JpegWriteOptions();
        invalidFallback.Quality = 0;
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateDefaultTablesWithOptions(invalidFallback);
            });

        global::jpeg.JpegWriteOptions invalidComponent = new global::jpeg.JpegWriteOptions();
        invalidComponent.LuminanceQuality = 101;
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                factory.CreateDefaultTablesWithOptions(invalidComponent);
            });
    }
}
