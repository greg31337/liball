using System.Collections.Generic;

namespace jpeg.Tests;

internal class LosslessSof3HuffmanDecoderTests
{
    [Test]
    public void Decoder_ReconstructsGrayscaleSamplesWithLeftPredictor()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(3, 2, 12, 1);
        global::jpeg.JpegScanHeader scan = CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        byte[] entropy = CreateEntropy(
            tables.HuffmanTables[0],
            new int[] { 2, 1, 1, -1, 1, 1 });
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.Decode(frame, scan, tables, entropy);
        global::jpeg.JpegPlanarIntComponentBuffer component = image.Components[0];

        Assert.That(component.Samples[0], Is.EqualTo(2050));
        Assert.That(component.Samples[1], Is.EqualTo(2051));
        Assert.That(component.Samples[2], Is.EqualTo(2052));
        Assert.That(component.Samples[component.Stride], Is.EqualTo(2049));
        Assert.That(component.Samples[component.Stride + 1], Is.EqualTo(2050));
        Assert.That(component.Samples[component.Stride + 2], Is.EqualTo(2051));
    }

    [Test]
    public void Decoder_DecodesInterleavedComponentsInPixelOrder()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(2, 1, 8, 3);
        global::jpeg.JpegScanHeader scan = CreateScan(3, 1, 0);
        global::jpeg.JpegTableStore tables = CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        byte[] entropy = CreateEntropy(
            tables.HuffmanTables[0],
            new int[] { 2, 1, 0, 1, 1, 1 });
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.Decode(frame, scan, tables, entropy);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(130));
        Assert.That(image.Components[0].Samples[1], Is.EqualTo(131));
        Assert.That(image.Components[1].Samples[0], Is.EqualTo(129));
        Assert.That(image.Components[1].Samples[1], Is.EqualTo(130));
        Assert.That(image.Components[2].Samples[0], Is.EqualTo(128));
        Assert.That(image.Components[2].Samples[1], Is.EqualTo(129));
    }

    [Test]
    public void Predictor_EvaluatesAllLosslessSelections()
    {
        global::jpeg.JpegPlanarIntComponentBuffer component = new global::jpeg.JpegPlanarIntComponentBuffer();
        component.Width = 2;
        component.Height = 2;
        component.PaddedWidth = 2;
        component.PaddedHeight = 2;
        component.Stride = 2;
        component.Precision = 8;
        component.Samples = new int[] { 10, 20, 30, 0 };
        global::jpeg.JpegLosslessPredictor predictor = new global::jpeg.JpegLosslessPredictor();

        Assert.That(predictor.Predict(component, 1, 1, 1, 128, 0, false), Is.EqualTo(30));
        Assert.That(predictor.Predict(component, 1, 1, 2, 128, 0, false), Is.EqualTo(20));
        Assert.That(predictor.Predict(component, 1, 1, 3, 128, 0, false), Is.EqualTo(10));
        Assert.That(predictor.Predict(component, 1, 1, 4, 128, 0, false), Is.EqualTo(40));
        Assert.That(predictor.Predict(component, 1, 1, 5, 128, 0, false), Is.EqualTo(35));
        Assert.That(predictor.Predict(component, 1, 1, 6, 128, 0, false), Is.EqualTo(30));
        Assert.That(predictor.Predict(component, 1, 1, 7, 128, 0, false), Is.EqualTo(25));
        Assert.That(predictor.Predict(component, 1, 1, 1, 128, 0, true), Is.EqualTo(128));
    }

    [Test]
    public void EntropyDecoder_AllowsSixteenBitLosslessDifferenceCategory()
    {
        global::jpeg.JpegHuffmanTable table = CreateSingleSymbolDcTable(16);
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(32768, 16);
        writer.FlushToByteBoundary();
        global::jpeg.JpegLosslessHuffmanEntropyDecoder decoder = new global::jpeg.JpegLosslessHuffmanEntropyDecoder(writer.ToArray());

        int difference = decoder.DecodeDifference(table);

        Assert.That(difference, Is.EqualTo(32768));
    }

    [Test]
    public void Decoder_ConsumesRestartMarkersAndRestartsPrediction()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(2, 1, 12, 1);
        global::jpeg.JpegScanHeader scan = CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        WriteDifference(writer, tables.HuffmanTables[0], 2);
        writer.WriteMarkerAfterByteAlign(global::jpeg.JpegConstants.MarkerRestart0);
        WriteDifference(writer, tables.HuffmanTables[0], 2);
        writer.FlushToByteBoundary();
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.DecodeWithRestartInterval(frame, scan, tables, writer.ToArray(), 1);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(2050));
        Assert.That(image.Components[0].Samples[1], Is.EqualTo(2050));
    }

    [Test]
    public void Decoder_RejectsInvalidSof3Inputs()
    {
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();
        global::jpeg.JpegTableStore tables = CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(CreateFrameWithProcess(global::jpeg.JpegProcess.BaselineDct), CreateScan(1, 1, 0), tables, new byte[] { 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(CreateFrame(1, 1, 8, 1), CreateScan(1, 8, 0), tables, new byte[] { 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(CreateSubsampledFrame(), CreateScan(1, 1, 0), tables, new byte[] { 0 });
            });
    }

    public static global::jpeg.JpegFrameHeader CreateFrame(int width, int height, int precision, int componentCount)
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame3;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame3;
        frame.Process = global::jpeg.JpegProcess.LosslessSequential;
        frame.Width = width;
        frame.Height = height;
        frame.Precision = precision;
        frame.ComponentCount = componentCount;
        int index = 0;
        while (index < componentCount)
        {
            global::jpeg.JpegFrameComponent component = new global::jpeg.JpegFrameComponent();
            component.Identifier = index + 1;
            component.HorizontalSamplingFactor = 1;
            component.VerticalSamplingFactor = 1;
            component.QuantizationTableIdentifier = 0;
            frame.Components.Add(component);
            index = index + 1;
        }

        return frame;
    }

    public static global::jpeg.JpegFrameHeader CreateFrameWithProcess(global::jpeg.JpegProcess process)
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(1, 1, 8, 1);
        frame.Process = process;
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame0;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame0;
        return frame;
    }

    public static global::jpeg.JpegFrameHeader CreateSubsampledFrame()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(1, 1, 8, 1);
        frame.Components[0].HorizontalSamplingFactor = 2;
        return frame;
    }

    public static global::jpeg.JpegScanHeader CreateScan(int componentCount, int predictorSelection, int pointTransform)
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = componentCount;
        scan.SpectralStart = predictorSelection;
        scan.SpectralEnd = 0;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = pointTransform;
        int index = 0;
        while (index < componentCount)
        {
            global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
            component.Selector = index + 1;
            component.DcEntropyTableIdentifier = 0;
            component.AcEntropyTableIdentifier = 0;
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    public static global::jpeg.JpegTableStore CreateTables(global::jpeg.JpegHuffmanTable dcTable)
    {
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        tables.HuffmanTables.Add(dcTable);
        return tables;
    }

    public static byte[] CreateEntropy(global::jpeg.JpegHuffmanTable table, int[] differences)
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        int index = 0;
        while (index < differences.Length)
        {
            WriteDifference(writer, table, differences[index]);
            index = index + 1;
        }

        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static void WriteDifference(global::jpeg.JpegBitWriter writer, global::jpeg.JpegHuffmanTable table, int difference)
    {
        int category = GetMagnitudeCategory(difference);
        WriteSymbol(writer, table, category);
        WriteMagnitudeBits(writer, difference, category);
    }

    public static void WriteSymbol(global::jpeg.JpegBitWriter writer, global::jpeg.JpegHuffmanTable table, int symbol)
    {
        int index = 0;
        while (index < table.SymbolCount)
        {
            if (table.Symbols[index] == symbol)
            {
                writer.WriteBits(table.Codes[index], table.CodeLengths[index]);
                return;
            }

            index = index + 1;
        }

        throw new System.InvalidOperationException("Missing test Huffman symbol.");
    }

    public static void WriteMagnitudeBits(global::jpeg.JpegBitWriter writer, int value, int category)
    {
        if (category == 0)
        {
            return;
        }

        int bits = value;
        if (value < 0)
        {
            bits = value + (1 << category) - 1;
        }

        writer.WriteBits(bits, category);
    }

    public static int GetMagnitudeCategory(int value)
    {
        if (value == 0)
        {
            return 0;
        }

        long magnitude = value;
        if (magnitude < 0)
        {
            magnitude = -magnitude;
        }

        int category = 0;
        while (magnitude > 0)
        {
            category = category + 1;
            magnitude = magnitude >> 1;
        }

        return category;
    }

    public static global::jpeg.JpegHuffmanTable CreateSingleSymbolDcTable(int symbol)
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 0;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { symbol };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }
}
