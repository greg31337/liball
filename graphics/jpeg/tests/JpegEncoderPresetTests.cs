namespace jpeg.Tests;

internal class JpegEncoderPresetTests
{
    [Test]
    public void NonePreset_PreservesIndividualSettingsAndReportsResolution()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Preset = global::jpeg.JpegEncoderPreset.None;
        options.Quality = 91;
        options.Subsampling = global::jpeg.JpegSubsampling.Grayscale;
        options.RestartInterval = 2;

        global::jpeg.JpegWriteResult result = new global::jpeg.JpegWriter().WriteWithResult(
            JpegWriteApiTests.CreateGrayscaleImage(16, 8, 138), options);

        Assert.That(result.ResolvedOptions.Preset, Is.EqualTo(global::jpeg.JpegEncoderPreset.None));
        Assert.That(result.ResolvedOptions.Quality, Is.EqualTo(91));
        Assert.That(result.ResolvedOptions.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
        Assert.That(result.ResolvedOptions.RestartInterval, Is.EqualTo(2));
    }

    [TestCase(global::jpeg.JpegEncoderPreset.Web, global::jpeg.JpegProcess.ProgressiveDct, 82, global::jpeg.JpegSubsampling.Ratio420, global::jpeg.JpegMetadataPolicy.StripUnsafe, false, false)]
    [TestCase(global::jpeg.JpegEncoderPreset.Archival, global::jpeg.JpegProcess.ProgressiveDct, 95, global::jpeg.JpegSubsampling.Ratio444, global::jpeg.JpegMetadataPolicy.PreserveAll, false, true)]
    [TestCase(global::jpeg.JpegEncoderPreset.MaximumCompatibility, global::jpeg.JpegProcess.BaselineDct, 75, global::jpeg.JpegSubsampling.Ratio420, global::jpeg.JpegMetadataPolicy.PreserveKnown, false, true)]
    [TestCase(global::jpeg.JpegEncoderPreset.MaximumCompression, global::jpeg.JpegProcess.BaselineDct, 60, global::jpeg.JpegSubsampling.Ratio420, global::jpeg.JpegMetadataPolicy.StripAll, true, false)]
    public void WriteWithResult_AppliesDctPresetAndReportsResolvedSettings(
        global::jpeg.JpegEncoderPreset preset,
        global::jpeg.JpegProcess process,
        int quality,
        global::jpeg.JpegSubsampling subsampling,
        global::jpeg.JpegMetadataPolicy metadataPolicy,
        bool optimizeHuffmanTables,
        bool preservesComment)
    {
        global::jpeg.JpegImage source = JpegEncoderComponentOptionsTests.CreateYcbcrImage(17, 9, 100, 120, 140);
        source.Metadata.Comments.Add("private note");
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Preset = preset;
        options.Process = global::jpeg.JpegProcess.LosslessSequential;
        options.Quality = 99;
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.ReplaceSelected;
        options.OptimizeHuffmanTables = !optimizeHuffmanTables;

        global::jpeg.JpegWriteResult result = new global::jpeg.JpegWriter().WriteWithResult(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result.Data);

        Assert.That(result.ResolvedOptions.Preset, Is.EqualTo(preset));
        Assert.That(result.ResolvedOptions.Process, Is.EqualTo(process));
        Assert.That(result.ResolvedOptions.SamplePrecision, Is.EqualTo(8));
        Assert.That(result.ResolvedOptions.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Huffman));
        Assert.That(result.ResolvedOptions.Quality, Is.EqualTo(quality));
        Assert.That(result.ResolvedOptions.Subsampling, Is.EqualTo(subsampling));
        Assert.That(result.ResolvedOptions.ScanLayout, Is.EqualTo(global::jpeg.JpegScanLayout.Interleaved));
        Assert.That(result.ResolvedOptions.MetadataPolicy, Is.EqualTo(metadataPolicy));
        Assert.That(result.ResolvedOptions.OptimizeHuffmanTables, Is.EqualTo(optimizeHuffmanTables));
        Assert.That(info.Process, Is.EqualTo(process));
        Assert.That(info.Subsampling, Is.EqualTo(subsampling));
        Assert.That(info.Progressive, Is.EqualTo(process == global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(info.CommentCount > 0, Is.EqualTo(preservesComment));
        Assert.That(options.Process, Is.EqualTo(global::jpeg.JpegProcess.LosslessSequential));
        Assert.That(options.Quality, Is.EqualTo(99));
    }

    [Test]
    public void MetadataPreservingPreset_RetainsMetadataSamplingAndQuantizationTables()
    {
        global::jpeg.JpegImage original = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        original.Metadata.Comments.Add("retained note");
        global::jpeg.JpegWriteOptions initialOptions = new global::jpeg.JpegWriteOptions();
        initialOptions.Tables.QuantizationTables.Add(JpegEncoderTableOptionsTests.CreateQuantizationTable(0, 13));
        byte[] initialData = new global::jpeg.JpegWriter().WriteWithOptions(original, initialOptions);
        global::jpeg.JpegImage source = global::jpeg.Jpeg.Read(initialData);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Preset = global::jpeg.JpegEncoderPreset.MetadataPreserving;
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;

        global::jpeg.JpegWriteResult result = new global::jpeg.JpegWriter().WriteWithResult(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result.Data);

        Assert.That(result.ResolvedOptions.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
        Assert.That(result.ResolvedOptions.PreserveQuantizationTables, Is.True);
        Assert.That(result.ResolvedOptions.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "retained note" }));
        Assert.That(info.QuantizationTables[0].Values, Is.All.EqualTo(13));
    }

    [Test]
    public void DeterministicOutput_IsRepeatableCanonicalAndDoesNotMutateCallerOptions()
    {
        global::jpeg.JpegImage source = JpegEncoderComponentOptionsTests.CreateYcbcrImage(17, 9, 100, 120, 140);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Deterministic = true;
        options.OptimizeHuffmanTables = true;
        options.RestartInterval = 1;
        options.RestartIntervalUnit = global::jpeg.JpegRestartIntervalUnit.McuRows;
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();

        global::jpeg.JpegWriteResult first = writer.WriteWithResult(source, options);
        global::jpeg.JpegWriteResult second = writer.WriteWithResult(source, options);
        global::jpeg.JpegWriteOptions optimizedOptions = new global::jpeg.JpegWriteOptions();
        optimizedOptions.OptimizeHuffmanTables = true;
        byte[] optimized = writer.WriteWithOptions(source, optimizedOptions);

        Assert.That(first.Data, Is.EqualTo(second.Data));
        Assert.That(first.Data, Is.Not.EqualTo(optimized));
        Assert.That(first.ResolvedOptions.Deterministic, Is.True);
        Assert.That(first.ResolvedOptions.OptimizeHuffmanTables, Is.False);
        Assert.That(first.ResolvedOptions.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(first.ResolvedOptions.SamplePrecision, Is.EqualTo(8));
        Assert.That(first.ResolvedOptions.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Huffman));
        Assert.That(first.ResolvedOptions.ScanLayout, Is.EqualTo(global::jpeg.JpegScanLayout.Interleaved));
        Assert.That(first.ResolvedOptions.RestartIntervalUnit, Is.EqualTo(global::jpeg.JpegRestartIntervalUnit.McuBlocks));
        Assert.That(first.ResolvedOptions.RestartInterval, Is.EqualTo(3));
        Assert.That(options.OptimizeHuffmanTables, Is.True);
        Assert.That(options.RestartIntervalUnit, Is.EqualTo(global::jpeg.JpegRestartIntervalUnit.McuRows));
    }

    [Test]
    public void WriteWithResult_RejectsUnknownPreset()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Preset = (global::jpeg.JpegEncoderPreset)99;

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            () => new global::jpeg.JpegWriter().WriteWithResult(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138), options));
    }
}
