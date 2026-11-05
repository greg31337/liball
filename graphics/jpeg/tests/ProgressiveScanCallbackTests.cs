using System.Collections.Generic;

namespace jpeg.Tests;

internal class ProgressiveScanCallbackTests
{
    [Test]
    public void Decoder_InvokesCallbackAfterEachScanWithPreview()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader dcFirst = ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 1);
        global::jpeg.JpegScanHeader dcRefinement = ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 1, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(dcFirst, ProgressiveSof2FullImageDecoderTests.CreateDcEntropy(dcFirst, new int[] { 2 }, new int[] { 0 })));
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(dcRefinement, ProgressiveSof2FullImageDecoderTests.CreateDcRefinementEntropy(3)));
        List<global::jpeg.JpegProgressiveScanCallbackUpdate> updates = new List<global::jpeg.JpegProgressiveScanCallbackUpdate>();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        global::jpeg.JpegPlanarImageBuffer image = decoder.DecodeWithScanCallback(
            frame,
            scans,
            tables,
            delegate (global::jpeg.JpegProgressiveScanCallbackUpdate update)
            {
                updates.Add(update);
            });

        Assert.That(updates.Count, Is.EqualTo(2));
        Assert.That(updates[0].ScanIndex, Is.EqualTo(0));
        Assert.That(updates[0].ScanCount, Is.EqualTo(2));
        Assert.That(updates[0].Completed, Is.False);
        Assert.That(updates[0].Scan, Is.SameAs(dcFirst));
        Assert.That(updates[0].Coefficients.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(updates[0].Preview.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(updates[1].ScanIndex, Is.EqualTo(1));
        Assert.That(updates[1].Completed, Is.True);
        Assert.That(updates[1].Preview.Components[0].Samples[0], Is.EqualTo(143));
        Assert.That(image.Components[0].Samples[0], Is.EqualTo(143));
    }

    [Test]
    public void Decoder_CallbackReceivesCoefficientUpdatesAfterAcScan()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader dcScan = ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0);
        global::jpeg.JpegScanHeader acScan = ProgressiveSof2FullImageDecoderTests.CreateSingleComponentScan(1, 1, 0, 0, 1);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(1);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(dcScan, ProgressiveSof2FullImageDecoderTests.CreateDcEntropy(dcScan, new int[] { 2 }, new int[] { 0 })));
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(acScan, ProgressiveSof2FullImageDecoderTests.CreateAcFirstEntropy(acScan, -2)));
        List<int> acCoefficientAfterScan = new List<int>();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        decoder.DecodeWithScanCallback(
            frame,
            scans,
            tables,
            delegate (global::jpeg.JpegProgressiveScanCallbackUpdate update)
            {
                acCoefficientAfterScan.Add(update.Coefficients.Components[0].Coefficients[1]);
            });

        Assert.That(acCoefficientAfterScan.Count, Is.EqualTo(2));
        Assert.That(acCoefficientAfterScan[0], Is.EqualTo(0));
        Assert.That(acCoefficientAfterScan[1], Is.EqualTo(-2));
    }

    [Test]
    public void Decoder_RejectsNullProgressiveScanCallback()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader dcScan = ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        List<global::jpeg.JpegProgressiveScanData> scans = new List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(dcScan, new byte[0]));
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeWithScanCallback(frame, scans, tables, null!);
            });
    }
}
