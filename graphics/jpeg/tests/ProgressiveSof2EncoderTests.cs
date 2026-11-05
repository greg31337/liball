using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveSof2EncoderTests
{
    [Test]
    public void Encoder_CreatesDefaultDcThenPerComponentAcScanScript()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();

        List<global::jpeg.JpegScanHeader> scans = encoder.CreateDefaultScanScript(frame);

        Assert.That(scans.Count, Is.EqualTo(4));
        Assert.That(scans[0].ComponentCount, Is.EqualTo(3));
        Assert.That(scans[0].SpectralStart, Is.EqualTo(0));
        Assert.That(scans[0].SpectralEnd, Is.EqualTo(0));
        Assert.That(scans[1].ComponentCount, Is.EqualTo(1));
        Assert.That(scans[1].Components[0].Selector, Is.EqualTo(1));
        Assert.That(scans[1].SpectralStart, Is.EqualTo(1));
        Assert.That(scans[1].SpectralEnd, Is.EqualTo(63));
        Assert.That(scans[3].Components[0].Selector, Is.EqualTo(3));
    }

    [Test]
    public void Encoder_EncodesDefaultProgressiveScansThatDecodeBack()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 8, 0, 143);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.Encode(frame, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(scans.Count, Is.EqualTo(2));
        Assert.That(scans[0].Scan.SpectralStart, Is.EqualTo(0));
        Assert.That(scans[1].Scan.SpectralStart, Is.EqualTo(1));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
    }

    [Test]
    public void Encoder_EncodesAcCoefficientsInDefaultAcScan()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(50);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        FillCenteredBlock(source.Components[0], IntegerForwardDctTests.CreateHorizontalAcPattern());
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.Encode(frame, tables, source);
        global::jpeg.JpegProgressiveCoefficientBuffer coefficients = decoder.DecodeCoefficients(frame, scans, tables);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(coefficients.Components[0].Coefficients[0], Is.EqualTo(0));
        Assert.That(coefficients.Components[0].Coefficients[1], Is.EqualTo(2));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(145));
        Assert.That(decoded.Components[0].Samples[7], Is.EqualTo(111));
    }

    [Test]
    public void Encoder_WritesRestartMarkersInDefaultScans()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 8, 0, 138);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.EncodeWithRestartInterval(frame, tables, source, 1);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.DecodeWithRestartInterval(frame, scans, tables, 1);

        Assert.That(BaselineSof0EncoderTests.ContainsRestart0(scans[0].EntropyData), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(138));
    }

    [Test]
    public void Encoder_RejectsInvalidProgressiveInputsAndUnsupportedScript()
    {
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegFrameHeader baseline = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
                baseline.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
                baseline.ComponentCount = 1;
                encoder.Encode(baseline, tables, source);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarImageBuffer badSource = BaselineSof0EncoderTests.CreatePlanarSource(frame);
                badSource.ComponentCount = 2;
                encoder.Encode(frame, tables, badSource);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                List<global::jpeg.JpegScanHeader> scanScript = new List<global::jpeg.JpegScanHeader>();
                scanScript.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 1, 0));
                encoder.EncodeScanScript(frame, tables, encoder.BuildCoefficientBuffer(frame, tables, source), scanScript, 0);
            });
    }

    [Test]
    public void Encoder_AlignsExtremeNegativeCoefficientsWithoutOverflow()
    {
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();

        Assert.That(encoder.AlignCoefficientForFirstScan(int.MinValue, 15), Is.EqualTo(int.MinValue));
        Assert.That(encoder.AlignCoefficientForFirstScan(int.MinValue + 1, 1), Is.EqualTo(int.MinValue));
    }

    public static void FillCenteredBlock(global::jpeg.JpegPlanarComponentBuffer component, int[] centeredSamples)
    {
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            component.Samples[index] = (byte)(centeredSamples[index] + 128);
            index = index + 1;
        }
    }
}
