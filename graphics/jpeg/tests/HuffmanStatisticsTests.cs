namespace jpeg.Tests;

internal class HuffmanStatisticsTests
{
    [Test]
    public void Collector_CountsBaselineDcAndAcSymbolsForBlock()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 5;
        coefficients[1] = 3;
        coefficients[5] = -2;

        collector.CollectBlock(coefficients, 0, dcStatistics, acStatistics);

        Assert.That(dcStatistics.Counts[3], Is.EqualTo(1));
        Assert.That(dcStatistics.TotalCount, Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x02], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x32], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x00], Is.EqualTo(1));
        Assert.That(acStatistics.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void Collector_EmitsZrlForLongZeroRuns()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[20] = 1;

        collector.CollectBlock(coefficients, 0, dcStatistics, acStatistics);

        Assert.That(dcStatistics.Counts[0], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0xF0], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x31], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x00], Is.EqualTo(1));
        Assert.That(acStatistics.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void Collector_TracksDcPredictorPerComponentAndCanReset()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);

        collector.CollectDcCoefficient(5, 0, dcStatistics);
        collector.CollectDcCoefficient(7, 0, dcStatistics);
        collector.CollectDcCoefficient(4, 1, dcStatistics);
        collector.ResetDcPredictors();
        collector.CollectDcCoefficient(7, 0, dcStatistics);

        Assert.That(dcStatistics.Counts[2], Is.EqualTo(1));
        Assert.That(dcStatistics.Counts[3], Is.EqualTo(3));
        Assert.That(dcStatistics.TotalCount, Is.EqualTo(4));
        Assert.That(collector.DcPredictors[0], Is.EqualTo(7));
        Assert.That(collector.DcPredictors[1], Is.EqualTo(0));
    }

    [Test]
    public void Collector_CollectsAcSpectralRangeOnly()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 7;
        coefficients[5] = 1;
        coefficients[8] = 2;
        coefficients[12] = 3;

        collector.CollectAcCoefficients(coefficients, 5, 10, acStatistics);

        Assert.That(acStatistics.Counts[0x01], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x22], Is.EqualTo(1));
        Assert.That(acStatistics.Counts[0x00], Is.EqualTo(1));
        Assert.That(acStatistics.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void Collector_RejectsInvalidInputs()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                collector.CollectBlock(null!, 0, dcStatistics, acStatistics);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                collector.CollectBlock(new int[1], 0, dcStatistics, acStatistics);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                collector.CollectBlock(coefficients, 256, dcStatistics, acStatistics);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                collector.CollectBlock(coefficients, 0, acStatistics, acStatistics);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                collector.CollectAcCoefficients(coefficients, 0, 5, acStatistics);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                collector.IncrementSymbol(acStatistics, 256);
            });
    }

    [Test]
    public void Collector_RejectsCategoriesOutsideBaselineRange()
    {
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.That(collector.GetMagnitudeCategory(-1024), Is.EqualTo(11));
        Assert.That(collector.GetMagnitudeCategory(-2147483648), Is.EqualTo(32));

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                collector.CollectDcCoefficient(4096, 0, dcStatistics);
            });

        coefficients[1] = 2048;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                collector.CollectAcCoefficients(coefficients, 1, 1, acStatistics);
            });
    }
}
