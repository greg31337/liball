namespace jpeg.Tests;

internal class JpegRestartIntervalUnitTests
{
    [Test]
    public void WriteOptions_DistinguishMcuBlockAndMcuRowRestartIntervals()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(24, 24, 138);
        global::jpeg.JpegWriteOptions blockOptions = new global::jpeg.JpegWriteOptions();
        blockOptions.RestartInterval = 1;
        blockOptions.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuBlocks;
        global::jpeg.JpegWriteOptions rowOptions = new global::jpeg.JpegWriteOptions();
        rowOptions.RestartInterval = 1;
        rowOptions.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuRows;

        byte[] blockData = new global::jpeg.JpegWriter().WriteWithOptions(source, blockOptions);
        byte[] rowData = new global::jpeg.JpegWriter().WriteWithOptions(source, rowOptions);
        global::jpeg.JpegInfo blockInfo = global::jpeg.JpegIdentifier.Identify(blockData);
        global::jpeg.JpegInfo rowInfo = global::jpeg.JpegIdentifier.Identify(rowData);
        global::jpeg.JpegImage blockDecoded = global::jpeg.Jpeg.Read(blockData);
        global::jpeg.JpegImage rowDecoded = global::jpeg.Jpeg.Read(rowData);

        Assert.That(blockInfo.RestartInterval, Is.EqualTo(1));
        Assert.That(blockInfo.ResolvedRestartInterval, Is.EqualTo(1));
        Assert.That(blockInfo.RestartMarkerCount, Is.EqualTo(8));
        Assert.That(rowInfo.RestartInterval, Is.EqualTo(3));
        Assert.That(rowInfo.ResolvedRestartInterval, Is.EqualTo(3));
        Assert.That(rowInfo.RestartMarkerCount, Is.EqualTo(2));
        Assert.That(blockDecoded.Pixels, Is.All.EqualTo(138));
        Assert.That(rowDecoded.Pixels, Is.All.EqualTo(138));
    }

    [Test]
    public void WriteOptions_ResolveLosslessMcuRowsFromImageWidth()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(3, 2, 130);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequential;
        options.RestartInterval = 1;
        options.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuRows;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegLosslessSof3HuffmanDecoder().DecodeWithRestartInterval(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData,
            info.ResolvedRestartInterval);

        Assert.That(info.RestartInterval, Is.EqualTo(3));
        Assert.That(info.ResolvedRestartInterval, Is.EqualTo(3));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(130));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 2], Is.EqualTo(130));
    }

    [Test]
    public void WriteOptions_UseResolvedMcuRowsAcrossProgressiveScans()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(16, 16, 138);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        options.RestartInterval = 1;
        options.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuRows;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegPlanarImageBuffer decoded = new global::jpeg.JpegProgressiveSof2FullImageDecoder().DecodeWithRestartInterval(
            info.Frame,
            scans,
            reader.CreateTableStore(info),
            info.ResolvedRestartInterval);

        Assert.That(info.ResolvedRestartInterval, Is.EqualTo(2));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(2));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[255], Is.EqualTo(138));
    }

    [Test]
    public void WriteOptions_RejectUnknownOrOverflowingRestartIntervalUnits()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(16, 8, 128);
        global::jpeg.JpegWriteOptions unknown = new global::jpeg.JpegWriteOptions();
        unknown.RestartInterval = 1;
        unknown.RestartIntervalUnit = (global::jpeg.JpegRestartIntervalUnit)99;
        global::jpeg.JpegWriteOptions overflow = new global::jpeg.JpegWriteOptions();
        overflow.RestartInterval = 65535;
        overflow.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuRows;

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, unknown); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, overflow); });
    }
}
