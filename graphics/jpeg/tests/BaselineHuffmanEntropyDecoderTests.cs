using System.Collections.Generic;

namespace jpeg.Tests;

internal class BaselineHuffmanEntropyDecoderTests
{
    [Test]
    public void Decoder_DecodesBaselineDcAndAcCoefficients()
    {
        byte[] entropy = CreateEntropyForPositiveDcAndNegativeAc();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(entropy);

        int[] block = decoder.DecodeBlock(CreateDcTable(), CreateAcTable(), 0);

        Assert.That(block[0], Is.EqualTo(2));
        Assert.That(block[1], Is.EqualTo(-2));
        Assert.That(block[2], Is.EqualTo(0));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(2));
    }

    [Test]
    public void Decoder_AppliesAndResetsDcPredictors()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(3, 2);
        writer.WriteBits(2, 2);
        writer.WriteBits(0, 1);
        writer.WriteBits(2, 2);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(writer.ToArray());
        global::jpeg.JpegHuffmanTable dcTable = CreateDcTable();
        global::jpeg.JpegHuffmanTable acTable = CreateAcTable();

        int[] first = decoder.DecodeBlock(dcTable, acTable, 0);
        int[] second = decoder.DecodeBlock(dcTable, acTable, 0);
        decoder.ResetDcPredictors();

        Assert.That(first[0], Is.EqualTo(2));
        Assert.That(second[0], Is.EqualTo(3));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_HandlesAcZeroRunLengthSymbol()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(6, 3);
        writer.WriteBits(7, 3);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(writer.ToArray());

        int[] block = decoder.DecodeBlock(CreateDcTable(), CreateAcTable(), 0);

        Assert.That(block[0], Is.EqualTo(0));
        Assert.That(block[18], Is.EqualTo(1));
        Assert.That(block[17], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_FindsTablesForScanComponent()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(CreateDcTable());
        tables.Add(CreateAcTable());
        global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
        component.DcEntropyTableIdentifier = 0;
        component.AcEntropyTableIdentifier = 0;
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(CreateEntropyForPositiveDcAndNegativeAc());

        int[] block = decoder.DecodeBlockForScanComponent(tables, component, 0);

        Assert.That(block[0], Is.EqualTo(2));
        Assert.That(block[1], Is.EqualTo(-2));
    }

    [Test]
    public void Decoder_ReportsMarkerLookaheadWhenEntropyMarkerIsReached()
    {
        global::jpeg.JpegBitReader bitReader = new global::jpeg.JpegBitReader(new byte[] { 0xFF, 0xD0 });
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = global::jpeg.JpegBaselineHuffmanEntropyDecoder.FromBitReader(bitReader);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeBlock(CreateDcTable(), CreateAcTable(), 0);
            });

        Assert.That(bitReader.HasMarker, Is.True);
        Assert.That(bitReader.GetMarkerLookahead().Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
    }

    [Test]
    public void Decoder_RejectsInvalidAcZeroSizeRun()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(writer.ToArray());

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeBlock(CreateDcZeroTable(), CreateInvalidAcZeroSizeRunTable(), 0);
            });
    }

    [Test]
    public void Decoder_RejectsDcCategoriesOutsideBaselineRange()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(writer.ToArray());

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeBlock(CreateBadDcCategoryTable(), CreateAcTable(), 0);
            });
    }

    [Test]
    public void Decoder_RejectsMissingTablesWrongClassesAndInvalidArguments()
    {
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(new byte[] { 0 });
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(CreateDcTable());

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegBaselineHuffmanEntropyDecoder.FromBitReader((global::jpeg.JpegBitReader)null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.FindTable(tables, 1, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeBlock(CreateAcTable(), CreateAcTable(), 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.DecodeBlock(CreateDcTable(), CreateAcTable(), -1);
            });
    }

    [Test]
    public void Decoder_ConsumesRestartMarkerLookaheadAndResetsState()
    {
        global::jpeg.JpegBitReader bitReader = new global::jpeg.JpegBitReader(new byte[] { 0xFF, 0xD0, 0x80 });
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = global::jpeg.JpegBaselineHuffmanEntropyDecoder.FromBitReader(bitReader);
        decoder.ConfigureRestartInterval(2);
        decoder.DcPredictors[0] = 9;
        decoder.RecordMcuDecoded();
        decoder.RecordMcuDecoded();

        Assert.That(decoder.ShouldExpectRestartMarker(), Is.True);
        Assert.That(bitReader.AdvanceToMarker(), Is.True);
        decoder.ConsumeRestartMarkerLookahead();

        Assert.That(decoder.DcPredictors[0], Is.EqualTo(0));
        Assert.That(decoder.McuCountSinceRestart, Is.EqualTo(0));
        Assert.That(decoder.ExpectedRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(decoder.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(bitReader.HasMarker, Is.False);
        Assert.That(bitReader.ReadBit(), Is.EqualTo(1));
    }

    [Test]
    public void Decoder_ValidatesRestartMarkerSequenceAndWraparound()
    {
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(new byte[0]);
        decoder.ConfigureRestartInterval(1);

        int index = 0;
        while (index < 9)
        {
            decoder.RecordMcuDecoded();
            decoder.AcceptRestartMarker(CreateRestartMarker(index & 7));
            index = index + 1;
        }

        Assert.That(decoder.ExpectedRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(decoder.RestartMarkerCount, Is.EqualTo(9));
    }

    [Test]
    public void Decoder_RejectsRestartMarkersThatArriveEarlyOrOutOfOrder()
    {
        global::jpeg.JpegBaselineHuffmanEntropyDecoder early = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(new byte[0]);
        early.ConfigureRestartInterval(2);
        early.RecordMcuDecoded();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                early.AcceptRestartMarker(CreateRestartMarker(0));
            });

        global::jpeg.JpegBaselineHuffmanEntropyDecoder outOfOrder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(new byte[0]);
        outOfOrder.ConfigureRestartInterval(1);
        outOfOrder.RecordMcuDecoded();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                outOfOrder.AcceptRestartMarker(CreateRestartMarker(1));
            });
    }

    [Test]
    public void Decoder_RejectsRestartMarkersWithoutIntervalAndNonRestartMarkers()
    {
        global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder = new global::jpeg.JpegBaselineHuffmanEntropyDecoder(new byte[0]);
        decoder.RecordMcuDecoded();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.AcceptRestartMarker(CreateRestartMarker(0));
            });

        decoder.ConfigureRestartInterval(1);
        decoder.RecordMcuDecoded();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.AcceptRestartMarker(CreateMarker(global::jpeg.JpegConstants.MarkerEndOfImage));
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.ConfigureRestartInterval(65536);
            });
    }

    public static byte[] CreateEntropyForPositiveDcAndNegativeAc()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(3, 2);
        writer.WriteBits(2, 2);
        writer.WriteBits(2, 2);
        writer.WriteBits(1, 2);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static global::jpeg.JpegHuffmanTable CreateDcTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 0;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.CodeCounts[1] = 2;
        table.SymbolCount = 3;
        table.Symbols = new int[] { 0, 1, 2 };
        table.Codes = new int[3];
        table.CodeLengths = new int[3];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateDcZeroTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 0;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { 0 };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateBadDcCategoryTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 0;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { 12 };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateAcTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.CodeCounts[1] = 1;
        table.CodeCounts[2] = 2;
        table.SymbolCount = 4;
        table.Symbols = new int[] { 0x00, 0x02, 0xF0, 0x11 };
        table.Codes = new int[4];
        table.CodeLengths = new int[4];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateInvalidAcZeroSizeRunTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { 0x20 };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegScannedMarker CreateRestartMarker(int restartIndex)
    {
        return CreateMarker(global::jpeg.JpegConstants.MarkerRestart0 + restartIndex);
    }

    public static global::jpeg.JpegScannedMarker CreateMarker(int markerValue)
    {
        global::jpeg.JpegScannedMarker marker = new global::jpeg.JpegScannedMarker();
        marker.Value = markerValue;
        marker.Code = markerValue & 255;
        marker.Marker = (global::jpeg.JpegMarker)markerValue;
        marker.Offset = 0;
        marker.FoundAfterEntropyData = true;
        marker.EntropyBytesSkipped = 0;
        return marker;
    }
}
