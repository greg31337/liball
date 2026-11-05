namespace jpeg.Tests;

internal class JpegProgressiveScanScriptOptionsTests
{
    [Test]
    public void WriteOptions_EncodeCustomFirstAndRefinementScanScript()
    {
        global::jpeg.JpegImage source = CreatePatternImage();
        global::jpeg.JpegWriteOptions customOptions = new global::jpeg.JpegWriteOptions();
        customOptions.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 0, 1, 1));
        customOptions.ProgressiveScanScript.Scans.Add(CreateScan(1, 5, 0, 1, 1));
        customOptions.ProgressiveScanScript.Scans.Add(CreateScan(6, 63, 0, 0, 1));
        customOptions.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 1, 0, 1));
        customOptions.ProgressiveScanScript.Scans.Add(CreateScan(1, 5, 1, 0, 1));
        global::jpeg.JpegWriteOptions standardOptions = new global::jpeg.JpegWriteOptions();
        standardOptions.Process = global::jpeg.JpegProcess.ProgressiveDct;

        byte[] customData = new global::jpeg.JpegWriter().WriteWithOptions(source, customOptions);
        byte[] standardData = new global::jpeg.JpegWriter().WriteWithOptions(source, standardOptions);
        global::jpeg.JpegInfo customInfo = global::jpeg.JpegIdentifier.Identify(customData);
        global::jpeg.JpegPlanarImageBuffer customDecoded = DecodeProgressive(customData, customInfo);
        global::jpeg.JpegInfo standardInfo = global::jpeg.JpegIdentifier.Identify(standardData);
        global::jpeg.JpegPlanarImageBuffer standardDecoded = DecodeProgressive(standardData, standardInfo);

        Assert.That(customInfo.ScanCount, Is.EqualTo(5));
        Assert.That(customInfo.ScanHeaders[0].SuccessiveLow, Is.EqualTo(1));
        Assert.That(customInfo.ScanHeaders[1].SpectralStart, Is.EqualTo(1));
        Assert.That(customInfo.ScanHeaders[1].SpectralEnd, Is.EqualTo(5));
        Assert.That(customInfo.ScanHeaders[3].SuccessiveHigh, Is.EqualTo(1));
        Assert.That(customInfo.ScanHeaders[3].SuccessiveLow, Is.EqualTo(0));
        Assert.That(customInfo.ScanHeaders[4].SpectralEnd, Is.EqualTo(5));
        Assert.That(customDecoded.Components[0].Samples, Is.EqualTo(standardDecoded.Components[0].Samples));
    }

    [Test]
    public void StandardFactory_CreatesScriptUsableByPublicWriter()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(10, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(20, 1, 1, 1));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(30, 1, 1, 1));
        frame.ComponentCount = 3;
        global::jpeg.JpegProgressiveScanScript script = new global::jpeg.JpegProgressiveScanScriptFactory().CreateStandardScript(frame);
        global::jpeg.JpegImage source = JpegEncoderComponentOptionsTests.CreateYcbcrImage(8, 8, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        options.Components.Add(JpegEncoderComponentOptionsTests.CreateComponent(0, 10, 1, 1));
        options.Components.Add(JpegEncoderComponentOptionsTests.CreateComponent(1, 20, 1, 1));
        options.Components.Add(JpegEncoderComponentOptionsTests.CreateComponent(2, 30, 1, 1));
        options.ProgressiveScanScript = script;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);

        Assert.That(script.Scans.Count, Is.EqualTo(4));
        Assert.That(script.Scans[0].Components.Select(item => item.Selector), Is.EqualTo(new int[] { 10, 20, 30 }));
        Assert.That(script.Scans[1].SpectralStart, Is.EqualTo(1));
        Assert.That(script.Scans[3].Components[0].Selector, Is.EqualTo(30));
        Assert.That(info.ScanCount, Is.EqualTo(4));
        Assert.That(info.ScanHeaders[0].Components.Select(item => item.Selector), Is.EqualTo(new int[] { 10, 20, 30 }));
    }

    [Test]
    public void WriteOptions_RejectInvalidProgressiveScanScripts()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        global::jpeg.JpegWriteOptions incomplete = CreateProgressiveOptions();
        incomplete.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 0, 0, 1));
        global::jpeg.JpegWriteOptions acBeforeDc = CreateProgressiveOptions();
        acBeforeDc.ProgressiveScanScript.Scans.Add(CreateScan(1, 63, 0, 0, 1));
        acBeforeDc.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 0, 0, 1));
        global::jpeg.JpegWriteOptions invalidRange = CreateProgressiveOptions();
        invalidRange.ProgressiveScanScript.Scans.Add(CreateScan(0, 5, 0, 0, 1));
        global::jpeg.JpegWriteOptions invalidRefinement = CreateProgressiveOptions();
        invalidRefinement.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 0, 2, 1));
        invalidRefinement.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 2, 0, 1));
        invalidRefinement.ProgressiveScanScript.Scans.Add(CreateScan(1, 63, 0, 0, 1));
        global::jpeg.JpegWriteOptions unknownSelector = CreateProgressiveOptions();
        unknownSelector.ProgressiveScanScript.Scans.Add(CreateScan(0, 0, 0, 0, 2));
        unknownSelector.ProgressiveScanScript.Scans.Add(CreateScan(1, 63, 0, 0, 2));

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, incomplete); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, acBeforeDc); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, invalidRange); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, invalidRefinement); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, unknownSelector); });
    }

    internal static global::jpeg.JpegWriteOptions CreateProgressiveOptions()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        return options;
    }

    internal static global::jpeg.JpegScanHeader CreateScan(
        int spectralStart,
        int spectralEnd,
        int successiveHigh,
        int successiveLow,
        params int[] selectors)
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = selectors.Length;
        scan.SpectralStart = spectralStart;
        scan.SpectralEnd = spectralEnd;
        scan.SuccessiveHigh = successiveHigh;
        scan.SuccessiveLow = successiveLow;
        int index = 0;
        while (index < selectors.Length)
        {
            global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
            component.Selector = selectors[index];
            component.DcEntropyTableIdentifier = 0;
            component.AcEntropyTableIdentifier = 0;
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    internal static global::jpeg.JpegImage CreatePatternImage()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 112);
        int y = 0;
        while (y < image.Height)
        {
            int x = 4;
            while (x < image.Width)
            {
                image.Pixels[y * image.Width + x] = 151;
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    internal static global::jpeg.JpegPlanarImageBuffer DecodeProgressive(byte[] data, global::jpeg.JpegInfo info)
    {
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        return new global::jpeg.JpegProgressiveSof2FullImageDecoder().Decode(
            info.Frame,
            scans,
            reader.CreateTableStore(info));
    }
}
