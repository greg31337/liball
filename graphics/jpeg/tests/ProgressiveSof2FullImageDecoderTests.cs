using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveSof2FullImageDecoderTests
{
    [Test]
    public void Decoder_DecodesDcOnlyGrayscaleScansToPlanarSamples()
    {
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = CreateDcScan(frame, 0, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegProgressiveScanData scanData = CreateScanData(scan, CreateDcEntropy(scan, new int[] { 2, 3 }, new int[] { 0, 0 }));
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(scanData);
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.Decode(frame, scans, tables);

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[7], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(image.Components[0].Samples[15], Is.EqualTo(143));
    }

    [Test]
    public void Decoder_AccumulatesAcFirstScanIntoCoefficientBuffer()
    {
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader dcScan = CreateDcScan(frame, 0, 0);
        global::jpeg.JpegScanHeader acScan = CreateSingleComponentScan(1, 1, 0, 0, 1);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(1);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(CreateScanData(dcScan, CreateDcEntropy(dcScan, new int[] { 2 }, new int[] { 0 })));
        scans.Add(CreateScanData(acScan, CreateAcFirstEntropy(acScan, -2)));
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegProgressiveCoefficientBuffer coefficients = decoder.DecodeCoefficients(frame, scans, tables);

        Assert.That(coefficients.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(coefficients.Components[0].Coefficients[1], Is.EqualTo(-2));
    }

    [Test]
    public void Decoder_AppliesDcRefinementScan()
    {
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader dcFirst = CreateDcScan(frame, 0, 1);
        global::jpeg.JpegScanHeader dcRefinement = CreateDcScan(frame, 1, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(CreateScanData(dcFirst, CreateDcEntropy(dcFirst, new int[] { 2 }, new int[] { 0 })));
        scans.Add(CreateScanData(dcRefinement, CreateDcRefinementEntropy(3)));
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.Decode(frame, scans, tables);
        global::jpeg.JpegProgressiveCoefficientBuffer coefficients = decoder.DecodeCoefficients(frame, scans, tables);

        Assert.That(coefficients.Components[0].Coefficients[0], Is.EqualTo(3));
        Assert.That(image.Components[0].Samples[0], Is.EqualTo(143));
    }

    [Test]
    public void Decoder_DecodesInterleavedFourTwoZeroDcScan()
    {
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = CreateDcScan(frame, 0, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(CreateScanData(scan, CreateDcEntropy(scan, new int[] { 2, 3, 1, 0, 1, 2 }, new int[] { 0, 0, 0, 0, 1, 2 })));
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.Decode(frame, scans, tables);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(image.Components[0].Samples[8 * image.Components[0].Stride], Is.EqualTo(133));
        Assert.That(image.Components[0].Samples[8 * image.Components[0].Stride + 8], Is.EqualTo(128));
        Assert.That(image.Components[1].Samples[0], Is.EqualTo(133));
        Assert.That(image.Components[2].Samples[0], Is.EqualTo(138));
    }

    [Test]
    public void Decoder_ConsumesRestartMarkersInProgressiveDcScan()
    {
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = CreateDcScan(frame, 0, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(CreateScanData(scan, CreateDcEntropyWithRestart(scan, new int[] { 2, 2 }, 1)));
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.DecodeWithRestartInterval(frame, scans, tables, 1);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(138));
    }

    [Test]
    public void Decoder_RejectsUnsupportedProgressiveInputs()
    {
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();
        global::jpeg.JpegFrameHeader frame = CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(CreateScanData(CreateDcScan(frame, 0, 0), new byte[0]));

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegFrameHeader baseline = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
                baseline.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
                baseline.ComponentCount = 1;
                decoder.Decode(baseline, scans, tables);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(frame, new List<global::jpeg.JpegProgressiveScanData>(), tables);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegScanHeader ac = CreateSingleComponentScan(1, 5, 0, 0, 1);
                ac.Components.Add(ScanHeaderWriterTests.CreateComponent(2, 0, 0));
                ac.ComponentCount = 2;
                List<global::jpeg.JpegProgressiveScanData> badScans = new List<global::jpeg.JpegProgressiveScanData>();
                badScans.Add(CreateScanData(ac, new byte[0]));
                decoder.Decode(frame, badScans, tables);
            });
    }

    public static global::jpeg.JpegFrameHeader CreateProgressiveFrame(int width, int height)
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(width, height);
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame2;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame2;
        frame.Process = global::jpeg.JpegProcess.ProgressiveDct;
        frame.Progressive = true;
        return frame;
    }

    public static global::jpeg.JpegScanHeader CreateDcScan(global::jpeg.JpegFrameHeader frame, int successiveHigh, int successiveLow)
    {
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        scan.SpectralStart = 0;
        scan.SpectralEnd = 0;
        scan.SuccessiveHigh = successiveHigh;
        scan.SuccessiveLow = successiveLow;
        return scan;
    }

    public static global::jpeg.JpegScanHeader CreateSingleComponentScan(
        int spectralStart,
        int spectralEnd,
        int successiveHigh,
        int successiveLow,
        int selector)
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = 1;
        scan.Components.Add(ScanHeaderWriterTests.CreateComponent(selector, 0, 0));
        scan.SpectralStart = spectralStart;
        scan.SpectralEnd = spectralEnd;
        scan.SuccessiveHigh = successiveHigh;
        scan.SuccessiveLow = successiveLow;
        return scan;
    }

    public static global::jpeg.JpegProgressiveScanData CreateScanData(global::jpeg.JpegScanHeader scan, byte[] entropyData)
    {
        global::jpeg.JpegProgressiveScanData data = new global::jpeg.JpegProgressiveScanData();
        data.Scan = scan;
        data.EntropyData = entropyData;
        return data;
    }

    public static byte[] CreateDcEntropy(global::jpeg.JpegScanHeader scan, int[] dcCoefficients, int[] componentIndexes)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        int index = 0;
        while (index < dcCoefficients.Length)
        {
            int[] block = new int[global::jpeg.JpegConstants.BlockSampleCount];
            block[0] = dcCoefficients[index];
            global::jpeg.JpegScanComponent component = scan.Components[componentIndexes[index]];
            encoder.EncodeDcFirstBlockForScanComponent(scanComponentTables(), scan, component, block, componentIndexes[index]);
            index = index + 1;
        }

        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static byte[] CreateDcEntropyWithRestart(global::jpeg.JpegScanHeader scan, int[] dcCoefficients, int restartInterval)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        encoder.ConfigureRestartInterval(restartInterval);
        int index = 0;
        while (index < dcCoefficients.Length)
        {
            int[] block = new int[global::jpeg.JpegConstants.BlockSampleCount];
            block[0] = dcCoefficients[index];
            encoder.EncodeDcFirstBlockForScanComponent(scanComponentTables(), scan, scan.Components[0], block, 0);
            encoder.RecordMcuEncoded();
            if (encoder.ShouldWriteRestartMarker() && index + 1 < dcCoefficients.Length)
            {
                encoder.WriteRestartMarker();
            }

            index = index + 1;
        }

        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static byte[] CreateDcRefinementEntropy(int dcCoefficient)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        int[] block = new int[global::jpeg.JpegConstants.BlockSampleCount];
        block[0] = dcCoefficient;
        encoder.EncodeDcRefinementBlock(block, 0);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static byte[] CreateAcFirstEntropy(global::jpeg.JpegScanHeader scan, int acCoefficient)
    {
        global::jpeg.JpegProgressiveHuffmanEntropyEncoder encoder = new global::jpeg.JpegProgressiveHuffmanEntropyEncoder();
        int[] block = new int[global::jpeg.JpegConstants.BlockSampleCount];
        block[scan.SpectralStart] = acCoefficient;
        encoder.EncodeAcFirstBlockForScan(scan, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), block);
        encoder.FlushToByteBoundary();
        return encoder.ToArray();
    }

    public static List<global::jpeg.JpegHuffmanTable> scanComponentTables()
    {
        List<global::jpeg.JpegHuffmanTable> tables = new List<global::jpeg.JpegHuffmanTable>();
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        tables.Add(BaselineHuffmanEntropyDecoderTests.CreateAcTable());
        return tables;
    }
}
