namespace jpeg.Tests;

internal class AbbreviatedStreamApiTests
{
    [Test]
    public void TableStreamApi_WritesAndReadsAllTableKinds()
    {
        global::jpeg.JpegTableSet source = CreateBaselineTableSet();
        global::jpeg.JpegArithmeticConditioningTable arithmeticTable = new global::jpeg.JpegArithmeticConditioningTable();
        arithmeticTable.TableClass = 0;
        arithmeticTable.Identifier = 0;
        arithmeticTable.Lower = 3;
        arithmeticTable.Upper = 10;
        arithmeticTable.ConditioningValue = 0xA3;
        source.ArithmeticConditioningTables.Add(arithmeticTable);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();

        byte[] data = inspector.WriteTableStream(source);
        global::jpeg.JpegTableSet result = inspector.ReadTableStream(data);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);

        Assert.That(info.IsAbbreviatedTableStream, Is.True);
        Assert.That(info.SawStartOfFrame, Is.False);
        Assert.That(info.SawStartOfScan, Is.False);
        Assert.That(result.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(result.HuffmanTables.Count, Is.EqualTo(2));
        Assert.That(result.ArithmeticConditioningTables.Count, Is.EqualTo(1));
        Assert.That(result.QuantizationTables[0].Values[0], Is.EqualTo(source.QuantizationTables[0].Values[0]));
        Assert.That(result.HuffmanTables[0].Symbols, Is.EqualTo(source.HuffmanTables[0].Symbols));
        Assert.That(result.ArithmeticConditioningTables[0].Lower, Is.EqualTo(3));
        Assert.That(result.ArithmeticConditioningTables[0].Upper, Is.EqualTo(10));
    }

    [Test]
    public void AbbreviatedImageApi_EncodesWithoutDefinitionsAndDecodesWithSuppliedTables()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        writeOptions.RestartInterval = 1;
        byte[] complete = writer.WriteWithOptions(source, writeOptions);
        global::jpeg.JpegTableSet tables = CreateTableSetFromJpeg(complete);

        byte[] data = writer.WriteAbbreviatedImageWithOptions(source, tables, writeOptions);
        global::jpeg.JpegInfo withoutTables = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.L8;
        global::jpeg.JpegImage result = reader.ReadAbbreviatedImageWithOptions(data, tables, readOptions);
        global::jpeg.JpegImage defaultResult = reader.ReadAbbreviatedImage(data, tables);

        Assert.That(withoutTables.QuantizationTableCount, Is.EqualTo(0));
        Assert.That(withoutTables.HuffmanTableCount, Is.EqualTo(0));
        Assert.That(withoutTables.HasRestartInterval, Is.True);
        Assert.That(withoutTables.RestartInterval, Is.EqualTo(1));
        Assert.That(result.Width, Is.EqualTo(16));
        Assert.That(result.Height, Is.EqualTo(8));
        Assert.That(result.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(result.Pixels[0], Is.EqualTo(138));
        Assert.That(result.Pixels[8], Is.EqualTo(143));
        Assert.That(defaultResult.Pixels, Is.EqualTo(result.Pixels));
    }

    [Test]
    public void AbbreviatedImageApi_RejectsMissingRequiredTables()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        global::jpeg.JpegTableSet empty = new global::jpeg.JpegTableSet();
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteAbbreviatedImage(source, empty);
            });
    }

    internal static global::jpeg.JpegTableSet CreateBaselineTableSet()
    {
        byte[] complete = new global::jpeg.JpegWriter().Write(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128));
        return CreateTableSetFromJpeg(complete);
    }

    internal static global::jpeg.JpegTableSet CreateTableSetFromJpeg(byte[] complete)
    {
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(complete);
        global::jpeg.JpegTableSet tables = new global::jpeg.JpegTableSet();
        tables.QuantizationTables.Add(info.QuantizationTables[0]);
        tables.HuffmanTables.Add(info.HuffmanTables[0]);
        tables.HuffmanTables.Add(info.HuffmanTables[1]);
        return tables;
    }
}
