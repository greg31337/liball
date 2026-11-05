namespace jpeg.Tests;

internal class EnumDefinitionTests
{
    [Test]
    public void PublicEnums_AreDefinedForCoreApiConcepts()
    {
        Assert.That(typeof(global::jpeg.JpegProcess).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegPixelFormat).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegColorSpace).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegSubsampling).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegChromaDownsamplingFilter).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegMetadataPolicy).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegColorPolicy).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegIccBehaviorPolicy).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegTiffByteOrder).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegErrorPolicy).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegInverseDctMethod).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegDiagnosticSeverity).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegDiagnosticCode).IsEnum, Is.True);
        Assert.That(typeof(global::jpeg.JpegMarker).IsEnum, Is.True);
    }

    [Test]
    public void MarkerEnum_UsesFullJpegMarkerValues()
    {
        Assert.That((int)global::jpeg.JpegMarker.StartOfImage, Is.EqualTo(0xFFD8));
        Assert.That((int)global::jpeg.JpegMarker.EndOfImage, Is.EqualTo(0xFFD9));
        Assert.That((int)global::jpeg.JpegMarker.StartOfScan, Is.EqualTo(0xFFDA));
        Assert.That((int)global::jpeg.JpegMarker.DefineQuantizationTable, Is.EqualTo(0xFFDB));
        Assert.That((int)global::jpeg.JpegMarker.DefineHuffmanTable, Is.EqualTo(0xFFC4));
        Assert.That((int)global::jpeg.JpegMarker.DefineRestartInterval, Is.EqualTo(0xFFDD));
        Assert.That((int)global::jpeg.JpegMarker.App0, Is.EqualTo(0xFFE0));
        Assert.That((int)global::jpeg.JpegMarker.App15, Is.EqualTo(0xFFEF));
        Assert.That((int)global::jpeg.JpegMarker.JpegLs, Is.EqualTo(0xFFF7));
        Assert.That((int)global::jpeg.JpegMarker.Comment, Is.EqualTo(0xFFFE));
    }

    [Test]
    public void ProcessEnum_MapsToStartOfFrameMarkers()
    {
        Assert.That((int)global::jpeg.JpegProcess.BaselineDct, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfFrame0));
        Assert.That((int)global::jpeg.JpegProcess.ExtendedSequentialDct, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfFrame1));
        Assert.That((int)global::jpeg.JpegProcess.ProgressiveDct, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfFrame2));
        Assert.That((int)global::jpeg.JpegProcess.LosslessSequential, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfFrame3));
        Assert.That((int)global::jpeg.JpegProcess.JpegLs, Is.EqualTo((int)global::jpeg.JpegMarker.JpegLs));
    }

    [Test]
    public void PolicyEnums_HaveExplicitDefaults()
    {
        Assert.That(global::jpeg.JpegMetadataPolicy.PreserveAll, Is.EqualTo(default(global::jpeg.JpegMetadataPolicy)));
        Assert.That(global::jpeg.JpegColorPolicy.PreserveSource, Is.EqualTo(default(global::jpeg.JpegColorPolicy)));
        Assert.That(global::jpeg.JpegIccBehaviorPolicy.Preserve, Is.EqualTo(default(global::jpeg.JpegIccBehaviorPolicy)));
        Assert.That(global::jpeg.JpegTiffByteOrder.Unknown, Is.EqualTo(default(global::jpeg.JpegTiffByteOrder)));
        Assert.That(global::jpeg.JpegErrorPolicy.Strict, Is.EqualTo(default(global::jpeg.JpegErrorPolicy)));
        Assert.That(global::jpeg.JpegInverseDctMethod.Accurate, Is.EqualTo(default(global::jpeg.JpegInverseDctMethod)));
        Assert.That(global::jpeg.JpegChromaDownsamplingFilter.Box, Is.EqualTo(default(global::jpeg.JpegChromaDownsamplingFilter)));
        Assert.That(global::jpeg.JpegDiagnosticSeverity.Info, Is.EqualTo(default(global::jpeg.JpegDiagnosticSeverity)));
        Assert.That(global::jpeg.JpegDiagnosticCode.None, Is.EqualTo(default(global::jpeg.JpegDiagnosticCode)));
    }
}
