namespace jpeg.Tests;

internal class HuffmanTableGeneratorTests
{
    [Test]
    public void Generator_BuildsSingleSymbolTableWithOneBitCode()
    {
        global::jpeg.JpegHuffmanStatistics statistics = new global::jpeg.JpegHuffmanStatistics();
        statistics.TableClass = 0;
        statistics.Identifier = 2;
        statistics.Counts[5] = 12;
        statistics.TotalCount = 12;
        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();

        global::jpeg.JpegHuffmanTable table = generator.GenerateTable(statistics);

        Assert.That(table.TableClass, Is.EqualTo(0));
        Assert.That(table.Identifier, Is.EqualTo(2));
        Assert.That(table.SymbolCount, Is.EqualTo(1));
        Assert.That(table.CodeCounts[0], Is.EqualTo(1));
        Assert.That(table.Symbols, Is.EqualTo(new int[] { 5 }));
        Assert.That(table.CodeLengths[0], Is.EqualTo(1));
        Assert.That(table.Codes[0], Is.EqualTo(0));
    }

    [Test]
    public void Generator_AssignsShorterCodesToMoreFrequentSymbols()
    {
        global::jpeg.JpegHuffmanStatistics statistics = new global::jpeg.JpegHuffmanStatistics();
        statistics.TableClass = 1;
        statistics.Identifier = 1;
        statistics.Counts[0x01] = 100;
        statistics.Counts[0x02] = 50;
        statistics.Counts[0x03] = 25;
        statistics.Counts[0x04] = 1;
        statistics.TotalCount = 176;
        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();

        global::jpeg.JpegHuffmanTable table = generator.GenerateTable(statistics);

        Assert.That(table.TableClass, Is.EqualTo(1));
        Assert.That(table.Identifier, Is.EqualTo(1));
        Assert.That(table.SymbolCount, Is.EqualTo(4));
        Assert.That(GetCodeLength(table, 0x01), Is.LessThanOrEqualTo(GetCodeLength(table, 0x02)));
        Assert.That(GetCodeLength(table, 0x02), Is.LessThanOrEqualTo(GetCodeLength(table, 0x03)));
        Assert.That(GetCodeLength(table, 0x03), Is.LessThanOrEqualTo(GetCodeLength(table, 0x04)));
        Assert.That(table.Symbols[0], Is.EqualTo(0x01));
    }

    [Test]
    public void Generator_LimitsOptimizedCodesToJpegMaximumLength()
    {
        global::jpeg.JpegHuffmanStatistics statistics = new global::jpeg.JpegHuffmanStatistics();
        statistics.TableClass = 1;
        statistics.Identifier = 0;
        int first = 1;
        int second = 1;
        int index = 0;
        while (index < 24)
        {
            statistics.Counts[index] = first;
            statistics.TotalCount = statistics.TotalCount + first;
            int next = first + second;
            first = second;
            second = next;
            index = index + 1;
        }

        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();

        global::jpeg.JpegHuffmanTable table = generator.GenerateTable(statistics);

        Assert.That(table.SymbolCount, Is.EqualTo(24));
        index = 0;
        while (index < table.CodeLengths.Length)
        {
            Assert.That(table.CodeLengths[index], Is.LessThanOrEqualTo(16));
            index = index + 1;
        }

        new global::jpeg.JpegHuffmanTableWriter().ValidateTable(table);
    }

    [Test]
    public void Generator_WritesAndParsesGeneratedCanonicalTable()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 5;
        coefficients[1] = 3;
        coefficients[20] = -1;

        collector.CollectBlock(coefficients, 0, dcStatistics, acStatistics);
        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();
        global::jpeg.JpegHuffmanTable table = generator.GenerateTable(acStatistics);

        byte[] payload = new global::jpeg.JpegHuffmanTableWriter().WritePayload(table);
        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> parsed = new global::jpeg.JpegHuffmanTableParser().Parse(payload);

        Assert.That(parsed.Count, Is.EqualTo(1));
        Assert.That(parsed[0].TableClass, Is.EqualTo(1));
        Assert.That(parsed[0].Symbols, Is.EqualTo(table.Symbols));
        Assert.That(GetCodeLength(parsed[0], 0x02), Is.GreaterThan(0));
        Assert.That(GetCodeLength(parsed[0], 0xF0), Is.GreaterThan(0));
        Assert.That(GetCodeLength(parsed[0], 0x21), Is.GreaterThan(0));
        Assert.That(GetCodeLength(parsed[0], 0x00), Is.GreaterThan(0));
    }

    [Test]
    public void Generator_RejectsInvalidStatistics()
    {
        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                generator.GenerateTable(null!);
            });

        global::jpeg.JpegHuffmanStatistics empty = new global::jpeg.JpegHuffmanStatistics();
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                generator.GenerateTable(empty);
            });

        global::jpeg.JpegHuffmanStatistics badClass = new global::jpeg.JpegHuffmanStatistics();
        badClass.TableClass = 2;
        badClass.Counts[0] = 1;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                generator.GenerateTable(badClass);
            });

        global::jpeg.JpegHuffmanStatistics badCounts = new global::jpeg.JpegHuffmanStatistics();
        badCounts.Counts = new int[255];
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                generator.GenerateTable(badCounts);
            });

        global::jpeg.JpegHuffmanStatistics negative = new global::jpeg.JpegHuffmanStatistics();
        negative.Counts[2] = -1;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                generator.GenerateTable(negative);
            });
    }

    public int GetCodeLength(global::jpeg.JpegHuffmanTable table, int symbol)
    {
        int index = 0;
        while (index < table.SymbolCount)
        {
            if (table.Symbols[index] == symbol)
            {
                return table.CodeLengths[index];
            }

            index = index + 1;
        }

        return 0;
    }
}
