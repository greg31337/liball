using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveHuffmanDcFirstTests
{
    [Test]
    public void Decoder_DecodesDcFirstBlockAndAppliesSuccessiveLowShift()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateDcEntropy(3, 2));

        int[] block = decoder.DecodeDcFirstBlock(BaselineHuffmanEntropyDecoderTests.CreateDcTable(), 0, 2);

        Assert.That(block[0], Is.EqualTo(8));
        Assert.That(block[1], Is.EqualTo(0));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(2));
    }

    [Test]
    public void Decoder_KeepsUnshiftedDcPredictorAcrossBlocks()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(3, 2);
        writer.WriteBits(2, 2);
        writer.WriteBits(2, 2);
        writer.WriteBits(1, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        global::jpeg.JpegHuffmanTable dcTable = BaselineHuffmanEntropyDecoderTests.CreateDcTable();

        int[] first = decoder.DecodeDcFirstBlock(dcTable, 0, 1);
        int[] second = decoder.DecodeDcFirstBlock(dcTable, 0, 1);

        Assert.That(first[0], Is.EqualTo(4));
        Assert.That(second[0], Is.EqualTo(6));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(3));
    }

    [Test]
    public void Decoder_FindsDcTableForProgressiveScanComponent()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegScanHeader scan = CreateDcFirstScan(2);
        global::jpeg.JpegScanComponent component = ScanHeaderWriterTests.CreateComponent(1, 0, 3);
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateDcEntropy(3, 2));

        int[] block = decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 0);

        Assert.That(block[0], Is.EqualTo(8));
    }

    [Test]
    public void Decoder_RestartMarkerResetsDcPredictors()
    {
        global::jpeg.JpegBitReader bitReader = new global::jpeg.JpegBitReader(new byte[] { 0xFF, 0xD0, 0x80 });
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = global::jpeg.JpegProgressiveHuffmanEntropyDecoder.FromBitReader(bitReader);
        decoder.ConfigureRestartInterval(1);
        decoder.DcPredictors[0] = 7;
        decoder.RecordMcuDecoded();

        Assert.That(bitReader.AdvanceToMarker(), Is.True);
        decoder.ConsumeRestartMarkerLookahead();

        Assert.That(decoder.DcPredictors[0], Is.EqualTo(0));
        Assert.That(bitReader.ReadBit(), Is.EqualTo(1));
    }

    [Test]
    public void Decoder_RejectsNonDcFirstScanParameters()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[0]);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcFirstScan(CreateScan(1, 0, 0, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcFirstScan(CreateScan(0, 1, 0, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcFirstScan(CreateScan(0, 0, 1, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcFirstScan(CreateScan(0, 0, 0, 16));
            });
    }

    [Test]
    public void Decoder_RejectsNullInputsAndMissingTables()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[0]);
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        global::jpeg.JpegScanHeader scan = CreateDcFirstScan(0);
        global::jpeg.JpegScanComponent component = ScanHeaderWriterTests.CreateComponent(1, 0, 0);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegProgressiveHuffmanEntropyDecoder.FromBitReader((global::jpeg.JpegBitReader)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.ValidateDcFirstScan(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 0);
            });

        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeDcFirstBlockForScanComponent(tables, scan, null!, 0);
            });
    }

    public static byte[] CreateDcEntropy(int code, int valueBits)
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(code, 2);
        writer.WriteBits(valueBits, 2);
        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static global::jpeg.JpegScanHeader CreateDcFirstScan(int successiveLow)
    {
        return CreateScan(0, 0, 0, successiveLow);
    }

    public static global::jpeg.JpegScanHeader CreateScan(int spectralStart, int spectralEnd, int successiveHigh, int successiveLow)
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = 1;
        scan.Components.Add(ScanHeaderWriterTests.CreateComponent(1, 0, 0));
        scan.SpectralStart = spectralStart;
        scan.SpectralEnd = spectralEnd;
        scan.SuccessiveHigh = successiveHigh;
        scan.SuccessiveLow = successiveLow;
        return scan;
    }
}
