namespace jpeg.Tests;

internal class JpegEncoderComponentOptionsTests
{
    [Test]
    public void WriteOptions_ApplyExplicitComponentOrderIdentifiersAndArbitrarySampling()
    {
        global::jpeg.JpegImage source = CreateYcbcrImage(24, 16, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.ScanLayout = global::jpeg.JpegScanLayout.Interleaved;
        options.Components.Add(CreateComponent(2, 30, 1, 1));
        options.Components.Add(CreateComponent(0, 10, 3, 2));
        options.Components.Add(CreateComponent(1, 20, 1, 1));

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.RawComponentOutput = true;
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data, readOptions);

        Assert.That(info.Components[0].Identifier, Is.EqualTo(30));
        Assert.That(info.Components[0].HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(info.Components[0].VerticalSamplingFactor, Is.EqualTo(1));
        Assert.That(info.Components[1].Identifier, Is.EqualTo(10));
        Assert.That(info.Components[1].HorizontalSamplingFactor, Is.EqualTo(3));
        Assert.That(info.Components[1].VerticalSamplingFactor, Is.EqualTo(2));
        Assert.That(info.Components[2].Identifier, Is.EqualTo(20));
        Assert.That(info.LastScanHeader.Components.Select(item => item.Selector), Is.EqualTo(new int[] { 30, 10, 20 }));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Custom));
        Assert.That(decoded.PlanarBuffer.Components[0].Identifier, Is.EqualTo(30));
        Assert.That(decoded.PlanarBuffer.Components[0].Width, Is.EqualTo(8));
        Assert.That(decoded.PlanarBuffer.Components[0].Height, Is.EqualTo(8));
        Assert.That(decoded.PlanarBuffer.Components[0].Samples[0], Is.InRange(139, 141));
        Assert.That(decoded.PlanarBuffer.Components[1].Identifier, Is.EqualTo(10));
        Assert.That(decoded.PlanarBuffer.Components[1].Width, Is.EqualTo(24));
        Assert.That(decoded.PlanarBuffer.Components[1].Height, Is.EqualTo(16));
        Assert.That(decoded.PlanarBuffer.Components[1].Samples[0], Is.InRange(99, 101));
        Assert.That(decoded.PlanarBuffer.Components[2].Identifier, Is.EqualTo(20));
        Assert.That(decoded.PlanarBuffer.Components[2].Width, Is.EqualTo(8));
        Assert.That(decoded.PlanarBuffer.Components[2].Height, Is.EqualTo(8));
        Assert.That(decoded.PlanarBuffer.Components[2].Samples[0], Is.InRange(119, 121));
    }

    [Test]
    public void WriteOptions_ApplyNamedSubsamplingPreset()
    {
        global::jpeg.JpegImage source = CreateYcbcrImage(17, 9, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio420;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);

        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        Assert.That(info.Components[0].HorizontalSamplingFactor, Is.EqualTo(2));
        Assert.That(info.Components[0].VerticalSamplingFactor, Is.EqualTo(2));
        Assert.That(info.Components[1].HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(info.Components[1].VerticalSamplingFactor, Is.EqualTo(1));
        Assert.That(info.Components[2].HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(info.Components[2].VerticalSamplingFactor, Is.EqualTo(1));
    }

    [Test]
    public void WriteOptions_EncodeDecodableNonInterleavedSequentialScans()
    {
        global::jpeg.JpegImage source = CreateYcbcrImage(17, 9, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.ScanLayout = global::jpeg.JpegScanLayout.NonInterleaved;
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio420;
        options.OptimizeHuffmanTables = true;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegTableStore tables = reader.CreateTableStore(info);
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(info.Frame);

        Assert.That(info.ScanCount, Is.EqualTo(3));
        Assert.That(scans.Count, Is.EqualTo(3));
        Assert.That(scans.Select(item => item.Scan.Components[0].Selector), Is.EqualTo(new int[] { 1, 2, 3 }));
        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            global::jpeg.JpegProgressiveScanData scanData = scans[scanIndex];
            global::jpeg.JpegMcuComponentLayout componentLayout = new global::jpeg.JpegMcuLayoutCalculator().FindComponentLayout(
                layout,
                scanData.Scan.Components[0].Selector);
            global::jpeg.JpegBaselineHuffmanEntropyDecoder decoder =
                new global::jpeg.JpegBaselineHuffmanEntropyDecoder(scanData.EntropyData);
            int blockIndex = 0;
            while (blockIndex < componentLayout.TotalBlockCount)
            {
                int[] block = decoder.DecodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanData.Scan.Components[0],
                    componentLayout.ComponentIndex);
                Assert.That(block, Has.Length.EqualTo(64));
                blockIndex = blockIndex + 1;
            }

            scanIndex = scanIndex + 1;
        }
    }

    [Test]
    public void WriteOptions_EncodeDecodableNonInterleavedProgressiveScans()
    {
        global::jpeg.JpegImage source = CreateYcbcrImage(17, 9, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        options.ScanLayout = global::jpeg.JpegScanLayout.NonInterleaved;
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio422;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegPlanarImageBuffer decoded = new global::jpeg.JpegProgressiveSof2FullImageDecoder().Decode(
            info.Frame,
            scans,
            reader.CreateTableStore(info));

        Assert.That(info.ScanCount, Is.EqualTo(6));
        Assert.That(scans.All(item => item.Scan.Components.Count == 1), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.InRange(99, 101));
        Assert.That(decoded.Components[1].Samples[0], Is.InRange(119, 121));
        Assert.That(decoded.Components[2].Samples[0], Is.InRange(139, 141));
    }

    [Test]
    public void WriteOptions_EncodeDecodableNonInterleavedExtendedSequentialScan()
    {
        int[] samples = new int[64];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = 2058;
            index = index + 1;
        }

        global::jpeg.JpegImage source = new global::jpeg.JpegImageFactory().CreatePackedImageFromIntSamples(
            samples,
            8,
            8,
            1,
            8,
            12,
            global::jpeg.JpegColorSpace.Grayscale);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ExtendedSequentialDct;
        options.SamplePrecision = 12;
        options.ScanLayout = global::jpeg.JpegScanLayout.NonInterleaved;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegExtendedSequentialSof1Decoder().Decode(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData);

        Assert.That(info.ScanCount, Is.EqualTo(1));
        Assert.That(info.LastScanComponentCount, Is.EqualTo(1));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[63], Is.EqualTo(2058));
    }

    [Test]
    public void WriteOptions_RejectInvalidExplicitComponentMappings()
    {
        global::jpeg.JpegImage source = CreateYcbcrImage(8, 8, 100, 120, 140);
        global::jpeg.JpegWriteOptions duplicateSource = CreateIdentityOptions();
        duplicateSource.Components[2].SourceComponentIndex = 1;
        global::jpeg.JpegWriteOptions duplicateIdentifier = CreateIdentityOptions();
        duplicateIdentifier.Components[2].Identifier = 2;
        global::jpeg.JpegWriteOptions invalidSampling = CreateIdentityOptions();
        invalidSampling.Components[0].HorizontalSamplingFactor = 5;
        global::jpeg.JpegWriteOptions excessiveInterleavedSampling = CreateIdentityOptions();
        excessiveInterleavedSampling.Components[0].HorizontalSamplingFactor = 4;
        excessiveInterleavedSampling.Components[0].VerticalSamplingFactor = 4;

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, duplicateSource); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, duplicateIdentifier); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, invalidSampling); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, excessiveInterleavedSampling); });
    }

    internal static global::jpeg.JpegWriteOptions CreateIdentityOptions()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Components.Add(CreateComponent(0, 1, 1, 1));
        options.Components.Add(CreateComponent(1, 2, 1, 1));
        options.Components.Add(CreateComponent(2, 3, 1, 1));
        return options;
    }

    internal static global::jpeg.JpegEncoderComponent CreateComponent(
        int sourceIndex,
        int identifier,
        int horizontalSamplingFactor,
        int verticalSamplingFactor)
    {
        global::jpeg.JpegEncoderComponent component = new global::jpeg.JpegEncoderComponent();
        component.SourceComponentIndex = sourceIndex;
        component.Identifier = identifier;
        component.HorizontalSamplingFactor = horizontalSamplingFactor;
        component.VerticalSamplingFactor = verticalSamplingFactor;
        return component;
    }

    internal static global::jpeg.JpegImage CreateYcbcrImage(
        int width,
        int height,
        int first,
        int second,
        int third)
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        image.ColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        image.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        image.Pixels = new byte[width * height * 3];
        int index = 0;
        while (index < image.Pixels.Length)
        {
            image.Pixels[index] = (byte)first;
            image.Pixels[index + 1] = (byte)second;
            image.Pixels[index + 2] = (byte)third;
            index = index + 3;
        }

        return image;
    }
}
