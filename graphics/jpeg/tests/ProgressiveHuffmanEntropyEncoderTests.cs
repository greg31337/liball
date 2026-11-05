using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveHuffmanEntropyEncoderTests
{
    [Test]
    public void Encoder_EncodesDcFirstBlockThatDecoderReads()
    {
        int[] coefficients = ProgressiveHuffmanDcRefinementTests.CreateBlock(8);
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeDcFirstBlock(coefficients, BaselineHuffmanEntropyDecoderTests.CreateDcTable(), 0, 2);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = decoder.DecodeDcFirstBlock(BaselineHuffmanEntropyDecoderTests.CreateDcTable(), 0, 2);

        Assert.That(decoded[0], Is.EqualTo(8));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(2));
    }

    [Test]
    public void Encoder_KeepsUnshiftedDcPredictorAcrossDcFirstBlocks()
    {
        int[] first = ProgressiveHuffmanDcRefinementTests.CreateBlock(4);
        int[] second = ProgressiveHuffmanDcRefinementTests.CreateBlock(6);
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeDcFirstBlock(first, dcTable, 0, 1);
        encoder.EncodeDcFirstBlock(second, dcTable, 0, 1);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decodedFirst = decoder.DecodeDcFirstBlock(dcTable, 0, 1);
        int[] decodedSecond = decoder.DecodeDcFirstBlock(dcTable, 0, 1);

        Assert.That(decodedFirst[0], Is.EqualTo(4));
        Assert.That(decodedSecond[0], Is.EqualTo(6));
        Assert.That(encoder.DcPredictors[0], Is.EqualTo(3));
    }

    [Test]
    public void Encoder_EncodesDcRefinementBits()
    {
        int[] positive = ProgressiveHuffmanDcRefinementTests.CreateBlock(10);
        int[] negative = ProgressiveHuffmanDcRefinementTests.CreateBlock(-3);
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeDcRefinementBlock(positive, 1);
        encoder.EncodeDcRefinementBlock(negative, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decodedPositive = ProgressiveHuffmanDcRefinementTests.CreateBlock(8);
        int[] decodedNegative = ProgressiveHuffmanDcRefinementTests.CreateBlock(-4);
        decoder.DecodeDcRefinementBlock(decodedPositive, 1);
        decoder.DecodeDcRefinementBlock(decodedNegative, 0);

        Assert.That(decodedPositive[0], Is.EqualTo(10));
        Assert.That(decodedNegative[0], Is.EqualTo(-3));
    }

    [Test]
    public void Encoder_EncodesAcFirstCoefficientAndZrlRuns()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[18] = 1;
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeAcFirstBlock(coefficients, acTable, 1, 20, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = new int[global::jpeg.JpegConstants.BlockSampleCount];
        decoder.DecodeAcFirstBlock(decoded, acTable, 1, 20, 0);

        Assert.That(decoded[18], Is.EqualTo(1));
        Assert.That(decoded[17], Is.EqualTo(0));
    }

    [Test]
    public void Encoder_AccumulatesAndFlushesAcFirstEobRuns()
    {
        int[] first = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] second = new int[global::jpeg.JpegConstants.BlockSampleCount];
        global::jpeg.JpegHuffmanTable acTable = ProgressiveHuffmanAcFirstTests.CreateEobRunAcTable();
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeAcFirstBlock(first, acTable, 1, 10, 0);
        encoder.EncodeAcFirstBlock(second, acTable, 1, 10, 0);
        encoder.FlushEobRun(acTable);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        decoder.DecodeAcFirstBlock(first, acTable, 1, 10, 0);
        decoder.DecodeAcFirstBlock(second, acTable, 1, 10, 0);

        Assert.That(decoder.EobRun, Is.EqualTo(0));
        Assert.That(first[1], Is.EqualTo(0));
        Assert.That(second[10], Is.EqualTo(0));
    }

    [Test]
    public void Encoder_EncodesAcRefinementExistingAndNewCoefficients()
    {
        int[] target = new int[global::jpeg.JpegConstants.BlockSampleCount];
        target[1] = 3;
        target[2] = 1;
        global::jpeg.JpegHuffmanTable acTable = ProgressiveHuffmanAcRefinementTests.CreateNewCoefficientAcTable();
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeAcRefinementBlock(target, acTable, 1, 5, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = new int[global::jpeg.JpegConstants.BlockSampleCount];
        decoded[1] = 2;
        decoder.DecodeAcRefinementBlock(decoded, acTable, 1, 5, 0);

        Assert.That(decoded[1], Is.EqualTo(3));
        Assert.That(decoded[2], Is.EqualTo(1));
    }

    [Test]
    public void Encoder_EncodesProgressiveBlocksFromScanHeaders()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanDcFirstTests.CreateDcFirstScan(2);
        global::jpeg.JpegScanComponent component = ScanHeaderWriterTests.CreateComponent(1, 0, 0);
        int[] coefficients = ProgressiveHuffmanDcRefinementTests.CreateBlock(8);
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();

        encoder.EncodeDcFirstBlockForScanComponent(tables, scan, component, coefficients, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 0);

        Assert.That(decoded[0], Is.EqualTo(8));
    }

    [Test]
    public void EncoderDecoder_ReconstructsBlockAcrossSuccessiveProgressiveScans()
    {
        List<global::jpeg.JpegHuffmanTable> dcTables = new List<global::jpeg.JpegHuffmanTable>();
        dcTables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegScanComponent component = ScanHeaderWriterTests.CreateComponent(1, 0, 0);
        global::jpeg.JpegScanHeader dcFirstScan = ProgressiveHuffmanDcFirstTests.CreateDcFirstScan(1);
        global::jpeg.JpegScanHeader dcRefinementScan = ProgressiveHuffmanDcFirstTests.CreateScan(0, 0, 1, 0);
        global::jpeg.JpegScanHeader acFirstScan = ProgressiveHuffmanAcFirstTests.CreateAcFirstScan(5, 7, 1);
        global::jpeg.JpegScanHeader acRefinementScan = ProgressiveHuffmanDcFirstTests.CreateScan(5, 7, 1, 0);
        global::jpeg.JpegHuffmanTable acFirstTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        global::jpeg.JpegHuffmanTable acRefinementTable = ProgressiveHuffmanAcRefinementTests.CreateNewCoefficientAcTable();
        int[] coarse = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coarse[0] = 6;
        coarse[5] = 4;
        int[] target = new int[global::jpeg.JpegConstants.BlockSampleCount];
        target[0] = 7;
        target[5] = 5;
        target[6] = -1;

        int[] decoded = DecodeDcFirstScan(EncodeDcFirstScan(dcTables, dcFirstScan, component, coarse), dcTables, dcFirstScan, component);
        DecodeDcRefinementScan(EncodeDcRefinementScan(dcRefinementScan, target), dcRefinementScan, decoded);
        DecodeAcFirstScan(EncodeAcFirstScan(acFirstScan, acFirstTable, coarse), acFirstScan, acFirstTable, decoded);
        DecodeAcRefinementScan(EncodeAcRefinementScan(acRefinementScan, acRefinementTable, target), acRefinementScan, acRefinementTable, decoded);

        Assert.That(decoded[0], Is.EqualTo(7));
        Assert.That(decoded[5], Is.EqualTo(5));
        Assert.That(decoded[6], Is.EqualTo(-1));
        Assert.That(decoded[7], Is.EqualTo(0));
    }

    [Test]
    public void EncoderDecoder_RoundTripsAcEobRunBeforeRestartMarker()
    {
        global::jpeg.JpegHuffmanTable acTable = CreateAcEobRunAndCoefficientTable();
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.ConfigureRestartInterval(2);
        int[] empty = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] nonzero = new int[global::jpeg.JpegConstants.BlockSampleCount];
        nonzero[1] = 2;

        encoder.EncodeAcFirstBlock(empty, acTable, 1, 3, 0);
        encoder.RecordMcuEncoded();
        encoder.EncodeAcFirstBlock(empty, acTable, 1, 3, 0);
        encoder.FlushEobRun(acTable);
        encoder.RecordMcuEncoded();
        Assert.That(encoder.ShouldWriteRestartMarker(), Is.True);
        encoder.WriteRestartMarker();
        encoder.EncodeAcFirstBlock(nonzero, acTable, 1, 3, 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder =
            new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(encoder.ToArray());
        decoder.ConfigureRestartInterval(2);
        int[] first = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] second = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] third = new int[global::jpeg.JpegConstants.BlockSampleCount];
        decoder.DecodeAcFirstBlock(first, acTable, 1, 3, 0);
        decoder.RecordMcuDecoded();
        decoder.DecodeAcFirstBlock(second, acTable, 1, 3, 0);
        decoder.RecordMcuDecoded();
        Assert.That(decoder.BaselineDecoder.ShouldExpectRestartMarker(), Is.True);
        Assert.That(decoder.BitReader.AdvanceToMarker(), Is.True);
        decoder.ConsumeRestartMarkerLookahead();
        decoder.DecodeAcFirstBlock(third, acTable, 1, 3, 0);

        Assert.That(first[1], Is.EqualTo(0));
        Assert.That(second[3], Is.EqualTo(0));
        Assert.That(third[1], Is.EqualTo(2));
        Assert.That(decoder.BaselineDecoder.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoder.BaselineDecoder.ExpectedRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(encoder.BaselineEncoder.RestartMarkerCount, Is.EqualTo(1));
    }

    [Test]
    public void Encoder_RejectsInvalidProgressiveInputs()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = BaselineHuffmanEntropyDecoderTests.CreateAcTable();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegProgressiveHuffmanEntropyEncoder.FromBitWriter((global::jpeg.JpegBitWriter)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.EncodeDcFirstBlock(null!, dcTable, 0, 0);
            });

        coefficients[0] = 3;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeDcFirstBlock(coefficients, dcTable, 0, 1);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeAcFirstBlock(coefficients, acTable, 0, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeAcRefinementBlock(new int[1], acTable, 1, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.ValidateAcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(1, 5, 0, 0));
            });
    }

    public static byte[] EncodeDcFirstScan(
        List<global::jpeg.JpegHuffmanTable> tables,
        global::jpeg.JpegScanHeader scan,
        global::jpeg.JpegScanComponent component,
        int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.EncodeDcFirstBlockForScanComponent(tables, scan, component, coefficients, 0);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static int[] DecodeDcFirstScan(
        byte[] entropy,
        List<global::jpeg.JpegHuffmanTable> tables,
        global::jpeg.JpegScanHeader scan,
        global::jpeg.JpegScanComponent component)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(entropy);
        return decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 0);
    }

    public static byte[] EncodeDcRefinementScan(global::jpeg.JpegScanHeader scan, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.EncodeDcRefinementBlockForScan(scan, coefficients);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static void DecodeDcRefinementScan(byte[] entropy, global::jpeg.JpegScanHeader scan, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(entropy);
        decoder.DecodeDcRefinementBlockForScan(scan, coefficients);
    }

    public static byte[] EncodeAcFirstScan(global::jpeg.JpegScanHeader scan, global::jpeg.JpegHuffmanTable table, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.EncodeAcFirstBlockForScan(scan, table, coefficients);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static void DecodeAcFirstScan(byte[] entropy, global::jpeg.JpegScanHeader scan, global::jpeg.JpegHuffmanTable table, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(entropy);
        decoder.DecodeAcFirstBlockForScan(scan, table, coefficients);
    }

    public static byte[] EncodeAcRefinementScan(global::jpeg.JpegScanHeader scan, global::jpeg.JpegHuffmanTable table, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.EncodeAcRefinementBlockForScan(scan, table, coefficients);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static void DecodeAcRefinementScan(byte[] entropy, global::jpeg.JpegScanHeader scan, global::jpeg.JpegHuffmanTable table, int[] coefficients)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(entropy);
        decoder.DecodeAcRefinementBlockForScan(scan, table, coefficients);
    }

    public static global::jpeg.JpegHuffmanTable CreateAcEobRunAndCoefficientTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.CodeCounts[1] = 2;
        table.SymbolCount = 3;
        table.Symbols = new int[] { 0x10, 0x02, 0x00 };
        table.Codes = new int[3];
        table.CodeLengths = new int[3];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }
}
