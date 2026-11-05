using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveCustomScanScriptTests
{
    [Test]
    public void Encoder_WritesCustomSplitAcScanScript()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(50);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        ProgressiveSof2EncoderTests.FillCenteredBlock(source.Components[0], IntegerForwardDctTests.CreateHorizontalAcPattern());
        List<global::jpeg.JpegScanHeader> script = new List<global::jpeg.JpegScanHeader>();
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0));
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 1, 0, 0, 1));
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(2, 63, 0, 0, 1));
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.EncodeWithScanScript(frame, tables, source, script);
        global::jpeg.JpegProgressiveCoefficientBuffer coefficients = decoder.DecodeCoefficients(frame, scans, tables);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(scans.Count, Is.EqualTo(3));
        Assert.That(scans[1].Scan.SpectralStart, Is.EqualTo(1));
        Assert.That(scans[1].Scan.SpectralEnd, Is.EqualTo(1));
        Assert.That(coefficients.Components[0].Coefficients[1], Is.EqualTo(2));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(145));
        Assert.That(decoded.Components[0].Samples[7], Is.EqualTo(111));
    }

    [Test]
    public void Encoder_WritesCustomSuccessiveApproximationScript()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 143);
        List<global::jpeg.JpegScanHeader> script = new List<global::jpeg.JpegScanHeader>();
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 1));
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 1, 0));
        script.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 63, 0, 0, 1));
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.EncodeWithScanScript(frame, tables, source, script);
        global::jpeg.JpegProgressiveCoefficientBuffer coefficients = decoder.DecodeCoefficients(frame, scans, tables);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(scans.Count, Is.EqualTo(3));
        Assert.That(scans[0].Scan.SuccessiveLow, Is.EqualTo(1));
        Assert.That(scans[1].Scan.SuccessiveHigh, Is.EqualTo(1));
        Assert.That(coefficients.Components[0].Coefficients[0], Is.EqualTo(3));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(143));
    }

    [Test]
    public void Encoder_RejectsIncompleteDuplicateAndOutOfOrderCustomScripts()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                List<global::jpeg.JpegScanHeader> incomplete = new List<global::jpeg.JpegScanHeader>();
                incomplete.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0));
                encoder.EncodeWithScanScript(frame, tables, source, incomplete);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                List<global::jpeg.JpegScanHeader> duplicate = new List<global::jpeg.JpegScanHeader>();
                duplicate.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0));
                duplicate.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0));
                duplicate.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 63, 0, 0, 1));
                encoder.EncodeWithScanScript(frame, tables, source, duplicate);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                List<global::jpeg.JpegScanHeader> outOfOrder = new List<global::jpeg.JpegScanHeader>();
                outOfOrder.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 63, 0, 0, 1));
                outOfOrder.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0));
                encoder.EncodeWithScanScript(frame, tables, source, outOfOrder);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                List<global::jpeg.JpegScanHeader> missingRefinement = new List<global::jpeg.JpegScanHeader>();
                missingRefinement.Add(ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 1));
                missingRefinement.Add(ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 63, 0, 0, 1));
                encoder.EncodeWithScanScript(frame, tables, source, missingRefinement);
            });
    }
}
