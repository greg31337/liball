using System.Collections.Generic;

namespace jpeg.Tests;

internal class BaselineSof0FullImageDecoderTests
{
    [Test]
    public void Decoder_DecodesGrayscaleMcusIntoPlanarBuffer()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(16, 8);
        frame.Components.Add(CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = CreateScan(frame);
        global::jpeg.JpegTableStore tables = CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = CreateEncoder();
        encoder.EncodeBlock(CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(CreateDcOnlyBlock(3), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.Decode(frame, scan, tables, encoder.ToArray());

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(1));
        Assert.That(image.Components.Count, Is.EqualTo(1));
        Assert.That(image.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[7], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(image.Components[0].Samples[15], Is.EqualTo(143));
    }

    [Test]
    public void Decoder_PlacesFourTwoZeroBlocksInComponentPlanes()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(16, 16);
        frame.Components.Add(CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = CreateScan(frame);
        global::jpeg.JpegTableStore tables = CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = CreateEncoder();
        encoder.EncodeBlock(CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(CreateDcOnlyBlock(3), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(CreateDcOnlyBlock(1), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(CreateDcOnlyBlock(0), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(CreateDcOnlyBlock(1), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1);
        encoder.EncodeBlock(CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 2);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.Decode(frame, scan, tables, encoder.ToArray());

        global::jpeg.JpegPlanarComponentBuffer y = image.Components[0];
        global::jpeg.JpegPlanarComponentBuffer cb = image.Components[1];
        global::jpeg.JpegPlanarComponentBuffer cr = image.Components[2];
        Assert.That(y.Samples[0], Is.EqualTo(138));
        Assert.That(y.Samples[8], Is.EqualTo(143));
        Assert.That(y.Samples[8 * y.Stride], Is.EqualTo(133));
        Assert.That(y.Samples[8 * y.Stride + 8], Is.EqualTo(128));
        Assert.That(cb.Width, Is.EqualTo(8));
        Assert.That(cb.Height, Is.EqualTo(8));
        Assert.That(cb.Samples[0], Is.EqualTo(133));
        Assert.That(cr.Samples[0], Is.EqualTo(138));
    }

    [Test]
    public void Decoder_ConsumesRestartMarkersBetweenMcus()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(16, 8);
        frame.Components.Add(CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = CreateScan(frame);
        global::jpeg.JpegTableStore tables = CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = CreateEncoder();
        encoder.ConfigureRestartInterval(1);
        encoder.EncodeBlock(CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.RecordMcuEncoded();
        encoder.WriteRestartMarker();
        encoder.EncodeBlock(CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.DecodeWithRestartInterval(frame, scan, tables, encoder.ToArray(), 1);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(138));
    }

    [Test]
    public void Decoder_RejectsUnsupportedFramesScansAndMissingTables()
    {
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();
        global::jpeg.JpegFrameHeader frame = CreateFrame(8, 8);
        frame.Components.Add(CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = CreateScan(frame);
        global::jpeg.JpegTableStore tables = CreateTableStore(40);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegFrameHeader progressive = CreateFrame(8, 8);
                progressive.Process = global::jpeg.JpegProcess.ProgressiveDct;
                progressive.Components.Add(CreateFrameComponent(1, 1, 1, 0));
                progressive.ComponentCount = 1;
                decoder.Decode(progressive, scan, tables, new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegScanHeader partial = CreateScan(frame);
                partial.SpectralStart = 1;
                decoder.Decode(frame, partial, tables, new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegTableStore missingQuantization = CreateTableStore(40);
                missingQuantization.QuantizationTables.Clear();
                decoder.Decode(frame, scan, missingQuantization, new byte[0]);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.DecodeWithRestartInterval(frame, scan, tables, new byte[0], -1);
            });
    }

    public static global::jpeg.JpegFrameHeader CreateFrame(int width, int height)
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame0;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame0;
        frame.Process = global::jpeg.JpegProcess.BaselineDct;
        frame.Precision = 8;
        frame.Width = width;
        frame.Height = height;
        return frame;
    }

    public static global::jpeg.JpegFrameComponent CreateFrameComponent(int identifier, int horizontalSampling, int verticalSampling, int quantizationTable)
    {
        global::jpeg.JpegFrameComponent component = new global::jpeg.JpegFrameComponent();
        component.Identifier = identifier;
        component.HorizontalSamplingFactor = horizontalSampling;
        component.VerticalSamplingFactor = verticalSampling;
        component.QuantizationTableIdentifier = quantizationTable;
        return component;
    }

    public static global::jpeg.JpegScanHeader CreateScan(global::jpeg.JpegFrameHeader frame)
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = frame.Components.Count;
        scan.SpectralStart = 0;
        scan.SpectralEnd = 63;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
            component.Selector = frame.Components[index].Identifier;
            component.DcEntropyTableIdentifier = 0;
            component.AcEntropyTableIdentifier = 0;
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    public static global::jpeg.JpegTableStore CreateTableStore(int quantizationValue)
    {
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        tables.QuantizationTables.Add(CreateQuantizationTable(0, quantizationValue));
        tables.HuffmanTables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        tables.HuffmanTables.Add(BaselineHuffmanEntropyDecoderTests.CreateAcTable());
        return tables;
    }

    public static global::jpeg.JpegQuantizationTable CreateQuantizationTable(int identifier, int quantizationValue)
    {
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTable();
        table.Identifier = identifier;
        table.Precision = 8;
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            table.Values[index] = quantizationValue;
            table.ZigZagValues[index] = quantizationValue;
            index = index + 1;
        }

        return table;
    }

    public static int[] CreateDcOnlyBlock(int dcCoefficient)
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = dcCoefficient;
        return coefficients;
    }

    public static global::jpeg.JpegBaselineHuffmanEntropyEncoder CreateEncoder()
    {
        return new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
    }
}
