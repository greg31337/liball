using System.Collections.Generic;

namespace jpeg.Tests;

internal class BaselineHuffmanEntropyEncoderTests
{
    [Test]
    public void Encoder_EncodesBaselineBlockThatDecoderReads()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 2;
        coefficients[1] = -2;
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlock(coefficients, BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();

        byte[] encoded = encoder.ToArray();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoded);
        int[] decoded = decoder.DecodeBlock(BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);

        Assert.That(encoded, Is.EqualTo(BaselineHuffmanEntropyDecoderTests.CreateEntropyForPositiveDcAndNegativeAc()));
        Assert.That(decoded[0], Is.EqualTo(2));
        Assert.That(decoded[1], Is.EqualTo(-2));
    }

    [Test]
    public void Encoder_AppliesAndResetsDcPredictors()
    {
        int[] first = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] second = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] third = new int[global::jpeg.JpegConstants.BlockSampleCount];
        first[0] = 2;
        second[0] = 3;
        third[0] = 3;
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlock(first, dcTable, acTable, 0);
        encoder.EncodeBlock(second, dcTable, acTable, 0);
        encoder.ResetDcPredictors();
        encoder.EncodeBlock(third, dcTable, acTable, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoder.ToArray());
        int[] decodedFirst = decoder.DecodeBlock(dcTable, acTable, 0);
        int[] decodedSecond = decoder.DecodeBlock(dcTable, acTable, 0);
        decoder.ResetDcPredictors();
        int[] decodedThird = decoder.DecodeBlock(dcTable, acTable, 0);

        Assert.That(decodedFirst[0], Is.EqualTo(2));
        Assert.That(decodedSecond[0], Is.EqualTo(3));
        Assert.That(decodedThird[0], Is.EqualTo(3));
        Assert.That(encoder.DcPredictors[0], Is.EqualTo(3));
    }

    [Test]
    public void Encoder_EmitsZrlForLongZeroRun()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[18] = 1;
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlock(coefficients, dcTable, acTable, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = decoder.DecodeBlock(dcTable, acTable, 0);

        Assert.That(decoded[0], Is.EqualTo(0));
        Assert.That(decoded[18], Is.EqualTo(1));
        Assert.That(decoded[17], Is.EqualTo(0));
    }

    [Test]
    public void Encoder_FindsTablesForScanComponent()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateAcTable());
        global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
        component.DcEntropyTableIdentifier = 0;
        component.AcEntropyTableIdentifier = 0;
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 2;
        coefficients[1] = -2;
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlockForScanComponent(tables, component, coefficients, 0);
        encoder.FlushToByteBoundary();

        Assert.That(encoder.ToArray(), Is.EqualTo(BaselineHuffmanEntropyDecoderTests.CreateEntropyForPositiveDcAndNegativeAc()));
    }

    [Test]
    public void Encoder_RoundTripsWithGeneratedOptimizedTables()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 5;
        coefficients[1] = 3;
        coefficients[5] = -2;
        global::jpeg.JpegHuffmanStatisticsCollector collector = new global::jpeg.JpegHuffmanStatisticsCollector();
        global::jpeg.JpegHuffmanStatistics dcStatistics = collector.CreateStatistics(0, 0);
        global::jpeg.JpegHuffmanStatistics acStatistics = collector.CreateStatistics(1, 0);
        collector.CollectBlock(coefficients, 0, dcStatistics, acStatistics);
        global::jpeg.JpegHuffmanTableGenerator generator = new global::jpeg.JpegHuffmanTableGenerator();
        global::jpeg.JpegHuffmanTable dcTable = generator.GenerateTable(dcStatistics);
        global::jpeg.JpegHuffmanTable acTable = generator.GenerateTable(acStatistics);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlock(coefficients, dcTable, acTable, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = decoder.DecodeBlock(dcTable, acTable, 0);

        Assert.That(decoded[0], Is.EqualTo(5));
        Assert.That(decoded[1], Is.EqualTo(3));
        Assert.That(decoded[5], Is.EqualTo(-2));
    }

    [Test]
    public void EncoderDecoder_RoundTripsInterleavedComponentPredictorsIndependently()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateAcTable());
        global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
        component.DcEntropyTableIdentifier = 0;
        component.AcEntropyTableIdentifier = 0;
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();

        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(2, 1, -2), 0);
        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(1, 18, 1), 1);
        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(-1, 0, 0), 2);
        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(3, 0, 0), 0);
        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(1, 0, 0), 1);
        encoder.EncodeBlockForScanComponent(tables, component, CreateBlock(-2, 0, 0), 2);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder =
            new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoder.ToArray());
        int[] y0 = decoder.DecodeBlockForScanComponent(tables, component, 0);
        int[] cb0 = decoder.DecodeBlockForScanComponent(tables, component, 1);
        int[] cr0 = decoder.DecodeBlockForScanComponent(tables, component, 2);
        int[] y1 = decoder.DecodeBlockForScanComponent(tables, component, 0);
        int[] cb1 = decoder.DecodeBlockForScanComponent(tables, component, 1);
        int[] cr1 = decoder.DecodeBlockForScanComponent(tables, component, 2);

        Assert.That(y0[0], Is.EqualTo(2));
        Assert.That(y0[1], Is.EqualTo(-2));
        Assert.That(cb0[0], Is.EqualTo(1));
        Assert.That(cb0[18], Is.EqualTo(1));
        Assert.That(cr0[0], Is.EqualTo(-1));
        Assert.That(y1[0], Is.EqualTo(3));
        Assert.That(cb1[0], Is.EqualTo(1));
        Assert.That(cr1[0], Is.EqualTo(-2));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(3));
        Assert.That(decoder.DcPredictors[1], Is.EqualTo(1));
        Assert.That(decoder.DcPredictors[2], Is.EqualTo(-2));
    }

    [Test]
    public void EncoderDecoder_RoundTripsRestartMarkerAndResetsPredictors()
    {
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.ConfigureRestartInterval(2);

        encoder.EncodeBlock(CreateBlock(2, 0, 0), dcTable, acTable, 0);
        encoder.RecordMcuEncoded();
        encoder.EncodeBlock(CreateBlock(3, 0, 0), dcTable, acTable, 0);
        encoder.RecordMcuEncoded();
        Assert.That(encoder.ShouldWriteRestartMarker(), Is.True);
        encoder.WriteRestartMarker();
        encoder.EncodeBlock(CreateBlock(2, 0, 0), dcTable, acTable, 0);
        encoder.RecordMcuEncoded();
        encoder.FlushToByteBoundary();

        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder =
            new global::jpeg.JpegBaselineHuffmanEntropyDecoder(encoder.ToArray());
        decoder.ConfigureRestartInterval(2);
        int[] first = decoder.DecodeBlock(dcTable, acTable, 0);
        decoder.RecordMcuDecoded();
        int[] second = decoder.DecodeBlock(dcTable, acTable, 0);
        decoder.RecordMcuDecoded();
        Assert.That(decoder.ShouldExpectRestartMarker(), Is.True);
        Assert.That(decoder.BitReader.AdvanceToMarker(), Is.True);
        decoder.ConsumeRestartMarkerLookahead();
        int[] third = decoder.DecodeBlock(dcTable, acTable, 0);
        decoder.RecordMcuDecoded();

        Assert.That(first[0], Is.EqualTo(2));
        Assert.That(second[0], Is.EqualTo(3));
        Assert.That(third[0], Is.EqualTo(2));
        Assert.That(decoder.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoder.ExpectedRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(decoder.McuCountSinceRestart, Is.EqualTo(1));
        Assert.That(encoder.RestartMarkerCount, Is.EqualTo(1));
    }

    [Test]
    public void Encoder_WritesRestartMarkersAndResetsState()
    {
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.ConfigureRestartInterval(1);
        encoder.DcPredictors[0] = 5;
        encoder.RecordMcuEncoded();

        encoder.WriteRestartMarker();

        byte[] data = encoder.ToArray();
        Assert.That(data[0], Is.EqualTo(0xFF));
        Assert.That(data[1], Is.EqualTo(0xD0));
        Assert.That(encoder.DcPredictors[0], Is.EqualTo(0));
        Assert.That(encoder.McuCountSinceRestart, Is.EqualTo(0));
        Assert.That(encoder.NextRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(encoder.RestartMarkerCount, Is.EqualTo(1));
    }

    [Test]
    public void Encoder_RejectsInvalidInputsTablesAndMissingSymbols()
    {
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegBaselineHuffmanEntropyEncoder.FromBitWriter((global::jpeg.JpegBitWriter)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.EncodeBlock(null!, dcTable, acTable, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeBlock(new int[1], dcTable, acTable, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeBlock(coefficients, dcTable, acTable, -1);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeBlock(coefficients, acTable, acTable, 0);
            });

        coefficients[0] = 8;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeBlock(coefficients, dcTable, acTable, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.WriteMagnitudeBits(1, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.WriteRestartMarker();
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.CalculateDifference(int.MaxValue, int.MinValue);
            });
    }

    public static int[] CreateBlock(int dc, int acIndex, int acValue)
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = dc;
        if (acIndex > 0)
        {
            coefficients[acIndex] = acValue;
        }

        return coefficients;
    }
}
